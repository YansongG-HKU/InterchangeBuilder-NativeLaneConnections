import { chromium } from "playwright";
import assert from "node:assert/strict";
import fs from "node:fs";
const browser = await chromium.launch({
  executablePath: "/usr/bin/chromium",
  headless: true,
  args: ["--no-sandbox"],
});
const page = await browser.newPage({ viewport: { width: 1500, height: 1050 } }),
  checks = [],
  errors = [];
page.on("pageerror", (e) => errors.push(e.message));
const check = (name, expected, actual) => {
  assert.deepEqual(actual, expected, name);
  checks.push({ name, expected, actual });
};
const snap = () => page.evaluate(() => window.tailSnapshot());
try {
  await page.goto("http://127.0.0.1:4173/curve-lab.html");
  await page.waitForFunction(() => window.tailSnapshot);
  check(
    "generated candidate shown",
    "BOUNDED_CANDIDATE",
    (await snap()).result.status,
  );
  check(
    "actual finite tail",
    40,
    (await snap()).selected.facts.metrics.straightTail,
  );
  check(
    "actual WebGL geometry rendered",
    true,
    await page.evaluate(() => window.tailScene().triangles > 100),
  );
  await page.locator("#camera-reset").click();
  await page
    .locator("#three-host")
    .screenshot({ path: "evidence-tail/middle-three-zh.png" });
  const before = await snap();
  await page.locator("#language").click();
  check("language preserves full computation", before, await snap());
  await page.locator("#building").check();
  let normal = await snap();
  check(
    "normal collision blocks preview",
    false,
    normal.selected.permission.allowedForOfflinePreview,
  );
  await page.locator("#mode").selectOption("no-collision");
  let loose = await snap();
  check(
    "building-only relaxation permits",
    true,
    loose.selected.permission.allowedForOfflinePreview,
  );
  check(
    "mode retains actual collision facts",
    normal.selected.facts,
    loose.selected.facts,
  );
  check(
    "mode invalidates prior revision",
    true,
    loose.revision > normal.revision,
  );
  await page.locator("#mode").selectOption("normal");
  check(
    "return to normal blocks again",
    false,
    (await snap()).selected.permission.allowedForOfflinePreview,
  );
  await page.locator("#building").uncheck();
  await page.locator("#tail-length").fill("50");
  check(
    "editing immediately invalidates export",
    true,
    await page.locator("#export").isDisabled(),
  );
  await page.locator("#tail-length").press("Tab");
  check(
    "new actual tail length",
    50,
    (await snap()).selected.facts.metrics.straightTail,
  );
  const exported = await snap(),
    download = page.waitForEvent("download");
  await page.locator("#export").click();
  await (await download).saveAs("evidence-tail/export.json");
  await page.locator("#fixture").selectOption("straight-main-left");
  await page.locator("#import").setInputFiles("evidence-tail/export.json");
  await page.waitForFunction(
    () => window.tailSnapshot().result.status === "IMPORTED_RECHECK",
  );
  check(
    "export reimport output unchanged",
    exported.selected.output,
    (await snap()).selected.output,
  );
  check(
    "export reimport independent facts unchanged",
    exported.selected.facts,
    (await snap()).selected.facts,
  );
  await page.locator("#fixture").selectOption("curved-main-left");
  await page.locator("#continuity").selectOption("G2");
  check(
    "G2 strict-tail conflict explicit",
    "PROVEN_CONSTRAINT_CONFLICT",
    (await snap()).result.status,
  );
  check(
    "no fake mesh on conflict",
    0,
    await page.evaluate(() => window.tailScene().roads.length),
  );
  await page.locator("#fixture").selectOption("equal-height-overpass");
  check(
    "arch generation unsupported",
    "UNSUPPORTED",
    (await snap()).result.status,
  );
  await page.locator("#reference").check();
  check(
    "reference is explicitly different",
    "REFERENCE_INSPECTION",
    (await snap()).result.status,
  );
  check(
    "full overlap known reference clearance",
    8.5,
    (await snap()).selected.facts.collisions[0].clearance.minimum,
  );
  await page.locator("#camera-reset").click();
  await page
    .locator("#three-host")
    .screenshot({ path: "evidence-tail/bridge-reference-en.png" });
  await page.locator("#fixture").selectOption("same-direction-shift");
  await page.locator("#end-side").selectOption("left");
  check(
    "left is positive travel-normal offset",
    6,
    (await snap()).input.end.offset,
  );
  check(
    "selected node identity retained",
    "T",
    (await snap()).selected.output.endId,
  );
  await page.locator("#end-height").fill("");
  await page.locator("#end-height").press("Tab");
  check(
    "invalid numeric input rejected",
    "INPUT_INVALID",
    (await snap()).result.status,
  );
  check("invalid input clears output", null, (await snap()).selected);
  await page.locator("#fixture").selectOption("middle-tail");
  await page.locator("#import").setInputFiles({
    name: "bad.json",
    mimeType: "application/json",
    buffer: Buffer.from("{bad"),
  });
  await page.waitForFunction(() =>
    window.tailSnapshot().error.startsWith("IMPORT_REJECTED"),
  );
  check(
    "bad import retains previous model",
    "middle-tail",
    (await snap()).input.name,
  );
  await page.setViewportSize({ width: 390, height: 844 });
  await page.locator("#camera-reset").click();
  check(
    "mobile no horizontal overflow",
    true,
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  );
  await page
    .locator("#three-host")
    .screenshot({ path: "evidence-tail/mobile-en.png" });
  check("no unhandled browser errors", [], errors);
  fs.writeFileSync(
    "evidence-tail/browser.json",
    JSON.stringify(
      {
        date: new Date().toISOString(),
        browser: await browser.version(),
        checks,
      },
      null,
      2,
    ) + "\n",
  );
  console.log(`${checks.length} tail browser assertions passed`);
} finally {
  await browser.close();
}
