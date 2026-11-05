using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 拠点「セノーテの洞窟」と家具の共通マテリアル＆造形ヘルパー（Unityコード版、Oct 30）。
    /// わくさん決定：
    /// - 岩壁・流木・家具はすべて「カクカクの面（Flat Facets, _Facet = 1）」で、角を丸めたずんぐり・ころんとした塊にする。
    /// - Slug/Toon の陰で暗く沈まないよう、石灰岩（#E5DFCF）や流木（#C8BAA4）はワントーン明るい色にする。
    /// </summary>
    public static partial class CenoteBaseCodeBuilder
    {
        public const string BaseDir = "Assets/Art/Models/Base/CenoteBaseCode";
        public const string FurnDir = "Assets/Art/Models/Base/CenoteFurnitureCode";
        public const string MatDir  = "Assets/Art/Models/Base/CenoteBaseCode/Materials";
        public const string PreviewScenePath = "Assets/Scenes/Preview/CenoteBaseLook.unity";

        // マテリアル番号
        public const int
            M_LIMESTONE = 0, M_LIMESTONE_WET = 1, M_DRIFTWOOD = 2, M_DRIFTWOOD_DARK = 3,
            M_VINE = 4, M_COIR = 5, M_RUG = 6, M_LEAF = 7, M_THATCH = 8, M_MOSS = 9,
            M_BARK = 10, M_NOREN = 11, M_CLOTH_RED = 12, M_CLOTH_INDIGO = 13, M_CLOTH_OCHRE = 14,
            M_CLAM = 15, M_CLAM_IN = 16, M_CONCH = 17, M_WHALE = 18, M_COCONUT = 19,
            M_CLAY = 20, M_LAVA = 21, M_OBSIDIAN = 22, M_CORAL = 23, M_SOIL = 24,
            M_COMPOST = 25, M_ASH = 26, M_SALT = 27, M_BLACK_SALT = 28, M_PILE = 29,
            M_WATER = 30, M_DYE_RED = 31, M_DYE_INDIGO = 32, M_DYE_OCHRE = 33,
            M_COPPER = 34, M_BRASS = 35, M_FLAME = 36, M_FEATHER = 37, M_CHARCOAL = 38,
            M_BONE = 39, M_MAP = 40, M_COUNT = 41;

        struct MatDef
        {
            public string name, hex;
            public float rim, mottle, dent;
            public string mottleHex;
            public bool twoSided, emissive;
            public MatDef(string n, string h, float r = 0.08f, float m = 0.2f, float d = 0.2f, string mh = null, bool ts = false, bool em = false)
            { name = n; hex = h; rim = r; mottle = m; dent = d; mottleHex = mh; twoSided = ts; emissive = em; }
        }

        static readonly MatDef[] Defs = new MatDef[M_COUNT]
        {
            new MatDef("CenoteBase_Limestone",     "#E5DFCF", 0.12f, 0.28f, 0.35f),
            new MatDef("CenoteBase_LimestoneWet",  "#B6B09E", 0.20f, 0.30f, 0.35f),
            new MatDef("CenoteBase_Driftwood",     "#C8BAA4", 0.08f, 0.22f, 0.20f),
            new MatDef("CenoteBase_DriftwoodDark", "#9A8A72", 0.10f, 0.22f, 0.20f),
            new MatDef("CenoteBase_Vine",          "#74734C", 0.06f, 0.15f, 0.10f),
            new MatDef("CenoteBase_Coir",          "#987B58", 0.06f, 0.20f, 0.25f),
            new MatDef("CenoteBase_Rug",           "#9A8C6A", 0.06f, 0.20f, 0.10f, null, true),
            new MatDef("CenoteBase_Leaf",          "#586B42", 0.10f, 0.20f, 0.10f, null, true),
            new MatDef("CenoteBase_Thatch",        "#B0A070", 0.06f, 0.22f, 0.20f, null, true),
            new MatDef("CenoteBase_Moss",          "#768650", 0.06f, 0.35f, 0.40f),
            new MatDef("CenoteBase_BarkCloth",     "#E2D6BA", 0.06f, 0.15f, 0.10f, null, true),
            new MatDef("CenoteBase_Noren",         "#E2D6BA", 0.06f, 0.10f, 0.05f, null, true),
            new MatDef("CenoteBase_ClothRed",      "#B46E62", 0.06f, 0.10f, 0.05f, null, true),
            new MatDef("CenoteBase_ClothIndigo",   "#667896", 0.06f, 0.10f, 0.05f, null, true),
            new MatDef("CenoteBase_ClothOchre",    "#CCA05A", 0.06f, 0.10f, 0.05f, null, true),
            new MatDef("CenoteBase_ClamShell",     "#EAE3D2", 0.12f, 0.20f, 0.25f),
            new MatDef("CenoteBase_ClamInner",     "#F4EFE4", 0.32f, 0.05f, 0.00f),
            new MatDef("CenoteBase_Conch",         "#E0BEA2", 0.24f, 0.30f, 0.10f, "#B08060"),
            new MatDef("CenoteBase_WhaleBone",     "#ECE4D0", 0.08f, 0.22f, 0.35f, "#C2B8A2"),
            new MatDef("CenoteBase_Coconut",       "#7A5C48", 0.10f, 0.25f, 0.25f),
            new MatDef("CenoteBase_Clay",          "#C09072", 0.06f, 0.20f, 0.15f),
            new MatDef("CenoteBase_LavaStone",     "#56524E", 0.08f, 0.28f, 0.45f, "#423F3C"),
            new MatDef("CenoteBase_Obsidian",      "#2E2E36", 0.45f, 0.00f, 0.00f),
            new MatDef("CenoteBase_Coral",         "#D8CEC0", 0.06f, 0.20f, 0.55f),
            new MatDef("CenoteBase_Soil",          "#645344", 0.02f, 0.30f, 0.35f),
            new MatDef("CenoteBase_Compost",       "#504238", 0.02f, 0.35f, 0.45f),
            new MatDef("CenoteBase_Ash",           "#A4A098", 0.02f, 0.20f, 0.25f),
            new MatDef("CenoteBase_Salt",          "#F4F1EA", 0.16f, 0.10f, 0.25f),
            new MatDef("CenoteBase_BlackSalt",     "#585458", 0.20f, 0.15f, 0.25f),
            new MatDef("CenoteBase_Pile",          "#D2CBB8", 0.06f, 0.20f, 0.25f),
            new MatDef("CenoteBase_Water",         "#448890", 0.32f, 0.00f, 0.00f),
            new MatDef("CenoteBase_DyeRed",        "#985C56", 0.30f, 0.00f, 0.00f),
            new MatDef("CenoteBase_DyeIndigo",     "#56748C", 0.30f, 0.00f, 0.00f),
            new MatDef("CenoteBase_DyeOchre",      "#B49C56", 0.30f, 0.00f, 0.00f),
            new MatDef("CenoteBase_Copper",        "#B88254", 0.25f, 0.30f, 0.10f, "#749278"),
            new MatDef("CenoteBase_Brass",         "#B29044", 0.22f, 0.25f, 0.05f, "#749278"),
            new MatDef("CenoteBase_LampFlame",     "#FFB860", 0.45f, 0.00f, 0.00f, null, false, true),
            new MatDef("CenoteBase_Feather",       "#F2ECE0", 0.06f, 0.05f, 0.00f, null, true),
            new MatDef("CenoteBase_Charcoal",      "#403C3A", 0.02f, 0.10f, 0.00f),
            new MatDef("CenoteBase_Bone",          "#ECE6D6", 0.10f, 0.05f, 0.10f),
            new MatDef("CenoteBase_Map",           "#DFD2B5", 0.05f, 0.10f, 0.05f),
        };

        public static Material[] BuildAllMaterials()
        {
            System.IO.Directory.CreateDirectory(MatDir);
            var mats = new Material[M_COUNT];
            for (int i = 0; i < M_COUNT; i++)
            {
                var d = Defs[i];
                ColorUtility.TryParseHtmlString(d.hex, out var col);
                Color? mc = null;
                if (!string.IsNullOrEmpty(d.mottleHex) && ColorUtility.TryParseHtmlString(d.mottleHex, out var mCol))
                    mc = mCol;
                var m = WorldLook.GetOrCreate($"{MatDir}/{d.name}.mat");
                // ポリゴンのカクつき・荒さをなくすため、高分割メッシュ＋なめらか法線（_Facet = 0）で統一する
                WorldLook.Apply(m, col, d.rim, 0f, mc, 12f, 0f, 70f, d.twoSided);
                m.SetFloat("_Facet", 0f);
                if (d.emissive)
                {
                    m.SetFloat("_AmbientStrength", 0.95f);
                    m.SetColor("_ShadeColor", col);
                }
                EditorUtility.SetDirty(m);
                mats[i] = m;
            }
            return mats;
        }

        // ---- 共通の造形ヘルパー（高分割・角丸のなめらかな塊を作る） ----

        /// <summary>角を丸めた平たい・ずんぐりした岩や木の塊（自動で高分割化してポリゴンの荒さをなくす）。</summary>
        public static void AddChunkyBlob(NatureMeshKit kit, int sub, Vector3 pos, Vector3 size, Quaternion rot,
                                         int rings = 12, int segs = 24, float powerY = 0.72f, float powerR = 0.80f,
                                         float wobble = 0.05f, float seed = 1.0f, float taperZ = 0f)
        {
            rings = Mathf.Max(rings * 2, 12);
            segs = Mathf.Max(segs * 2, 22);
            Vector3 ext = size * 0.5f;
            int start = kit.VertexCount;
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings, phi = Mathf.PI * v;
                float cy = -Mathf.Cos(phi), sr = Mathf.Max(0f, Mathf.Sin(phi));
                float py = Mathf.Sign(cy) * Mathf.Pow(Mathf.Abs(cy), powerY) * ext.y;
                float pr = Mathf.Pow(sr, powerR);
                for (int x = 0; x <= segs; x++)
                {
                    int xi = x % segs;
                    float th = Mathf.PI * 2f * xi / segs;
                    float wob = (y == 0 || y == rings) ? 1f : 1f + wobble * Mathf.Sin(th * 2f + seed) + wobble * 0.6f * Mathf.Cos(th * 3f + seed * 1.7f + v * 2.5f);
                    float tip = Mathf.Sin(th);
                    float tx = 1f - taperZ * tip;
                    Vector3 local = new Vector3(Mathf.Cos(th) * pr * ext.x * wob * tx, py, tip * pr * ext.z * wob);
                    kit.Vert(pos + rot * local, new Vector2(x / (float)segs, v));
                }
            }
            for (int y = 0; y < rings; y++)
                for (int x = 0; x < segs; x++)
                {
                    int a = start + y * (segs + 1) + x, b = a + segs + 1;
                    kit.Quad(sub, a, b, b + 1, a + 1);
                }
        }

        /// <summary>角を丸めた高分割の板・箱（流木の板、作業台、プランター、塩箱など）。</summary>
        public static void AddPlank(NatureMeshKit kit, int sub, Vector3 center, Vector3 size, Quaternion rot, float warpY = 0f, int segs = 20)
        {
            AddChunkyBlob(kit, sub, center + Vector3.up * warpY, size, rot, 7, Mathf.Max(segs, 12), 0.42f, 0.52f, 0.02f, center.x * 3.1f + center.z * 1.7f);
        }

        /// <summary>2点 p0 → p1 を結ぶ太く滑らかな流木の柱・丸太・縄。</summary>
        public static void AddLog(NatureMeshKit kit, int sub, Vector3 p0, Vector3 p1, float r0, float r1, int sides = 16, float bow = 0f, Vector3 bowDir = default)
        {
            sides = Mathf.Max(sides * 2, 16);
            if (bowDir == Vector3.zero) bowDir = Vector3.up;
            var rows = new List<Vector3>
            {
                new Vector3(0.00f, 0.88f, sub),
                new Vector3(0.08f, 0.98f, sub),
                new Vector3(0.22f, 1.02f, sub),
                new Vector3(0.38f, 1.00f, sub),
                new Vector3(0.50f, 1.00f, sub),
                new Vector3(0.62f, 1.00f, sub),
                new Vector3(0.78f, 1.02f, sub),
                new Vector3(0.92f, 0.98f, sub),
                new Vector3(1.00f, 0.88f, sub),
            };
            kit.BeginTube();
            kit.Tube(
                t => Vector3.Lerp(p0, p1, t) + bowDir * (bow * Mathf.Sin(t * Mathf.PI)),
                t => Mathf.Lerp(r0, r1, t),
                rows, sides, sub, sub
            );
        }

        /// <summary>閉じた高分割の輪（つるの輪、樽のタガ、縄の輪、籠の縁など）。</summary>
        public static void AddTorus(NatureMeshKit kit, int sub, Vector3 center, Quaternion rot, float majorRX, float majorRZ, float tubeR, int segs = 28, int tubeSides = 12)
        {
            segs = Mathf.Max(segs * 2, 28);
            tubeSides = Mathf.Max(tubeSides * 2, 12);
            var rows = new List<Vector3>();
            for (int i = 0; i <= segs; i++) rows.Add(new Vector3(i / (float)segs, 1f, sub));
            kit.BeginTube();
            kit.Tube(
                t =>
                {
                    float a = Mathf.PI * 2f * t;
                    return center + rot * new Vector3(Mathf.Cos(a) * majorRX, 0f, Mathf.Sin(a) * majorRZ);
                },
                _ => tubeR,
                rows, tubeSides, -1, -1
            );
        }

        /// <summary>厚みのある高分割の器・籠・鉢・皿（底が閉じて上が開いた滑らかな形）。</summary>
        public static void AddVessel(NatureMeshKit kit, int subOuter, int subInner, Vector3 bottomCenter, Quaternion rot,
                                     float rBottom, float rTop, float height, float wallThick = 0.05f, int segs = 24)
        {
            segs = Mathf.Max(segs * 2, 24);
            int start = kit.VertexCount;
            float rMid = Mathf.Max(rBottom, rTop) * 1.08f;
            float rLow = Mathf.Lerp(rBottom, rMid, 0.55f);
            float rHigh = Mathf.Lerp(rMid, rTop, 0.55f);
            float inTop = Mathf.Max(0.02f, rTop - wallThick);
            float inMid = Mathf.Max(0.02f, rMid - wallThick);
            float inBot = Mathf.Max(0.02f, rBottom - wallThick);
            float[] profR = { 0.001f, rBottom * 0.6f, rBottom, rLow, rMid, rHigh, rTop, (rTop + inTop) * 0.5f, inTop, inMid, inBot, 0.001f };
            float[] profY = { 0f, 0.005f, 0.03f, height * 0.25f, height * 0.52f, height * 0.78f, height * 0.97f, height, height * 0.96f, height * 0.50f, wallThick, wallThick };
            int rings = profR.Length - 1;

            for (int y = 0; y <= rings; y++)
            {
                for (int x = 0; x <= segs; x++)
                {
                    float th = Mathf.PI * 2f * (x % segs) / segs;
                    Vector3 local = new Vector3(Mathf.Cos(th) * profR[y], profY[y], Mathf.Sin(th) * profR[y]);
                    kit.Vert(bottomCenter + rot * local, new Vector2(x / (float)segs, y / (float)rings));
                }
            }
            for (int y = 0; y < rings; y++)
            {
                int sub = y >= 7 ? subInner : subOuter;
                for (int x = 0; x < segs; x++)
                {
                    int a = start + y * (segs + 1) + x, b = a + segs + 1;
                    kit.Quad(sub, a, b, b + 1, a + 1);
                }
            }
        }

        public static Mesh SaveMeshAsset(Mesh m, string dir, string name)
        {
            m.name = name;
            string path = $"{dir}/{name}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) { old.Clear(); EditorUtility.CopySerialized(m, old); EditorUtility.SetDirty(old); return old; }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        public static GameObject CreateMeshChild(string name, Transform parent, Vector3 pos, Quaternion rot, NatureMeshKit kit, string dir, Material[] allMats, params int[] matIndices)
        {
            var usedMats = new Material[matIndices.Length];
            for (int i = 0; i < matIndices.Length; i++) usedMats[i] = allMats[matIndices[i]];
            var mesh = SaveMeshAsset(kit.ToMesh(name), dir, name + "_Mesh");
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(pos, rot);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = usedMats;
            return go;
        }
    }
}
