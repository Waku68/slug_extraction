using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 石積みの道しるべ（ケルン）（Oct 25、docs/modeling/cairn.md）。
    /// メニュー Slug/Props/Build Cairn で、Cairn_A / Cairn_B / Cairn_C のメッシュ・マテリアル・プレハブを作り直す。
    ///
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - Cairn_A（立っている）：高さ 0.80 m（人なら約 2.9 m）、石 7 個（下 6 個＋上の細長い指し石 1 個）、ヤシ縄あり、苔 3 か所。
    /// - Cairn_B（低い）      ：高さ 0.55 m（人なら約 2.0 m）、石 5 個（下 4 個＋上の細長い指し石 1 個）、ヤシ縄あり、苔 2 か所。
    /// - Cairn_C（崩れた）    ：残った石 3 個（高さ 0.35 m）＋横に転がった石 3 個（0.5〜0.8 m）＋地面に落ちた切れ縄、苔 2 か所。
    /// - いちばん下の石は地面に 5 cm（-0.05 m）埋める。いちばん上の細長い石（0.15 × 0.10 × 長さ 0.45 m）は +Z（道の進む向き）を指す。
    /// - ヤシ縄：太さ 3 cm（半径 0.015 m）、上から 2 番目と 3 番目の石の間に 1 周＋端が 10 cm 垂れる。
    /// - 苔：下の 2〜3 個の石の上面（+X 側）に厚み 3 cm の薄いかたまり。
    /// - 色（本番）：Stone #A39C8E、StoneDark #7E786C、Moss #73804C、Rope #B8A272（GrayRough=true の間は灰色ラフ）。
    /// </summary>
    public static class CairnBuilder
    {
        const string Dir = "Assets/Art/Models/Props/Cairn";
        const int STONE = 0, DARK = 1, MOSS = 2, ROPE = 3;
        const bool GrayRough = true; // ステップ2：灰色ラフ（ステップ3で false にする）

        // 形の数字
        const float PointerW = 0.15f, PointerH = 0.10f, PointerL = 0.45f, PointerShiftZ = 0.11f;
        const float RopeR = 0.015f, RopeTailLen = 0.10f;
        const float MossThick = 0.03f;
        const int StoneRings = 5, StoneSegs = 8;

        struct StoneSpec
        {
            public Vector3 pos, size, euler;
            public int mat;
            public float taperZ;
            public bool moss;
        }

        [MenuItem("Slug/Props/Build Cairn")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var mats = Materials();
            string a = BuildVariant("Cairn_A", SpecA(), new Vector3(0f, 0.625f, 0.01f), new Vector2(0.155f, 0.132f), true, mats);
            string b = BuildVariant("Cairn_B", SpecB(), new Vector3(0f, 0.365f, 0.01f), new Vector2(0.160f, 0.135f), true, mats);
            string c = BuildVariant("Cairn_C", SpecC(), Vector3.zero, Vector2.zero, false, mats);
            AssetDatabase.SaveAssets();
            Debug.Log($"Cairn built: {a} / {b} / {c}");
        }

        static List<StoneSpec> SpecA() => new List<StoneSpec>
        {
            new StoneSpec { pos = new Vector3( 0.00f, 0.055f,  0.00f), size = new Vector3(0.66f, 0.21f, 0.54f), euler = new Vector3( 3f,  10f, -4f), mat = STONE, moss = true },
            new StoneSpec { pos = new Vector3(-0.02f, 0.200f,  0.02f), size = new Vector3(0.57f, 0.18f, 0.46f), euler = new Vector3(-4f, -25f,  4f), mat = DARK,  moss = true },
            new StoneSpec { pos = new Vector3( 0.03f, 0.330f, -0.02f), size = new Vector3(0.49f, 0.16f, 0.40f), euler = new Vector3( 4f,  35f, -3f), mat = STONE, moss = true },
            new StoneSpec { pos = new Vector3(-0.02f, 0.455f,  0.01f), size = new Vector3(0.42f, 0.15f, 0.35f), euler = new Vector3(-4f, -15f, -5f), mat = STONE },
            new StoneSpec { pos = new Vector3( 0.02f, 0.570f,  0.00f), size = new Vector3(0.36f, 0.14f, 0.30f), euler = new Vector3( 4f,  20f,  4f), mat = DARK },
            new StoneSpec { pos = new Vector3(-0.01f, 0.675f,  0.01f), size = new Vector3(0.31f, 0.13f, 0.26f), euler = new Vector3(-3f, -12f,  3f), mat = STONE },
            new StoneSpec { pos = new Vector3( 0.00f, 0.750f, PointerShiftZ), size = new Vector3(PointerW, PointerH, PointerL), euler = new Vector3(-3f, 0f, 2f), mat = STONE, taperZ = 0.28f },
        };

        static List<StoneSpec> SpecB() => new List<StoneSpec>
        {
            new StoneSpec { pos = new Vector3( 0.00f, 0.050f,  0.00f), size = new Vector3(0.62f, 0.20f, 0.50f), euler = new Vector3(-3f,  15f,  4f), mat = STONE, moss = true },
            new StoneSpec { pos = new Vector3( 0.02f, 0.190f, -0.02f), size = new Vector3(0.50f, 0.16f, 0.41f), euler = new Vector3( 4f, -30f, -4f), mat = DARK,  moss = true },
            new StoneSpec { pos = new Vector3(-0.02f, 0.310f,  0.02f), size = new Vector3(0.40f, 0.14f, 0.33f), euler = new Vector3(-4f,  22f,  5f), mat = STONE },
            new StoneSpec { pos = new Vector3( 0.01f, 0.420f,  0.00f), size = new Vector3(0.31f, 0.13f, 0.26f), euler = new Vector3( 3f, -10f, -3f), mat = DARK },
            new StoneSpec { pos = new Vector3( 0.00f, 0.500f, PointerShiftZ), size = new Vector3(PointerW, PointerH, PointerL), euler = new Vector3(-3f, 0f, -2f), mat = STONE, taperZ = 0.28f },
        };

        static List<StoneSpec> SpecC() => new List<StoneSpec>
        {
            new StoneSpec { pos = new Vector3( 0.00f, 0.050f,  0.00f), size = new Vector3(0.62f, 0.20f, 0.50f), euler = new Vector3( 4f, -12f,  4f), mat = STONE, moss = true },
            new StoneSpec { pos = new Vector3(-0.03f, 0.185f,  0.02f), size = new Vector3(0.50f, 0.15f, 0.41f), euler = new Vector3(-5f,  28f, -6f), mat = DARK,  moss = true },
            new StoneSpec { pos = new Vector3( 0.03f, 0.290f, -0.02f), size = new Vector3(0.40f, 0.12f, 0.33f), euler = new Vector3( 6f, -18f,  7f), mat = STONE },
            // 横 0.5〜0.8 m に転がった石 3 個
            new StoneSpec { pos = new Vector3( 0.42f, 0.045f, -0.36f), size = new Vector3(0.35f, 0.13f, 0.28f), euler = new Vector3( 10f,  32f, -8f), mat = STONE },
            new StoneSpec { pos = new Vector3( 0.50f, 0.040f,  0.38f), size = new Vector3(0.30f, 0.12f, 0.25f), euler = new Vector3(-8f, -42f,  12f), mat = DARK },
            new StoneSpec { pos = new Vector3( 0.16f, 0.038f,  0.65f), size = new Vector3(PointerW, PointerH, PointerL), euler = new Vector3( 5f, 28f, -6f), mat = STONE, taperZ = 0.28f },
        };

        static string BuildVariant(string name, List<StoneSpec> specs, Vector3 ropeCenter, Vector2 ropeRad, bool standingRope, Material[] mats)
        {
            var kit = new NatureMeshKit(4);
            var rnd = new System.Random(name.GetHashCode());

            foreach (var s in specs)
            {
                Quaternion rot = Quaternion.Euler(s.euler);
                AddStone(kit, s.mat, s.pos, s.size, rot, rnd, s.taperZ);
                if (s.moss) AddMoss(kit, MOSS, s.pos, s.size, rot);
            }

            if (standingRope) AddRopeRing(kit, ROPE, ropeCenter, ropeRad);
            else AddLooseRope(kit, ROPE);

            var mesh = kit.ToMesh(name);
            string meshPath = $"{Dir}/{name}_Mesh.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (old != null) { old.Clear(); EditorUtility.CopySerialized(mesh, old); mesh = old; }
            else AssetDatabase.CreateAsset(mesh, meshPath);

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            if (standingRope)
            {
                float h = name == "Cairn_A" ? 0.80f : 0.55f;
                var col = go.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, h * 0.5f, 0.02f);
                col.height = h + 0.05f;
                col.radius = 0.30f;
            }
            else
            {
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.175f, 0f);
                col.size = new Vector3(0.60f, 0.35f, 0.48f);
            }
            PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return $"{name} {kit.TriangleCount} tris";
        }

        /// <summary>角を丸めた平たい楕円の塊（ころん・少しでこぼこ）。taperZ > 0 なら +Z の先を細めて指し石にする。</summary>
        static void AddStone(NatureMeshKit kit, int sub, Vector3 pos, Vector3 size, Quaternion rot, System.Random rnd, float taperZ)
        {
            Vector3 ext = size * 0.5f;
            float p1 = (float)rnd.NextDouble() * 6.28f, p2 = (float)rnd.NextDouble() * 6.28f;
            int start = -1;
            for (int y = 0; y <= StoneRings; y++)
            {
                float v = y / (float)StoneRings, phi = Mathf.PI * v;
                float cy = -Mathf.Cos(phi), sr = Mathf.Max(0f, Mathf.Sin(phi));
                float py = Mathf.Sign(cy) * Mathf.Pow(Mathf.Abs(cy), 0.72f) * ext.y;
                float pr = Mathf.Pow(sr, 0.82f);
                for (int x = 0; x <= StoneSegs; x++)
                {
                    int xi = x % StoneSegs;
                    float th = Mathf.PI * 2f * xi / StoneSegs;
                    float wob = (y == 0 || y == StoneRings) ? 1f : 1f + 0.06f * Mathf.Sin(th * 2f + p1) + 0.04f * Mathf.Cos(th * 3f + p2 + v * 2.5f);
                    float tip = Mathf.Sin(th); // +1 が +Z 側
                    float tx = 1f - taperZ * tip;
                    float ty = 1f - taperZ * 0.35f * Mathf.Max(0f, tip);
                    var local = new Vector3(Mathf.Cos(th) * pr * ext.x * wob * tx, py * ty, tip * pr * ext.z * wob);
                    int idx = kit.Vert(pos + rot * local, new Vector2(x / (float)StoneSegs, v));
                    if (start < 0) start = idx;
                }
            }
            for (int y = 0; y < StoneRings; y++)
                for (int x = 0; x < StoneSegs; x++)
                {
                    int a = start + y * (StoneSegs + 1) + x, b = a + StoneSegs + 1;
                    kit.Quad(sub, a, b, b + 1, a + 1);
                }
        }

        /// <summary>石の上面の +X 側に寄った薄い苔のかたまり（厚み 3 cm）。</summary>
        static void AddMoss(NatureMeshKit kit, int sub, Vector3 stonePos, Vector3 stoneSize, Quaternion stoneRot)
        {
            int rings = 3, segs = 7, start = -1;
            Vector3 mSize = new Vector3(stoneSize.x * 0.46f, MossThick, stoneSize.z * 0.56f) * 0.5f;
            Vector3 offset = new Vector3(stoneSize.x * 0.22f, stoneSize.y * 0.44f, 0.02f);
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings, phi = Mathf.PI * v;
                float cy = -Mathf.Cos(phi), sr = Mathf.Max(0f, Mathf.Sin(phi));
                for (int x = 0; x <= segs; x++)
                {
                    float th = Mathf.PI * 2f * (x % segs) / segs;
                    float lx = Mathf.Cos(th) * sr * mSize.x;
                    float lz = Mathf.Sin(th) * sr * mSize.z;
                    float drape = -0.22f * Mathf.Max(0f, lx) - 0.10f * (lx * lx + lz * lz);
                    var p = offset + new Vector3(lx, cy * mSize.y + drape, lz);
                    int idx = kit.Vert(stonePos + stoneRot * p, new Vector2(x / (float)segs, v));
                    if (start < 0) start = idx;
                }
            }
            for (int y = 0; y < rings; y++)
                for (int x = 0; x < segs; x++)
                {
                    int a = start + y * (segs + 1) + x, b = a + segs + 1;
                    kit.Quad(sub, a, b, b + 1, a + 1);
                }
        }

        /// <summary>上から 2 番目と 3 番目の石の間に巻いたヤシ縄（閉じた輪＋10 cm 垂れる端）。</summary>
        static void AddRopeRing(NatureMeshKit kit, int sub, Vector3 center, Vector2 rad)
        {
            var rows = new List<Vector3>();
            for (int i = 0; i <= 10; i++) rows.Add(new Vector3(i / 10f, 1f, sub));
            kit.BeginTube();
            kit.Tube(t =>
            {
                float a = Mathf.PI * 2f * t;
                return center + new Vector3(Mathf.Cos(a) * rad.x, 0.005f * Mathf.Sin(a * 2f), Mathf.Sin(a) * rad.y);
            }, _ => RopeR, rows, 5, -1, -1);

            // 垂れる端（10 cm、+X 手前側に見える位置）
            float a0 = 0.35f;
            Vector3 knot = center + new Vector3(Mathf.Cos(a0) * rad.x, 0f, Mathf.Sin(a0) * rad.y);
            Vector3 outDir = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)).normalized;
            var tailRows = new List<Vector3> { new Vector3(0f, 1.15f, sub), new Vector3(0.3f, 1f, sub), new Vector3(0.7f, 0.95f, sub), new Vector3(1f, 0.8f, sub) };
            kit.BeginTube();
            kit.Tube(t => knot + outDir * (0.025f * Mathf.Sin(Mathf.PI * 0.6f * t)) + Vector3.down * (RopeTailLen * t), _ => RopeR, tailRows, 5, sub, sub);
        }

        /// <summary>Cairn_C の切れて地面に落ちたゆるい縄。</summary>
        static void AddLooseRope(NatureMeshKit kit, int sub)
        {
            var rows = new List<Vector3>();
            for (int i = 0; i <= 7; i++) rows.Add(new Vector3(i / 7f, 1f, sub));
            kit.BeginTube();
            kit.Tube(t =>
            {
                float x = 0.38f + 0.08f * Mathf.Sin(t * Mathf.PI * 1.5f);
                float z = Mathf.Lerp(-0.10f, 0.32f, t);
                return new Vector3(x, RopeR, z);
            }, _ => RopeR, rows, 5, sub, sub);
        }

        static Material[] Materials()
        {
            var cols = GrayRough
                ? new[] { "#9E9E9E", "#808080", "#6E6E6E", "#B8B8B8" }
                : new[] { "#A39C8E", "#7E786C", "#73804C", "#B8A272" };
            var names = new[] { "Cairn_Stone", "Cairn_StoneDark", "Cairn_Moss", "Cairn_Rope" };
            var res = new Material[4];
            for (int i = 0; i < 4; i++)
            {
                ColorUtility.TryParseHtmlString(cols[i], out var c);
                var m = WorldLook.GetOrCreate($"{Dir}/{names[i]}.mat");
                WorldLook.Apply(m, c, 0.15f, 0f);
                res[i] = m;
            }
            return res;
        }
    }
}
