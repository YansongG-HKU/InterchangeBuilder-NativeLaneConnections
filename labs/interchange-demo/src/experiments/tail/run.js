import { generate, convert, validateInput, VERSION } from "./generator.js";
import { inspect, permission } from "./checker.js";
export function evaluateCase(
  input,
  { reference = null, budget = 3, resolution } = {},
) {
  const problem = validateInput(input);
  if (problem)
    return {
      version: VERSION,
      status: "INPUT_INVALID",
      reason: problem,
      candidates: [],
    };
  const generation = generate(input, budget);
  const candidates = (reference ? [reference] : generation.candidates).map(
    (output) => {
      const facts = inspect(input, output, resolution),
        converted = convert(output),
        convertedFacts = inspect(input, converted, resolution);
      return {
        output: converted,
        facts: convertedFacts,
        conversionIdentical:
          JSON.stringify(facts) === JSON.stringify(convertedFacts),
        permission: permission(convertedFacts, input.mode),
      };
    },
  );
  const feasible = candidates.filter(
    (c) => c.permission.allowedForOfflinePreview,
  );
  // Hard permission first, then length proxy; no global optimum or exact footprint claim.
  feasible.sort(
    (a, b) => a.facts.metrics.lengthBounds[1] - b.facts.metrics.lengthBounds[1],
  );
  return {
    version: VERSION,
    generationStatus: generation.status,
    status: reference
      ? "REFERENCE_INSPECTION"
      : feasible.length
        ? "BOUNDED_CANDIDATE"
        : candidates.length
          ? candidates.every((c) => c.facts.status === "NUMERICAL_FAILURE")
            ? "NUMERICAL_FAILURE"
            : "SEARCH_BUDGET_EXHAUSTED"
          : generation.status,
    reason: generation.reason,
    candidates,
    selected: feasible[0]?.output.id ?? candidates[0]?.output.id ?? null,
    game: "NOT_RUN",
  };
}
