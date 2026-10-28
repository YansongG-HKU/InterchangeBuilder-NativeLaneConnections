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
const upload = (name, input, output) => ({
  name,
  mimeType: "application/json",
  buffer: Buffer.from(
    JSON.stringify({ schema: "tail-preview-v1", input, output }),
  ),
});
fs.mkdirSync("evidence-tail-v2", { recursive: true });
try {
  await page.goto("http://127.0.0.1:4173/curve-lab.html");
  await page.waitForFunction(() => window.tailSnapshot);
  await page.locator("#fixture").selectOption("automatic-overpass");
  let s = await snap();
  check("automatic profile generated, not reference", false, s.reference);
  check("automatic profile status", "BOUNDED_CANDIDATE", s.result.status);
  check(
    "actual generated peak grade",
    0.075,
    s.selected.facts.metrics.maxGrade,
  );
  check(
    "actual full overlap clearance",
    8.5,
    s.selected.facts.collisions[0].clearance.minimum,
  );
  check(
    "actual elevated mesh",
    true,
    await page.evaluate(() => window.tailScene().triangles > 100),
  );
  await page.locator("#camera-reset").click();
  await page.screenshot({
    path: "evidence-tail-v2/automatic-overpass-zh.png",
    fullPage: true,
  });
  const before = s;
  await page.locator("#language").click();
  check("language preserves generated result", before, await snap());
  await page.locator("#required-clearance").fill("9");
  await page.locator("#required-clearance").press("Tab");
  check(
    "clearance edit regenerates higher profile",
    true,
    (await snap()).selected.output.segments[1].z.z0 > 10,
  );
  await page.locator("#fixture").selectOption("curved-context-left");
  check(
    "actual full curved contexts shown",
    true,
    await page.evaluate(() => window.tailScene().triangles > 100),
  );
  await page.locator("#camera-reset").click();
  await page.screenshot({
    path: "evidence-tail-v2/curved-context-en.png",
    fullPage: true,
  });
  for (const start of ["left", "middle", "right"])
    for (const end of ["left", "middle", "right"]) {
      await page.locator("#start-side").selectOption(start);
      await page.locator("#end-side").selectOption(end);
      s = await snap();
      check(
        `fixed ports ${start}/${end}`,
        ["S", "T"],
        [s.selected.output.startId, s.selected.output.endId],
      );
    }
  await page.locator("#fixture").selectOption("middle-tail");
  await page.locator("#building").check();
  const facts = (await snap()).selected.facts;
  let previous = (await snap()).revision;
  for (const mode of ["no-collision", "normal", "no-collision", "normal"]) {
    await page.locator("#mode").selectOption(mode);
    s = await snap();
    check(
      `mode ${mode} retains facts rev${s.revision}`,
      facts,
      s.selected.facts,
    );
    check(
      `mode ${mode} permission rev${s.revision}`,
      mode === "no-collision",
      s.selected.permission.allowedForOfflinePreview,
    );
    assert.ok(s.revision > previous);
    previous = s.revision;
  }
  await page.locator("#fixture").selectOption("automatic-overpass");
  s = await snap();
  const original = structuredClone(s);
  await page.evaluate(() => {
    const real = File.prototype.text;
    window.delayed = {};
    File.prototype.text = function () {
      if (this.name.startsWith("slow"))
        return new Promise(
          (resolve) =>
            (window.delayed[this.name] = () => real.call(this).then(resolve)),
        );
      return real.call(this);
    };
  });
  await page
    .locator("#import")
    .setInputFiles(upload("slow-B.json", s.input, s.selected.output));
  check("pending import clears old selected", null, (await snap()).selected);
  check(
    "pending import disables export",
    true,
    await page.locator("#export").isDisabled(),
  );
  await page.locator("#tail-length").fill("45"); // invalidates B even before blur
  await page.locator("#tail-length").press("Tab");
  await page.locator("#mode").selectOption("no-collision");
  await page.locator("#mode").selectOption("normal");
  const d = await snap();
  await page
    .locator("#import")
    .setInputFiles(upload("fast-D.json", d.input, d.selected.output));
  await page.waitForFunction(
    () => window.tailSnapshot().result.status === "IMPORTED_RECHECK",
  );
  const latest = await snap();
  await page.evaluate(() => window.delayed["slow-B.json"]());
  await page.waitForTimeout(100);
  check("late B cannot replace fast D after edits/modes", latest, await snap());
  await page
    .locator("#import")
    .setInputFiles(
      upload("slow-language.json", original.input, original.selected.output),
    );
  await page.locator("#language").click();
  const lang = await snap();
  await page.evaluate(() => window.delayed["slow-language.json"]());
  await page.waitForTimeout(100);
  check("language intent invalidates old import", lang, await snap());
  check(
    "invalidated pending import cannot revive permission",
    null,
    (await snap()).selected,
  );
  await page.locator("#import").setInputFiles({
    name: "broken.json",
    mimeType: "application/json",
    buffer: Buffer.from("{bad"),
  });
  await page.waitForFunction(() =>
    window.tailSnapshot().error.startsWith("IMPORT_REJECTED"),
  );
  check(
    "failed import keeps export disabled",
    true,
    await page.locator("#export").isDisabled(),
  );
  await page.locator("#fixture").selectOption("automatic-overpass");
  s = await snap();
  const download = page.waitForEvent("download");
  await page.locator("#export").click();
  await (await download).saveAs("evidence-tail-v2/automatic-export.json");
  await page.locator("#fixture").selectOption("middle-tail");
  await page
    .locator("#import")
    .setInputFiles("evidence-tail-v2/automatic-export.json");
  await page.waitForFunction(
    () => window.tailSnapshot().result.status === "IMPORTED_RECHECK",
  );
  check(
    "saved actual generated profile rechecked unchanged",
    s.selected.output,
    (await snap()).selected.output,
  );
  check(
    "saved generated profile passes independent recheck",
    "VERIFIED_BOUNDED",
    (await snap()).selected.facts.status,
  );
  await page.setViewportSize({ width: 390, height: 844 });
  await page.locator("#camera-reset").click();
  await page.screenshot({
    path: "evidence-tail-v2/automatic-mobile.png",
    fullPage: true,
  });
  check(
    "mobile no horizontal overflow",
    true,
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  );
  check("no browser errors", [], errors);
  fs.writeFileSync(
    "evidence-tail-v2/browser.json",
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
  console.log(`${checks.length} phase2 browser assertions passed`);
} finally {
  await browser.close();
}
