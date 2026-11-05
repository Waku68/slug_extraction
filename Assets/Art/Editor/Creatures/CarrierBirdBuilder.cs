using System.Collections.Generic;
using slug = SlugBase;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 籠を運ぶ鳥（Unityコード版、Oct 30、docs/modeling/carrier_bird_code.md）。
    /// メニュー Slug/Creatures/Build Carrier Bird で、メッシュ・マテリアル・プレハブ（A無難・Bずんぐり・C攻め、および本番 CarrierBird）と
    /// 確認シーン Assets/Scenes/Preview/CarrierBirdLook.unity を作り直す。
    ///
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A 無難   ：立った高さ 2.10 m（人なら約 7.5 m）、胴幅 1.30 m、翼幅 7.0 m。いまの寸法のまま、わきにぴったり沿う畳み翼となめらか造形。
    /// - B ずんぐり：立った高さ 1.78 m（人なら約 6.4 m）、胴幅 1.44 m、短足・丸胴・大きなくちばしとのど袋（本番 CarrierBird.prefab に採用）。
    /// - C 攻め   ：立った高さ 1.62 m（人なら約 5.8 m）、胴と頭が1つの丸い塊、体の半分を占める大くちばし、大きな3枚の板翼。
    /// - 原点：両足の間の地面 (0,0,0)。+Y 上、+Z くちばし（前）、+X 右の翼。
    /// - ふわふわ（Oct 30 Claude、わくさん指定「輪郭に羽の房」）：胴と頭の表面に丸い房のふくらみを足す（房どうしの間は浅い谷）。
    ///   胴：房 70 個、幅 σ 0.16 rad、高さ ふだん 1.4 cm → 胸〜腹 4.9 cm・お尻 4.2 cm、背中の上は 4 割。
    ///   頭：房 60 個、幅 σ 0.17 rad、高さ ふだん 2 cm → ほお 4.5 cm・首まわりの襟 5.2 cm・後ろ頭 3.8 cm・てっぺん 3.5 cm、目のすぐまわりとくちばしの付け根は 0。
    ///   （Nov 1 わくさん「ぼこぼこをもう少し小さく」で、どこも約 7 割に）
    /// - 色（sRGB、面はなめらか _Facet=0）：Feather #F5E6D1、WingMid #D1A36B、WingTip #383636、Beak #F59429、Pouch #F5D152、Foot #F2B226、Eye #0D0D0F。
    /// </summary>
    public static partial class CarrierBirdBuilder
    {
        public const string Dir = "Assets/Art/Models/Creatures/CarrierBirdCode";
        public const string PreviewScenePath = "Assets/Scenes/Preview/CarrierBirdLook.unity";

        const int FEATHER = 0, WING_MID = 1, WING_TIP = 2, BEAK = 3, POUCH = 4, FOOT = 5, EYE = 6, SHINE = 7;

        // 目の形：0 長丸（Argon の元）、1 つぶら（丸＋白い光）、2 眠そう（まぶたが上から半分）、3 色の輪（オレンジの輪に小さな黒目）、4 羽の眉（丸＋光＋黄土の眉）
        public static readonly string[] EyeStyleNames = { "長丸", "つぶら", "眠そう", "色の輪", "羽の眉" };

        public struct BirdSpec
        {
            public string tag;
            public Vector3 bodySize;     // width, height, length
            public Vector3 bodyCenter;   // (0, y, z)
            public Vector3 neckPivot;    // world (0, y, z)
            public Vector3 headCenter;   // world (0, y, z)
            public Vector3 headScale;    // radii (rx, ry, rz)
            public float beakLen, beakRootW, beakTipW, beakH, pouchDepth;
            public float wingSpanHalf, wingChordRoot, wingChordTip, wingThick;
            public float legX, legR, legH, footLen, footW;
            public float tailLen, tailW, crestLen;
            public bool slabWings;
            public int eyeStyle;
        }

        public static BirdSpec SpecA() => new BirdSpec
        {
            tag = "A",
            bodySize = new Vector3(1.12f, 1.14f, 1.62f),
            bodyCenter = new Vector3(0f, 0.92f, -0.05f),
            neckPivot = new Vector3(0f, 1.26f, 0.28f),
            headCenter = new Vector3(0f, 1.60f, 0.30f),
            headScale = new Vector3(0.41f, 0.44f, 0.44f),
            beakLen = 0.98f, beakRootW = 0.50f, beakTipW = 0.28f, beakH = 0.17f, pouchDepth = 0.41f,
            wingSpanHalf = 3.00f, wingChordRoot = 0.96f, wingChordTip = 0.48f, wingThick = 0.085f,
            legX = 0.18f, legR = 0.068f, legH = 0.40f, footLen = 0.38f, footW = 0.32f,
            tailLen = 0.55f, tailW = 0.40f, crestLen = 0.36f,
            slabWings = false,
        };

        public static BirdSpec SpecB() => new BirdSpec
        {
            tag = "B",
            bodySize = new Vector3(1.06f, 1.08f, 1.56f),
            bodyCenter = new Vector3(0f, 0.86f, -0.04f),
            neckPivot = new Vector3(0f, 1.18f, 0.26f),
            headCenter = new Vector3(0f, 1.48f, 0.28f),
            headScale = new Vector3(0.39f, 0.42f, 0.42f),
            beakLen = 0.96f, beakRootW = 0.48f, beakTipW = 0.27f, beakH = 0.17f, pouchDepth = 0.40f,
            wingSpanHalf = 2.95f, wingChordRoot = 0.95f, wingChordTip = 0.48f, wingThick = 0.085f,
            legX = 0.17f, legR = 0.066f, legH = 0.36f, footLen = 0.38f, footW = 0.32f,
            tailLen = 0.52f, tailW = 0.38f, crestLen = 0.34f,
            slabWings = false,
        };

        public static BirdSpec SpecC() => new BirdSpec
        {
            tag = "C",
            bodySize = new Vector3(1.18f, 1.12f, 1.58f),
            bodyCenter = new Vector3(0f, 0.82f, -0.02f),
            neckPivot = new Vector3(0f, 1.10f, 0.28f),
            headCenter = new Vector3(0f, 1.38f, 0.30f),
            headScale = new Vector3(0.43f, 0.42f, 0.44f),
            beakLen = 1.02f, beakRootW = 0.54f, beakTipW = 0.32f, beakH = 0.19f, pouchDepth = 0.45f,
            wingSpanHalf = 2.85f, wingChordRoot = 1.00f, wingChordTip = 0.52f, wingThick = 0.095f,
            legX = 0.19f, legR = 0.072f, legH = 0.32f, footLen = 0.40f, footW = 0.34f,
            tailLen = 0.48f, tailW = 0.42f, crestLen = 0.32f,
            slabWings = false,
        };

        [MenuItem("Slug/Creatures/Build Carrier Bird")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var mats = BuildMaterials();

            string sA = BuildVariantPrefab("CarrierBird_A", SpecA(), mats);
            string sB = BuildVariantPrefab("CarrierBird_B", SpecB(), mats);
            string sC = BuildVariantPrefab("CarrierBird_C", SpecC(), mats);
            // 本番用 CarrierBird.prefab（B ずんぐりベース）
            string sMain = BuildVariantPrefab("CarrierBird", SpecB(), mats);

            AssetDatabase.SaveAssets();
            BuildLookScene();
            Debug.Log($"[CarrierBirdBuilder] Built: {sA} | {sB} | {sC} | Main={sMain}");
        }

        static Material[] BuildMaterials()
        {
            string[] names =
            {
                "CarrierBird_Feather", "CarrierBird_WingMid", "CarrierBird_WingTip",
                "CarrierBird_Beak", "CarrierBird_Pouch", "CarrierBird_Foot", "CarrierBird_Eye", "CarrierBird_EyeShine"
            };
            string[] hex = { "#F5E6D1", "#D1A36B", "#383636", "#F59429", "#F5D152", "#F2B226", "#0D0D0F", "#FFFFFF" };
            float[] rims = { 0.18f, 0.15f, 0.12f, 0.22f, 0.18f, 0.16f, 0.05f, 0f };

            var mats = new Material[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                ColorUtility.TryParseHtmlString(hex[i], out var col);
                var m = WorldLook.GetOrCreate($"{Dir}/{names[i]}.mat");
                WorldLook.Apply(m, col, rims[i], 0f);
                m.SetFloat("_Facet", 0f); // 生き物はなめらか
                EditorUtility.SetDirty(m);
                mats[i] = m;
            }
            return mats;
        }

        static string BuildVariantPrefab(string prefabName, BirdSpec s, Material[] mats)
        {
            int totalTris = 0;
            var rootGo = new GameObject(prefabName);
            var motion = rootGo.AddComponent<slug.CarrierBirdMotion>();

            var cbRoot = new GameObject("CarrierBird_Root").transform;
            cbRoot.SetParent(rootGo.transform, false);

            var bodyT = new GameObject("Body").transform;
            bodyT.SetParent(cbRoot, false);
            bodyT.localPosition = s.bodyCenter;

            // 1. 胴（Body の子）
            var bodyMesh = MakeDropletBody(s, out int tBody); totalTris += tBody;
            var bodyGo = MakePart("CarrierBird_Body", bodyT, Vector3.zero, SaveMesh(bodyMesh, $"{prefabName}_Body"), mats[FEATHER]);
            var bodyCol = bodyGo.AddComponent<CapsuleCollider>();
            bodyCol.direction = 2; // Z軸方向
            bodyCol.center = new Vector3(0f, 0.02f, 0.02f);
            bodyCol.radius = s.bodySize.x * 0.46f;
            bodyCol.height = s.bodySize.z * 0.92f;

            // 2. 首（Body の子）
            var neckT = new GameObject("Neck").transform;
            neckT.SetParent(bodyT, false);
            neckT.localPosition = s.neckPivot - s.bodyCenter;

            Vector3 headLocal = s.headCenter - s.neckPivot;
            var headMesh = MakeHeadWithCollar(s, headLocal, out int tHead); totalTris += tHead;
            var headGo = MakePart("CarrierBird_Head", neckT, Vector3.zero, SaveMesh(headMesh, $"{prefabName}_Head"), mats[FEATHER]);
            var headCol = headGo.AddComponent<SphereCollider>();
            headCol.center = headLocal;
            headCol.radius = Mathf.Max(s.headScale.x, s.headScale.y) * 0.95f;

            // 冠羽（3房）
            var crestMesh = MakeCrest(s, headLocal, out int tCrest); totalTris += tCrest;
            MakePart("CarrierBird_Crest", neckT, Vector3.zero, SaveMesh(crestMesh, $"{prefabName}_Crest"), mats[FEATHER]);

            // 上くちばし
            Vector3 beakBase = headLocal + new Vector3(0f, -s.headScale.y * 0.10f, s.headScale.z * 0.72f);
            var beakMesh = MakeUpperBeak(s, beakBase, out int tBeak); totalTris += tBeak;
            MakePart("CarrierBird_BeakUpper", neckT, Vector3.zero, SaveMesh(beakMesh, $"{prefabName}_BeakUpper"), mats[BEAK]);

            // のど袋（回転中心＝あごの付け根）
            Vector3 jawPivotLocal = beakBase + new Vector3(0f, -0.06f, -0.05f);
            var pouchMesh = MakePouch(s, beakBase - jawPivotLocal, out int tPouch); totalTris += tPouch;
            MakePart("CarrierBird_Pouch", neckT, jawPivotLocal, SaveMesh(pouchMesh, $"{prefabName}_Pouch"), mats[POUCH]);

            // 目（左右）
            totalTris += AddEyes(s, s.eyeStyle, headLocal, neckT, mats, prefabName);

            // 3. たたんだ翼（3色マテリアル）
            var wingMats = new[] { mats[FEATHER], mats[WING_MID], mats[WING_TIP] };
            var foldMeshL = MakeFoldedWing(s, -1f, out int tFoldL); totalTris += tFoldL;
            var foldMeshR = MakeFoldedWing(s, +1f, out int tFoldR); totalTris += tFoldR;
            MakePartMulti("CarrierBird_WingFolded_L", cbRoot, s.bodyCenter, SaveMesh(foldMeshL, $"{prefabName}_WingFolded_L"), wingMats);
            MakePartMulti("CarrierBird_WingFolded_R", cbRoot, s.bodyCenter, SaveMesh(foldMeshR, $"{prefabName}_WingFolded_R"), wingMats);

            // 4. 広げた翼（肩が回転中心、ふだんは非表示）
            Vector3 shoulderR = s.bodyCenter + new Vector3(s.bodySize.x * 0.38f, s.bodySize.y * 0.16f, 0.10f);
            Vector3 shoulderL = new Vector3(-shoulderR.x, shoulderR.y, shoulderR.z);
            var openMeshL = MakeOpenWing(s, -1f, out int tOpenL); totalTris += tOpenL;
            var openMeshR = MakeOpenWing(s, +1f, out int tOpenR); totalTris += tOpenR;
            var openGoL = MakePartMulti("CarrierBird_WingOpen_L", cbRoot, shoulderL, SaveMesh(openMeshL, $"{prefabName}_WingOpen_L"), wingMats);
            var openGoR = MakePartMulti("CarrierBird_WingOpen_R", cbRoot, shoulderR, SaveMesh(openMeshR, $"{prefabName}_WingOpen_R"), wingMats);
            openGoL.SetActive(false);
            openGoR.SetActive(false);

            // 5. 尾
            Vector3 tailBase = s.bodyCenter + new Vector3(0f, 0.05f, -s.bodySize.z * 0.48f);
            var tailMesh = MakeTail(s, out int tTail); totalTris += tTail;
            MakePart("CarrierBird_Tail", cbRoot, tailBase, SaveMesh(tailMesh, $"{prefabName}_Tail"), mats[FEATHER]);

            // 6. 脚と足
            var legMeshL = MakeLeg(s, -1f, out int tLegL); totalTris += tLegL;
            var legMeshR = MakeLeg(s, +1f, out int tLegR); totalTris += tLegR;
            MakePart("CarrierBird_Leg_L", cbRoot, Vector3.zero, SaveMesh(legMeshL, $"{prefabName}_Leg_L"), mats[FOOT]);
            MakePart("CarrierBird_Leg_R", cbRoot, Vector3.zero, SaveMesh(legMeshR, $"{prefabName}_Leg_R"), mats[FOOT]);

            var footMeshL = MakeFoot(s, -1f, out int tFootL); totalTris += tFootL;
            var footMeshR = MakeFoot(s, +1f, out int tFootR); totalTris += tFootR;
            MakePart("CarrierBird_Foot_L", cbRoot, Vector3.zero, SaveMesh(footMeshL, $"{prefabName}_Foot_L"), mats[FOOT]);
            MakePart("CarrierBird_Foot_R", cbRoot, Vector3.zero, SaveMesh(footMeshR, $"{prefabName}_Foot_R"), mats[FOOT]);

            PrefabUtility.SaveAsPrefabAsset(rootGo, $"{Dir}/{prefabName}.prefab");
            Object.DestroyImmediate(rootGo);
            return $"{prefabName} ({totalTris} tris)";
        }

        static GameObject MakePart(string name, Transform parent, Vector3 localPos, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject MakePartMulti(string name, Transform parent, Vector3 localPos, Mesh mesh, Material[] mats)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            return go;
        }

        static Mesh SaveMesh(Mesh m, string name)
        {
            m.name = name;
            string path = $"{Dir}/{name}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) { old.Clear(); EditorUtility.CopySerialized(m, old); EditorUtility.SetDirty(old); return old; }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ---- 各部品のメッシュ生成 ----

        static Mesh MakeDropletBody(BirdSpec s, out int tris)
        {
            var kit = new NatureMeshKit(1);
            int rings = 44, segs = 64;
            Vector3 ext = s.bodySize * 0.5f;
            var tufts = TuftDirs(70, 11);
            int start = kit.VertexCount;
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings, phi = Mathf.PI * v;
                float cy = -Mathf.Cos(phi), sr = Mathf.Sin(phi);
                for (int x = 0; x <= segs; x++)
                {
                    float u = x / (float)segs, th = Mathf.PI * 2f * u;
                    float nx = Mathf.Cos(th) * sr;
                    float nz = Mathf.Sin(th) * sr;
                    // すっきりしたしずく形（横への張り出しを抑え、背中〜尾へスッと細る）
                    float backTaper = nz < 0f ? 1f + 0.34f * nz : 1f - 0.04f * nz;
                    float bellyFull = cy < 0f ? 0.96f : 0.94f;
                    float tailLift = nz < 0f ? (-nz) * (-nz) * 0.14f * ext.y : -0.03f * nz * ext.y;
                    Vector3 p = new Vector3(
                        nx * ext.x * backTaper * bellyFull,
                        cy * ext.y + tailLift,
                        nz * ext.z
                    );
                    // 羽の房：胸〜腹とお尻を大きく、背中の上は控えめ
                    var n = new Vector3(nx, cy, nz);
                    float chest = Mathf.Clamp01(0.5f + 0.6f * nz - 0.6f * cy);
                    float rump = Mathf.Clamp01(-1.2f * nz - 0.5f) * Mathf.Clamp01(0.6f - cy);
                    float top = cy > 0.5f ? 0.4f : 1f;
                    float amp = (0.014f + 0.035f * chest * chest + 0.028f * rump) * top;
                    float fluff = Tuft(n, tufts, 0.16f) * amp;
                    var outDir = new Vector3(nx / ext.x, cy / ext.y, nz / ext.z).normalized;
                    kit.Vert(p + outDir * fluff, new Vector2(u, v));
                }
            }
            GridQuads(kit, 0, start, rings, segs);
            tris = kit.TriangleCount;
            return kit.ToMesh("Body");
        }

        static Mesh MakeHeadWithCollar(BirdSpec s, Vector3 headLocal, out int tris)
        {
            var kit = new NatureMeshKit(1);
            int rings = 44, segs = 64;
            var tufts = TuftDirs(60, 23);
            var eyeDir = new Vector3(0.78f, 0.20f, 0.58f).normalized;
            int start = kit.VertexCount;
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings, phi = Mathf.PI * v;
                float cy = -Mathf.Cos(phi), sr = Mathf.Sin(phi);
                for (int x = 0; x <= segs; x++)
                {
                    float u = x / (float)segs, th = Mathf.PI * 2f * u;
                    float nx = Mathf.Cos(th) * sr;
                    float nz = Mathf.Sin(th) * sr;
                    // 横ぶくれせず胸へすっとつながるスマートな首元
                    float skirt = cy < -0.2f ? Mathf.Lerp(1f, 1.05f, (-cy - 0.2f) / 0.8f) : 1f;
                    float deepY = cy < 0f ? cy * s.headScale.y * 1.32f : cy * s.headScale.y;
                    Vector3 p = headLocal + new Vector3(
                        nx * s.headScale.x * skirt,
                        deepY,
                        nz * s.headScale.z * skirt
                    );
                    // 羽の房：頭ぜんぶ。ほお・首まわりの襟・後ろ頭・てっぺんを大きく。目のすぐまわりとくちばしの付け根は平らのまま
                    var n = new Vector3(nx, cy, nz);
                    float cheek = Mathf.Clamp01(Mathf.Abs(nx) * 1.6f - 0.6f) * Mathf.Clamp01(0.4f - cy * 1.5f);
                    float collar = Mathf.Clamp01(-cy * 2f - 0.6f);
                    float nape = Mathf.Clamp01(-nz * 1.5f - 0.3f) * Mathf.Clamp01(0.6f - cy);
                    float crown = Mathf.Clamp01(cy * 2f - 0.9f);
                    float amp = 0.02f + Mathf.Max(Mathf.Max(0.045f * cheek, 0.052f * collar), Mathf.Max(0.038f * nape, 0.035f * crown));
                    float eyeNear = Mathf.Max(
                        Vector3.Dot(n, eyeDir),
                        Vector3.Dot(n, new Vector3(-eyeDir.x, eyeDir.y, eyeDir.z)));
                    amp *= Mathf.Clamp01((0.9f - eyeNear) / 0.08f);
                    float beakRoot = Vector3.Dot(n, new Vector3(0f, -0.1f, 1f).normalized);
                    amp *= Mathf.Clamp01((0.92f - beakRoot) / 0.2f);
                    float fluff = Tuft(n, tufts, 0.17f) * amp;
                    var outDir = new Vector3(nx / s.headScale.x, cy / s.headScale.y, nz / s.headScale.z).normalized;
                    kit.Vert(p + outDir * fluff, new Vector2(u, v));
                }
            }
            GridQuads(kit, 0, start, rings, segs);
            tris = kit.TriangleCount;
            return kit.ToMesh("Head");
        }

        // 球の上にほぼ均等に房の向きを置く（フィボナッチ球＋少しのゆらぎ、seed で毎回同じ）
        static Vector3[] TuftDirs(int count, int seed)
        {
            var rnd = new System.Random(seed);
            var dirs = new Vector3[count];
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < count; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / count;
                float r = Mathf.Sqrt(1f - y * y);
                float a = golden * i;
                var d = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                d += new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 0.12f;
                dirs[i] = d.normalized;
            }
            return dirs;
        }

        // いちばん近い房の高さ（0..1）。房どうしの間は浅い谷になる
        static float Tuft(Vector3 n, Vector3[] dirs, float sigma)
        {
            float best = 0f, k = 1f / (2f * sigma * sigma);
            foreach (var d in dirs)
            {
                float dot = Vector3.Dot(n, d);
                if (dot < 0.6f) continue;
                float ang = Mathf.Acos(Mathf.Min(1f, dot));
                best = Mathf.Max(best, Mathf.Exp(-ang * ang * k));
            }
            return best;
        }

        static Mesh MakeCrest(BirdSpec s, Vector3 headLocal, out int tris)
        {
            var kit = new NatureMeshKit(1);
            // 頭の後ろになびく柔らかい丸いしずく房 3 つ
            Vector3 basePos = headLocal + new Vector3(0f, s.headScale.y * 0.52f, -s.headScale.z * 0.66f);
            float[] xOff = { 0f, -0.09f, 0.09f };
            float[] pitchDeg = { 24f, 15f, 15f };
            float[] yawDeg = { 0f, -18f, 18f };
            float[] scale = { 1.0f, 0.82f, 0.82f };
            int rings = 14, segs = 18;
            for (int i = 0; i < 3; i++)
            {
                float len = s.crestLen * scale[i];
                float rad = 0.064f * scale[i];
                Quaternion rot = Quaternion.Euler(pitchDeg[i], yawDeg[i], 0f);
                Vector3 p0 = basePos + new Vector3(xOff[i], -0.02f * Mathf.Abs(i), 0f);
                int start = kit.VertexCount;
                for (int y = 0; y <= rings; y++)
                {
                    float v = y / (float)rings, phi = Mathf.PI * v;
                    float cz = -Mathf.Cos(phi), sr = Mathf.Sin(phi);
                    float t = (cz + 1f) * 0.5f;
                    float profile = Mathf.Sin(phi) * (1f - 0.25f * t);
                    float archY = Mathf.Sin(t * Mathf.PI) * 0.035f;
                    for (int x = 0; x <= segs; x++)
                    {
                        float u = x / (float)segs, th = Mathf.PI * 2f * u;
                        Vector3 local = new Vector3(
                            Mathf.Cos(th) * rad * profile * 1.10f,
                            Mathf.Sin(th) * rad * profile * 0.85f + archY,
                            -t * len
                        );
                        kit.Vert(p0 + rot * local, new Vector2(u, v));
                    }
                }
                GridQuads(kit, 0, start, rings, segs);
            }
            tris = kit.TriangleCount;
            return kit.ToMesh("Crest");
        }

        static Mesh MakeUpperBeak(BirdSpec s, Vector3 beakBase, out int tris)
        {
            var kit = new NatureMeshKit(1);
            int lenSegs = 24, ringSegs = 28;
            int start = kit.VertexCount;
            for (int i = 0; i <= lenSegs; i++)
            {
                float t = i / (float)lenSegs;
                // 先は丸く閉じて少し下がる（とがらない）
                float z = s.beakLen * Mathf.Sin(t * Mathf.PI * 0.5f);
                float halfW = Mathf.Lerp(s.beakRootW * 0.5f, s.beakTipW * 0.5f, t * 0.85f) * Mathf.Cos(t * Mathf.PI * 0.48f);
                float h = s.beakH * (1f - 0.55f * t) * Mathf.Cos(t * Mathf.PI * 0.48f);
                float droop = -0.07f * t * t;
                for (int j = 0; j <= ringSegs; j++)
                {
                    float u = j / (float)ringSegs, th = Mathf.PI * 2f * u;
                    float cx = Mathf.Cos(th), sy = Mathf.Sin(th);
                    float y = sy >= 0f ? sy * h : sy * h * 0.28f;
                    float sideRidge = Mathf.Exp(-sy * sy * 8f) * 0.018f * Mathf.Sin(t * Mathf.PI);
                    Vector3 p = beakBase + new Vector3(cx * (halfW + sideRidge), y + droop, z);
                    kit.Vert(p, new Vector2(u, t));
                }
            }
            GridQuadsFlip(kit, 0, start, lenSegs, ringSegs);
            tris = kit.TriangleCount;
            return kit.ToMesh("BeakUpper");
        }

        static Mesh MakePouch(BirdSpec s, Vector3 offsetFromHinge, out int tris)
        {
            var kit = new NatureMeshKit(1);
            int lenSegs = 24, ringSegs = 28;
            float pouchLen = s.beakLen * 0.86f;
            int start = kit.VertexCount;
            for (int i = 0; i <= lenSegs; i++)
            {
                float t = i / (float)lenSegs;
                float z = pouchLen * Mathf.Sin(t * Mathf.PI * 0.5f);
                float env = Mathf.Max(0f, Mathf.Sin(Mathf.Pow(Mathf.Max(0f, t), 0.62f) * Mathf.PI));
                float halfW = Mathf.Lerp(s.beakRootW * 0.46f, s.beakTipW * 0.40f, t) * Mathf.Max(0f, Mathf.Cos(t * Mathf.PI * 0.47f));
                float depth = s.pouchDepth * env;
                for (int j = 0; j <= ringSegs; j++)
                {
                    float u = j / (float)ringSegs, th = Mathf.PI * 2f * u;
                    float cx = Mathf.Cos(th), sy = Mathf.Sin(th);
                    float y = sy <= 0f ? sy * depth : sy * 0.040f * env;
                    Vector3 p = offsetFromHinge + new Vector3(cx * halfW, y - 0.02f * t, z);
                    kit.Vert(p, new Vector2(u, t));
                }
            }
            GridQuadsFlip(kit, 0, start, lenSegs, ringSegs);
            tris = kit.TriangleCount;
            return kit.ToMesh("Pouch");
        }

        // 目の部品を左右に作って neckT の子にする。prefabName が null ならメッシュを保存しない（見比べ用）
        static int AddEyes(BirdSpec s, int style, Vector3 headLocal, Transform neckT, Material[] mats, string prefabName)
        {
            int tris = 0;
            foreach (float side in new[] { -1f, 1f })
            {
                string lr = side < 0f ? "L" : "R";
                foreach (var part in MakeEyeParts(s, style, headLocal, side))
                {
                    tris += part.mesh.triangles.Length / 3;
                    string name = $"CarrierBird_{part.name}_{lr}";
                    var mesh = prefabName != null ? SaveMesh(part.mesh, $"{prefabName}_{part.name}_{lr}") : part.mesh;
                    MakePart(name, neckT, Vector3.zero, mesh, mats[part.mat]);
                }
            }
            return tris;
        }

        static List<(string name, Mesh mesh, int mat)> MakeEyeParts(BirdSpec s, int style, Vector3 headLocal, float side)
        {
            var parts = new List<(string, Mesh, int)>();
            Vector3 pos = headLocal + new Vector3(
                side * s.headScale.x * 0.78f,
                s.headScale.y * 0.20f,
                s.headScale.z * 0.58f
            );
            if (style == 0)
            {
                Quaternion rot0 = Quaternion.Euler(-8f, side * 28f, -side * 6f);
                parts.Add(("Eye", Ellipsoid(pos, new Vector3(0.032f, 0.100f, 0.042f), rot0, "Eye"), EYE));
                return parts;
            }

            // 頭の面に沿う向き：o 外へ、u 上、f 前
            Vector3 o = new Vector3(side * 0.78f / s.headScale.x, 0.20f / s.headScale.y, 0.58f / s.headScale.z).normalized;
            Vector3 f = Vector3.ProjectOnPlane(Vector3.forward, o).normalized;
            Vector3 u = Vector3.Cross(f, o) * side;
            Quaternion rot = Quaternion.LookRotation(f, u); // x＝厚み、y＝上、z＝前
            Vector3 round = new Vector3(0.034f, 0.074f, 0.068f);

            switch (style)
            {
                case 1: // つぶら
                    parts.Add(("Eye", Ellipsoid(pos, round, rot, "Eye"), EYE));
                    parts.Add(("EyeShine", Ellipsoid(pos + o * 0.03f + u * 0.032f + f * 0.02f, Vector3.one * 0.019f, rot, "EyeShine"), SHINE));
                    break;
                case 2: // 眠そう：まぶたが上から黒目の半分にかぶる
                    parts.Add(("Eye", Ellipsoid(pos, round, rot, "Eye"), EYE));
                    parts.Add(("Lid", Ellipsoid(pos + u * 0.045f + o * 0.006f, new Vector3(0.05f, 0.058f, 0.088f), rot, "Lid"), FEATHER));
                    break;
                case 3: // 色の輪：オレンジの平たい輪に小さな黒目
                    parts.Add(("EyeRing", Ellipsoid(pos - o * 0.006f, new Vector3(0.022f, 0.105f, 0.105f), rot, "EyeRing"), BEAK));
                    parts.Add(("Eye", Ellipsoid(pos + o * 0.008f, new Vector3(0.032f, 0.052f, 0.052f), rot, "Eye"), EYE));
                    parts.Add(("EyeShine", Ellipsoid(pos + o * 0.034f + u * 0.02f + f * 0.014f, Vector3.one * 0.013f, rot, "EyeShine"), SHINE));
                    break;
                default: // 羽の眉：目の上に黄土色の短い眉。前（くちばし側）が少し上がって、やさしい顔
                    parts.Add(("Eye", Ellipsoid(pos, round, rot, "Eye"), EYE));
                    parts.Add(("EyeShine", Ellipsoid(pos + o * 0.03f + u * 0.032f + f * 0.02f, Vector3.one * 0.019f, rot, "EyeShine"), SHINE));
                    Quaternion browRot = rot * Quaternion.AngleAxis(-18f, Vector3.right);
                    parts.Add(("Brow", Ellipsoid(pos + u * 0.088f + o * 0.026f, new Vector3(0.034f, 0.024f, 0.10f), browRot, "Brow"), WING_MID));
                    break;
            }
            return parts;
        }

        static Mesh Ellipsoid(Vector3 center, Vector3 radii, Quaternion rot, string name)
        {
            var kit = new NatureMeshKit(1);
            AddEllipsoid(kit, 0, center, radii, rot, 12, 18);
            return kit.ToMesh(name);
        }

        // 見比べ：開いているシーンに本番の鳥を目の形ごとに並べる（メッシュは保存しない）
        public static void PlaceEyeTrial(Vector3 start, Vector3 step)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/CarrierBird.prefab");
            var mats = BuildMaterials();
            var s = SpecB();
            Vector3 headLocal = s.headCenter - s.neckPivot;
            for (int style = 0; style < EyeStyleNames.Length; style++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                go.name = $"EyeTrial_{style}_{EyeStyleNames[style]}";
                go.transform.position = start + step * style;
                var neckT = FindChild(go.transform, "Neck");
                foreach (var t in neckT.GetComponentsInChildren<Transform>(true))
                    if (t != neckT && t.name.StartsWith("CarrierBird_Eye")) Object.DestroyImmediate(t.gameObject);
                AddEyes(s, style, headLocal, neckT, mats, null);
            }
        }
    }
}
