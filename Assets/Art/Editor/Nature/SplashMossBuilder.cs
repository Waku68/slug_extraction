using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// しぶき苔（Nov 3 下書き。わくさんのペルソナが監督）。滝のしぶきが届く崖・洞窟の入り口の濡れた壁に張り付く、ふかふかの苔のクッション。
    /// ピッケルで採る。晴れが島で 1 日続くと縮む。メニュー Slug/Nature/Build Splash Moss（Assets/Art/Nature/SplashMoss）。
    /// 壁に付く物なので、原点は壁に触れる面のいちばん下の真ん中（壁は原点を通る XY の面、苔は +Z へふくらむ、+Y が上）。
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A 濡れた株：幅 0.65 m・高さ 0.3 m・厚み 0.13 m（壁に沿って横長。人なら 2.3 m × 1.1 m のクッション）。塊 4 つが重なり、ふちに大きなこぶ 5 つ、
    ///   小さな白い光の点が 3 つ、低く片寄せて散らす（濡れている印。青い大きな水の玉は水玉模様に、横に並んだ 2 つは目に見えた）。
    ///   垂れる房は付けない（壁の上では足に見えた）。房のマテリアル（Fringe）は残してあるが使っていない
    /// - B 縮んだ株：大きさ 7 割・厚み半分、薄い茶色、上のふちが壁から 4 cm はがれて反る、光の点なし
    /// 色：苔 #6F9450・こぶ #7EA05A（陰は青み）、房 #5E8044、光の点 #F6F8F4、縮んだ苔 #9A9060・こぶ #A49A68・房 #8A7E52。苔はかじれるのでなめらか。
    /// 当たり判定は箱（ピッケルの的）。
    /// </summary>
    public static class SplashMossBuilder
    {
        const string Dir = "Assets/Art/Nature/SplashMoss";
        const int MOSS = 0, BUMP = 1, FRINGE = 2, DROP = 3, DRYMOSS = 4, DRYBUMP = 5, DRYFRINGE = 6;
        static Material[] _m;

        [MenuItem("Slug/Nature/Build Splash Moss")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "SplashMoss",
                new PropKit.Spec("Moss", "#6F9450", false, 0.01f).Mottle("#7EA05A", 0.25f, 9f),
                new PropKit.Spec("Bump", "#7EA05A", false, 0.01f),
                new PropKit.Spec("Fringe", "#5E8044", false, 0.03f),
                new PropKit.Spec("Drop", "#F6F8F4", false),
                new PropKit.Spec("DryMoss", "#9A9060", false).Mottle("#8A8052", 0.3f, 9f),
                new PropKit.Spec("DryBump", "#A49A68", false),
                new PropKit.Spec("DryFringe", "#8A7E52", false, 0.02f));
            foreach (var x in _m) PropKit.CairnPaint(x);
            _m[DROP].SetFloat("_RimStrength", 0.8f);   // the water drops catch the light
            var log = new System.Text.StringBuilder("Splash moss built:");
            log.Append(' ').Append(Make(0)).Append(' ').Append(Make(1));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Make(int v)
        {
            bool dry = v == 1;
            var kit = new NatureMeshKit(_m.Length);
            int seed = 3100 + v * 40;
            float s = dry ? 0.7f : 1f, th = dry ? 0.5f : 1f, lift = 0.12f * s;
            int moss = dry ? DRYMOSS : MOSS, bump = dry ? DRYBUMP : BUMP;
            // the dry one peels off the wall along its top edge
            System.Func<Vector3, Vector3> wall = p =>
            {
                float peel = dry ? 0.04f * Mathf.SmoothStep(0f, 1f, (p.y - lift - 0.18f * s) / (0.12f * s)) : 0f;
                return new Vector3(p.x, p.y, Mathf.Max(0f, p.z) + peel);
            };

            // the cushion: four overlapping lumps pressed flat against the wall, spread sideways along it (a tall heap reads as broccoli)
            var lumps = new (Vector3 c, Vector3 r)[]
            {
                (new Vector3(-0.04f, 0.13f, 0f), new Vector3(0.2f, 0.12f, 0.12f)),
                (new Vector3(-0.21f, 0.1f, 0f), new Vector3(0.12f, 0.09f, 0.09f)),
                (new Vector3(0.18f, 0.11f, 0f), new Vector3(0.14f, 0.1f, 0.1f)),
                (new Vector3(0.07f, 0.2f, 0f), new Vector3(0.13f, 0.07f, 0.08f)),
            };
            int from = kit.VertexCount;
            for (int k = 0; k < lumps.Length; k++)
            {
                var c = lumps[k].c * s + Vector3.up * lift;
                var r = Vector3.Scale(lumps[k].r * s, new Vector3(1f, 1f, th));
                int lf = kit.VertexCount;
                PropKit.Lump(kit, c, r, 9, 16, moss, seed + k, 0.12f, 2f, float.NaN, null, 0f, 1f, null, wall);
                kit.Paint(lf, (p, uv) => new Color(0f, 0f, 0f, 1f));
                kit.Mass(lf, c, r, 1f);
            }
            // a few big soft bumps on the rim, half sunk: the outline reads soft without turning into a pile of little balls
            for (int i = 0; i < 5; i++)
            {
                var (c, r) = lumps[i % lumps.Length];
                c = c * s + Vector3.up * lift;
                r = Vector3.Scale(r * s, new Vector3(1f, 1f, th));
                float az = PropKit.R(seed + 10, i, 0f, Mathf.PI * 2f);
                var n = new Vector3(Mathf.Cos(az) * 0.85f, Mathf.Sin(az) * 0.85f, 0.5f);
                float br = PropKit.R(seed + 30, i, 0.045f, 0.06f) * s;
                kit.Ball(wall(c + Vector3.Scale(n, r) * 0.85f), new Vector3(br, br * 0.85f, br * 0.7f * th), 8, bump);
            }
            kit.Mass(from, new Vector3(0f, 0.14f * s + lift, 0f), new Vector3(0.38f, 0.2f, 0.15f) * s, 0.4f);

            // no hanging strands: on a wall any row of them read as legs (and two glints above them as eyes)
            // the wet one glints: three tiny white points of light, scattered low and to one side, never a level pair
            if (!dry)
            {
                var glints = new[] { new Vector3(0.21f, 0.07f, 0f), new Vector3(0.12f, 0.17f, 0f), new Vector3(-0.17f, 0.05f, 0f) };
                for (int i = 0; i < glints.Length; i++)
                {
                    var p = glints[i] + Vector3.up * lift;
                    var hit = p + Vector3.forward * 0.3f;
                    // drop it onto the cushion's front: march back toward the wall until inside a lump
                    for (int step = 0; step < 60; step++)
                    {
                        bool inside = false;
                        foreach (var (c, r) in lumps)
                        {
                            var d = hit - (c + Vector3.up * lift);
                            if (d.x * d.x / (r.x * r.x) + d.y * d.y / (r.y * r.y) + d.z * d.z / (r.z * r.z) < 1f) { inside = true; break; }
                        }
                        if (inside) break;
                        hit -= Vector3.forward * 0.005f;
                    }
                    float dr = PropKit.R(seed + 51, i, 0.005f, 0.0065f);
                    kit.Ball(hit + Vector3.forward * 0.004f, new Vector3(dr, dr, dr * 0.7f), 4, DROP);
                }
            }
            return PropKit.Save(Dir, $"SplashMoss_{(char)('A' + v)}", kit, _m, 1);
        }
    }
}
