using SlugMove;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の灰色の箱（ステップ2：間取りと動線だけ。色・細部はまだ）。Nov 2 に1版、Nov 3 に2版（岩の段々・板の足場・水路3本）。
    /// 目標の絵 docs/modeling/reference/base_concept_nov02_*.webp、決まり docs/spec.md 10章、docs/discussions/base_layout_v2.md。
    /// メニュー Slug/Base/Build Base Graybox → Assets/Scenes/Preview/BaseLook.unity（再生すると2本足のナメクジで桟橋から歩ける）、
    /// 絵は Screenshots/Base/base_graybox.png（1 帰ってきた瞬間・2 出ていく瞬間・3 中の全景・4 池と苗床・5 西の壁・6 真上）。
    /// Nov 3 の3版：壁を凹ませた横穴（BaseGrayboxNiches.cs）、地図の台のまわりを広く、苗床をそろえる、カタツムリの寝床、苔・シダ・つる・小エビ。
    ///
    /// 向き：+Z が北（島の奥）、-Z が南（海）。高さ 0 が海面。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - 広間：直径 12 m（人なら 43 m）、高さ 7 m（人なら 25 m）。壁は y 6.0 までまっすぐ、上は丸天井
    /// - 床：岩の段々と板の足場（BaseGrayboxGround.cs の冒頭）。下段 2.4・中段 3.0・上段 3.6、板はその 0.25 m 上
    /// - 天窓：直径 4 m、上段の池の真上（中心 x 0.6, z 3.2）。島のてっぺん y 11.4 まで縦穴、ふちに手すり
    /// - トンネル：幅 5.2 m・高さ 3.2 m。床はでこぼこの岩に板の道。東に水路、西に行商人（出口の手前）と水車（出口、半分が崖の外）
    /// - 水車：直径 3.4 m。主軸はトンネルの西の壁ぞい（y 3.5）→ 広間の入口で y 4.3 へ上がり → 精製所の臼（下段の西）→ 細工場の砥ぎ車（中段の西）。
    ///   臼の上から2階の乾燥室の羽根車へ縦の軸
    /// - 2階：西の壁の木の回廊 y 5.05（218°〜250°、乾燥室）。中段の細工場の横から板の階段。3階：y 6.25（146°〜196°）、西へ抜けて崖の中腹（観測所と鳥の巣）。
    ///   3階の北の端から池へ張り出す縁側と飛びこみ台（Nov 4、BaseGrayboxEngawa.cs、絵は Screenshots/Base/base_3f_engawa.png）
    /// - 外：桟橋に丸い籠の発着場と鳥、東の岩棚にカタツムリの小屋。灯 25 個くらい（天井の梁から吊るす灯も）
    /// - 足場と水まわり（Nov 4、BaseGrayboxWaterDetails.cs）：水路の段の樋と落ち口、池の口の石、縄の手すり、柄杓、梁の端。柱の縄と筋かいは Planks
    /// - 暮らしの跡・乾燥室・染め場・苗床・問屋の窓口・桟橋と舟・カタツムリの小屋の小物（Nov 4、BaseGrayboxLiving.cs）。灯は油の皿と腕木（Kit の Lamp）
    /// - 設備の中身（Nov 4、BaseGrayboxDetails.cs）：臼の溝と注ぎ口・型の台と押し棒・アモレの積み場・地図の台の駒と帳面・細工場の道具・観測所の予報の板など
    /// </summary>
    public static partial class BaseGrayboxBuilder
    {
        const string Dir = "Assets/Art/Models/Base/Graybox";
        const string ScenePath = "Assets/Scenes/Preview/BaseLook.unity";
        const string ShotPath = "Screenshots/Base/base_graybox.png";
        const string EngawaShotPath = "Screenshots/Base/base_3f_engawa.png";
        const string RefineryShotPath = "Screenshots/Base/base_refinery.png";

        const float FloorY = 2.4f, HallR = 6f, HallH = 7f, WallH = 3.6f;
        static readonly Vector2 SkyC = new Vector2(0.6f, 3.2f);   // 天窓と池の真ん中
        const float SkyR = 2f, TopY = 11.4f;
        const float TunY = 1.2f, TunHalfW = 2.6f, TunH = 3.2f, TunZ0 = -5.3f, MouthZ = -13.9f;
        const float StepTopZ = -6.2f, StepRun = 0.35f; const int Steps = 8;
        const float F2Y = 5.05f, F3Y = 6.25f, AxleY = 3.5f, HallAxleY = 4.3f, AxleX = -1.8f;
        static readonly Vector3 WheelC = new Vector3(-3.7f, 1.7f, -13.6f);
        const float WheelR = 1.7f, WheelW = 0.7f;

        [MenuItem("Slug/Base/Build Base Graybox")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[BaseGraybox] stop play first"); return; }
            System.IO.Directory.CreateDirectory(Dir);
            var before = SceneManager.GetActiveScene();
            var old = SceneManager.GetSceneByPath(ScenePath);
            if (old.IsValid() && old.isLoaded) EditorSceneManager.CloseScene(old, true);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                Materials();
                _root = new GameObject("Base").transform;
                Sun();
                Hall();
                Niches();
                Ground();
                Tunnel();
                Boards();
                Engawa();
                Rooms();
                Outside();
                Details();
                WaterDetails();
                Living();
                Lamps();
                Life();
                Stand();
                DinoMoveTestBuilder.BuildPlayer(MoveTestBuilder.LoadSettings(), new Vector3(0f, TunY + Board + 0.05f, -16.2f));
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                string png = Shots(scene, Views(), ShotPath, EngawaViews(), EngawaShotPath, RefineryViews(), RefineryShotPath);
                Debug.Log($"[BaseGraybox] saved {ScenePath}, shots {png}");
            }
            finally
            {
                if (before.IsValid() && before.isLoaded) SceneManager.SetActiveScene(before);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static (string, Vector3, Vector3, float, bool)[] Views() => new[]
        {
            ("return", new Vector3(-0.3f, LDeck + 0.47f, -5.95f), new Vector3(-0.2f, MDeck + 1.3f, 1.5f), 70f, false),
            ("depart", new Vector3(0.0f, TunY + Board + 0.47f, -9.3f), new Vector3(-0.4f, TunY + 0.7f, -18f), 70f, false),
            ("hall", new Vector3(9f, 13f, -14f), new Vector3(-0.5f, 3f, -1.5f), 50f, true),
            ("pool", new Vector3(0.4f, UDeck + 0.47f, 1.75f), new Vector3(-1.9f, UpY + 0.2f, 3.3f), 70f, false),
            ("west", new Vector3(0.8f, MDeck + 0.47f, -1.1f), new Vector3(-5.5f, F2Y - 0.3f, -0.6f), 70f, false),
            ("plan", new Vector3(0.5f, 40f, -5.5f), new Vector3(0.5f, 0f, -5.4f), 42f, true),
        };

        /// <summary>精製所（Nov 5）：1 帰って踊り場に出た所・2 中段からの階段の途中・3 型の台の前・4 臼と横穴・5 横穴の中・6 上から。どれもナメクジの目の高さ（6 だけ上）。</summary>
        static (string, Vector3, Vector3, float, bool)[] RefineryViews() => new[]
        {
            ("return", new Vector3(0.4f, LDeck + 0.47f, -5.7f), new Vector3(-3.0f, LDeck + 0.9f, -3.8f), 70f, false),
            ("down", new Vector3(-0.3f, LDeck + 0.15f * 3f + 0.47f, -3.75f), new Vector3(-2.5f, LDeck + 0.5f, -4.2f), 70f, false),
            ("bench", new Vector3(-0.45f, LDeck + 0.47f, -4.75f), new Vector3(-1.6f, LDeck + 0.5f, -3.7f), 60f, false),
            ("mill", new Vector3(-1.6f, LDeck + 0.47f, -5.0f), new Vector3(-3.2f, LDeck + 0.65f, -3.6f), 68f, false),
            ("niche", new Vector3(-3.9f, LDeck + 0.47f, -3.0f), new Vector3(-5.5f, LDeck + 0.65f, -4.3f), 66f, false),
            ("above", new Vector3(1.2f, 6.4f, -1.4f), new Vector3(-2.8f, LDeck, -4.1f), 55f, false),
        };

        static void Sun()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(_root, false);
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(72f, 20f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.33f, 0.34f, 0.38f);
            Prim(PrimitiveType.Plane, _root, "Sea", new Vector3(0f, 0f, 0f), new Vector3(30f, 1f, 30f), _water, 0f, null, false);
        }

        // ───────── 器：広間・天窓・島の外側 ─────────

        static float HallRadius(float h) => h <= WallH ? HallR : HallR * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((h - WallH) / (HallH - WallH), 2f)));

        static void Hall()
        {
            var g = Group("Shell");
            Grid(g, "Shell_Hall", (u, v) =>
            {
                float a = u * Mathf.PI * 2f, h = v * HallH;
                float r = HallRadius(h) * (1f + 0.04f * PropKit.Noise(new Vector3(Mathf.Cos(a) * 3f, h * 0.6f, Mathf.Sin(a) * 3f), 11));
                return new Vector3(Mathf.Cos(a) * r, FloorY - 0.2f + h, Mathf.Sin(a) * r);
            }, 72, 28, p =>
                (p.z < -3f && Mathf.Abs(p.x) < TunHalfW + 0.15f && p.y < TunY + TunH + 0.1f)                   // トンネルの口
                || (p.y > FloorY + WallH && new Vector2(p.x - SkyC.x, p.z - SkyC.y).magnitude < SkyR)           // 天窓
                || (p.x < -4f && Mathf.Abs(p.z) < 0.9f && p.y > F3Y - 0.1f && p.y < F3Y + 1.95f)              // 3階から崖の中腹へ
                || InNiche(p),                                                                                  // 壁を凹ませた横穴
            p => new Vector3(0f, FloorY + 2.5f, 0f) - p, _rock);

            // 天窓の縦穴（丸天井から島のてっぺんまで）とふちの手すり
            Grid(g, "Shell_Skylight", (u, v) =>
            {
                float a = u * Mathf.PI * 2f;
                float x = SkyC.x + Mathf.Cos(a) * SkyR, z = SkyC.y + Mathf.Sin(a) * SkyR;
                float r = new Vector2(x, z).magnitude;   // 丸天井に当たる高さから上へ（広間の中へ垂れない）
                float dome = FloorY + WallH + (HallH - WallH) * Mathf.Sqrt(Mathf.Max(0f, 1f - r * r / (HallR * HallR)));
                return new Vector3(x, Mathf.Lerp(dome - 0.1f, TopY + 0.05f, v), z);
            }, 32, 4, null, p => new Vector3(SkyC.x, p.y, SkyC.y) - p, _rock, false);
            for (int i = 0; i < 16; i++)
            {
                float a = i * 22.5f * Mathf.Deg2Rad, b = (i + 1) * 22.5f * Mathf.Deg2Rad;
                var pa = new Vector3(SkyC.x + Mathf.Cos(a) * 2.25f, TopY, SkyC.y + Mathf.Sin(a) * 2.25f);
                var pb = new Vector3(SkyC.x + Mathf.Cos(b) * 2.25f, TopY, SkyC.y + Mathf.Sin(b) * 2.25f);
                Rod(g, "SkyRailPost", pa, pa + Vector3.up * 0.7f, 0.04f, _wood);
                Rod(g, "SkyRail", pa + Vector3.up * 0.7f, pb + Vector3.up * 0.7f, 0.03f, _wood);
            }

            // 島の外側（角の丸い四角の崖）とてっぺん。入口・水車・3階の出口を抜く
            Vector2 c = new Vector2(0.3f, -1.8f), ax = new Vector2(9f, 11.85f);
            System.Func<float, float, Vector3> edge = (a, y) =>
            {
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                float k = Mathf.Pow(Mathf.Pow(Mathf.Abs(ca), 3.5f) + Mathf.Pow(Mathf.Abs(sa), 3.5f), -1f / 3.5f);
                float n = 1f + 0.035f * PropKit.Noise(new Vector3(ca * 4f, y * 0.4f, sa * 4f), 5);
                float shrink = 1f - 0.05f * Mathf.Clamp01(y / TopY);
                return new Vector3(c.x + ca * ax.x * k * n * shrink, y, c.y + sa * ax.y * k * n * shrink);
            };
            Grid(g, "Shell_Island", (u, v) => edge(u * Mathf.PI * 2f, Mathf.Lerp(-0.5f, TopY, v)), 96, 18, p =>
                (p.z < -10f && p.x > -5.4f && p.x < 3.2f && p.y < TunY + TunH + 0.3f)
                || (p.x < -6f && Mathf.Abs(p.z) < 1.1f && p.y > F3Y - 0.2f && p.y < F3Y + 2.1f),
                p => new Vector3(p.x - c.x, 0f, p.z - c.y), _rock);
            Grid(g, "Shell_Top", (u, v) =>
            {
                var e = edge(u * Mathf.PI * 2f, TopY);
                return Vector3.Lerp(new Vector3(c.x, TopY + 0.6f, c.y), e, v);
            }, 64, 8, p => new Vector2(p.x - SkyC.x, p.z - SkyC.y).magnitude < SkyR, _ => Vector3.up, _rock);
        }

        // ───────── トンネル：殻・行商人の場所・水車と主軸（床と板は Ground / Boards） ─────────

        static void Tunnel()
        {
            var g = Group("Tunnel");
            Grid(g, "Shell_Tunnel", (u, v) =>
            {
                float a = Mathf.PI * u;
                float x = -Mathf.Cos(a) * TunHalfW * (1f + 0.04f * Mathf.Sin(v * 9f));
                float y = TunY - 0.6f + (TunH + 0.6f) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(a)), 0.45f);
                return new Vector3(x, y, Mathf.Lerp(TunZ0, MouthZ, v));
            }, 20, 24, p =>
                (p.x < 0f && p.z < -8.8f && p.z > -10.6f && p.y < TunY + 2.05f)    // 行商人の場所
                || (p.x < 0f && p.z < -11.4f && p.y < WheelC.y + WheelR + 0.35f), // 水車の入る切れこみ
            p => new Vector3(0f, TunY + 1f, p.z) - p, _rock);

            // 行商人の場所（出口の手前の西。絵の2枚目の右手前）。奥の壁に植物の籠の棚（湿った所に植物を置く、Oct 17）
            var shop = Group("Shop");
            float fy = TunY + Board;
            Grid(shop, "Shell_ShopNook", (u, v) =>
            {
                // 北のわき → 奥の壁 → 南のわき（上から見てコの字、u で回る）
                var c = new[] { new Vector2(-TunHalfW, -8.8f), new Vector2(-4.4f, -8.8f), new Vector2(-4.4f, -10.6f), new Vector2(-TunHalfW, -10.6f) };
                float f = u * 3f; int i = Mathf.Min(2, Mathf.FloorToInt(f));
                var q = Vector2.Lerp(c[i], c[i + 1], f - i);
                return new Vector3(q.x, Mathf.Lerp(TunY - 0.3f, TunY + 2.1f, v), q.y);
            }, 12, 3, null, p => new Vector3(-3.5f, p.y, -9.7f) - p, _rock);
            Box(shop, "Shell_ShopCeiling", new Vector3(-4.4f, TunY + 2.1f, -10.6f), new Vector3(-TunHalfW, TunY + 2.3f, -8.8f), _rock, false);
            Box(shop, "Counter", new Vector3(-3.05f, fy, -10.3f), new Vector3(-2.7f, fy + 0.6f, -9.1f), _wood);
            Box(shop, "BasketShelf", new Vector3(-4.38f, fy, -10.5f), new Vector3(-4.05f, fy + 1.6f, -8.9f), _wood, false);
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 4; i++)
                    Cyl(shop, "Basket", new Vector3(-4.0f, fy + 0.05f + row * 0.52f, -10.3f + i * 0.4f), 0.15f, 0.22f, _mark, false);
            // 問屋の天秤（演出。片方の皿に塩の延べ棒）
            var scale = new Vector3(-2.88f, fy + 0.6f, -9.4f);
            Rod(shop, "ScalePost", scale, scale + Vector3.up * 0.55f, 0.025f, _wood);
            Rod(shop, "ScaleBeam", scale + new Vector3(0f, 0.55f, -0.3f), scale + new Vector3(0f, 0.55f, 0.3f), 0.015f, _wood);
            foreach (float s in new[] { -0.3f, 0.3f })
            {
                Rod(shop, "ScaleCord", scale + new Vector3(0f, 0.55f, s), scale + new Vector3(0f, 0.25f, s), 0.008f, _wood);
                Cyl(shop, "ScalePan", scale + new Vector3(0f, 0.23f, s), 0.12f, 0.02f, _wood, false);
            }
            Box(shop, "ScaleBar", scale + new Vector3(-0.04f, 0.25f, 0.24f), scale + new Vector3(0.04f, 0.29f, 0.36f), _mark, false);
            Cyl(shop, "Merchant", new Vector3(-3.6f, fy, -9.7f), 0.22f, 0.6f, _slugMat, false);
            Prim(PrimitiveType.Sphere, shop, "MerchantShell", new Vector3(-3.85f, fy + 0.55f, -9.7f), Vector3.one * 0.55f, _slugMat, 0f, null, false);
            Label("問屋の窓口（天秤）・植物の籠の棚", new Vector3(-2.7f, TunY + 2.5f, -9.7f), -90f, 1.2f);   // 口の上の看板の板（BaseGrayboxLiving.cs）より上

            // 水車（出口の西、半分が崖の外）と主軸
            var mill = Group("WaterWheel");
            Prim(PrimitiveType.Cylinder, mill, "Wheel", WheelC, new Vector3(WheelR * 2f, WheelW * 0.5f, WheelR * 2f), _wood, 0f, new Vector3(0f, 0f, 90f), false);
            for (int i = 0; i < 12; i++)
            {
                var q = Quaternion.AngleAxis(i * 30f, Vector3.right);
                Prim(PrimitiveType.Cube, mill, "Paddle", WheelC + q * new Vector3(0f, WheelR + 0.15f, 0f), new Vector3(WheelW + 0.1f, 0.35f, 0.08f), _wood, 0f, q.eulerAngles, false);
            }
            var hub = new Vector3(-2.35f, WheelC.y, WheelC.z);
            Rod(mill, "WheelAxle", WheelC + Vector3.left * 0.5f, hub, 0.12f, _wood);
            Box(mill, "GearBox", hub + new Vector3(-0.25f, -0.35f, -0.35f), hub + new Vector3(0.25f, AxleY - WheelC.y + 0.3f, 0.35f), _wood, false);
            var a0 = new Vector3(AxleX, AxleY, WheelC.z);
            var a1 = new Vector3(AxleX, AxleY, -5.8f);
            var a2 = new Vector3(AxleX, HallAxleY, -5.8f);
            var m = new Vector3(MillAt.x, HallAxleY, MillAt.z);
            var gr = new Vector3(GrindAt.x, HallAxleY, GrindAt.z);
            Rod(mill, "GearAxle", hub, a0, 0.08f, _wood);
            Rod(mill, "MainAxle", a0, a1, 0.1f, _wood);
            Rod(mill, "RiseShaft", a1, a2, 0.08f, _wood);
            Box(mill, "GearBox2", a1 + new Vector3(-0.2f, -0.2f, -0.2f), a2 + new Vector3(0.2f, 0.2f, 0.2f), _wood, false);
            Rod(mill, "MainAxle", a2, m, 0.1f, _wood);
            Rod(mill, "MainAxle", m, gr, 0.1f, _wood);
            Rod(mill, "TurbineShaft", m, new Vector3(m.x, F2Y + 1.0f, m.z), 0.06f, _wood);
            for (float z = -6.5f; z > -12.6f; z -= 2f) Rod(mill, "AxleHanger", new Vector3(AxleX, AxleY, z), new Vector3(AxleX - 0.1f, TunY + TunH - 0.2f, z), 0.04f, _wood);
            // 水車の穴に落ちない手すり
            for (float z = -11.4f; z > MouthZ - 0.05f; z -= 0.6f) Rod(mill, "PitPost", new Vector3(-2.55f, TunY, z), new Vector3(-2.55f, TunY + 0.75f, z), 0.04f, _wood);
            Rod(mill, "PitRail", new Vector3(-2.55f, TunY + 0.75f, -11.4f), new Vector3(-2.55f, TunY + 0.75f, MouthZ), 0.035f, _wood);
            Label("水車", WheelC + new Vector3(1.2f, WheelR + 0.3f, 0.8f), -90f);
        }
    }
}
