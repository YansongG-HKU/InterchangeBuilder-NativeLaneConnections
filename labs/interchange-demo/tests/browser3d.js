import { chromium } from "playwright";
import assert from "node:assert/strict";
import fs from "node:fs";
import crypto from "node:crypto";
import { execFileSync } from "node:child_process";
const browser = await chromium.launch({
  executablePath: "/usr/bin/chromium",
  headless: true,
  args: ["--no-sandbox"],
});
const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } }),
  checks = [],
  errors = [];
page.on("pageerror", (e) => errors.push(e.message));
const check = (name, expected, actual) => {
  assert.deepEqual(actual, expected, name);
  checks.push({
    name,
    expected: typeof expected === "object" ? "deep equality" : expected,
    actual: typeof actual === "object" ? "deep equality passed" : actual,
  });
};
const ready = () =>
  page.waitForFunction(
    () =>
      window.labSnapshot && JSON.parse(window.labSnapshot()).result !== null,
  );
const snap = () => page.evaluate(() => window.sceneSnapshot());
const model = () => page.evaluate(() => JSON.parse(window.labSnapshot()));
const pixels = async () =>
  crypto
    .createHash("sha256")
    .update(await page.locator("#scene-canvas").screenshot())
    .digest("hex");
try {
  await page.goto("http://127.0.0.1:4173");
  await ready();
  check(
    "real WebGL2 renderer produces triangles",
    true,
    (await snap()).triangles > 1000,
  );
  check(
    "single persistent canvas",
    1,
    await page.locator("#scene-canvas").count(),
  );
  await page.evaluate(() => {
    window.initialCanvas = document.querySelector("#scene-canvas");
  });
  await page.locator("#example").selectOption("elevated-full");
  await page.waitForFunction(
    () => JSON.parse(window.labSnapshot()).model.name === "elevated-full",
  );
  await ready();
  check(
    "mainline identity labels",
    ["A", "B"],
    await page.locator(".scene-label").allTextContents(),
  );
  let s = await snap();
  check("full preview contains two mains plus eight ramps", 10, s.roads.length);
  check("full fixed OD paths present", 12, Object.keys(s.data.paths).length);
  check(
    "source heights really differ",
    true,
    s.data.roads.find((r) => r.id === "A").points[0][2] !==
      s.data.roads.find((r) => r.id === "B").points[0][2],
  );
  await page.locator("#camera-reset").click();
  await page
    .locator("#three-panel")
    .screenshot({ path: "evidence/three-initial.png" });
  await page.locator("#scene-canvas").scrollIntoViewIfNeeded();
  let before = await snap(),
    beforePixels = await pixels(),
    box = await page.locator("#scene-canvas").boundingBox();
  await page.mouse.move(box.x + box.width * 0.5, box.y + box.height * 0.5);
  await page.mouse.down();
  await page.mouse.move(box.x + box.width * 0.7, box.y + box.height * 0.62, {
    steps: 12,
  });
  await page.mouse.up();
  check(
    "actual left drag rotates camera",
    true,
    JSON.stringify((await snap()).camera) !== JSON.stringify(before.camera),
  );
  check(
    "orbit changes actual rendered pixels",
    true,
    (await pixels()) !== beforePixels,
  );
  before = await snap();
  await page.mouse.move(box.x + box.width * 0.5, box.y + box.height * 0.5);
  await page.mouse.down({ button: "right" });
  await page.mouse.move(box.x + box.width * 0.58, box.y + box.height * 0.55, {
    steps: 8,
  });
  await page.mouse.up({ button: "right" });
  check(
    "actual right drag pans target",
    true,
    JSON.stringify((await snap()).target) !== JSON.stringify(before.target),
  );
  before = await snap();
  await page.mouse.wheel(0, -250);
  await page.waitForTimeout(80);
  check(
    "wheel changes camera distance",
    true,
    JSON.stringify((await snap()).camera) !== JSON.stringify(before.camera),
  );
  await page.locator("#selected-od").selectOption("A->B+");
  s = await snap();
  check("OD selection highlights full path", "A->B+", s.selected);
  check(
    "selected path includes approach and exit",
    true,
    s.data.paths[s.selected].length > 129,
  );
  await page
    .locator("#three-panel")
    .screenshot({ path: "evidence/three-orbit-zh.png" });
  const computed = await model(),
    camera = (await snap()).camera;
  await page.locator("#lang").click();
  check(
    "language preserves all model/computation data",
    computed,
    await model(),
  );
  check("language preserves camera", camera, (await snap()).camera);
  await page.locator('[data-od="A->B+"]').uncheck();
  await ready();
  check(
    "partial removes only requested connector",
    9,
    (await snap()).roads.length,
  );
  check(
    "excluded path is not fabricated",
    false,
    Object.hasOwn((await snap()).data.paths, "A->B+"),
  );
  check(
    "excluded selection explicitly labelled",
    true,
    (await page.locator("#path-state").textContent()).includes("Excluded"),
  );
  await page.locator("#full").click();
  await ready();
  check("full restores connector preview", 10, (await snap()).roads.length);
  await page.locator('[data-profile="1"][data-field="z0"]').fill("80");
  await page.locator('[data-profile="1"][data-field="z0"]').press("Tab");
  await ready();
  check(
    "elevation edit updates actual scene height",
    80,
    (await snap()).data.roads.find((r) => r.id === "B").points[0][2],
  );
  check(
    "invalid ramps remain visible red-tagged",
    true,
    (await snap()).roads.some((r) => r.kind === "ramp" && r.invalid),
  );
  await page.locator('[data-profile="0"][data-field="g0"]').fill("3");
  await page.locator('[data-profile="0"][data-field="g0"]').press("Tab");
  await ready();
  check(
    "percent UI stored as dimensionless grade",
    0.03,
    (await model()).model.vertical.profiles[0].g0,
  );
  await page.locator("#camera-reset").click();
  await page
    .locator("#three-panel")
    .screenshot({ path: "evidence/three-height-en.png" });
  await page.locator("#selected-od").selectOption("A+>B+");
  await page.locator("#view-profile").click();
  check(
    "profile view contains computed curve",
    1,
    await page.locator("#profile-chart polyline").count(),
  );
  check(
    "profile labels analytical peak and station",
    true,
    (await page.locator("#profile-panel").textContent()).includes(
      "Analytical peak grade",
    ),
  );
  await page
    .locator("#profile-panel")
    .screenshot({ path: "evidence/three-profile-en.png" });
  await page.locator("#view-three").click();
  await page.locator("#camera-top").click();
  s = await snap();
  check(
    "top camera is above target",
    true,
    Math.abs(s.camera[0] - s.target[0]) < 1e-8 &&
      Math.abs(s.camera[2] - s.target[2]) < 0.01,
  );
  await page
    .locator("#three-panel")
    .screenshot({ path: "evidence/three-invalid-top-en.png" });
  await page.locator("#camera-reset").click();
  const beforeExport = await model(),
    download = page.waitForEvent("download");
  await page.locator("#export").click();
  await (await download).saveAs("evidence/vertical-export.json");
  await page.locator("#reset").click();
  await ready();
  await page.locator("#import").setInputFiles("evidence/vertical-export.json");
  await page.waitForFunction(
    () => JSON.parse(window.labSnapshot()).model.name === "elevated-full",
  );
  await ready();
  check(
    "schema2 export/reload recomputes identical profiles and paths",
    beforeExport,
    await model(),
  );
  const prior = await model(),
    legacy = structuredClone(prior.model);
  delete legacy.vertical;
  legacy.schema = 1;
  legacy.solver = "offline-0.1.0";
  await page.locator("#import").setInputFiles({
    name: "legacy.json",
    mimeType: "application/json",
    buffer: Buffer.from(JSON.stringify(legacy)),
  });
  await page.locator("#accept-migration").waitFor();
  check(
    "legacy import does not silently replace current model",
    prior,
    await model(),
  );
  await page.locator("#accept-migration").click();
  await ready();
  check(
    "accepted legacy migration records assumption",
    "legacy-flat-accepted",
    (await model()).model.vertical.source,
  );
  check(
    "flat assumed heights only after acceptance",
    true,
    (await snap()).data.roads
      .filter((r) => r.kind === "mainline")
      .every((r) => r.points.every((p) => p[2] === 0)),
  );
  await page.locator('[data-profile="0"][data-field="z0"]').fill("");
  await page.locator('[data-profile="0"][data-field="z0"]').press("Tab");
  await ready();
  check(
    "invalid height produces input failure",
    "INPUT_INVALID",
    (await model()).result.status,
  );
  check("invalid input clears scene geometry", 0, (await snap()).roads.length);
  await page.locator("#undo").click();
  await ready();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.locator("#view-three").click();
  await page.locator("#camera-reset").click();
  check(
    "mobile real canvas has usable dimensions",
    true,
    (await page.locator("#scene-canvas").boundingBox()).width >= 300,
  );
  before = await snap();
  await page.locator("#camera-in").click();
  check(
    "mobile zoom button changes view",
    true,
    JSON.stringify(before.camera) !== JSON.stringify((await snap()).camera),
  );
  await page
    .locator("#three-panel")
    .screenshot({ path: "evidence/three-mobile-en.png" });
  check(
    "no horizontal page overflow on mobile",
    true,
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  );
  check(
    "same WebGL canvas reused across input/locale/view changes",
    true,
    await page.evaluate(
      () => window.initialCanvas === document.querySelector("#scene-canvas"),
    ),
  );
  const fallback = await browser.newPage();
  await fallback.addInitScript(() => {
    const original = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = function (type, ...args) {
      return type === "webgl2" ? null : original.call(this, type, ...args);
    };
  });
  await fallback.goto("http://127.0.0.1:4173");
  await fallback.waitForFunction(
    () => window.labSnapshot && JSON.parse(window.labSnapshot()).result,
  );
  check(
    "unavailable WebGL exposes actual 2D fallback",
    true,
    await fallback.locator("#plan-panel").isVisible(),
  );
  check(
    "fallback reports unavailable renderer",
    true,
    (await fallback.locator("#three-host").textContent()).includes("WebGL2"),
  );
  await fallback.close();
  check("no unhandled browser errors", [], errors);
  fs.writeFileSync(
    "evidence/browser3d.json",
    JSON.stringify(
      {
        date: new Date().toISOString(),
        sourceParent: execFileSync("git", ["rev-parse", "HEAD"], {
          encoding: "utf8",
        }).trim(),
        version: "offline-0.2.0",
        browser: await browser.version(),
        renderer: "Three.js 0.186.1 WebGL2",
        checks,
        screenshots: [
          "three-initial.png",
          "three-height-en.png",
          "three-orbit-zh.png",
          "three-profile-en.png",
          "three-invalid-top-en.png",
          "three-mobile-en.png",
        ],
      },
      null,
      2,
    ) + "\n",
  );
  console.log(`${checks.length} 3D browser assertions passed`);
} finally {
  await browser.close();
}
