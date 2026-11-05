using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 砂漠の株（アロエ・リュウゼツランのような形。Oct 27 下書き。西の砂漠と赤い台地のすそ）。メニュー Slug/Nature/Build Desert Rosette で、
    /// 3 案 × なめらか・カクカクのメッシュ・マテリアル・プレハブを作り直す（Assets/Art/Nature/DesertRosette）。
    ///
    /// 特徴を濃く：根元から放射状に、肉厚で硬い葉がらせんに重なって広がる（外ほど寝て長く、内ほど立って短い。真ん中は葉が閉じた芯）。
    /// 葉の上は浅い溝、下は丸い背。外側の葉のふちに大きめのトゲを少しだけ、どの葉も先に黒いトゲ。本物より葉を太く厚く（ずんぐり）。
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A リュウゼツラン：葉 13 枚・長さ 0.6 m、株の幅 約 1.1 m・高さ 約 0.5 m（人なら幅 4 m・目の高さ）。青みの灰緑、外に枯れ葉 3 枚
    /// - B アロエ：葉 11 枚・長さ 0.5 m、Aより細めで先が外へ反る。短い茎の上。花茎 1 本 0.9 m の先に橙のろうそく形の花
    /// - C 子株の群れ：親 0.38 m の葉 ＋ まわりに子株 4 つ（葉 0.15〜0.26 m）。全体で幅 約 1 m
    /// 色：リュウゼツランの葉 #8EA59A、アロエの葉 #86A055、トゲ #5B4634、枯れ葉 #B89D70、花茎 #8E8A5A、花 #E0783A。
    /// 当たり判定は株の真ん中の低いカプセル（葉先はすり抜ける）。
    /// </summary>
    public static class DesertRosetteBuilder
    {
        const string Dir = "Assets/Art/Nature/DesertRosette";
        const int LEAF = 0, SPINE = 1, DRY = 2, STALK = 3, FLOWER = 4;

        struct Rosette
        {
            public Vector3 at;
            public int leaves, seed;
            public float len, width, thick, elevOuter, elevInner, bend, innerScale, teeth;
        }

        struct Variant
        {
            public string name;
            public bool aloe;
            public int dryLeaves, stalks;
            public float stem, colliderR, colliderH;
            public Rosette[] rosettes;
        }

        static readonly Variant[] Variants =
        {
            new Variant
            {
                name = "A", dryLeaves = 3, colliderR = 0.32f, colliderH = 0.4f,
                rosettes = new[] { new Rosette { leaves = 13, seed = 3, len = 0.6f, width = 0.14f, thick = 0.08f, elevOuter = 14f, elevInner = 80f, bend = 6f, innerScale = 0.55f, teeth = 2 } },
            },
            new Variant
            {
                name = "B", aloe = true, stalks = 1, stem = 0.08f, colliderR = 0.22f, colliderH = 0.45f,
                rosettes = new[] { new Rosette { leaves = 11, seed = 13, len = 0.5f, width = 0.095f, thick = 0.065f, elevOuter = 38f, elevInner = 78f, bend = -40f, innerScale = 0.6f, teeth = 3 } },
            },
            new Variant
            {
                name = "C", colliderR = 0.22f, colliderH = 0.3f,
                rosettes = new[]
                {
                    new Rosette { leaves = 11, seed = 31, len = 0.38f, width = 0.1f, thick = 0.06f, elevOuter = 20f, elevInner = 80f, bend = 6f, innerScale = 0.55f, teeth = 2 },
                    new Rosette { at = new Vector3(0.4f, 0f, 0.12f), leaves = 8, seed = 41, len = 0.26f, width = 0.07f, thick = 0.045f, elevOuter = 26f, elevInner = 80f, bend = 6f, innerScale = 0.6f, teeth = 1 },
                    new Rosette { at = new Vector3(-0.18f, 0f, 0.34f), leaves = 7, seed = 43, len = 0.2f, width = 0.055f, thick = 0.036f, elevOuter = 30f, elevInner = 80f, bend = 6f, innerScale = 0.6f, teeth = 1 },
                    new Rosette { at = new Vector3(-0.32f, 0f, -0.2f), leaves = 8, seed = 47, len = 0.23f, width = 0.062f, thick = 0.04f, elevOuter = 26f, elevInner = 80f, bend = 6f, innerScale = 0.6f, teeth = 1 },
                    new Rosette { at = new Vector3(0.1f, 0f, -0.34f), leaves = 6, seed = 53, len = 0.15f, width = 0.045f, thick = 0.03f, elevOuter = 34f, elevInner = 82f, bend = 6f, innerScale = 0.6f, teeth = 0 },
                },
            },
        };

        [MenuItem("Slug/Nature/Build Desert Rosette")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var log = new System.Text.StringBuilder("Desert rosette built:");
            for (int f = 0; f < 2; f++)
            {
                bool facet = f == 1;
                var mats = Materials(facet);
                foreach (var v in Variants) log.Append(' ').Append(Make(v, facet, mats));
            }
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Make(Variant v, bool facet, Material[] mats)
        {
            string name = $"DesertRosette_{v.name}{(facet ? "_Facet" : "")}";
            int around = facet ? 6 : 12, segs = facet ? 5 : 12;
            var kit = new NatureMeshKit(5);

            if (v.stem > 0f)
            {
                // the aloe's short trunk, clothed in old leaf bases
                kit.BeginTube();
                kit.Tube(t => Vector3.up * (v.stem * t), t => 0.07f * (1f + 0.3f * (1f - t)), new[] { new Vector3(0f, 1f, DRY), new Vector3(1f, 1f, DRY) }, around, DRY, -1);
            }

            foreach (var r in v.rosettes)
            {
                Vector3 crown = r.at + Vector3.up * v.stem;
                // small pups get fewer rings (they're only seen small)
                int ra = r.len < 0.3f ? Mathf.Max(5, around * 2 / 3) : around, rs = r.len < 0.3f ? Mathf.Max(4, segs * 2 / 3) : segs;
                float y0 = NatureAssets.Hash(r.seed, 0) * Mathf.PI * 2f;
                for (int i = 0; i < r.leaves; i++)
                {
                    float k = i / (float)(r.leaves - 1);   // 0 the outermost, 1 the closed heart
                    float yaw = y0 + i * 137.5f * Mathf.Deg2Rad + (NatureAssets.Hash(r.seed, i + 1) - 0.5f) * 0.2f;
                    float elev = Mathf.Lerp(r.elevOuter, r.elevInner, Mathf.Pow(k, 0.8f)) + (NatureAssets.Hash(r.seed, i + 20) - 0.5f) * 8f;
                    float len = r.len * Mathf.Lerp(1f, r.innerScale, k) * Mathf.Lerp(0.9f, 1.08f, NatureAssets.Hash(r.seed, i + 40));
                    float w = r.width * Mathf.Lerp(1f, 0.75f, k), th = r.thick * Mathf.Lerp(1f, 0.8f, k);
                    var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                    // inner leaves start higher up the heart and a little in from the edge
                    Vector3 root = crown + Vector3.up * (r.len * 0.12f * k) + h * (r.width * 0.6f * (1f - k));
                    Leaf(kit, root, h, len, w, th, elev, r.bend * (1f - 0.6f * k), k < 0.6f ? (int)r.teeth : 0, ra, rs, LEAF, r.seed * 100 + i);
                }
            }

            // A: old leaves lying flat on the sand around the foot, dry and drooped
            var main = v.rosettes[0];
            for (int i = 0; i < v.dryLeaves; i++)
            {
                float yaw = (i * 2.2f + 0.5f) + NatureAssets.Hash(main.seed, i + 200);
                var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                Leaf(kit, main.at + Vector3.up * 0.03f, h, main.len * 1.05f, main.width * 0.85f, main.thick * 0.35f, 2f, -10f, 0, around, segs, DRY, -1);
            }

            // B: the flower stalk, leaning a little, with an orange candle of flowers on top
            for (int i = 0; i < v.stalks; i++)
            {
                Vector3 c0 = Vector3.up * (v.stem + main.len * 0.15f);
                var lean = new Vector3(0.12f, 0f, 0.05f);
                float H = 0.9f;
                Vector3 Stalk(float t) => c0 + Vector3.up * (H * t) + lean * (H * t * t);
                int from = kit.VertexCount;
                kit.BeginTube();
                kit.Tube(Stalk, t => Mathf.Lerp(0.02f, 0.012f, t), new[] { new Vector3(0f, 1f, STALK), new Vector3(0.5f, 1f, STALK), new Vector3(1f, 1f, STALK) }, around / 2 + 2, -1, STALK);
                kit.Paint(from, (p, uv) => new Color(uv.y, 0f, 0.3f, 1f));
                from = kit.VertexCount;
                kit.Ball(Stalk(1f) + Vector3.up * 0.1f, new Vector3(0.065f, 0.16f, 0.065f), facet ? 4 : 8, FLOWER);
                kit.Paint(from, (p, uv) => new Color(1f, 0.3f, 0.3f, 1f));
            }

            var mesh = NatureAssets.SaveMesh(kit.ToMesh(name), $"{Dir}/{name}_Mesh.asset");
            var leafMat = v.aloe ? mats[5] : mats[0];
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, new[] { leafMat, mats[1], mats[2], mats[3], mats[4] }, go =>
            {
                var c = go.AddComponent<CapsuleCollider>();
                c.center = Vector3.up * (v.colliderH * 0.5f); c.height = Mathf.Max(v.colliderH, v.colliderR * 2f); c.radius = v.colliderR;
            });
            return $"{name} {kit.TriangleCount} tris";
        }

        /// <summary>
        /// 肉厚の葉 1 枚：root から水平の向き h へ、仰角 elev（度）で出て、先へ行くほど bend（度）だけ曲がる（＋で起き、－で外へ反る）。
        /// 断面は上が浅い溝・下が丸い背の楕円、幅 w・厚み th は先へ細って点になる。ふちに teeth 本ずつのトゲ、先に 1 本のトゲ（toothSeed &lt; 0 でトゲなし）。
        /// </summary>
        static void Leaf(NatureMeshKit kit, Vector3 root, Vector3 h, float len, float w, float th, float elev, float bend, int teeth,
                         int around, int segs, int sub, int toothSeed)
        {
            float e = elev * Mathf.Deg2Rad, b = bend * Mathf.Deg2Rad;
            Vector3 Path(float s)
            {
                // the axis bends at a constant rate: integrate (cos, sin) of the angle e + b·s
                float x, y;
                if (Mathf.Abs(b) < 1e-4f) { x = Mathf.Cos(e) * s; y = Mathf.Sin(e) * s; }
                else { x = (Mathf.Sin(e + b * s) - Mathf.Sin(e)) / b; y = (Mathf.Cos(e) - Mathf.Cos(e + b * s)) / b; }
                return root + (h * x + Vector3.up * y) * len;
            }
            // widest a third of the way out, then a long taper to the point; thickest at the base
            float Width(float s) => w * (s < 0.3f ? Mathf.Lerp(0.7f, 1f, s / 0.3f) : Mathf.Pow(Mathf.Max(0f, 1f - (s - 0.3f) / 0.7f), 0.85f));
            float Thick(float s) => th * Mathf.Pow(Mathf.Max(0f, 1f - s), 0.6f);
            Vector3 side = Vector3.Cross(Vector3.up, h).normalized;
            Vector3 Normal(float s) => Vector3.Cross((Path(Mathf.Min(1f, s + 0.01f)) - Path(Mathf.Max(0f, s - 0.01f))).normalized, side).normalized;
            Vector3 Ring(float s, float a)
            {
                float u = Mathf.Cos(a), q = Mathf.Sin(a);
                // the top dips into a shallow channel along the middle; the underside is a full round back
                float y = q > 0f ? Thick(s) * 0.55f * q * (0.35f + 0.65f * u * u) : Thick(s) * q;
                return Path(s) + side * (Width(s) * u) + Normal(s) * y;
            }

            int from = kit.VertexCount;
            int prev = -1, first = -1;
            for (int i = 0; i < segs; i++)
            {
                float s = i / (float)segs;
                int start = kit.VertexCount;
                for (int j = 0; j < around; j++) kit.Vert(Ring(s, Mathf.PI * 2f * j / around), new Vector2(j / (float)around, s));
                if (prev >= 0)
                    for (int j = 0; j < around; j++)
                    {
                        int jn = (j + 1) % around;
                        kit.Quad(sub, prev + jn, start + jn, start + j, prev + j);
                    }
                else first = start;
                prev = start;
            }
            int tip = kit.Vert(Path(1f), new Vector2(0.5f, 1f));
            int foot = kit.Vert(Path(0f), new Vector2(0.5f, 0f));
            for (int j = 0; j < around; j++)
            {
                int jn = (j + 1) % around;
                kit.Tri(sub, tip, prev + jn, prev + j);
                kit.Tri(sub, foot, first + j, first + jn);
            }
            kit.Paint(from, (p, uv) => new Color(uv.y * 0.3f, uv.y * 0.2f, 0f, 1f));
            if (toothSeed < 0) return;

            // the spines: a dark point at the tip, a few big hooked teeth along each edge
            Vector3 tan1 = (Path(1f) - Path(0.97f)).normalized;
            Spike(kit, Path(1f) - tan1 * 0.01f, tan1, len * 0.12f, w * 0.18f);
            for (int t = 0; t < teeth; t++)
                foreach (float sd in new[] { -1f, 1f })
                {
                    float s = Mathf.Lerp(0.2f, 0.8f, (t + 0.5f + (NatureAssets.Hash(toothSeed, t * 2 + (sd > 0 ? 1 : 0)) - 0.5f) * 0.4f) / teeth);
                    Vector3 tan = (Path(s + 0.01f) - Path(s - 0.01f)).normalized;
                    Vector3 edge = Path(s) + side * (Width(s) * sd * 0.92f);
                    Spike(kit, edge, (side * sd + tan * 0.7f).normalized, w * 0.45f, w * 0.12f);
                }
        }

        static void Spike(NatureMeshKit kit, Vector3 at, Vector3 dir, float len, float r)
        {
            kit.BeginTube();
            kit.Tube(t => at + dir * (len * t), t => r * Mathf.Max(0.05f, 1f - t), new[] { new Vector3(0f, 1f, SPINE), new Vector3(1f, 1f, SPINE) }, 4, -1, -1);
        }

        static Material[] Materials(bool facet)
        {
            string f = facet ? "_Facet" : "";
            var m = new[]
            {
                NatureAssets.Mat($"{Dir}/DesertRosette_AgaveLeaf{f}.mat", "#8EA59A", false, 0.01f),
                NatureAssets.Mat($"{Dir}/DesertRosette_Spine{f}.mat", "#5B4634", false, 0f),
                NatureAssets.Mat($"{Dir}/DesertRosette_Dry{f}.mat", "#B89D70", false, 0f),
                NatureAssets.Mat($"{Dir}/DesertRosette_Stalk{f}.mat", "#8E8A5A", false, 0.06f),
                NatureAssets.Mat($"{Dir}/DesertRosette_Flower{f}.mat", "#E0783A", false, 0.06f),
                NatureAssets.Mat($"{Dir}/DesertRosette_AloeLeaf{f}.mat", "#86A055", false, 0.01f),
            };
            foreach (var x in m) x.SetFloat("_Facet", facet ? 1f : 0f);
            return m;
        }
    }
}
