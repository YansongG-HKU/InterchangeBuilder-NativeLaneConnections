import { chromium } from "playwright";
import assert from "node:assert/strict";
import fs from "node:fs";
import { execFileSync } from "node:child_process";
const browser = await chromium.launch({
  executablePath: "/usr/bin/chromium",
  headless: true,
  args: ["--no-sandbox"],
});
const page = await browser.newPage({ viewport: { width: 1500, height: 1100 } });
const errors = [];
page.on("pageerror", (e) => errors.push(e.message));
const checks = [];
const check = (name, expected, actual) => {
  assert.deepEqual(actual, expected, name);
  checks.push({ name, expected, actual });
};
const snapshot = async () =>
  JSON.parse(await page.evaluate(() => window.labSnapshot()));
const ready = async () => {
  await page.waitForFunction(
    () =>
      window.labSnapshot && JSON.parse(window.labSnapshot()).result !== null,
  );
};
async function checkLaneLabels(context) {
  const before = await snapshot();
  check(
    `${context}: all six labels`,
    ["L1 → M1", "L2 → M2", "L3 → M3", "L4 → M4", "L5 → R1", "L6 → R2"],
    await page.locator("[data-lane]").allTextContents(),
  );
  const layout = await page.locator("[data-lane]").evaluateAll((nodes) =>
    nodes.map((n) => {
      const r = n.getBoundingClientRect();
      return {
        x: r.x,
        y: r.y,
        w: r.width,
        h: r.height,
        font: parseFloat(getComputedStyle(n).fontSize),
        fits: n.scrollWidth <= n.clientWidth,
      };
    }),
  );
  check(
    `${context}: labels readable and nonoverlapping`,
    true,
    layout.every(
      (a, i) =>
        a.font >= 14 &&
        a.fits &&
        layout.every(
          (b, j) =>
            i === j ||
            a.x + a.w <= b.x ||
            b.x + b.w <= a.x ||
            a.y + a.h <= b.y ||
            b.y + b.h <= a.y,
        ),
    ),
  );
  for (const lane of ["L1", "L2", "L3", "L4", "L5", "L6"]) {
    await page.locator(`[data-lane="${lane}"]`).click();
    check(
      `${context}: ${lane} selects its connector`,
      lane,
      await page
        .locator('#split polyline[stroke="#ffffff"]')
        .getAttribute("data-lane-path"),
    );
  }
  check(
    `${context}: selection does not change computations`,
    before,
    await snapshot(),
  );
}
async function importOwnFields(context) {
  const before = await snapshot();
  for (const key of ["constructor", "toString", "__proto__"]) {
    const raw = JSON.stringify(before.model).replace(
      "{",
      `{${JSON.stringify(key)}:{},`,
    );
    await page.locator("#import").setInputFiles({
      name: `${key}.json`,
      mimeType: "application/json",
      buffer: Buffer.from(raw),
    });
    await page.waitForFunction(
      (key) => document.querySelector("#status").textContent.includes(key),
      key,
    );
    check(
      `${context}: rejects imported ${key}`,
      true,
      (await page.locator("#status").textContent()).includes(key),
    );
    check(
      `${context}: ${key} preserves previous input/result`,
      before,
      await snapshot(),
    );
  }
}
try {
  await page.goto("http://127.0.0.1:4173");
  await ready();
  await page.locator("#view-plan").click();
  let a = await snapshot();
  check(
    "default nonorthogonal full baseline",
    12,
    a.result.fullBaseline.length,
  );
  check(
    "computed SVG",
    true,
    (await page.locator("#plan polyline").count()) > 5,
  );
  await checkLaneLabels("desktop zh");
  await importOwnFields("zh");
  await page.locator("#recompute").click();
  await ready();
  await page.screenshot({ path: "evidence/browser-zh.png", fullPage: true });
  await page.locator("#lang").click();
  let b = await snapshot();
  check("language leaves geometry/model byte-identical", a, b);
  check(
    "English title",
    true,
    (await page.locator("h1").textContent()).includes("Offline"),
  );
  await checkLaneLabels("desktop en");
  await importOwnFields("en");
  await page.locator("#example").selectOption("partial");
  await page.waitForFunction(
    (name) => JSON.parse(window.labSnapshot()).model.name === name,
    "partial",
  );
  await ready();
  a = await snapshot();
  check("partial status", "UNKNOWN", a.result.status);
  check("partial mode", "partial", a.result.mode);
  await page.locator("#anchor").fill("92.5");
  await page.locator("#anchor").press("Tab");
  await ready();
  a = await snapshot();
  check("fractional numeric parameter", 92.5, a.model.anchor);
  await page.locator("#undo").click();
  await ready();
  check("undo restores anchor", 80, (await snapshot()).model.anchor);
  await page.locator("#preference").selectOption("suburban");
  await ready();
  check(
    "suburban selected longer anchor",
    "d1.2",
    (await snapshot()).result.recommended,
  );
  const before = await snapshot();
  const downloadPromise = page.waitForEvent("download");
  await page.locator("#export").click();
  const download = await downloadPromise;
  await download.saveAs("evidence/exported-case.json");
  await page.locator("#reset").click();
  await ready();
  await page.locator("#import").setInputFiles("evidence/exported-case.json");
  await page.waitForFunction(
    () => JSON.parse(window.labSnapshot()).model.name === "partial",
  );
  await ready();
  check(
    "export import restores input and exact result",
    before,
    await snapshot(),
  );
  await page.locator("#areaCap").fill("1");
  await page.locator("#areaCap").press("Tab");
  await ready();
  check(
    "tiny area fails",
    "NO_CANDIDATE_FOUND",
    (await snapshot()).result.status,
  );
  check(
    "error targets cap",
    true,
    (await page.locator("#diagnostics").textContent()).includes("areaCap"),
  );
  await page.locator("[data-diag]").first().click();
  check(
    "diagnostic map marker",
    1,
    await page.locator('#plan circle[stroke="#ff986c"]').count(),
  );
  await page.locator("#example").selectOption("multiple");
  await page.waitForFunction(
    (name) => JSON.parse(window.labSnapshot()).model.name === name,
    "multiple",
  );
  await ready();
  check(
    "three selectable intersections",
    3,
    (await snapshot()).result.crossings.length,
  );
  await page.locator('[data-hit="1"]').click();
  await ready();
  check(
    "second intersection explicit selection",
    1,
    (await snapshot()).model.crossing,
  );
  await page.locator("#example").selectOption("near-tangent");
  await page.waitForFunction(
    (name) => JSON.parse(window.labSnapshot()).model.name === name,
    "near-tangent",
  );
  await ready();
  check(
    "near tangent rejection",
    "UNSUPPORTED",
    (await snapshot()).result.status,
  );
  await page.locator("#example").selectOption("small-site");
  await page.waitForFunction(
    (name) => JSON.parse(window.labSnapshot()).model.name === name,
    "small-site",
  );
  await ready();
  check(
    "overflow diagnostic",
    true,
    (await page.locator("#diagnostics").textContent()).includes("OVERFLOW"),
  );
  await page.screenshot({
    path: "evidence/browser-overflow-en.png",
    fullPage: true,
  });
  await page.locator("details:has(#json) summary").click();
  await page.locator("#json").fill("{oops");
  await page.locator("#applyJSON").click();
  check(
    "malformed JSON actionable message",
    true,
    (await page.locator("#status").textContent()).includes("JSON parse failed"),
  );
  await page.locator("#json").fill("null");
  await page.locator("#applyJSON").click();
  check(
    "wrong JSON shape rejected without crash",
    true,
    (await page.locator("#status").textContent()).includes("Invalid input"),
  );
  await page.locator("#reset").click();
  await ready();
  await page.locator("#anchor").fill("");
  await page.locator("#anchor").press("Tab");
  await ready();
  check(
    "empty numeric input is not coerced to zero",
    "INPUT_INVALID",
    (await snapshot()).result.status,
  );
  await page.locator("#undo").click();
  await ready();
  check("recover from invalid input", 80, (await snapshot()).model.anchor);
  // Hold real fetch responses and deliver older requests last.
  const pending = new Map();
  await page.route("**/examples/*.json", (route) => {
    pending.set(
      route.request().url().split("/").at(-1).replace(".json", ""),
      route,
    );
  });
  async function begin(name) {
    await page.locator("#example").selectOption(name);
    while (!pending.has(name)) await page.waitForTimeout(10);
  }
  async function release(name) {
    await pending.get(name).fulfill({
      contentType: "application/json",
      body: fs.readFileSync(`examples/${name}.json`, "utf8"),
    });
    pending.delete(name);
    await page.waitForTimeout(100);
  }
  await begin("multiple");
  await begin("partial");
  await release("partial");
  await ready();
  const newest = await snapshot();
  await release("multiple");
  check(
    "inverse fetch order preserves later selection",
    newest,
    await snapshot(),
  );
  for (const action of ["reset", "edit", "language"]) {
    await begin("multiple");
    if (action === "reset") {
      await page.locator("#reset").click();
      await ready();
    }
    if (action === "edit") {
      await page.locator("#anchor").fill("91");
      await page.locator("#anchor").press("Tab");
      await ready();
    }
    if (action === "language") await page.locator("#lang").click();
    const current = await snapshot();
    await release("multiple");
    check(`late fetch cannot overwrite ${action}`, current, await snapshot());
  }
  // Hold File.text() after the real file-input interaction; reset before completion.
  await page.evaluate(() => {
    window.originalFileText = File.prototype.text;
    File.prototype.text = function () {
      return new Promise((resolve) => {
        window.releaseImport = async () =>
          resolve(await window.originalFileText.call(this));
      });
    };
  });
  await page.locator("#import").setInputFiles("examples/partial.json");
  await page.waitForFunction(() => typeof window.releaseImport === "function");
  await page.locator("#reset").click();
  await ready();
  const reset = await snapshot();
  await page.evaluate(() => window.releaseImport());
  await page.waitForTimeout(100);
  check("late file read cannot overwrite reset", reset, await snapshot());
  await page.evaluate(() => {
    File.prototype.text = window.originalFileText;
  });
  await page.unroute("**/examples/*.json");
  // Explicitly separate OD and local-fork states on a supported conflicting fork.
  const conflict = (await snapshot()).model;
  conflict.anchor = 10;
  conflict.handle = 100;
  conflict.required = conflict.required.filter(
    (m) => m.split(">")[0] === m.split(">")[1],
  );
  await page.locator("details:has(#json) summary").click();
  await page.locator("#json").fill(JSON.stringify(conflict));
  await page.locator("#applyJSON").click();
  await ready();
  check(
    "OD status remains independent",
    "UNKNOWN",
    (await snapshot()).result.status,
  );
  check(
    "lane state reports its own violation",
    true,
    (await page.locator("#split-status").textContent()).includes("违反"),
  );
  await page.locator("#reset").click();
  await ready();
  await page.locator("#lang").click();
  await page.locator("#anchor").fill("300");
  await page.locator("#anchor").press("Tab");
  await ready();
  await page.locator('[data-candidate="d1"]').click();
  check(
    "out-of-range lane experiment English NOT_RUN",
    true,
    (await page.locator("#split-status").textContent()).includes("NOT_RUN"),
  );
  check(
    "unrun lane experiment has no connectors",
    0,
    await page.locator("#split polyline").count(),
  );
  await page.locator("#lang").click();
  check(
    "out-of-range lane experiment Chinese NOT_RUN",
    true,
    (await page.locator("#split-status").textContent()).includes("未运行"),
  );
  await page.locator("#reset").click();
  await ready();
  await page.locator("#lang").click();
  await page.setViewportSize({ width: 390, height: 844 });
  await checkLaneLabels("mobile en");
  await page.screenshot({
    path: "evidence/browser-mobile-en.png",
    fullPage: true,
  });
  check(
    "mobile no horizontal overflow",
    true,
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  );
  await page.locator("#lang").click();
  await checkLaneLabels("mobile zh");
  await page.screenshot({
    path: "evidence/browser-mobile-zh.png",
    fullPage: true,
  });
  check("browser console uncaught errors", [], errors);
  fs.writeFileSync(
    "evidence/browser.json",
    JSON.stringify(
      {
        date: new Date().toISOString(),
        sourceCommit: execFileSync("git", ["rev-parse", "HEAD"], {
          encoding: "utf8",
        }).trim(),
        workingTree: "implementation under test; final source hash in manifest",
        browser: await browser.version(),
        checks: checks.map((c) => ({
          ...c,
          expected:
            typeof c.expected === "object"
              ? "deep equality asserted"
              : c.expected,
          actual:
            typeof c.actual === "object" ? "deep equality passed" : c.actual,
        })),
        screenshots: [
          "browser-zh.png",
          "browser-overflow-en.png",
          "browser-mobile-en.png",
          "browser-mobile-zh.png",
        ],
      },
      null,
      2,
    ) + "\n",
  );
  console.log(`${checks.length} browser assertions passed`);
} finally {
  await browser.close();
}
