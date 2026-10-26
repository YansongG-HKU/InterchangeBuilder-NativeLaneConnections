# 三维连接预览契约 / 3D connection preview contract

Implemented bounded V1 / 已实现有限 V1. Same bounded feature branch; not a CS2 integration or full lane interchange.

- Primary scene owner: `ConnectionScene` in `src/view/scene.js`; one WebGL2 renderer/context. Three.js 0.186.1, explicit OrbitControls addon. Existing SVG plan/profile are deliberate alternate views, never a fake duplicate 3D renderer.
- Coordinate mapping: computational (x,y,z height) metres → Three (x,z,-y), Y-up. No vertical exaggeration. Camera perspective for spatial inspection, fixed named top/reset positions, DOM orientation and units. All geometry derives from the same pure computed model.
- Semantics: two mainline pavement strips, requested turning strips, endpoint markers/direction arrows; strips are sampled pavement, not complete structural meshes or detailed lane allocation. Existing six-lane local fork remains separate and excluded from OD area.
- Selection: stable directed OD IDs; highlighted route includes incoming corridor section, actual connector and outgoing section, or full directed source for a through OD. Forbidden OD shows not provided, never draws an invented connector.
- Appearance: invalid connector red and retained; unknown validation amber/neutral, never green pass. Selection uses thick highlight in addition to color. No moving cars/flow particles or traffic claims.
- Input state: loading revision guards remain; parameter change invalidates displayed data. Locale/camera/selection do not modify geometric inputs. Recomputations preserve camera unless reset is explicitly requested.
- Camera controls: mouse orbit/pan/wheel, touch orbit/two-finger pan/zoom; reset/top and zoom buttons available without gesture dependence. No camera permissions, WebXR, remote assets, continuous animation or background rendering.
- Performance/lifecycle: cap DPR at 1.5; bounded sampled BufferGeometry; replace/dispose road buffers only on changed data, keep one canvas across DOM renders, on-demand render for controls/resize/data. ResizeObserver detached/rebound with host. No unbounded animation loop. Renderer failure shows explicit WebGL unavailable and real 2D fallback.
- Readiness/test signal: read-only camera, object IDs, selected OD, rendered triangle count and scene data snapshot. QA requires actual drag/pan/wheel DOM input and pixels/screenshots; scene object existence alone is insufficient.
- Vertical model is independent pure computation, documented in [VERTICAL_PROFILES](VERTICAL_PROFILES.md). Core preview emits the only road/path coordinates consumed by Three.js; no scene-specific elevation interpolation. Schema 1 migration is explicit; terrain/clearance never become a valid zero-height assumption.

Official references read: [OrbitControls](https://threejs.org/docs/pages/OrbitControls.html), [WebGLRenderer](https://threejs.org/docs/pages/WebGLRenderer.html), [BufferGeometry](https://threejs.org/docs/pages/BufferGeometry.html). Library pinned in package-lock.json. Three.js visualization skill applied; linked optional template resource was unavailable, so this document explicitly records its required scene/coordinate/fallback/interaction/readiness contract.

Measured tests and actual screenshots are indexed in [OFFLINE_VALIDATION](OFFLINE_VALIDATION.md). No camera permission or external GPU service; local Chromium WebGL2 was exercised. DOM stays readable at 390px; touch controls use OrbitControls, while automated gesture evidence is mouse and explicit zoom buttons, not a claim of physical-device touch testing.

Build tradeoff: Three.js increases the production JavaScript chunk beyond Vite's default 500kB warning threshold. This is recorded as a bundle-size warning, not a build failure or a claim of arbitrary-device performance; no texture/model assets or paid services are fetched at runtime.

Diagnostic navigation follows the active view: located errors pan the 3D camera and add the existing issue marker in three mode; plan mode retains its SVG marker and scroll target. Profile mode scrolls the existing profile panel without claiming diagnostic-to-profile selection. Diagnostics without coordinates do not invent a camera target. / 诊断按当前视图定位；无位置的错误不伪造空间目标。

## 后续范围 / Future scope

[深度案例](DEEP_CASE_PLAN.md) 将三维用于完整车道路径、断面和结构冲突验证；这是未实现目标。现有道路条带、OD高亮和浏览器测试只证明当前展示行为。 / Future lane and structural inspection is not established by current rendering evidence.
