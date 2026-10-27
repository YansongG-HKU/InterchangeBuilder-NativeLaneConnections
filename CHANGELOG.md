# Changelog

## Unreleased — proposed offline-lab integration

- Add an audited source snapshot and isolated Node lab; do not change C# runtime, release files or merge PR #3/#4.
- Establish one maintenance plan and approved semi-direct left-turn scope; no new algorithm or game validation in this revision.
- Preserve imported rights, evidence and source provenance; see docs/INTEGRATION.md.

## 2.2.0 - 2026-08-16

- Added width-aware endpoint candidates that mirror the game's cell-snap and free-width alignment branches.
- Retained centre alignment for mismatched road widths, including four-lane to two-lane connections.
- Selected left, centre, or right alignment from the pointer hit and preserved it through preview and placement.
- Added native connection-compatibility filtering and direction-aware multi-arm junction selection.
- Preserved node-split-only behavior and special non-zonable network handling.
- Added a Simplified Chinese UI translation and an in-panel endpoint-rule notice.
- Added 12 core tests and a seven-target runtime metadata smoke harness.
- Made the build accept explicit base-package and game paths without embedding game assemblies in source control.
- Published a ready-to-use binary package with direct Skyve II installation instructions.

## Local experiment — 2026-10-27

- Add isolated explicit straight-tail/quintic transition experiment and independent reference/fault checks for Issue #10; preserve baseline/runtime.
- Intermediate crossing generation remains unsupported; reference-only full-width orthogonal clearance is separate.
- No push, PR mutation, merge, release or game validation in this task.

- Local pre-delivery review: reject overflowing imported profiles, malformed joins/segments and invalid context elevation before geometry rendering; numerical failures never receive preview permission. Core/browser import regressions and evidence refreshed; game/Anarchy/C#/CI NOT_RUN.

- Local V2: automatic plateau overpass family with independent full-overlap clearance, actual Bezier context corridors/port binding, explicit width rejection, stronger pending-import invalidation and full regression. No runtime/CI/dependency change; game/Anarchy/C#/remote CI NOT_RUN.

- Local development/curve-kernel-port: migrate bounded quintic+finite-tail planar family to default-disabled pure C# Core; add shared JS/C# contracts and actual no-NuGet compiler runner. Lab adds local-frame/arc-length/oblique algorithm checks only. Existing runtime not called/changed, no push/merge/release; engine and standard test/build gates separately unrun.

Current local milestone: [independent C# planar verifier](docs/CORE_GEOMETRY_VALIDATION.md) inspects actual geometry and request binding; standard netstandard2.0 build and xUnit now pass. No runtime activation or game permission. / 已迁入独立平面验收，未接入游戏。
