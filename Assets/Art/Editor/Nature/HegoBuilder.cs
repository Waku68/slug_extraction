using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ヘゴ（木の形のシダ。Oct 26 下書き。溶岩原と森、本物の溶岩原でも最初に生える植物）。メニュー Slug/Nature/Build Hego で、
    /// 3 案 × なめらか・カクカクのメッシュ・マテリアル・プレハブを作り直す（Assets/Art/Nature/Hego）。
    ///
    /// 特徴を濃く：根元が気根のかたまりで太く広がる幹に、葉の落ちた跡の輪が重なり、てっぺんから細かく切れ込んだ大きな葉が
    /// 傘のように弓なりに広がる。真ん中に、巻いたままの若い芽（ぜんまい）が 2〜3 本立つ（いちばんの見分けどころ）。
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A ふつう：幹 1.5 m（人なら 5.4 m）、葉 11 枚・長さ 2.0 m
    /// - B 低い：幹 0.6 m、葉 10 枚・長さ 2.2 m（地面近くに大きく広がる。ナメクジの頭の上に屋根）
    /// - C 背高：幹 2.5 m、葉 12 枚・長さ 1.8 m
    /// 色：幹 #7E6A54、葉の跡のこぶ #4E4236、葉 #7E9A50（ヤシより明るく黄み）、ぜんまい #9A9258。
    /// 当たり判定は幹だけ。
    /// </summary>
    public static class HegoBuilder
    {
        const string Dir = "Assets/Art/Nature/Hego";
        const int TRUNK = 0, SCAR = 1, FROND = 2, CURL = 3;

        struct Variant
        {
            public string name;
            public int fronds, curls, seed;
            public float trunkH, rBase, rTop, frondLen, frondWidth;
        }

        static readonly Variant[] Variants =
        {
            new Variant { name = "A", fronds = 11, curls = 3, seed = 5, trunkH = 1.5f, rBase = 0.2f, rTop = 0.15f, frondLen = 2.0f, frondWidth = 0.4f },
            new Variant { name = "B", fronds = 10, curls = 2, seed = 17, trunkH = 0.6f, rBase = 0.22f, rTop = 0.17f, frondLen = 2.2f, frondWidth = 0.46f },
            new Variant { name = "C", fronds = 12, curls = 3, seed = 29, trunkH = 2.5f, rBase = 0.18f, rTop = 0.13f, frondLen = 1.8f, frondWidth = 0.36f },
        };

        [MenuItem("Slug/Nature/Build Hego")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var log = new System.Text.StringBuilder("Hego built:");
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
            string name = $"Hego_{v.name}{(facet ? "_Facet" : "")}";
            int sides = facet ? 7 : 14, segs = facet ? 10 : 30;
            var kit = new NatureMeshKit(4);

            // the trunk: a skirt of aerial roots at the foot, then stacked rings where old fronds fell
            // (uneven knobs, not the palm's even rings: the scars are dark and lumpy)
            int rings = Mathf.Max(3, Mathf.RoundToInt(v.trunkH / 0.16f));
            var rows = new List<Vector3> { new Vector3(-0.06f, 1.1f, TRUNK) };
            for (int k = 0; k < rings; k++)
            {
                float t0 = k / (float)rings, t1 = (k + 1) / (float)rings, len = t1 - t0;
                float knob = 1.04f + 0.16f * NatureAssets.Hash(v.seed, 200 + k);
                rows.Add(new Vector3(t0 + len * 0.3f, knob, k == 0 ? TRUNK : SCAR));
                rows.Add(new Vector3(t0 + len * 0.7f, 0.94f, SCAR));
                rows.Add(new Vector3(t1, 0.97f, TRUNK));
            }
            Vector3 Center(float t) => new Vector3(0.05f * v.trunkH * t * t, v.trunkH * t, 0f);
            float Radius(float t) => Mathf.Lerp(v.rBase, v.rTop, Mathf.Max(0f, t)) * (1f + 1.1f * Mathf.Exp(-Mathf.Max(0f, t) * v.trunkH / 0.18f));
            kit.BeginTube();
            kit.Tube(Center, Radius, rows, sides, TRUNK, TRUNK, 0.6f);
            Vector3 top = Center(1f);

            // the fronds: up and out, arching over and down like an umbrella; finely cut into many round lobes
            float y0 = NatureAssets.Hash(v.seed, 0) * Mathf.PI * 2f;
            for (int i = 0; i < v.fronds; i++)
            {
                float yaw = y0 + Mathf.PI * 2f * i / v.fronds + (NatureAssets.Hash(v.seed, i + 1) - 0.5f) * 0.3f;
                float pitch = Mathf.Deg2Rad * Mathf.Lerp(22f, 48f, NatureAssets.Hash(v.seed, i + 20));
                float len = v.frondLen * Mathf.Lerp(0.88f, 1.08f, NatureAssets.Hash(v.seed, i + 40));
                var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                var d0 = h * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
                Vector3 Path(float s) => top + Vector3.up * 0.05f + d0 * (len * s) - Vector3.up * (0.7f * len * s * s);
                float Width(float s)
                {
                    float body = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, s))), 0.7f);
                    float lobe = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(Mathf.PI * 16f * s));
                    return v.frondWidth * body * (s < 0.12f ? 0.15f + 5f * s : lobe);
                }
                int from = kit.VertexCount;
                kit.Blade(Path, Width, segs, 0.02f, 0.2f, FROND);
                kit.Paint(from, (p, uv) => new Color(uv.y, uv.y * uv.y, NatureAssets.Hash(v.seed, i + 60), 1f));
            }

            // the young fronds in the middle, still coiled (fiddleheads): a stalk up, then a shrinking spiral over forward
            for (int c = 0; c < v.curls; c++)
            {
                float yaw = y0 + 0.4f + Mathf.PI * 2f * c / v.curls;
                var fwd = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                Vector3 b = top + fwd * (v.rTop * 0.35f);
                float stalk = 0.32f + 0.1f * NatureAssets.Hash(v.seed, c + 90), r0 = 0.16f;
                Vector3 Curl(float t)
                {
                    if (t < 0.4f) return b + Vector3.up * (stalk * t / 0.4f) + fwd * (0.04f * t / 0.4f);
                    float u = (t - 0.4f) / 0.6f, th = u * Mathf.PI * 1.7f, r = r0 * (1f - 0.65f * u);
                    Vector3 tip = b + Vector3.up * stalk + fwd * 0.04f, centre = tip + fwd * r0;
                    return centre + (-fwd * Mathf.Cos(th) + Vector3.up * Mathf.Sin(th)) * r;
                }
                var crow = new List<Vector3>();
                for (int k = 0; k <= 12; k++) crow.Add(new Vector3(k / 12f, 1f, CURL));
                kit.BeginTube();
                kit.Tube(Curl, t => Mathf.Lerp(0.05f, 0.034f, t), crow, sides / 2 + 2, CURL, CURL);
                kit.Ball(Curl(1f), Vector3.one * 0.042f, facet ? 3 : 6, CURL);
            }

            var mesh = NatureAssets.SaveMesh(kit.ToMesh(name), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, mats, go =>
            {
                var col = go.AddComponent<CapsuleCollider>();
                col.center = Vector3.up * (v.trunkH * 0.5f); col.height = v.trunkH + v.rBase; col.radius = v.rBase;
            });
            return $"{name} {kit.TriangleCount} tris";
        }

        static Material[] Materials(bool facet)
        {
            string f = facet ? "_Facet" : "";
            var m = new[]
            {
                NatureAssets.Mat($"{Dir}/Hego_Trunk{f}.mat", "#7E6A54", false, 0f),
                NatureAssets.Mat($"{Dir}/Hego_Scar{f}.mat", "#4E4236", false, 0f),
                NatureAssets.Mat($"{Dir}/Hego_Frond{f}.mat", "#7E9A50", true, 0.12f),
                NatureAssets.Mat($"{Dir}/Hego_Curl{f}.mat", "#9A9258", false, 0f),
            };
            foreach (var x in m) x.SetFloat("_Facet", facet ? 1f : 0f);
            return m;
        }
    }
}
