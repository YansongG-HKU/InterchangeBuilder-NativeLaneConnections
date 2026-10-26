import { chromium } from "playwright";
import assert from "node:assert/strict";
import fs from "node:fs";
import { execFileSync } from "node:child_process";
const browser = await chromium.launch({
  executablePath: "/usr/bin/chromium",
  headless: true,
  args: ["--no-sandbox"],
});
const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } });
const checks = [],
  errors = [];
page.on("pageerror", (e) => errors.push(e.message));
const check = (name, expected, actual) => {
  assert.deepEqual(actual, expected, name);
  checks.push({ name, expected, actual });
};
const snap = () => page.evaluate(() => window.sceneSnapshot());
try {
  await page.goto("http://127.0.0.1:4173");
  await page.waitForFunction(
    () => window.labSnapshot && JSON.parse(window.labSnapshot()).result,
  );
  await page.locator("#example").selectOption("elevated-full");
  await page.waitForFunction(
    () =>
      JSON.parse(window.labSnapshot()).model.name === "elevated-full" &&
      JSON.parse(window.labSnapshot()).result,
  );
  await page.locator('[data-profile="1"][data-field="z0"]').fill("80");
  await page.locator('[data-profile="1"][data-field="z0"]').press("Tab");
  await page.waitForFunction(
    () =>
      JSON.parse(window.labSnapshot()).result &&
      JSON.parse(window.labSnapshot()).model.vertical.profiles[1].z0 === 80,
  );
  const info = await page.evaluate(() => {
    const { result } = JSON.parse(window.labSnapshot());
    const c =
      result.candidates.find((c) => c.id === "d1") ?? result.candidates[0];
    const ds = [...result.diagnostics, ...c.diagnostics, ...c.splitDiagnostics];
    const index = ds.findIndex((d) => d.code === "GRADE_LIMIT" && d.point);
    return { index, diagnostic: ds[index] };
  });
  assert.ok(info.index >= 0, "fixture has located ramp grade diagnostic");
  for (const lang of ["zh", "en"]) {
    if (lang === "en") await page.locator("#lang").click();
    await page.locator("#view-three").click();
    await page.locator("#camera-reset").click();
    const before = await snap();
    await page.locator(`[data-diag="${info.index}"]`).click();
    const after = await snap();
    check(
      `${lang}: three camera actually moves`,
      true,
      JSON.stringify(before.camera) !== JSON.stringify(after.camera),
    );
    check(`${lang}: target matches diagnostic XY`, info.diagnostic.point, [
      after.target[0],
      -after.target[2],
    ]);
    check(
      `${lang}: three view remains visible`,
      true,
      await page.locator("#three-panel").isVisible(),
    );
    await page.locator("#view-plan").click();
    const planarCamera = (await snap()).camera;
    await page.locator(`[data-diag="${info.index}"]`).click();
    check(
      `${lang}: plan remains visible`,
      true,
      await page.locator("#plan-panel").isVisible(),
    );
    const marker = page.locator('#plan circle[stroke="#ff986c"]');
    check(`${lang}: planar marker matches diagnostic`, info.diagnostic.point, [
      Number(await marker.getAttribute("cx")),
      -Number(await marker.getAttribute("cy")),
    ]);
    check(
      `${lang}: plan click preserves three camera`,
      planarCamera,
      (await snap()).camera,
    );
  }
  check("no unhandled browser errors", [], errors);
  fs.writeFileSync(
    "evidence/diagnostic-focus.json",
    JSON.stringify(
      {
        date: new Date().toISOString(),
        sourceParent: execFileSync("git", ["rev-parse", "HEAD"], {
          encoding: "utf8",
        }).trim(),
        version: "offline-0.2.0",
        browser: await browser.version(),
        input: "elevated-full; B.z0=80m; selected=d1",
        diagnostic: info.diagnostic,
        checks,
      },
      null,
      2,
    ) + "\n",
  );
  console.log(`${checks.length} diagnostic focus browser assertions passed`);
} finally {
  await browser.close();
}
