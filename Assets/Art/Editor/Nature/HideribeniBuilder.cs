using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ヒデリベニ（日照り紅、古い名前サンゴソウ。Oct 27 灰色ラフ 3 案）。砂漠の塩田の、川の流れ込む端と割れ目に生える。
    /// 雨季は緑、乾季が深まるほど赤い（白い塩田の赤い目印）。ピッケルで株ごと採る（spec 6章）。メニュー Slug/Nature/Build Hideribeni。
    ///
    /// 形は本物のアッケシソウ（わくさん Oct 27）：葉のない、節のある多肉の棒が向かい合わせに枝分かれする、サンゴのような株。
    /// ポップは太さで出す：棒の太さ 2.5〜4 cm（本物の 5〜8 倍）、節と節の間がふくらむ。
    /// - A こんもり：低く丸く広がる株。高さ 25 cm、幅 60 cm（人なら 90 cm × 2.2 m の茂み）
    /// - B 燭台：太い 3 本が立ち、枝が横へ出てから上へ。高さ 40 cm（人なら 1.4 m）。サンゴの枝らしさがいちばん強い
    /// - C 這う：割れ目に沿って地を這う茎から、短い指が並んで立つ。長さ 70 cm、高さ 12 cm
    /// 色（仮）：茎 #7E9A52（雨季。乾季は赤 #B4483C、マテリアルの色だけで変える）、節 #5E7642（赤は #7E2F2A）、株もと #8C7B5E。
    /// Oct 27 わくさん：A こんもりに決定。赤い姿は同じ形のプレハブ Hideribeni_A_Red。
    /// </summary>
    public static class HideribeniBuilder
    {
        const string Dir = "Assets/Art/Nature/Hideribeni";
        const int STEM = 0, JOINT = 1, BASE = 2;
        const int Sides = 7;
        const float OutlineWidth = 0.004f;

        [MenuItem("Slug/Nature/Build Hideribeni")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var mats = Materials(false);
            var red = Materials(true);
            var log = new System.Text.StringBuilder("Hideribeni built:");
            log.Append(' ').Append(Make("A", Mound, mats, red));
            log.Append(' ').Append(Make("B", Candelabra, mats, red));
            log.Append(' ').Append(Make("C", Creeping, mats, red));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        /// <summary>緑（雨季）の株と、同じ形で色だけ赤い乾季の株（_Red）。</summary>
        static string Make(string variant, Action<NatureMeshKit> build, Material[] mats, Material[] red)
        {
            string name = $"Hideribeni_{variant}";
            var kit = new NatureMeshKit(3);
            build(kit);
            var mesh = NatureAssets.SaveMesh(kit.ToMesh(name), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, mats, null);
            NatureAssets.SavePrefab($"{Dir}/{name}_Red.prefab", mesh, red, null);
            return $"{name} {kit.TriangleCount} tris";
        }

        /// <summary>節のある棒：節と節の間がふくらみ、節は細く暗い帯。先は丸い。</summary>
        static void Jointed(NatureMeshKit kit, Func<float, Vector3> c, float r, int segs, int seed)
        {
            var rows = new List<Vector3> { new Vector3(0f, 0.9f, STEM) };
            float d = 0.06f / segs;
            for (int k = 0; k < segs; k++)
            {
                float t0 = k / (float)segs, t1 = (k + 1f) / segs;
                rows.Add(new Vector3(Mathf.Lerp(t0, t1, 0.5f), 1.15f, STEM));
                if (k < segs - 1) { rows.Add(new Vector3(t1 - d, 0.86f, STEM)); rows.Add(new Vector3(t1 + d, 0.86f, JOINT)); }
            }
            rows.Add(new Vector3(0.97f, 0.85f, STEM));
            rows.Add(new Vector3(1f, 0.45f, STEM));
            int from = kit.VertexCount;
            kit.BeginTube();
            kit.Tube(c, t => r * (1f - 0.3f * t), rows, Sides, STEM, STEM);
            kit.Paint(from, (p, uv) => new Color(Mathf.Clamp01(p.y * 2f), 0f, NatureAssets.Hash(seed, 3), 1f));
        }

        /// <summary>
        /// 1 本の枝と、節ごとに向かい合う小枝（向きは節ごとに 90° ずつ回る）。小枝は外へ出てから上へ。
        /// </summary>
        static void Branch(NatureMeshKit kit, Func<float, Vector3> c, float r, int segs, float twigLen, int twigSegs, int firstJoint, int seed, float outward = 0.5f)
        {
            Jointed(kit, c, r, segs, seed);
            for (int k = firstJoint; k < segs; k++)
            {
                float t = k / (float)segs;
                Vector3 p = c(t), tan = (c(t + 0.02f) - c(t - 0.02f)).normalized;
                var s0 = Vector3.Cross(Vector3.up, tan);
                if (s0.sqrMagnitude < 1e-4f) s0 = Vector3.right;
                s0.Normalize();
                if ((k & 1) == 1) s0 = Vector3.Cross(tan, s0).normalized;
                float len = twigLen * Mathf.Lerp(1f, 0.6f, t) * Mathf.Lerp(0.85f, 1.15f, NatureAssets.Hash(seed, k));
                foreach (float sign in new[] { -1f, 1f })
                {
                    var o = s0 * sign;
                    Vector3 P(float u) => p + o * (len * outward * u * (2f - u)) + Vector3.up * (len * 0.9f * u * u) + tan * (len * 0.25f * u);
                    Jointed(kit, P, r * 0.72f, twigSegs, seed + k * 7 + (sign > 0 ? 1 : 0));
                }
            }
        }

        static void Base(NatureMeshKit kit, Vector3 scale) => kit.Ball(Vector3.up * (scale.y * 0.3f), scale, 5, BASE);

        // A: many short stems radiating low and curling up, a rounded cushion
        static void Mound(NatureMeshKit kit)
        {
            const int seed = 701;
            Base(kit, new Vector3(0.06f, 0.035f, 0.06f));
            const int n = 7;
            for (int i = 0; i < n; i++)
            {
                float yaw = Mathf.PI * 2f * i / n + (NatureAssets.Hash(seed, i) - 0.5f) * 0.6f;
                var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                float L = Mathf.Lerp(0.2f, 0.27f, NatureAssets.Hash(seed, i + 10));
                float lift = Mathf.Lerp(0.15f, 0.4f, NatureAssets.Hash(seed, i + 20));
                Vector3 start = Vector3.up * 0.015f;
                Vector3 C(float t) => start + h * (L * t) + Vector3.up * (L * (lift * t + 0.35f * t * t));
                Branch(kit, C, 0.016f, 4, 0.1f, 2, 2, seed + i * 31);
            }
        }

        // B: three thick upright stems; branches go out then turn up like a candelabra
        static void Candelabra(NatureMeshKit kit)
        {
            const int seed = 811;
            Base(kit, new Vector3(0.05f, 0.04f, 0.05f));
            for (int i = 0; i < 3; i++)
            {
                float yaw = Mathf.PI * 2f * i / 3f + 0.4f;
                var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                float L = Mathf.Lerp(0.34f, 0.42f, NatureAssets.Hash(seed, i));
                Vector3 C(float t) => Vector3.up * 0.02f + h * (L * (0.22f * t + 0.08f * t * t)) + Vector3.up * (L * t);
                Branch(kit, C, 0.02f, 5, 0.14f, 3, 1, seed + i * 31, 0.65f);
            }
        }

        // C: stems crawl along a crack, short fingers standing up from every joint
        static void Creeping(NatureMeshKit kit)
        {
            const int seed = 919;
            Base(kit, new Vector3(0.05f, 0.03f, 0.05f));
            for (int i = 0; i < 3; i++)
            {
                float dirX = i == 1 ? -1f : 1f;
                float z = (i - 1) * 0.04f;
                float L = i == 1 ? 0.3f : Mathf.Lerp(0.3f, 0.38f, NatureAssets.Hash(seed, i));
                Vector3 C(float t) => new Vector3(dirX * L * t, 0.016f + 0.03f * Mathf.Sin(Mathf.PI * t * 0.7f), z * (1f + 2f * t));
                Jointed(kit, C, 0.016f, 5, seed + i);
                for (int k = 1; k < 5; k++)
                {
                    Vector3 p = C(k / 5f);
                    float len = Mathf.Lerp(0.08f, 0.13f, NatureAssets.Hash(seed, i * 10 + k));
                    float lean = (NatureAssets.Hash(seed, i * 10 + k + 50) - 0.5f) * 0.6f;
                    Vector3 F(float u) => p + Vector3.up * (len * u) + new Vector3(lean * dirX, 0f, lean) * (len * 0.4f * u * u);
                    Jointed(kit, F, 0.013f, 2, seed + i * 10 + k);
                }
            }
        }

        static Material[] Materials(bool red)
        {
            var m = new[]
            {
                red ? NatureAssets.Mat($"{Dir}/Hideribeni_StemRed.mat", "#B4483C", false, 0.01f)
                    : NatureAssets.Mat($"{Dir}/Hideribeni_Stem.mat", "#7E9A52", false, 0.01f),
                red ? NatureAssets.Mat($"{Dir}/Hideribeni_JointRed.mat", "#7E2F2A", false, 0.01f)
                    : NatureAssets.Mat($"{Dir}/Hideribeni_Joint.mat", "#5E7642", false, 0.01f),
                NatureAssets.Mat($"{Dir}/Hideribeni_Base.mat", "#8C7B5E", false, 0f),
            };
            foreach (var x in m) { x.SetFloat("_Facet", 0f); x.SetFloat("_OutlineWidth", OutlineWidth); }   // 小さな草花は輪郭線を細く
            return m;
        }
    }
}
