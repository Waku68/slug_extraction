using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using SlugDesign;

namespace SlugExtract
{
    /// <summary>
    /// The rock-salt mountain as a rugged mountain of tiers (Cairn look, Nov 4; docs/ideas/cairn_terrain_ideas.md §12,
    /// docs/ideas/mountain_other_games.md). わくさん: "height is the stake"; craggy, ways that run sideways along ledges, and
    /// now and then a way up. Nov 4 (2): the ways up cut into the heightmap were jagged (a 2.4 m path is 3 cells of 0.78 m),
    /// so the heightmap now holds only the smooth cone cut into ledges and steps, and everything narrow is a placed part:
    /// - the steps' walls: rock parts (MountainWallBuilder: big faceted blocks, strata slabs, columns, by sector round the
    ///   mountain), stretched to each wall's height and run, about every 5 m along the wall
    /// - the ways up: shelves on the wall, 2.6 m to walk (a human's 9 m), with a smooth collider of their own. Half a
    ///   diagonal (one way up along the wall, about 20°), half a switchback (half way up, a landing, back the other way
    ///   above it; the upper leg sits on a retaining face over the lower one). Rock shelf : laid stone stair : old boards
    ///   = 3 : 1 : 1 (わくさん). One every 40 m of ledge (26 m on the north side by the lava), a little off from tier to tier
    /// - between each two ways up, a climbing stair: two columns of stone blocks, 0.38 m up each (a human's 1.4 m; the
    ///   slug climbs up to 0.45 m), sideways from one column to the other, 0.3 m further in each step. Slow and exposed
    /// The heightmap is carved just under every shelf and block (so nothing pokes through), and knobs stay off them.
    /// The notch (キレット) is out for now (わくさん); the false summit stays. The pool, the falls and the salt cave (south)
    /// and the canyon's head (west) are left as they were. Remove puts all of it back
    /// (Assets/TestScenes/Generated/SaltTiers_Backup.txt). Menu Slug/IslandTest 800 m/Cairn Try/Salt Tiers ...
    /// Walk Test walks a slug-sized CharacterController (r 0.18, h 0.56, 50°, step 0.15) up every way, and checks every
    /// climbing block (rise at most 0.45 m, room for the body on its top).
    /// </summary>
    public static class IslandSaltTiers
    {
        const string Root = "SaltTiers";
        const string Backup = "Assets/TestScenes/Generated/SaltTiers_Backup.txt";
        const float TierH = 10f, LedgePart = 0.72f, LedgeRise = 0.12f;
        const float ZoneIn = 150f, ZoneOut = 186f, BelowNot = 14f;
        static readonly Vector2 KeepAt = new Vector2(-4f, -110f);      // pool, falls, salt cave (world x, z)
        const float KeepIn = 30f, KeepOut = 44f;

        static Terrain _t;
        static Vector2 Center => IslandTest800Builder.World(new Vector2(375f, 262f));

        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Salt Tiers Build")]
        public static string BuildMenu() => CairnTryShots.WithIsland(Build, true);
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Salt Tiers Remove")]
        public static string RemoveMenu() => CairnTryShots.WithIsland(Remove, true);

        // the false summit, south-east of the summit (metres from the summit, east and north)
        static readonly Vector2 FalseRel = new Vector2(34f, -28f);
        const float FalseTop = 101f;
        static readonly Vector2 KnobGap = new Vector2(17f, -14f);       // (where the notch was: the knobs keep off it)

        // a way or stair the walk test could not get up is left out, and the mountain built again without it (up to 3 times)
        static readonly HashSet<string> _deny = new HashSet<string>();
        static readonly List<string> _failed = new List<string>();
        static string Key(bool cliff, int k, bool stair, int n) => $"{(cliff ? "c" : "t")}{k}/{(stair ? "s" : "w")}/{n}";

        public static string Build()
        {
            _deny.Clear();
            var sb = new StringBuilder();
            for (int pass = 0; pass < 3; pass++)
            {
                string r = BuildOnce();
                if (_failed.Count == 0 || pass == 2) { sb.Append(r); break; }
                foreach (var name in _failed)
                {
                    // "段7 往復の道 岩の棚 #5", "西のがけ3 よじ登る段 #2"
                    var parts = name.Split(' ');
                    bool cliff = parts[0].StartsWith("西のがけ");
                    int k = int.Parse(parts[0].Substring(cliff ? 4 : 1));
                    int n = int.Parse(parts[parts.Length - 1].TrimStart('#'));
                    _deny.Add(Key(cliff, k, name.Contains("よじ登る段"), n));
                }
                sb.AppendLine($"(build {pass + 1}: {_failed.Count} could not be walked up; left out and built again)");
            }
            return sb.ToString();
        }

        static string BuildOnce()
        {
            _t = Object.FindAnyObjectByType<Terrain>();
            if (!_t) return "open IslandTest_800 first";
            if (File.Exists(Backup)) Remove();
            _mountainGround = null; _mesa = null;
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(MountainWallBuilder.WallPrefab(2, 2))) MountainWallBuilder.Build();
            var sb = new StringBuilder();
            SaveBackup();
            var root = new GameObject(Root).transform;
            sb.AppendLine(Reshape(root));
            sb.AppendLine(CliffWays(root));
            sb.AppendLine(MoveThings());
            sb.AppendLine(Walls(root));
            sb.AppendLine(ThingsOffWays());
            sb.AppendLine(Scatter(root));
            sb.AppendLine(Markers(root));
            sb.AppendLine(PaintWaysUp());
            sb.AppendLine(Report());
            sb.AppendLine(WalkTest());
            return sb.ToString();
        }

