using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の灰色の箱：設備の中身（Nov 4、わくさん「拠点の細かいところ」→ 設備の中身から）。色はまだ、形だけ。
    /// 置き場所と大きな箱は BaseGrayboxRooms.cs、ここはその上に乗る小物。当たり判定は乗れる物だけ（ほかは見た目）。
    /// 寸法はナメクジ（体長 1.1 m、目の高さ 0.47 m、人の約 1/3.6）と比べて。
    /// - （精製所の臼・型の台・アモレ・塩の壺のふたは、本物の形に替えた：Refinery/BaseRefineryInBase.cs）
    /// - 旧：精製所の臼：上の石に溝 4 本と真ん中の穴、下の石のふちに粉の受け溝、東へ注ぎ口と粉の鉢。入れ口に脚 4 本と塩の塊、臼の穴への樋
    /// - 型の台：穴 6 つの型の板、押し棒（支点の柱とてこ）、できた弾は木の皿に。ミニアモレの束
    /// - アモレの積み場（spec 9章の例「箱3・束4・延べ棒2・ミニ7」）：延べ棒 0.2×0.06×0.04 m、束＝延べ棒 10 本（5 本×2 段）に縄 2 本、
    ///   箱＝ふた付きの木箱 0.44×0.32×0.34 m、ミニ＝穴のある円盤 直径 6 cm を壁の釘に縄で吊る
    /// - 塩の壺の棚：壺にふた、棚の端にすくい匙
    /// - 地図の台：ふちの木の輪と、シーズンの刻み目、地図の上に石の駒 4 つと方位の円盤、開いた帳面
    /// - 細工場：奥の道具掛けにピッケル・拳銃・ライフル・苔玉（自分の装備が掛かる）、砥ぎ車の下に水の舟、作業台に万力と作りかけの殻の箱と大きい苔玉
    /// - 観測所：小屋に葉の屋根と東向きの戸口、戸の横に予報の板（6 刻み）、雨量の筒、西の空を見る覗き筒（三脚）
    /// - 図鑑の棚：棚板 3 段に本。飾り枠：額縁と台と来歴の札
    /// </summary>
    public static partial class BaseGrayboxBuilder
    {
        static void Details()
        {
            var g = Group("Details");
            MapDetails(g);
            WorkshopDetails(g);
            ObservatoryDetails(g);
            ShelfDetails(g);
        }

        /// <summary>壁ぎわの角度 deg の前で、広間の中を向く横向き（右）と内向き。</summary>
        static (Vector3 right, Vector3 inward) WallFrame(float deg)
        {
            var rot = Quaternion.Euler(0f, FaceCenter(deg), 0f);
            return (rot * Vector3.right, rot * Vector3.forward);
        }

        // ───────── 地図の台 ─────────

        static void MapDetails(Transform g)
        {
            var top = MapAt + Vector3.up * 0.4f;
            // ふちの木の輪と、シーズンの刻み目（大干ばつのあとに残る）
            for (int k = 0; k < 24; k++)
            {
                float a = k * 15f;
                Prim(PrimitiveType.Cube, g, "MapRim", MapAt + Polar(a, 1.0f, 0.42f), new Vector3(0.27f, 0.06f, 0.07f), _wood, FaceCenter(a), null, false);
            }
            for (int k = 0; k < 5; k++)
            {
                float a = 250f + k * 4f;
                Prim(PrimitiveType.Cube, g, "SeasonNotch", MapAt + Polar(a, 1.0f, 0.455f), new Vector3(0.015f, 0.02f, 0.08f), _floor, FaceCenter(a), null, false);
            }
            // 石の駒 4 つ（行く所・キャンプの印）と方位の円盤
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Sphere, g, "MapStone", top + new Vector3(-0.45f + 0.3f * i, 0.04f, -0.15f + 0.2f * PropKit.H(i, 71)), new Vector3(0.08f, 0.06f, 0.07f), _rock, i * 40f, null, false);
            Cyl(g, "Compass", top + new Vector3(-0.55f, 0.01f, 0.48f), 0.12f, 0.03f, _wood, false);
            Prim(PrimitiveType.Cube, g, "CompassNeedle", top + new Vector3(-0.55f, 0.045f, 0.48f), new Vector3(0.02f, 0.01f, 0.2f), _mark, 8f, null, false);
            // 開いた帳面（閉じた帳面の上に、左右のページ）
            var led = MapAt + new Vector3(0.62f, 0.475f, -0.35f);
            foreach (float s in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, g, "LedgerPage", led + Quaternion.Euler(0f, -20f, 0f) * new Vector3(0.11f * s, 0.01f, 0f), new Vector3(0.21f, 0.015f, 0.26f), _mark, -20f, new Vector3(0f, -20f, -6f * s), false);
        }

        // ───────── 細工場 ─────────

        static void WorkshopDetails(Transform g)
        {
            // 奥の道具掛け（172°、半径 7.5 の板の手前）に自分の装備
            var (right, inward) = WallFrame(172f);
            var rack = Polar(172f, 7.5f, MDeck + 1.1f) + inward * 0.08f;
            for (int i = -2; i <= 2; i++) Rod(g, "ToolPeg", rack + right * (i * 0.4f) + Vector3.up * 0.35f, rack + right * (i * 0.4f) + Vector3.up * 0.35f + inward * 0.1f, 0.012f, _wood);
            // ピッケル（柄と頭）
            var pk = rack + right * -0.8f + inward * 0.06f;
            Rod(g, "Pickaxe", pk + Vector3.up * 0.35f, pk + Vector3.down * 0.3f, 0.02f, _wood);
            float wy = FaceCenter(172f);   // 箱の x が壁ぞいの横向きになる
            Prim(PrimitiveType.Cube, g, "PickaxeHead", pk + Vector3.up * 0.3f, new Vector3(0.36f, 0.05f, 0.04f), _rock, 0f, new Vector3(0f, wy, 8f), false);
            // 拳銃
            var ps = rack + right * -0.4f + inward * 0.06f + Vector3.up * 0.2f;
            Prim(PrimitiveType.Cube, g, "Pistol", ps, new Vector3(0.24f, 0.07f, 0.04f), _floor, wy, null, false);
            Prim(PrimitiveType.Cube, g, "PistolGrip", ps + Vector3.down * 0.07f - right * 0.07f, new Vector3(0.05f, 0.12f, 0.04f), _wood, 0f, new Vector3(0f, wy, -12f), false);
            // ライフル（横に長く、釘 2 本に掛ける）
            var rf = rack + right * 0.4f + inward * 0.06f + Vector3.up * 0.28f;
            Rod(g, "Rifle", rf - right * 0.42f, rf + right * 0.42f, 0.025f, _floor);
            Prim(PrimitiveType.Cube, g, "RifleStock", rf + right * 0.32f + Vector3.down * 0.05f, new Vector3(0.24f, 0.1f, 0.04f), _wood, wy, null, false);
            // 苔玉（網に入れて吊る）
            var mb = rack + right * 0.8f + inward * 0.1f + Vector3.down * 0.05f;
            Rod(g, "MossBallCord", mb + Vector3.up * 0.4f, mb + Vector3.up * 0.09f, 0.006f, _wood);
            Prim(PrimitiveType.Sphere, g, "MossBall", mb, Vector3.one * 0.18f, _moss, 0f, null, false);

            // 砥ぎ車の下の水の舟（石の下が水に浸かる）
            Prim(PrimitiveType.Cube, g, "GrindTrough", GrindAt + Vector3.up * 0.1f, new Vector3(0.7f, 0.2f, 0.32f), _wood, FaceCenter(174f), null, false);
            Prim(PrimitiveType.Cube, g, "GrindWater", GrindAt + Vector3.up * 0.19f, new Vector3(0.62f, 0.02f, 0.24f), _water, FaceCenter(174f), null, false);

            // 作業台（上の面 MDeck + 0.6）：万力・作りかけの殻の箱・大きい苔玉
            var (br, bi) = WallFrame(167f);
            var bench = Polar(167f, 6.9f, MDeck + 0.6f);
            float byaw = FaceCenter(167f);
            Prim(PrimitiveType.Cube, g, "ViseJaw", bench - br * 0.5f + bi * 0.2f + Vector3.up * 0.08f, new Vector3(0.06f, 0.16f, 0.14f), _rock, byaw, null, false);
            Prim(PrimitiveType.Cube, g, "ViseJaw", bench - br * 0.38f + bi * 0.2f + Vector3.up * 0.08f, new Vector3(0.06f, 0.16f, 0.14f), _rock, byaw, null, false);
            Rod(g, "ViseScrew", bench - br * 0.6f + bi * 0.2f + Vector3.up * 0.1f, bench - br * 0.3f + bi * 0.2f + Vector3.up * 0.1f, 0.012f, _wood);
            Prim(PrimitiveType.Sphere, g, "ShellBoxHalf", bench + br * 0.05f + Vector3.up * 0.05f, new Vector3(0.36f, 0.14f, 0.28f), _rock, byaw + 20f, null, false);
            Prim(PrimitiveType.Sphere, g, "BigMossBall", bench + br * 0.45f + Vector3.up * 0.14f, Vector3.one * 0.28f, _moss, 0f, null, false);
            for (int i = 0; i < 5; i++)
                Prim(PrimitiveType.Cube, g, "Shaving", bench + br * (0.1f * i - 0.2f) - bi * 0.15f + Vector3.up * 0.005f, new Vector3(0.06f, 0.01f, 0.02f), _wood, byaw + i * 50f, null, false);
        }

        // ───────── 観測所（崖の中腹） ─────────

        static void ObservatoryDetails(Transform g)
        {
            // 小屋 x -12.3〜-10.9、z 0.6〜2.0、高さ 1.7。葉の屋根（西が低い片流れ、ひさしが出る）
            var roofC = new Vector3(-11.6f, F3Y + 1.85f, 1.3f);
            Prim(PrimitiveType.Cube, g, "ObsRoof", roofC, new Vector3(1.9f, 0.12f, 1.9f), _moss, 0f, new Vector3(0f, 0f, 12f), false);
            // 東向きの戸口（暗い穴の代わりに板の枠）
            float ex = -10.88f;
            Rod(g, "ObsDoorPost", new Vector3(ex, F3Y, 1.05f), new Vector3(ex, F3Y + 1.1f, 1.05f), 0.03f, _wood);
            Rod(g, "ObsDoorPost", new Vector3(ex, F3Y, 1.55f), new Vector3(ex, F3Y + 1.1f, 1.55f), 0.03f, _wood);
            Rod(g, "ObsDoorLintel", new Vector3(ex, F3Y + 1.1f, 1.0f), new Vector3(ex, F3Y + 1.1f, 1.6f), 0.03f, _wood);
            Box(g, "ObsDoorDark", new Vector3(ex - 0.02f, F3Y, 1.08f), new Vector3(ex + 0.005f, F3Y + 1.08f, 1.52f), _floor, false);
            // 予報の板：戸の南の壁に 6 刻み（晴れ＝丸、雨＝しずく、曇り＝横棒）
            var board = new Vector3(ex + 0.03f, F3Y + 0.95f, 0.78f);
            Box(g, "ForecastBoard", board + new Vector3(0f, -0.25f, -0.15f), board + new Vector3(0.03f, 0.25f, 0.15f), _wood, false);
            for (int i = 0; i < 6; i++)
            {
                var c = board + new Vector3(0.04f, 0.15f - 0.08f * (i / 2) * 1.6f, -0.07f + 0.14f * (i % 2));
                int kind = i % 3;
                if (kind == 0) Prim(PrimitiveType.Sphere, g, "ForecastSun", c, new Vector3(0.01f, 0.06f, 0.06f), _mark, 0f, null, false);
                else if (kind == 1) Prim(PrimitiveType.Sphere, g, "ForecastRain", c, new Vector3(0.01f, 0.07f, 0.04f), _mark, 0f, null, false);
                else Prim(PrimitiveType.Cube, g, "ForecastCloud", c, new Vector3(0.01f, 0.025f, 0.08f), _mark, 0f, null, false);
            }
            // 雨量の筒（じょうごと目盛りの輪）
            var rain = new Vector3(-9.1f, F3Y, 1.6f);
            Cyl(g, "RainGauge", rain, 0.09f, 0.55f, _wood, false);
            Prim(PrimitiveType.Cylinder, g, "RainFunnel", rain + Vector3.up * 0.62f, new Vector3(0.3f, 0.06f, 0.3f), _wood, 0f, null, false);
            for (int i = 0; i < 3; i++) Cyl(g, "RainMark", rain + Vector3.up * (0.15f + 0.13f * i), 0.095f, 0.015f, _mark, false);
            // 西の空を見る覗き筒（三脚に竹の筒）
            var tri = new Vector3(-11.9f, F3Y, -1.4f);
            var head = tri + Vector3.up * 0.75f;
            for (int k = 0; k < 3; k++) Rod(g, "TripodLeg", head, tri + Polar(k * 120f + 30f, 0.32f, 0f), 0.02f, _wood);
            Rod(g, "SightTube", head + new Vector3(0.3f, -0.08f, 0f), head + new Vector3(-0.45f, 0.12f, 0f), 0.05f, _wood);
            Label("予報の板・雨量の筒・覗き筒", new Vector3(-10.4f, F3Y + 1.6f, -0.6f), -90f, 1.0f);   // 東の通路から来て読める向き
        }

        // ───────── 図鑑の棚・飾り枠 ─────────

        static void ShelfDetails(Transform g)
        {
            var (zr, zi) = WallFrame(317f);
            var zk = Polar(317f, 6.45f, LDeck) + zi * 0.26f;   // 棚の板（奥行き 0.3、前の面は半径 6.3）の手前
            for (int s = 0; s < 3; s++)
            {
                var sh = zk + Vector3.up * (0.2f + 0.42f * s);
                Prim(PrimitiveType.Cube, g, "ZukanShelf", sh, new Vector3(1.0f, 0.03f, 0.22f), _wood, FaceCenter(317f), null, false);
                int n = 5 + s;   // 下の段から埋まっていく
                for (int b = 0; b < n; b++)
                {
                    float hgt = 0.22f + 0.06f * PropKit.H(b + s * 10, 81);
                    Prim(PrimitiveType.Cube, g, "ZukanBook", sh + zr * (-0.42f + 0.085f * b) + Vector3.up * (0.015f + hgt * 0.5f),
                         new Vector3(0.06f, hgt, 0.16f), b % 3 == 0 ? _mark : _wood, FaceCenter(317f), null, false);
                }
            }

            var (kr, ki) = WallFrame(304f);
            var kz = Polar(304f, 6.4f, LDeck + 0.5f) + ki * 0.16f;
            foreach (float s in new[] { -1f, 1f })
            {
                Prim(PrimitiveType.Cube, g, "KazariFrame", kz + kr * 0.47f * s, new Vector3(0.06f, 1.0f, 0.06f), _wood, FaceCenter(304f), null, false);
                Prim(PrimitiveType.Cube, g, "KazariFrame", kz + Vector3.up * 0.47f * s, new Vector3(1.0f, 0.06f, 0.06f), _wood, FaceCenter(304f), null, false);
            }
            var plinth = Polar(304f, 6.1f, LDeck);
            Cyl(g, "KazariPlinth", plinth, 0.22f, 0.95f, _rock);
            var tag = plinth + Vector3.up * 0.75f + ki * 0.24f;
            Rod(g, "KazariTagCord", plinth + Vector3.up * 0.95f + ki * 0.2f, tag + Vector3.up * 0.06f, 0.005f, _wood);
            Prim(PrimitiveType.Cube, g, "KazariTag", tag, new Vector3(0.14f, 0.1f, 0.01f), _mark, FaceCenter(304f), null, false);
        }
    }
}
