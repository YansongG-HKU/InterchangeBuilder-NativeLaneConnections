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

2026-10-28 local extension: [curve primitives, bounded station/profile functions and inspected split candidates](docs/CURVE_FUNCTIONS.md). The original inspector scope and game exclusions remain; experimental search is default-disabled. / 已扩充纯数学内核，未启用游戏路径。

Current extension: [hard-region routing and independent smoothing validation](docs/RESTRICTED_ROUTING.md); bounded planar support, runtime disabled.

[Partial spatial proof checkpoint](docs/SPATIAL_PROOF_CHECKPOINT.md): exact straight-prism references only; curved 3D/global self-proof and selection integration remain incomplete. Current runtime/game boundaries unchanged.

[Nonstraight exact quarter-arc proof slice](docs/QUARTER_ARC_PROOF.md) adds bounded flat annular-strip+tail full-domain certification and actual 3D candidate selection. General curved/nonconstant-profile proof remains incomplete.

[General rising/descending Bezier external certificate](docs/BEZIER_EXTERNAL_3D.md) now checks continuous station envelopes against all supplied obstacles. General self remains UNKNOWN; no complete placement permission.

[Noncircular normal-strip global sufficient certificate](docs/NORMAL_STRIP_SELF_PROOF.md) now covers forward narrow-cone Bezier subsets; arbitrary self domains and game permission remain unsupported. Historical general-self UNKNOWN records are not rewritten.
