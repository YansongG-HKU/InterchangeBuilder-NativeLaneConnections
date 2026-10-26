# Curved-corridor interchange / land-use optimization research

Date: 2026-10-25. Scope: requirements and mathematical design only, not implementation, game validation, real engineering approval, or a guarantee of globally optimal results. Proposal below is our project synthesis unless explicitly attributed. All game-dependent thresholds remain versioned/configured and NOT_TESTED.

## 1. Source-backed findings and limits

- **CGAL 5.2.3 arrangements** defines the planar subdivision induced by curves, including intersecting/self-intersecting curves, and subdivision into suitable subcurves. This supports treating multiple intersections/branches explicitly rather than assuming one orthogonal crossing. This is an older stable conceptual reference; it does not prove CS2 uses CGAL. [Official manual](https://doc.cgal.org/5.2.3/Arrangement_on_surface_2/index.html)
- **CGAL Boolean set operations** supports polygon-set union, difference, containment, and distinctions between boundary contact and interior overlap. These are the right mathematical concepts for area accounting and site masks, not bounding-box area. Library selection is undecided. [Official manual](https://doc.cgal.org/latest/Boolean_set_operations_2/index.html)
- **ASAM OpenDRIVE 1.8.1** describes straight, circular, spiral and parametric-cubic reference-line elements and using spirals to avoid abrupt curvature changes. Useful modeling analogy, NOT CS2 API or format compatibility. [Geometry introduction](https://publications.pages.asam.net/standards/ASAM_OpenDRIVE/ASAM_OpenDRIVE_Specification/v1.8.1/specification/09_geometries/09_01_introduction.html)
- **FHWA interchange promptlist** calls for evaluating alternatives against operations, topography and site constraints, directional movements, weaving and route continuity. Historical checklist, not a current universal regulation. [FHWA](https://highways.dot.gov/field-offices/missouri/interchange-design-promptlist)
- **TxDOT ramp chapter** treats ramp horizontal/vertical geometry, terminals and spacing together and explicitly acknowledges constrained right-of-way. We borrow considerations, NOT its jurisdiction-specific numeric values as game limits. [TxDOT §15.7](https://www.txdot.gov/content/txdotoms/us/en/manuals/des/rdw/chapter-15-grade-separations-and-interchanges-/15-7-ramps---direct-connectors-.html)
- **TxDOT cross-section chapter** distinguishes road, border/ROW, slopes, retaining structures and utilities. Borders can extend beyond construction limits for maintenance. This supports wider impact envelopes than asphalt width. No legal ROW inference is possible from geometry alone. [TxDOT §4.10](https://www.txdot.gov/content/txdotoms/us/en/manuals/des/rdw/chapter-4--basic-design-criteria/4-10-cross-sectional-elements.html)
- **pymoo authors' documentation** distinguishes hard constraints from objectives, mixed continuous/discrete variables, and non-dominated solution sets. This grounds our Pareto comparison proposal, not a commitment to install pymoo or claim global optimality. [Basics](https://pymoo.org/getting_started/preface.html), [Constrained formulation](https://pymoo.org/getting_started/part_1.html)

## 2. Two curved corridors: requirements, not a rotated cross template

Represent selected mainline planar reference curves as p(s), q(t), where s,t are **horizontal arc length in metres**, not raw Bézier parameters or world X/Y. Elevations z_p(s), z_q(t) are separate. Roads may be divided into directional carriageways; preserve their individual identities and directions.

1. Find all planar intersection pairs p(s)=q(t) in the selected influence area. Multiple pairs need explicit selection/grouping; do not silently choose the first root. A shared/coincident segment is a distinct ambiguous topology, not infinitely many ordinary crossings.
2. If no crossing exists, consider closest-approach candidates minimizing ||p(s)-q(t)||² (m²), including interior stationary candidates **and endpoints**. A nearby pair is a proposed connection site, not a crossing that already exists. Require distance/context checks; report unsupported/unselected cases.
3. Near-tangencies and overlaps need length/angular tolerances and an ambiguity warning. Bounding boxes are broad-phase culling only. Refinement must preserve branches; coarse sampling alone must not assert no intersection.
4. At each proposed entrance/exit anchor, calculate local unit tangent T=p'(s), normal N=(-T_y,T_x), and signed angle atan2(cross(T_p,T_q),dot(T_p,T_q)). A single crossing angle is insufficient: the tangents and curvature at all ramp terminals matter.
5. Move anchors by arc length along the original corridor and retain lockable sections. Keep direction/left-right consistency after rotation, mirroring, left-hand traffic, and S-bends. Reject nonregular cusps (undefined tangent) or explicitly split them before solving.
6. Planar crossings do not create network nodes automatically: elevations and requested movements distinguish overpass, underpass, and physical merge. Geometric proximity does not prove traversability.
7. Ramp endpoint constraints: position, directed tangent, lane correspondence, elevation and grade. Prefer smooth curvature progression; do not equate visually smooth centerline with a valid lane-width sweep. Validate swept road/shoulder geometry for self-overlap and inner-edge collapse.
8. When curve representation is a cubic polynomial, arc length normally requires numerical integration/inversion. Set a documented spatial/area error budget; do not use parameter t as metres. Validate the game's final discretized geometry separately.

Useful local metrics: curvature κ=dθ/ds (m⁻¹), radius R=1/|κ| (m where nonzero), grade g=dz/ds (dimensionless; display 100g percent). No default numeric radius/grade limit is asserted here.

## 3. Three different land metrics — never one ambiguous “area”

Let μ₂ be planar area in m²; 1 ha=10,000 m². All unions include their declared geometry error. Holes remain holes unless an explicit reservation rule fills them.

### A. Projected physical coverage / 平面投影覆盖

For each road/structure i with full left/right cross-section extents w_L(s), w_R(s) in m, a conceptual sweep is:

W_i = union over s of {p_i(s)+u N_i(s) : -w_R(s) <= u <= w_L(s)}, with explicit end caps, terminal/gore geometry and other physical structure projections.

A_projected = μ₂(union_i W_i).

Count overlapping bridge decks **once** in this metric, even at different heights. Do not sum segment rectangles, per-level areas, or bounding boxes. Simple normal offsets can self-intersect on tight curves; form/validate the full swept set instead of assuming an offset boundary is simple.

### B. Exclusive ground occupation / 地面实占

G = union of actual ground roadway footprint, embankment/cut footprint, abutments, piers, retaining-wall bases and explicitly reserved ground operational envelopes.

A_ground = μ₂(G).

Elevated deck projection is not automatically all ground occupation, but neither is land underneath automatically buildable/free: clearance, supports, access and game's zoning/collision restrictions still apply. Where support/terrain extents are unknown, mark the estimate uncertain and use a conservative exclusion envelope for feasibility. Keep physical ground occupation and additional reservation components inspectable.

### C. Total affected/reserved envelope / 综合影响范围

I = union of projected works, ground works, terrain-change extent, maintenance/access allowance, protected clearances, temporary or permanent construction reservations (separate toggles), and affected existing-object regions.

A_impact = μ₂(I).

An I envelope is a **planning/game approximation**, not a cadastral boundary or legal ROW. Different kinds of restriction must remain tagged. A polygon inside a ramp loop may be physically untouched yet inaccessible or unbuildable: show such residual parcels separately rather than count them as either automatically lost or automatically useful land.

### Baseline and reporting

For the same metric and study boundary, with baseline I_old and proposed I_new:

- newly affected area = μ₂(I_new \\ I_old);
- released area = μ₂(I_old \\ I_new);
- net area change = μ₂(I_new)-μ₂(I_old).

Do not call net change “new land required”; a small net change can hide large relocation. Ground/projected/impact deltas use their own matching baseline sets. Gross deck surface summed across levels may be useful as a structure-cost proxy, but label it separately; it is NOT land footprint. Bounding rectangle is only camera/layout/broad-phase information. If any measure is clipped to a study boundary, flag works extending outside instead of hiding them.

## 4. Terrain, height and space coupling

Hard inputs: editable allowed-site polygon P; tagged no-build areas/volumes N; locked objects; corridor selection; required movements; direction/lane constraints; configured max height H_max (m), grade g_max, minimum radius/clearance, and selected area cap (m²) **with an explicit metric**.

- Ground prohibition, overhead prohibition, and protected three-dimensional volume are different. A generic “no-build parcel” defaults to blocking all works unless user explicitly permits elevated crossing; do not infer bridge permission.
- Test the applicable envelope against P and N. A compliant road centerline can still have an illegal shoulder, pier or side slope. Do not use Anarchy/no-collision as a reason to ignore constraints.
- Elevation is measured against a named datum; display height above terrain and absolute elevation separately. Maximum stack count is a local crossing/topology attribute, not simply height divided by a universal floor height.
- At every overlapped point/region, upper bridge underside must exceed lower road clearance envelope. Simplified same-datum check: z_upper_road - deck_thickness_upper - z_lower_road >= clearance_required. All terms in m; full swept envelopes, superelevation and support collision may require stronger checks.
- From |dz/ds|<=g_max follows L_horizontal >= |Δz|/g_max (m) when g_max>0. This is only a necessary lower bound: vertical transitions, both ascent/descent, terminal grade, curve length and weaving/acceleration length can demand much more. It does NOT justify a feasible ramp by itself.
- For a simple flat-ground embankment of height h with side-slope ratio m horizontal per 1 vertical, one-side extra width is approximately m*h. This is an illustration, not a fixed game rule: actual terrain, drainage, cut/fill intersections and retaining walls alter extent. Evaluate terrain against the entire works surface; do not estimate by one center sample.
- If terrain changes are modeled, cut/fill volumes may be computed as integrals of positive/negative proposed-ground minus existing-ground height over their domain (m³). Compare volume and displaced objects separately; do not invent currency or environmental certification.

## 5. Presets and Pareto choices

**Suburban / 展开低层**: prefer lower structures/fewer stacked crossings; allow larger editable land envelope; preserve sensible ramp length, curvature and terminal spacing. Larger area is permitted, never forced. Environmental parcels or terrain can still make a suburban site compact.

**Urban / 紧凑用地**: tighter boundary or area cap; still enforce height, slope, clearance, movement/lane and existing-city restrictions. May favor directional ramps or retaining structures where feasible. Must not silently raise the height limit, delete movements, shrink lane widths or create extreme grades to meet land cap.

**Custom / 自定义**: independent controls for land cap, preferred height/stacking, allowed mainline displacement, preservation, required movement set, and aesthetic priorities. Clearly separate hard caps from soft preferences. “City/suburb” is not a legal or geometric classification and must not hardcode a fixed number of levels.

Conceptual design vector x combines discrete topology/level assignments with continuous anchors/control geometry/elevations. Generate and validate finite topology candidates before exposing preference ranking. Phase 1 need not promise a globally optimizing solver.

Proposed objective vector, with units preserved:

f(x) = (A_impact m², A_ground m², max_height m, local_stack_count integer, total_ramp_length m, structure_area m², earthwork_volume m³, modified_objects count, weaving-risk indicators).

Some objectives are correlated; choose a small legible subset in UI, keep detail available. Feasible x satisfies hard geometry/network/site constraints first. Candidate a dominates b only if a is no worse in all compared objectives and better in at least one. Show non-dominated trade-offs **among evaluated candidates**, not “the global Pareto frontier.” Candidates must use the same demand/movement set and hard settings to be comparable.

A weighted score may help sorting only after feasible filtering and dimensionless normalization against disclosed reference scales. Never add raw m²+m+m³ or let a low objective compensate for a hard violation. Prefer an area cap with alternative feasible trade-offs to one opaque “compactness” slider.

## 6. Explain failures and show the user the trade-off

Return distinct states: feasible candidate; invalid input/ambiguous selected crossing; provably contradictory constraints; no candidate found within explored topology/search budget; unknown because engine geometry/terrain/support data is unavailable. “Search failed” is not “mathematically impossible.”

For a failed candidate identify the offending place and measured constraint, e.g. required climb with insufficient available horizontal run, swept shoulder outside P, pier in protected parcel, or full-movement ramp without room. Offer explicit user-selectable changes: enlarge boundary, move anchor, change allowed topology, permit more height, change mainline displacement, or deliberately choose partial access. Never apply relaxation silently. Do not claim a globally minimal relaxation without proof.

Visualization requirements (zh-CN/en):

- selectable intersection/closest-approach markers and individual directional corridors;
- plan and 3D view with distinct projected / ground / impact layer toggles;
- editable allowed site, no-build masks, keep-out volumes and locked boundaries;
- elevation/grade profile along ramp arc length; hotspot labels at conflicts;
- side-by-side candidate cards showing m²/ha, height, levels, length, changed objects and unsupported/unknown indicators;
- compactness control reveals changed hard caps vs preferences, previews before apply;
- hatch unknown terrain/support envelopes; do not color uncertain constraints green;
- residual-land accessibility/buildability separately marked “unverified” unless game-tested.

## 7. Acceptance scenarios to incorporate

1. Two S-curves with several intersections: no implicit first-root choice; select crossing cluster, preserve unselected network.
2. No intersection but close approach, near tangent, coincident segments, and cusp: distinct outcomes; no fabricated node.
3. Rotate/mirror curve/site inputs: same physical areas and consistent directed movements within tolerance.
4. Arc-length terminal distances invariant to equivalent curve reparameterization; sampling tolerance documented.
5. Two overlapping decks: projected union no double count; deck-surface metric still records both; actual supports/ground independently counted.
6. Concave footprint / loop with hole: union area differs from bounding box; residual parcel not promised usable.
7. Embankment or pier outside boundary while centerline inside: blocked; overhead-only permission cannot authorize ground supports.
8. Same site, movement set, road widths and limits under urban/suburban presets: display genuine feasible trade-offs, not forced level count.
9. Very small land cap plus low height/grade limits: no silent movement deletion; distinguish proven conflict from exhausted search.
10. Tighten cap: unchanged hard constraints; any suggested relaxation explicit and unapplied until accepted.
11. Existing-vs-new footprint relocation: separately show newly affected, released, net; matching baseline semantics.
12. Geometry accepted but game discretization unsupported: remain NOT_TESTED / NOT_RUN and block claims of gameplay compatibility.

## Integration note

Add requirements for selectable crossing topology, editable land/height presets, named footprint metrics, constrained candidate comparison, and explanatory failure states. Synchronize both language READMEs, design/geometry/UX/acceptance docs and changelog in the parent-owned PR; this research file itself does not modify that repository. All newly proposed acceptance cases remain NOT_RUN until implemented and executed.
