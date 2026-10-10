using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800: how the slugs will move round the island (動線). On the built terrain (a 1.9 m grid), it finds
    /// the quickest crawl between every pair of the places people go (the camps, the summit, the canyon, the crater...),
    /// never up or down ground steeper than 50° and not into the sea, rivers slower to cross. Out: the crawl times, and
    /// a map of where the quickest ways bunch up (the routes everyone will use, and the chokepoints), with the ground
    /// too steep to climb in red.
    /// </summary>
    public static partial class IslandTest800Builder
    {
        static readonly (string name, Vector2 svg)[] FlowPlaces =
        {
            ("東のキャンプ", new Vector2(500, 300)), ("塩田のキャンプ", new Vector2(250, 318)), ("山頂", new Vector2(375, 262)),
            ("たまり池", new Vector2(368, 314)), ("谷の奥", new Vector2(326, 257)), ("台地の上", new Vector2(292, 222)),
            ("火口の中", new Vector2(350, 165)), ("入江", new Vector2(442.5f, 153.6f)), ("黒い砂の浜", new Vector2(438, 130)),
            ("大ガジュマル", new Vector2(500, 228)), ("菌輪", new Vector2(346, 372)), ("塩田の大岩", new Vector2(214, 332)),
            ("砂漠の西", new Vector2(172, 256)), ("溶岩のふちの泉", new Vector2(452, 205)),
        };
        const int FlowN = 513;   // every other height sample: 1.875 m

        [MenuItem("Slug/IslandTest 800 m/Measure Flow")]
        public static string MeasureFlow() => MeasureFlow("flow");

        public static string MeasureFlow(string tag)
        {
            var t = OpenTerrain();
            if (!t) return "no terrain";
            var data = t.terrainData;
            int res = data.heightmapResolution, step = (res - 1) / (FlowN - 1);
            float cell = data.size.x / (FlowN - 1), y0 = t.transform.position.y;
            var raw = data.GetHeights(0, 0, res, res);
            var h = new float[FlowN * FlowN];
            for (int j = 0; j < FlowN; j++) for (int i = 0; i < FlowN; i++) h[j * FlowN + i] = raw[j * step, i * step] * data.size.y + y0;
            // water: the rivers' channels (slower: wading or being carried)
            var wet = new bool[FlowN * FlowN];
            for (int j = 0; j < FlowN; j++)
                for (int i = 0; i < FlowN; i++)
                {
                    float x = -Size * 0.5f + i * cell, z = -Size * 0.5f + j * cell;
                    wet[j * FlowN + i] = _rivers != null && RiverBankDistance(x, z) < 0f;
                }
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1, 2, 1, -1, -2, -2, -1, 1, 2 };
            int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1, 1, 2, 2, 1, -1, -2, -2, -1 };
            float tan50 = Mathf.Tan(50f * Mathf.Deg2Rad);
            var steep = new bool[FlowN * FlowN];
            for (int j = 1; j < FlowN - 1; j++)
                for (int i = 1; i < FlowN - 1; i++)
                {
                    int n = j * FlowN + i;
                    for (int d = 0; d < 8 && !steep[n]; d++)
                    {
                        float dist = Mathf.Sqrt(dx[d] * dx[d] + dy[d] * dy[d]) * cell;
                        if (Mathf.Abs(h[(j + dy[d]) * FlowN + i + dx[d]] - h[n]) / dist > tan50) steep[n] = true;
                    }
                }

            int P = FlowPlaces.Length;
            var dists = new float[P][];
            var prevs = new int[P][];
            int Node(Vector2 svg) { var w = W(svg); int i = Mathf.Clamp(Mathf.RoundToInt((w.x + Size * 0.5f) / cell), 0, FlowN - 1), j = Mathf.Clamp(Mathf.RoundToInt((w.y + Size * 0.5f) / cell), 0, FlowN - 1); return j * FlowN + i; }
            var nodes = new int[P];
            for (int p = 0; p < P; p++)
            {
                // a place on a steep bit (the edge of a flow, a pit) counts from the nearest ground that is not
                int n0 = Node(FlowPlaces[p].svg), best = n0;
                float bd = float.MaxValue;
                for (int b = -6; b <= 6; b++)
                    for (int a = -6; a <= 6; a++)
                    {
                        int n = n0 + b * FlowN + a;
                        if (n < 0 || n >= h.Length || steep[n] || h[n] < 0.3f) continue;
                        float dd = a * a + b * b;
                        if (dd < bd) { bd = dd; best = n; }
                    }
                nodes[p] = best;
            }
            for (int p = 0; p < P; p++)
            {
                var g = new float[FlowN * FlowN];
                var prev = new int[FlowN * FlowN];
                for (int k = 0; k < g.Length; k++) { g[k] = float.MaxValue; prev[k] = -1; }
                var heap = new Heap();
                g[nodes[p]] = 0f; heap.Push(nodes[p], 0f);
                var done = new bool[g.Length];
                while (heap.Count > 0)
                {
                    int c = heap.Pop();
                    if (done[c]) continue;
                    done[c] = true;
                    int ci = c % FlowN, cj = c / FlowN;
                    for (int d = 0; d < 16; d++)
                    {
                        int ni = ci + dx[d], nj = cj + dy[d];
                        if (ni < 1 || nj < 1 || ni >= FlowN - 1 || nj >= FlowN - 1) continue;
                        int m = nj * FlowN + ni;
                        if (done[m] || h[m] < 0.2f) continue;
                        float flat = Mathf.Sqrt(dx[d] * dx[d] + dy[d] * dy[d]) * cell, rise = h[m] - h[c];
                        if (Mathf.Abs(rise) / flat > tan50) continue;
                        if (d >= 8)
                        {
                            // a long step: the ground halfway must not be too steep either way (the same both directions)
                            float hm = 0.5f * (h[(cj + Mathf.FloorToInt(dy[d] * 0.5f)) * FlowN + ci + Mathf.FloorToInt(dx[d] * 0.5f)] + h[(cj + Mathf.CeilToInt(dy[d] * 0.5f)) * FlowN + ci + Mathf.CeilToInt(dx[d] * 0.5f)]);
                            if (Mathf.Abs(hm - h[c]) / (flat * 0.5f) > tan50 || Mathf.Abs(h[m] - hm) / (flat * 0.5f) > tan50) continue;
                        }
                        float cost = Mathf.Sqrt(flat * flat + rise * rise) / 1.2f * (wet[m] ? 2f : 1f);
                        float ng = g[c] + cost;
                        if (ng < g[m]) { g[m] = ng; prev[m] = c; heap.Push(m, ng); }
                    }
                }
                dists[p] = g; prevs[p] = prev;
            }

            // the quickest ways between every pair, laid on top of each other
            var flow = new float[FlowN * FlowN];
            var sb = new StringBuilder();
            sb.Append("| 軽くて（分） |");
            for (int q = 0; q < P; q++) sb.Append($" {FlowPlaces[q].name} |");
            sb.Append("\n| --- |");
            for (int q = 0; q < P; q++) sb.Append(" --- |");
            sb.AppendLine();
            for (int p = 0; p < P; p++)
            {
                sb.Append($"| {FlowPlaces[p].name} |");
                for (int q = 0; q < P; q++)
                {
                    float s = dists[p][nodes[q]];
                    sb.Append(q == p ? " — |" : s == float.MaxValue ? " 行けない |" : $" {s / 60f:0.0} |");
                    if (q > p && s < float.MaxValue)
                        for (int k = nodes[q]; k >= 0; k = prevs[p][k]) flow[k] += 1f;
                }
                sb.AppendLine();
            }
            // spread the lines a little (they are one cell wide)
            var blur = new float[flow.Length];
            for (int j = 2; j < FlowN - 2; j++)
                for (int i = 2; i < FlowN - 2; i++)
                {
                    float s = 0f;
                    for (int b = -2; b <= 2; b++) for (int a = -2; a <= 2; a++) s = Mathf.Max(s, flow[(j + b) * FlowN + i + a] * (1f - 0.18f * (Mathf.Abs(a) + Mathf.Abs(b))));
                    blur[j * FlowN + i] = s;
                }

            // ---- the picture: hillshade, sea, steep ground red, the ways from thin blue to thick yellow
            var tex = new Texture2D(FlowN, FlowN, TextureFormat.RGB24, false);
            float maxFlow = 1f;
            foreach (var f in blur) maxFlow = Mathf.Max(maxFlow, f);
            for (int j = 0; j < FlowN; j++)
                for (int i = 0; i < FlowN; i++)
                {
                    int n = j * FlowN + i;
                    Color c;
                    if (h[n] < 0.2f) c = new Color(0.55f, 0.75f, 0.8f);
                    else
                    {
                        float gx = (h[n + (i < FlowN - 1 ? 1 : 0)] - h[n - (i > 0 ? 1 : 0)]) / (2f * cell), gz = (h[n + (j < FlowN - 1 ? FlowN : 0)] - h[n - (j > 0 ? FlowN : 0)]) / (2f * cell);
                        var nrm = new Vector3(-gx, 1f, -gz).normalized;
                        float sh = Mathf.Clamp01(0.45f + 0.6f * Vector3.Dot(nrm, new Vector3(-0.5f, 0.75f, 0.45f).normalized));
                        float tone = Mathf.Lerp(0.55f, 0.9f, Mathf.Clamp01(h[n] / 110f));
                        c = new Color(tone, tone, tone * 0.95f) * sh;
                        if (wet[n]) c = Color.Lerp(c, new Color(0.25f, 0.5f, 0.85f), 0.7f);
                        if (steep[n]) c = Color.Lerp(c, new Color(0.85f, 0.15f, 0.12f), 0.65f);
                    }
                    float fl = blur[n] / maxFlow;
                    if (blur[n] > 0f)
                    {
                        var fc = fl < 0.25f ? Color.Lerp(new Color(0.2f, 0.45f, 1f), new Color(0.1f, 0.8f, 0.6f), fl / 0.25f) : Color.Lerp(new Color(0.1f, 0.8f, 0.6f), new Color(1f, 0.85f, 0.1f), (fl - 0.25f) / 0.75f);
                        c = Color.Lerp(c, fc, 0.55f + 0.45f * Mathf.Sqrt(fl));
                    }
                    tex.SetPixel(i, j, c);
                }
            tex.Apply();
            // crop to the island and flip so north is up in the SVG
            int ci0 = Mathf.RoundToInt((W(new Vector2(130, 0)).x + Size * 0.5f) / cell), ci1 = Mathf.RoundToInt((W(new Vector2(612, 0)).x + Size * 0.5f) / cell);
            int cj1 = Mathf.RoundToInt((W(new Vector2(0, 100)).y + Size * 0.5f) / cell), cj0 = Mathf.RoundToInt((W(new Vector2(0, 425)).y + Size * 0.5f) / cell);
            int cw = ci1 - ci0, chh = cj1 - cj0;
            var crop = new Texture2D(cw, chh, TextureFormat.RGB24, false);
            crop.SetPixels(tex.GetPixels(ci0, cj0, cw, chh));
            crop.Apply();
            byte[] png = crop.EncodeToPNG();
            Directory.CreateDirectory(ShotDir);
            File.WriteAllBytes($"{ShotDir}/{tag}_map.png", png);
            int scale = 3;
            var svg = new StringBuilder();
            svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{cw * scale}\" height=\"{chh * scale + 40}\" viewBox=\"0 0 {cw * scale} {chh * scale + 40}\" font-family=\"sans-serif\">");
            svg.Append($"<image width=\"{cw * scale}\" height=\"{chh * scale}\" href=\"data:image/png;base64,{Convert.ToBase64String(png)}\"/>");
            for (int p = 0; p < P; p++)
            {
                int n = nodes[p];
                float px = (n % FlowN - ci0) * scale, py = (chh - (n / FlowN - cj0)) * scale;
                svg.Append($"<circle cx=\"{px}\" cy=\"{py}\" r=\"6\" fill=\"#fff\" stroke=\"#000\" stroke-width=\"2\"/>");
                svg.Append($"<text x=\"{px + 9}\" y=\"{py + 5}\" font-size=\"16\" font-weight=\"bold\" fill=\"#000\" stroke=\"#fff\" stroke-width=\"3\" paint-order=\"stroke\">{FlowPlaces[p].name}</text>");
            }
            svg.Append($"<text x=\"10\" y=\"{chh * scale + 28}\" font-size=\"16\" fill=\"#222\">赤：50° より急（登れない） 青い線：川 道筋：細い青 → 太い黄（たくさんの最短の道が重なる所） 上が北</text></svg>");
            File.WriteAllText($"{ShotDir}/{tag}_map.svg", svg.ToString());
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(crop);
            // where the ways bunch up most: the strongest spots, at least 25 m apart (svg units, so they can be named)
            var picked = new List<int>();
            var order = new List<int>();
            for (int n = 0; n < blur.Length; n++) if (blur[n] > maxFlow * 0.3f) order.Add(n);
            order.Sort((a, b) => blur[b].CompareTo(blur[a]));
            foreach (int n in order)
            {
                bool far = true;
                foreach (int q in nodes) if (Mathf.Pow((n % FlowN - q % FlowN) * cell, 2) + Mathf.Pow((n / FlowN - q / FlowN) * cell, 2) < 30f * 30f) { far = false; break; }   // not the places themselves
                foreach (int q in picked) if (Mathf.Pow((n % FlowN - q % FlowN) * cell, 2) + Mathf.Pow((n / FlowN - q / FlowN) * cell, 2) < 25f * 25f) { far = false; break; }
                if (!far) continue;
                picked.Add(n);
                if (picked.Count >= 14) break;
            }
            sb.AppendLine();
            sb.AppendLine("道筋が重なる所（多い順。地図の座標と、何本の最短の道が通るか）：");
            foreach (int n in picked)
            {
                var sv = Svg(-Size * 0.5f + (n % FlowN) * cell, -Size * 0.5f + (n / FlowN) * cell);
                sb.AppendLine($"- ({sv.x:0},{sv.y:0}) {blur[n]:0} 本、高さ {h[n]:0} m");
            }
            int land = 0, st = 0;
            for (int n = 0; n < h.Length; n++) if (h[n] >= 0.3f) { land++; if (steep[n]) st++; }
            sb.AppendLine();
            sb.AppendLine($"50° より急な地面：陸の {100f * st / land:0.0}%");
            return sb.ToString();
        }
    }
}
