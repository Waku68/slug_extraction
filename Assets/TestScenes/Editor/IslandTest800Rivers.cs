using System.Collections.Generic;
using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800, part 4: the two rivers, shaped like real streams (Oct 10). Each follows its line on the map,
    /// meanders where the ground is flat (hardly at all on the mountain), and grows wider and deeper downstream. Its
    /// bed only ever goes down: where the ground rises in the way the river has cut a little gorge, where the ground
    /// drops it falls (rapids, the waterfall under the pool). Across: a channel deepest toward the outside of a bend,
    /// a steep cut bank on the outside, a gravel bar on the inside, a floodplain where the land is flat, and valley
    /// sides back up to the ground. The water is one ribbon that follows the bed, rocks stand in it where it runs
    /// steep, the east river widens into a shallow ford in the forest and an estuary at the sea, and the south river
    /// spreads thin and dies out on the salt flat.
    /// </summary>
    public static partial class IslandTest800Builder
    {
        class River
        {
            public string name;
            public List<Vector2> p = new List<Vector2>();       // the centreline (world x, z), a point every 2 m
            public List<Vector2> n = new List<Vector2>();       // the left normal
            public float[] bed, half, bank, water, bend, flat;   // bed height, channel half width, bank height, water depth, signed bend, flatness 0-1
            public float[] gorge;                                 // 1 where the river runs in the gorge below a waterfall (steep walls)
            public int fall = -1;                                 // the point where it falls (IslandTest800Falls.cs)
            public Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
        }

        static List<River> _rivers;
        static readonly Vector2 FordSvg = new Vector2(522f, 266f);       // the ford in the forest (浅瀬)
        const float RiverGrid = 16f, ValleyReach = 40f;

        static float GroundNoRiver(float x, float z)
        {
            var s = Svg(x, z);
            return Base(s.x, s.y, null, false) + (_erosion != null ? ErosionAt(s.x, s.y) : 0f);
        }

        static List<Vector2> Resample(List<Vector2> p, float step)
        {
            var r = new List<Vector2> { p[0] };
            float carry = 0f;
            for (int i = 0; i + 1 < p.Count; i++)
            {
                float len = (p[i + 1] - p[i]).magnitude;
                for (float s = step - carry; s < len; s += step) r.Add(Vector2.Lerp(p[i], p[i + 1], s / len));
                carry = (carry + len) % step;
            }
            r.Add(p[p.Count - 1]);
            return r;
        }

        static List<Vector2> Chaikin(List<Vector2> p, int times)
        {
            for (int it = 0; it < times; it++)
            {
                var q = new List<Vector2> { p[0] };
                for (int i = 0; i + 1 < p.Count; i++) { q.Add(Vector2.Lerp(p[i], p[i + 1], 0.25f)); q.Add(Vector2.Lerp(p[i], p[i + 1], 0.75f)); }
                q.Add(p[p.Count - 1]);
                p = q;
            }
            return p;
        }

        static void Normals(River r)
        {
            r.n.Clear();
            for (int i = 0; i < r.p.Count; i++)
            {
                Vector2 t = (r.p[Mathf.Min(r.p.Count - 1, i + 1)] - r.p[Mathf.Max(0, i - 1)]).normalized;
                r.n.Add(new Vector2(-t.y, t.x));
            }
        }

        /// <summary>Works out both rivers' lines, beds and widths on the eroded ground (before they are cut in).</summary>
        static void PrepareRivers()
        {
            _rivers = new List<River>();
            foreach (var (name, line, wide, deep, lambda, phase, end) in new[]
            {
                ("東の川", _east, 4.5f, 1.3f, 56f, 0.7f, "sea"),
                ("南の川", _south, 3.5f, 1.0f, 46f, 2.1f, "salt"),
            })
            {
                var r = new River { name = name };
                var p0 = new List<Vector2>();
                foreach (var s in line) p0.Add(W(s));
                var p = Resample(Chaikin(p0, 2), 2f);
                // how flat the ground is along it decides how much it meanders
                var g = new float[p.Count];
                for (int i = 0; i < p.Count; i++) g[i] = GroundNoRiver(p[i].x, p[i].y);
                r.p = p; Normals(r);
                float total = (p.Count - 1) * 2f;
                var moved = new List<Vector2>();
                for (int i = 0; i < p.Count; i++)
                {
                    float slope = Mathf.Abs(g[Mathf.Min(p.Count - 1, i + 6)] - g[Mathf.Max(0, i - 6)]) / 24f;
                    float frac = i * 2f / total, amp = 7f * (1f - Smooth(0.02f, 0.08f, slope)) * Smooth(0f, 0.12f, frac) * (1f - Smooth(0.9f, 1f, frac));
                    float wob = Mathf.Sin(i * 2f / lambda * Mathf.PI * 2f + phase) + 0.35f * Mathf.Sin(i * 2f / (lambda * 0.43f) * Mathf.PI * 2f + phase * 2.3f);
                    moved.Add(p[i] + r.n[i] * amp * wob);
                }
                r.p = Resample(Chaikin(moved, 1), 2f);
                Normals(r);
                int m = r.p.Count;
                total = (m - 1) * 2f;
                r.bed = new float[m]; r.half = new float[m]; r.bank = new float[m]; r.water = new float[m]; r.bend = new float[m]; r.flat = new float[m];
                var ground = new float[m];
                for (int i = 0; i < m; i++) ground[i] = GroundNoRiver(r.p[i].x, r.p[i].y);
                Vector2 ford = W(FordSvg);
                for (int i = 0; i < m; i++)
                {
                    float frac = i * 2f / total;
                    float slope = Mathf.Abs(ground[Mathf.Min(m - 1, i + 6)] - ground[Mathf.Max(0, i - 6)]) / 24f;
                    r.flat[i] = 1f - Smooth(0.02f, 0.1f, slope);
                    r.half[i] = Mathf.Lerp(1f, wide, Mathf.Pow(frac, 0.8f)) * Mathf.Lerp(0.8f, 1f, r.flat[i]);
                    r.bank[i] = Mathf.Lerp(0.5f, deep, frac);
                    r.water[i] = Mathf.Lerp(0.25f, 0.6f, frac);
                    if (name == "東の川")
                    {
                        float f = 1f - Smooth(10f, 22f, (r.p[i] - ford).magnitude);     // the ford: wide and shallow
                        r.half[i] *= 1f + 0.8f * f; r.water[i] = Mathf.Lerp(r.water[i], 0.15f, f); r.bank[i] = Mathf.Lerp(r.bank[i], 0.4f, f);
                    }
                    if (end == "sea") r.half[i] *= 1f + 0.9f * Smooth(0.88f, 1f, frac);                     // the estuary
                    else { float e = Smooth(0.85f, 1f, frac); r.half[i] *= 1f + 2f * e; r.water[i] = Mathf.Lerp(r.water[i], 0.08f, e); r.bank[i] = Mathf.Lerp(r.bank[i], 0.3f, e); }   // dies out on the salt flat
                }
                // the bed only goes down: cut through rises, drop with falls
                r.bed[0] = ground[0] - r.bank[0];
                for (int i = 1; i < m; i++) r.bed[i] = Mathf.Min(r.bed[i - 1] - 0.004f, ground[i] - r.bank[i]);
                if (end == "salt") MakeFall(r, ground);           // the waterfall under the pool
                // the bends: signed turn per metre, smoothed
                var raw = new float[m];
                for (int i = 2; i + 2 < m; i++)
                {
                    Vector2 a = (r.p[i] - r.p[i - 2]).normalized, b = (r.p[i + 2] - r.p[i]).normalized;
                    raw[i] = (a.x * b.y - a.y * b.x) / 4f;
                }
                for (int i = 0; i < m; i++)
                {
                    float s = 0f; int c = 0;
                    for (int k = Mathf.Max(0, i - 4); k <= Mathf.Min(m - 1, i + 4); k++) { s += raw[k]; c++; }
                    r.bend[i] = s / c;
                }
                // a grid of the segments, for finding the nearest quickly
                for (int i = 0; i + 1 < m; i++)
                {
                    float reach = r.half[i] + ValleyReach;
                    Vector2 lo = Vector2.Min(r.p[i], r.p[i + 1]) - Vector2.one * reach, hi = Vector2.Max(r.p[i], r.p[i + 1]) + Vector2.one * reach;
                    for (int cx = Mathf.FloorToInt(lo.x / RiverGrid); cx <= Mathf.FloorToInt(hi.x / RiverGrid); cx++)
                        for (int cz = Mathf.FloorToInt(lo.y / RiverGrid); cz <= Mathf.FloorToInt(hi.y / RiverGrid); cz++)
                        {
                            long key = ((long)cx << 32) ^ (uint)cz;
                            if (!r.grid.TryGetValue(key, out var list)) r.grid[key] = list = new List<int>();
                            list.Add(i);
                        }
                }
                _rivers.Add(r);
            }
        }

        /// <summary>The nearest point of a river: distance (m), which segment and how far along it, which side (+ left).</summary>
        static bool Nearest(River r, float x, float z, out float d, out int seg, out float t, out float side)
        {
            d = float.MaxValue; seg = -1; t = 0f; side = 1f;
            long key = ((long)Mathf.FloorToInt(x / RiverGrid) << 32) ^ (uint)Mathf.FloorToInt(z / RiverGrid);
            if (!r.grid.TryGetValue(key, out var list)) return false;
            foreach (int i in list)
            {
                Vector2 a = r.p[i], e = r.p[i + 1] - a;
                float tt = Mathf.Clamp01(((x - a.x) * e.x + (z - a.y) * e.y) / e.sqrMagnitude);
                float dx = x - a.x - e.x * tt, dz = z - a.y - e.y * tt, dd = dx * dx + dz * dz;
                if (dd < d) { d = dd; seg = i; t = tt; side = e.x * dz - e.y * dx >= 0f ? 1f : -1f; }
            }
            d = Mathf.Sqrt(d);
            return seg >= 0;
        }

        /// <summary>The ground with the rivers cut in. wet: the channel (0-1); bar: the gravel of the inside banks (0-1).
        /// (Where a way crosses, the way is cut in after and runs over the river on its bank, the water passing under it.)</summary>
        static float Rivers(float x, float z, float h, out float wet, out float bar)
        {
            wet = 0f; bar = 0f;
            if (_rivers == null) return h;
            foreach (var r in _rivers)
            {
                if (!Nearest(r, x, z, out float d, out int i, out float t, out float side)) continue;
                float L(float[] a) => Mathf.Lerp(a[i], a[i + 1], t);
                float bed = L(r.bed), half = L(r.half), bank = L(r.bank), bend = L(r.bend), flat = L(r.flat);
                if (d > half + ValleyReach) continue;
                // is this the outside of the bend (cut bank, deep) or the inside (gravel bar)?
                float turn = Mathf.Clamp01(Mathf.Abs(bend) * 25f);
                bool outside = side * bend < 0f;
                float u = d / Mathf.Max(0.5f, half), x0 = d - half, target;
                float barW = 1.5f + (outside ? 0f : 6f * turn) + 2f * flat, cutW = outside ? Mathf.Lerp(1.5f, 0.6f, turn) : barW;
                if (x0 <= 0f)
                {
                    float scour = (outside ? 0.5f * u : -0.25f * u) * turn;              // pools at the outside of bends
                    target = bed - 0.35f * (1f - u * u) - scour + 0.12f * Fbm(x * 0.15f, z * 0.15f, 2, 101);
                    wet = Mathf.Max(wet, 1f - Smooth(0.75f, 1.05f, u));
                }
                else if (x0 < cutW)
                {
                    float k = x0 / cutW;
                    target = bed + bank * (outside ? Mathf.Sqrt(k) : k * k);
                    if (!outside) bar = Mathf.Max(bar, (1f - k) * Smooth(0.1f, 0.4f, turn + flat * 0.3f));
                    wet = Mathf.Max(wet, 1f - Smooth(0f, 0.6f, x0));
                }
                else
                {
                    // the floodplain where the land is flat, then the valley sides (about 29°) back up to the ground
                    // (below a waterfall the walls are steep: the gorge the falls cut as they wore back)
                    float gorge = r.gorge != null ? L(r.gorge) : 0f;
                    float plain = (2f + 12f * flat) * (0.6f + 0.4f * Vn(x * 0.03f, z * 0.03f)) * (1f - gorge), vx = x0 - cutW;
                    target = bed + bank + 0.25f * Fbm(x * 0.05f, z * 0.05f, 2, 103) + Mathf.Max(0f, vx - plain) * Mathf.Lerp(0.55f, 2.6f, gorge);
                }
                if (target < h) h = target;
            }
            return h;
        }

        /// <summary>How far a point is from the nearest river's bank (m; below zero in the water).</summary>
        static float RiverBankDistance(float x, float z)
        {
            float best = float.MaxValue;
            if (_rivers == null) return best;
            foreach (var r in _rivers)
                if (Nearest(r, x, z, out float d, out int i, out float t, out _))
                    best = Mathf.Min(best, d - Mathf.Lerp(r.half[i], r.half[i + 1], t));
            return best;
        }

        /// <summary>The water: one ribbon a river, its surface following the bed, the moisture zones and rocks in the fast parts.</summary>
        static void BuildRiverWater(Transform props, Material water, Terrain terrain)
        {
            EnsureRocks();
            var rockMat = Mat("IT_Stone", new Color(0.62f, 0.5f, 0.4f));
            var rnd = new System.Random(801);
            foreach (var r in _rivers)
            {
                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                var rocks = new GameObject(r.name + "の岩").transform;
                rocks.SetParent(props, false);
                float lastLevel = float.MaxValue;
                bool prevIn = false;
                for (int i = 0; i < r.p.Count; i++)
                {
                    // the falls and their pool have their own water (IslandTest800Falls.cs)
                    if (r.fall >= 0 && i > r.fall && (r.p[i] - _fallC).magnitude < FallR + 1f) { prevIn = false; continue; }
                    float level = Mathf.Min(lastLevel, r.bed[i] + r.water[i]);
                    lastLevel = level;
                    if (level < 0.03f) { level = 0.03f; }
                    float centre = terrain.SampleHeight(new Vector3(r.p[i].x, 0f, r.p[i].y)) + terrain.transform.position.y;
                    if (centre < -0.6f && i > 0) break;                                           // out into the sea
                    float w = r.half[i] + 1.2f * (r.water[i] / Mathf.Max(0.2f, r.bank[i]));       // the water reaches a little up the banks
                    w = Mathf.Min(w, 0.8f / Mathf.Max(0.002f, Mathf.Abs(r.bend[i])));               // never wider than the bend can take (the ribbon would fold)
                    Vector2 a = r.p[i] + r.n[i] * w, b = r.p[i] - r.n[i] * w;
                    verts.Add(new Vector3(a.x, level, a.y)); verts.Add(new Vector3(b.x, level, b.y));
                    uvs.Add(new Vector2(0f, i * 0.1f)); uvs.Add(new Vector2(1f, i * 0.1f));
                    if (prevIn) { int k = verts.Count - 4; tris.AddRange(new[] { k, k + 1, k + 2, k + 1, k + 3, k + 2 }); }
                    prevIn = true;
                    if (i % 5 == 0) WeatherSceneSetup.Zone(props, "River_Zone", MoistureZone.Kind.Water, new Vector3(r.p[i].x, level, r.p[i].y), new Vector3(w * 2f + 3f, 4f, w * 2f + 3f));
                    // rocks where it runs fast (steep), near the banks and some mid-stream
                    if (i > 2 && i % 4 == 0 && (r.fall < 0 || (r.p[i] - _fallC).magnitude > FallR + 4f))
                    {
                        float drop = r.bed[i - 2] - r.bed[Mathf.Min(r.p.Count - 1, i + 2)];
                        if (drop / 8f > 0.05f && rnd.NextDouble() < 0.8)
                        {
                            float off = ((float)rnd.NextDouble() * 2f - 1f) * r.half[i] * 0.9f;
                            Vector2 q = r.p[i] + r.n[i] * off;
                            var at = new Vector3(q.x, terrain.SampleHeight(new Vector3(q.x, 0f, q.y)) + terrain.transform.position.y, q.y);
                            float s = 0.5f + (float)rnd.NextDouble() * 0.8f;
                            Rock(rocks, "川の岩", rockMat, at, new Vector3(1.2f, 0.9f, 1.1f) * s, rnd, 0.3f);
                        }
                    }
                }
                var mesh = new Mesh { name = "IT_River_" + (r.name == "東の川" ? "East" : "South") };
                mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                string path = $"{MoveTestBuilder.GenDir}/{mesh.name}.asset";
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (old) { old.Clear(); EditorUtility.CopySerialized(mesh, old); mesh = old; EditorUtility.SetDirty(old); }
                else AssetDatabase.CreateAsset(mesh, path);
                var go = new GameObject(r.name + "の水");
                go.transform.SetParent(props, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = water;
            }
        }

        /// <summary>Slug's-eye shots from the banks, and from the air (right after Build Scene, which works the rivers out).</summary>
        [MenuItem("Slug/IslandTest 800 m/Shoot Rivers")]
        public static string ShootRivers()
        {
            var t = OpenTerrain();
            if (!t || _rivers == null) return "build the scene first";
            var sb = new System.Text.StringBuilder();
            float G(Vector2 p) => t.SampleHeight(new Vector3(p.x, 0f, p.y)) + t.transform.position.y;
            // (file, which river, how far down it 0-1, which bank, how far ahead to look in points, look up m)
            (string file, int river, float at, float bankSide, int ahead, float up)[] views =
            {
                ("30_east_river_upper", 0, 0.12f, 1f, 18, -2f),
                ("31_east_river_meander", 0, 0.55f, -1f, 22, 0f),
                ("32_east_river_ford", 0, 0.7f, 1f, 14, 0f),
                ("33_east_river_mouth", 0, 0.82f, 1f, 40, 0f),
                ("34_waterfall_from_below", 1, -1f, 1f, -12, 4f),     // (at < 0: below the biggest drop)
                ("35_south_river_meadow", 1, 0.45f, -1f, 25, 0f),
                ("36_south_river_dies", 1, 0.85f, 1f, 30, 0f),
            };
            foreach (var v in views)
            {
                var r = _rivers[v.river];
                int i = Mathf.Clamp(Mathf.RoundToInt(v.at * (r.p.Count - 1)), 0, r.p.Count - 1);
                if (v.at < 0f) { int best = 1; for (int k = 1; k < r.p.Count / 3; k++) if (r.bed[k - 1] - r.bed[k] > r.bed[best - 1] - r.bed[best]) best = k; i = Mathf.Min(r.p.Count - 1, best + 10); }
                int j = Mathf.Clamp(i + v.ahead, 0, r.p.Count - 1);
                Vector2 from = r.p[i] + r.n[i] * v.bankSide * (r.half[i] + 3.5f);
                Vector3 eye = new Vector3(from.x, G(from) + 0.47f, from.y), look = new Vector3(r.p[j].x, G(r.p[j]) + 0.5f + v.up, r.p[j].y);
                sb.AppendLine(ShotUtil.Capture($"{ShotDir}/{v.file}.png", eye, look, 60f, 1200, 600, 3000f));
            }
            foreach (var (file, river, at) in new[] { ("air_east_river", 0, 0.55f), ("air_south_river", 1, 0.4f) })
            {
                var r = _rivers[river];
                var c = r.p[Mathf.RoundToInt(at * (r.p.Count - 1))];
                var target = new Vector3(c.x, G(c), c.y);
                sb.AppendLine(ShotUtil.Capture($"{ShotDir}/{file}.png", target + new Vector3(-70f, 85f, -90f), target, 50f, 1400, 800, 4000f));
            }
            return sb.ToString();
        }

        static void EnsureRocks()
        {
            if (_rocks != null && _rocks.Length == 5 && _rocks[0]) return;
            _rocks = new Mesh[5];
            for (int i = 0; i < 5; i++) _rocks[i] = RockMesh(i);
        }
    }
}