        // ------------------------------------------------------------------ the shape
        static float Weight(float x, float z)
        {
            var rel = new Vector2(x, z) - Center;
            float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ZoneIn, ZoneOut, rel.magnitude));
            w *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(KeepIn, KeepOut, (new Vector2(x, z) - KeepAt).magnitude));
            // the canyon's head cuts into the west flank: leave it
            float inCanyon = Mathf.Clamp01(Mathf.InverseLerp(-48f, -62f, rel.x)) * Mathf.Clamp01(Mathf.Min(Mathf.InverseLerp(-22f, -10f, rel.y), Mathf.InverseLerp(76f, 64f, rel.y)));
            return w * (1f - inCanyon) * Mathf.Max(MountainGround(x, z), Westness(rel) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(60f, 72f, rel.magnitude))));
        }

        // only the salt mountain's own ground is cut into tiers (its terrain layer, IT_Rock and the roads on it), not the
        // lava field to the north nor the red mesa to the west; softened over about 9 m
        static float[,] _mountainGround;
        static float MountainGround(float x, float z)
        {
            var data = _t.terrainData;
            int ar = data.alphamapResolution;
            if (_mountainGround == null)
            {
                int layers = data.alphamapLayers;
                var a = data.GetAlphamaps(0, 0, ar, ar);
                var raw = new float[ar, ar];
                for (int l = 0; l < layers; l++)
                {
                    string n = data.terrainLayers[l] ? data.terrainLayers[l].name : "";
                    if (n != "IT_Rock" && n != "IT_Path") continue;
                    for (int j = 0; j < ar; j++) for (int i = 0; i < ar; i++) raw[j, i] += a[j, i, l];
                }
                const int B = 5;
                _mountainGround = new float[ar, ar];
                for (int j = 0; j < ar; j++)
                    for (int i = 0; i < ar; i++)
                    {
                        float sum = 0f; int c = 0;
                        for (int dj = -B; dj <= B; dj++) for (int di = -B; di <= B; di++)
                        {
                            int jj = j + dj, ii = i + di;
                            if (jj < 0 || ii < 0 || jj >= ar || ii >= ar) continue;
                            sum += raw[jj, ii]; c++;
                        }
                        _mountainGround[j, i] = sum / c;
                    }
            }
            Vector3 tp = _t.transform.position;
            float fx = Mathf.Clamp01((x - tp.x) / data.size.x) * (ar - 1), fz = Mathf.Clamp01((z - tp.z) / data.size.z) * (ar - 1);
            int i0 = Mathf.Min((int)fx, ar - 2), j0 = Mathf.Min((int)fz, ar - 2);
            float m = Mathf.Lerp(Mathf.Lerp(_mountainGround[j0, i0], _mountainGround[j0, i0 + 1], fx - i0), Mathf.Lerp(_mountainGround[j0 + 1, i0], _mountainGround[j0 + 1, i0 + 1], fx - i0), fz - j0);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.85f, m));
        }

        // how much of the new shape the ground takes (0 = the old ground, 1 = all new)
        static float Blend(float x, float z)
        {
            float old = OldGround(new Vector3(x, 0f, z));
            if (old < BelowNot) return 0f;
            return Weight(x, z) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BelowNot, BelowNot + 6f, old));
        }

        // the ways up are spaced by the walk along the ledge: one about every 40 m (a human's 145 m), closer on the north
        // side towards the lava (about 26 m)
        const float BreakEvery = 40f, BreakEveryNorth = 26f;

        static float BreakSpacing(float ang)
        {
            float north = Mathf.Clamp01(Mathf.Sin(ang * Mathf.Deg2Rad));
            return Mathf.Lerp(BreakEvery, BreakEveryNorth, Mathf.Pow(north, 1.5f));
        }

        static float Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9e3779b9u;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                return (h & 0xffffff) / (float)0x1000000;
            }
        }

        // the tiers on the cone: a ledge (rising a little towards the mountain), then a wall up to the next ledge. k is the
        // tier, f how far across it (0 at the ledge's outer edge, LedgePart where the wall starts, 1 at the wall's top)
        static float Tiered(float h, float x, float z, out int k, out float f)
        {
            var rel = new Vector2(x, z) - Center;
            float r = rel.magnitude, ang = Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg;
            // wander the tier lines (they follow the ground's height, so a little sideways noise makes them bend)
            float hs = h + 3.5f * (Mathf.PerlinNoise(x * 0.025f + 11f, z * 0.025f + 3f) - 0.5f) * 2f;
            // the tiers are not all alike: 6-16 m (a human's 22-58 m), changing round the mountain and with height
            float th = TierH * (0.6f + 1.0f * Mathf.PerlinNoise(ang * 0.012f + 4.2f, h * 0.012f + 1.7f));
            k = Mathf.FloorToInt(hs / th);
            f = hs / th - k;
            float s = f < LedgePart ? LedgeRise * f / LedgePart : Mathf.Lerp(LedgeRise, 1f, Mathf.SmoothStep(0f, 1f, (f - LedgePart) / (1f - LedgePart)));
            return h + ((k + s) * th - hs) - Gully(ang, r);
        }

        // the new ground before knobs and the false summit
        static float H0(float x, float z) => Tiered(Cone((new Vector2(x, z) - Center).magnitude, x, z), x, z, out _, out _);
        static Vector2 Dir2(float ang) => new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
        static float H0At(float ang, float r) { var p = Center + Dir2(ang) * r; return HAt(p.x, p.y); }

        // the west cliffs' ways (between the red mesa and the mountain's tiers) are planned on the ground as it is built,
        // not on the plan: while one of them is worked on, heights come from the terrain
        static bool _onGround;
        static float HAt(float x, float z) => _onGround ? Ground(new Vector2(x, z)) : H0(x, z);

        // gullies (沢) cut down the flanks and through the tiers: they wander, 4-8 m deep, with sides gentle enough that
        // a ledge dips through them and out again (they don't cut the sideways ways)
        static readonly float[] GullyAt = { 22f, 63f, 112f, 152f, 205f, 238f, 296f, 333f };
        static float Gully(float ang, float r)
        {
            float depth = 0f;
            for (int i = 0; i < GullyAt.Length; i++)
            {
                float at = GullyAt[i] + 14f * Mathf.Sin(r * 0.035f + i * 1.7f);
                float d = Mathf.Abs(Mathf.DeltaAngle(ang, at)) * Mathf.Deg2Rad * Mathf.Max(r, 1f);
                float v = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3f, 15f + r * 0.04f, d));
                float D = (4f + 4f * Mathf.Repeat(i * 0.618f, 1f)) * Mathf.Clamp01((r - 35f) / 30f) * Mathf.Clamp01((178f - r) / 40f);
                depth = Mathf.Max(depth, D * v);
            }
            return depth;
        }

        // the mountain under the tiers: a smooth cone (113 m at the top, 18 m at 150 m out), a little uneven
        static float Cone(float r, float x, float z)
        {
            float t = Mathf.Clamp01((r - 8f) / 182f);
            float h = MountainTop * Mathf.Pow(1f - t, 1.2f) + 3f * (Mathf.PerlinNoise(x * 0.03f + 2f, z * 0.03f + 5f) - 0.5f);
            // the west (the red mesa, the desert side; わくさん Nov 4「砂漠側に通路がない」): the old mountain stood 20-30 m over
            // the mesa there and was left as a cliff. The cone now comes down to the mesa's own height 72 m out and rises
            // from it as steeply as the cone elsewhere, so that side is cut into tiers with ways up like the rest
            var rel = new Vector2(x, z) - Center;
            float west = Westness(rel) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(22f, 36f, r));
            if (west > 0f) h = Mathf.Lerp(h, Mathf.Min(h, MesaAt(rel) + 0.75f * (72f - r)), west);
            // the false summit: a hump on the cone south-east of the summit, cut into tiers like the rest (as a dome laid
            // over the tiers it stood up as a smooth drum)
            float df = (new Vector2(x, z) - (Center + FalseRel)).magnitude;
            if (df < 32f) h += 12f * (0.5f + 0.5f * Mathf.Cos(Mathf.PI * df / 32f));
            return h;
        }

        // how much a place is on the west, the red mesa's side (1 from about 125° round to -125°)
        static float Westness(Vector2 rel)
        {
            float c = rel.x / Mathf.Max(rel.magnitude, 0.01f);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.3f, -0.55f, c));
        }

        // the mesa's height 72 m out from the summit in this direction (the old ground, averaged over 10°; never below 50 m:
        // the canyon's floor is not where the mountain should come down to)
        static float[] _mesa;
        static float MesaAt(Vector2 rel)
        {
            if (_mesa == null)
            {
                _mesa = new float[360];
                for (int a = 0; a < 360; a++)
                {
                    float sum = 0f; int n = 0;
                    for (int da = -5; da <= 5; da++) { var p = Center + Dir2(a + da) * 72f; sum += OldGround(new Vector3(p.x, 0f, p.y)); n++; }
                    _mesa[a] = Mathf.Max(50f, sum / n);
                }
            }
            float ang = Mathf.Repeat(Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg, 360f);
            int i0 = Mathf.FloorToInt(ang) % 360;
            return Mathf.Lerp(_mesa[i0], _mesa[(i0 + 1) % 360], ang - Mathf.Floor(ang));
        }
        const float MountainTop = 113f;

        // rounded rock knobs on the slopes (granite-like domes)
        static List<Vector4> Knobs()
        {
            var list = new List<Vector4>();
            for (int i = 0; i < 140 && list.Count < 34; i++)
            {
                float a = i * 2.39996f, r = 30f + 130f * Mathf.Repeat(i * 0.618f + 0.1f, 1f);
                var rel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                var at = Center + rel;
                if (Weight(at.x, at.y) < 0.95f) continue;
                if ((rel - KnobGap).magnitude < 26f) continue;
                bool clear = true;
                foreach (var o in list) if ((new Vector2(o.x, o.y) - at).magnitude < 22f) clear = false;
                if (!clear) continue;
                float rad = 10f + 12f * Mathf.Repeat(i * 0.377f, 1f);
                list.Add(new Vector4(at.x, at.y, rad, rad * (0.45f + 0.3f * Mathf.Repeat(i * 0.71f, 1f))));
            }
            return list;
        }

        static float KnobAt(List<Vector4> knobs, float x, float z)
        {
            float knob = 0f;
            foreach (var kb in knobs)
            {
                float d = new Vector2(x - kb.x, z - kb.y).magnitude / kb.z;
                if (d < 1f) knob += kb.w * Mathf.Pow(1f - d * d, 1.2f);
            }
            return knob;
        }

        // ------------------------------------------------------------------ where the walls are
        const float AngStep = 0.25f;
        const int NAng = 1440;
        static float AngOf(int ai) => -180f + ai * AngStep;

        struct WallAt { public bool ok; public float rFoot, rTop, yFoot, yTop; }
        static Dictionary<int, WallAt[]> _walls;

        // walks in along every 0.25° line and notes each tier's wall: where it starts (its foot, the ledge's inner edge) and
        // where it ends (its top, the next ledge's outer edge), with their heights
        static void ProfileWalls()
        {
            _walls = new Dictionary<int, WallAt[]>();
            for (int ai = 0; ai < NAng; ai++)
            {
                float ang = AngOf(ai);
                var d = Dir2(ang);
                int prevK = int.MinValue; bool inWall = false; float rF = 0f;
                for (float r = ZoneOut; r >= 6f; r -= 0.1f)
                {
                    var p = Center + d * r;
                    Tiered(Cone(r, p.x, p.y), p.x, p.y, out int k, out float f);
                    if (k != prevK)
                    {
                        if (inWall && k == prevK + 1) Record(prevK, ai, ang, rF, r + 0.1f);
                        inWall = false; prevK = k;
                    }
                    if (!inWall && f > LedgePart && f < 0.97f) { inWall = true; rF = r; }
                }
            }
        }

        static void Record(int k, int ai, float ang, float rF, float rT)
        {
            if (rF - rT < 0.8f) return;
            var d = Dir2(ang);
            Vector2 pf = Center + d * (rF + 1.5f), pt = Center + d * (rT - 1.5f), fals = Center + FalseRel;
            if (Blend(pf.x, pf.y) < 0.98f || Blend(pt.x, pt.y) < 0.98f) return;
                        float yF = H0At(ang, rF), yT = H0At(ang, rT);
            if (yT - yF < 2f) return;
            if (!_walls.TryGetValue(k, out var arr)) _walls[k] = arr = new WallAt[NAng];
            if (arr[ai].ok) return;
            arr[ai] = new WallAt { ok = true, rFoot = rF, rTop = rT, yFoot = yF, yTop = yT };
        }

        // one wall followed round the mountain (its samples by angle, and the walk along its foot)
        class Run { public int k; public bool cliff; public WallAt[] walls; public List<int> ai = new List<int>(); public List<float> s = new List<float>(); public float Len => s[s.Count - 1]; }
        static List<Run> _runs;

        static List<Run> Runs(Dictionary<int, WallAt[]> walls, bool cliff)
        {
            var runs = new List<Run>();
            foreach (var kv in walls)
            {
                Run cur = null; WallAt prev = default; int prevAi = -10;
                for (int ai = 0; ai < NAng; ai++)
                {
                    var w = kv.Value[ai];
                    if (!w.ok) { cur = null; continue; }
                    if (cur != null && (ai != prevAi + 1 || Mathf.Abs(w.rFoot - prev.rFoot) > 2f || Mathf.Abs(w.yFoot - prev.yFoot) > 2f || Mathf.Abs(w.yTop - prev.yTop) > 2f)) cur = null;
                    if (cur == null) { cur = new Run { k = kv.Key, cliff = cliff, walls = kv.Value }; runs.Add(cur); cur.ai.Add(ai); cur.s.Add(0f); }
                    else
                    {
                        Vector2 a = Center + Dir2(AngOf(prevAi)) * prev.rFoot, b = Center + Dir2(AngOf(ai)) * w.rFoot;
                        cur.ai.Add(ai); cur.s.Add(cur.s[cur.s.Count - 1] + (b - a).magnitude);
                    }
                    prev = w; prevAi = ai;
                }
            }
            runs.RemoveAll(r => r.ai.Count < 8);
            return runs;
        }

        struct At { public float ang, rFoot, rTop, yFoot, yTop; }

        static At AtArc(Run run, float s)
        {
            s = Mathf.Clamp(s, 0f, run.Len);
            int i = run.s.BinarySearch(s);
            if (i < 0) i = ~i - 1;
            i = Mathf.Clamp(i, 0, run.s.Count - 2);
            float t = Mathf.InverseLerp(run.s[i], run.s[i + 1], s);
            var a = run.walls[run.ai[i]]; var b = run.walls[run.ai[i + 1]];
            return new At
            {
                ang = Mathf.Lerp(AngOf(run.ai[i]), AngOf(run.ai[i + 1]), t),
                rFoot = Mathf.Lerp(a.rFoot, b.rFoot, t), rTop = Mathf.Lerp(a.rTop, b.rTop, t),
                yFoot = Mathf.Lerp(a.yFoot, b.yFoot, t), yTop = Mathf.Lerp(a.yTop, b.yTop, t)
            };
        }

        // how far out (from the summit) the wall's face stands at height y
        static float RW(At w, float y)
        {
            float lo = w.rTop - 0.6f, hi = w.rFoot + 0.6f;
            if (y >= H0At(w.ang, lo)) return lo;
            if (y <= H0At(w.ang, hi)) return hi;
            for (int i = 0; i < 22; i++)
            {
                float m = 0.5f * (lo + hi);
                if (H0At(w.ang, m) > y) lo = m; else hi = m;
            }
            return 0.5f * (lo + hi);
        }

        // ------------------------------------------------------------------ the ways up and the climbing stairs
        static string TierName(Feature f) => f.run.cliff ? $"西のがけ{f.k}" : $"段{f.k}";
        static readonly string[] KindJa = { "斜めの道", "往復の道", "よじ登る段" };
        static readonly string[] StyleJa = { "岩の棚", "石の段", "板の桟道" };

        class Feature
        {
            public int k, n, kind, style;                  // kind 0 diagonal, 1 switchback, 2 climbing stair
            public Run run;
            public float s0, s1;                          // its stretch along the wall's foot (with a little room)
            public float sStart, dir, L, yA, yB;          // ways: where u = 0 on the wall, which way u runs, a leg's length, foot and top heights
            public float oA, oB;                          // ... and how far those stand from the wall's own foot and top there
            public List<Vector3> walk = new List<Vector3>();
            public int N; public float ft, fb, g0, yT;    // stairs: blocks, the top and bottom blocks' fronts, the ground at the foot, the top
            public List<Vector3> mask = new List<Vector3>();   // (x, z, radius) kept clear of knobs
            public List<(Collider c, float depth)> carve = new List<(Collider, float)>();
            public Vector3 foot, head;                    // where the way starts (lower ledge) and ends (upper ledge)
        }
        static List<Feature> _features;

        static string PlaceFeatures()
        {
            _features = new List<Feature>();
            _taken.Clear();
            _why.Clear();
            _runs = Runs(_walls, false);
            var placed = PlaceOn(_runs);
            _features.AddRange(placed);
            var why = new StringBuilder();
            foreach (var kv in _why) why.Append($"{kv.Key} {kv.Value}, ");
            return $"walls {_runs.Count} runs; {Count(placed)}; left out: {why}";
        }

        static string Count(List<Feature> fs)
        {
            int ways = 0, zig = 0, stairs = 0;
            foreach (var f in fs) { if (f.kind == 2) stairs++; else { ways++; if (f.kind == 1) zig++; } }
            return $"ways up {ways} (switchbacks {zig}), climbing stairs {stairs}";
        }

        static List<Vector3> _taken = new List<Vector3>();   // (x, z, radius) of every part placed so far

        // the ground a part covers, as circles (x, z, radius): a way's legs and its foot and head, a stair's line of blocks
        static List<Vector3> Footprint(Feature f)
        {
            var list = new List<Vector3>();
            if (f.kind == 2)
            {
                _onGround = f.run.cliff;
                var w = AtArc(f.run, f.sStart);
                var d = Dir2(w.ang);
                for (float r = f.fb + 1f; r >= f.ft - BlockDepth; r -= 1f) { var p = Center + d * r; list.Add(new Vector3(p.x, p.y, 1.6f)); }
                return list;
            }
            foreach (int leg in Legs(f))
            {
                LegRange(f, leg, out float u0, out float u1);
                for (float u = u0; u <= u1 + 0.01f; u += 2f)
                {
                    var c = Section(f, leg, u);
                    var p = P(c, 0.5f * (c.rIn + c.rOut), 0f);
                    list.Add(new Vector3(p.x, p.z, 0.5f * (c.rOut - c.rIn) + 0.3f));
                }
            }
            var wf = AtArc(f.run, f.sStart - f.dir * 1.5f);
            var foot = Center + Dir2(wf.ang) * (wf.rFoot + 2.2f);
            list.Add(new Vector3(foot.x, foot.y, 2f));
            return list;
        }

        // along each wall: a way up every spacing, a climbing stair half way between
        static List<Feature> PlaceOn(List<Run> runs)
        {
            var list = new List<Feature>();
            foreach (var run in runs)
            {
                var cnt = new float[run.s.Count];
                for (int i = 1; i < cnt.Length; i++) cnt[i] = cnt[i - 1] + (run.s[i] - run.s[i - 1]) / BreakSpacing(AngOf(run.ai[i]));
                float phase = Mathf.Repeat(run.k * 0.618f + 0.3f + run.ai[0] * 0.0137f, 1f);
                var spans = new List<Vector2>();
                for (int pass = 0; pass < 2; pass++)
                    for (int n = 0; ; n++)
                    {
                        float c = n + phase + 0.5f * pass;
                        if (c > cnt[cnt.Length - 1]) break;
                        // where it does not fit, a little to one side or the other
                        Feature f = null;
                        foreach (float shift in pass == 0 ? new[] { 0f, 0.2f, -0.2f, 0.35f } : new[] { 0f, 0.12f, -0.12f })
                        {
                            float cs = c + shift;
                            if (cs < 0f || cs > cnt[cnt.Length - 1]) continue;
                            f = pass == 0 ? TryWay(run, ArcAtCount(run, cnt, cs), n) : TryStair(run, ArcAtCount(run, cnt, cs), n);
                            if (f == null) continue;
                            bool near = false;
                            foreach (var sp in spans) if (f.s1 + 2.5f > sp.x && f.s0 - 2.5f < sp.y) near = true;
                            if (!near) break;
                        }
                        if (f == null) continue;
                        bool clash = false;
                        foreach (var sp in spans) if (f.s1 + 2.5f > sp.x && f.s0 - 2.5f < sp.y) clash = true;
                        if (clash) { Why(pass == 0 ? "way clashes" : "stair clashes"); continue; }
                        // nor on the ground of a part of another tier (a stair's top cuts into the ledge above, where that
                        // tier's ways start)
                        var fp = Footprint(f);
                        bool taken = false;
                        foreach (var q in fp) foreach (var o in _taken) if ((new Vector2(q.x - o.x, q.y - o.y)).sqrMagnitude < (q.z + o.z) * (q.z + o.z)) { taken = true; break; }
                        if (taken) { Why(pass == 0 ? "way on another tier's part" : "stair on another tier's part"); continue; }
                        _taken.AddRange(fp);
                        spans.Add(new Vector2(f.s0, f.s1));
                        list.Add(f);
                    }
            }
            return list;
        }

        static float ArcAtCount(Run run, float[] cnt, float c)
        {
            for (int i = 0; i < cnt.Length - 1; i++)
                if (c <= cnt[i + 1]) return Mathf.Lerp(run.s[i], run.s[i + 1], Mathf.InverseLerp(cnt[i], cnt[i + 1], c));
            return run.Len;
        }

        // a way follows the wall down into a gully and up out of it; it is left out only where the wall's height changes much
        static bool GroundOk(Run run, float sFrom, float sTo, float dy)
        {
            // (where the wall runs in towards the summit rather than round it, a shelf measured outwards from it is sheared
            // flat and steep: leave such a stretch)
            for (float s = sFrom; s + 1f <= sTo + 0.01f; s += 1f)
                if (Mathf.Abs(AtArc(run, s + 1f).rFoot - AtArc(run, s).rFoot) > 0.6f) { Why("wall runs across"); return false; }
            if (dy <= 0f) return true;
            for (float s = sFrom; s <= sTo + 0.01f; s += 1f)
            {
                var w = AtArc(run, s);
                if (Mathf.Abs(w.yTop - w.yFoot - dy) > 3f) { Why("wall height changes"); return false; }
            }
            return true;
        }

        static Dictionary<string, int> _why = new Dictionary<string, int>();
        static void Why(string reason) { _why.TryGetValue(reason, out int n); _why[reason] = n + 1; }

        static Feature TryWay(Run run, float sc, int n)
        {
            _onGround = run.cliff;
            if (_deny.Contains(Key(run.cliff, run.k, false, n))) { Why("failed the walk test"); return null; }
            var w = AtArc(run, sc);
            float S = BreakSpacing(w.ang), dy = w.yTop - w.yFoot;
            int kind = Hash(n, run.k * 31 + run.ai[0]) < 0.5f && dy >= 5f ? 1 : 0;
            // a way leaves room for a climbing stair half way to the next (about 10 m)
            float g = 0.36f, room = S - 10f;                                   // tan 20°
            float L = kind == 0 ? dy / g : 0.5f * dy / g;
            float span = kind == 0 ? L : L + 2.8f;
            if (span > room && kind == 0 && dy >= 5f) { kind = 1; L = 0.5f * dy / g; span = L + 2.8f; }
            if (span > room)
            {
                L = kind == 0 ? room : room - 2.8f;
                if ((kind == 0 ? dy : 0.5f * dy) / L > 0.47f) { Why("too steep for the room"); return null; }   // steeper than 25°
                span = kind == 0 ? L : L + 2.8f;
            }
            float h = Hash(n * 7 + run.k, 911);
            var f = new Feature { k = run.k, n = n, kind = kind, run = run, L = L, style = h < 0.6f ? 0 : h < 0.8f ? 1 : 2, dir = Hash(n + 101, run.k * 7 + 3) < 0.5f ? -1f : 1f };
            f.sStart = sc - f.dir * span * 0.5f;
            float a = f.sStart, b = f.sStart + f.dir * span;
            f.s0 = Mathf.Min(a, b) - 1.5f; f.s1 = Mathf.Max(a, b) + 1.5f;
            if (f.s0 < 0f || f.s1 > run.Len) { Why("way past the wall's end"); return null; }
            if (!GroundOk(run, f.s0, f.s1, dy)) return null;
            var w0 = AtArc(run, f.sStart);
            f.yA = H0At(w0.ang, w0.rFoot + 0.15f) - 0.03f;
            f.oA = f.yA - w0.yFoot;
            var wTop = AtArc(run, f.sStart + f.dir * (kind == 0 ? L : 0f));
            f.yB = H0At(wTop.ang, wTop.rTop - 0.4f);
            f.oB = f.yB - wTop.yTop;
            // (following the wall, a gully makes it steeper on one side)
            for (int leg = 0; leg <= 2; leg += 2)
            {
                if (leg == 2 && kind == 0) break;
                for (float u = 0f; u + 0.5f <= L + 0.01f; u += 0.5f)
                {
                    float y0 = leg == 0 ? Y1(f, u) : Y2(f, u), y1 = leg == 0 ? Y1(f, u + 0.5f) : Y2(f, u + 0.5f);
                    if (Mathf.Abs(y1 - y0) > 0.35f) { Why("way too steep"); return null; }     // 35° over half a metre
                }
            }
            return f;
        }

        const float Rise = 0.38f, StepIn = 0.3f, BlockDepth = 2f, BlockW = 0.96f, ColGap = 0.49f;

        static Feature TryStair(Run run, float sc, int n)
        {
            _onGround = run.cliff;
            if (_deny.Contains(Key(run.cliff, run.k, true, n))) { Why("failed the walk test"); return null; }
            var f = new Feature { k = run.k, n = n, kind = 2, run = run, sStart = sc, s0 = sc - 1.4f, s1 = sc + 1.4f, dir = Hash(n + 55, run.k * 13 + 1) < 0.5f ? -1f : 1f };
            if (f.s0 < 0f || f.s1 > run.Len) { Why("stair past the wall's end"); return null; }
            if (!GroundOk(run, f.s0, f.s1, -1f)) return null;
            var w = AtArc(run, sc);
            float mid = 0.5f * (w.rFoot + w.rTop);
            f.yT = H0At(w.ang, w.rTop - 1.0f);
            int N = Mathf.Max(2, Mathf.CeilToInt((f.yT - w.yFoot) / Rise));
            for (int it = 0; it < 6; it++)
            {
                f.ft = Mathf.Min(mid - 0.5f * StepIn * (N - 1), w.rTop + 0.2f);
                f.fb = f.ft + StepIn * (N - 1);
                f.g0 = H0At(w.ang, f.fb + 0.25f);
                int need = Mathf.CeilToInt((f.yT - f.g0) / Rise);
                if (need <= N) break;
                N = need;
            }
            f.N = N;
            if ((f.yT - f.g0) / N > 0.4f || f.fb - w.rFoot > 4.2f) { Why("stair sticks out too far"); return null; }
            if (H0At(w.ang, f.fb + 1.2f) < f.g0 - 0.8f || H0At(w.ang, f.fb + 2.5f) < f.g0 - 1.5f) { Why("stair over a drop"); return null; }
            return f;
        }

        // ------------------------------------------------------------------ the shelves of a way up
        struct Sec { public Vector2 d; public float rIn, rOut, y, keelR, keelY; }
        static Vector3 P(Sec c, float r, float y) => new Vector3(Center.x + c.d.x * r, y, Center.y + c.d.y * r);
        static Vector3 Out3(Sec c) => new Vector3(c.d.x, 0f, c.d.y);
        static Vector3 Along(Feature f, Sec c) => f.dir * new Vector3(-c.d.y, 0f, c.d.x);   // the way u runs

        // the heights follow the wall's own foot and top round the mountain (so a way dips into a gully with the wall)
        static float FootY(Feature f, float u) => AtArc(f.run, f.sStart + f.dir * u).yFoot + f.oA;
        static float TopY(Feature f, float u) => AtArc(f.run, f.sStart + f.dir * u).yTop + f.oB;
        static float MidY(Feature f, float u) => 0.5f * (FootY(f, u) + TopY(f, u));
        static float Y1(Feature f, float u) => f.kind == 0 ? Mathf.Lerp(FootY(f, u), TopY(f, u), u / f.L) : Mathf.Lerp(FootY(f, u), MidY(f, u), u / f.L);
        static float Y2(Feature f, float u) => Mathf.Lerp(TopY(f, u), MidY(f, u), u / f.L);

        // leg 0: the diagonal or the switchback's lower leg; 1: the landing; 2: the upper leg (back over the lower one)
        static Sec Section(Feature f, int leg, float u)
        {
            _onGround = f.run.cliff;
            var w = AtArc(f.run, f.sStart + f.dir * u);
            var c = new Sec { d = Dir2(w.ang) };
            if (leg == 0)
            {
                c.y = Y1(f, u);
                float rw = RW(w, c.y);
                // (a switchback's lower leg reaches on under the upper one, so the ground is cut down to it there too: else
                // the heightmap's cells between the two legs stand up as teeth in front of the upper leg's face)
                c.rIn = rw - (f.kind == 1 ? 2.3f : 1.3f); c.rOut = rw + 1.6f;
                c.keelY = c.y - 1.8f; c.keelR = Mathf.Min(RW(w, c.keelY), c.rOut - 0.25f);
            }
            else if (leg == 1)
            {
                c.y = MidY(f, f.L);
                float rw = RW(w, c.y);
                c.rIn = rw - 3.9f; c.rOut = rw + 1.6f;
                c.keelY = c.y - 1.8f; c.keelR = Mathf.Min(RW(w, c.keelY), c.rOut - 0.25f);
            }
            else
            {
                c.y = Y2(f, u);
                float y1 = Y1(f, u);
                c.rOut = RW(w, y1) - 1.0f;                                  // on the lower leg's inner edge
                c.rIn = Mathf.Min(c.rOut - 2.9f, RW(w, c.y) - 1.0f);         // back to the wall (or the ledge above)
                c.keelY = Mathf.Min(c.y - 0.4f, y1 - 0.1f); c.keelR = c.rOut + 0.12f;     // (just over the lower leg's edge, hiding the cut ground)
            }
            if (c.rOut - c.rIn < 0.6f) c.rIn = c.rOut - 0.6f;
            return c;
        }

        static void LegRange(Feature f, int leg, out float u0, out float u1)
        {
            if (leg == 1) { u0 = f.L - 0.05f; u1 = f.L + 2.8f; }        // (starting earlier, the flat landing stood a step above the sloping leg)
            else { u0 = 0f; u1 = f.L; }
        }
        static int[] Legs(Feature f) => f.kind == 0 ? new[] { 0 } : new[] { 0, 1, 2 };

        static SimpleMesh _apron;   // the foot's ramp of the collider last made

        static Mesh ColliderMesh(Feature f)
        {
            var m = new SimpleMesh(1);
            foreach (int leg in Legs(f))
            {
                LegRange(f, leg, out float u0, out float u1);
                int n = Mathf.Max(2, Mathf.CeilToInt((u1 - u0) / 0.5f));
                int[] pa = null; Sec first = default, last = default;
                for (int i = 0; i <= n; i++)
                {
                    var c = Section(f, leg, Mathf.Lerp(u0, u1, i / (float)n));
                    var q = new[] { m.V(P(c, c.rIn, c.y)), m.V(P(c, c.rOut, c.y)), m.V(P(c, c.rOut, c.y - 0.35f)), m.V(P(c, c.rIn, c.y - 0.35f)) };
                    if (pa != null)
                    {
                        var o = Out3(c);
                        m.Quad(0, pa[0], pa[1], q[1], q[0], Vector3.up);
                        m.Quad(0, pa[1], pa[2], q[2], q[1], o);
                        m.Quad(0, pa[2], pa[3], q[3], q[2], Vector3.down);
                        m.Quad(0, pa[3], pa[0], q[0], q[3], -o);
                    }
                    else { first = c; m.Quad(0, q[0], q[1], q[2], q[3], -Along(f, c)); }
                    pa = q; last = c;
                }
                m.Quad(0, pa[0], pa[1], pa[2], pa[3], Along(f, last));
            }
            // the foot: a short ramp from the shelf's first end down into the ground (not carved under: where the ledge is
            // higher it stays under the ground)
            {
                var c0 = Section(f, 0, 0f);
                var t = Along(f, c0) * -1.2f;
                Vector3 a = P(c0, c0.rIn, c0.y), b = P(c0, c0.rOut, c0.y);
                Vector3 a2 = a + t + Vector3.down * 0.45f, b2 = b + t + Vector3.down * 0.45f;
                _apron = new SimpleMesh(1);
                int ia = _apron.V(a), ib = _apron.V(b), ib2 = _apron.V(b2), ia2 = _apron.V(a2);
                int ja = _apron.V(a + Vector3.down * 0.5f), jb = _apron.V(b + Vector3.down * 0.5f);
                _apron.Quad(0, ia, ib, ib2, ia2, Vector3.up);
                _apron.Quad(0, ia2, ib2, jb, ja, -Vector3.up - t.normalized);
                _apron.Tri(0, ia, ia2, ja, -Out3(c0)); _apron.Tri(0, ib, ib2, jb, Out3(c0));
            }
            return m.ToMesh("WayCollider");
        }

        static float Wob(Feature f, int leg, float u, float salt) => Mathf.PerlinNoise(u * 0.45f + f.n * 3.1f + salt, leg * 7.7f + f.k * 1.3f + salt * 2f);

        // a rock shelf jutting from the wall: a flat top, a rough lip, and a craggy keel back into the wall below (on a
        // switchback's upper leg the keel is the tall face down to the lower leg: rows of crags, not one flat face)
        static void RockShelf(SimpleMesh m, Feature f, int leg)
        {
            LegRange(f, leg, out float u0, out float u1);
            int n = Mathf.Max(2, Mathf.CeilToInt((u1 - u0) / 0.6f));
            int K = leg == 2 ? 7 : 3;
            int[] pa = null; Sec last = default;
            for (int i = 0; i <= n; i++)
            {
                float u = Mathf.Lerp(u0, u1, i / (float)n);
                var c = Section(f, leg, u);
                float j1 = 0.22f * (Wob(f, leg, u, 0f) - 0.35f), j2 = Wob(f, leg, u, 5f);
                var q = new int[3 + K];
                q[0] = m.V(P(c, c.rIn, c.y)); q[1] = m.V(P(c, c.rOut + j1, c.y));
                float rC = c.rOut + 0.5f * j1 + 0.08f, yC = c.y - 0.3f - 0.25f * j2;
                q[2] = m.V(P(c, rC, yC));
                for (int k = 1; k <= K; k++)
                {
                    float t = k / (float)K;
                    float crag = (leg == 2 ? 0.35f : 0.25f) * (Mathf.PerlinNoise(u * 0.55f + f.n * 2.3f, k * 1.37f + leg * 5f) - 0.5f) * (k == K ? 0.3f : 1f);
                    q[2 + k] = m.V(P(c, Mathf.Lerp(rC, c.keelR, t) + crag, Mathf.Lerp(yC, c.keelY, t)));
                }
                var o = Out3(c);
                if (pa != null)
                {
                    m.Quad(0, pa[0], pa[1], q[1], q[0], Vector3.up);
                    for (int k = 1; k < q.Length - 1; k++) m.Quad(0, pa[k], pa[k + 1], q[k + 1], q[k], o + Vector3.down * (k == 1 ? 0f : 0.3f));
                }
                else for (int k = 1; k < q.Length - 1; k++) m.Tri(0, q[0], q[k], q[k + 1], -Along(f, c));
                pa = q; last = c;
            }
            for (int k = 1; k < pa.Length - 1; k++) m.Tri(0, pa[0], pa[k], pa[k + 1], Along(f, last));
        }

        static float H(Feature f, int leg, int idx, int k) => Hash(idx + leg * 1000 + f.n * 10000 + (f.run.cliff ? 500000 : 0), k * 31 + f.k * 17);

        // a stair of laid stones: each tread two or three flat-topped stones of different widths, a little uneven, on a
        // dry-stone retaining wall (stacked stones, every other row offset) down to the ground or the lower leg.
        // (Nov 4 わくさん「石の段が不自然」: before, smooth even treads on one flat face)
        static void StoneShelf(NatureMeshKit kit, Feature f, int leg)
        {
            LegRange(f, leg, out float u0, out float u1);
            int n = Mathf.Max(1, Mathf.RoundToInt((u1 - u0) / 0.42f));
            float run = (u1 - u0) / n;
            for (int j = 0; j < n; j++)
            {
                float ua = u0 + j * run, ub = ua + run, um = 0.5f * (ua + ub);
                var mid = Section(f, leg, um);
                float yt = mid.y + 0.02f;
                var rot = Quaternion.LookRotation(Along(f, mid));
                float rA = leg == 1 ? mid.rIn + 0.3f : Mathf.Max(mid.rIn + 0.3f, mid.rOut - 2.8f), rB = mid.rOut + 0.05f;
                int stones = H(f, leg, j, 1) < 0.5f ? 2 : 3;
                float r0 = rA;
                for (int s = 0; s < stones; s++)
                {
                    float share = (0.7f + 0.6f * H(f, leg, j, 2 + s)) / (stones - s);
                    float r1 = s == stones - 1 ? rB : Mathf.Lerp(r0, rB, Mathf.Clamp(share, 0.25f, 0.75f));
                    float top = yt + 0.03f * (H(f, leg, j, 6 + s) - 0.5f), du = 0.05f * (H(f, leg, j, 9 + s) - 0.5f);
                    var cpos = P(mid, 0.5f * (r0 + r1), top - 0.24f) + Along(f, mid) * du;
                    float topY = top;
                    PropKit.Lump(kit, cpos, new Vector3(0.5f * (r1 - r0) + 0.03f, 0.26f, 0.5f * run + 0.04f), 4, 5, 0, 4000 + j * 7 + s + f.n * 31, 0.09f, 1.8f, float.NaN, rot * Quaternion.Euler(0f, 6f * (H(f, leg, j, 14 + s) - 0.5f), 0f), 0f, 1f,
                        null, p => { if (p.y > topY) p.y = topY; return p; }, 4.5f);
                    r0 = r1;
                }
            }
            // the retaining wall: rows of stones under the outer edge, of uneven heights; in a row, stones of uneven lengths
            // (0.4-1.2 m) and depths, each a little turned, set in or out (a dry-stone wall, not a honeycomb)
            float depth = 0.45f;
            for (int row = 0; row < 40; row++)
            {
                float rowH = (leg == 2 ? 0.4f : 0.3f) + (leg == 2 ? 0.35f : 0.25f) * H(f, leg, row, 30);
                bool any = false;
                float u = u0 - 0.6f * H(f, leg, row, 31);
                for (int k = 0; u < u1; k++)
                {
                    float len = (leg == 2 ? 0.6f : 0.4f) + (leg == 2 ? 0.7f : 0.6f) * H(f, leg, row * 100 + k, 32);
                    float uc = Mathf.Clamp(u + 0.5f * len, u0 + 0.1f, u1 - 0.1f);
                    var c = Section(f, leg, uc);
                    float h = rowH * (0.75f + 0.3f * H(f, leg, row * 100 + k, 33));
                    float yc = c.y + 0.02f - depth - 0.5f * rowH + 0.06f * (H(f, leg, row * 100 + k, 34) - 0.5f);
                    float bottom = Mathf.Min(c.keelY, c.y - 0.6f);
                    if (yc + 0.5f * h >= bottom)
                    {
                        any = true;
                        float jr = 0.1f * (H(f, leg, row * 100 + k, 35) - 0.5f);
                        var cpos = P(c, c.rOut - 0.2f + jr, yc);
                        var rot = Quaternion.LookRotation(Along(f, c)) * Quaternion.Euler(0f, 8f * (H(f, leg, row * 100 + k, 36) - 0.5f), 4f * (H(f, leg, row * 100 + k, 37) - 0.5f));
                        PropKit.Lump(kit, cpos, new Vector3(0.18f + 0.1f * H(f, leg, row * 100 + k, 38), 0.5f * h + 0.02f, 0.5f * len + 0.02f), 4, 5, 0, 6000 + row * 97 + k * 13 + f.n, 0.1f, 1.8f, float.NaN,
                            rot, 0f, 1f, null, null, 5.5f);
                    }
                    u += len;
                }
                depth += rowH;
                if (!any) break;
            }
        }

        // old boards, weathered (Nov 4 わくさん「木の上り坂を風化させて」): most planks gone silver-grey, widths and gaps uneven,
        // some turned or tilted a little, a quarter broken off short at the outer end (a jagged end), now and then one
        // missing; posts leaning, a few gone. The collider under it stays whole and smooth.
        // Submesh 0 brown wood, 1 weathered grey.
        static void WoodShelf(SimpleMesh m, Feature f, int leg)
        {
            LegRange(f, leg, out float u0, out float u1);
            int idx = 0;
            bool lastMissing = false;
            for (float u = u0; u < u1 - 0.1f; idx++)
            {
                float w = 0.22f + 0.1f * H(f, leg, idx, 1), gap = 0.03f + 0.06f * H(f, leg, idx, 2);
                float ub = Mathf.Min(u + w, u1);
                bool missing = !lastMissing && H(f, leg, idx, 3) < 0.07f && u > u0 + 1f && ub < u1 - 1f;
                lastMissing = missing;
                if (!missing)
                {
                    Sec a = Section(f, leg, u), b = Section(f, leg, ub), mid = Section(f, leg, 0.5f * (u + ub));
                    int sub = H(f, leg, idx, 4) < 0.7f ? 1 : 0;
                    float y = mid.y + 0.02f * (H(f, leg, idx, 5) - 0.5f), tilt = 0.035f * (H(f, leg, idx, 6) - 0.5f);
                    float ra = 0.15f * (Wob(f, leg, u, 3f) - 0.5f);
                    float cut = H(f, leg, idx, 7) < 0.25f ? 0.25f + 0.8f * H(f, leg, idx, 8) : 0f;
                    var along = Along(f, mid) * (0.07f * (H(f, leg, idx, 9) - 0.5f));
                    Vector3 up = Vector3.up * tilt;
                    Vector3 ai = P(a, a.rIn + 0.3f, y) - along + up, bi = P(b, b.rIn + 0.3f, y) - along + up;
                    Vector3 ao = P(a, a.rOut + ra - cut, y) + along - up, bo = P(b, b.rOut + ra - cut * (0.6f + 0.5f * H(f, leg, idx, 10)), y) + along - up;
                    Vector3 mo = Vector3.Lerp(ao, bo, 0.5f) + Out3(mid) * (cut > 0f ? 0.12f * (H(f, leg, idx, 11) - 0.3f) : 0f);
                    Vector3 dn = Vector3.down * 0.07f;
                    int iai = m.V(ai), ibi = m.V(bi), ibo = m.V(bo), imo = m.V(mo), iao = m.V(ao);
                    m.Tri(sub, iai, ibi, ibo, Vector3.up); m.Tri(sub, iai, ibo, imo, Vector3.up); m.Tri(sub, iai, imo, iao, Vector3.up);
                    m.Quad(sub, ai, ao, ao + dn, ai + dn, -Along(f, a));
                    m.Quad(sub, bi, bo, bo + dn, bi + dn, Along(f, b));
                    m.Quad(sub, ao, mo, mo + dn, ao + dn, Out3(mid));
                    m.Quad(sub, mo, bo, bo + dn, mo + dn, Out3(mid));
                }
                u = ub + gap;
            }
            int n = Mathf.Max(2, Mathf.CeilToInt((u1 - u0) / 0.6f));
            foreach (float off in new[] { -0.25f, -1.4f })
            {
                Vector3[] pa = null;
                for (int i = 0; i <= n; i++)
                {
                    var c = Section(f, leg, Mathf.Lerp(u0, u1, i / (float)n));
                    float r = c.rOut + off, y0 = c.y - 0.07f, y1 = c.y - 0.27f;
                    var q = new[] { P(c, r - 0.08f, y0), P(c, r + 0.08f, y0), P(c, r + 0.08f, y1), P(c, r - 0.08f, y1) };
                    if (pa != null)
                    {
                        var o = Out3(c);
                        m.Quad(1, pa[1], pa[2], q[2], q[1], o);
                        m.Quad(1, pa[2], pa[3], q[3], q[2], Vector3.down);
                        m.Quad(1, pa[3], pa[0], q[0], q[3], -o);
                    }
                    pa = q;
                }
            }
            int pi = 0;
            bool lastGone = false;
            for (float u = u0 + 0.4f; u <= u1; u += 2.4f, pi++)
            {
                bool gone = !lastGone && pi > 0 && H(f, leg, pi, 20) < 0.15f;
                lastGone = gone;
                if (gone) continue;
                var c = Section(f, leg, u);
                var at = P(c, c.rOut - 0.25f, 0f);
                float bottom = leg == 2 ? Y1(f, u) - 0.1f : Mathf.Min(HAt(at.x, at.z), c.y - 0.55f) - 0.25f;
                // a leaning post: its top pushed along and out a little
                var lean = Along(f, c) * (0.18f * (H(f, leg, pi, 21) - 0.5f)) + Out3(c) * (0.08f * H(f, leg, pi, 22));
                float top = c.y - 0.07f, hgt = top - bottom;
                var shaft = Vector3.up * hgt + lean;
                var axis = shaft.normalized;
                var side = Vector3.Cross(axis, Out3(c)).normalized;
                var fwd = Vector3.Cross(side, axis);
                m.Box(H(f, leg, pi, 23) < 0.6f ? 1 : 0, at + Vector3.up * bottom, side, axis, fwd, new Vector3(-0.08f, 0f, -0.08f), new Vector3(0.08f, shaft.magnitude, 0.08f));
            }
        }

        static Material _wallMat, _stoneMat, _woodMat, _woodOldMat;

        static string BuildFeatures(Transform root, List<Feature> fs)
        {
            var ways = root.Find("登り口の道"); var stairs = root.Find("よじ登る段");
            if (!ways) { ways = new GameObject("登り口の道").transform; ways.SetParent(root, false); }
            if (!stairs) { stairs = new GameObject("よじ登る段").transform; stairs.SetParent(root, false); }
            _wallMat = AssetDatabase.LoadAssetAtPath<Material>(MountainWallBuilder.MatPath("Block"));
            _stoneMat = AssetDatabase.LoadAssetAtPath<Material>(MountainWallBuilder.MatPath("PathStone"));
            _woodMat = AssetDatabase.LoadAssetAtPath<Material>(MountainWallBuilder.MatPath("Wood"));
            _woodOldMat = AssetDatabase.LoadAssetAtPath<Material>(MountainWallBuilder.MatPath("WoodOld"));
            var blocks = new GameObject[MountainWallBuilder.Variants];
            for (int v = 0; v < blocks.Length; v++) blocks[v] = AssetDatabase.LoadAssetAtPath<GameObject>(MountainWallBuilder.BlockPrefab(v));
            int tris = 0, nBlocks = 0;
            foreach (var f in fs)
            {
                if (f.kind == 2) { nBlocks += Stair(f, stairs, blocks); continue; }
                var go = new GameObject($"{TierName(f)} {KindJa[f.kind]} {StyleJa[f.style]} #{f.n}");
                go.transform.SetParent(ways, false);
                Mesh mesh; Material[] mats;
                if (f.style == 1)
                {
                    var kit = new NatureMeshKit(1);
                    foreach (int leg in Legs(f)) StoneShelf(kit, f, leg);
                    mesh = kit.ToMesh("Way"); mats = new[] { _stoneMat }; tris += kit.TriangleCount;
                }
                else
                {
                    var look = new SimpleMesh(f.style == 2 ? 2 : 1);
                    foreach (int leg in Legs(f)) { if (f.style == 0) RockShelf(look, f, leg); else WoodShelf(look, f, leg); }
                    mesh = look.ToMesh("Way"); mats = f.style == 0 ? new[] { _wallMat } : new[] { _woodMat, _woodOldMat }; tris += look.TriangleCount;
                }
                var vis = new GameObject("見た目");
                vis.transform.SetParent(go.transform, false);
                vis.AddComponent<MeshFilter>().sharedMesh = mesh;
                vis.AddComponent<MeshRenderer>().sharedMaterials = mats;
                var colGo = new GameObject("当たり");
                colGo.transform.SetParent(go.transform, false);
                var col = colGo.AddComponent<MeshCollider>();
                col.sharedMesh = ColliderMesh(f);
                f.carve.Add((col, f.style == 2 ? 0.55f : 0.15f));
                colGo.AddComponent<MeshCollider>().sharedMesh = _apron.ToMesh("WayFoot");
                WalkPoints(f, go.transform);
                foreach (int leg in Legs(f))
                {
                    LegRange(f, leg, out float u0, out float u1);
                    for (float u = u0; u <= u1 + 0.01f; u += 1.5f)
                    {
                        var c = Section(f, leg, u);
                        var p = P(c, 0.5f * (c.rIn + c.rOut), 0f);
                        f.mask.Add(new Vector3(p.x, p.z, 0.5f * (c.rOut - c.rIn) + 0.5f));
                    }
                }
                f.mask.Add(new Vector3(f.foot.x, f.foot.z, 2.5f));
                f.mask.Add(new Vector3(f.head.x, f.head.z, 2.5f));
            }
            return $"way meshes {tris} triangles, climbing blocks {nBlocks}";
        }

        // the points a walk up this way passes (the lower ledge, along each leg, the upper ledge)
        static void WalkPoints(Feature f, Transform way)
        {
            _onGround = f.run.cliff;
            var pts = new GameObject("歩く点").transform;
            pts.SetParent(way, false);
            var list = new List<Vector3>();
            var wf = AtArc(f.run, f.sStart - f.dir * 1.5f);
            var foot = Center + Dir2(wf.ang) * (wf.rFoot + 2.2f);
            f.foot = new Vector3(foot.x, HAt(foot.x, foot.y), foot.y);
            list.Add(f.foot);
            for (float u = 0.5f; u <= f.L + 0.01f; u += 1f) { var c = Section(f, 0, u); list.Add(P(c, c.rOut - 1.3f, c.y)); }
            Vector2 head;
            if (f.kind == 0)
            {
                var wh = AtArc(f.run, f.sStart + f.dir * (f.L + 0.6f));
                head = Center + Dir2(wh.ang) * (wh.rTop - 2f);
            }
            else
            {
                var land = Section(f, 1, f.L + 1.4f);
                list.Add(P(land, land.rOut - 1.8f, land.y));
                list.Add(P(land, land.rOut - 4.0f, land.y));
                for (float u = f.L - 0.5f; u >= 0.4f; u -= 1f) { var c = Section(f, 2, u); list.Add(P(c, c.rOut - 1.3f, c.y)); }
                var wh = AtArc(f.run, f.sStart);
                head = Center + Dir2(wh.ang) * (wh.rTop - 2f);
            }
            f.head = new Vector3(head.x, HAt(head.x, head.y), head.y);
            list.Add(f.head);
            f.walk = list;
            for (int i = 0; i < list.Count; i++)
            {
                var p = new GameObject(i.ToString()).transform;
                p.SetParent(pts, false);
                p.position = list[i];
            }
        }

        // a climbing stair: two columns of blocks side by side, each block 0.38 m above the last and 0.3 m further in,
        // so the climb goes sideways from column to column up the wall
        static int Stair(Feature f, Transform parent, GameObject[] blocks)
        {
            var go = new GameObject($"{TierName(f)} よじ登る段 #{f.n}").transform;
            go.SetParent(parent, false);
            var w = AtArc(f.run, f.sStart);
            var d = Dir2(w.ang);
            Vector3 o3 = new Vector3(d.x, 0f, d.y), along = new Vector3(-d.y, 0f, d.x) * f.dir;
            float rise = (f.yT - f.g0) / f.N;
            var rot = Quaternion.LookRotation(o3);
            for (int i = 0; i < f.N; i++)
            {
                float front = f.fb - StepIn * i, top = f.g0 + rise * (i + 1);
                var c = Center + d * front;
                var pos = new Vector3(c.x, top, c.y) + along * (i % 2 == 0 ? -ColGap : ColGap);
                _onGround = f.run.cliff;
                float below = HAt(pos.x, pos.z);
                float bottom = Mathf.Min(top - 1.2f, below - 0.35f);
                if (i >= 2) bottom = Mathf.Min(bottom, f.g0 + rise * (i - 1) - 0.3f);
                var src = blocks[(i + f.n) % blocks.Length];
                if (!src) continue;
                var b = (GameObject)PrefabUtility.InstantiatePrefab(src, go);
                b.name = i.ToString();
                b.transform.SetPositionAndRotation(pos, rot);
                b.transform.localScale = new Vector3(BlockW, top - bottom, BlockDepth);
                f.carve.Add((b.GetComponent<Collider>(), 0.12f));
            }
            for (float r = f.fb + 0.5f; r >= f.ft - BlockDepth; r -= 1f)
            {
                var p = Center + d * r;
                f.mask.Add(new Vector3(p.x, p.y, 1.6f));
            }
            var fp = Center + d * (f.fb + 0.8f);
            f.foot = new Vector3(fp.x, f.g0, fp.y);
            return f.N;
        }

        // things on the mountain (rock salt, bots, hiding things) that came down on a way up, a climbing stair or a wall
        // rock: moved along the ledge to bare ground beside it
        static string ThingsOffWays()
        {
            Physics.SyncTransforms();
            var root = GameObject.Find(Root);
            int moved = 0;
            foreach (var t in Things())
            {
                var p = t.position;
                if (!OnParts(p, root)) continue;
                var rel = new Vector2(p.x, p.z) - Center;
                float r = rel.magnitude, a0 = Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg;
                for (float off = 1f; off <= 30f; off += 1f)
                {
                    bool done = false;
                    foreach (float sgn in new[] { 1f, -1f })
                    {
                        var q2 = Center + Dir2(a0 + sgn * off / Mathf.Max(r, 5f) * Mathf.Rad2Deg) * r;
                        var q = new Vector3(q2.x, 0f, q2.y);
                        q.y = Ground(q2);
                        if (MaskAt(q.x, q.z) > 0.3f || OnParts(q, root) || Steep(q2) > 30f) continue;
                        t.position = q + Vector3.up * (p.y - OldGroundOrNew(p));
                        moved++; done = true; break;
                    }
                    if (done) break;
                }
            }
            return $"things moved off the ways {moved}";
        }

        static float OldGroundOrNew(Vector3 p) => Ground(new Vector2(p.x, p.z));

        // is anything of the mountain's parts (a shelf, a block, a wall rock) over this point?
        static bool OnParts(Vector3 p, GameObject root)
        {
            foreach (var h in Physics.RaycastAll(new Vector3(p.x, p.y + 40f, p.z), Vector3.down, 45f, ~0, QueryTriggerInteraction.Ignore))
                if (h.collider.transform.IsChildOf(root.transform)) return true;
            return Physics.CheckSphere(p + Vector3.up * 0.4f, 0.6f, ~0, QueryTriggerInteraction.Ignore) && OverlapsParts(p, root);
        }

        static bool OverlapsParts(Vector3 p, GameObject root)
        {
            foreach (var c in Physics.OverlapSphere(p + Vector3.up * 0.4f, 0.6f, ~0, QueryTriggerInteraction.Ignore))
                if (c.transform.IsChildOf(root.transform)) return true;
            return false;
        }

        // ------------------------------------------------------------------ the west: from the red mesa and the canyon up
        // Between the red mesa (the desert side) and the mountain's tiers the old mountain's cliff stands 20-30 m high (the
        // tiers are cut only into the salt mountain's own ground). わくさん Nov 4「砂漠側に通路がない」: on the built ground,
        // find those cliffs on the west (each 0.25°, 25-80 m from the summit, rising faster than 50° for 5 m or more, its foot
        // on ground the tiers left alone) and put ways up and climbing stairs on them as on the tiers' walls.
        static List<Run> _cliffRuns = new List<Run>();

        static string CliffWays(Transform root)
        {
            var cliffs = new Dictionary<int, WallAt[]>();
            for (int ai = 0; ai < NAng; ai++)
            {
                float ang = AngOf(ai);
                if (ang < 110f && ang > -120f) continue;
                var d = Dir2(ang);
                int idx = 0; bool inBand = false; float rF = 0f, yF = 0f;
                float prev = Ground(Center + d * 80f);
                for (float r = 79.5f; r >= 25f; r -= 0.5f)
                {
                    float h = Ground(Center + d * r);
                    float slope = (h - prev) / 0.5f;
                    if (!inBand && slope > 1.2f) { inBand = true; rF = r + 0.5f; yF = prev; }
                    else if (inBand && slope < 0.7f)
                    {
                        inBand = false;
                        float rT = r + 0.5f, yT = prev;
                        var pf = Center + d * (rF + 1.5f);
                        if (yT - yF >= 5f && rF - rT >= 0.8f && Blend(pf.x, pf.y) < 0.9f && yF > BelowNot)
                        {
                            if (!cliffs.TryGetValue(idx, out var arr)) cliffs[idx] = arr = new WallAt[NAng];
                            arr[ai] = new WallAt { ok = true, rFoot = rF, rTop = rT, yFoot = yF, yTop = yT };
                            idx++;
                        }
                    }
                    prev = h;
                }
            }
            _cliffRuns = Runs(cliffs, true);
            _why.Clear();
            var fs = PlaceOn(_cliffRuns);
            var why = new StringBuilder();
            foreach (var kv in _why) why.Append($"{kv.Key} {kv.Value}, ");
            foreach (var run in _cliffRuns) why.Append($"[{AngOf(run.ai[0]):0}..{AngOf(run.ai[run.ai.Count - 1]):0}° {run.Len:0} m, wall {run.walls[run.ai[0]].yTop - run.walls[run.ai[0]].yFoot:0} m at r {run.walls[run.ai[0]].rFoot:0}] ");
            var built = BuildFeatures(root, fs);
            // fit the built ground to them
            var data = _t.terrainData;
            int res = data.heightmapResolution;
            var hm = data.GetHeights(0, 0, res, res);
            Vector3 tp = _t.transform.position, size = data.size;
            var nh = new float[res, res]; var blend = new float[res, res];
            for (int j = 0; j < res; j++) for (int i = 0; i < res; i++) { nh[j, i] = tp.y + hm[j, i] * size.y; blend[j, i] = 1f; }
            FitGround(fs, nh, blend, out int scree, out int carved);
            for (int j = 0; j < res; j++) for (int i = 0; i < res; i++) hm[j, i] = (nh[j, i] - tp.y) / size.y;
            data.SetHeights(0, 0, hm);
            _t.Flush();
            _features.AddRange(fs);
            StampMask(res, tp, size);
            _onGround = false;
            return $"west cliffs: {_cliffRuns.Count} runs, {Count(fs)}; {built}; scree {scree}, carved {carved}; left out: {why}";
        }

        // ------------------------------------------------------------------ the heightmap
        static float[,] _mask;      // 1 where the knobs stay off (the ways and stairs), in heightmap points

        static void StampMask(int res, Vector3 tp, Vector3 size)
        {
            _mask = new float[res, res];
            float cell = size.x / (res - 1f);
            foreach (var f in _features)
                foreach (var m in f.mask)
                {
                    float R = m.z + 8f;
                    int i0 = Mathf.Max(0, Mathf.FloorToInt((m.x - R - tp.x) / cell)), i1 = Mathf.Min(res - 1, Mathf.CeilToInt((m.x + R - tp.x) / cell));
                    int j0 = Mathf.Max(0, Mathf.FloorToInt((m.y - R - tp.z) / cell)), j1 = Mathf.Min(res - 1, Mathf.CeilToInt((m.y + R - tp.z) / cell));
                    for (int j = j0; j <= j1; j++)
                        for (int i = i0; i <= i1; i++)
                        {
                            float dd = new Vector2(tp.x + i * cell - m.x, tp.z + j * cell - m.y).magnitude;
                            float v = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(m.z + 2f, m.z + 8f, dd));
                            if (v > _mask[j, i]) _mask[j, i] = v;
                        }
                }
        }

        static float MaskAt(float x, float z)
        {
            if (_mask == null) return 0f;
            var data = _t.terrainData; Vector3 tp = _t.transform.position;
            int res = data.heightmapResolution;
            int i = Mathf.Clamp(Mathf.RoundToInt((x - tp.x) / data.size.x * (res - 1)), 0, res - 1), j = Mathf.Clamp(Mathf.RoundToInt((z - tp.z) / data.size.z * (res - 1)), 0, res - 1);
            return _mask[j, i];
        }

        // the ground under the parts: a slope of scree up to each climbing stair's first block, then carved just under every
        // shelf and block (nh: the new ground in metres; blend: how much of it is taken)
        static void FitGround(List<Feature> fs, float[,] nh, float[,] blend, out int scree, out int carved)
        {
            var data = _t.terrainData;
            int res = data.heightmapResolution;
            Vector3 tp = _t.transform.position;
            float cell = data.size.x / (res - 1f);
            // carve just under every shelf and block, so the ground never pokes through them
            Physics.SyncTransforms();
            carved = 0;
            var underWay = new bool[res, res];
            foreach (var f in fs)
                foreach (var (c, depth) in f.carve)
                {
                    if (!c) continue;
                    var b = c.bounds;
                    // a block is narrower than the heightmap's cells: carve 0.9 m round it too, or the cell's corners outside
                    // (the wall) would tilt the ground up over its top
                    bool block = c is BoxCollider;
                    if (block) b.Expand(new Vector3(2f, 0f, 2f));
                    int i0 = Mathf.Max(0, Mathf.FloorToInt((b.min.x - tp.x) / cell)), i1 = Mathf.Min(res - 1, Mathf.CeilToInt((b.max.x - tp.x) / cell));
                    int j0 = Mathf.Max(0, Mathf.FloorToInt((b.min.z - tp.z) / cell)), j1 = Mathf.Min(res - 1, Mathf.CeilToInt((b.max.z - tp.z) / cell));
                    for (int j = j0; j <= j1; j++)
                        for (int i = i0; i <= i1; i++)
                        {
                            if (blend[j, i] <= 0f) continue;
                            if (block)
                            {
                                var tr = c.transform; var sc = tr.lossyScale;
                                var lp = tr.InverseTransformPoint(new Vector3(tp.x + i * cell, tr.position.y, tp.z + j * cell));
                                if (Mathf.Abs(lp.x) > 0.5f + 0.9f / sc.x || lp.z < -1f - 0.9f / sc.z || lp.z > 0.3f / sc.z) continue;
                                float tb = tr.position.y - depth;
                                if (tb < nh[j, i]) { nh[j, i] = tb; carved++; }
                                continue;
                            }
                            // the lowest top under this point (a switchback's lower leg runs on under the upper one)
                            float top = float.MaxValue, from = b.max.y + 1f;
                            for (int k = 0; k < 6; k++)
                            {
                                var ray = new Ray(new Vector3(tp.x + i * cell, from, tp.z + j * cell), Vector3.down);
                                if (!c.Raycast(ray, out var hit, from - b.min.y + 1f)) break;
                                if (hit.normal.y > 0.3f) top = Mathf.Min(top, hit.point.y);
                                from = hit.point.y - 0.01f;
                            }
                            if (top == float.MaxValue) continue;
                            underWay[j, i] = true;
                            float t = top - depth;
                            if (t < nh[j, i]) { nh[j, i] = t; carved++; }
                        }
                }
            // then in front of each climbing stair, a little slope of scree up to one step below its first block (where the
            // ledge dips in front of it, the first block would stand too high to climb)
            scree = 0;
            foreach (var f in fs)
            {
                if (f.kind != 2 || f.carve.Count == 0 || !(f.carve[0].c is BoxCollider)) continue;
                var tr = f.carve[0].c.transform; var sc = tr.lossyScale;
                float top0 = tr.position.y;
                int i0 = Mathf.Max(0, Mathf.FloorToInt((tr.position.x - 5f - tp.x) / cell)), i1 = Mathf.Min(res - 1, Mathf.CeilToInt((tr.position.x + 5f - tp.x) / cell));
                int j0 = Mathf.Max(0, Mathf.FloorToInt((tr.position.z - 5f - tp.z) / cell)), j1 = Mathf.Min(res - 1, Mathf.CeilToInt((tr.position.z + 5f - tp.z) / cell));
                for (int j = j0; j <= j1; j++)
                    for (int i = i0; i <= i1; i++)
                    {
                        if (blend[j, i] <= 0f || underWay[j, i]) continue;
                        var lp = tr.InverseTransformPoint(new Vector3(tp.x + i * cell, top0, tp.z + j * cell));
                        float dx = Mathf.Abs(lp.x) * sc.x, dz = lp.z * sc.z;
                        if (dz < -0.05f || dz > 3.5f || dx > 2.2f) continue;
                        float target = top0 - 0.33f - 0.3f * Mathf.Max(0f, dz - 0.4f) - 0.3f * Mathf.Max(0f, dx - 0.6f);
                        if (target > nh[j, i]) { nh[j, i] = target; scree++; }
                    }
            }
        }

        static string Reshape(Transform root)
        {
            var data = _t.terrainData;
            int res = data.heightmapResolution;
            var hm = data.GetHeights(0, 0, res, res);
            Vector3 tp = _t.transform.position, size = data.size;
            float cell = size.x / (res - 1f);
            ProfileWalls();
            var sb = new StringBuilder(PlaceFeatures());
            var built = BuildFeatures(root, _features);
            StampMask(res, tp, size);
            var knobs = Knobs();
            Vector2 fals = Center + FalseRel;
            var nh = new float[res, res];
            var blend = new float[res, res];
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                {
                    float x = tp.x + cell * i, z = tp.z + cell * j;
                    float w = Weight(x, z);
                    float old = tp.y + hm[j, i] * size.y;
                    // (a way's inner side may reach past the salt mountain's own ground: round the ways it is all new)
                    if ((w <= 0f || old < BelowNot) && _mask[j, i] <= 0f) continue;
                    if (old < BelowNot) w = 0f;
                    // a smooth, gentle cone (never steeper than about 38°) cut into tiers, knobs on it but not on the ways
                    float h = H0(x, z) + KnobAt(knobs, x, z) * (1f - _mask[j, i]);
                    float df = (new Vector2(x, z) - fals).magnitude;
                    nh[j, i] = h;
                    // (under and round the ways, all new: a 2% share of the old, higher mountain would poke 0.5 m through)
                    blend[j, i] = Mathf.Max(w * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BelowNot, BelowNot + 6f, old)), _mask[j, i]);
                }
            FitGround(_features, nh, blend, out int scree, out int carved);
            sb.Append($"; scree at stair feet {scree} points");
            int changed = 0;
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                {
                    if (blend[j, i] <= 0f) continue;
                    float old = tp.y + hm[j, i] * size.y;
                    float nw = Mathf.Lerp(old, nh[j, i], blend[j, i]);
                    if (Mathf.Abs(nw - old) > 0.01f) { hm[j, i] = (nw - tp.y) / size.y; changed++; }
                }
            data.SetHeights(0, 0, hm);
            _t.Flush();
            sb.Append($"; {built}; reshaped {changed} points (carved {carved}), knobs {knobs.Count}");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ the walls' rock
        static int WallStyleOf(string name) => name.Contains("Strata") ? 1 : name.Contains("Column") ? 2 : 0;
        static int WallStyle(float ang, int k) => Mathf.FloorToInt(Mathf.Repeat(ang + 180f + 20f * Mathf.Sin(k * 1.3f), 360f) / 72f) % 3;

        static string Walls(Transform root)
        {
            var parent = new GameObject("段の壁の岩").transform;
            parent.SetParent(root, false);
            var prefabs = new GameObject[3, MountainWallBuilder.Variants];
            for (int s = 0; s < 3; s++) for (int v = 0; v < MountainWallBuilder.Variants; v++) prefabs[s, v] = AssetDatabase.LoadAssetAtPath<GameObject>(MountainWallBuilder.WallPrefab(s, v));
            var knobs = Knobs();
            int n = 0, skipped = 0;
            var byStyle = new int[3];
            foreach (var run in _runs)
            {
                var spans = new List<Vector2>();
                foreach (var f in _features) if (f.run == run) spans.Add(new Vector2(f.s0, f.s1));
                for (float s = 2.5f; s <= run.Len - 0.5f; s += 5f)
                {
                    bool clash = false;
                    foreach (var sp in spans) if (s + 3.2f > sp.x && s - 3.2f < sp.y) clash = true;
                    if (clash) continue;
                    var w = AtArc(run, s);
                    var d = Dir2(w.ang);
                    // a knob over the wall changes its shape: leave the bare ground there
                    bool knobbed = false;
                    foreach (float r in new[] { w.rFoot, 0.5f * (w.rFoot + w.rTop), w.rTop })
                    {
                        var p = Center + d * r;
                        if (KnobAt(knobs, p.x, p.y) * (1f - MaskAt(p.x, p.y)) > 0.3f) knobbed = true;
                    }
                    if (knobbed) { skipped++; continue; }
                    int style = WallStyle(w.ang, run.k), v = Mathf.FloorToInt(Hash(Mathf.RoundToInt(s * 10f), run.k * 17 + run.ai[0]) * MountainWallBuilder.Variants) % MountainWallBuilder.Variants;
                    var src = prefabs[style, v];
                    if (!src) continue;
                    var foot = Center + d * w.rFoot;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
                    go.transform.SetPositionAndRotation(new Vector3(foot.x, w.yFoot, foot.y), Quaternion.LookRotation(new Vector3(d.x, 0f, d.y)));
                    go.transform.localScale = new Vector3(1f, (w.yTop - w.yFoot + 0.05f) / MountainWallBuilder.Hn, Mathf.Max(0.4f, (w.rFoot - w.rTop) / MountainWallBuilder.Run));
                    n++; byStyle[style]++;
                }
            }
            // no wall rock in the way: the space a slug walks through on every way up, and on and above every climbing block
            Physics.SyncTransforms();
            var gone = new HashSet<GameObject>();
            void Clear(Collider[] hits) { foreach (var h in hits) if (h && h.transform.parent == parent) gone.Add(h.gameObject); }
            foreach (var f in _features)
            {
                if (f.kind != 2)
                    for (int i = 1; i < f.walk.Count; i++)
                    {
                        Vector3 a = f.walk[i - 1], b = f.walk[i];
                        int steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / 0.5f));
                        for (int k = 0; k <= steps; k++)
                        {
                            var p = Vector3.Lerp(a, b, k / (float)steps);
                            Clear(Physics.OverlapCapsule(p + Vector3.up * 0.25f, p + Vector3.up * 0.9f, 0.45f, ~0, QueryTriggerInteraction.Ignore));
                        }
                    }
                foreach (var (c, _) in f.carve)
                    if (c is BoxCollider)
                    {
                        var tr = c.transform;
                        Clear(Physics.OverlapBox(tr.position + Vector3.up * 0.4f - tr.forward * 0.5f * tr.lossyScale.z, new Vector3(0.5f * tr.lossyScale.x + 0.15f, 0.35f, 0.5f * tr.lossyScale.z + 0.15f), tr.rotation, ~0, QueryTriggerInteraction.Ignore));
                    }
            }
            foreach (var g in gone) { byStyle[WallStyleOf(g.name)]--; Object.DestroyImmediate(g); n--; }
            return $"wall rocks {n} (blocks {byStyle[0]}, strata {byStyle[1]}, columns {byStyle[2]}), left bare for knobs {skipped}, taken out of the ways {gone.Count}";
        }

        // ------------------------------------------------------------------ rocks on the ledges, cairns, the worn paths
        static float Ground(Vector2 xz) => _t.SampleHeight(new Vector3(xz.x, 0f, xz.y)) + _t.transform.position.y;
        static float Steep(Vector2 xz)
        {
            var d = _t.terrainData; var tp = _t.transform.position;
            return d.GetSteepness((xz.x - tp.x) / d.size.x, (xz.y - tp.z) / d.size.z);
        }

        static GameObject Put(Transform parent, string path, Vector3 pos, float yaw, float scale = 1f)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!src) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        static string Scatter(Transform root)
        {
            var rocks = new GameObject("段の上の岩").transform;
            rocks.SetParent(root, false);
            string[] kinds = { "Rock_A", "Rock_C", "Rock_B", "Talus_B", "SaltCrystal_A", "SaltCrystal_B", "Rock_A", "Talus_A" };
            int n = 0;
            for (int i = 0; i < 900 && n < 140; i++)
            {
                float a = i * 2.39996f, r = 20f + 150f * Mathf.Sqrt(Mathf.Repeat(i * 0.6180339f + 0.31f, 1f));
                var xz = Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (Weight(xz.x, xz.y) < 0.9f || MaskAt(xz.x, xz.y) > 0.2f) continue;
                float g = Ground(xz);
                if (g < BelowNot + 4f || Steep(xz) > 18f) continue;
                string kind = kinds[i % kinds.Length];
                float s = kind.StartsWith("Rock") ? 1.2f + 1.8f * Mathf.Repeat(i * 0.377f, 1f) : 1f + 0.6f * Mathf.Repeat(i * 0.5f, 1f);
                if (Put(rocks, $"Assets/Art/Nature/Rocks/{kind}.prefab", new Vector3(xz.x, g - 0.1f, xz.y), i * 47f % 360f, s)) n++;
            }
            return $"rocks {n}";
        }

        // stone cairns at the foot and the head of each way up (so they read from the ledges), a little to the side
        static string Markers(Transform root)
        {
            var mk = new GameObject("登り口の道しるべ").transform;
            mk.SetParent(root, false);
            int n = 0;
            foreach (var f in _features)
            {
                if (f.kind == 2) continue;
                foreach (var at in new[] { f.foot, f.head })
                {
                    var rel = new Vector2(at.x, at.z) - Center;
                    var side = new Vector2(-rel.y, rel.x).normalized * 2.2f * f.dir;
                    var xz = new Vector2(at.x, at.z) - side;
                    var src = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/Props/Cairn/Cairn_{(char)('A' + (n % 3))}.prefab");
                    if (!src) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(src, mk);
                    go.transform.SetPositionAndRotation(new Vector3(xz.x, Ground(xz) - 0.05f, xz.y), Quaternion.Euler(0f, n * 53f, 0f));
                    n++;
                }
            }
            return $"cairns {n}";
        }

        // the ways up are worn: the ground at their foot and head takes the old road's colour (IT_Path), so they read from
        // the ledges and from afar; the old roads' colour on the mountain goes back to the ground round them
        static string PaintWaysUp()
        {
            var data = _t.terrainData;
            int ar = data.alphamapResolution, layers = data.alphamapLayers;
            int path = -1;
            for (int l = 0; l < layers; l++) if (data.terrainLayers[l] && data.terrainLayers[l].name == "IT_Path") path = l;
            if (path < 0) return "no path layer";
            Vector3 tp = _t.transform.position, size = data.size;
            var spots = new List<Vector3>();
            foreach (var f in _features)
            {
                spots.Add(new Vector3(f.foot.x, f.foot.z, f.kind == 2 ? 1.6f : 2.6f));
                if (f.kind != 2) spots.Add(new Vector3(f.head.x, f.head.z, 2.6f));
            }
            var a = data.GetAlphamaps(0, 0, ar, ar);
            int n = 0;
            for (int j = 0; j < ar; j++)
                for (int i = 0; i < ar; i++)
                {
                    float x = tp.x + size.x * i / (ar - 1f), z = tp.z + size.z * j / (ar - 1f);
                    bool worn = false;
                    foreach (var s in spots) if ((new Vector2(x - s.x, z - s.y)).sqrMagnitude < s.z * s.z) { worn = true; break; }
                    if (!worn)
                    {
                        if (a[j, i, path] <= 0f || Weight(x, z) < 0.3f) continue;
                        int best = -1;
                        for (int l = 0; l < layers; l++) if (l != path && (best < 0 || a[j, i, l] > a[j, i, best])) best = l;
                        a[j, i, best] += a[j, i, path]; a[j, i, path] = 0f;
                        continue;
                    }
                    for (int l = 0; l < layers; l++) a[j, i, l] = l == path ? 1f : 0f;
                    n++;
                }
            data.SetAlphamaps(0, 0, a);
            return $"worn ground painted {n}";
        }

        // per tier: the ways up, the climbing stairs, and the longest walk along a ledge from one to the next
        static string Report()
        {
            var sb = new StringBuilder("| 段 | 壁の高さ | 道（うち往復） | よじ登る段 | 次の登り口まで、いちばん長い横歩き |\n| --- | --- | --- | --- | --- |\n");
            var tiers = new List<int>(_walls.Keys);
            tiers.Sort();
            foreach (int k in tiers)
            {
                int ways = 0, zig = 0, st = 0; float gap = 0f, hLo = 999f, hHi = 0f;
                foreach (var run in _runs)
                {
                    if (run.k != k) continue;
                    for (float s = 0f; s <= run.Len; s += 5f) { var w = AtArc(run, s); hLo = Mathf.Min(hLo, w.yTop - w.yFoot); hHi = Mathf.Max(hHi, w.yTop - w.yFoot); }
                    var spans = new List<Vector2>();
                    foreach (var f in _features)
                    {
                        if (f.run != run) continue;
                        spans.Add(new Vector2(f.s0, f.s1));
                        if (f.kind == 2) st++; else { ways++; if (f.kind == 1) zig++; }
                    }
                    spans.Sort((p, q) => p.x.CompareTo(q.x));
                    for (int i = 1; i < spans.Count; i++) gap = Mathf.Max(gap, spans[i].x - spans[i - 1].y);
                }
                if (hHi <= 0f) continue;
                sb.AppendLine($"| {k} | {hLo:0}〜{hHi:0} m | {ways}（{zig}） | {st} | {gap:0} m |");
            }
            {
                int ways = 0, zig = 0, st = 0; float hLo = 999f, hHi = 0f;
                foreach (var run in _cliffRuns)
                    for (float s = 0f; s <= run.Len; s += 5f) { var w = AtArc(run, s); hLo = Mathf.Min(hLo, w.yTop - w.yFoot); hHi = Mathf.Max(hHi, w.yTop - w.yFoot); }
                foreach (var f in _features) if (f.run.cliff) { if (f.kind == 2) st++; else { ways++; if (f.kind == 1) zig++; } }
                if (hHi > 0f) sb.AppendLine($"| 西のがけ | {hLo:0}〜{hHi:0} m | {ways}（{zig}） | {st} | |");
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ can you walk up?
        /// <summary>
        /// Walks a slug-sized CharacterController (radius 0.18, height 0.56, 50°, step 0.15: the two-legged slug's) from
        /// the lower ledge up every way to the upper ledge, along its walk points; says which ways stop it or drop it. Then
        /// checks every climbing block: no more than 0.45 m above the last, and room for the body on its top.
        /// </summary>
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Salt Tiers Walk Test")]
        public static string WalkTestMenu() => CairnTryShots.WithIsland(WalkTest, false);

        public static string WalkTest()
        {
            var ways = GameObject.Find(Root + "/登り口の道");
            var stairs = GameObject.Find(Root + "/よじ登る段");
            if (!ways) return "build the salt tiers first";
            Physics.SyncTransforms();
            var go = new GameObject("WalkTester") { hideFlags = HideFlags.HideAndDontSave };
            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.18f; cc.height = 0.56f; cc.center = new Vector3(0f, 0.3f, 0f);
            cc.slopeLimit = 50f; cc.stepOffset = 0.15f; cc.skinWidth = 0.02f; cc.minMoveDistance = 0f;
            var sb = new StringBuilder();
            int ok = 0, all = 0;
            var bad = new List<string>();
            _failed.Clear();
            try
            {
                foreach (Transform w in ways.transform)
                {
                    var pts = w.Find("歩く点");
                    if (!pts || pts.childCount < 2) continue;
                    all++;
                    string fail = Walk(cc, pts);
                    if (fail == null) ok++; else { bad.Add($"{w.name}: {fail}"); _failed.Add(w.name); }
                }
            }
            finally { Object.DestroyImmediate(go); }
            sb.AppendLine($"ways walked up: {ok} / {all}");
            foreach (var b in bad) sb.AppendLine("  " + b);
            if (stairs)
            {
                int blocks = 0, badBlocks = 0, okStairs = 0, allStairs = 0, nRise = 0, nRoom = 0, nTop = 0;
                string firstBad = "";
                foreach (Transform s in stairs.transform)
                {
                    allStairs++;
                    bool good = true;
                    for (int i = 0; i < s.childCount; i++)
                    {
                        blocks++;
                        var b = s.GetChild(i);
                        float below = i == 0 ? _t.SampleHeight(b.position + b.forward * 0.4f) + _t.transform.position.y : s.GetChild(i - 1).position.y;
                        bool bad1 = b.position.y - below > 0.45f;
                        // the body on this block's top, at the inner half of the tread (where the next block is beside it)
                        var stand = b.position - b.forward * 0.42f;
                        bool bad2 = i < s.childCount - 1 && Physics.CheckCapsule(stand + Vector3.up * 0.21f, stand + Vector3.up * 0.41f, 0.16f, ~0, QueryTriggerInteraction.Ignore);
                        bool bad3 = !Physics.Raycast(stand + Vector3.up * 1.5f, Vector3.down, out var hit, 2f, ~0, QueryTriggerInteraction.Ignore) || hit.transform != b;
                        if (bad1) nRise++;
                        if (bad2) nRoom++;
                        if (bad3) nTop++;
                        if (bad1 || bad2 || bad3)
                        {
                            badBlocks++; good = false;
                            if (firstBad.Length < 600) firstBad += $"\n  {s.name} {i}: {(bad1 ? $"rise {b.position.y - below:0.00} " : "")}{(bad2 ? "no room " : "")}{(bad3 ? $"top is {(hit.transform ? hit.transform.name : "nothing")} at {hit.point.y - b.position.y:0.00}" : "")}";
                        }
                    }
                    if (good) okStairs++; else _failed.Add(s.name);
                }
                sb.AppendLine($"climbing stairs whole: {okStairs} / {allStairs} (blocks {blocks}, bad {badBlocks}: too high {nRise}, no room {nRoom}, something on top {nTop}){firstBad}");
            }
            return sb.ToString();
        }

        static string Walk(CharacterController cc, Transform pts)
        {
            cc.enabled = false;
            cc.transform.position = pts.GetChild(0).position + Vector3.up * 0.3f;
            cc.enabled = true;
            Physics.SyncTransforms();
            for (int i = 1; i < pts.childCount; i++)
            {
                var target = pts.GetChild(i).position;
                float floor = Mathf.Min(target.y, pts.GetChild(i - 1).position.y) - 1.2f;
                float best = float.MaxValue; int since = 0;
                for (int step = 0; step < 4000; step++)
                {
                    var p = cc.transform.position;
                    var d = target - p; d.y = 0f;
                    float dist = d.magnitude;
                    if (dist < 0.3f) break;
                    if (dist < best - 0.01f) { best = dist; since = 0; }
                    else if (++since > 80) return $"止まった（点 {i}、{p.x:0.0} {p.y:0.0} {p.z:0.0}）";
                    cc.Move(d / dist * 0.04f + Vector3.down * 0.06f);
                    if (cc.transform.position.y < floor) return $"落ちた（点 {i}、{p.x:0.0} {p.y:0.0} {p.z:0.0}）";
                }
            }
            var end = pts.GetChild(pts.childCount - 1).position;
            if (cc.transform.position.y < end.y - 0.6f) return $"上の棚に出ない（{cc.transform.position.y:0.0} m、棚は {end.y:0.0} m）";
            return null;
        }

        // ------------------------------------------------------------------ a map of the ways up
        /// <summary>
        /// A picture of the mountain from straight above (Screenshots/CairnTry/tiers_map.png): yellow, the cairns at the
        /// foot and head of each way up; orange, the foot of each climbing stair; blue, the summit; teal, the false summit.
        /// </summary>
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Salt Tiers Map")]
        public static string MapMenu() => CairnTryShots.WithIsland(Map, false);

        public static string Map() => Map("tiers_map", true);

        static string Map(string file, bool marks)
        {
            _t = Object.FindAnyObjectByType<Terrain>();
            var w = Object.FindAnyObjectByType<SlugWorld.WeatherPresenter>();
            if (!_t || !w) return "open IslandTest_800 first";
            if (!w.sun) foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional) { w.sun = l; break; }
            bool air = w.airLook; w.airLook = false;
            w.ShowNow(13f, SlugWorld.IslandSettings.DevSky.Sunny);
            const int px = 1200; const float half = 190f;
            var go = new GameObject("MapCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.enabled = false; cam.orthographic = true; cam.orthographicSize = half; cam.farClipPlane = 2000f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = w.SkyColor;
            go.transform.SetPositionAndRotation(new Vector3(Center.x, 900f, Center.y), Quaternion.Euler(90f, 0f, 0f));
            var rt = RenderTexture.GetTemporary(px, px, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(px, px, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, px, px), 0, 0);
            RenderTexture.active = null; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(go);
            w.airLook = air;
            System.Func<Vector3, Vector2Int> Px = p => new Vector2Int(Mathf.RoundToInt((p.x - Center.x + half) / (2f * half) * px), Mathf.RoundToInt((p.z - Center.y + half) / (2f * half) * px));
            if (marks)
            {
                var mk = GameObject.Find(Root + "/登り口の道しるべ");
                if (mk) foreach (Transform c in mk.transform) Dot(tex, Px(c.position), new Color(1f, 0.85f, 0.1f), 6);
                var st = GameObject.Find(Root + "/よじ登る段");
                if (st) foreach (Transform s in st.transform) if (s.childCount > 0) Dot(tex, Px(s.GetChild(0).position), new Color(1f, 0.5f, 0.1f), 5);
                Dot(tex, Px(new Vector3(Center.x, 0f, Center.y)), new Color(0.1f, 0.1f, 0.6f), 9);
                Dot(tex, Px(new Vector3((Center + FalseRel).x, 0f, (Center + FalseRel).y)), new Color(0.1f, 0.5f, 0.6f), 8);
            }
            tex.Apply();
            Directory.CreateDirectory("Screenshots/CairnTry");
            File.WriteAllBytes($"Screenshots/CairnTry/{file}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return $"Screenshots/CairnTry/{file}.png";
        }

        /// <summary>
        /// The ways you can pass, in red: the worn paths (IT_Path: the feet and heads of the ways up, and the roads) turn
        /// red for a moment, the ground is baked again, the mountain is shot from straight above and from four sides
        /// (Screenshots/CairnTry/ways_red_*.png), then everything goes back to brown.
        /// </summary>
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Salt Tiers Red Ways")]
        public static string RedWaysMenu() => CairnTryShots.WithIsland(RedWays, false);

        public static string RedWays()
        {
            _t = Object.FindAnyObjectByType<Terrain>();
            if (!_t) return "open IslandTest_800 first";
            Texture2D pathTex = null;
            foreach (var l in _t.terrainData.terrainLayers) if (l && l.name == "IT_Path") pathTex = l.diffuseTexture;
            if (!pathTex) return "no path layer";
            var keep = pathTex.GetPixels();
            var red = new Color[keep.Length];
            for (int i = 0; i < red.Length; i++) red[i] = new Color(0.86f, 0.08f, 0.06f, keep[i].a);
            var sb = new StringBuilder();
            try
            {
                pathTex.SetPixels(red); pathTex.Apply();
                IslandToonGround.On();
                sb.AppendLine(Map("ways_red_top", false));
                var c3 = new Vector3(Center.x, 0f, Center.y);
                var views = new (string name, Vector3 from)[] { ("ne", new Vector3(220, 160, 230)), ("se", new Vector3(230, 170, -260)), ("sw", new Vector3(-240, 170, -230)), ("nw", new Vector3(-250, 160, 230)) };
                foreach (var v in views)
                    sb.AppendLine(CairnTryShots.ShotAt("ways_red_" + v.name, c3 + v.from, c3 + new Vector3(0f, 50f, 0f), 13f, 40f, false));
            }
            finally
            {
                pathTex.SetPixels(keep); pathTex.Apply();
                EditorUtility.SetDirty(pathTex);
                IslandToonGround.On();
            }
            return sb.ToString();
        }

        static void Dot(Texture2D t, Vector2Int c, Color col, int r)
        {
            for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r * r && c.x + x >= 0 && c.y + y >= 0 && c.x + x < t.width && c.y + y < t.height)
                    t.SetPixel(c.x + x, c.y + y, x * x + y * y > (r - 2) * (r - 2) ? Color.black : col);
        }

        // ------------------------------------------------------------------ the things on the mountain, and putting it back
        static IEnumerable<Transform> Things()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name.StartsWith("ボット") && r.name != "ボット用の岩塩") { yield return r.transform; continue; }
                if (r.name == "ボット用の岩塩") { foreach (Transform c in r.transform) yield return c; continue; }
                if (r.name != "Map") continue;
                foreach (Transform g in r.transform)
                {
                    if (g.name.StartsWith("Label_")) yield return g;
                    else if (g.name == "隠れる物") { foreach (Transform kind in g) foreach (Transform c in kind) yield return c; }
                    else if (g.name == "Island") { foreach (Transform c in g) if (c.name.StartsWith("RockSalt")) yield return c; }
                }
            }
        }

        static readonly string[] MountainWays = { "肩の道（山をひと回り）", "尾根道（北の尾根）", "尾根道（東の尾根）", "尾根道（南の尾根）", "尾根道（西の尾根）" };

        static IEnumerable<Transform> Ways()
        {
            // (the island's own Map: another open scene may have one too)
            GameObject map = null;
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) if (r.name == "Map") map = r;
            if (!map) yield break;
            foreach (Transform g in map.transform)
                if (g.name == "道")
                    foreach (Transform c in g)
                        if (System.Array.IndexOf(MountainWays, c.name) >= 0) yield return c;
        }

        static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name + "#" + t.GetSiblingIndex() : t.name;
        static string V(Vector3 v) => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R}", v.x, v.y, v.z);
        static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

        static float[,] _old;

        static void SaveBackup()
        {
            var data = _t.terrainData;
            int res = data.heightmapResolution;
            _old = data.GetHeights(0, 0, res, res);
            var sb = new StringBuilder();
            sb.AppendLine(res.ToString());
            for (int j = 0; j < res; j++)
            {
                var row = new string[res];
                for (int i = 0; i < res; i++) row[i] = _old[j, i].ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                sb.AppendLine(string.Join(",", row));
            }
            foreach (var t in Things()) sb.AppendLine($"T|{Path(t)}|{V(t.position)}|{(t.gameObject.activeSelf ? 1 : 0)}");
            foreach (var t in Ways()) sb.AppendLine($"T|{Path(t)}|{V(t.position)}|{(t.gameObject.activeSelf ? 1 : 0)}");
            File.WriteAllText(Backup, sb.ToString());
            int ar = data.alphamapResolution, layers = data.alphamapLayers;
            var a = data.GetAlphamaps(0, 0, ar, ar);
            var bytes = new byte[ar * ar * layers * 4];
            int o = 0;
            for (int j = 0; j < ar; j++) for (int i = 0; i < ar; i++) for (int l = 0; l < layers; l++) { System.BitConverter.GetBytes(a[j, i, l]).CopyTo(bytes, o); o += 4; }
            File.WriteAllBytes(Backup + ".alpha", bytes);
        }

        static float OldGround(Vector3 p)
        {
            var data = _t.terrainData;
            Vector3 tp = _t.transform.position, size = data.size;
            int res = data.heightmapResolution;
            float fx = Mathf.Clamp01((p.x - tp.x) / size.x) * (res - 1), fz = Mathf.Clamp01((p.z - tp.z) / size.z) * (res - 1);
            int i = Mathf.Clamp((int)fx, 0, res - 2), j = Mathf.Clamp((int)fz, 0, res - 2);
            float u = fx - i, v = fz - j;
            return tp.y + size.y * Mathf.Lerp(Mathf.Lerp(_old[j, i], _old[j, i + 1], u), Mathf.Lerp(_old[j + 1, i], _old[j + 1, i + 1], u), v);
        }

        static string MoveThings()
        {
            int moved = 0, hidden = 0;
            foreach (var t in Things())
            {
                var p = t.position;
                float d = Ground(new Vector2(p.x, p.z)) - OldGround(p);
                if (Mathf.Abs(d) > 0.02f) { t.position = p + Vector3.up * d; moved++; }
            }
            foreach (var t in Ways()) { t.gameObject.SetActive(false); hidden++; }
            return $"things moved {moved}, ways hidden {hidden}";
        }

        public static string Remove()
        {
            _t = Object.FindAnyObjectByType<Terrain>();
            var old = GameObject.Find(Root);
            if (old) Object.DestroyImmediate(old);
            if (!_t || !File.Exists(Backup)) return "nothing to put back";
            var lines = File.ReadAllLines(Backup);
            int res = int.Parse(lines[0]);
            var hm = new float[res, res];
            for (int j = 0; j < res; j++)
            {
                var parts = lines[1 + j].Split(',');
                for (int i = 0; i < res; i++) hm[j, i] = F(parts[i]);
            }
            _t.terrainData.SetHeights(0, 0, hm);
            if (File.Exists(Backup + ".alpha"))
            {
                var d = _t.terrainData;
                int ar = d.alphamapResolution, layers = d.alphamapLayers;
                var bytes = File.ReadAllBytes(Backup + ".alpha");
                if (bytes.Length == ar * ar * layers * 4)
                {
                    var a = new float[ar, ar, layers];
                    int o = 0;
                    for (int j = 0; j < ar; j++) for (int i = 0; i < ar; i++) for (int l = 0; l < layers; l++) { a[j, i, l] = System.BitConverter.ToSingle(bytes, o); o += 4; }
                    d.SetAlphamaps(0, 0, a);
                }
                File.Delete(Backup + ".alpha");
            }
            _t.Flush();
            var byPath = new Dictionary<string, Transform>();
            foreach (var t in Things()) byPath[Path(t)] = t;
            foreach (var t in Ways()) byPath[Path(t)] = t;
            int n = 0;
            foreach (var line in lines)
            {
                if (!line.StartsWith("T|")) continue;
                var parts = line.Split('|');
                if (!byPath.TryGetValue(parts[1], out var t)) continue;
                var v = parts[2].Split(',');
                t.position = new Vector3(F(v[0]), F(v[1]), F(v[2]));
                t.gameObject.SetActive(parts[3] == "1");
                n++;
            }
            File.Delete(Backup);
            return $"mountain put back ({n} things)";
        }
    }
}
