using System.Collections.Generic;
using System.Text;
using SlugMove;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800, part 2: the ways and the cover (discussions/island_height_discussion.md).
    /// - Roads (the old salt-carriers' roads): found on the ground by a path search that never climbs more than 15°, so
    ///   they zigzag up the mountain by themselves, then cut into the slope as a 3 m bench. Animal trails: up to 40°,
    ///   straighter, a narrow groove. Crawling on them is no faster; they are just gentle and easy to follow.
    /// - Cover, the small triangles: something to hide behind every 15-30 m, by region (trees, logs and rocks in the
    ///   forest, grass clumps in the meadow, rocks and dry shrubs in the desert, rock pillars on the mesa top, salt
    ///   boulders on the mountain, lava boulders, fallen rocks in the canyon, driftwood on the beaches). Nothing on the
    ///   salt flat (the dangerous open crossing) but one big rock, nothing on the ways, the water or too steep ground.
    /// </summary>
    public static partial class IslandTest800Builder
    {
        // ---- 数字
        const float RoadGrade = 15f, TrailGrade = 40f;      // 道と、けものみちの、いちばん急な坂（度）
        const float RoadHalfWidth = 1.6f, TrailHalfWidth = 0.5f;

        struct Way { public string name; public bool road; public float[] svg; }
        static readonly Way[] Ways =
        {
            new Way { name = "塩の道（塩田 → たまり池 → 山頂）", road = true, svg = new float[] { 236,322, 384,318, 375,262 } },
            new Way { name = "東の道（東の森 → 山頂）", road = true, svg = new float[] { 482,300, 375,262 } },
            new Way { name = "北の道（溶岩原 → 鞍部 → 山頂）", road = true, svg = new float[] { 414,176, 359,199, 375,262 } },
            new Way { name = "けものみち（花の草原 → たまり池）", road = false, svg = new float[] { 346,372, 386,322 } },
            new Way { name = "けものみち（雨の森 → 溶岩のふちの泉）", road = false, svg = new float[] { 540,302, 452,206 } },
            new Way { name = "けものみち（砂漠 → 赤い台地の上）", road = false, svg = new float[] { 188,252, 288,224 } },
            new Way { name = "けものみち（赤い台地の上 → 山の西の平場）", road = false, svg = new float[] { 300,272, 352,252 } },
            new Way { name = "けものみち（東の川の浅瀬 → 大ガジュマル）", road = false, svg = new float[] { 522,268, 500,232 } },
        };

        static readonly List<(string name, bool road, List<Vector3> pts)> _ways = new List<(string, bool, List<Vector3>)>();
        static float[] _wayMask;
        static bool[] _wayCore;    // the flat strip of a way already cut: another way's shoulder leaves it alone

        // ------------------------------------------------------------------ the ways
        static float HmAt(float[] hm, float x, float z)
        {
            float cell = Size / (Res - 1f);
            float fi = Mathf.Clamp((x + Size * 0.5f) / cell, 0f, Res - 1.001f), fj = Mathf.Clamp((z + Size * 0.5f) / cell, 0f, Res - 1.001f);
            int i = (int)fi, j = (int)fj, a = j * Res + i;
            float fx = fi - i, fy = fj - j;
            return hm[a] * (1f - fx) * (1f - fy) + hm[a + 1] * fx * (1f - fy) + hm[a + Res] * (1f - fx) * fy + hm[a + Res + 1] * fx * fy;
        }

        /// <summary>Finds and cuts every way into the heights; returns how much each height cell is a way (for the paint).</summary>
        static float[] CarveRoads(float[] hm)
        {
            _ways.Clear();
            var mask = new float[Res * Res];
            _wayCore = new bool[Res * Res];
            // the trails first, so a road crossing one keeps its own bench
            // the roads that went to the summit now end on the shoulder at the foot of the nearest ridge; the ridge
            // trails and the road round the shoulder come from the mountain (IslandTest800Mountain.cs)
            var ways = new List<Way>();
            foreach (var w0 in Ways)
            {
                var w = w0;
                int n = w.svg.Length;
                if (w.road && w.svg[n - 2] == MountainSvg.x && w.svg[n - 1] == MountainSvg.y)
                {
                    var from = new Vector2(w.svg[n - 4], w.svg[n - 3]);
                    float a = Mathf.Atan2(from.y - MountainSvg.y, from.x - MountainSvg.x);
                    int best = 0;
                    for (int k = 1; k < Spurs.Length; k++) if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, SpurAngle(k) * Mathf.Rad2Deg)) < Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, SpurAngle(best) * Mathf.Rad2Deg))) best = k;
                    var end = RidgeFoot(best);
                    w.svg = (float[])w.svg.Clone();
                    w.svg[n - 2] = end.x; w.svg[n - 1] = end.y;
                }
                ways.Add(w);
            }
            ways.AddRange(MountainWays());
            CarveRing(hm, mask);                                      // the road round the shoulder, first of all
            foreach (bool road in new[] { true, false })            // the roads first: their wide cuttings would break a trail crossed after
                foreach (var w in ways)
                {
                    if (w.road != road) continue;
                    // each leg at the gentlest grade it can be found at (a road that cannot keep to 15° gets steeper bits);
                    // a leg with no way at all leaves a gap, and the way goes on from the next point
                    float[] grades = road ? new[] { RoadGrade, 22f, 30f } : new[] { TrailGrade };
                    var flat = new List<Vector2>();
                    float used = grades[0];
                    int piece = 0;
                    void Finish()
                    {
                        if (flat.Count >= 2)
                        {
                            float g = Mathf.Tan(used * Mathf.Deg2Rad);
                            var pts = Profile(hm, Smooth(flat), g * 0.92f, road);
                            Carve(hm, mask, pts, road ? RoadHalfWidth : TrailHalfWidth, road ? 5.5f : 1.2f, road ? 0f : 0.12f);
                            _ways.Add((piece == 0 ? w.name : $"{w.name} {piece + 1}", road, pts));
                            piece++;
                        }
                        flat.Clear(); used = grades[0];
                    }
                    for (int i = 0; i + 3 < w.svg.Length; i += 2)
                    {
                        List<Vector2> leg = null;
                        foreach (float gd in grades)
                        {
                            leg = FindPath(hm, W(w.svg[i], w.svg[i + 1]), W(w.svg[i + 2], w.svg[i + 3]), Mathf.Tan(gd * Mathf.Deg2Rad) * 0.95f, road ? 3f : 1f);
                            if (leg != null) { used = Mathf.Max(used, gd); break; }
                        }
                        if (leg == null) { Debug.LogWarning($"[IslandTest800] no way found: {w.name} (leg {i / 2})"); Finish(); continue; }
                        if (flat.Count > 0) leg.RemoveAt(0);
                        flat.AddRange(leg);
                    }
                    Finish();
                }
            _wayMask = mask;
            return mask;
        }

        /// <summary>A path on a 2.8 m grid of the ground (16 directions) that never climbs or drops more than the grade.</summary>
        static List<Vector2> FindPath(float[] hm, Vector2 from, Vector2 to, float maxGrade, float bend)
        {
            const int St = 3;
            int n = (Res - 1) / St + 1;
            float cell = Size / (Res - 1f) * St;
            int Cell(float v) => Mathf.Clamp(Mathf.RoundToInt((v + Size * 0.5f) / cell), 0, n - 1);
            float H(int i, int j) => hm[(j * St) * Res + i * St];
            int start = Cell(from.y) * n + Cell(from.x), goal = Cell(to.y) * n + Cell(to.x);
            var g = new float[n * n];
            var came = new int[n * n];
            var closed = new bool[n * n];
            for (int k = 0; k < g.Length; k++) { g[k] = float.MaxValue; came[k] = -1; }
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1, 2, 1, -1, -2, -2, -1, 1, 2 };
            int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1, 1, 2, 2, 1, -1, -2, -2, -1 };
            int gx = goal % n, gy = goal / n;
            var heap = new Heap();
            g[start] = 0f;
            heap.Push(start, 0f);
            while (heap.Count > 0)
            {
                int c = heap.Pop();
                if (c == goal) break;
                if (closed[c]) continue;
                closed[c] = true;
                int ci = c % n, cj = c / n;
                float h1 = H(ci, cj);
                for (int d = 0; d < 16; d++)
                {
                    int ni = ci + dx[d], nj = cj + dy[d];
                    if (ni < 0 || nj < 0 || ni >= n || nj >= n) continue;
                    int m = nj * n + ni;
                    if (closed[m]) continue;
                    float h2 = H(ni, nj);
                    if (h2 < 0.4f) continue;                                    // not into the sea
                    float dist = Mathf.Sqrt(dx[d] * dx[d] + dy[d] * dy[d]) * cell, grade = Mathf.Abs(h2 - h1) / dist;
                    if (grade > maxGrade) continue;
                    if (d >= 8)                                                   // a long step: the ground halfway must not be steeper either
                    {
                        float hmid = HmAt(hm, (ci + dx[d] * 0.5f) * cell - Size * 0.5f, (cj + dy[d] * 0.5f) * cell - Size * 0.5f);
                        if (Mathf.Abs(hmid - h1) / (dist * 0.5f) > maxGrade || Mathf.Abs(h2 - hmid) / (dist * 0.5f) > maxGrade) continue;
                    }
                    float r = grade / maxGrade;
                    float ng = g[c] + dist * (1f + bend * r * r);
                    if (ng < g[m])
                    {
                        g[m] = ng; came[m] = c;
                        heap.Push(m, ng + Mathf.Sqrt((ni - gx) * (ni - gx) + (nj - gy) * (nj - gy)) * cell);
                    }
                }
            }
            if (came[goal] < 0 && goal != start) return null;
            var path = new List<Vector2>();
            for (int k = goal; k >= 0; k = came[k]) path.Add(new Vector2((k % n) * cell - Size * 0.5f, (k / n) * cell - Size * 0.5f));
            path.Reverse();
            return path;
        }

        /// <summary>Rounds the grid's corners (Chaikin) and puts a point every 2 m.</summary>
        static List<Vector2> Smooth(List<Vector2> p)
        {
            for (int it = 0; it < 3; it++)
            {
                var q = new List<Vector2> { p[0] };
                for (int i = 0; i + 1 < p.Count; i++) { q.Add(Vector2.Lerp(p[i], p[i + 1], 0.25f)); q.Add(Vector2.Lerp(p[i], p[i + 1], 0.75f)); }
                q.Add(p[p.Count - 1]);
                p = q;
            }
            var r = new List<Vector2> { p[0] };
            float carry = 0f;
            for (int i = 0; i + 1 < p.Count; i++)
            {
                float len = (p[i + 1] - p[i]).magnitude;
                for (float s = 2f - carry; s < len; s += 2f) r.Add(Vector2.Lerp(p[i], p[i + 1], s / len));
                carry = (carry + len) % 2f;
            }
            r.Add(p[p.Count - 1]);
            return r;
        }

        /// <summary>The way's height along it: the ground, evened out, never steeper than the grade.</summary>
        static List<Vector3> Profile(float[] hm, List<Vector2> p, float grade, bool road)
        {
            var h = new float[p.Count];
            for (int i = 0; i < p.Count; i++) h[i] = HmAt(hm, p[i].x, p[i].y);
            int win = road ? 3 : 1;
            var s = new float[p.Count];
            for (int i = 0; i < p.Count; i++)
            {
                float sum = 0f; int c = 0;
                for (int k = Mathf.Max(0, i - win); k <= Mathf.Min(p.Count - 1, i + win); k++) { sum += h[k]; c++; }
                s[i] = sum / c;
            }
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 1; i < p.Count; i++) { float d = (p[i] - p[i - 1]).magnitude * grade; s[i] = Mathf.Clamp(s[i], s[i - 1] - d, s[i - 1] + d); }
                for (int i = p.Count - 2; i >= 0; i--) { float d = (p[i] - p[i + 1]).magnitude * grade; s[i] = Mathf.Clamp(s[i], s[i + 1] - d, s[i + 1] + d); }
            }
            var r = new List<Vector3>();
            for (int i = 0; i < p.Count; i++) r.Add(new Vector3(p[i].x, s[i], p[i].y));
            return r;
        }

        /// <summary>Cuts a way into the heights: flat across, the ground blended back over the shoulder.</summary>
        static void Carve(float[] hm, float[] mask, List<Vector3> pts, float half, float shoulder, float groove)
        {
            float cell = Size / (Res - 1f), reach = half + shoulder;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
            foreach (var p in pts) { min = Vector2.Min(min, new Vector2(p.x, p.z)); max = Vector2.Max(max, new Vector2(p.x, p.z)); }
            int i0 = Mathf.Max(0, Mathf.FloorToInt((min.x - reach + Size * 0.5f) / cell)), i1 = Mathf.Min(Res - 1, Mathf.CeilToInt((max.x + reach + Size * 0.5f) / cell));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((min.y - reach + Size * 0.5f) / cell)), j1 = Mathf.Min(Res - 1, Mathf.CeilToInt((max.y + reach + Size * 0.5f) / cell));
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    float x = i * cell - Size * 0.5f, z = j * cell - Size * 0.5f, best = float.MaxValue, y = 0f;
                    for (int k = 0; k + 1 < pts.Count; k++)
                    {
                        Vector3 a = pts[k], b = pts[k + 1];
                        if (x < Mathf.Min(a.x, b.x) - reach || x > Mathf.Max(a.x, b.x) + reach || z < Mathf.Min(a.z, b.z) - reach || z > Mathf.Max(a.z, b.z) + reach) continue;
                        float ex = b.x - a.x, ez = b.z - a.z, t = Mathf.Clamp01(((x - a.x) * ex + (z - a.z) * ez) / (ex * ex + ez * ez + 1e-6f));
                        float ddx = x - a.x - ex * t, ddz = z - a.z - ez * t, d = ddx * ddx + ddz * ddz;
                        if (d < best) { best = d; y = Mathf.Lerp(a.y, b.y, t); }
                    }
                    if (best == float.MaxValue) continue;
                    float dist = Mathf.Sqrt(best), w = 1f - Smooth(half, reach, dist);
                    if (w <= 0f) continue;
                    int n = j * Res + i;
                    if (dist > half && _wayCore[n]) continue;
                    if (dist <= half) _wayCore[n] = true;
                    hm[n] = Mathf.Lerp(hm[n], y - groove, w);
                    mask[n] = Mathf.Max(mask[n], (1f - Smooth(half * 0.7f, half + 0.6f, dist)) * (groove > 0f ? 0.8f : 1f));
                }
        }

        /// <summary>Keeps the ways in the scene (a hidden line each, for measuring and for finding them in the Hierarchy).</summary>
        static void RecordRoads(Transform map)
        {
            var root = new GameObject("道").transform;
            root.SetParent(map, false);
            foreach (var (name, road, pts) in _ways)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.enabled = false;
                lr.positionCount = pts.Count;
                lr.SetPositions(pts.ToArray());
            }
        }

        class Heap
        {
            readonly List<int> _n = new List<int>();
            readonly List<float> _p = new List<float>();
            public int Count => _n.Count;
            public void Push(int node, float pri)
            {
                _n.Add(node); _p.Add(pri);
                int i = _n.Count - 1;
                while (i > 0) { int up = (i - 1) / 2; if (_p[up] <= _p[i]) break; Swap(i, up); i = up; }
            }
            public int Pop()
            {
                int top = _n[0], last = _n.Count - 1;
                Swap(0, last); _n.RemoveAt(last); _p.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, s = i;
                    if (l < _n.Count && _p[l] < _p[s]) s = l;
                    if (r < _n.Count && _p[r] < _p[s]) s = r;
                    if (s == i) break;
                    Swap(i, s); i = s;
                }
                return top;
            }
            void Swap(int a, int b) { (_n[a], _n[b]) = (_n[b], _n[a]); (_p[a], _p[b]) = (_p[b], _p[a]); }
        }

        // ------------------------------------------------------------------ cover (the small triangles)
        static Material Mat(string name, Color c) => MoveTestBuilder.SaveMaterial(name, Shader.Find("Universal Render Pipeline/Lit"), c);

        static Mesh[] _rocks;

        /// <summary>A lumpy rock (an icosphere pushed in and out), 5 shapes kept as assets.</summary>
        static Mesh RockMesh(int seed)
        {
            string path = $"{MoveTestBuilder.GenDir}/IT_RockMesh_{seed}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh) return mesh;
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var v = new List<Vector3> { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            var f = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int it = 0; it < 2; it++)
            {
                var mid = new Dictionary<long, int>();
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (mid.TryGetValue(key, out int m)) return m;
                    v.Add((v[a] + v[b]) * 0.5f);
                    return mid[key] = v.Count - 1;
                }
                var nf = new List<int>();
                for (int i = 0; i < f.Count; i += 3)
                {
                    int a = f[i], b = f[i + 1], c = f[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                f = nf;
            }
            for (int i = 0; i < v.Count; i++)
            {
                Vector3 d = v[i].normalized;
                float r = 1f + 0.28f * (Vn(d.x * 1.6f + seed * 5.3f, d.y * 1.6f + d.z * 1.1f) - 0.5f) * 2f + 0.08f * (Vn(d.x * 4f + seed, d.z * 4f + d.y) - 0.5f) * 2f;
                Vector3 p = d * r;
                if (p.y < -0.35f) p.y = -0.35f - (p.y + 0.35f) * 0.2f;      // a flatter bottom to sit on
                v[i] = p * 0.5f;
            }
            mesh = new Mesh { name = $"IT_RockMesh_{seed}" };
            mesh.SetVertices(v);
            mesh.SetTriangles(f, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static GameObject Rock(Transform parent, string name, Material mat, Vector3 at, Vector3 size, System.Random rnd, float sink = 0.2f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at - Vector3.up * size.y * sink;
            go.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 12f - 6f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 12f - 6f);
            go.transform.localScale = size;
            var mesh = _rocks[rnd.Next(_rocks.Length)];
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
            col.convex = true;
            return go;
        }

        static void Soft(Transform parent, string name, Material mat, Vector3 at, Vector3 size, float yaw)
        {
            var go = WeatherSceneSetup.Prim(parent, name, PrimitiveType.Sphere, mat, at, size, yaw);
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        /// <summary>Something to hide behind every 15-30 m (closer in the forest and the meadow), by region.</summary>
        static void PlaceCover(Transform map, Terrain terrain)
        {
            EnsureRocks();
            var stone = Mat("IT_Stone", new Color(0.62f, 0.5f, 0.4f));
            var red = Mat("IT_RedRock", new Color(0.66f, 0.42f, 0.3f));
            var salt = Mat("IT_SaltRock", new Color(0.84f, 0.83f, 0.79f));
            var lava = Mat("IT_LavaRock", new Color(0.2f, 0.2f, 0.21f));
            var bark = Mat("W_Bark", new Color(0.33f, 0.25f, 0.18f));
            var leaves = Mat("W_Leaves", new Color(0.24f, 0.38f, 0.2f));
            var grass = Mat("IT_Grass", new Color(0.42f, 0.56f, 0.3f));
            var shrub = Mat("IT_Shrub", new Color(0.46f, 0.47f, 0.3f));
            var drift = Mat("IT_Driftwood", new Color(0.6f, 0.56f, 0.5f));

            var root = new GameObject("隠れる物").transform;
            root.SetParent(map, false);
            var groups = new Dictionary<string, Transform>();
            Transform G(string n) { if (!groups.TryGetValue(n, out var t)) { t = new GameObject(n).transform; t.SetParent(root, false); groups[n] = t; } return t; }

            var rnd = new System.Random(800);
            var taken = new Dictionary<long, List<Vector3>>();      // x, z, spacing; a hash grid of 8 m cells
            long Key(int a, int b) => ((long)a << 32) ^ (uint)b;
            bool Free(Vector2 p, float space)
            {
                int ci = Mathf.FloorToInt(p.x / 8f), cj = Mathf.FloorToInt(p.y / 8f), reach = Mathf.CeilToInt(space / 8f) + 1;
                for (int a = -reach; a <= reach; a++)
                    for (int b = -reach; b <= reach; b++)
                        if (taken.TryGetValue(Key(ci + a, cj + b), out var list))
                            foreach (var q in list)
                                if ((new Vector2(q.x, q.y) - p).magnitude < (space + q.z) * 0.5f) return false;
                return true;
            }
            void Take(Vector2 p, float space)
            {
                long k = Key(Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.y / 8f));
                if (!taken.TryGetValue(k, out var list)) taken[k] = list = new List<Vector3>();
                list.Add(new Vector3(p.x, p.y, space));
            }
            // keep the camps, the pool and the spring clear
            foreach (var s in new[] { new Vector2(250, 318), new Vector2(500, 300), PoolSvg, SpringSvg }) Take(W(s), 24f);
            if (_fallOK) Take(_fallC, FallR * 2f + 8f);              // the plunge pool and the ledge behind the falls
            foreach (var m in _mouths) Take(new Vector2(m.x, m.z), 12f);   // the caves' mouths
            Take(W(MountainSvg), 12f);                                  // the summit's rock salt

            var data = terrain.terrainData;
            var o = new float[Kinds];
            float cellW = Size / (Res - 1f);
            int count = 0;
            for (int tries = 0; tries < 400000; tries++)
            {
                var sv = new Vector2(140f + (float)rnd.NextDouble() * 460f, 108f + (float)rnd.NextDouble() * 306f);
                var p = W(sv);
                float h = Base(sv.x, sv.y, o, false);
                if (h < 0.8f) continue;                                           // the sea and the beach line
                float nx = (p.x + Size * 0.5f) / Size, nz = (p.y + Size * 0.5f) / Size;
                float steep = data.GetSteepness(nx, nz);
                if (steep > 38f) continue;
                int hi = Mathf.Clamp(Mathf.RoundToInt((p.x + Size * 0.5f) / cellW), 0, Res - 1), hj = Mathf.Clamp(Mathf.RoundToInt((p.y + Size * 0.5f) / cellW), 0, Res - 1);
                if (_wayMask != null && _wayMask[hj * Res + hi] > 0.05f) continue;   // keep the ways clear
                if (RiverBankDistance(p.x, p.y) < 3f || o[8] > 0.3f) continue;     // the rivers, the salt flat
                var at = new Vector3(p.x, terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y, p.y);
                float r01 = (float)rnd.NextDouble();
                float space; System.Action make;
                if (o[3] > 0.85f && h > Mesa - 8f)
                {   // the mesa top: rock pillars left by the rain
                    space = 22f;
                    make = () => Rock(G("台地の岩の柱"), "岩の柱", red, at, new Vector3(1.6f + r01 * 1.2f, 2.5f + r01 * 3f, 1.6f + r01), rnd, 0.1f);
                }
                else if (o[6] > 0.5f)
                {   // the canyon floor: rocks fallen from the walls
                    space = 16f;
                    make = () => Rock(G("谷の落石"), "落石", red, at, new Vector3(1.5f, 1.1f, 1.8f) * (0.8f + r01), rnd);
                }
                else if (o[4] > 0.5f)
                {   // the lava field and the cone: lava boulders, more where the flows are rough
                    space = 14f;
                    make = () => Rock(G("溶岩の岩"), "溶岩の岩", lava, at, new Vector3(1.6f, 1.4f, 1.8f) * (0.8f + r01 * 0.9f), rnd, 0.08f);
                }
                else if (o[5] > 0.6f)
                {   // the rock-salt mountain
                    space = 18f;
                    make = () => Rock(G("岩塩の山の岩"), "岩塩の岩", salt, at, new Vector3(1.6f, 1.1f, 1.4f) * (0.8f + r01), rnd);
                }
                else if (o[2] > 0.6f && o[0] < 0.3f)
                {   // the desert (and the beaches): sandstone, dry shrubs, driftwood at the shore
                    if (Sd(sv.x, sv.y, _island, _islandBox) < 18f) { space = 34f; make = () => Log(G("流木"), drift, at, 3f + r01 * 3f, 0.35f, rnd); }
                    else if (r01 < 0.55f) { space = 22f; make = () => Rock(G("砂漠の岩"), "砂岩", stone, at, new Vector3(1.5f, 1f, 1.3f) * (0.8f + r01), rnd); }
                    else { space = 16f; make = () => Soft(G("乾いた茂み"), "茂み", shrub, at + Vector3.up * 0.35f, new Vector3(1.8f, 1.1f, 1.8f) * (0.8f + r01 * 0.5f), r01 * 360f); }
                }
                else if (o[1] > 0.6f)
                {   // the flower meadow: grass clumps at the slug's height, a few rocks
                    if (r01 < 0.9f) { space = 6f; make = () => Soft(G("草むら"), "草むら", grass, at + Vector3.up * 0.15f, new Vector3(1.6f, 0.65f, 1.6f) * (0.8f + r01 * 0.4f), r01 * 360f); }
                    else { space = 30f; make = () => Rock(G("草原の岩"), "岩", stone, at, new Vector3(1.4f, 0.9f, 1.2f), rnd); }
                }
                else if (o[0] > 0.5f)
                {   // the rain forest: trees, logs, rocks
                    if (r01 < 0.5f) { space = 8f; make = () => Tree(G("森の木"), bark, leaves, at, rnd); }
                    else if (r01 < 0.85f) { space = 6.5f; make = () => Soft(G("下草"), "下草", leaves, at + Vector3.up * 0.3f, new Vector3(2.2f, 1.2f, 2.2f) * (0.8f + r01 * 0.4f), r01 * 360f); }
                    else if (r01 < 0.93f) { space = 20f; make = () => Log(G("倒木"), bark, at, 4f + r01 * 3f, 0.4f, rnd); }
                    else { space = 22f; make = () => Rock(G("森の岩"), "苔の岩", stone, at, new Vector3(1.5f, 1.1f, 1.4f) * (0.8f + r01 * 0.5f), rnd); }
                }
                else
                {   // the low ground between
                    if (r01 < 0.6f) { space = 16f; make = () => Soft(G("茂み"), "茂み", shrub, at + Vector3.up * 0.35f, new Vector3(1.8f, 1.1f, 1.8f), r01 * 360f); }
                    else { space = 24f; make = () => Rock(G("低い土地の岩"), "岩", stone, at, new Vector3(1.4f, 1f, 1.3f) * (0.8f + r01 * 0.6f), rnd); }
                }
                space *= 0.85f + 0.3f * (float)rnd.NextDouble();
                if (!Free(p, space)) continue;
                Take(p, space);
                make();
                count++;
            }
            // the one big rock in the middle of the salt flat (a camp spot: the open crossing has one place to stand)
            {
                var p = W(SaltSvg + new Vector2(4f, 2f));
                var at = new Vector3(p.x, terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y, p.y);
                Rock(G("塩田の大岩"), "塩田の大岩", stone, at, new Vector3(6f, 3.5f, 5f), rnd, 0.15f);
                count++;
            }
            Debug.Log($"[IslandTest800] cover: {count}");
        }

        static void Tree(Transform parent, Material bark, Material leaves, Vector3 at, System.Random rnd)
        {
            // ordinary trees stay well under the banyan (35 m) so it stands out over the canopy; now and then a taller one
            float h = rnd.NextDouble() < 0.06 ? 10f + (float)rnd.NextDouble() * 3f : 5f + (float)rnd.NextDouble() * 4f;
            float crown = h * (0.55f + 0.2f * (float)rnd.NextDouble());
            var t = new GameObject("木").transform;
            t.SetParent(parent, false);
            WeatherSceneSetup.Prim(t, "幹", PrimitiveType.Cylinder, bark, at + Vector3.up * h * 0.5f, new Vector3(0.28f + h * 0.02f, h * 0.5f, 0.28f + h * 0.02f), 0f);
            Soft(t, "葉", leaves, at + Vector3.up * (h + crown * 0.15f), new Vector3(crown, crown * 0.7f, crown), (float)rnd.NextDouble() * 360f);
        }

        static void Log(Transform parent, Material mat, Vector3 at, float length, float radius, System.Random rnd)
        {
            var go = WeatherSceneSetup.Prim(parent, "倒木", PrimitiveType.Cylinder, mat, at + Vector3.up * radius * 0.8f, new Vector3(radius * 2f, length * 0.5f, radius * 2f), 0f);
            go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 90f);
        }

        // ------------------------------------------------------------------ measuring the ways
        /// <summary>Length, time, the steepest bit of every way (crawling along it on the built ground).</summary>
        [MenuItem("Slug/IslandTest 800 m/Measure Ways")]
        public static string MeasureWays()
        {
            var t = OpenTerrain();
            if (!t) return "no terrain";
            var sb = new StringBuilder("| 道 | 長さ | 軽い | 重い | いちばん急 | 30° より急 |\n| --- | --- | --- | --- | --- | --- |\n");
            foreach (var lr in Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include))
            {
                if (lr.transform.parent == null || lr.transform.parent.name != "道") continue;
                var pts = new Vector3[lr.positionCount];
                lr.GetPositions(pts);
                float len = 0f, steep = 0f, over = 0f;
                Vector3 prev = Vector3.zero;
                for (int i = 0; i < pts.Length; i++)
                {
                    var q = new Vector3(pts[i].x, t.SampleHeight(pts[i]) + t.transform.position.y, pts[i].z);
                    if (i > 0)
                    {
                        float seg = (q - prev).magnitude, deg = Mathf.Atan2(Mathf.Abs(q.y - prev.y), new Vector2(q.x - prev.x, q.z - prev.z).magnitude) * Mathf.Rad2Deg;
                        len += seg; steep = Mathf.Max(steep, deg);
                        if (deg > 30f) over += seg;
                    }
                    prev = q;
                }
                sb.AppendLine($"| {lr.name} | {len:0} m | {len / 1.2f / 60f:0.0} 分 | {len / 0.36f / 60f:0.0} 分 | {steep:0}° | {over:0} m |");
            }
            return sb.ToString();
        }

        /// <summary>Slug's-eye shots along the ways and in the cover, into Screenshots/Island800.</summary>
        [MenuItem("Slug/IslandTest 800 m/Shoot Ways And Cover")]
        public static string ShootWays()
        {
            var t = OpenTerrain();
            if (!t) return "no terrain";
            var sb = new StringBuilder();
            foreach (var lr in Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include))
            {
                if (lr.transform.parent == null || lr.transform.parent.name != "道" || !lr.name.StartsWith("東の道") && !lr.name.StartsWith("塩の道") && !lr.name.StartsWith("けものみち（砂漠")) continue;
                int n = lr.positionCount;
                Vector3 a = lr.GetPosition(n * 6 / 10), b = lr.GetPosition(Mathf.Min(n - 1, n * 6 / 10 + 12));
                string file = lr.name.StartsWith("東") ? "20_east_road" : lr.name.StartsWith("塩") ? "21_salt_road" : "22_trail_to_mesa";
                sb.AppendLine(ShotUtil.Capture($"{ShotDir}/{file}.png", a + Vector3.up * 0.47f, b + Vector3.up * 0.47f, 60f, 1200, 600, 3000f) + "  " + lr.name);
            }
            (string file, Vector2 from, Vector2 to, float up)[] views =
            {
                ("23_forest_inside", new Vector2(548, 300), new Vector2(500, 290), 2f),
                ("24_mesa_pillars", new Vector2(290, 222), new Vector2(150, 240), -10f),
                ("25_meadow_grass", new Vector2(330, 380), new Vector2(375, 262), 30f),
                ("26_lava_boulders", new Vector2(440, 172), new Vector2(400, 145), 5f),
            };
            foreach (var v in views)
                sb.AppendLine(ShotUtil.Capture($"{ShotDir}/{v.file}.png", Ground(t, v.from) + Vector3.up * 0.47f, Ground(t, v.to) + Vector3.up * v.up, 60f, 1200, 600, 3000f));
            Vector3 c = Ground(t, new Vector2(366, 262));
            sb.AppendLine(ShotUtil.Capture($"{ShotDir}/air_south_east_cover.png", c + new Vector3(260f, 170f, -300f), c + Vector3.up * 10f, 45f, 1400, 800, 4000f));
            return sb.ToString();
        }
    }
}
