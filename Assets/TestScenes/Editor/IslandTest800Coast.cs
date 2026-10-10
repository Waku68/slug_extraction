using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800: can the island be crawled right round by its coast? Points every 6° round the island, 4 m in
    /// from the water's edge; the quickest crawl from each to the next (the same rules as Measure Flow: nothing steeper
    /// than 50°, not into the sea, rivers twice as slow). Out: the length and time of the round, and the legs that
    /// cannot be crawled or have to go far inland.
    /// </summary>
    public static partial class IslandTest800Builder
    {
        [MenuItem("Slug/IslandTest 800 m/Measure Coast Round")]
        public static string MeasureCoast()
        {
            var t = OpenTerrain();
            if (!t) return "no terrain";
            var data = t.terrainData;
            int res = data.heightmapResolution, step = (res - 1) / (FlowN - 1);
            float cell = data.size.x / (FlowN - 1), y0 = t.transform.position.y;
            var raw = data.GetHeights(0, 0, res, res);
            var h = new float[FlowN * FlowN];
            for (int j = 0; j < FlowN; j++) for (int i = 0; i < FlowN; i++) h[j * FlowN + i] = raw[j * step, i * step] * data.size.y + y0;
            float tan50 = Mathf.Tan(50f * Mathf.Deg2Rad);
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1, 2, 1, -1, -2, -2, -1, 1, 2 };
            int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1, 1, 2, 2, 1, -1, -2, -2, -1 };
            int Node(Vector2 w) => Mathf.Clamp(Mathf.RoundToInt((w.y + Size * 0.5f) / cell), 0, FlowN - 1) * FlowN + Mathf.Clamp(Mathf.RoundToInt((w.x + Size * 0.5f) / cell), 0, FlowN - 1);
            // the points round the coast
            var pts = new System.Collections.Generic.List<Vector2>();
            Vector2 c = W(new Vector2(366, 262));
            for (int a = 0; a < 360; a += 6)
            {
                Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                float l = 0f;
                for (float r = 50f; r < 470f; r += 1f) if (h[Node(c + dir * r)] > 0.3f) l = r;
                pts.Add(c + dir * Mathf.Max(0f, l - 4f));
            }
            var sb = new StringBuilder();
            float total = 0f, totalLen = 0f;
            int blocked = 0;
            for (int k = 0; k < pts.Count; k++)
            {
                int s = Node(pts[k]), e = Node(pts[(k + 1) % pts.Count]);
                var g = new System.Collections.Generic.Dictionary<int, float> { [s] = 0f };
                var heap = new Heap();
                heap.Push(s, 0f);
                var done = new System.Collections.Generic.HashSet<int>();
                float found = -1f;
                while (heap.Count > 0)
                {
                    int cn = heap.Pop();
                    if (!done.Add(cn)) continue;
                    if (cn == e) { found = g[cn]; break; }
                    if (g[cn] > 600f) break;
                    int ci = cn % FlowN, cj = cn / FlowN;
                    for (int d = 0; d < 16; d++)
                    {
                        int ni = ci + dx[d], nj = cj + dy[d];
                        if (ni < 1 || nj < 1 || ni >= FlowN - 1 || nj >= FlowN - 1) continue;
                        int m = nj * FlowN + ni;
                        if (h[m] < 0.2f || done.Contains(m)) continue;
                        float flat = Mathf.Sqrt(dx[d] * dx[d] + dy[d] * dy[d]) * cell, rise = h[m] - h[cn];
                        if (Mathf.Abs(rise) / flat > tan50) continue;
                        if (d >= 8)
                        {
                            float hm2 = 0.5f * (h[(cj + Mathf.FloorToInt(dy[d] * 0.5f)) * FlowN + ci + Mathf.FloorToInt(dx[d] * 0.5f)] + h[(cj + Mathf.CeilToInt(dy[d] * 0.5f)) * FlowN + ci + Mathf.CeilToInt(dx[d] * 0.5f)]);
                            if (Mathf.Abs(hm2 - h[cn]) / (flat * 0.5f) > tan50 || Mathf.Abs(h[m] - hm2) / (flat * 0.5f) > tan50) continue;
                        }
                        float wet = _rivers != null && RiverBankDistance(-Size * 0.5f + ni * cell, -Size * 0.5f + nj * cell) < 0f ? 2f : 1f;
                        float ng = g[cn] + Mathf.Sqrt(flat * flat + rise * rise) / 1.2f * wet;
                        if (!g.TryGetValue(m, out float old) || ng < old) { g[m] = ng; heap.Push(m, ng); }
                    }
                }
                float straight = (pts[(k + 1) % pts.Count] - pts[k]).magnitude;
                var sv = Svg(pts[k].x, pts[k].y);
                if (found < 0f) { blocked++; sb.AppendLine($"- 通れない：({sv.x:0},{sv.y:0}) から次へ"); continue; }
                total += found; totalLen += straight;
                if (found * 1.2f > straight * 2.2f) sb.AppendLine($"- 遠回り：({sv.x:0},{sv.y:0}) から次へ、まっすぐ {straight:0} m に {found / 60f:0.0} 分");
            }
            return $"海沿いをひと回り：まっすぐつなぐと {totalLen:0} m、軽くて {total / 60f:0.0} 分、重いと {total / 60f * 3.33f:0} 分。通れない区間 {blocked}\n" + sb;
        }
    }
}
