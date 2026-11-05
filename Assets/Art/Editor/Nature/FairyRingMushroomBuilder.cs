using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 菌輪のキノコ（Oct 27 下書き。花の草原の真ん中、直径 60〜80 m の輪の上に並ぶ。採れる）。メニュー Slug/Nature/Build Fairy Ring Mushroom で、
    /// 3 案 × なめらか・カクカクのメッシュ・マテリアル・プレハブを作り直す（Assets/Art/Nature/FairyRingMushroom）。
    ///
    /// 大きさは草より少し低い 10〜20 cm（わくさん Oct 27。人なら ひざ〜もも、36〜72 cm）：草原の中からは見えず、足元に見えたら輪の上。
    /// 森の茶色いウゴタケとは色も形も分ける（どれも白〜うすいクリーム）。本物よりかさを大きく、柄を太く（ずんぐり）。
    /// 1 つの株は輪の一部（ゆるい弧に 3〜5 本）。輪に並べるときは株を弧の向きに回して続ける。
    /// - A シバフタケ風（本物の菌輪のキノコ）：5 本、高さ 0.10〜0.17 m、うすい黄土のかさの真ん中にぽっこりこぶ、細めの柄
    /// - B ホコリタケ風：柄のない白い玉 5 つ、直径 0.07〜0.13 m、下すぼまりの洋なし形、てっぺんに小さな穴
    /// - C 平たい白いかさ：3 本、高さ 0.13〜0.20 m、かさ直径 0.14〜0.18 m の平たい傘、柄につば、根元がふくらむ（いちばん小さいのはまだ閉じた丸いかさ）
    /// かさの裏（ひだ）は別のマテリアル：満月の夜にかすかに光らせるときは、ここ（とかさ）を光らせる。
    /// 当たり判定はなし（ナメクジは上を這って越えられる高さ）。採るときの判定は輪に置くときに足す。
    /// </summary>
    public static class FairyRingMushroomBuilder
    {
        const string Dir = "Assets/Art/Nature/FairyRingMushroom";
        const int CAP = 0, CENTER = 1, GILL = 2, STEM = 3;

        enum Kind { Bell, Puff, Flat }

        struct Shroom
        {
            public Vector3 at;
            public float h, capR, capH, stemR, open;   // open: 1 平らに開いたかさ、0 閉じた丸いかさ
        }

        struct Variant
        {
            public string name;
            public Kind kind;
            public int seed;
            public string cap, center, gill, stem;
            public Shroom[] shrooms;
        }

        static readonly Variant[] Variants =
        {
            new Variant
            {
                name = "A", kind = Kind.Bell, seed = 7, cap = "#CDB48A", center = "#A98559", gill = "#E6DABF", stem = "#DED2B4",
                shrooms = new[]
                {
                    new Shroom { at = new Vector3(-0.2f, 0f, 0.04f), h = 0.14f, capR = 0.055f, capH = 0.035f, stemR = 0.012f },
                    new Shroom { at = new Vector3(-0.08f, 0f, -0.03f), h = 0.17f, capR = 0.062f, capH = 0.04f, stemR = 0.014f },
                    new Shroom { at = new Vector3(0.03f, 0f, 0.05f), h = 0.11f, capR = 0.048f, capH = 0.032f, stemR = 0.011f },
                    new Shroom { at = new Vector3(0.14f, 0f, -0.01f), h = 0.15f, capR = 0.058f, capH = 0.036f, stemR = 0.013f },
                    new Shroom { at = new Vector3(0.24f, 0f, 0.06f), h = 0.1f, capR = 0.042f, capH = 0.03f, stemR = 0.01f },
                },
            },
            new Variant
            {
                name = "B", kind = Kind.Puff, seed = 19, cap = "#E6DFCC", center = "#B8A27A", gill = "#E6DFCC", stem = "#C4B38E",
                shrooms = new[]
                {
                    new Shroom { at = new Vector3(-0.17f, 0f, 0.02f), h = 0.1f, capR = 0.055f },
                    new Shroom { at = new Vector3(-0.06f, 0f, -0.05f), h = 0.13f, capR = 0.065f },
                    new Shroom { at = new Vector3(0.05f, 0f, 0.04f), h = 0.08f, capR = 0.045f },
                    new Shroom { at = new Vector3(0.14f, 0f, -0.03f), h = 0.11f, capR = 0.058f },
                    new Shroom { at = new Vector3(0.22f, 0f, 0.05f), h = 0.065f, capR = 0.035f },
                },
            },
            new Variant
            {
                name = "C", kind = Kind.Flat, seed = 29, cap = "#EAE3D2", center = "#C2A77C", gill = "#D9C4B0", stem = "#E4DCCB",
                shrooms = new[]
                {
                    new Shroom { at = new Vector3(-0.14f, 0f, 0.02f), h = 0.17f, capR = 0.08f, capH = 0.03f, stemR = 0.018f, open = 1f },
                    new Shroom { at = new Vector3(0.05f, 0f, -0.04f), h = 0.2f, capR = 0.09f, capH = 0.032f, stemR = 0.021f, open = 1f },
                    new Shroom { at = new Vector3(0.2f, 0f, 0.05f), h = 0.13f, capR = 0.05f, capH = 0.045f, stemR = 0.017f, open = 0f },
                },
            },
        };

        [MenuItem("Slug/Nature/Build Fairy Ring Mushroom")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var log = new System.Text.StringBuilder("Fairy ring mushroom built:");
            for (int f = 0; f < 2; f++)
            {
                bool facet = f == 1;
                foreach (var v in Variants) log.Append(' ').Append(Make(v, facet, Materials(v, facet)));
            }
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Make(Variant v, bool facet, Material[] mats)
        {
            string name = $"FairyRingMushroom_{v.name}{(facet ? "_Facet" : "")}";
            int around = facet ? 7 : 18;
            var kit = new NatureMeshKit(4);

            for (int i = 0; i < v.shrooms.Length; i++)
            {
                var s = v.shrooms[i];
                // each one leans a little its own way (up to about 9°), the cap leaning with the stem
                float yaw = NatureAssets.Hash(v.seed, i) * Mathf.PI * 2f, lean = NatureAssets.Hash(v.seed, i + 10) * 0.16f;
                var tilt = Quaternion.AngleAxis(lean * Mathf.Rad2Deg, new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)));
                var spin = Quaternion.AngleAxis(NatureAssets.Hash(v.seed, i + 20) * 360f, Vector3.up);
                var rot = tilt * spin;
                switch (v.kind)
                {
                    case Kind.Bell: Bell(kit, s, rot, around); break;
                    case Kind.Puff: Puff(kit, s, rot, around); break;
                    case Kind.Flat: Flat(kit, s, rot, around); break;
                }
            }

            var mesh = NatureAssets.SaveMesh(kit.ToMesh(name), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, mats, null);
            return $"{name} {kit.TriangleCount} tris";
        }

        /// <summary>A: a slender stem under a bell cap that has opened out, with a round knob (umbo) in the middle.</summary>
        static void Bell(NatureMeshKit kit, Shroom s, Quaternion rot, int around)
        {
            float rimY = s.h - s.capH;
            Stem(kit, s.at, rot, s.stemR, rimY + s.capH * 0.3f, 1.35f, around, -1f);
            var p = new List<Vector3>();
            // the underside: gills rising from the rim to the stem
            for (int k = 0; k <= 4; k++)
            {
                float u = k / 4f, r = Mathf.Lerp(s.stemR * 0.9f, s.capR * 0.97f, u);
                p.Add(new Vector3(r, rimY + s.capH * 0.32f * (1f - u), GILL));
            }
            // the rim, a little rolled under, then the dome and the knob on top
            p.Add(new Vector3(s.capR, rimY + s.capH * 0.06f, CAP));
            for (int k = 1; k <= 8; k++)
            {
                float u = 1f - k / 8f, r = s.capR * u;
                float dome = Mathf.Pow(Mathf.Max(0f, 1f - u * u), 0.6f) * 0.75f;
                float knob = 0.3f * Mathf.Exp(-(u / 0.3f) * (u / 0.3f));
                p.Add(new Vector3(Mathf.Max(0.0005f, r), rimY + s.capH * (dome + knob), u < 0.3f ? CENTER : CAP));
            }
            Lathe(kit, s.at, rot, p, around);
        }

        /// <summary>B: a stalkless white ball, pinched toward the foot (a pear), with a small dark pore on top.</summary>
        static void Puff(NatureMeshKit kit, Shroom s, Quaternion rot, int around)
        {
            var p = new List<Vector3>();
            const int n = 14;
            for (int k = 0; k <= n; k++)
            {
                float th = Mathf.PI * k / n;   // 0 the foot, π the top
                float y = s.h * 0.5f * (1f - Mathf.Cos(th));
                // narrow at the foot, widest a little above the middle, a flattened top
                float pear = Mathf.Lerp(0.45f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, th / (Mathf.PI * 0.6f))));
                float r = s.capR * Mathf.Sin(th) * pear;
                if (k == n) r = 0.0005f;
                // the pore: the top dips into a small crater
                if (k == n - 1) y -= s.h * 0.1f;
                if (k == n) y -= s.h * 0.07f;
                int sub = k <= 3 ? STEM : k >= n - 1 ? CENTER : CAP;
                p.Add(new Vector3(Mathf.Max(0.0005f, r), y - s.h * 0.04f, sub));
            }
            Lathe(kit, s.at, rot, p, around);
        }

        /// <summary>C: a thick stem with a skirt (ring) and a swollen foot, under a wide flat cap with a tan middle. open 0 = a young one, still a closed round cap.</summary>
        static void Flat(NatureMeshKit kit, Shroom s, Quaternion rot, int around)
        {
            float rimY = s.h - s.capH * Mathf.Lerp(1.6f, 1f, s.open);
            Stem(kit, s.at, rot, s.stemR, rimY + s.capH * 0.3f, 1.9f, around, s.open > 0.5f ? 0.72f : -1f);
            var p = new List<Vector3>();
            for (int k = 0; k <= 4; k++)
            {
                float u = k / 4f, r = Mathf.Lerp(s.stemR * 0.9f, s.capR * 0.95f, u);
                p.Add(new Vector3(r, rimY + s.capH * Mathf.Lerp(0.3f, 0.6f, 1f - s.open) * (1f - u), GILL));
            }
            p.Add(new Vector3(s.capR, rimY + s.capH * 0.1f, CAP));
            for (int k = 1; k <= 8; k++)
            {
                float u = 1f - k / 8f, r = s.capR * u;
                // open: a shoulder at the rim, then nearly flat with a low dome; closed: a half ball
                float flat = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(u, 4f)), 0.5f) * (1f - 0.2f * u * u);
                float ball = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) * 1.6f;
                p.Add(new Vector3(Mathf.Max(0.0005f, r), rimY + s.capH * Mathf.Lerp(ball, flat, s.open), u < 0.35f ? CENTER : CAP));
            }
            Lathe(kit, s.at, rot, p, around);
        }

        /// <summary>
        /// The stem from the ground to top (in the shroom's own up), fattening to footFlare × at the foot.
        /// skirtAt (0〜1 of the height, &lt; 0 for none) adds a drooping ring around it.
        /// </summary>
        static void Stem(NatureMeshKit kit, Vector3 at, Quaternion rot, float r, float top, float footFlare, int around, float skirtAt)
        {
            var rows = new List<Vector3>();
            for (int k = 0; k <= 6; k++) rows.Add(new Vector3(k / 6f, 1f, STEM));
            // the foot starts a little under the ground (t stays 0〜1: a negative t flips the tube's first ring)
            kit.BeginTube();
            kit.Tube(t => at + rot * (Vector3.up * (top * t - 0.008f)),
                     t => r * (1f + (footFlare - 1f) * Mathf.Exp(-t * 7f)) * (1f - 0.12f * t),
                     rows, Mathf.Max(6, around * 2 / 3), STEM, -1);
            if (skirtAt < 0f) return;
            float y = top * skirtAt, rr = r * (1f - 0.12f * skirtAt);
            var p = new List<Vector3>
            {
                new Vector3(rr * 0.95f, y - 0.004f, STEM),
                new Vector3(rr * 2.1f, y - 0.016f, STEM),
                new Vector3(rr * 2.15f, y - 0.011f, STEM),
                new Vector3(rr * 0.95f, y + 0.004f, STEM),
            };
            Lathe(kit, at, rot, p, around);
        }

        /// <summary>
        /// A surface of revolution around the shroom's own up. profile is (radius, height, material of the band up to this row),
        /// ordered underside outward first, then over the top inward (that order makes the faces point out).
        /// </summary>
        static void Lathe(NatureMeshKit kit, Vector3 at, Quaternion rot, IList<Vector3> profile, int around)
        {
            int prev = -1;
            for (int k = 0; k < profile.Count; k++)
            {
                int start = kit.VertexCount;
                for (int j = 0; j < around; j++)
                {
                    float a = Mathf.PI * 2f * j / around;
                    var local = new Vector3(Mathf.Cos(a) * profile[k].x, profile[k].y, Mathf.Sin(a) * profile[k].x);
                    kit.Vert(at + rot * local, new Vector2(j / (float)around, k / (float)(profile.Count - 1)));
                }
                if (prev >= 0)
                    for (int j = 0; j < around; j++)
                    {
                        int jn = (j + 1) % around;
                        kit.Quad((int)profile[k].z, prev + j, start + j, start + jn, prev + jn);
                    }
                prev = start;
            }
        }

        static Material[] Materials(Variant v, bool facet)
        {
            string f = facet ? "_Facet" : "", n = $"{Dir}/FairyRingMushroom_{v.name}";
            var m = new[]
            {
                NatureAssets.Mat($"{n}_Cap{f}.mat", v.cap, false, 0f),
                NatureAssets.Mat($"{n}_Center{f}.mat", v.center, false, 0f),
                NatureAssets.Mat($"{n}_Gill{f}.mat", v.gill, false, 0f),
                NatureAssets.Mat($"{n}_Stem{f}.mat", v.stem, false, 0f),
            };
            foreach (var x in m) x.SetFloat("_Facet", facet ? 1f : 0f);
            return m;
        }
    }
}
