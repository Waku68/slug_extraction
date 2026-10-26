using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// クワズイモの株（Oct 26 下書き。森の塊の層の主役、ナメクジには傘）。メニュー Slug/Nature/Build Kuwazuimo で、
    /// 3 案 × なめらか・カクカクのメッシュ・マテリアル・プレハブを作り直す（Assets/Art/Nature/Kuwazuimo）。
    ///
    /// 特徴を濃く：太く短い茎の株から、太い葉柄が斜め上へ伸び、その先に大きなハート形（矢じり形）の葉が先を下に向けて垂れる。
    /// 葉の付け根は切れ込み（左右の耳たぶ 2 枚で作る）。太い中央の葉脈。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A ふつう：葉 5 枚、葉の長さ 1.0 m（人なら 3.6 m の葉）、葉柄 1.1 m、株の高さ 0.35 m
    /// - B ずんぐり大葉：葉 3 枚、葉 1.3 m、葉柄 0.6 m（低く大きく、傘らしさがいちばん強い）
    /// - C 若い株：葉 4 枚、葉 0.7 m、葉柄 0.8 m（立ち気味、小さい）
    /// 色：葉 #6F8C4A、葉脈・葉柄 #9AA866、株 #8B7D62、株の節 #6A5E4A。
    /// 当たり判定は株だけ（葉にはまだない。葉の日陰が乾きに効くようにするなら、葉にも要る）。
    /// </summary>
    public static class KuwazuimoBuilder
    {
        const string Dir = "Assets/Art/Nature/Kuwazuimo";
        const int STEM = 0, RING = 1, LEAF = 2, VEIN = 3;

        struct Variant
        {
            public string name;
            public int leaves, seed;
            public float leafLen, leafWidth, petiole, rise, stemH, stemR;
        }

        static readonly Variant[] Variants =
        {
            new Variant { name = "A", leaves = 5, seed = 11, leafLen = 1.0f, leafWidth = 0.42f, petiole = 1.1f, rise = 60f, stemH = 0.35f, stemR = 0.16f },
            new Variant { name = "B", leaves = 3, seed = 23, leafLen = 1.3f, leafWidth = 0.58f, petiole = 0.6f, rise = 45f, stemH = 0.22f, stemR = 0.2f },
            new Variant { name = "C", leaves = 4, seed = 37, leafLen = 0.7f, leafWidth = 0.3f, petiole = 0.8f, rise = 68f, stemH = 0.25f, stemR = 0.11f },
        };

        [MenuItem("Slug/Nature/Build Kuwazuimo")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var log = new System.Text.StringBuilder("Kuwazuimo built:");
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
            string name = $"Kuwazuimo_{v.name}{(facet ? "_Facet" : "")}";
            int sides = facet ? 7 : 14, segs = facet ? 7 : 18;
            var kit = new NatureMeshKit(4);

            // the stem: short, thick, ringed, flaring into the ground
            var rows = new List<Vector3> { new Vector3(-0.05f, 1.25f, STEM) };
            for (int k = 0; k < 3; k++)
            {
                float t0 = k / 3f, t1 = (k + 1) / 3f;
                rows.Add(new Vector3(t0 + 0.04f, 0.9f, k == 0 ? STEM : RING));
                rows.Add(new Vector3((t0 + t1) * 0.5f, 1.05f, STEM));
                rows.Add(new Vector3(t1, 0.9f, STEM));
            }
            kit.BeginTube();
            kit.Tube(t => Vector3.up * (v.stemH * t), t => v.stemR * (1f + 0.5f * Mathf.Exp(-Mathf.Max(0f, t) * 6f)), rows, sides, STEM, STEM);
            Vector3 crown = Vector3.up * v.stemH;

            float y0 = NatureAssets.Hash(v.seed, 0) * Mathf.PI * 2f;
            for (int i = 0; i < v.leaves; i++)
            {
                float yaw = y0 + Mathf.PI * 2f * i / v.leaves + (NatureAssets.Hash(v.seed, i + 1) - 0.5f) * 0.5f;
                float rise = Mathf.Deg2Rad * (v.rise + (NatureAssets.Hash(v.seed, i + 20) - 0.5f) * 14f);
                float len = v.petiole * Mathf.Lerp(0.85f, 1.1f, NatureAssets.Hash(v.seed, i + 40));
                var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                var d0 = h * Mathf.Cos(rise) + Vector3.up * Mathf.Sin(rise);
                // the petiole: out and up, bowing a little outward at the top under the leaf's weight
                Vector3 Petiole(float t) => crown + d0 * (len * t) + h * (0.12f * len * t * t) - Vector3.up * (0.08f * len * t * t);
                int from = kit.VertexCount;
                kit.BeginTube();
                kit.Tube(Petiole, t => Mathf.Lerp(v.stemR * 0.42f, v.stemR * 0.24f, t), new[] { new Vector3(0f, 1f, VEIN), new Vector3(0.5f, 1f, VEIN), new Vector3(1f, 1f, VEIN) }, sides / 2 + 2, -1, VEIN);
                kit.Paint(from, (p, uv) => new Color(uv.y * 0.6f, 0f, NatureAssets.Hash(v.seed, i + 60), 1f));

                // the blade: from where the petiole meets it, pointing out and down, the tip drooping further
                Vector3 at = Petiole(1f);
                float tilt = Mathf.Deg2Rad * (-10f - 12f * NatureAssets.Hash(v.seed, i + 80));
                var b0 = h * Mathf.Cos(tilt) + Vector3.up * Mathf.Sin(tilt);
                float L = v.leafLen * Mathf.Lerp(0.9f, 1.08f, NatureAssets.Hash(v.seed, i + 100));
                Vector3 Blade(float s) => at + b0 * (L * s) - Vector3.up * (0.16f * L * s * s);
                // a heart: widest a fifth of the way out, then a long straight taper to a point
                float Width(float s) => v.leafWidth * (s < 0.2f ? Mathf.Lerp(0.75f, 1f, s / 0.2f) : Mathf.Pow(Mathf.Max(0f, 1f - (s - 0.2f) / 0.8f), 0.9f));
                from = kit.VertexCount;
                kit.Blade(Blade, Width, segs, 0.025f, 0.12f, LEAF);
                // the two ear lobes behind the petiole (the heart's notch between them)
                foreach (float side in new[] { -1f, 1f })
                {
                    var across = Vector3.Cross(Vector3.up, b0).normalized * side;
                    var e0 = (-b0 * 1.1f + across).normalized;
                    float el = L * 0.42f;
                    Vector3 Ear(float s) => at + b0 * (0.04f * L) + e0 * (el * s) - Vector3.up * (0.12f * el * s * s);
                    kit.Blade(Ear, s => v.leafWidth * 0.62f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, 0.2f + s * 0.8f))), 0.5f), segs / 2 + 2, 0.022f, 0.08f, LEAF);
                }
                // the thick midrib along the top of the blade
                kit.BeginTube();
                kit.Tube(s => NatureAssets.BladePoint(Blade, Width, 0.025f, 0.12f, s, 0f, 0.012f), s => Mathf.Lerp(0.022f, 0.006f, s) * v.leafLen,
                         new[] { new Vector3(0f, 1f, VEIN), new Vector3(0.5f, 1f, VEIN), new Vector3(0.95f, 1f, VEIN) }, 5, -1, -1);
                kit.Paint(from, (p, uv) => new Color(0.6f + 0.4f * uv.y, uv.y, NatureAssets.Hash(v.seed, i + 60), 1f));
            }

            var mesh = NatureAssets.SaveMesh(kit.ToMesh(name), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, mats, go =>
            {
                var c = go.AddComponent<CapsuleCollider>();
                c.center = Vector3.up * (v.stemH * 0.5f); c.height = v.stemH + v.stemR; c.radius = v.stemR;
            });
            return $"{name} {kit.TriangleCount} tris";
        }

        static Material[] Materials(bool facet)
        {
            string f = facet ? "_Facet" : "";
            var m = new[]
            {
                NatureAssets.Mat($"{Dir}/Kuwazuimo_Stem{f}.mat", "#8B7D62", false, 0f),
                NatureAssets.Mat($"{Dir}/Kuwazuimo_StemRing{f}.mat", "#6A5E4A", false, 0f),
                NatureAssets.Mat($"{Dir}/Kuwazuimo_Leaf{f}.mat", "#6F8C4A", true, 0.08f),
                NatureAssets.Mat($"{Dir}/Kuwazuimo_Vein{f}.mat", "#9AA866", false, 0.08f),
            };
            foreach (var x in m) x.SetFloat("_Facet", facet ? 1f : 0f);
            return m;
        }
    }
}
