import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
const vector = (p) => new THREE.Vector3(p[0], p[2], -p[1]);
function dispose(group) {
  group.traverse((o) => {
    o.geometry?.dispose();
    if (Array.isArray(o.material)) o.material.forEach((m) => m.dispose());
    else o.material?.dispose();
  });
  group.clear();
}
function strip(points, width, color) {
  const positions = [],
    indices = [];
  points.forEach((p, i) => {
    const a = points[Math.max(0, i - 1)],
      b = points[Math.min(points.length - 1, i + 1)],
      dx = b[0] - a[0],
      dy = b[1] - a[1],
      len = Math.hypot(dx, dy) || 1;
    for (const side of [-1, 1])
      positions.push(
        p[0] - (((side * dy) / len) * width) / 2,
        p[2],
        -(p[1] + (((side * dx) / len) * width) / 2),
      );
    if (i) {
      const k = i * 2;
      indices.push(k - 2, k - 1, k, k - 1, k + 1, k);
    }
  });
  const g = new THREE.BufferGeometry();
  g.setAttribute("position", new THREE.Float32BufferAttribute(positions, 3));
  g.setIndex(indices);
  g.computeVertexNormals();
  return new THREE.Mesh(
    g,
    new THREE.MeshStandardMaterial({
      color,
      side: THREE.DoubleSide,
      roughness: 0.85,
    }),
  );
}
export class ConnectionScene {
  constructor() {
    this.renderer = new THREE.WebGLRenderer({
      antialias: true,
      preserveDrawingBuffer: true,
    });
    this.renderer.setPixelRatio(Math.min(devicePixelRatio, 1.5));
    this.renderer.setClearColor(0x101c29);
    this.renderer.domElement.id = "scene-canvas";
    this.renderer.domElement.setAttribute(
      "aria-label",
      "3D connection preview",
    );
    this.renderer.domElement.tabIndex = 0;
    this.labels = document.createElement("div");
    this.labels.className = "scene-label-layer";
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(45, 1, 0.1, 20000);
    this.controls = new OrbitControls(this.camera, this.renderer.domElement);
    this.controls.enableDamping = false;
    this.controls.minDistance = 10;
    this.controls.maxDistance = 15000;
    this.controls.addEventListener("change", () => this.render());
    this.scene.add(new THREE.HemisphereLight(0xdcecff, 0x334335, 2.5));
    const sun = new THREE.DirectionalLight(0xffffff, 2);
    sun.position.set(200, 400, 100);
    this.scene.add(sun);
    this.roads = new THREE.Group();
    this.highlight = new THREE.Group();
    this.issue = new THREE.Group();
    this.scene.add(this.roads, this.highlight, this.issue);
    this.data = null;
    this.key = "";
    this.selected = null;
    this.frames = 0;
    this.controls.target.set(0, 0, 0);
    this.camera.position.set(420, 360, 460);
    this.controls.update();
  }
  attach(host) {
    this.observer?.disconnect();
    host.append(this.renderer.domElement, this.labels);
    this.host = host;
    this.observer = new ResizeObserver(() => this.resize());
    this.observer.observe(host);
    this.resize();
  }
  resize() {
    if (!this.host) return;
    const w = this.host.clientWidth,
      h = this.host.clientHeight;
    if (!w || !h) return;
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(w, h, false);
    this.render();
  }
  update(data) {
    const key = JSON.stringify(data);
    if (key === this.key) return;
    this.key = key;
    this.data = data;
    this.labels.replaceChildren();
    for (const road of data?.roads.filter((r) => r.kind === "mainline") ?? []) {
      const label = document.createElement("span");
      label.className = "scene-label";
      label.textContent = road.id;
      label.dataset.road = road.id;
      this.labels.append(label);
    }
    dispose(this.roads);
    dispose(this.issue);
    dispose(this.highlight);
    if (!data) {
      this.render();
      return;
    }
    const grid = new THREE.GridHelper(data.extent, 20, 0x648079, 0x263e42);
    grid.position.y = data.datum;
    this.roads.add(grid);
    for (const r of data.roads) {
      const color = r.invalid
        ? 0xef665d
        : r.kind === "mainline"
          ? 0x768b9d
          : 0xd4b37c;
      const mesh = strip(r.points, r.width, color);
      mesh.userData = { id: r.id, invalid: r.invalid };
      this.roads.add(mesh);
      for (const f of [0.25, 0.65])
        for (const sign of r.kind === "mainline" ? [1, -1] : [1]) {
          const i = Math.floor((r.points.length - 2) * f),
            p = r.points[i],
            q = r.points[i + 1],
            a = vector(p),
            b = vector(q),
            direction = b.clone().sub(a).normalize().multiplyScalar(sign);
          if (r.kind === "mainline") {
            const dx = q[0] - p[0],
              dy = q[1] - p[1],
              len = Math.hypot(dx, dy),
              offset = (r.side * sign * r.width) / 4;
            a.x -= (dy / len) * offset;
            a.z -= (dx / len) * offset;
          }
          a.y += 0.5;
          this.roads.add(
            new THREE.ArrowHelper(direction, a, 12, 0xf5f2df, 4, 3),
          );
        }
      if (r.kind === "ramp")
        for (const p of [r.points[0], r.points.at(-1)]) {
          const dot = new THREE.Mesh(
            new THREE.SphereGeometry(2.5, 8, 6),
            new THREE.MeshBasicMaterial({
              color: r.invalid ? 0xff7666 : 0xf1d3a2,
            }),
          );
          dot.position.copy(vector(p));
          this.roads.add(dot);
        }
    }
    this.select(this.selected);
    this.render();
  }
  select(od) {
    this.selected = od;
    dispose(this.highlight);
    const path = this.data?.paths[od];
    if (path?.length > 1) {
      const mesh = strip(path, 2.5, 0xffee88);
      mesh.position.y = 0.45;
      this.highlight.add(mesh);
      for (const p of [path[0], path.at(-1)]) {
        const marker = new THREE.Mesh(
          new THREE.SphereGeometry(4, 10, 8),
          new THREE.MeshBasicMaterial({ color: 0xffee88 }),
        );
        marker.position.copy(vector(p));
        this.highlight.add(marker);
      }
    }
    this.render();
  }
  focusIssue(point) {
    dispose(this.issue);
    const all = this.data?.roads.flatMap((r) => r.points) ?? [];
    if (!all.length) return;
    const nearest = all.reduce((a, b) =>
      Math.hypot(a[0] - point[0], a[1] - point[1]) <
      Math.hypot(b[0] - point[0], b[1] - point[1])
        ? a
        : b,
    );
    const target = vector([point[0], point[1], nearest[2]]),
      delta = target.clone().sub(this.controls.target);
    this.controls.target.copy(target);
    this.camera.position.add(delta);
    const marker = new THREE.Mesh(
      new THREE.SphereGeometry(7, 12, 8),
      new THREE.MeshBasicMaterial({ color: 0xff6655, wireframe: true }),
    );
    marker.position.copy(target);
    this.issue.add(marker);
    this.controls.update();
    this.render();
  }
  view(kind) {
    const center = this.data?.center ?? [0, 0, 0],
      c = vector(center),
      r = Math.max(120, (this.data?.extent ?? 700) * 0.75);
    this.controls.target.copy(c);
    if (kind === "top")
      this.camera.position.copy(c).add(new THREE.Vector3(0, r, 0.001));
    else
      this.camera.position
        .copy(c)
        .add(new THREE.Vector3(r * 0.7, r * 0.65, r * 0.8));
    this.controls.update();
    this.render();
  }
  zoom(factor) {
    this.camera.position
      .sub(this.controls.target)
      .multiplyScalar(factor)
      .add(this.controls.target);
    this.controls.update();
    this.render();
  }
  render() {
    if (!this.renderer || !this.camera) return;
    this.renderer.render(this.scene, this.camera);
    if (this.host)
      for (const label of this.labels.children) {
        const road = this.data?.roads.find((r) => r.id === label.dataset.road);
        if (!road) continue;
        const p = vector(road.points[Math.floor(road.points.length * 0.18)]);
        p.y += 5;
        p.project(this.camera);
        label.style.left = `${((p.x + 1) * this.host.clientWidth) / 2}px`;
        label.style.top = `${((1 - p.y) * this.host.clientHeight) / 2}px`;
        label.hidden =
          p.z < -1 || p.z > 1 || Math.abs(p.x) > 1 || Math.abs(p.y) > 1;
      }
    this.frames++;
    this.renderer.domElement.dataset.ready = "true";
  }
  snapshot() {
    return {
      camera: this.camera.position.toArray(),
      target: this.controls.target.toArray(),
      selected: this.selected,
      frames: this.frames,
      triangles: this.renderer.info.render.triangles,
      roads:
        this.data?.roads.map((r) => ({
          id: r.id,
          kind: r.kind,
          invalid: r.invalid,
          points: r.points.length,
        })) ?? [],
      data: this.data,
    };
  }
}
