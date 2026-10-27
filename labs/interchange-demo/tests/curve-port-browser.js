import { chromium } from "playwright";
import assert from "node:assert/strict";
import fs from "node:fs";
const browser = await chromium.launch({
  executablePath: "/usr/bin/chromium",
  headless: true,
  args: ["--no-sandbox"],
});
const page = await browser.newPage({ viewport: { width: 1500, height: 1050 } }),
  checks = [];
const errors = [];
page.on("pageerror", (e) => errors.push(e.message));
const check = (name, expected, actual) => {
  assert.deepEqual(actual, expected, name);
  checks.push({ name, expected, actual });
};
try {
  await page.goto("http://127.0.0.1:4173/curve-lab.html");
  await page.waitForFunction(() => window.tailSnapshot);
  for (const name of [
    "rotated-automatic-overpass",
    "curved-oblique-overpass",
  ]) {
    await page.locator("#fixture").selectOption(name);
    const s = await page.evaluate(() => window.tailSnapshot());
    check(name + " generated", "BOUNDED_CANDIDATE", s.result.status);
    check(name + " not handwritten reference", false, s.reference);
    check(
      name + " independent geometry check",
      "VERIFIED_BOUNDED",
      s.selected.facts.status,
    );
    check(
      name + " real nonflat profile",
      true,
      s.selected.facts.points.some((p) => p[2] > 5),
    );
    await page.locator("#camera-reset").click();
    await page.screenshot({
      path: `evidence-curve-port/${name}.png`,
      fullPage: true,
    });
  }
  check("no browser errors", [], errors);
  fs.writeFileSync(
    "evidence-curve-port/browser.json",
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
  console.log(checks.length + " algorithm preview assertions passed");
} finally {
  await browser.close();
}
