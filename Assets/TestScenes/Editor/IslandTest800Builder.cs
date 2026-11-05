using System.Collections.Generic;
using System.Text;
using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Builds IslandTest_800: the gray island at its decided size, 800 m from east to west (docs/spec.md ch. 3,
    /// discussions/island_height_discussion.md), shaped from the height talk of Oct 8-9 rather than scaled from the
    /// 1.5 km sketch. One terrain for the whole island, its shape worked out in the map drawing's units
    /// (island_map.svg) and made natural with a wobble of every line, rain erosion (droplets on a coarse grid, the
    /// change laid over the fine heights) and walls of layered rock:
    /// - the rock-salt mountain in the middle (113 m), a little steeper to the north and west, with benches (flat
    ///   spots) in patches every 19 m of height, ridges and gullies; a pool on a south bench feeds the south river
    /// - the desert as low as the meadow, with dunes the east wind lined up; a red mesa (50 m) east of it, split by the
    ///   dry canyon (200 to 350 m from the west coast, about 45 m deep at its dead end) and four buttes west of it
    /// - the north: a cinder cone (rim 55 m, a crater 15 m deep) open to the east-north-east, three lava flows running
    ///   out of the breach to the sea (rough aa, smooth pahoehoe with a lava tube under it and skylights), islands of
    ///   old forest between them (kipuka), a low sea cliff and one black sand cove; the cone's west flank is cut by
    ///   the sea
    /// - the rain forest rolling in the east and south, the low flower meadow with its fairy ring, the salt flat
    /// Gray stand-ins for the landmarks, and the viewpoints of the talk as markers (Shoot Views renders them at the
    /// slug's eye).
    /// </summary>
    public static partial class IslandTest800Builder
    {
        const string ScenePath = "Assets/Scenes/Game/IslandTest_800.unity";
        static string TerrainPath => MoveTestBuilder.GenDir + "/IslandTest_800_Terrain.asset";
        const string ShotDir = "Screenshots/Island800";

        // ---- 数字（m）。変えたら Slug/IslandTest 800 m/Build Scene で作り直す（手で直した地形は消える）
        const float MountainTop = 113f;         // 岩塩の山の頂上
        const float MountainFoot = 190f;        // 山のすその広がり（東と南は少し広く、北は少し狭い）
        const float BenchStep = 19f;            // 平場の間隔（高さ）
        const float BenchFlat = 0.3f;           // 平場の広さ（1段のうち平らな割合）
        const float Low = 8f;                   // 低い土地・草原・砂漠
        const float Dune = 3f;                  // 砂丘の高さ
        const float Mesa = 50f;                 // 枯れた谷の両側の台地
        const float CanyonFloorWidth = 24f;     // 谷底の幅（口のあたり。奥は半分くらいに細る）
        const float CanyonFloorRise = 6f;       // 谷底の上り（入口から奥まで）
        const float ConeRim = 55f;              // 火口の丘の縁
        const float CraterDepth = 15f;          // 火口のすり鉢の深さ
        const float ConeSlope = 0.62f;          // 火口の丘の斜面（tan 32°）
        const float LavaField = 11f;            // 溶岩原の高さ
        const float SeaCliff = 8f;              // 溶岩原の北の海の崖
        const float ForestRoll = 12f;           // 森のうねり
        const float Warp = 1f;                  // 形のゆらぎ
        const int Drops = 25000;                // 雨で削る（しずくの数）

        const float IslandWidth = 800f, K = IslandWidth / 452f;   // metres per unit of the map drawing
        const float Size = 960f, MaxHeight = 160f, Bottom = -20f;
        const int Res = 1025, AlphaRes = 512;

        // ---- the map drawing (island_map.svg units: x to the east, y to the south)
        static readonly float[] IslandSvg = { 150,215, 175,188, 230,178, 300,180, 305,145, 330,122, 380,113, 430,122, 458,150, 462,185, 502,186, 522,214, 562,228, 592,250, 586,282, 576,312, 522,326, 482,346, 452,372, 420,396, 380,406, 340,412, 300,402, 286,377, 250,368, 215,362, 185,348, 160,352, 150,330, 140,290 };
        static readonly float[] DesertSvg = { 150,215, 175,190, 230,180, 300,185, 345,200, 350,240, 345,262, 330,285, 300,297, 260,300, 225,305, 195,308, 165,300, 150,285, 143,262, 143,235 };
        static readonly float[] ForestSvg = { 400,195, 450,175, 502,186, 522,214, 562,228, 592,250, 586,282, 576,312, 522,326, 482,346, 452,372, 420,396, 380,406, 340,412, 300,402, 286,377, 300,345, 330,330, 362,312, 395,300, 400,272 };
        static readonly float[] MesaSvg = { 250,214, 272,206, 300,205, 325,210, 342,222, 348,245, 346,268, 338,284, 316,292, 290,293, 262,290, 242,282, 236,262, 240,236 };
        static readonly float[] CanyonSvg = { 330,258, 318,256, 295,252, 275,246, 255,240, 243,236.5f, 232,233 };   // from the dead end to the mouth
        static readonly float[] EastRiverSvg = { 452,203, 458,222, 466,240, 476,252, 488,262, 506,274, 522,264, 540,262, 558,260, 572,276, 590,288, 604,296 };
        static readonly float[] SouthRiverSvg = { 368,316, 361,325, 356,331, 345,338, 330,340, 305,344, 275,338, 238,332, 222,330 };
        static readonly float[] LavaZoneSvg = { 372,128, 380,113, 430,122, 458,150, 466,172, 458,196, 440,210, 420,200, 400,178, 385,160 };
        static readonly float[][] FlowSvg =
        {
            new float[] { 395,146, 400,135, 404,124, 407,110 },                  // north, aa
            new float[] { 395,146, 412,148, 430,150, 448,155, 468,160 },         // east, pahoehoe (the lava tube under it)
            new float[] { 395,146, 408,162, 420,178, 432,193, 438,203 },         // south-east, aa, its front ends in the forest
        };
        static readonly float[] FlowThick = { 6f, 4f, 7f };
        static readonly bool[] FlowRough = { true, false, true };
        static readonly float[] Buttes = { 222,268,8, 226,214,7, 204,246,5.5f, 214,292,6.5f };   // x, y, radius (units)
        static readonly float[] MesaBreaks = { 246,221, 270,292, 300,206 };                         // where the mesa's walls have surely fallen into a slope
        static readonly float[] Kipukas = { 420,133,11, 424,166,12, 449,180,14 };                  // x, y, radius (m)
        static readonly float[] Skylights = { 394,147, 410,148, 428,150, 446,155 };   // the first is the vent's collapse, the way into the lava tube from the breach (the fourth is now the inlet's, placed by IslandTest800Inlet.cs)
        static readonly Vector2 Cone = new Vector2(350f, 165f), Breach = new Vector2(0.92f, -0.39f).normalized;
        static readonly Vector2 MountainSvg = new Vector2(375f, 262f), MeadowSvg = new Vector2(346f, 372f), SaltSvg = new Vector2(210f, 330f);
        static readonly Vector2 PoolSvg = new Vector2(368f, 316f), SpringSvg = new Vector2(452f, 203f), CoveSvg = new Vector2(438f, 128f);
        static readonly Vector2 BanyanSvg = new Vector2(500f, 228f), ArchSvg = new Vector2(284f, 249f);

        static Vector2[] _island, _desert, _forest, _mesa, _canyon, _east, _south, _lavaZone;
        static Vector2[][] _flows;
        static float[] _canyonAt, _eastAt, _southAt;
        static float[][] _flowAt;
        static Rect _islandBox, _desertBox, _forestBox, _mesaBox, _lavaBox;
        static float _canyonFloorEnd, _poolH;
        static bool _prepared;

        /// <summary>A point of the drawing in the world (m, x east, z north).</summary>
        static Vector2 W(float sx, float sy) => new Vector2((sx - 366f) * K, (260f - sy) * K);
        static Vector2 W(Vector2 s) => W(s.x, s.y);
        /// <summary>A point of the drawing (the talk's map units) on the ground plane (x, z).</summary>
        public static Vector2 World(Vector2 svg) => W(svg);

        /// <summary>Region names by index (the paint's kinds; RegionAt's array uses the same order).</summary>
        public static readonly string[] RegionNames = { "森", "花の草原", "砂漠・浜", "赤い台地", "溶岩原・火口の丘", "岩塩の山", "枯れた谷の底", "川", "塩田", "菌輪" };

        /// <summary>How much a world point (x east, z north, m) belongs to each region, 0..1 each, in RegionNames' order
        /// (Nov 5, for Argon's plant placer docs/modeling/island_planting.md). All near 0 = the low ground between regions.
        /// Reads the drawing only (not the terrain), so わくさん's hand edits of the mountain don't change it.</summary>
        public static float[] RegionAt(float x, float z)
        {
            if (!_prepared) Prepare();
            var o = new float[Kinds];
            var s = Svg(x, z);
            Base(s.x, s.y, o, false);
            return o;
        }
        static Vector2 Svg(float x, float z) => new Vector2(x / K + 366f, 260f - z / K);

        static Vector2[] Pts(float[] a)
        {
            var p = new Vector2[a.Length / 2];
            for (int i = 0; i < p.Length; i++) p[i] = new Vector2(a[i * 2], a[i * 2 + 1]);
            return p;
        }

        static float[] Along(Vector2[] line)
        {
            var a = new float[line.Length];
            for (int i = 1; i < line.Length; i++) a[i] = a[i - 1] + (line[i] - line[i - 1]).magnitude;
            return a;
        }

        static Rect Box(Vector2[] pts, float margin)
        {
            Vector2 min = pts[0], max = pts[0];
            foreach (var p in pts) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            return Rect.MinMaxRect(min.x - margin, min.y - margin, max.x + margin, max.y + margin);
        }

        static void Prepare()
        {
            _island = Pts(IslandSvg); _desert = Pts(DesertSvg); _forest = Pts(ForestSvg); _mesa = Pts(MesaSvg);
            _canyon = Pts(CanyonSvg); _east = Pts(EastRiverSvg); _south = Pts(SouthRiverSvg); _lavaZone = Pts(LavaZoneSvg);
            _canyonAt = Along(_canyon); _eastAt = Along(_east); _southAt = Along(_south);
            _flows = new Vector2[FlowSvg.Length][]; _flowAt = new float[FlowSvg.Length][];
            for (int i = 0; i < FlowSvg.Length; i++) { _flows[i] = Pts(FlowSvg[i]); _flowAt[i] = Along(_flows[i]); }
            _islandBox = Box(_island, 40f); _desertBox = Box(_desert, 40f); _forestBox = Box(_forest, 40f);
            _mesaBox = Box(_mesa, 60f); _lavaBox = Box(_lavaZone, 30f);
            _prepared = true;
            _canyonFloorEnd = 0f; _poolH = float.NaN;
            float around = Base(330f, 258f, null, true);              // the ground round the canyon's dead end
            _canyonFloorEnd = Mathf.Max(Low, Mathf.Min(Low + CanyonFloorRise, around - 2f));
            PrepareCanyon();
            _poolH = Base(PoolSvg.x, PoolSvg.y, null, false);
            PrepareInlet();                                            // (IslandTest800Inlet.cs)
        }

        // ------------------------------------------------------------------ noise and shapes
        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        static float Hash(int i, int j)
        {
            unchecked
            {
                int h = i * 374761393 + j * 668265263;
                h = (h ^ (int)((uint)h >> 13)) * 1274126177;
                h ^= (int)((uint)h >> 16);
                return (uint)h / 4294967295f;
            }
        }

        static float Vn(float x, float y)
        {
            int i = Mathf.FloorToInt(x), j = Mathf.FloorToInt(y);
            float fx = x - i, fy = y - j, u = fx * fx * (3f - 2f * fx), v = fy * fy * (3f - 2f * fy);
            float a = Hash(i, j), b = Hash(i + 1, j), c = Hash(i, j + 1), d = Hash(i + 1, j + 1);
            return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
        }

        static float Fbm(float x, float y, int octaves, float seed)
        {
            float a = 0f, amp = 1f, f = 1f;
            for (int i = 0; i < octaves; i++) { a += (Vn(x * f + seed * 7.1f, y * f + seed * 3.3f) - 0.5f) * amp; f *= 2.03f; amp *= 0.5f; }
            return a;
        }

        static float Ridged(float x, float y, float seed) => 1f - Mathf.Abs(Fbm(x, y, 3, seed)) * 1.6f;
        static float Smax(float a, float b, float k) { float d = Mathf.Abs(a - b); return Mathf.Max(a, b) + (d < k ? (k - d) * (k - d) / (4f * k) : 0f); }

        /// <summary>Signed distance to a polygon of the drawing (m): + inside, - outside (-1000 when far off its box).</summary>
        static float Sd(float x, float y, Vector2[] poly, Rect box)
        {
            if (!box.Contains(new Vector2(x, y))) return -1000f;
            float d = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                float ax = poly[j].x, ay = poly[j].y, ex = poly[i].x - ax, ey = poly[i].y - ay, wx = x - ax, wy = y - ay;
                float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float dx = wx - ex * t, dy = wy - ey * t, q = dx * dx + dy * dy;
                if (q < d) d = q;
                if ((ay > y) != (poly[i].y > y) && x < ex * (y - ay) / ey + ax) inside = !inside;
            }
            d = Mathf.Sqrt(d) * K;
            return inside ? d : -d;
        }

        /// <summary>Distance to a line of the drawing (m), and how far along it the nearest point is (0 to 1).</summary>
        static float Line(float x, float y, Vector2[] line, float[] at, out float along)
        {
            float best = float.MaxValue;
            along = 0f;
            for (int i = 1; i < line.Length; i++)
            {
                float ax = line[i - 1].x, ay = line[i - 1].y, ex = line[i].x - ax, ey = line[i].y - ay;
                float t = Mathf.Clamp01(((x - ax) * ex + (y - ay) * ey) / (ex * ex + ey * ey));
                float dx = x - ax - ex * t, dy = y - ay - ey * t, q = dx * dx + dy * dy;
                if (q < best) { best = q; along = at[i - 1] + t * (at[i] - at[i - 1]); }
            }
            along /= at[at.Length - 1];
            return Mathf.Sqrt(best) * K;
        }

        // what a point is made of (the paint): 0 forest, 1 meadow, 2 sand, 3 red rock, 4 lava, 5 salt mountain,
        // 6 canyon floor, 7 river, 8 salt flat, 9 fairy ring
        const int Kinds = 10;

        /// <summary>The ground's height (m, the sea at 0) at a point of the drawing, before the rain erosion.</summary>
        static float Base(float sx, float sy, float[] o, bool noCanyon)
        {
            if (o != null) System.Array.Clear(o, 0, Kinds);
            float mx = sx * K, my = sy * K;
            float ux = sx + Fbm(mx * 0.008f, my * 0.008f, 3, 1) * 22f * Warp / K, uy = sy + Fbm(mx * 0.008f, my * 0.008f, 3, 2) * 22f * Warp / K;
            float inM = Sd(sx, sy, _island, _islandBox) + Fbm(mx * 0.025f, my * 0.025f, 3, 7) * 14f * Warp;
            if (inM < -30f) return Mathf.Max(-14f, inM * 0.12f);
            float land = Smooth(-2f, 2f, inM);

            // the low ground, a beach at the coast
            float n1 = Fbm(mx * 0.015f, my * 0.015f, 4, 3);
            float h = Low * Smooth(4f, 50f, inM) + 4f * n1 * Smooth(0f, 50f, inM) + 0.6f * Smooth(0f, 5f, inM);
            float beach = 1f - Smooth(3f, 14f, inM);

            // the rain forest rolls; the meadow stays low
            float fw = Smooth(-30f, 30f, Sd(ux, uy, _forest, _forestBox));
            float meE = Mathf.Sqrt((ux - MeadowSvg.x) * (ux - MeadowSvg.x) / (46f * 46f) + (uy - MeadowSvg.y) * (uy - MeadowSvg.y) / (34f * 34f));
            float low = 1f - Smooth(0.7f, 2f, meE);
            h += ForestRoll * Mathf.Max(0f, 0.45f + Fbm(mx * 0.008f, my * 0.008f, 4, 11) * 1.6f) * fw * (1f - low) * Smooth(10f, 100f, inM);

            // the desert: as low as the meadow, dunes lined up across the east wind
            float dw = Smooth(-35f, 35f, Sd(ux, uy, _desert, _desertBox)) * Smooth(0f, 20f, inM);
            if (dw > 0f)
            {
                float ph = (mx * 0.9f + my * 0.43f) * 0.085f + Fbm(mx * 0.01f, my * 0.01f, 3, 21) * 7f;
                float dune = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(ph), 2.5f) * (0.4f + 1.2f * Vn(mx * 0.02f + 5f, my * 0.02f + 5f));
                h = Lerp(h, Low * Smooth(4f, 50f, inM) + 1.5f * n1 + Dune * dune * Smooth(10f, 40f, inM), dw);
            }

            // the red mesa and the buttes the rain left west of it: walls of layered sandstone like the canyon's
            // (IslandTest800Canyon.cs), the mesa's here and there broken down into a slope that can be climbed
            float ms = 0f;
            {
                float msd = Sd(ux, uy, _mesa, _mesaBox);
                if (msd > -1000f)
                {
                    msd += Fbm(mx * 0.03f, my * 0.03f, 3, 41) * 14f * Warp;
                    float top = Mesa + 2f * Fbm(mx * 0.03f, my * 0.03f, 2, 43) - Low, foot = WallFoot(top);
                    float mt = Low + Lerp(WallFromFoot(msd + foot, top), Mathf.Clamp(top + msd * 0.65f, 0f, top), MesaBreak(sx, sy, mx, my));
                    ms = Smooth(-foot - 4f, -foot * 0.3f, msd);
                    if (mt > h) h = mt;
                }
                for (int i = 0; i < Buttes.Length; i += 3)
                {
                    float bx = Buttes[i], by = Buttes[i + 1];
                    // each butte leans its own way and is worn unevenly
                    float ba = Hash((int)bx, (int)by) * Mathf.PI, bc = Mathf.Cos(ba), bs = Mathf.Sin(ba), stretch = 0.5f + 0.4f * Hash((int)by, (int)bx);
                    float lx = (ux - bx) * bc + (uy - by) * bs, ly = -(ux - bx) * bs + (uy - by) * bc;
                    float bd = (Buttes[i + 2] - Mathf.Sqrt(lx * lx * stretch + ly * ly)) * K + Fbm(mx * 0.03f, my * 0.03f, 2, bx) * 10f + Fbm(mx * 0.08f, my * 0.08f, 2, by) * 4f;
                    float top = Mesa * (0.35f + 0.5f * Hash((int)bx + 7, (int)by)) - Low, foot = WallFoot(top);
                    if (bd < -foot - 2f) continue;
                    ms = Mathf.Max(ms, Smooth(-foot - 2f, 0f, bd));
                    float bt = Low + WallFromFoot(bd + foot, top);
                    if (bt > h) h = bt;
                }
            }

            // the lava field in the north-east: flows out of the cone's breach, kipuka, a low sea cliff, a black cove
            float lavaW = 0f, kipW = 0f;
            float lsd = Sd(ux, uy, _lavaZone, _lavaBox);
            float lz = lsd > -1000f ? Smooth(-20f, 20f, lsd + Fbm(mx * 0.03f, my * 0.03f, 3, 51) * 8f * Warp) : 0f;
            if (lz > 0f)
            {
                float field = SeaCliff + (LavaField - SeaCliff) * Smooth(0f, 60f, inM);
                float cove = 1f - Smooth(15f, 40f, Mathf.Sqrt((sx - CoveSvg.x) * (sx - CoveSvg.x) + (sy - CoveSvg.y) * (sy - CoveSvg.y)) * K);
                field = Lerp(field, LavaField * Smooth(2f, 45f, inM), cove) + 1.2f * Fbm(mx * 0.02f, my * 0.02f, 3, 57);
                float top = field;
                for (int i = 0; i < _flows.Length; i++)
                {
                    float d = Line(ux, uy, _flows[i], _flowAt[i], out float fr);
                    float half = 13f + 6f * Fbm(mx * 0.02f, my * 0.02f, 2, 81 + i), edge = 3f + 3f * Vn(mx * 0.05f, my * 0.05f);
                    float inFlow = Smooth(half, half - edge, d);
                    if (inFlow <= 0f) continue;
                    float fh = field + FlowThick[i] * (0.7f + 0.5f * Vn(fr * 6f + i * 10f, 3f)) * inFlow;
                    if (FlowRough[i]) fh += 1.3f * Ridged(mx * 0.08f, my * 0.08f, 59) * inFlow;
                    top = Mathf.Max(top, fh);
                }
                for (int i = 0; i < Kipukas.Length; i += 3)
                {
                    float kd = Mathf.Sqrt((ux - Kipukas[i]) * (ux - Kipukas[i]) + (uy - Kipukas[i + 1]) * (uy - Kipukas[i + 1])) * K + Fbm(mx * 0.03f, my * 0.03f, 2, 61 + i) * 9f + Fbm(mx * 0.09f, my * 0.09f, 2, 67 + i) * 3f;
                    float kw = 1f - Smooth(Kipukas[i + 2] - 4f, Kipukas[i + 2] + 3f, kd);
                    if (kw > 0f) { top = Lerp(top, field + 1.5f + n1, kw); kipW = Mathf.Max(kipW, kw); }
                }
                for (int i = 0; i < 6; i += 2)            // (the fourth skylight is in the inlet's roof: IslandTest800Inlet.cs)
                {
                    float sd = Mathf.Sqrt((sx - Skylights[i]) * (sx - Skylights[i]) + (sy - Skylights[i + 1]) * (sy - Skylights[i + 1])) * K;
                    top -= 3.5f * (1f - Smooth(2.5f, 4.5f, sd));
                }
                h = Lerp(h, top, lz);
                lavaW = lz * (1f - kipW);
            }

            // the cinder cone: 32 degrees, a crater 15 m deep, open to the east-north-east down to the lava field
            {
                float cx = ux - Cone.x, cy = uy - Cone.y, dc = Mathf.Sqrt(cx * cx + cy * cy) * K, rimR = 32f, floorH = ConeRim - CraterDepth;
                if (dc < 140f)
                {
                    float cone = dc < rimR ? floorH + CraterDepth * (dc / rimR) * (dc / rimR) : ConeRim - (dc - rimR) * ConeSlope;
                    cone += 1.2f * Fbm(mx * 0.05f, my * 0.05f, 3, 63);
                    float along = (cx * Breach.x + cy * Breach.y) * K, side = Mathf.Abs(cx * Breach.y - cy * Breach.x) * K;
                    if (along > 0f)
                    {
                        float floorOut = floorH - Mathf.Max(0f, along - rimR * 0.6f) * 0.4f;
                        cone = Lerp(cone, Mathf.Min(cone, floorOut), 1f - Smooth(10f, 20f, side));
                    }
                    float cw0 = Smooth(-2f, 2f, cone - h);
                    h = Smax(h, cone, 6f);
                    lavaW = Mathf.Max(lavaW, cw0);
                }
            }

            // the rock-salt mountain: a cliff band round its top broken by four ridges, a shoulder round it (IslandTest800Mountain.cs)
            float mw;
            {
                float hm = MountainHeight(sx, sy, mx, my, out _);
                mw = Smooth(-3f, 3f, hm - h);
                h = Smax(h, hm, 8f);
            }

            // the dry canyon, cut in layered sandstone (IslandTest800Canyon.cs)
            float cw = 0f;
            if (!noCanyon) h = Canyon(ux, uy, mx, my, h, ms, out cw);

            // the pool on the mountain's south bench (the south river's source)
            if (!float.IsNaN(_poolH))
            {
                float pd = Mathf.Sqrt((sx - PoolSvg.x) * (sx - PoolSvg.x) + (sy - PoolSvg.y) * (sy - PoolSvg.y)) * K;
                h = Lerp(h, _poolH, 1f - Smooth(12f, 20f, pd));
                h -= 0.8f * (1f - Smooth(4f, 7f, pd));            // (the waterfall below it: IslandTest800Falls.cs)
            }

            // (the rivers are cut in after the rain erosion: IslandTest800Rivers.cs)

            // the salt flat where the south river ends, and the fairy ring in the meadow
            float se = Mathf.Sqrt((sx - SaltSvg.x) * (sx - SaltSvg.x) / (28f * 28f) + (sy - SaltSvg.y) * (sy - SaltSvg.y) / (15f * 15f));
            float salt = (1f - Smooth(0.85f, 1.1f, se)) * land;
            h = Lerp(h, 1.2f, salt);
            float basin = (1f - Smooth(1f, 2.6f, se)) * (1f - ms) * (1f - lavaW);       // the low ground slopes gently down to it
            h = Mathf.Min(h, Lerp(h, 1.2f + 5f * Smooth(1f, 2.6f, se), basin));
            float ring = (1f - Smooth(1.2f, 3f, Mathf.Abs(Mathf.Sqrt((sx - MeadowSvg.x) * (sx - MeadowSvg.x) + (sy - MeadowSvg.y) * (sy - MeadowSvg.y)) * K - 18f))) * low;

            if (o != null)
            {
                o[0] = Mathf.Max(fw * (1f - lavaW), kipW);
                o[1] = low;
                o[2] = Mathf.Max(dw, beach * (1f - lavaW));
                o[3] = ms;
                o[4] = lavaW;
                o[5] = mw;
                o[6] = cw;
                o[8] = salt;
                o[9] = ring;
            }
            // where the coast is a cliff, the waves have cut a rock platform at its foot (about 6 m wide, 1.2 m above the
            // sea): a way round the island under the cliffs, and where the lava tube's mouths open
            float cliffCoast = Smooth(2.5f, 4f, h) * (1f - Smooth(6f, 9f, inM));
            if (cliffCoast > 0f)
            {
                float pf = PlatformH + 0.25f * Fbm(mx * 0.1f, my * 0.1f, 2, 151);
                h = Lerp(h, Mathf.Min(h, pf), cliffCoast * (1f - Smooth(5f, 7.5f, inM)));
                land = Mathf.Max(land, cliffCoast * Smooth(-6f, -4f, inM));
                if (o != null) { o[2] = Mathf.Max(o[2], cliffCoast * (1f - Smooth(5f, 7.5f, inM))); }
            }
            return h * land + Mathf.Max(-14f, inM * 0.12f) * (1f - land);
        }

        // ------------------------------------------------------------------ rain erosion (on a coarse grid, as in the talk's tool)
        const int GW = 320, GH = 210;
        const float GU = 1.5f, GX0 = 130f, GY0 = 100f;   // grid step and corner, in drawing units (2.65 m a cell)
        static float[] _erosion;

        static void Erode()
        {
            var hg = new float[GW * GH];
            var h0 = new float[GW * GH];
            for (int j = 0; j < GH; j++)
                for (int i = 0; i < GW; i++)
                    hg[j * GW + i] = h0[j * GW + i] = Base(GX0 + i * GU, GY0 + j * GU, null, false);
            uint s = 12345;
            System.Func<float> rnd = () => { s = s * 1664525u + 1013904223u; return s / 4294967296f; };
            for (int k = 0; k < Drops; k++)
            {
                float x = 1f + rnd() * (GW - 3), y = 1f + rnd() * (GH - 3), dx = 0f, dy = 0f, sp = 1f, wt = 1f, sed = 0f;
                for (int l = 0; l < 45; l++)
                {
                    int i = (int)x, j = (int)y, a = j * GW + i;
                    float fx = x - i, fy = y - j;
                    Sample(hg, x, y, out float hh, out float gx, out float gy);
                    if (hh < 0.3f) break;
                    dx = dx * 0.15f - gx; dy = dy * 0.15f - gy;
                    float len = Mathf.Sqrt(dx * dx + dy * dy);
                    if (len < 1e-6f) break;
                    dx /= len; dy /= len;
                    float nx = x + dx, ny = y + dy;
                    if (nx < 1f || ny < 1f || nx >= GW - 2 || ny >= GH - 2) break;
                    Sample(hg, nx, ny, out float nh, out _, out _);
                    float dh = nh - hh, cap = Mathf.Max(-dh * sp * wt * 1.5f, 0.01f), amt;
                    if (sed > cap || dh > 0f) { amt = dh > 0f ? Mathf.Min(dh, sed) : (sed - cap) * 0.3f; sed -= amt; }
                    else { amt = -Mathf.Min(Mathf.Min((cap - sed) * 0.06f, -dh), 0.2f); sed -= amt; }
                    hg[a] += amt * (1f - fx) * (1f - fy); hg[a + 1] += amt * fx * (1f - fy); hg[a + GW] += amt * (1f - fx) * fy; hg[a + GW + 1] += amt * fx * fy;
                    sp = Mathf.Sqrt(Mathf.Max(0f, sp * sp - dh * 2f)); wt *= 0.97f; x = nx; y = ny;
                }
            }
            _erosion = new float[GW * GH];
            for (int j = 1; j < GH - 1; j++)
                for (int i = 1; i < GW - 1; i++)
                {
                    float sum = 0f;
                    for (int b = -1; b <= 1; b++) for (int a = -1; a <= 1; a++) { int n = (j + b) * GW + i + a; sum += hg[n] - h0[n]; }
                    _erosion[j * GW + i] = sum / 9f;
                }
        }

        static void Sample(float[] g, float x, float y, out float h, out float gx, out float gy)
        {
            int i = (int)x, j = (int)y, a = j * GW + i;
            float fx = x - i, fy = y - j, h00 = g[a], h10 = g[a + 1], h01 = g[a + GW], h11 = g[a + GW + 1];
            h = h00 * (1f - fx) * (1f - fy) + h10 * fx * (1f - fy) + h01 * (1f - fx) * fy + h11 * fx * fy;
            gx = (h10 - h00) * (1f - fy) + (h11 - h01) * fy;
            gy = (h01 - h00) * (1f - fx) + (h11 - h10) * fx;
        }

        static float ErosionAt(float sx, float sy)
        {
            float fi = (sx - GX0) / GU, fj = (sy - GY0) / GU;
            if (fi < 0f || fj < 0f || fi >= GW - 1 || fj >= GH - 1) return 0f;
            int i = (int)fi, j = (int)fj, a = j * GW + i;
            float fx = fi - i, fy = fj - j;
            return _erosion[a] * (1f - fx) * (1f - fy) + _erosion[a + 1] * fx * (1f - fy) + _erosion[a + GW] * (1f - fx) * fy + _erosion[a + GW + 1] * fx * fy;
        }

        /// <summary>The finished ground height at a world point (m, the sea at 0).</summary>
        static float Height(float x, float z) => InletCarve(x, z, HeightNoInlet(x, z));

        /// <summary>The ground before the inlet is cut into it (the top of the inlet's roof).</summary>
        static float HeightNoInlet(float x, float z)
        {
            var s = Svg(x, z);
            float h = Base(s.x, s.y, null, false) + (_erosion != null ? ErosionAt(s.x, s.y) : 0f);
            return Rivers(x, z, h, out _, out _);
        }

        // ------------------------------------------------------------------ the viewpoints of the talk (slug's eye 0.47 m)
        struct View { public string file, name; public Vector2 from, to; public float up, lookUp; }
        static readonly View[] Views =
        {
            new View { file = "01_saltflat", name = "塩田から北（台地の南の壁、岩山、谷の口は隠れる）", from = new Vector2(212, 322), to = new Vector2(250, 250), up = 0.47f, lookUp = 20f },
            new View { file = "02_desert", name = "砂漠の砂丘の谷間から東（岩山と台地）", from = new Vector2(180, 255), to = new Vector2(260, 250), up = 0.47f, lookUp = 15f },
            new View { file = "03_canyon_mouth", name = "枯れた谷の口から奥", from = new Vector2(228, 232), to = new Vector2(300, 252), up = 0.47f, lookUp = 12f },
            new View { file = "04_canyon_inside", name = "谷の中、石橋の手前から奥", from = new Vector2(258, 241), to = new Vector2(330, 258), up = 0.47f, lookUp = 15f },
            new View { file = "05_mesa_top", name = "台地の上から西の海", from = new Vector2(290, 222), to = new Vector2(150, 240), up = 0.47f, lookUp = -10f },
            new View { file = "06_meadow", name = "花の草原から山と滝", from = new Vector2(346, 378), to = new Vector2(368, 316), up = 0.47f, lookUp = 10f },
            new View { file = "07_pool", name = "中腹のたまり池のふちから南（草原と海）", from = new Vector2(372, 322), to = new Vector2(345, 400), up = 0.47f, lookUp = -50f },
            new View { file = "08_summit", name = "山頂から西（台地、砂漠、海）", from = new Vector2(365, 260), to = new Vector2(200, 245), up = 0.47f, lookUp = -70f },
            new View { file = "09_forest_river", name = "雨の森の東の川から上流（泉と山）", from = new Vector2(536, 270), to = new Vector2(452, 210), up = 0.47f, lookUp = 15f },
            new View { file = "10_cone_foot", name = "鞍部から北の火口の丘", from = new Vector2(362, 212), to = new Vector2(350, 165), up = 0.47f, lookUp = 30f },
            new View { file = "11_lava_field", name = "溶岩原の中から（流れのふちとキプカ）", from = new Vector2(440, 172), to = new Vector2(400, 145), up = 0.47f, lookUp = 5f },
            new View { file = "12_crater", name = "火口の中から切れ目", from = new Vector2(345, 168), to = new Vector2(400, 145), up = 0.47f, lookUp = 0f },
            new View { file = "13_east_forest_camp", name = "東の森のキャンプから山", from = new Vector2(500, 300), to = new Vector2(375, 262), up = 0.47f, lookUp = 60f },
            new View { file = "14_canyon_rim", name = "台地の北のふちから谷をのぞく", from = new Vector2(302, 236), to = new Vector2(306, 262), up = 0.47f, lookUp = -45f },
            new View { file = "15_canyon_floor_up", name = "谷底から壁を見上げる（奥の涸れ滝の手前）", from = new Vector2(312, 255), to = new Vector2(330, 258), up = 0.47f, lookUp = 25f },
        };

        // ------------------------------------------------------------------ build
        [MenuItem("Slug/IslandTest 800 m/Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Prepare();
            Erode();
            PrepareRivers();
            var moveSettings = MoveTestBuilder.LoadSettings();
            var scene = MoveTestBuilder.NewScene(new Vector2(60f, 60f));
            ExtractTestBuilder.PrepareAssets();
            var map = MoveTestBuilder._map;
            Random.InitState(800);

            var terrain = BuildTerrain(map);
            System.Func<Vector2, Vector3> on = v => { var w = W(v); return new Vector3(w.x, terrain.SampleHeight(new Vector3(w.x, 0f, w.y)) + Bottom, w.y); };

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var sea = MoveTestBuilder.SaveMaterial("IT_Sea", lit, new Color(0.36f, 0.62f, 0.66f));
            sea.SetFloat("_Smoothness", 0.85f);
            var water = MoveTestBuilder.SaveMaterial("W_Puddle", lit, new Color(0.2f, 0.3f, 0.38f));
            water.SetFloat("_Smoothness", 0.92f);
            var bark = MoveTestBuilder.SaveMaterial("W_Bark", lit, new Color(0.33f, 0.25f, 0.18f));
            var leaves = MoveTestBuilder.SaveMaterial("W_Leaves", lit, new Color(0.24f, 0.38f, 0.2f));
            var stone = MoveTestBuilder.SaveMaterial("IT_Stone", lit, new Color(0.62f, 0.5f, 0.4f));
            var props = new GameObject("Island").transform;
            props.SetParent(map, false);

            var seaGo = WeatherSceneSetup.Prim(props, "Sea", PrimitiveType.Cube, sea, new Vector3(0f, -0.05f, 0f), new Vector3(6000f, 0.1f, 6000f), 0f);
            Object.DestroyImmediate(seaGo.GetComponent<Collider>());

            // the rivers' water (IslandTest800Rivers.cs)
            BuildRiverWater(props, water, terrain);
            BuildFalls(map, terrain, water);
            BuildCaves(map, terrain);
            BuildRingBridges(props);
            {
                Vector3 pool = on(PoolSvg);
                var p = WeatherSceneSetup.Prim(props, "たまり池", PrimitiveType.Cylinder, water, new Vector3(pool.x, _poolH - 0.35f, pool.z), new Vector3(13f, 0.03f, 13f), 0f);
                Object.DestroyImmediate(p.GetComponent<Collider>());
                WeatherSceneSetup.Zone(props, "Pool_Zone", MoistureZone.Kind.Water, new Vector3(pool.x, _poolH, pool.z), new Vector3(14f, 4f, 14f));
                Vector3 spring = on(SpringSvg);
                var sp = WeatherSceneSetup.Prim(props, "溶岩のふちの泉", PrimitiveType.Cylinder, water, spring + Vector3.up * 0.9f, new Vector3(6f, 0.03f, 6f), 0f);
                Object.DestroyImmediate(sp.GetComponent<Collider>());
                WeatherSceneSetup.Zone(props, "Spring_Zone", MoistureZone.Kind.Water, spring + Vector3.up, new Vector3(8f, 4f, 8f));
            }
            WeatherSceneSetup.Zone(props, "SaltFlat_Salt", MoistureZone.Kind.Salt, on(SaltSvg) + Vector3.up * 3f, new Vector3(56f * K, 10f, 30f * K));

            // the giant banyan (the forest's landmark, 35 m): a trunk, a flat crown, roots hanging down as pillars
            {
                Vector3 bb = on(BanyanSvg);
                var banyan = new GameObject("大ガジュマル（仮）").transform;
                banyan.SetParent(props, false);
                WeatherSceneSetup.Prim(banyan, "Trunk", PrimitiveType.Cylinder, bark, bb + Vector3.up * 15f, new Vector3(6f, 15f, 6f), 0f);
                WeatherSceneSetup.Prim(banyan, "Crown", PrimitiveType.Sphere, leaves, bb + Vector3.up * 31f, new Vector3(56f, 12f, 56f), 0f);
                for (int i = 0; i < 24; i++)
                {
                    Vector2 r = BanyanSvg + Random.insideUnitCircle * (24f / K);
                    if ((r - BanyanSvg).magnitude * K < 5f) continue;
                    Vector3 g0 = on(r);
                    float top = bb.y + 27f;
                    WeatherSceneSetup.Prim(banyan, "Root", PrimitiveType.Cylinder, bark, new Vector3(g0.x, (g0.y + top) * 0.5f, g0.z), new Vector3(0.9f, (top - g0.y) * 0.5f, 0.9f), 0f);
                }
            }

            // the stone arch over the canyon, between the two halves of the mesa
            {
                Line(ArchSvg.x, ArchSvg.y, _canyon, _canyonAt, out float fr);
                int i = 0;
                while (i + 2 < _canyonAt.Length && _canyonAt[i + 1] / _canyonAt[_canyonAt.Length - 1] < fr) i++;
                Vector2 dir = (W(_canyon[i + 1]) - W(_canyon[i])).normalized;
                Vector2 at = W(ArchSvg), side = new Vector2(-dir.y, dir.x);
                float yaw = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
                float rimY = Mathf.Min(terrain.SampleHeight(new Vector3(at.x + side.x * 32f, 0f, at.y + side.y * 32f)), terrain.SampleHeight(new Vector3(at.x - side.x * 32f, 0f, at.y - side.y * 32f))) + Bottom;
                WeatherSceneSetup.Prim(props, "石橋（仮）", PrimitiveType.Cube, stone, new Vector3(at.x, rimY - 2.5f, at.y), new Vector3(70f, 3.5f, 6f), yaw);
            }

            RecordRoads(map);
            PlaceCover(map, terrain);

            // rock salt on the summit: the prize for getting up one of the four ridges
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f + 0.4f;
                var w = W(MountainSvg) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (2.5f + i);
                ExtractTestBuilder.SaltRock(props, new Vector3(w.x, terrain.SampleHeight(new Vector3(w.x, 0f, w.y)) + Bottom, w.y), 1.3f);
            }

            // labels
            void L(string text, Vector2 at, float up, float size) => MoveTestBuilder.Label(text, on(at) + Vector3.up * up, size);
            L("岩塩の山", MountainSvg, 18f, 22f);
            L("火口の丘", Cone, 12f, 18f);
            L("切れ目", Cone + Breach * 30f, 8f, 9f);
            L("黒い溶岩原", new Vector2(420, 150), 14f, 18f);
            for (int i = 0; i < Kipukas.Length; i += 3) L("キプカ", new Vector2(Kipukas[i], Kipukas[i + 1]), 6f, 7f);
            for (int i = 0; i < 6; i += 2) L("天窓", new Vector2(Skylights[i], Skylights[i + 1]), 4f, 5f);
            L("黒い砂の浜", CoveSvg, 5f, 8f);
            L("砂漠", new Vector2(190, 205), 10f, 18f);
            L("赤い台地", new Vector2(300, 215), 12f, 14f);
            L("枯れた谷", new Vector2(238, 236), 8f, 12f);
            L("涸れ滝", new Vector2(331, 258), 10f, 8f);
            L("雨の森", new Vector2(540, 300), 15f, 20f);
            L("大ガジュマル", BanyanSvg, 42f, 12f);
            L("花の草原（菌輪）", MeadowSvg, 6f, 14f);
            L("塩田", SaltSvg, 5f, 14f);
            L("中腹のたまり池", PoolSvg, 6f, 8f);
            L("溶岩のふちの泉", SpringSvg, 6f, 8f);
            L("浅瀬", FordSvg, 4f, 7f);

            // the viewpoints of the talk, as empty markers (double-click one in the Hierarchy to go there)
            var marks = new GameObject("見る場所").transform;
            marks.SetParent(map, false);
            foreach (var v in Views)
            {
                var m = new GameObject(v.file + " " + v.name).transform;
                m.SetParent(marks, false);
                Vector3 from = on(v.from) + Vector3.up * v.up, to = on(v.to) + Vector3.up * v.lookUp;
                m.position = from;
                m.rotation = Quaternion.LookRotation(to - from);
            }

            // a camp to set out from (east forest) and one to go home from (by the salt flat)
            var exit = ExtractTestBuilder.BuildExit(map, on(new Vector2(250, 318)));
            Vector3 spawn = on(new Vector2(500, 300)) + Vector3.up * 0.05f;
            var player = ExtractTestBuilder.BuildRaidPlayer(moveSettings, spawn, exit);
            Vector3 toMountain = on(MountainSvg) - spawn;
            player.transform.rotation = Quaternion.LookRotation(new Vector3(toMountain.x, 0f, toMountain.z));
            var cam = player.GetComponentInChildren<Camera>();
            if (cam) cam.farClipPlane = 3000f;
            var fast = player.AddComponent<DevFastCrawl>();
            fast.controller = player.GetComponent<SlugMoveController>();

            WeatherSceneSetup.Fit(false);
            var presenter = Object.FindAnyObjectByType<WeatherPresenter>();
            if (presenter) { presenter.fogScale = 0.25f; EditorUtility.SetDirty(presenter); }
            EditorSceneManager.SaveScene(scene, ScenePath);
            MoveTestBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[IslandTest800] scene saved: {ScenePath} ({watch.ElapsedMilliseconds} ms)");
        }

        static Terrain BuildTerrain(Transform map)
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
            if (!data) { data = new TerrainData(); AssetDatabase.CreateAsset(data, TerrainPath); }
            data.heightmapResolution = Res;
            data.size = new Vector3(Size, MaxHeight, Size);
            var heights = new float[Res, Res];
            var hm = new float[Res * Res];
            for (int iz = 0; iz < Res; iz++)
                for (int ix = 0; ix < Res; ix++)
                {
                    float x = -Size * 0.5f + Size * ix / (Res - 1f), z = -Size * 0.5f + Size * iz / (Res - 1f);
                    hm[iz * Res + ix] = FallGround(x, z, Height(x, z), out _, out _);
                }
            Swells(hm);                            // low swells on the mountain to drop behind (IslandTest800Bot.cs)
            var paths = CarveRoads(hm);            // the roads and the animal trails, cut into the ground
            PlanCaves(hm);                         // the caves (IslandTest800Caves.cs): planned on the finished ground
            for (int iz = 0; iz < Res; iz++)
                for (int ix = 0; ix < Res; ix++)
                    heights[iz, ix] = Mathf.Clamp01((hm[iz * Res + ix] - Bottom) / MaxHeight);
            data.SetHeights(0, 0, heights);
            PunchHoles(data, hm);

            var layers = new[]
            {
                Layer("IT_Forest", new Color(0.36f, 0.45f, 0.33f)),
                Layer("IT_Low", new Color(0.55f, 0.6f, 0.45f)),
                Layer("IT_Desert", new Color(0.78f, 0.7f, 0.55f)),
                Layer("IT_Salt", new Color(0.9f, 0.89f, 0.86f)),
                Layer("IT_Rock", new Color(0.62f, 0.62f, 0.6f)),
                Layer("IT_Lava", new Color(0.28f, 0.27f, 0.27f)),
                Layer("IT_Canyon", new Color(0.66f, 0.5f, 0.38f)),
                Layer("IT_Ring", new Color(0.22f, 0.34f, 0.2f)),
                Layer("IT_Water", new Color(0.3f, 0.42f, 0.48f)),
                Layer("IT_Path", new Color(0.52f, 0.44f, 0.34f)),
                Layer("IT_RedPale", new Color(0.86f, 0.74f, 0.6f)),
                Layer("IT_Gravel", new Color(0.52f, 0.5f, 0.43f)),
            };
            data.terrainLayers = layers;
            data.alphamapResolution = AlphaRes;
            var alpha = new float[AlphaRes, AlphaRes, layers.Length];
            var o = new float[Kinds];
            float cell = Size / (Res - 1f);
            for (int iz = 0; iz < AlphaRes; iz++)
                for (int ix = 0; ix < AlphaRes; ix++)
                {
                    float x = -Size * 0.5f + Size * ix / (AlphaRes - 1f), z = -Size * 0.5f + Size * iz / (AlphaRes - 1f);
                    var s = Svg(x, z);
                    Base(s.x, s.y, o, false);
                    int hx = Mathf.Clamp(Mathf.RoundToInt(ix * (Res - 1f) / (AlphaRes - 1f)), 1, Res - 2), hz = Mathf.Clamp(Mathf.RoundToInt(iz * (Res - 1f) / (AlphaRes - 1f)), 1, Res - 2);
                    float gx = (hm[hz * Res + hx + 1] - hm[hz * Res + hx - 1]) / (2f * cell), gz = (hm[(hz + 1) * Res + hx] - hm[(hz - 1) * Res + hx]) / (2f * cell);
                    float slope = Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg, rock = Smooth(32f, 46f, slope);
                    var w = new float[layers.Length];
                    Rivers(x, z, 1e6f, out float wet, out float gravel);
                    w[8] = wet;
                    w[11] = gravel * (1f - wet);
                    FallGround(x, z, 0f, out float fallRock, out float fallPool);
                    w[8] = Mathf.Lerp(w[8], fallPool, fallRock + fallPool);        // (the river's line runs on behind the lip, under the rock)
                    w[11] *= 1f - fallRock - fallPool;
                    w[4] = Mathf.Max(w[4], fallRock);
                    w[7] = o[9] * (1f - w[8]);
                    w[3] = o[8] * (1f - w[7]);
                    float red = o[3] * (1f - o[6]);          // the canyon's floor is sand
                    w[6] = red * (1f - w[3] - w[8]);
                    w[5] = o[4] * (1f - w[6] - w[3]);
                    w[4] = o[5] * (1f - o[2] * 0.6f) * (1f - w[5] - w[6]);
                    w[2] = Mathf.Max(o[2], o[6]) * (1f - w[4] - w[5] - w[6] - w[3]);
                    float rest = Mathf.Max(0f, 1f - w[2] - w[3] - w[4] - w[5] - w[6] - w[7] - w[8]);
                    w[1] = rest * Mathf.Max(o[1], 1f - o[0]);
                    w[0] = rest - w[1];
                    // steep ground shows its rock: red in the mesa and the canyon, black in the lava, gray elsewhere
                    if (rock > 0f)
                    {
                        // the red rock shows its strata: red and pale bands, about 5 m each, a little wavy
                        float hh = hm[hz * Res + hx], band = (hh + 3f * Fbm(x * 0.02f, z * 0.02f, 2, 97)) / 5f;
                        int to = red > 0.3f ? (Mathf.FloorToInt(band) % 3 == 1 ? 10 : 6) : o[4] > 0.5f ? 5 : 4;
                        for (int l = 0; l < w.Length; l++) w[l] *= 1f - rock * 0.85f;
                        w[to] += rock * 0.85f;
                    }
                    float sum = 1e-4f;
                    foreach (float v in w) sum += Mathf.Max(0f, v);
                    float path = paths[hz * Res + hx];
                    for (int l = 0; l < w.Length; l++) alpha[iz, ix, l] = Mathf.Max(0f, w[l]) / sum * (1f - path);
                    alpha[iz, ix, 9] += path;
                }
            data.SetAlphamaps(0, 0, alpha);
            EditorUtility.SetDirty(data);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(map, false);
            go.transform.position = new Vector3(-Size * 0.5f, Bottom, -Size * 0.5f);
            var terrain = go.GetComponent<Terrain>();
            var matPath = MoveTestBuilder.GenDir + "/RT_TerrainMat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            terrain.materialTemplate = mat;
            terrain.heightmapPixelError = 2f;
            terrain.basemapDistance = 3000f;
            terrain.Flush();
            return terrain;
        }

        static TerrainLayer Layer(string name, Color color)
        {
            string texPath = $"{MoveTestBuilder.GenDir}/{name}_Tex.asset", layerPath = $"{MoveTestBuilder.GenDir}/{name}.terrainlayer";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (!tex) { tex = new Texture2D(4, 4, TextureFormat.RGBA32, false); AssetDatabase.CreateAsset(tex, texPath); }
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(color.r, color.g, color.b, 0f);
            tex.SetPixels(px);
            tex.Apply();
            EditorUtility.SetDirty(tex);
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (!layer) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, layerPath); }
            layer.diffuseTexture = tex;
            layer.tileSize = new Vector2(4f, 4f);
            layer.smoothness = 0f;
            layer.metallic = 0f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        // ------------------------------------------------------------------ looking and measuring (the scene must be open)
        static Terrain OpenTerrain()
        {
            if (!_prepared) Prepare();
            var t = Object.FindAnyObjectByType<Terrain>();
            if (!t) Debug.LogError("[IslandTest800] open IslandTest_800 first");
            return t;
        }

        static Vector3 Ground(Terrain t, Vector2 svg) { var w = W(svg); return new Vector3(w.x, t.SampleHeight(new Vector3(w.x, 0f, w.y)) + t.transform.position.y, w.y); }

        /// <summary>Renders every viewpoint at the slug's eye, and three views from the air, into Screenshots/Island800.</summary>
        [MenuItem("Slug/IslandTest 800 m/Shoot Views")]
        public static string ShootViews()
        {
            var t = OpenTerrain();
            if (!t) return "no terrain";
            var sb = new StringBuilder();
            foreach (var v in Views)
            {
                Vector3 from = Ground(t, v.from) + Vector3.up * v.up, to = Ground(t, v.to) + Vector3.up * v.lookUp;
                sb.AppendLine(ShotUtil.Capture($"{ShotDir}/{v.file}.png", from, to, 60f, 1200, 600, 3000f) + "  " + v.name);
            }
            Vector3 c = Ground(t, new Vector2(366, 262));
            sb.AppendLine(ShotUtil.Capture($"{ShotDir}/air_south_east.png", c + new Vector3(380f, 260f, -440f), c + Vector3.up * 10f, 45f, 1400, 800, 4000f));
            sb.AppendLine(ShotUtil.Capture($"{ShotDir}/air_north_west.png", c + new Vector3(-420f, 250f, 380f), c + Vector3.up * 10f, 45f, 1400, 800, 4000f));
            Vector3 cy = Ground(t, new Vector2(282, 248));
            sb.AppendLine(ShotUtil.Capture($"{ShotDir}/air_canyon.png", cy + new Vector3(-90f, 85f, -110f), cy, 45f, 1400, 800, 4000f));
            sb.AppendLine(ShotUtil.Capture($"{ShotDir}/air_top.png", c + new Vector3(0f, 1100f, -0.1f), c, 45f, 1200, 1000, 4000f));
            return sb.ToString();
        }

        /// <summary>Crawls straight lines on the ground: length, time light and heavy, the steepest bit, the length steeper than 50°.</summary>
        [MenuItem("Slug/IslandTest 800 m/Measure Routes")]
        public static string MeasureRoutes()
        {
            var t = OpenTerrain();
            if (!t) return "no terrain";
            var routes = new List<(string name, Vector2[] pts)>
            {
                ("東の森のキャンプ → 山頂", new[] { new Vector2(500, 300), new Vector2(375, 262) }),
                ("花の草原 → たまり池 → 山頂", new[] { new Vector2(346, 372), new Vector2(368, 316), new Vector2(375, 262) }),
                ("塩田そばのキャンプ → 谷の口 → 谷の奥", new[] { new Vector2(250, 318), new Vector2(228, 232), new Vector2(243, 236.5f), new Vector2(275, 246), new Vector2(295, 252), new Vector2(318, 256), new Vector2(330, 258) }),
                ("火口の縁（南） → 鞍部 → 山頂", new[] { new Vector2(350, 183), new Vector2(359, 199), new Vector2(375, 262) }),
                ("火口の切れ目 → 溶岩原 → 入江", new[] { new Vector2(372, 155), new Vector2(395, 146), new Vector2(430, 150), new Vector2(442.5f, 153.6f) }),
                ("東の森のキャンプ → 菌輪", new[] { new Vector2(500, 300), new Vector2(346, 372) }),
                ("東の森のキャンプ → 南を回って → 塩田そばのキャンプ", new[] { new Vector2(500, 300), new Vector2(420, 360), new Vector2(330, 370), new Vector2(250, 318) }),
                ("東の海岸 → 山を越えて → 西の海岸", new[] { new Vector2(586, 262), new Vector2(375, 262), new Vector2(148, 262) }),
            };
            var sb = new StringBuilder("| 道 | 長さ | 軽い | 重い | いちばん急 | 50° より急 |\n| --- | --- | --- | --- | --- | --- |\n");
            foreach (var (name, pts) in routes)
            {
                float len = 0f, over = 0f, steep = 0f;
                Vector3 prev = Ground(t, pts[0]);
                for (int i = 0; i + 1 < pts.Length; i++)
                {
                    Vector2 a = W(pts[i]), b = W(pts[i + 1]);
                    int n = Mathf.CeilToInt((b - a).magnitude / 0.5f);
                    for (int k = 1; k <= n; k++)
                    {
                        Vector2 p = Vector2.Lerp(a, b, k / (float)n);
                        var q = new Vector3(p.x, t.SampleHeight(new Vector3(p.x, 0f, p.y)) + t.transform.position.y, p.y);
                        float flat = new Vector2(q.x - prev.x, q.z - prev.z).magnitude, seg = (q - prev).magnitude;
                        float deg = Mathf.Atan2(Mathf.Abs(q.y - prev.y), flat) * Mathf.Rad2Deg;
                        len += seg;
                        if (deg > steep) steep = deg;
                        if (deg > 50f) over += seg;
                        prev = q;
                    }
                }
                sb.AppendLine($"| {name} | {len:0} m | {len / 1.2f / 60f:0.0} 分 | {len / 0.36f / 60f:0.0} 分 | {steep:0}° | {over:0} m |");
            }
            return sb.ToString();
        }

        /// <summary>Heights at the named places of the talk (m).</summary>
        public static string Heights()
        {
            var t = OpenTerrain();
            if (!t) return "no terrain";
            var places = new (string, Vector2)[]
            {
                ("山頂", MountainSvg), ("火口の縁（南）", new Vector2(350, 183)), ("火口の底", Cone), ("鞍部", new Vector2(359, 199)),
                ("溶岩原", new Vector2(440, 172)), ("台地の上", new Vector2(290, 222)), ("谷底（口）", new Vector2(236, 234)), ("谷底（奥）", new Vector2(328, 258)),
                ("たまり池", PoolSvg), ("花の草原", MeadowSvg), ("塩田", SaltSvg), ("砂漠", new Vector2(180, 255)), ("森", new Vector2(540, 300)),
            };
            var sb = new StringBuilder();
            foreach (var (name, p) in places) sb.AppendLine($"{name}: {Ground(t, p).y:0.0} m");
            return sb.ToString();
        }
    }
}
