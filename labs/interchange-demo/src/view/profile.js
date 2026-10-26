export function profileMarkup(profile, labels) {
  if (!profile) return `<p>${labels.notRun}</p>`;
  const {
    length,
    coefficients: [a, b, c, d],
    minZ,
    maxZ,
    peakGrade,
  } = profile;
  const low = minZ.z - 2,
    range = Math.max(4, maxZ.z - minZ.z + 4),
    xp = (s) => 45 + (s / length) * 510,
    yp = (z) => 180 - ((z - low) / range) * 150;
  const samples = Array.from({ length: 129 }, (_, i) => {
    const t = i / 128;
    return `${xp(t * length)},${yp(((a * t + b) * t + c) * t + d)}`;
  }).join(" ");
  return `<svg id="profile-chart" viewBox="0 0 600 220" role="img" aria-label="${labels.title}"><path d="M45 20V180H560" fill="none" stroke="#9eb5c6"/><polyline points="${samples}" fill="none" stroke="#edc06e" stroke-width="3"/><circle cx="${xp(peakGrade.s)}" cy="${yp(peakGrade.z)}" r="5" fill="#ff8972"/><text x="45" y="205" fill="white">0 m</text><text x="480" y="205" fill="white">${length.toFixed(1)} m</text><text x="10" y="18" fill="white">z (m)</text></svg><p>${labels.peak}: ${(100 * peakGrade.grade).toFixed(2)}% · s=${peakGrade.s.toFixed(2)} m; z=${peakGrade.z.toFixed(2)} m</p><p>z min/max: ${minZ.z.toFixed(2)} / ${maxZ.z.toFixed(2)} m · ${labels.scale}</p>`;
}
