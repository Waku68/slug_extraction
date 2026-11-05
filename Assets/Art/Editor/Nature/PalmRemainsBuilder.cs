using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ヤシの残り（Nov 2 下書き、Nov 3 作り直し：立っているヤシと同じ A 曲がり・B まっすぐ高いの形 PalmShape で）。
    /// メニュー Slug/Nature/Props/Build Palm Remains で作り直す（Assets/Art/Nature/PalmRemains）。
    /// 枯れた幹・ヤシの実の皮と殻はかじれないのでカクカク、葉・実の中身・芽はなめらか（葉はケルン風）。
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - DeadPalm 立ち枯れのヤシ（砂漠の端、島が乾いていく絵）：A 曲がりのヤシが葉を落とし、5.6 m で折れた幹（人なら 20 m）。
    ///   B まっすぐ高いヤシが立ったまま枯れ、9 枚の葉がみな茶色く幹に沿って垂れる（10 m）。C 曲がりのヤシが 1 m で折れた株と、鞘と枯れ葉がついたまま倒れた上の部分
    /// - FallenFrond 落ちたヤシの葉（立っているヤシと同じ葉、3.6〜3.8 m、人なら 13 m）：A 落ちたばかりの緑。B 茶色く乾いて折れが深い。C 3 枚の重なり
    /// - Coconut ヤシの実（本物の大きさ、直径 約 28 cm）：A 茶色と緑の実 2 つ。B 割れた実（皮の椀と白い中身、伏せたもう半分）。C 芽の出た実
    /// 色：枯れた幹 #B4AA96（立っているヤシ #B0A28A を少し白っぽく）・節の溝 #7A6E5E、枯れ葉 #B89D70、緑の葉 #788A50、鞘 #8A8A55・枯れた鞘 #9A8A62、
    /// 実の皮 #6A5A3A・緑の皮 #8A8A45、殻 #4E3E2E、中身 #EDE6D6、芽 #8AA050。
    /// </summary>
    public static class PalmRemainsBuilder
    {
        const string Dir = "Assets/Art/Nature/PalmRemains";
        const int TRUNK = 0, RING = 1, DRY = 2, GREEN = 3, SHEATH = 4, HUSK = 5, HUSKGREEN = 6, NUTSHELL = 7, MEAT = 8, SHOOT = 9, DEADCROWN = 10;
        static Material[] _m;

        [MenuItem("Slug/Nature/Props/Build Palm Remains")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "PalmRemains",
                // the dead trunk is the living palm's colours (#B0A28A, #7A6C5A), a little greyer and paler from the sun
                new PropKit.Spec("Trunk", "#B4AA96", true).Mottle("#9E9480", 0.35f, 3f),
                new PropKit.Spec("Ring", "#7A6E5E", true),
                new PropKit.Spec("DryLeaf", "#B89D70", false, 0.02f, true).Mottle("#9C8058", 0.45f, 10f),
                new PropKit.Spec("GreenLeaf", "#788A50", false, 0.02f, true).Mottle("#9A9A54", 0.35f, 8f),
                new PropKit.Spec("Sheath", "#8A8A55", false).Felt(0.3f),
                new PropKit.Spec("Husk", "#6A5A3A", true, 0f, true).Mottle("#54462E", 0.5f, 14f).Felt(0.6f).Dent(0.5f, 70f),
                new PropKit.Spec("HuskGreen", "#8A8A45", true).Mottle("#6E7038", 0.5f, 14f).Felt(0.4f).Dent(0.3f, 70f),
                new PropKit.Spec("NutShell", "#4E3E2E", true, 0f, true).Felt(0.4f).Dent(0.4f, 90f),
                new PropKit.Spec("Meat", "#EDE6D6", false, 0f, true).Felt(0.15f),
                new PropKit.Spec("Shoot", "#8AA050", false, 0.03f, true).Mottle("#9AAA5C", 0.3f, 12f),
                new PropKit.Spec("DeadCrown", "#9A8A62", false));
            foreach (int i in new[] { DRY, GREEN, SHEATH, SHOOT, DEADCROWN }) PropKit.CairnPaint(_m[i], i == DRY || i == DEADCROWN ? "#4A3E2A" : "#2E3E2C");
            var log = new StringBuilder("Palm remains built:");
            for (int v = 0; v < 3; v++) log.Append(' ').Append(DeadPalm(v)).Append(' ').Append(Frond(v)).Append(' ').Append(Coconut(v));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Abc(int v) => ((char)('A' + v)).ToString();
        static NatureMeshKit Kit() => new NatureMeshKit(_m.Length);

        /// <summary>枯れて垂れた葉：付け根 crown から外の向き yaw へ少しだけ出て、幹に沿って垂れ下がる（幅は幹の回りの向き）。</summary>
        static void HangingFrond(NatureMeshKit kit, Vector3 crown, float yaw, float len, float width, int seed)
        {
            var path = PalmShape.FrondPath(crown, yaw, PropKit.R(seed, 0, -78f, -58f), len, PropKit.R(seed, 1, 0.08f, 0.25f));
            PalmShape.Frond(kit, path, width, 0.75f, DRY, 10, Vector3.Cross(Vector3.up, PropKit.Dir(yaw)).normalized);
        }

        static string DeadPalm(int v)
        {
            var kit = Kit();
            var A = PalmShape.A;
            if (v == 0)
            {
                // the curved palm, its crown long gone: snapped at 5.6 m, the break frayed
                PalmShape.Trunk(kit, A, -0.04f, 0.7f, TRUNK, RING, -1, DEADCROWN, 0.35f, 600);
                Fibres(kit, PalmShape.Center(A, 0.7f) - Vector3.up * 0.08f, Vector3.up, PalmShape.Radius(A, 0.7f) * 0.8f, 0.3f, 601);
            }
            else if (v == 1)
            {
                // the tall straight palm, dead where it stands: the same trunk and crown, every frond dried and hanging down round the trunk
                var B = PalmShape.B;
                PalmShape.Trunk(kit, B, -0.04f, 1f, TRUNK, RING, -1, -1, 0f, 602);
                Vector3 crown = PalmShape.Crown(kit, B, TRUNK, DEADCROWN);
                for (int i = 0; i < B.fronds; i++)
                    HangingFrond(kit, crown, 360f * i / B.fronds + PropKit.R(603, i, -12f, 12f), B.flen * PropKit.R(604, i, 0.7f, 0.9f), B.fwid * 0.62f, 605 + i * 3);
            }
            else
            {
                // the curved palm snapped low: the stump, and the top lying beside it with its crown and dead fronds still on
                PalmShape.Trunk(kit, A, -0.04f, 0.12f, TRUNK, RING, -1, DEADCROWN, 0.25f, 610);
                Fibres(kit, PalmShape.Center(A, 0.12f) - Vector3.up * 0.06f, Vector3.up, PalmShape.Radius(A, 0.12f) * 0.8f, 0.22f, 611);
                int from = kit.VertexCount;
                PalmShape.Trunk(kit, A, 0.14f, 1f, TRUNK, RING, DEADCROWN, -1, 0f, 612);
                Vector3 crown = PalmShape.Crown(kit, A, TRUNK, DEADCROWN);
                for (int i = 0; i < 6; i++)
                    HangingFrond(kit, crown, i * 60f + 15f, A.flen * PropKit.R(613, i, 0.7f, 0.85f), A.fwid * 0.62f, 614 + i * 3);
                // lay it down: the broken end near the stump, the crown away along +X, resting on the ground
                Vector3 pivot = PalmShape.Center(A, 0.14f);
                var q = Quaternion.Euler(0f, -25f, 0f) * Quaternion.Euler(0f, 0f, -88f);
                float lift = PalmShape.Radius(A, 0.14f) * 0.9f;
                kit.Transform(from, p =>
                {
                    var r = q * (p - pivot) + new Vector3(0.5f, lift, 0.35f);
                    r.y = Mathf.Max(r.y, 0.015f);   // the fronds under it are pressed flat on the ground
                    return r;
                });
            }
            return PropKit.Save(Dir, $"DeadPalm_{Abc(v)}", kit, _m, 3);
        }

        /// <summary>折れた所のけば立った繊維：中心 c のまわり（半径 r）から、向き dir へ長さ len くらいの細い筋がばらばらに出る。</summary>
        static void Fibres(NatureMeshKit kit, Vector3 c, Vector3 dir, float r, float len, int seed)
        {
            for (int i = 0; i < 16; i++)
            {
                float a = PropKit.R(seed, i, 0f, Mathf.PI * 2f), d = r * Mathf.Sqrt(PropKit.H(seed + 1, i));
                var at = c + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                var to = at + (dir + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * PropKit.R(seed + 2, i, 0.2f, 0.8f)).normalized * len * PropKit.R(seed + 3, i, 0.4f, 1.1f);
                PropKit.Stick(kit, at - dir * 0.1f, to, 0.014f, 0.004f, 4, DEADCROWN);
            }
        }

        /// <summary>
        /// 地面に落ちた葉：立っているヤシと同じ葉を、根元 o から水平の向き yaw へ寝かせる。V 字のふちが地面に着くように、
        /// 中心線をそのぶん浮かせ、ゆるくうねらせ、先を少し反らせる。
        /// </summary>
        static void LyingFrond(NatureMeshKit kit, Vector3 o, float yaw, float len, float width, float fold, int sub, int seed, float curlTip = 0f)
        {
            var d = PropKit.Dir(yaw); var side = Vector3.Cross(Vector3.up, d);
            float ph = PropKit.R(seed, 0, 0f, 6f);
            System.Func<float, Vector3> path = s0 =>
                o + d * (len * s0) + side * (0.15f * len / 3.6f * Mathf.Sin(s0 * 3f + ph))
                  + Vector3.up * (0.03f + PalmShape.FrondWidth(s0, width, 14) * fold + 0.06f * Mathf.Sin(Mathf.PI * s0 * 0.8f) + curlTip * Mathf.Pow(Mathf.Max(0f, s0 - 0.6f) / 0.4f, 2f));
            PalmShape.Frond(kit, path, width, fold, sub);
            // the stiff stalk at the base, where it broke from the crown
            PropKit.Pipe(kit, t => o - d * (0.35f * (1f - t)) + Vector3.up * (0.06f + 0.03f * t), (t, a2) => Mathf.Lerp(0.07f, 0.04f, t), 4, 7, sub == GREEN ? SHEATH : DEADCROWN, sub == GREEN ? SHEATH : DEADCROWN, -1);
        }

        static string Frond(int v)
        {
            var kit = Kit();
            var A = PalmShape.A;
            if (v == 0) LyingFrond(kit, new Vector3(-1.8f, 0f, 0f), 0f, A.flen, A.fwid, 0.45f, GREEN, 620);
            else if (v == 1) LyingFrond(kit, new Vector3(-1.6f, 0f, 0f), 5f, A.flen * 0.85f, A.fwid * 0.7f, 0.85f, DRY, 621, 0.35f);
            else
                for (int i = 0; i < 3; i++)
                {
                    float yaw = i * 55f - 30f, l = A.flen * (i == 1 ? 0.95f : 0.8f);
                    var o = -PropKit.Dir(yaw) * (l * 0.45f) + Vector3.up * (0.05f * i);
                    LyingFrond(kit, o, yaw, l, A.fwid * (i == 1 ? 1f : 0.7f), i == 1 ? 0.45f : 0.85f, i == 1 ? GREEN : DRY, 622 + i, i == 1 ? 0f : 0.25f);
                }
            return PropKit.Save(Dir, $"FallenFrond_{Abc(v)}", kit, _m, 0);
        }

        /// <summary>ヤシの実 1 つ（直径 約 28 cm）：三つの稜のある卵形、横に寝る。先にとがり、付け根に 3 つのへそ。</summary>
        static void Nut(NatureMeshKit kit, Vector3 at, float yaw, int sub, int seed, float tFrom = 0f, bool upturned = false)
        {
            int from = kit.VertexCount;
            PropKit.Lump(kit, Vector3.zero, new Vector3(0.13f, 0.155f, 0.13f), 14, 40, sub, seed, 0.05f, 1.5f, float.NaN, null, tFrom, 1f,
                n => 0.07f * Mathf.Cos(3f * Mathf.Atan2(n.z, n.x)) * (1f - n.y * n.y) + 0.12f * Mathf.Max(0f, n.y) * Mathf.Max(0f, n.y) * n.y
                     + 0.03f * Mathf.Abs(Mathf.Sin(Mathf.Atan2(n.z, n.x) * 16f + n.y * 2f)) * (1f - n.y * n.y));
            var q = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(upturned ? 180f : 0f, 0f, 0f) * Quaternion.Euler(0f, 0f, 90f);
            float lift = tFrom > 0f ? 0.005f : 0.12f;
            kit.Transform(from, p => at + q * p + Vector3.up * lift);
        }

        static string Coconut(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                Nut(kit, Vector3.zero, 15f, HUSK, 610);
                Nut(kit, new Vector3(0.3f, 0f, 0.12f), 70f, HUSKGREEN, 611);
            }
            else if (v == 1)
            {
                // one half lying cup-up: husk, the dark shell inside it, the white meat; the other half face down
                int from = kit.VertexCount;
                PropKit.Lump(kit, Vector3.zero, new Vector3(0.13f, 0.13f, 0.155f), 5, 12, HUSK, 612, 0.05f, 1.5f, float.NaN, null, 0.5f, 1f,
                    n => 0.07f * Mathf.Cos(3f * Mathf.Atan2(n.y, n.x)));
                PropKit.Lump(kit, Vector3.zero, new Vector3(0.095f, 0.095f, 0.1f), 4, 12, NUTSHELL, 613, 0f, 1f, float.NaN, null, 0.5f, 1f);
                PropKit.Lump(kit, Vector3.zero, new Vector3(0.087f, 0.087f, 0.092f), 4, 12, MEAT, 614, 0f, 1f, float.NaN, null, 0.5f, 1f);
                kit.Transform(from, p => Quaternion.Euler(180f, 20f, 0f) * p + Vector3.up * 0.13f);
                Nut(kit, new Vector3(0.32f, 0f, 0.06f), 100f, HUSK, 615, 0.5f, false);
                // loose husk fibres
                for (int i = 0; i < 6; i++)
                {
                    var a = new Vector3(PropKit.R(616, i, -0.2f, 0.45f), 0.006f, PropKit.R(617, i, -0.22f, 0.25f));
                    PropKit.Stick(kit, a, a + PropKit.Dir(PropKit.R(618, i, 0f, 360f)) * PropKit.R(619, i, 0.08f, 0.16f), 0.005f, 0.003f, 4, HUSK);
                }
            }
            else
            {
                Nut(kit, Vector3.zero, 0f, HUSK, 620);
                // the shoot comes out of the stalk end: a few folded young leaves standing up, and roots into the sand
                Vector3 eye = new Vector3(-0.15f, 0.14f, 0f);
                for (int i = 0; i < 4; i++)
                {
                    var h = PropKit.Dir(i * 90f + 40f);
                    float len = PropKit.R(621, i, 0.3f, 0.48f), lean = PropKit.R(622, i, 0.12f, 0.3f);
                    int from = kit.VertexCount;
                    kit.Blade(s => eye + Vector3.up * (len * s) + h * (lean * len * s * s), s => 0.035f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(s * 0.9f + 0.08f)), 8, 0.006f, 0.6f, SHOOT);
                    kit.Paint(from, (p, uv) => new Color(uv.y, uv.y * 0.5f, PropKit.H(623, i), 1f));
                }
                PropKit.Pipe(kit, t => eye + Vector3.up * (0.05f * t), (t, a) => 0.025f, 1, 7, SHOOT, SHOOT, -1);
                for (int i = 0; i < 3; i++)
                {
                    var d = PropKit.Dir(150f + i * 30f);
                    PropKit.Pipe(kit, t => new Vector3(-0.2f, 0.05f, 0f) + d * (0.12f * t) + Vector3.down * (0.06f * t * t), (t, a) => Mathf.Lerp(0.012f, 0.004f, t), 4, 5, HUSK, -1, HUSK);
                }
            }
            return PropKit.Save(Dir, $"Coconut_{Abc(v)}", kit, _m, 0);
        }
    }
}
