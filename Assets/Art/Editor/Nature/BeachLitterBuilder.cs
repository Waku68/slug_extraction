using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 浜の小物（Nov 2 下書き、3 案ずつ）。メニュー Slug/Nature/Props/Build Beach Litter で作り直す（Assets/Art/Nature/BeachLitter）。
    /// 足元の層なので当たり判定なし。知っている物なので本物の大きさ（ナメクジと比べて大きさが分かる手がかり）。
    /// 貝殻・カニの抜け殻はかじれない硬い物なのでカクカク、海藻はなめらか。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - Shells 貝殻：A ザルガイ 8 cm と小さい巻貝 5 cm。B 横に倒れた大きな巻貝 15 cm（人なら 54 cm）。C 小さい貝 6 個と、内側を上に向けた大きな二枚貝 20 cm
    /// - CrabMolt カニの抜け殻：A まるごと（甲羅の幅 12 cm、脚 8 本とはさみ 2 つ）。B 裏返った甲羅とばらけた脚。C 大きなヤシガニのような殻 25 cm
    /// - Seaweed 打ち上げられた海藻：A 潮の線に沿って 2 m の帯。B 浮き袋のついた海藻の山 40 cm。C 根と柄のついた大きな昆布 1 本、葉 1.5 m
    /// 色：貝 #E6DCC8・桃色の貝 #D8A898・貝の帯 #A88A6A、抜け殻 #D89A70・暗い抜け殻 #B0704C、海藻 #5E5A30・#7A6A38・#6A6034。
    /// </summary>
    public static class BeachLitterBuilder
    {
        const string Dir = "Assets/Art/Nature/BeachLitter";
        const int SHELL = 0, SHELLPINK = 1, SHELLBAND = 2, MOLT = 3, MOLTDARK = 4, KELP = 5, KELPLIGHT = 6, WRACK = 7;
        static Material[] _m;

        [MenuItem("Slug/Nature/Props/Build Beach Litter")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "Beach",
                new PropKit.Spec("Shell", "#E6DCC8", true, 0f, true).Mottle("#C8B49A", 0.55f, 30f).Dent(0.25f, 120f),
                new PropKit.Spec("ShellPink", "#D8A898", true, 0f, true).Mottle("#B88678", 0.55f, 30f).Dent(0.25f, 120f),
                new PropKit.Spec("ShellBand", "#A88A6A", true, 0f, true).Mottle("#866A50", 0.5f, 30f),
                new PropKit.Spec("Molt", "#D89A70", true).Mottle("#B8704C", 0.55f, 45f).Dent(0.5f, 150f),
                new PropKit.Spec("MoltDark", "#B0704C", true).Mottle("#8A5236", 0.55f, 45f).Dent(0.5f, 150f),
                new PropKit.Spec("Kelp", "#5E5A30", false, 0f, true).Mottle("#48441F", 0.45f, 9f).Felt(0.2f),
                new PropKit.Spec("KelpLight", "#7A6A38", false, 0f, true).Mottle("#5E5028", 0.45f, 9f).Felt(0.2f),
                new PropKit.Spec("Wrack", "#6A6034", false).Mottle("#4E4626", 0.5f, 20f).Dent(0.4f, 80f));
            foreach (int i in new[] { KELP, KELPLIGHT, WRACK }) PropKit.CairnPaint(_m[i], "#2E2A18", 0.55f, 0.6f, 0.72f);
            var log = new StringBuilder("Beach litter built:");
            for (int v = 0; v < 3; v++) log.Append(' ').Append(Shells(v)).Append(' ').Append(Crab(v)).Append(' ').Append(Seaweed(v));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Abc(int v) => ((char)('A' + v)).ToString();
        static NatureMeshKit Kit() => new NatureMeshKit(_m.Length);
        static string Save(string name, NatureMeshKit kit) => PropKit.Save(Dir, name, kit, _m, 0);

        /// <summary>二枚貝の片方：放射状の筋のある浅い椀（長さ len）。upturned で内側を上に向ける。</summary>
        static void Clam(NatureMeshKit kit, Vector3 at, float len, float yaw, bool upturned, int sub, int seed)
        {
            int from = kit.VertexCount;
            var size = new Vector3(len * 0.52f, len * 0.3f, len * 0.5f);
            PropKit.Lump(kit, Vector3.zero, size, 10, 36, sub, seed, 0.04f, 1f, float.NaN, null, 0.5f, 1f, n =>
            {
                float ang = Mathf.Atan2(n.z, n.x);
                float growth = 0.025f * Mathf.Abs(Mathf.Sin((n - new Vector3(0f, 0.4f, -0.9f)).magnitude * 16f));
                return 0.07f * Mathf.Abs(Mathf.Sin(ang * 9f)) * (1f - n.y) - 0.15f * Mathf.Max(0f, -n.z) * (1f - n.y) - growth;
            });
            // the umbo (the knob at the hinge) and a darker growth band
            kit.Ball(new Vector3(0f, size.y * 0.55f, -size.z * 0.8f), Vector3.one * len * 0.09f, 3, sub);
            var q = Quaternion.Euler(upturned ? 180f : 0f, yaw, 0f);
            float lift = upturned ? size.y : 0.002f;
            kit.Transform(from, p => at + q * p + Vector3.up * lift);
        }

        /// <summary>巻貝：とがった先へ縮むらせんの筒。lying で横に倒す。len は殻の高さ。</summary>
        static void Snail(NatureMeshKit kit, Vector3 at, float len, float yaw, bool lying, int sub, int seed, int bandSub = -1)
        {
            int from = kit.VertexCount;
            // a coiled tube: the cross-section lies in the plane of the coil's radius and the axis, so it never twists;
            // each whorl sits on the one below and is a little narrower, so the spiral seam shows
            float turns = 3.2f, R0 = len * 0.3f;
            System.Func<float, int> band = bandSub >= 0 ? t => (int)(t * 14f) % 3 == 1 ? bandSub : sub : (System.Func<float, int>)null;
            PropKit.Surface(kit, (t, a) =>
            {
                float th = t * turns * Mathf.PI * 2f, k = Mathf.Pow(1f - t, 1.1f);
                var radial = new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th));
                var c = radial * (R0 * k) + Vector3.up * (len * 0.8f * Mathf.Pow(t, 0.85f));
                float r = len * 0.17f * Mathf.Pow(1f - t, 1.05f) * (t < 0.03f ? 1.3f : 1f) + 0.002f;
                return c + (radial * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * r;
            }, 60, 8, sub, sub, sub, 0f, band);
            var q = Quaternion.Euler(0f, yaw, lying ? 80f : 0f);
            kit.Transform(from, p => at + q * p + Vector3.up * (lying ? len * 0.32f : len * 0.05f));
        }

        static string Shells(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                Clam(kit, new Vector3(-0.05f, 0f, 0f), 0.08f, 20f, false, SHELL, 400);
                Snail(kit, new Vector3(0.07f, 0f, 0.03f), 0.05f, 60f, true, SHELLPINK, 401);
            }
            else if (v == 1)
                Snail(kit, Vector3.zero, 0.15f, 30f, true, SHELL, 402, SHELLBAND);
            else
            {
                Clam(kit, Vector3.zero, 0.2f, -15f, true, SHELL, 403);
                for (int i = 0; i < 6; i++)
                {
                    float a = PropKit.R(404, i, 0f, 360f), d = PropKit.R(405, i, 0.16f, 0.32f);
                    var at = PropKit.Dir(a) * d;
                    if (i % 2 == 0) Clam(kit, at, PropKit.R(406, i, 0.03f, 0.06f), PropKit.R(407, i, 0f, 360f), i == 4, i == 2 ? SHELLPINK : SHELL, 408 + i);
                    else Snail(kit, at, PropKit.R(406, i, 0.03f, 0.05f), PropKit.R(407, i, 0f, 360f), true, i == 3 ? SHELLBAND : SHELL, 408 + i);
                }
            }
            return Save($"Shells_{Abc(v)}", kit);
        }

        /// <summary>カニの抜け殻：甲羅の幅 w、脚 legs 本（片側 legs/2）、はさみ 2 つ。upside で裏返す（脚は縮こまる）。</summary>
        static void Crab(NatureMeshKit kit, Vector3 at, float w, float yaw, bool upside, int legs, int shell, int seed)
        {
            int from = kit.VertexCount;
            float h = w * 0.32f;
            // the carapace: a wide flat dome, a little wider at the front, with two lumps for the gills behind
            PropKit.Lump(kit, new Vector3(0f, h * 0.5f, 0f), new Vector3(w * 0.5f, h * 0.5f, w * 0.38f), 5, 14, shell, seed, 0.06f, 2f, float.NaN, null, 0f, 1f,
                n => 0.08f * n.z + 0.06f * Mathf.Abs(Mathf.Sin(Mathf.Atan2(n.z, n.x) * 4f)) * Mathf.Max(0f, n.y));
            float lr = w * 0.035f;
            for (int i = 0; i < legs; i++)
            {
                float sd = i % 2 == 0 ? 1f : -1f, k = (i / 2) / Mathf.Max(1f, legs / 2 - 1f);
                var root = new Vector3(sd * w * 0.42f, h * 0.35f, Mathf.Lerp(0.12f, -0.22f, k) * w);
                var outDir = (new Vector3(sd, 0f, Mathf.Lerp(0.5f, -0.6f, k))).normalized;
                var knee = root + outDir * (w * 0.4f) + Vector3.up * (upside ? -h * 0.2f : h * 0.6f);
                var foot = knee + outDir * (w * (upside ? 0.15f : 0.35f)) + Vector3.down * (upside ? -h * 0.4f : h * 1.0f);
                PropKit.Stick(kit, root, knee, lr * 1.2f, lr, 5, shell);
                PropKit.Stick(kit, knee, foot, lr, lr * 0.4f, 5, i % 3 == 0 ? MOLTDARK : shell);
            }
            for (int sd = -1; sd <= 1; sd += 2)
            {
                var root = new Vector3(sd * w * 0.3f, h * 0.4f, w * 0.32f);
                var elbow = root + new Vector3(sd * w * 0.18f, h * 0.1f, w * 0.25f);
                PropKit.Stick(kit, root, elbow, lr * 1.6f, lr * 1.4f, 6, shell);
                var claw = elbow + new Vector3(-sd * w * 0.08f, 0f, w * 0.16f);
                PropKit.Lump(kit, claw, new Vector3(w * 0.09f, w * 0.06f, w * 0.14f), 4, 8, MOLTDARK, seed + sd + 5, 0.08f);
                PropKit.Stick(kit, claw + new Vector3(0f, 0f, w * 0.1f), claw + new Vector3(-sd * w * 0.02f, 0f, w * 0.24f), lr * 1.3f, lr * 0.3f, 5, MOLTDARK);
            }
            var q = Quaternion.Euler(0f, yaw, upside ? 180f : 0f);
            float lift = upside ? h * 1.0f : h * 0.75f;
            kit.Transform(from, p => at + q * p + Vector3.up * lift);
        }

        static string Crab(int v)
        {
            var kit = Kit();
            if (v == 0) Crab(kit, Vector3.zero, 0.12f, 15f, false, 8, MOLT, 420);
            else if (v == 1)
            {
                Crab(kit, Vector3.zero, 0.12f, -30f, true, 4, MOLT, 421);
                for (int i = 0; i < 4; i++)
                {
                    var a = new Vector3(PropKit.R(422, i, -0.2f, 0.2f), 0.005f, PropKit.R(423, i, -0.2f, 0.2f));
                    var d = PropKit.Dir(PropKit.R(424, i, 0f, 360f)) * 0.05f;
                    PropKit.Stick(kit, a, a + d + Vector3.up * 0.005f, 0.004f, 0.002f, 5, MOLT);
                }
            }
            else Crab(kit, Vector3.zero, 0.25f, 40f, false, 8, MOLTDARK, 425);
            return Save($"CrabMolt_{Abc(v)}", kit);
        }

        /// <summary>寝た海藻の帯 1 本：root から dir へ長さ len、幅 w、波打つ ripple（m）。</summary>
        static void Ribbon(NatureMeshKit kit, Vector3 root, Vector3 dir, float len, float w, float ripple, int sub, int seed, int segs = 10)
        {
            dir = new Vector3(dir.x, 0f, dir.z).normalized;
            var side = Vector3.Cross(Vector3.up, dir);
            float ph = PropKit.R(seed, 0, 0f, 6f), sway = PropKit.R(seed, 1, -0.15f, 0.15f);
            System.Func<float, Vector3> path = s => root + dir * (len * s) + side * (len * sway * Mathf.Sin(s * 3f + ph)) + Vector3.up * (0.006f + ripple * Mathf.Abs(Mathf.Sin(s * 9f + ph)));
            kit.Blade(path, s => w * (0.4f + 0.6f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(s * 0.9f + 0.1f))) * (1f + 0.25f * Mathf.Sin(s * 23f)), segs, 0.004f, 0.05f, sub);
        }

        static string Seaweed(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                // a strand along the high-tide line: ribbons lying along a wavy line, wrack lumps between
                for (int i = 0; i < 16; i++)
                {
                    float x = Mathf.Lerp(-1f, 1f, i / 15f) + PropKit.R(430, i, -0.06f, 0.06f);
                    var root = new Vector3(x, 0f, 0.12f * Mathf.Sin(x * 2.6f) + PropKit.R(431, i, -0.08f, 0.08f));
                    Ribbon(kit, root, PropKit.Dir(PropKit.R(432, i, -50f, 50f) + (i % 2) * 180f), PropKit.R(433, i, 0.3f, 0.55f), PropKit.R(434, i, 0.02f, 0.04f), 0.01f, i % 3 == 0 ? KELPLIGHT : KELP, 435 + i);
                }
                for (int i = 0; i < 5; i++)
                    PropKit.Lump(kit, new Vector3(Mathf.Lerp(-0.8f, 0.8f, i / 4f), 0f, 0.1f * Mathf.Sin(i * 1.7f)), new Vector3(0.08f, 0.03f, 0.06f), 3, 9, WRACK, 450 + i, 0.4f, 3f, 0f);
            }
            else if (v == 1)
            {
                for (int i = 0; i < 9; i++)
                {
                    var dir = PropKit.Dir(i * 40f + PropKit.R(460, i, -15f, 15f));
                    var root = dir * PropKit.R(461, i, 0f, 0.05f) + Vector3.up * (0.01f * (i % 3));
                    float len = PropKit.R(462, i, 0.15f, 0.22f);
                    Ribbon(kit, root, dir, len, 0.012f, 0.02f, i % 2 == 0 ? WRACK : KELPLIGHT, 463 + i, 6);
                    // the air bladders in pairs along the frond
                    for (int b = 0; b < 3; b++)
                    {
                        var p = root + dir * (len * (0.3f + 0.22f * b)) + Vector3.up * 0.016f;
                        kit.Ball(p + Vector3.Cross(Vector3.up, dir) * 0.012f, new Vector3(0.011f, 0.008f, 0.014f), 3, WRACK);
                        kit.Ball(p - Vector3.Cross(Vector3.up, dir) * 0.012f, new Vector3(0.011f, 0.008f, 0.014f), 3, WRACK);
                    }
                }
                PropKit.Lump(kit, Vector3.zero, new Vector3(0.1f, 0.045f, 0.08f), 3, 9, WRACK, 475, 0.4f, 3f, 0f);
            }
            else
            {
                // the holdfast: a knot of short root fingers; then the stipe; then one long rippled blade
                for (int i = 0; i < 7; i++)
                {
                    var d = PropKit.Dir(i * 51f);
                    PropKit.Pipe(kit, t => new Vector3(-0.8f, 0.03f, 0f) + d * (0.08f * t) + Vector3.down * (0.03f * t * t), (t, a) => Mathf.Lerp(0.014f, 0.006f, t), 3, 5, KELP, -1, KELP);
                }
                PropKit.Pipe(kit, t => new Vector3(-0.8f + 0.55f * t, 0.025f, 0.05f * Mathf.Sin(t * 3f)), (t, a) => Mathf.Lerp(0.022f, 0.016f, t), 6, 7, KELP, KELP, -1);
                Vector3 b0 = new Vector3(-0.25f, 0f, 0.05f * Mathf.Sin(3f));
                kit.Blade(s => b0 + new Vector3(1.5f * s, 0.008f + 0.012f * Mathf.Abs(Mathf.Sin(s * 14f)), 0.12f * Mathf.Sin(s * 2.4f)),
                    s => 0.12f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(s * 0.85f + 0.12f)), 0.6f) * (1f + 0.12f * Mathf.Sin(s * 31f)), 26, 0.004f, 0.08f, KELPLIGHT);
            }
            return Save($"Seaweed_{Abc(v)}", kit);
        }
    }
}
