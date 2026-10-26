import { ConnectionScene } from "./view/scene.js";
import { profileMarkup } from "./view/profile.js";
import { legacyProposal } from "./core/migration.js";
import "./style.css";
import { defaultCase, clone, FULL, VERSION } from "./core/model.js";
import { solve, validate } from "./core/solver.js";
import { messages } from "./i18n.js";
let model = defaultCase(),
  lang = "zh",
  result = null,
  selected = "d1",
  history = [],
  focus = null,
  serial = 0,
  inputRevision = 0,
  selectedLane = "L1",
  selectedOD = "A+>B+",
  viewMode = "three",
  pendingLegacy = null;
let scene = null,
  sceneFailed = false;
const t = (k) => messages[k]?.[lang === "zh" ? 0 : 1] ?? k;
const esc = (s) =>
  String(s).replace(
    /[&<>"']/g,
    (c) =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[
        c
      ],
  );
const fmt = (n) => (Number.isFinite(n) ? n.toFixed(2) : "UNKNOWN");
const $ = (id) => document.getElementById(id);
const points = (curve) =>
  curve.nodes.map((n) => [n.p[0], -n.p[1]].join(",")).join(" ");
const polygonPath = (multi) =>
  multi
    .map((p) =>
      p
        .map((r) => "M" + r.map((v) => [v[0], -v[1]].join(",")).join("L") + "Z")
        .join(""),
    )
    .join("");
function remember() {
  history.push(clone(model));
  if (history.length > 30) history.shift();
}
function viewBox(vertices, padding) {
  if (!vertices.length) return "-200 -200 400 400";
  const xs = vertices.map((p) => p[0]),
    ys = vertices.map((p) => -p[1]);
  const x = Math.min(...xs) - padding,
    y = Math.min(...ys) - padding;
  return `${x} ${y} ${Math.max(...xs) - x + padding} ${Math.max(...ys) - y + padding}`;
}
function mountScene(c) {
  const host = $("three-host");
  if (!scene && !sceneFailed) {
    try {
      scene = new ConnectionScene();
    } catch {
      sceneFailed = true;
    }
  }
  if (scene) {
    scene.attach(host);
    scene.update(c?.preview ?? result?.preview ?? null);
    scene.select(selectedOD);
    scene.resize();
  }
  if (sceneFailed) {
    host.textContent = t("webglUnavailable");
    viewMode = "plan";
    $("plan-panel").hidden = false;
  }
}
function render() {
  document.documentElement.lang = lang === "zh" ? "zh-CN" : "en";
  const c =
    result?.candidates.find((c) => c.id === selected) ?? result?.candidates[0];
  document.querySelector("#app").innerHTML =
    `<header><div><h1>${t("title")}</h1><small>${VERSION}</small></div>
<button id="lang">${lang === "zh" ? "English" : "中文"}</button></header><main><section><div class="panel"><h2>${t("inputs")}</h2>
<p>${t("intro")}</p><label>${t("example")}<select id="example"><option value="">—</option>${["elevated-full", "curved-skew", "partial", "multiple", "near-tangent", "small-site", "exclusion", "invalid"].map((n) => `<option>${n}</option>`).join("")}</select></label>${["anchor", "handle", "areaCap", "heightCap", "layerCap"].map((k) => `<label>${t(k)}<input id="${k}" type="number" step="any" value="${model[k]}"></label>`).join("")}<details open><summary>${t("verticalInputs")}</summary>${model.vertical.profiles.map((p, i) => `<fieldset><legend>${i ? "B" : "A"} · ${t("datum")}</legend>${["z0", "z1", "g0", "g1"].map((k) => `<label>${t(k)}<input type="number" step="any" data-profile="${i}" data-field="${k}" value="${p[k] === null ? "" : p[k] * (k[0] === "g" ? 100 : 1)}"></label>`).join("")}</fieldset>`).join("")}<label>${t("maxGrade")}<input id="max-grade" type="number" step="any" value="${model.vertical.maxGrade === null ? "" : model.vertical.maxGrade * 100}"></label><p>${t("terrainUnknown")}</p></details><label>${t("preference")}<select id="preference">${["urban", "suburban", "custom"].map((k) => `<option value="${k}" ${model.preference === k ? "selected" : ""}>${t(k)}</option>`).join("")}</select></label>
<label>${t("trafficSide")}<select id="trafficSide">${["RHT", "LHT"].map((k) => `<option ${model.trafficSide === k ? "selected" : ""}>${t(k)}</option>`).join("")}</select></label>
<label>${t("crossing")}<select id="crossing"><option value="">${t("choose")}</option>${(result?.crossings ?? []).map((h, i) => `<option value="${i}" ${model.crossing === i ? "selected" : ""}>${i + 1} · s=${fmt(h.s[0])}/${fmt(h.s[1])} m</option>`).join("")}</select></label>
<div class="row"><button id="recompute">${t("solve")}</button><button id="undo">${t("undo")}</button><button id="reset">${t("reset")}</button></div>
</div><div class="panel"><button id="export">${t("export")}</button><label><button id="importButton">${t("import")}</button><input hidden id="import" type="file" accept="application/json"></label>
<small>${t("saveHelp")}</small>${pendingLegacy ? `<p class="warn">${t("migrationWarning")}</p><button id="accept-migration">${t("acceptMigration")}</button><button id="cancel-migration">${t("cancelMigration")}</button>` : ""}</div>
</section>
<section><div class="warn">${t("warning")}</div>
<div id="status" role="status" aria-live="polite">${t("odState")}: ${result ? t(result.status) : t("running")}</div>
<h2>${t("preview")}</h2>
<div class="row"><button id="view-three">${t("viewThree")}</button><button id="view-plan">${t("viewPlan")}</button><button id="view-profile">${t("viewProfile")}</button><button id="camera-reset">${t("cameraReset")}</button><button id="camera-top">${t("cameraTop")}</button><button id="camera-in">＋</button><button id="camera-out">−</button></div>
<label>${t("selectOD")}<select id="selected-od">${FULL.map((od) => `<option value="${od}" ${selectedOD === od ? "selected" : ""}>${od}${model.required?.includes(od) ? "" : " · " + t("excluded")}</option>`).join("")}</select></label>
<p id="path-state">${!model.required?.includes(selectedOD) ? t("excluded") : c?.preview?.paths[selectedOD] ? t("pathPreview") : t("NOT_RUN")}</p>
<div id="three-panel" ${viewMode === "three" ? "" : "hidden"}><div id="three-host"></div><p>${t("cameraHelp")}</p><p>${t("sceneLegend")}</p></div>
<div id="profile-panel" ${viewMode === "profile" ? "" : "hidden"}>${profileMarkup(c?.preview?.profiles[selectedOD], { notRun: t("NOT_RUN"), title: t("viewProfile"), peak: t("peakGrade"), scale: t("profileScale") })}</div>
<div id="plan-panel" ${viewMode === "plan" ? "" : "hidden"}>
<svg id="plan" viewBox="${viewBox([...(model.site ?? []), ...(model.curves ?? []).flat()], 100)}" role="img" aria-label="${t("preview")}">${draw(c)}</svg><small>${t("legend")}</small></div><p>${t("heightDifference")}: ${c?.preview?.crossingDiagnostics.map((h) => h.heightDifference.toFixed(2) + " m").join(", ") ?? "UNKNOWN"}</p><h2>${t("split")}</h2>
<p>${t("splitHelp")}</p><p id="split-status">${t("splitState")}: ${c ? t(c.splitStatus) : "—"}</p><p>${t("laneHelp")}</p><div class="lane-legend">${(c?.lanePaths ?? []).map((r) => `<button data-lane="${r.from}" aria-pressed="${selectedLane === r.from}">${r.from} → ${r.to}</button>`).join("")}</div><svg id="split" viewBox="${viewBox(
      (c?.lanePaths ?? []).flatMap((r) => r.curve.nodes.map((n) => n.p)),
      25,
    )}" role="img" aria-label="${t("split")}">${(c?.lanePaths ?? []).map((r, i) => `<polyline data-lane-path="${r.from}" points="${points(r.curve)}" fill="none" stroke="${selectedLane === r.from ? "#ffffff" : i < 4 ? "#8ed9ff" : "#ffd176"}" stroke-width="${selectedLane === r.from ? 3 : 1.5}"/><title>${r.from} → ${r.to}</title>`).join("")}</svg><div class="panel"><h2>${t("od")}</h2>
<button id="full">${t("full")}</button><p id="mode">${result?.mode ?? ""} · ${result?.mode === "partial" ? t("partial") : ""}</p>${FULL.map((m) => `<label class="od"><input data-od="${m}" type="checkbox" ${model.required?.includes(m) ? "checked" : ""} ${m.split(">")[0] === m.split(">")[1] ? "disabled" : ""}>${m} ${model.required?.includes(m) ? "" : "(" + t("excluded") + ")"}</label>`).join("")}<p>${t("reachable")}: ${c?.od?.reachable.length ?? 0} / ${model.required?.length ?? 0}</p></div>
<details><summary>${t("advanced")}</summary><textarea id="json" aria-label="JSON">${esc(JSON.stringify(model, null, 2))}</textarea><button id="applyJSON">${t("applyJSON")}</button></details></section>
<aside><div class="panel"><h2>${t("candidates")}</h2>${(result?.candidates ?? []).map((v) => `<button data-candidate="${v.id}" class="diag ${v.id === c?.id ? "selected" : ""}">${v.id} · ${t(v.status)}<br>${fmt(v.land?.total)} m² · ${fmt(v.distance)} m</button>`).join("")}<small>${t(model.preference)}</small></div>
<div class="panel metric"><h2>${t("metrics")}</h2>${["total", "overflow", "ratio", "added", "removed", "retained", "net"].map((k) => `<p>${t(k)}: <strong>${fmt(c?.land?.[k])}</strong> ${k === "ratio" ? "" : "m²"}</p>`).join("")}<p>${t("projection")}: ${fmt(c?.projectionArea)} m²</p><p>${t("bound")}: ${c?.positionBound?.toFixed(6) ?? "—"}</p><p class="warn">${t("unknown")}</p><small>${t("approx")}</small></div>
<div class="panel"><h2>${t("diagnostics")}</h2>
<div id="diagnostics">${[...(result?.diagnostics ?? []), ...(c?.diagnostics ?? []), ...(c?.splitDiagnostics ?? [])].map((d, i) => `<button data-diag="${i}" class="diag error">⚠ ${t(d.code)}<br><code>${esc(d.code)} · ${esc(d.path)}</code>${d.measured !== undefined ? `<br>${fmt(d.measured)} / ${fmt(d.limit)} ${d.unit ?? "m²"}` : ""}</button>`).join("") || t("none")}</div>
<button disabled>${t("game")}</button></div>
<table><thead><tr><th>${t("from")}</th><th>${t("to")}</th></tr></thead><tbody>${(result?.lanes ?? []).map((l) => `<tr><td>${l.from}</td><td>${l.to}</td></tr>`).join("")}</tbody></table></aside></main><footer>${t("warning")}</footer>`;
  bind(c);
  mountScene(c);
}
function draw(c) {
  if (!result) return "";
  return `<path d="${polygonPath([[model.site ?? []]])}" fill="#233c38" stroke="#85ae8c" stroke-width="2"/>${(model.exclusions ?? []).map((r) => `<path d="${polygonPath([[r]])}" fill="#583934" stroke="#ffb39e"/>`).join("")}${c?.reserved ? `<path d="${polygonPath(c.reserved)}" fill="#41666b" fill-opacity=".4" stroke="#96b3bd" stroke-dasharray="4 4" fill-rule="evenodd"/>` : ""}${(result.curves ?? []).map((r, i) => `<polyline points="${points(r)}" fill="none" stroke="#778a9b" stroke-width="${model.roadWidth}"/><polyline points="${points(r)}" fill="none" stroke="white" stroke-width="1" stroke-dasharray="5 8"/><text x="${r.nodes[15].p[0]}" y="${-r.nodes[15].p[1]}" fill="white">${i ? "B" : "A"} →</text>`).join("")}${(c?.routes ?? []).map((r) => `<polyline points="${points(r.curve)}" fill="none" stroke="#65d4bf" stroke-width="${r.width}" stroke-opacity=".7"/><circle cx="${r.a.p[0]}" cy="${-r.a.p[1]}" r="3" fill="#fff"/>`).join("")}${(result.crossings ?? []).map((h, i) => `<g class="hit" data-hit="${i}" tabindex="0" role="button" aria-label="${t("crossing")} ${i + 1}"><circle cx="${h.p[0]}" cy="${-h.p[1]}" r="12" fill="${model.crossing === i ? "#edbe69" : "#314f67"}" stroke="white"/><text x="${h.p[0]}" y="${-h.p[1] + 5}" text-anchor="middle" fill="#fff">${i + 1}</text></g>`).join("")}${focus ? `<circle cx="${focus[0]}" cy="${-focus[1]}" r="22" fill="none" stroke="#ff986c" stroke-width="4"/>` : ""}`;
}
function bind(c) {
  for (const [id, mode] of [
    ["view-three", "three"],
    ["view-plan", "plan"],
    ["view-profile", "profile"],
  ])
    $(id).onclick = () => {
      inputRevision++;
      viewMode = mode;
      render();
    };
  $("selected-od").onchange = (e) => {
    inputRevision++;
    selectedOD = e.target.value;
    render();
  };
  $("camera-reset").onclick = () => scene?.view("reset");
  $("camera-top").onclick = () => scene?.view("top");
  $("camera-in").onclick = () => scene?.zoom(0.8);
  $("camera-out").onclick = () => scene?.zoom(1.25);
  document.querySelectorAll("[data-profile]").forEach(
    (e) =>
      (e.onchange = () => {
        remember();
        model.vertical.profiles[Number(e.dataset.profile)][e.dataset.field] =
          e.value === ""
            ? null
            : Number(e.value) / (e.dataset.field[0] === "g" ? 100 : 1);
        recompute();
      }),
  );
  $("max-grade").onchange = (e) => {
    remember();
    model.vertical.maxGrade =
      e.target.value === "" ? null : Number(e.target.value) / 100;
    recompute();
  };
  if (pendingLegacy) {
    $("accept-migration").onclick = () => {
      remember();
      model = pendingLegacy;
      pendingLegacy = null;
      recompute();
    };
    $("cancel-migration").onclick = () => {
      inputRevision++;
      pendingLegacy = null;
      render();
    };
  }

  document.querySelectorAll("[data-lane]").forEach(
    (e) =>
      (e.onclick = () => {
        inputRevision++;
        selectedLane = e.dataset.lane;
        render();
      }),
  );
  document.querySelectorAll("input[type=number], #json").forEach(
    (e) =>
      (e.oninput = () => {
        inputRevision++;
      }),
  );
  $("importButton").onclick = () => $("import").click();
  $("lang").onclick = () => {
    inputRevision++;
    lang = lang === "zh" ? "en" : "zh";
    render();
  };
  for (const key of ["anchor", "handle", "areaCap", "heightCap", "layerCap"])
    $(key).onchange = (e) => {
      remember();
      model[key] = e.target.value === "" ? null : Number(e.target.value);
      recompute();
    };
  for (const key of ["preference", "trafficSide"])
    $(key).onchange = (e) => {
      remember();
      model[key] =
        key === "trafficSide"
          ? ["RHT", "LHT"][e.target.selectedIndex]
          : e.target.value;
      recompute();
    };
  $("crossing").onchange = (e) =>
    choose(e.target.value === "" ? null : Number(e.target.value));
  document.querySelectorAll("[data-hit]").forEach((e) => {
    e.onclick = () => choose(Number(e.dataset.hit));
    e.onkeydown = (k) => {
      if (k.key === "Enter") e.click();
    };
  });
  document.querySelectorAll("[data-candidate]").forEach(
    (e) =>
      (e.onclick = () => {
        selected = e.dataset.candidate;
        focus = null;
        render();
      }),
  );
  const diags = [
    ...(result?.diagnostics ?? []),
    ...(c?.diagnostics ?? []),
    ...(c?.splitDiagnostics ?? []),
  ];
  document.querySelectorAll("[data-diag]").forEach(
    (e) =>
      (e.onclick = () => {
        focus = diags[Number(e.dataset.diag)].point ?? null;
        render();
        if (viewMode === "three") {
          if (focus) scene?.focusIssue(focus);
          $("three-panel").scrollIntoView({ block: "center" });
        } else if (viewMode === "plan") {
          $("plan").scrollIntoView({ block: "center" });
        } else {
          $("profile-panel").scrollIntoView({ block: "center" });
        }
      }),
  );
  document.querySelectorAll("[data-od]").forEach(
    (e) =>
      (e.onchange = () => {
        remember();
        model.required = e.checked
          ? [...model.required, e.dataset.od]
          : model.required.filter((m) => m !== e.dataset.od);
        recompute();
      }),
  );
  $("full").onclick = () => {
    remember();
    model.required = [...FULL];
    recompute();
  };
  $("recompute").onclick = recompute;
  $("reset").onclick = () => {
    remember();
    model = defaultCase();
    recompute();
  };
  $("undo").onclick = () => {
    inputRevision++;
    if (history.length) {
      model = history.pop();
      recompute();
    }
  };
  $("applyJSON").onclick = () => load($("json").value);
  $("import").onchange = async (e) => {
    inputRevision++;
    const f = e.target.files[0];
    if (f) {
      if (f.size > 100000) {
        $("status").textContent = t("INPUT_INVALID");
        return;
      }
      await readInput(() => f.text());
    }
  };
  $("export").onclick = () => {
    const blob = new Blob([JSON.stringify(model, null, 2)], {
        type: "application/json",
      }),
      url = URL.createObjectURL(blob),
      a = document.createElement("a");
    a.href = url;
    a.download = "interchange-case.json";
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  };
  $("example").onchange = async (e) => {
    inputRevision++;
    const name = e.target.value;
    if (name)
      await readInput(async () => {
        const response = await fetch(`/examples/${name}.json`);
        if (!response.ok) throw Error("load");
        return response.text();
      });
  };
}
function choose(i) {
  remember();
  model.crossing = i;
  recompute();
}
async function readInput(read) {
  const revision = ++inputRevision;
  try {
    const raw = await read();
    if (revision === inputRevision) load(raw);
  } catch {
    if (revision === inputRevision) $("status").textContent = t("loadFailed");
  }
}
function load(raw) {
  inputRevision++;
  try {
    const candidate = JSON.parse(raw);
    const proposal = legacyProposal(candidate);
    if (proposal) {
      pendingLegacy = proposal;
      render();
      return;
    }
    pendingLegacy = null;
    const errors = validate(candidate);
    if (errors.length) {
      $("status").textContent = errors
        .map((d) => t(d.code) + " · " + d.path)
        .join("; ");
      return;
    }
    remember();
    model = candidate;
    recompute();
  } catch {
    $("status").textContent = t("invalidJSON");
  }
}
async function recompute() {
  inputRevision++;
  pendingLegacy = null;
  const token = ++serial;
  result = null;
  focus = null;
  render();
  await new Promise((r) => setTimeout(r, 20));
  try {
    const computed = solve(model);
    if (token !== serial) return;
    result = computed;
    selected = computed.recommended ?? "d1";
    render();
  } catch (e) {
    $("status").textContent = t("INPUT_INVALID");
    console.error(e);
  }
}
// Read-only test probe: serialized value, not mutable internal model.
window.labSnapshot = () => JSON.stringify({ model, result });
window.sceneSnapshot = () => scene?.snapshot() ?? { unavailable: true };
recompute();
