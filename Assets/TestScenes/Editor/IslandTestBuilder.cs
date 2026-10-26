using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Builds IslandTest: a gray prototype of the whole island, 1.5 km from east to west, as drawn in docs/island_map.svg
    /// (docs/spec.md ch. 3, discussions/seamless_island_discussion.md). One terrain for the whole island, its heights the
    /// spec's rough numbers: the rock-salt mountain in the middle (the highest, about 250 m; gentle to the wet east and
    /// south, steep to the dry west and the north), the lava plateau with its cold crater in the north (115 m, the rim
    /// 200 m), the high desert plateau in the west (150 m, lower toward the west coast) cut by a dry canyon an old river
    /// left (about 100 m deep), the rain forest in the east and south, the low flower meadow with its fairy ring and the
    /// salt flat where the south river ends, and the sea all round. The borders are cliffs steeper than the slug can
    /// climb (50 degrees); the crossings (the pass at the canyon's head, the ridge from the crater, the crumbled slope,
    /// the stone arch) are left climbable. Gray stand-ins for the landmarks. A sketch to crawl and look out from.
    /// </summary>
    public static class IslandTestBuilder
    {
        // the island is drawn at 1.5 km (the numbers below are for that size) and built at any width, everything scaled
        // together (heights too, so the slopes stay the same): 1500 m is IslandTest, smaller ones are IslandTest_300 etc.
        static float _width = 1500f;
        static float S => _width / IslandWidth;
        static string Suffix => _width >= IslandWidth ? "" : $"_{_width:0}";
        static string ScenePath => $"Assets/Scenes/Tests/IslandTest{Suffix}.unity";
        static string TerrainPath => $"{MoveTestBuilder.GenDir}/IslandTest{Suffix}_Terrain.asset";
        // ---- 数字（m）。変えたら Slug/IslandTest Build Scene で作り直す（手で直した地形は消える。docs/tech/island_test_notes.md）
        const float IslandWidth = 1500f;                       // 島の東西の幅（下の数字はこの幅のとき。小さい島は全部が同じ割合で縮む）
        const float MountainTop = 250f;                        // 岩塩の山の頂上
        const float MountainFootSteep = 230f, MountainFootGentle = 430f;   // 山のすその広がり（西と北／東と南。島の幅 1500 m のとき）
        const float LavaPlateau = 115f, CraterRim = 200f, CraterBowl = 150f;  // 溶岩台地、火口の縁、火口の中
        const float DesertPlateau = 150f, DesertWestEnd = 40f; // 砂漠の台地（東側）、西の海岸の近く。枯れた谷の深さは台地の高さに合わせて変わる
        const float MeadowHeight = 6f;                         // 花の草原と、そのまわりの低い土地
        const float ForestRise = 55f;                          // 森が山へ向かって上がる高さ

        static float Size => _width * 1.2f;
        const float MaxHeight = 300f, Bottom = -20f;   // the terrain reaches 20 m under the sea (y = 0)
        const int Res = 1025, AlphaRes = 512;
        const float K = IslandWidth / 452f;    // metres per unit of the map drawing (the island is 452 units wide there)
        const float Scale = 1f;   // (the shapes are worked out at 1.5 km; S scales them to the island being built)

        // ---- the map drawing (island_map.svg), in its own units (x to the east, y to the south)
        static readonly float[] IslandSvg = { 150,215, 175,188, 230,178, 300,180, 305,145, 330,122, 380,113, 430,122, 458,150, 462,185, 502,186, 522,214, 562,228, 592,250, 586,282, 576,312, 522,326, 482,346, 452,372, 420,396, 380,406, 340,412, 300,402, 286,377, 250,368, 215,362, 185,348, 160,352, 150,330, 140,290 };
        static readonly float[] DesertSvg = { 150,215, 175,190, 230,180, 300,185, 345,200, 350,240, 345,262, 330,285, 300,297, 260,300, 225,305, 195,308, 165,300, 150,285, 143,262, 143,235 };
        static readonly float[] ForestSvg = { 400,195, 450,175, 502,186, 522,214, 562,228, 592,250, 586,282, 576,312, 522,326, 482,346, 452,372, 420,396, 380,406, 340,412, 300,402, 286,377, 300,345, 330,330, 362,312, 395,300, 400,272 };
        static readonly float[] CanyonSvg = { 345,260, 318,256, 295,252, 275,246, 255,240, 238,235, 225,234, 212,236, 190,238, 170,232, 150,228, 136,226 };
        static readonly float[] CanyonFloor = { 128, 95, 45, 40, 36, 33, 31, 30, 22, 12, 4, -6 };
        static readonly float[] EastRiverSvg = { 432,258, 452,250, 470,250, 488,262, 506,274, 522,264, 540,262, 558,260, 572,276, 590,288, 600,294 };
        static readonly float[] SouthRiverSvg = { 362,318, 356,330, 345,338, 330,340, 305,344, 275,338, 238,332, 215,330 };
        static readonly float[] RidgeSvg = { 357,170, 368,190, 377,212, 377,228, 376,246 };
        static readonly float[] RidgeTop = { 192, 175, 168, 180, 208 };

        static Vector2[] _island, _desert, _forest, _canyon, _eastRiver, _southRiver, _ridge;
        static float[] _canyonAt, _ridgeAt, _eastAt, _southAt;          // the distance along each line to each of its points (m)
        static Vector2 _mountain, _lava, _crater, _meadow, _salt, _ramp, _ring, _banyan, _arch, _waterfall;
        static Rect _desertBox, _forestBox, _canyonBox;

        static Vector2 W(float sx, float sy) => new Vector2((sx - 366f) * K, (260f - sy) * K);

        static Vector2[] Poly(float[] svg)
        {
            var p = new Vector2[svg.Length / 2];
            for (int i = 0; i < p.Length; i++) p[i] = W(svg[i * 2], svg[i * 2 + 1]);
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
            _island = Poly(IslandSvg); _desert = Poly(DesertSvg); _forest = Poly(ForestSvg);
            _canyon = Poly(CanyonSvg); _eastRiver = Poly(EastRiverSvg); _southRiver = Poly(SouthRiverSvg); _ridge = Poly(RidgeSvg);
            _canyonAt = Along(_canyon); _ridgeAt = Along(_ridge); _eastAt = Along(_eastRiver); _southAt = Along(_southRiver);
            _mountain = W(375, 262); _lava = W(380, 168); _crater = W(350, 155); _meadow = W(346, 372);
            _salt = W(210, 330); _ramp = W(240, 303); _ring = _meadow; _banyan = W(500, 228); _arch = W(262, 242);
            _waterfall = W(358, 324);
            _desertBox = Box(_desert, 260f); _forestBox = Box(_forest, 60f); _canyonBox = Box(_canyon, 120f);
        }

        // ------------------------------------------------------------------ shapes
        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        /// <summary>Signed distance to a polygon (m): + inside, - outside.</summary>
        static float SdPoly(Vector2 p, Vector2[] poly)
        {
            float d = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                Vector2 a = poly[j], b = poly[i], e = b - a, w = p - a;
                float t = Mathf.Clamp01(Vector2.Dot(w, e) / e.sqrMagnitude);
                d = Mathf.Min(d, (w - e * t).sqrMagnitude);
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            d = Mathf.Sqrt(d);
            return inside ? d : -d;
        }

        static float SdPoly(Vector2 p, Vector2[] poly, Rect box) => box.Contains(p) ? SdPoly(p, poly) : -1000f;

        /// <summary>Distance to a line of points (m), and how far along the line the nearest point is.</summary>
        static float DistLine(Vector2 p, Vector2[] line, float[] at, out float along)
        {
            float best = float.MaxValue;
            along = 0f;
            for (int i = 0; i + 1 < line.Length; i++)
            {
                Vector2 a = line[i], e = line[i + 1] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, e) / e.sqrMagnitude);
                float d = (p - a - e * t).sqrMagnitude;
                if (d < best) { best = d; along = at[i] + t * (at[i + 1] - at[i]); }
            }
            return Mathf.Sqrt(best);
        }

        /// <summary>A value given at each point of a line, read at a distance along it.</summary>
        static float AlongValue(float[] at, float[] values, float along)
        {
            for (int i = 0; i + 1 < at.Length; i++)
                if (along <= at[i + 1]) return Mathf.Lerp(values[i], values[i + 1], Mathf.InverseLerp(at[i], at[i + 1], along));
            return values[values.Length - 1];
        }

        static float Noise(float x, float z, float freq, float seed) => Mathf.PerlinNoise(x * freq + seed, z * freq + seed * 1.7f) - 0.5f;

        /// <summary>What a point of the island is: its height (m, the sea at 0) and how much of each kind of ground it is.</summary>
        struct Ground
        {
            public float h, forest, meadow, desert, salt, rock, lava, canyon, ring;
        }

        static Ground Sample(float x, float z)
        {
            var g = new Ground();
            var p = new Vector2(x, z);
            float inIsland = SdPoly(p, _island);
            float land = Smooth(-30f, 20f, inIsland);

            // the low ground: a gently shelving sea floor, a long flat beach, a little higher inland
            float h = Mathf.Clamp(inIsland * (inIsland >= 0f ? 0.04f : 0.07f), -16f, 4f) + 0.3f
                      + land * Smooth(30f, 220f, inIsland) * (5f + 6f * Noise(x, z, 0.006f, 11f));
            float coast = Smooth(0f, 180f, inIsland);       // the forest and the meadow rise slowly from the beach

            // the rain forest: rolling, rising toward the mountain
            float dM = (p - _mountain).magnitude;
            float forest = Smooth(-50f, 50f, SdPoly(p, _forest, _forestBox)) * land;
            float meadowE = ((p - _meadow) / (new Vector2(46f, 34f) * K)).magnitude;
            float low = 1f - Smooth(0.6f, 2.3f, meadowE);                 // the meadow and round it: low ground
            h += forest * coast * (8f + 14f * Noise(x, z, 0.012f, 3f) + ForestRise * (1f - Smooth(200f * Scale, 520f * Scale, dM)) * (1f - low));

            // the rock-salt mountain: gentle to the wet east and south, steep to the dry west and the north
            Vector2 d = p - _mountain;
            float ang = Mathf.Atan2(d.y, d.x);
            float gentle = Mathf.Max(Mathf.Max(0f, Mathf.Cos(ang)), 0.9f * Mathf.Max(0f, -Mathf.Sin(ang)));
            // ridges and gullies running down from the top (a wobble of the reach by direction), and a rough surface
            float wobble = Mathf.PerlinNoise(Mathf.Cos(ang) * 1.8f + 21f + dM * 0.002f, Mathf.Sin(ang) * 1.8f + 37f) - 0.5f;   // round the circle without a seam
            float reach = Mathf.Lerp(MountainFootSteep, MountainFootGentle, gentle) * Scale * (1f + 0.3f * wobble);
            float up = 1f - Smooth(0f, 1f, dM / reach);
            float mountain = (MountainTop * up + 18f * Noise(x, z, 0.012f, 7f) * up * (1f - up) * 4f) * land;
            if (dM < reach && mountain > h) { g.rock = Smooth(-10f, 15f, mountain - h); h = mountain; }

            // the lava plateau with the cold crater (the edge is a wall, softer to the east where the forest came back)
            Vector2 q = p - _lava;
            float e = Mathf.Sqrt(q.x * q.x / (80f * K * 80f * K) + q.y * q.y / (55f * K * 55f * K));
            float soft = Mathf.Lerp(0.03f, 0.25f, Mathf.Max(0f, q.x) / (80f * K));
            float lavaMask = (1f - Smooth(1f - soft * 0.5f, 1f + soft, e)) * land;
            if (lavaMask > 0f)
            {
                float dCr = (p - _crater).magnitude, rim = 20f * K;
                float crater = dCr < rim ? CraterBowl + (CraterRim - CraterBowl) * (dCr / rim) * (dCr / rim) : CraterRim - (CraterRim - LavaPlateau) * Smooth(0f, 90f * Scale, dCr - rim);
                float lavaH = Mathf.Max(LavaPlateau + 8f * Noise(x, z, 0.03f, 5f), crater);
                float before = h;
                h = Mathf.Max(h, Mathf.Lerp(h, lavaH, lavaMask));
                g.lava = h > before + 1f ? lavaMask : 0f;
            }
            // the ridge from the crater's rim to the mountain (the way across)
            float dR = DistLine(p, _ridge, _ridgeAt, out float alongR);
            if (dR < 40f)
            {
                float top = AlongValue(_ridgeAt, RidgeTop, alongR);
                float ridge = top - Mathf.Max(0f, dR - 6f) * 1.6f;
                if (ridge > h) { h = ridge; g.lava = Mathf.Max(g.lava, 0.7f); }
            }

            // the desert plateau: 150 m, lower toward the west coast; its edges are cliffs except the crumbled slope
            float sdD = SdPoly(p, _desert, _desertBox);
            if (sdD > -150f)
            {
                float plateau = Mathf.Lerp(DesertWestEnd, DesertPlateau, Smooth(W(150, 0).x, W(235, 0).x, x)) + 4f * Noise(x, z, 0.02f, 9f);
                float ramp = 1f - Smooth(40f, 110f, (p - _ramp).magnitude);
                float edge = Mathf.Lerp(8f, 150f, ramp);
                float desert = Smooth(-edge, edge, sdD) * Smooth(-10f, 15f, inIsland);
                float hd = Mathf.Lerp(h, plateau, desert);
                if (hd > h) { h = hd; g.desert = desert; g.rock *= 1f - desert; }
            }

            // the dry canyon an old river cut (terraced walls), and its shallow wash out to the west coast
            if (_canyonBox.Contains(p))
            {
                float dC = DistLine(p, _canyon, _canyonAt, out float alongC);
                float wash = Smooth(_canyonAt[7], _canyonAt[9], alongC);
                float floorHalf = Mathf.Lerp(12f, 30f, wash), wall = Mathf.Lerp(26f, 60f, wash);
                if (dC < floorHalf + wall)
                {
                    float floor = AlongValue(_canyonAt, CanyonFloor, alongC) * DesertPlateau / 150f;   // the canyon keeps its depth against the plateau
                    float t = Smooth(floorHalf, floorHalf + wall, dC);
                    float hc = floor + Mathf.Max(0f, h - floor) * t;
                    float step = 14f;                                           // the strata: steps in the walls
                    float terr = floor + Mathf.Floor((hc - floor) / step) * step + Smooth(0.75f, 1f, Mathf.Repeat((hc - floor) / step, 1f)) * step;
                    hc = Mathf.Lerp(terr, hc, wash);
                    if (hc < h) { g.canyon = Smooth(h - 2f, h - 12f, hc); h = hc; }
                }
            }

            // the low meadow and the salt flat where the south river ends (behind a ridge of dunes at the coast)
            h = Mathf.Lerp(h, Mathf.Min(h, MeadowHeight + 3f * Noise(x, z, 0.02f, 13f)), low * 0.9f);
            g.meadow = (1f - Smooth(0.9f, 1.15f, meadowE)) * land;
            float saltE = ((p - _salt) / (new Vector2(28f, 15f) * K)).magnitude;
            float salt = 1f - Smooth(0.85f, 1.1f, saltE);
            if (salt > 0f) { h = Mathf.Lerp(h, 1.2f, salt); g.salt = salt; }
            float dunes = Smooth(4f, 18f, inIsland) * (1f - Smooth(18f, 45f, inIsland)) * (1f - Smooth(150f, 260f, (p - _salt).magnitude));
            h += dunes * 7f;
            float ringD = Mathf.Abs((p - _ring).magnitude - 36f);
            g.ring = (1f - Smooth(1.5f, 3.5f, ringD)) * g.meadow;

            // the rivers, carved in
            float dEast = DistLine(p, _eastRiver, _eastAt, out _), dSouth = DistLine(p, _southRiver, _southAt, out _);
            float river = 1f - Smooth(4f, 10f, Mathf.Min(dEast, dSouth));
            h -= 2.6f * river * land;

            g.forest = forest;
            g.h = h;
            return g;
        }

        // ------------------------------------------------------------------ build
        [MenuItem("Slug/IslandTest Build Scene")]
        public static void Build() { _width = 1500f; Build(true); }
        [MenuItem("Slug/IslandTest Build 500 m")] public static void Build500() { _width = 500f; Build(true); }
        [MenuItem("Slug/IslandTest Build 400 m")] public static void Build400() { _width = 400f; Build(true); }
        [MenuItem("Slug/IslandTest Build 300 m")] public static void Build300() { _width = 300f; Build(true); }

        /// <summary>Rebuilds everything but the ground: the terrain keeps the heights and paint it has (edited by hand).</summary>
        [MenuItem("Slug/IslandTest Rebuild (keep my terrain edits)")]
        public static void RebuildKeepTerrain()
        {
            // the open IslandTest scene tells which size it is
            string name = EditorSceneManager.GetActiveScene().name;
            _width = name.StartsWith("IslandTest_") && float.TryParse(name.Substring(11), out float w) ? w : 1500f;
            Build(false);
        }

        static void Build(bool newGround)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            var moveSettings = MoveTestBuilder.LoadSettings();
            var scene = MoveTestBuilder.NewScene(new Vector2(60f, 60f));
            ExtractTestBuilder.PrepareAssets();
            var map = MoveTestBuilder._map;
            Random.InitState(1500);

            var terrain = BuildTerrain(map, newGround);
            float s = S;
            System.Func<Vector2, Vector3> on = v => new Vector3(v.x * s, terrain.SampleHeight(new Vector3(v.x * s, 0f, v.y * s)) + Bottom, v.y * s);   // a point of the drawing, on the ground

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var sea = MoveTestBuilder.SaveMaterial("IT_Sea", lit, new Color(0.36f, 0.62f, 0.66f));
            sea.SetFloat("_Smoothness", 0.85f);
            var water = MoveTestBuilder.SaveMaterial("W_Puddle", lit, new Color(0.2f, 0.3f, 0.38f));
            water.SetFloat("_Smoothness", 0.92f);     // (shared with RegionTest: keep it as it makes it)
            var bark = MoveTestBuilder.SaveMaterial("W_Bark", lit, new Color(0.33f, 0.25f, 0.18f));
            var leaves = MoveTestBuilder.SaveMaterial("W_Leaves", lit, new Color(0.24f, 0.38f, 0.2f));
            var stone = MoveTestBuilder.SaveMaterial("IT_Stone", lit, new Color(0.62f, 0.5f, 0.4f));
            var props = new GameObject("Island").transform;
            props.SetParent(map, false);

            // the sea all round (no collider: the slug crawls on the sea floor for now; salt water hurts, not made yet)
            var seaGo = WeatherSceneSetup.Prim(props, "Sea", PrimitiveType.Cube, sea, new Vector3(0f, -0.05f, 0f), new Vector3(6000f, 0.1f, 6000f), 0f);
            Object.DestroyImmediate(seaGo.GetComponent<Collider>());

            // the rivers: water a little above the bed, and wet to touch
            foreach (var line in new[] { _eastRiver, _southRiver })
            {
                for (int i = 0; i + 1 < line.Length; i++)
                {
                    Vector2 a = line[i], b = line[i + 1];
                    int n = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / 8f));
                    for (int k = 0; k < n; k++)
                    {
                        Vector2 c = Vector2.Lerp(a, b, (k + 0.5f) / n);
                        Vector3 at = on(c);
                        float yaw = -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                        var seg = WeatherSceneSetup.Prim(props, "River", PrimitiveType.Cube, water, at + Vector3.up * 1.4f, new Vector3(((b - a).magnitude / n + 2f) * s, 0.05f, 11f * s), yaw);
                        Object.DestroyImmediate(seg.GetComponent<Collider>());
                        if (k % 3 == 0) WeatherSceneSetup.Zone(props, "River_Zone", MoistureZone.Kind.Water, at + Vector3.up * 1f, new Vector3(14f * s, 4f, 14f * s));
                    }
                }
            }
            WeatherSceneSetup.Zone(props, "SaltFlat_Salt", MoistureZone.Kind.Salt, on(_salt) + Vector3.up * 3f, new Vector3(56f * K * s, 10f, 30f * K * s));

            // the giant banyan (the forest's landmark): a trunk, a flat crown 100 m across, and roots hanging down as pillars
            Vector3 bb = on(_banyan);
            var banyan = new GameObject("大ガジュマル（仮）").transform;
            banyan.SetParent(props, false);
            WeatherSceneSetup.Prim(banyan, "Trunk", PrimitiveType.Cylinder, bark, bb + Vector3.up * 22f * s, new Vector3(10f, 22f, 10f) * s, 0f);
            WeatherSceneSetup.Prim(banyan, "Crown", PrimitiveType.Sphere, leaves, bb + Vector3.up * 44f * s, new Vector3(100f, 14f, 100f) * s, 0f);
            for (int i = 0; i < 40; i++)
            {
                Vector2 r = _banyan + Random.insideUnitCircle * 44f;
                if ((r - _banyan).magnitude < 9f) continue;
                Vector3 g0 = on(r);
                float top = bb.y + 40f * s;
                WeatherSceneSetup.Prim(banyan, "Root", PrimitiveType.Cylinder, bark, new Vector3(g0.x, (g0.y + top) * 0.5f, g0.z), new Vector3(1.4f * s, (top - g0.y) * 0.5f, 1.4f * s), 0f);
            }

            // the stone arch over the dry canyon
            {
                float dC = DistLine(_arch, _canyon, _canyonAt, out float along);
                int i = 0;
                while (i + 2 < _canyonAt.Length && _canyonAt[i + 1] < along) i++;
                Vector2 dir = (_canyon[i + 1] - _canyon[i]).normalized;
                float yaw = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
                Vector3 rim = on(_arch + new Vector2(-dir.y, dir.x) * 60f);
                WeatherSceneSetup.Prim(props, "石橋（仮）", PrimitiveType.Cube, stone, new Vector3(_arch.x * s, rim.y - 2f * s, _arch.y * s), new Vector3(120f, 4f, 7f) * s, yaw);
            }

            // labels: the regions, the landmarks and the crossings
            void L(string text, Vector2 at, float up, float size) => MoveTestBuilder.Label(text, on(at) + Vector3.up * up * s, Mathf.Max(4f, size * s));
            L("岩塩の山", _mountain, 30f, 40f);
            L("黒い溶岩台地", W(410, 160), 25f, 40f);
            L("冷えた火口", _crater, 25f, 25f);
            L("砂漠（高い台地）", W(220, 205), 25f, 40f);
            L("枯れた谷", W(250, 240), 10f, 25f);
            L("峠（谷の入口）", W(318, 256), 15f, 18f);
            L("石橋", _arch, 18f, 18f);
            L("崩れた坂", _ramp, 12f, 18f);
            L("尾根", W(368, 190), 15f, 18f);
            L("雨の森", W(530, 300), 20f, 40f);
            L("大ガジュマル", _banyan, 60f, 25f);
            L("花の草原（菌輪）", _meadow, 15f, 30f);
            L("塩田", _salt, 10f, 30f);
            L("滝（仮。崖はまだ）", _waterfall, 12f, 16f);

            // a camp to set out from (east forest) and one to go home from (by the salt flat)
            Vector3 home = on(W(248, 318));
            var exit = ExtractTestBuilder.BuildExit(map, home);
            Vector3 spawn = on(W(500, 300)) + Vector3.up * 0.05f;
            var player = ExtractTestBuilder.BuildRaidPlayer(moveSettings, spawn, exit);
            Vector3 toMountain = on(_mountain) - spawn;
            player.transform.rotation = Quaternion.LookRotation(new Vector3(toMountain.x, 0f, toMountain.z));
            var cam = player.GetComponentInChildren<Camera>();
            if (cam) cam.farClipPlane = 3000f;
            var fast = player.AddComponent<DevFastCrawl>();          // F4: crawl 10 times faster to look round the island
            fast.controller = player.GetComponent<SlugMoveController>();

            WeatherSceneSetup.Fit(false);
            var presenter = Object.FindAnyObjectByType<WeatherPresenter>();
            if (presenter) { presenter.fogScale = 0.25f; EditorUtility.SetDirty(presenter); }   // thin haze: the whole island in view
            EditorSceneManager.SaveScene(scene, ScenePath);
            MoveTestBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[IslandTest] scene saved: " + ScenePath);
        }

        static Terrain BuildTerrain(Transform map, bool newGround)
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
            if (!data) { data = new TerrainData(); AssetDatabase.CreateAsset(data, TerrainPath); newGround = true; }
            if (newGround) MakeGround(data);
            return PlaceTerrain(map, data);
        }

        /// <summary>Works the heights and the paint out from the map drawing (overwriting any hand edits).</summary>
        static void MakeGround(TerrainData data)
        {
            data.heightmapResolution = Res;
            data.size = new Vector3(Size, MaxHeight, Size);
            var heights = new float[Res, Res];
            for (int iz = 0; iz < Res; iz++)
                for (int ix = 0; ix < Res; ix++)
                {
                    float x = -Size * 0.5f + Size * ix / (Res - 1f), z = -Size * 0.5f + Size * iz / (Res - 1f);
                    heights[iz, ix] = Mathf.Clamp01((Sample(x / S, z / S).h * S - Bottom) / MaxHeight);
                }
            data.SetHeights(0, 0, heights);

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
            };
            data.terrainLayers = layers;
            data.alphamapResolution = AlphaRes;
            var alpha = new float[AlphaRes, AlphaRes, layers.Length];
            for (int iz = 0; iz < AlphaRes; iz++)
                for (int ix = 0; ix < AlphaRes; ix++)
                {
                    float x = -Size * 0.5f + Size * ix / (AlphaRes - 1f), z = -Size * 0.5f + Size * iz / (AlphaRes - 1f);
                    var g = Sample(x / S, z / S);
                    float[] w = new float[layers.Length];
                    w[7] = g.ring;
                    w[3] = g.salt * (1f - w[7]);
                    w[6] = g.canyon * (1f - w[3]);
                    w[5] = g.lava * (1f - w[6]);
                    w[2] = g.desert * (1f - w[6] - w[5]);
                    w[4] = g.rock * (1f - w[2] - w[5] - w[6]);
                    float rest = Mathf.Max(0f, 1f - w[2] - w[3] - w[4] - w[5] - w[6] - w[7]);
                    float lowness = Mathf.Max(g.meadow, 1f - g.forest);
                    w[1] = rest * lowness;
                    w[0] = rest * (1f - lowness);
                    float sum = 1e-4f;
                    foreach (float v in w) sum += Mathf.Max(0f, v);
                    for (int l = 0; l < w.Length; l++) alpha[iz, ix, l] = Mathf.Max(0f, w[l]) / sum;
                }
            data.SetAlphamaps(0, 0, alpha);
            EditorUtility.SetDirty(data);
        }

        static Terrain PlaceTerrain(Transform map, TerrainData data)
        {
            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(map, false);
            go.transform.position = new Vector3(-data.size.x * 0.5f, Bottom, -data.size.z * 0.5f);
            var terrain = go.GetComponent<Terrain>();
            var matPath = MoveTestBuilder.GenDir + "/RT_TerrainMat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            terrain.materialTemplate = mat;
            terrain.heightmapPixelError = 4f;
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
            for (int i = 0; i < px.Length; i++) px[i] = new Color(color.r, color.g, color.b, 0f);   // alpha is smoothness: matte
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

        /// <summary>For measuring from MCP: the island's ground height at a world point (m, the sea at 0), and the map's points.</summary>
        public static float GroundAt(float x, float z) { if (_island == null) Prepare(); return Sample(x / S, z / S).h * S; }
        public static Vector2 MapPoint(float sx, float sy) => W(sx, sy) * S;
        /// <summary>For measuring: which width the next GroundAt / MapPoint answer for.</summary>
        public static void UseWidth(float width) => _width = width;
    }
}
