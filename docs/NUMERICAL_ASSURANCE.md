# Explicit numerical evidence levels / 显式数值保证等级

This safety-semantics slice changes structured result interpretation, not geometry thresholds or algorithms. All12 public Core result types with CanApplyToGame expose:

- NumericalEvidence = EngineeringChecksOnly (typed enum).
- NumericalEvidenceLabel = "EngineeringChecksOnly" (readable nested JSON label).
- IsMachineCertified = false.
- NumericalLimitation = shared explanation that Separated/Verified is scoped engineering evaluation without complete per-operation inclusion/roundoff proof.
- CanApplyToGame = false, unchanged.

Runner JSON also exposes textual evidence level, isMachineCertified=false and canApplyToGame=false at its root, so selectively exported old status/metrics records cannot be interpreted without this envelope. Status enums and candidate ranking remain compatible; successful geometric status/index means engineering candidate evaluation only. None is a placement permit. Exact dyadic seam predicates certify only that predicate, not the remaining floating-point pipeline. No new reliable-interval implementation is claimed.

Mathematical sufficient conditions remain documented separately. Reliable machine certification would require authenticated input-error bounds and full outward-rounded/inclusion calculations through subdivision, derivatives, norms, station/profile extrema and all branch decisions. Until then every existing certificate remains EngineeringChecksOnly regardless of a successful Status.

Regression enumerates every public game-gated result type and checks its explicit numerical fields, verifies successful self-status downgrade, and checks serialized success retains IsMachineCertified=false. Geometry/status/candidate generation/selection thresholds are unchanged. This is the next bounded improvement after the design audit, not another proof-family expansion.

User authorized pushing project code/tests/evidence/design and a stacked Draft PR on2026-10-28. Target base remains integration/offline-lab (PR9), no merge/main/release/installation changes. Historical records stating local-only/no-push describe their then authorization and are preserved. Current publication evidence and fresh tests are in assurance-release-evidence. Source includes only project materials, no Game/base Mod/Unity/Colossal payload.

Existing unsupported boundaries remain: general loop/wide-cone self domains, nonlinear joint-crossing generation, full-width slope/seam models, reliable machine inclusion, actual cubic engine conversion and game building/recovery. Missing real base/game versions and creation lifecycle contracts block engine activation, not independent Core math.
