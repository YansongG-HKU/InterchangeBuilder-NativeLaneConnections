import { evaluateCase } from "./run.js";
import { inspect, permission } from "./checker.js";
import { validateInput, convert } from "./generator.js";
import { ConnectionScene } from "../../view/scene.js";
import "../../style.css";
import "./ui.css";
const fixtures = Object.values(
  import.meta.glob("../../../tests/tail-fixtures/*.json", {
    eager: true,
    import: "default",
  }),
);
let fixture = fixtures.find((f) => f.input.name === "middle-tail"),
  input = structuredClone(fixture.input),
  lang = "zh",
  revision = 0,
  reference = false,
  selected = null,
  result = null,
  scene = null,
  error = "",
  generation = 0;
const $ = (id) => document.getElementById(id),
  tr = (a, b) => (lang === "zh" ? a : b),
  esc = (s) =>
    String(s)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll('"', "&quot;");
function recompute() {
  revision++;
  generation++;
  error = "";
  result = evaluateCase(input, {
    reference: reference ? fixture.reference : null,
  });
  selected =
    result.candidates.find((c) => c.output.id === result.selected) ?? null;
  render();
}
function render() {
  document.documentElement.lang = lang === "zh" ? "zh-CN" : "en";
  $("tail-app").innerHTML =
    `<header><h1>${tr("显式直尾曲线实验", "Explicit straight-tail experiment")}</h1><button id="language">${lang === "zh" ? "English" : "中文"}</button><a href="/">${tr("原基线", "Baseline")}</a></header><main><section class="panel"><label>${tr("合成基准", "Synthetic benchmark")}<select id="fixture">${fixtures.map((f) => `<option value="${f.input.name}" ${f.input.name === fixture.input.name ? "selected" : ""}>${f.input.name}</option>`).join("")}</select></label><label><input type="checkbox" id="reference" ${reference ? "checked" : ""}>${tr("仅检查独立手写参考（不是生成结果）", "Inspect handwritten reference only (not generated)")}</label><label>${tr("直尾长度 m", "Tail length m")}<input id="tail-length" type="number" value="${input.tail ?? ""}"></label><label>${tr("路宽 m", "Width m")}<input id="road-width" type="number" value="${input.width ?? ""}"></label><label>${tr("终端高程 m", "End elevation m")}<input id="end-height" type="number" value="${input.end.z ?? ""}"></label><label>${tr("终端坡度 %", "End grade %")}<input id="end-grade" type="number" step="any" value="${input.end.grade * 100}"></label><label>${tr("终端曲率 1/m", "End curvature 1/m")}<input id="end-curvature" type="number" step="any" value="${input.end.curvature}"></label><label>${tr("接入连续性", "Target continuity")}<select id="continuity"><option ${input.continuity === "G1" ? "selected" : ""}>G1</option><option ${input.continuity === "G2" ? "selected" : ""}>G2</option></select></label>${["start", "end"].map((k) => `<label>${tr(k === "start" ? "出发侧（按行驶方向）" : "接入侧（按行驶方向）", k + " side (travel frame)")}<select id="${k}-side">${["left", "middle", "right"].map((v) => `<option value="${v}" ${input[k].side === v ? "selected" : ""}>${tr({ left: "左 +6m", middle: "中 0m", right: "右 −6m" }[v], v)}</option>`).join("")}</select></label>`).join("")}<label>${tr("本地许可模式", "Offline permission mode")}<select id="mode"><option value="normal" ${input.mode === "normal" ? "selected" : ""}>${tr("正常", "Normal")}</option><option value="no-collision" ${input.mode === "no-collision" ? "selected" : ""}>${tr("无碰撞（仅建筑类别）", "No-collision (building only)")}</option></select></label><label><input id="building" type="checkbox" ${input.obstacles.some((o) => o.id === "test-building") ? "checked" : ""}>${tr("注入尾段建筑重叠", "Inject building overlap at tail")}</label><button id="export">${tr("导出当前实际几何", "Export actual geometry")}</button><label>${tr("导入并独立重验", "Import and independently recheck")}<input type="file" id="import" accept="application/json"></label><p>${tr("左中右表示明确参考线的侧偏，不自动识别游戏内部车道组。参数均为合成实验值，不是CS2标准。", "Sides are explicit reference offsets, not inferred game lane groups. Parameters are synthetic experiments, not CS2 standards.")}</p></section><section class="panel"><p id="state">${esc(result?.status ?? "NOT_RUN")} · ${tr("结果版本", "Result revision")} ${revision} · ${reference ? tr("独立参考检查", "REFERENCE ONLY") : tr("有限候选生成", "BOUNDED GENERATION")}</p><p id="reason">${esc(error || result?.reason || "")}</p><p id="permission">${selected ? tr("本地预览许可：", "Offline preview permission: ") + selected.permission.allowedForOfflinePreview : "NOT_RUN"} · ${tr("游戏放置／Anarchy：未验证", "Game placement / Anarchy: NOT_RUN")}</p><div class="toolbar"><button id="camera-reset">${tr("重置相机", "Reset camera")}</button><button id="camera-top">${tr("俯视", "Top")}</button><button id="show-plan">${tr("二维走廊", "Plan corridors")}</button></div><div id="three-host"></div><p>${tr("背景道路：", "Context roads: ")}${input.obstacles.map((o) => esc(o.id)).join(", ")}</p><svg id="tail-plan" viewBox="-30 -170 520 260" role="img" aria-label="plan corridors"></svg><pre id="metrics">${esc(JSON.stringify(selected?.facts.metrics ?? {}, null, 2))}</pre><div id="issues">${(selected?.facts.issues ?? []).map((i) => `<p>${esc(i.code)} · ${esc(i.road ?? "")} · ${i.severity}</p>`).join("")}</div><p id="scope">${tr("真实直尾保留为独立线段。全宽检查使用平面扫掠圆盘走廊与保守弦误差；不等于完整结构包络。中部跨越生成未实现，参考净空只支持正交平直全宽桥面。地形、桥墩、游戏和完整车道验证未运行。", "The finite tail remains a separate line. Plan checks use swept-disc corridors and conservative chord error, not complete structures. Intermediate crossing generation is unsupported; reference clearance supports orthogonal flat full-width decks only. Terrain, piers, game and full lane validation are NOT_RUN.")}</p><pre id="clearance">${esc(JSON.stringify(selected?.facts.collisions ?? [], null, 2))}</pre></section></main>`;
  if (!scene) {
    try {
      scene = new ConnectionScene();
    } catch {
      error = "WebGL unavailable; plan remains available";
    }
  }
  const points = selected?.facts.points ?? [],
    obstacles = input.obstacles.map((o) => ({
      id: o.id,
      kind: "mainline",
      side: 1,
      width: o.width,
      points: [
        [...o.a, o.z ?? 0],
        [...o.b, o.z ?? 0],
      ],
    }));
  const all = [...points, ...obstacles.flatMap((o) => o.points)],
    xs = all.map((p) => p[0]),
    ys = all.map((p) => p[1]);
  const bounds = all.length
    ? [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)]
    : [0, 0, 200, 100];
  if (scene) {
    scene.attach($("three-host"));
    scene.update(
      points.length
        ? {
            extent:
              Math.max(120, bounds[2] - bounds[0], bounds[3] - bounds[1]) *
              1.15,
            center: [
              (bounds[0] + bounds[2]) / 2,
              (bounds[1] + bounds[3]) / 2,
              0,
            ],
            datum: -1,
            roads: [
              ...obstacles,
              {
                id: "ramp",
                kind: "ramp",
                width: input.width,
                invalid: selected.facts.status === "CANDIDATE_FAILED",
                points,
              },
            ],
            paths: { route: points },
          }
        : null,
    );
    scene.select("route");
  }
  $("tail-plan").innerHTML =
    obstacles
      .map(
        (o) =>
          `<polyline points="${o.points.map((p) => `${p[0]},${-p[1]}`).join(" ")}" fill="none" stroke="#778a9b" stroke-width="${o.width}" stroke-linecap="round"/>`,
      )
      .join("") +
    (points.length
      ? `<polyline points="${points.map((p) => `${p[0]},${-p[1]}`).join(" ")}" fill="none" stroke="${selected.facts.status === "CANDIDATE_FAILED" ? "#ef665d" : "#e7be68"}" stroke-width="${input.width}" stroke-linecap="round" stroke-linejoin="round"/>`
      : "");
  $("language").onclick = () => {
    lang = lang === "zh" ? "en" : "zh";
    generation++;
    render();
  };
  $("fixture").onchange = (e) => {
    fixture = fixtures.find((f) => f.input.name === e.target.value);
    input = structuredClone(fixture.input);
    reference = false;
    recompute();
  };
  $("reference").onchange = (e) => {
    reference = e.target.checked;
    recompute();
  };
  for (const [id, key] of [
    ["tail-length", "tail"],
    ["road-width", "width"],
  ])
    $(id).onchange = (e) => {
      input[key] = e.target.value === "" ? null : Number(e.target.value);
      recompute();
    };
  for (const [id, key, mult] of [
    ["end-height", "z", 1],
    ["end-grade", "grade", 0.01],
    ["end-curvature", "curvature", 1],
  ])
    $(id).onchange = (e) => {
      input.end[key] =
        e.target.value === "" ? null : Number(e.target.value) * mult;
      recompute();
    };
  for (const k of ["start", "end"])
    $(k + "-side").onchange = (e) => {
      input[k].side = e.target.value;
      input[k].offset = { left: 6, middle: 0, right: -6 }[e.target.value];
      recompute();
    };
  $("continuity").onchange = (e) => {
    input.continuity = e.target.value;
    recompute();
  };
  $("mode").onchange = (e) => {
    input.mode = e.target.value;
    recompute();
  };
  $("building").onchange = (e) => {
    input.obstacles = input.obstacles.filter((o) => o.id !== "test-building");
    if (e.target.checked) {
      const p = input.end.position,
        d = input.end.direction,
        q = [p[0] - (d[0] * input.tail) / 2, p[1] - (d[1] * input.tail) / 2];
      input.obstacles.push({
        id: "test-building",
        kind: "building",
        a: [q[0] - d[1] * 20, q[1] + d[0] * 20],
        b: [q[0] + d[1] * 20, q[1] - d[0] * 20],
        width: 10,
      });
    }
    recompute();
  };
  $("camera-reset").onclick = () => scene?.view("reset");
  $("camera-top").onclick = () => scene?.view("top");
  $("show-plan").onclick = () =>
    $("tail-plan").scrollIntoView({ block: "center" });
  $("export").disabled = !selected;
  $("export").onclick = () => {
    const blob = new Blob(
        [
          JSON.stringify(
            {
              schema: "tail-preview-v1",
              revision,
              input,
              output: selected.output,
            },
            null,
            2,
          ),
        ],
        { type: "application/json" },
      ),
      url = URL.createObjectURL(blob),
      a = document.createElement("a");
    a.href = url;
    a.download = "tail-preview.json";
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  };
  $("import").onchange = async (e) => {
    const token = ++generation;
    try {
      const data = JSON.parse(await e.target.files[0].text());
      if (token !== generation) return;
      if (data.schema !== "tail-preview-v1" || validateInput(data.input))
        throw Error("INPUT_INVALID");
      const facts = inspect(data.input, convert(data.output));
      input = data.input;
      reference = true;
      fixture = { input, reference: data.output };
      revision++;
      selected = {
        output: data.output,
        facts,
        permission: permission(facts, input.mode),
      };
      result = { status: "IMPORTED_RECHECK", candidates: [selected] };
      error = "";
      render();
    } catch (err) {
      if (token !== generation) return;
      error = "IMPORT_REJECTED: " + err.message;
      render();
    }
  };
  document.querySelectorAll("input[type=number]").forEach(
    (e) =>
      (e.oninput = () => {
        generation++;
        $("export").disabled = true;
        $("permission").textContent = tr(
          "输入已变更，许可过期",
          "Input changed; permission stale",
        );
      }),
  );
  window.tailSnapshot = () =>
    structuredClone({ input, result, selected, revision, reference, error });
  window.tailScene = () => scene?.snapshot();
}
recompute();
