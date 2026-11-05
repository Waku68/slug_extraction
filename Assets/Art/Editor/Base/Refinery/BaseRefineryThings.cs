using System;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 精製所の一角 その 2：臼と羽根車、壺、アモレ、型の台、工房（道具の壁）、滝、干し草、つた、手前の大岩と葉、灯（Nov 5 作り直し）。
    /// 参考の絵に合わせた所：
    /// - 臼（8）：床の受け石（直径 1.8 m）に、横の太い軸（直径 0.24 m）の先の縦の石の車輪（直径 1.1 m）が乗る。軸の左の受け台の上に入れ口、
    ///   塩が受け石の左に山になる。軸の真ん中の歯車箱へ、奥の羽根車の柱から横の軸が来る
    /// - 羽根車：奥の柱（太さ 0.3 m、床から天井まで）の上の方（y 3.85）に、板の羽根 10 枚と外の輪（直径 1.8 m）
    /// - 工房（右奥のアーチ）：奥の板にピッケル 2・ライフル 2・苔玉 2、作業台に小さな壺と鉢、吊った灯、手前に緑の壺
    /// - 滝（11）：右の壁の穴（y 2.2〜2.75）から石の受け台（高さ 1.2）へ落ち、前のふちから床の水路へ
    /// - 干し草（10）：羽根車の柱から右と左へ 2 本、赤・橙・黄土・緑の房が大きく
    /// - つた（9）：岩棚の上と前、アーチの上、天窓のふち、3 階のふち、滝の穴に葉のかたまり。垂れる茎は太く
    /// - 手前（3・4）：左に大岩と大きな葉、右に葉。床に延べ棒の束・木箱・塩の塊の箱
    /// </summary>
    public static partial class BaseRefineryBuilder
    {
        static readonly Vector3 MillC = new Vector3(-0.15f, 0f, 0.85f);
        static readonly Vector3 ColC = new Vector3(0.2f, 0f, 2.0f);
        const float AxleY = 0.87f;
        static readonly Quaternion I = Quaternion.identity;

        // ───────── 臼 ─────────

        static void Mill()
        {
            var c = MillC;
            Part("Mill", m =>
            {
                m.Lathe(GR, c, I, P(0f, 0f, 0.86f, 0f, 0.9f, 0.05f, 0.9f, 0.28f, 0.86f, 0.33f, 0.78f, 0.35f, 0.74f, 0.31f, 0f, 0.31f), 56, 0.02f, 1);
                // 塩の山（入れ口の下）と、散った塊
                m.Lathe(SA, c + new Vector3(-0.42f, 0.31f, 0f), I, P(0.38f, 0f, 0.26f, 0.1f, 0.12f, 0.18f, 0f, 0.2f), 28, 0.2f, 2);
                for (int i = 0; i < 6; i++)
                    m.RoundBox(SA, c + new Vector3(Rs(501, i, -0.75f, 0.1f), 0.36f, Rs(502, i, -0.55f, 0.55f)), Vector3.one * Rs(503, i, 0.07f, 0.12f), Q(Rs(504, i, 0f, 90f), Rs(505, i, 0f, 90f), 0f), 0.03f, 3, 0.015f, 510 + i);
                // 縦の石の車輪（軸は x）
                var wc = new Vector3(0.45f, AxleY, c.z);
                var ax = Q(0f, 0f, 90f);
                m.Lathe(GR, wc, ax, P(0.13f, -0.12f, 0.5f, -0.12f, 0.55f, -0.07f, 0.55f, 0.07f, 0.5f, 0.12f, 0.13f, 0.12f, 0.13f, -0.12f), 48, 0.015f, 3);
                // 横の軸と鉄の輪
                m.Tube(WD, t => Vector3.Lerp(new Vector3(-1.5f, AxleY, c.z), new Vector3(0.75f, AxleY, c.z), t), _ => 0.12f, 18, 14);
                foreach (float x in new[] { -1.12f, -0.55f, 0.36f, 0.6f, 0.72f })
                    m.Lathe(IR, new Vector3(x, AxleY, c.z), ax, P(0.125f, -0.035f, 0.145f, -0.02f, 0.145f, 0.02f, 0.125f, 0.035f), 18);
                // 受け台（柱と箱、鉄の帯）
                Post(m, new Vector3(-1.5f, 0f, c.z), AxleY - 0.2f, 0.26f, 520, false);
                m.RoundBox(WD, new Vector3(-1.5f, AxleY, c.z), new Vector3(0.44f, 0.42f, 0.44f), I, 0.08f, 2, 0.01f, 521);
                foreach (float dy in new[] { -0.13f, 0.13f }) m.RoundBox(IR, new Vector3(-1.5f, AxleY + dy, c.z), new Vector3(0.47f, 0.05f, 0.47f), I, 0.015f, 2);
                // 軸の歯車箱と、羽根車の柱からの横の軸
                m.RoundBox(WD, new Vector3(0.15f, AxleY, c.z), new Vector3(0.34f, 0.46f, 0.46f), I, 0.07f, 2, 0.008f, 522);
                foreach (float x in new[] { 0.04f, 0.26f }) m.RoundBox(IR, new Vector3(x, AxleY, c.z), new Vector3(0.05f, 0.49f, 0.49f), I, 0.015f, 2);
                m.Tube(WD, t => Vector3.Lerp(new Vector3(0.2f, AxleY, ColC.z - 0.25f), new Vector3(0.2f, AxleY, c.z + 0.23f), t), _ => 0.09f, 6, 12);
                m.Lathe(IR, new Vector3(0.2f, AxleY, 1.4f), Q(90f, 0f, 0f), P(0.1f, -0.03f, 0.12f, 0f, 0.1f, 0.03f), 16);

                // 入れ口：板を井桁に 5 段（上 0.9 m 角）。受け台の上に乗せ、樋で塩の山へ
                var hc = new Vector3(-1.2f, 0f, c.z);
                for (int k = 0; k < 5; k++)
                {
                    float w = 0.15f + 0.07f * k, y = 1.15f + k * 0.11f;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        m.RoundBox(W, hc + new Vector3(0f, y, s * w), new Vector3(2f * w + 0.12f, 0.11f, 0.08f), Q(0f, Rs(k, s + 2, -1.5f, 1.5f), 0f), 0.032f, 2, 0.004f, k * 3 + s);
                        m.RoundBox(W, hc + new Vector3(s * w, y, 0f), new Vector3(0.08f, 0.11f, 2f * w + 0.12f), Q(0f, Rs(k, s + 4, -1.5f, 1.5f), 0f), 0.032f, 2, 0.004f, k * 5 + s);
                    }
                }
                var a = hc + new Vector3(0.1f, 1.12f, 0f); var b = c + new Vector3(-0.5f, 0.62f, 0f);
                m.RoundBox(W, (a + b) * 0.5f, new Vector3(0.18f, 0.05f, (b - a).magnitude + 0.06f), Quaternion.LookRotation(b - a), 0.02f, 2);
                foreach (float s in new[] { -1f, 1f })
                {
                    var p0 = new Vector3(-1.5f, 0.55f, c.z + s * 0.2f); var p1 = hc + new Vector3(-0.05f, 1.1f, s * 0.28f);
                    m.RoundBox(WD, (p0 + p1) * 0.5f, new Vector3(0.1f, 0.1f, (p1 - p0).magnitude), Quaternion.LookRotation(p1 - p0), 0.03f, 2);
                }
                for (int i = 0; i < 9; i++)
                    m.RoundBox(SA, hc + new Vector3(Rs(i, 1, -0.3f, 0.3f), 1.6f + Rs(i, 2, 0f, 0.08f), Rs(i, 3, -0.3f, 0.3f)), Vector3.one * Rs(i, 4, 0.13f, 0.19f),
                               Q(Rs(i, 5, 0f, 90f), Rs(i, 6, 0f, 90f), Rs(i, 7, 0f, 90f)), 0.045f, 3, 0.02f, i + 80);
            }, false, true, true);
        }

        static void Turbine()
        {
            Part("Turbine", m =>
            {
                m.RoundBox(WD, ColC + Vector3.up * 0.42f, new Vector3(0.6f, 0.84f, 0.6f), I, 0.1f, 2, 0.01f, 601);
                foreach (float y in new[] { 0.2f, 0.65f }) m.RoundBox(IR, ColC + Vector3.up * y, new Vector3(0.63f, 0.06f, 0.63f), I, 0.02f, 2);
                Post(m, ColC + Vector3.up * 0.84f, CeilY(ColC.z) - 0.84f, 0.3f, 602, false);
                var hub = ColC + Vector3.up * 3.85f;
                m.Lathe(WD, hub + Vector3.down * 0.18f, I, P(0f, 0f, 0.25f, 0f, 0.27f, 0.05f, 0.27f, 0.31f, 0.25f, 0.36f, 0f, 0.36f), 20, 0.01f, 603);
                for (int i = 0; i < 10; i++)
                {
                    var d = PropKit.Dir(i * 36f);
                    m.RoundBox(W, hub + d * 0.6f, new Vector3(0.3f, 0.045f, 0.66f), Quaternion.LookRotation(d) * Q(0f, 0f, 28f), 0.02f, 2, 0.004f, 610 + i);
                    m.Stick(WD, hub + d * 0.26f, hub + d * 0.9f, 0.035f, 8);
                }
                m.Tube(W, t => hub + PropKit.Dir(t * 360f) * 0.92f, _ => 0.045f, 56, 8, false);
                foreach (float y in new[] { -0.4f, 0.4f }) m.RoundBox(IR, hub + Vector3.up * y, new Vector3(0.33f, 0.05f, 0.33f), I, 0.015f, 2);
            }, false, true, true);
        }

        // ───────── 壺 ─────────

        /// <summary>素焼きの壺：足元 p、胴の半径 r、高さ h。cloth なら口に布をかぶせて縄で縛る。</summary>
        static void Jar(BaseMesh m, Vector3 p, float r, float h, int seed, bool cloth, int mat = CL)
        {
            float nr = r * 0.55f;
            m.Lathe(mat, p, Q(0f, seed * 37f, 0f), P(0f, 0f, r * 0.6f, 0f, r * 0.9f, h * 0.15f, r, h * 0.45f, r * 0.85f, h * 0.75f, nr, h * 0.88f, nr, h * 0.95f,
                                                     nr * 1.32f, h * 0.96f, nr * 1.3f, h, nr * 0.85f, h, nr * 0.8f, h * 0.9f, 0f, h * 0.9f), 40, 0.035f, seed);
            if (!cloth) return;
            m.Lathe(CLOTH, p + Vector3.up * (h * 0.86f), Q(0f, seed * 11f, 0f), P(nr * 1.5f, 0f, nr * 1.4f, h * 0.12f, nr * 0.95f, h * 0.2f, 0f, h * 0.23f), 32, 0.12f, seed + 3);
            m.Lathe(RP, p + Vector3.up * (h * 0.93f), I, P(nr * 1.36f, -0.022f, nr * 1.42f, 0f, nr * 1.36f, 0.022f), 32);
        }

        static void Jars()
        {
            Part("Jars", m =>
            {
                // 横穴の棚 3 段
                var a = JarArch;
                float hw = a.w * 0.5f - 0.04f;
                float[] ys = { 0.38f, 0.86f, 1.34f };
                int[] counts = { 4, 4, 2 };
                for (int s = 0; s < 3; s++)
                {
                    float y = ys[s];
                    m.RoundBox(W, a.c + new Vector3(0f, y - 0.05f, 0.24f), new Vector3(hw * 2f, 0.1f, 0.48f), Q(0f, 0f, Rs(s, 1, -0.8f, 0.8f)), 0.035f, 2, 0.005f, s + 90);
                    foreach (float x in new[] { -hw + 0.05f, hw - 0.05f })
                        m.RoundBox(WD, a.c + new Vector3(x, y - 0.17f, 0.02f), new Vector3(0.1f, 0.14f, 0.1f), I, 0.03f, 2);
                    for (int j = 0; j < counts[s]; j++)
                    {
                        int id = s * 10 + j;
                        float span = counts[s] == 2 ? 0.25f : hw - 0.2f;
                        float x = Mathf.Lerp(-span, span, counts[s] == 1 ? 0.5f : j / (counts[s] - 1f)) + Rs(id, 2, -0.03f, 0.03f);
                        Jar(m, a.c + new Vector3(x, y, 0.25f + Rs(id, 3, -0.04f, 0.04f)), Rs(id, 4, 0.13f, 0.16f), Rs(id, 5, 0.3f, 0.36f), id, Hs(id, 6) < 0.35f);
                    }
                }
                Jar(m, new Vector3(-2.2f, 0f, 2.55f), 0.2f, 0.5f, 77, false);
                Jar(m, new Vector3(-0.62f, 0f, 2.62f), 0.17f, 0.42f, 78, true);
                // 2 階の壺の棚（左奥）
                var rk = new Vector3(-1.6f, GalY, 2.88f);
                foreach (float x in new[] { -0.62f, 0.62f }) m.RoundBox(WD, rk + new Vector3(x, 0.48f, 0f), new Vector3(0.1f, 0.96f, 0.1f), I, 0.03f, 2, 0.005f, 95);
                foreach (float y in new[] { 0.32f, 0.72f })
                {
                    m.RoundBox(W, rk + new Vector3(0f, y, 0f), new Vector3(1.36f, 0.08f, 0.34f), Q(0f, 0f, Rs((int)(y * 10), 1, -1f, 1f)), 0.03f, 2, 0.004f, 96);
                    for (int j = 0; j < 3; j++)
                        Jar(m, rk + new Vector3(-0.38f + 0.38f * j, y + 0.04f, 0f), Rs(j, (int)(y * 10), 0.1f, 0.12f), Rs(j, (int)(y * 10) + 1, 0.24f, 0.28f), 40 + j + (int)(y * 10), j == 1);
                }
            }, false, true);
        }

        // ───────── アモレ・木箱 ─────────

        /// <summary>束：延べ棒 10 本（0.085×0.06×0.24 m を 5 本×2 段）に縄 2 本。p は底の真ん中。</summary>
        static void Bundle(BaseMesh m, Vector3 p, float yaw, int seed)
        {
            var rot = Q(0f, yaw, 0f);
            for (int layer = 0; layer < 2; layer++)
                for (int k = 0; k < 5; k++)
                    m.RoundBox(SA, p + rot * new Vector3(-0.18f + 0.09f * k, 0.03f + 0.062f * layer, 0f), new Vector3(0.085f, 0.06f, 0.24f), rot * Q(0f, Rs(seed * 10 + k, layer, -2f, 2f), 0f), 0.022f, 3, 0.003f, seed * 10 + k);
            foreach (float z in new[] { -0.065f, 0.065f })
                m.RoundBox(RP, p + rot * new Vector3(0f, 0.062f, z), new Vector3(0.47f, 0.135f, 0.035f), rot, 0.017f, 3);
        }

        /// <summary>板の木箱：四隅の角材、横の板 3 段、底。open ならふたがずれて中に塩の塊。</summary>
        static void Crate(BaseMesh m, Vector3 p, Vector3 s, int seed, bool open)
        {
            var rot = Q(0f, Rs(seed, 1, -8f, 8f), 0f);
            Vector3 L(float x, float y, float z) => p + rot * new Vector3(x, y, z);
            float hx = s.x * 0.5f, hz = s.z * 0.5f;
            foreach (float x in new[] { -hx + 0.045f, hx - 0.045f })
                foreach (float z in new[] { -hz + 0.045f, hz - 0.045f })
                    m.RoundBox(WD, L(x, s.y * 0.5f, z), new Vector3(0.09f, s.y, 0.09f), rot, 0.03f, 2);
            for (int k = 0; k < 3; k++)
            {
                float y = 0.07f + k * (s.y - 0.1f) / 2.2f;
                m.RoundBox(W, L(0f, y, -hz), new Vector3(s.x - 0.02f, s.y * 0.26f, 0.045f), rot, 0.018f, 2, 0.003f, seed + k);
                m.RoundBox(W, L(0f, y, hz), new Vector3(s.x - 0.02f, s.y * 0.26f, 0.045f), rot, 0.018f, 2, 0.003f, seed + k + 3);
                m.RoundBox(W, L(-hx, y, 0f), new Vector3(0.045f, s.y * 0.26f, s.z - 0.02f), rot, 0.018f, 2, 0.003f, seed + k + 6);
                m.RoundBox(W, L(hx, y, 0f), new Vector3(0.045f, s.y * 0.26f, s.z - 0.02f), rot, 0.018f, 2, 0.003f, seed + k + 9);
            }
            m.RoundBox(W, L(0f, 0.02f, 0f), new Vector3(s.x, 0.04f, s.z), rot, 0.015f, 1);
            if (open)
            {
                if (s.y > 0.3f) m.RoundBox(W, L(0.14f, s.y + 0.03f, -0.1f), new Vector3(s.x, 0.04f, s.z * 0.65f), rot * Q(0f, 14f, 3f), 0.015f, 2, 0.003f, seed + 20);   // 低い箱はふたなし
                for (int i = 0; i < 6; i++)
                    m.RoundBox(SA, L(Rs(seed, 30 + i, -hx + 0.12f, hx - 0.12f), s.y - 0.03f, Rs(seed, 40 + i, -hz + 0.12f, hz - 0.12f)), Vector3.one * Rs(seed, 50 + i, 0.12f, 0.17f),
                               Q(Rs(seed, 60 + i, 0f, 90f), Rs(seed, 70 + i, 0f, 90f), 0f), 0.04f, 3, 0.02f, seed + i);
            }
            else m.RoundBox(W, L(0f, s.y + 0.02f, 0f), new Vector3(s.x + 0.02f, 0.04f, s.z + 0.02f), rot, 0.015f, 2);
        }

        static void Amore()
        {
            Part("Amore", m =>
            {
                // 左の木箱の上に束 6（3 本 × 2 段）
                var c1 = new Vector3(-1.6f, 0f, -0.1f);
                Crate(m, c1, new Vector3(0.78f, 0.5f, 0.6f), 11, false);
                for (int k = 0; k < 6; k++)
                    Bundle(m, c1 + new Vector3(-0.25f + 0.25f * (k % 3), 0.54f + 0.135f * (k / 3), Rs(k, 2, -0.03f, 0.03f)), 90f + Rs(k, 3, -4f, 4f), k);
                // 奥の木箱（ふたがずれて塩の塊）
                Crate(m, new Vector3(-1.95f, 0f, 0.6f), new Vector3(0.6f, 0.42f, 0.5f), 12, true);
                // 床に積んだ束 4 と、ばらの延べ棒
                for (int k = 0; k < 4; k++)
                    Bundle(m, new Vector3(-0.95f, 0.135f * (k / 2), -0.69f + 0.27f * (k % 2)), 12f + Rs(k, 4, -5f, 5f), 20 + k);
                m.RoundBox(SA, new Vector3(-0.55f, 0.03f, -1.0f), new Vector3(0.085f, 0.06f, 0.24f), Q(0f, 35f, 0f), 0.022f, 3, 0.003f, 41);
                m.RoundBox(SA, new Vector3(-0.42f, 0.03f, -1.08f), new Vector3(0.085f, 0.06f, 0.24f), Q(0f, -20f, 0f), 0.022f, 3, 0.003f, 42);
                // 手前まん中の、塩の塊の箱
                Crate(m, new Vector3(-0.35f, 0f, -1.0f), new Vector3(0.9f, 0.24f, 0.45f), 13, true);
            }, false, true);
        }

        // ───────── 型の台 ─────────

        static void MoldBench()
        {
            var b = new Vector3(1.3f, 0f, 0.3f);
            Part("MoldBench", m =>
            {
                float top = 0.55f;
                var rot = Q(0f, -8f, 0f);
                m.RoundBox(W, b + Vector3.up * (top - 0.06f), new Vector3(1.05f, 0.12f, 0.55f), rot, 0.04f, 2, 0.005f, 1);
                foreach (float x in new[] { -0.44f, 0.44f })
                    foreach (float z in new[] { -0.2f, 0.2f })
                        m.RoundBox(WD, b + rot * new Vector3(x, (top - 0.08f) * 0.5f, z), new Vector3(0.12f, top - 0.08f, 0.12f), rot, 0.035f, 2);
                m.RoundBox(WD, b + rot * new Vector3(0f, 0.12f, 0f), new Vector3(0.9f, 0.09f, 0.09f), rot, 0.03f, 2);
                var plate = b + rot * new Vector3(-0.2f, top + 0.045f, 0.02f);
                m.RoundBox(GR, plate, new Vector3(0.4f, 0.09f, 0.3f), rot, 0.03f, 3, 0.003f, 2);
                for (int i = 0; i < 6; i++)
                    m.Lathe(SA, plate + rot * new Vector3(-0.11f + 0.11f * (i % 3), 0.04f, -0.07f + 0.14f * (i / 3)), I, P(0f, 0f, 0.038f, 0f, 0.042f, 0.03f, 0.03f, 0.055f, 0f, 0.062f), 18);
                var piv = b + rot * new Vector3(-0.2f, top, 0.22f);
                m.RoundBox(WD, piv + Vector3.up * 0.25f, new Vector3(0.11f, 0.5f, 0.11f), rot, 0.035f, 2);
                var la = piv + Vector3.up * 0.48f; var lb = b + rot * new Vector3(0.35f, top + 0.5f, -0.1f);
                m.Tube(W, t => Vector3.Lerp(la, lb, t) + Vector3.up * (0.03f * Mathf.Sin(Mathf.PI * t)), _ => 0.042f, 6, 12);
                m.Stick(WD, plate + Vector3.up * 0.14f, plate + Vector3.up * 0.44f, 0.05f, 12);
                var tray = b + rot * new Vector3(0.24f, top, -0.05f);
                m.Lathe(W, tray, I, P(0f, 0f, 0.17f, 0f, 0.21f, 0.06f, 0.18f, 0.07f, 0.15f, 0.025f, 0f, 0.025f), 32, 0.02f, 3);
                for (int i = 0; i < 7; i++)
                    m.Lathe(SA, tray + new Vector3(Rs(i, 1, -0.09f, 0.09f), 0.06f, Rs(i, 2, -0.09f, 0.09f)), Q(Rs(i, 3, 70f, 90f), Rs(i, 4, 0f, 180f), 0f),
                            P(0f, -0.035f, 0.038f, -0.035f, 0.042f, 0f, 0.03f, 0.03f, 0f, 0.04f), 18);
                var mini = b + rot * new Vector3(0.42f, top, 0.15f);
                for (int i = 0; i < 7; i++)
                    m.Lathe(SA, mini + Vector3.up * (0.019f * i), Q(0f, i * 23f, 0f), P(0.016f, 0f, 0.042f, 0f, 0.046f, 0.009f, 0.042f, 0.018f, 0.016f, 0.018f, 0.016f, 0f), 24, 0.04f, i);
            }, false, true);
        }

        // ───────── 工房（右奥のアーチ） ─────────

        static void Workshop()
        {
            var a = ShopArch;
            float back = ZB + a.depth;
            Part("Workshop", m =>
            {
                m.RoundBox(ST, a.c + new Vector3(0f, -0.05f, a.depth * 0.5f), new Vector3(a.w + 0.1f, 0.1f, a.depth + 0.1f), I, 0.03f, 1, 0.01f, 700);
                // 奥の道具の板
                var board = new Vector3(a.c.x, 1.25f, back - 0.08f);
                m.RoundBox(WD, board, new Vector3(1.6f, 0.8f, 0.06f), I, 0.025f, 2, 0.004f, 701);
                for (int i = 0; i < 6; i++)
                    m.Stick(WD, board + new Vector3(-0.68f + 0.27f * i, 0.32f, 0f), board + new Vector3(-0.68f + 0.27f * i, 0.32f, -0.12f), 0.02f, 6);
                // ピッケル 2
                foreach (float x in new[] { -0.6f, -0.33f })
                {
                    var top = board + new Vector3(x, 0.3f, -0.1f);
                    m.Tube(W, t => Vector3.Lerp(top, top + new Vector3(0.03f, -0.72f, 0f), t), _ => 0.03f, 4, 10);
                    foreach (float s in new[] { -1f, 1f })
                        m.RoundBox(IR, top + new Vector3(s * 0.12f, -0.02f, 0f), new Vector3(0.26f, 0.06f, 0.055f), Q(0f, 0f, -s * 14f), 0.02f, 3);
                }
                // 苔玉 2（縄で吊る）
                foreach (float x in new[] { -0.05f, 0.17f })
                {
                    var peg = board + new Vector3(x, 0.32f, -0.12f);
                    m.Stick(RP, peg, peg + Vector3.down * 0.22f, 0.012f, 6);
                    m.Lathe(LF, peg + Vector3.down * 0.42f, I, P(0f, 0f, 0.07f, 0.02f, 0.1f, 0.1f, 0.07f, 0.18f, 0f, 0.2f), 18, 0.12f, (int)(x * 100) + 5);
                }
                // ライフル 2
                foreach (float y in new[] { 0.1f, -0.15f })
                {
                    var p = board + new Vector3(0.48f, y, -0.1f);
                    m.Tube(IR, t => p + new Vector3(Mathf.Lerp(-0.34f, 0.2f, t), 0f, 0f), _ => 0.022f, 3, 10);
                    m.RoundBox(WD, p + new Vector3(0.3f, -0.03f, 0f), new Vector3(0.28f, 0.1f, 0.05f), Q(0f, 0f, -8f), 0.025f, 3);
                    m.RoundBox(WD, p + new Vector3(0.05f, -0.01f, 0f), new Vector3(0.3f, 0.06f, 0.05f), I, 0.02f, 3);
                }
                // 作業台と小物
                float ty = 0.62f, bz = back - 0.3f;
                m.RoundBox(W, new Vector3(a.c.x, ty - 0.05f, bz), new Vector3(1.7f, 0.1f, 0.42f), I, 0.035f, 2, 0.005f, 710);
                foreach (float x in new[] { -0.75f, 0.75f })
                    foreach (float z in new[] { -0.15f, 0.15f })
                        m.RoundBox(WD, new Vector3(a.c.x + x, (ty - 0.1f) * 0.5f, bz + z), new Vector3(0.1f, ty - 0.1f, 0.1f), I, 0.03f, 2);
                m.RoundBox(W, new Vector3(a.c.x, 0.15f, bz), new Vector3(1.6f, 0.06f, 0.36f), I, 0.025f, 2, 0.004f, 711);
                for (int i = 0; i < 5; i++)
                {
                    float x = a.c.x - 0.65f + 0.3f * i + Rs(720, i, -0.05f, 0.05f);
                    if (i == 2) { m.Lathe(GR, new Vector3(x, ty, bz), I, P(0f, 0f, 0.1f, 0f, 0.12f, 0.08f, 0.09f, 0.1f, 0.07f, 0.03f, 0f, 0.03f), 20, 0.02f, 721); continue; }
                    Jar(m, new Vector3(x, ty, bz + Rs(722, i, -0.06f, 0.06f)), Rs(723, i, 0.06f, 0.08f), Rs(724, i, 0.15f, 0.22f), 730 + i, false, i % 2 == 0 ? GZ : CL);
                }
                for (int i = 0; i < 3; i++) Jar(m, new Vector3(a.c.x - 0.5f + 0.45f * i, 0.18f, bz), 0.08f, 0.18f, 740 + i, false);
                // 手前の緑の壺
                Jar(m, new Vector3(2.25f, 0f, 2.6f), 0.18f, 0.4f, 750, false, GZ);
            }, false, true);
        }

        // ───────── 滝と石の段 ─────────

        /// <summary>水の帯：中心線 path(t) に沿って幅 width、横の向き side、表の向き face。</summary>
        static void Ribbon(BaseMesh m, int sub, Func<float, Vector3> path, float width, Vector3 side, Vector3 face, int rows)
        {
            int start = m.Count;
            for (int k = 0; k <= rows; k++)
            {
                var p = path(k / (float)rows);
                m.Add(p - side * (width * 0.5f), face, new Vector2(0f, k));
                m.Add(p + side * (width * 0.5f), face, new Vector2(1f, k));
            }
            for (int k = 0; k < rows; k++) m.Quad(sub, start + k * 2, start + k * 2 + 1, start + k * 2 + 3, start + k * 2 + 2);
        }

        static void Waterfall()
        {
            Part("Waterfall", m =>
            {
                // 右の石の段（4 段、0.22 m ずつ）と台（y 0.88）
                for (int i = 0; i < 4; i++)
                {
                    float z0 = -0.95f + 0.35f * i, h = i < 3 ? 0.22f * (i + 1) : PlatY;
                    float z1 = i < 3 ? 0.45f : 1.6f;
                    if (i == 3) z0 = 0.45f;
                    foreach (var (x0, x1) in new[] { (PlankX1, 2.35f), (2.35f, 2.8f) })
                        m.RoundBox(ST, new Vector3((x0 + x1) * 0.5f, h * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0 - 0.02f, h, z1 - z0 - 0.02f),
                                   Q(0f, Rs(800, i * 2 + (int)x0, -2f, 2f), 0f), 0.08f, 2, 0.02f, 800 + i * 7 + (int)(x0 * 3));
                }
                // 石の受け台（上 y 1.2）
                m.RoundBox(ST, new Vector3(3.16f, 0.6f, 1.3f), new Vector3(0.54f, 1.2f, 1.05f), Q(0f, 2f, 0f), 0.1f, 3, 0.03f, 820);
                // 上の落ち水（壁の穴 → 受け台）と、受け台の水面、前のふちから床の水路へ
                Ribbon(m, WT, t => new Vector3(Mathf.Lerp(X1 + 0.15f, 3.12f, Mathf.Sqrt(t)), Mathf.Lerp(2.42f, 1.22f, t * t * 0.4f + t * 0.6f), 1.25f), 0.42f, Vector3.forward, Vector3.left, 10);
                m.RoundBox(WT, new Vector3(3.16f, 1.2f, 1.3f), new Vector3(0.44f, 0.03f, 0.95f), I, 0.012f, 1);
                Ribbon(m, WT, t => new Vector3(3.16f, Mathf.Lerp(1.22f, 0.0f, t * t * 0.5f + t * 0.5f), Mathf.Lerp(0.8f, 0.58f, Mathf.Sqrt(t))), 0.32f, Vector3.right, Vector3.back, 10);
                m.RoundBox(WT, new Vector3(3.05f, -0.03f, (ZF + 0.8f) * 0.5f), new Vector3(0.38f, 0.03f, 0.8f - ZF), I, 0.012f, 1);
                // しぶき
                foreach (var (c, n) in new[] { (new Vector3(3.12f, 1.24f, 1.25f), 5), (new Vector3(3.12f, 0.02f, 0.55f), 6) })
                    for (int i = 0; i < n; i++)
                        m.RoundBox(SA, c + new Vector3(Rs(830, i + n, -0.15f, 0.15f), Rs(831, i, 0f, 0.05f), Rs(832, i + n, -0.15f, 0.15f)), Vector3.one * Rs(833, i + n, 0.07f, 0.12f), I, 0.04f, 3, 0.02f, 840 + i + n);
                // 段の上の灯の柱
                m.RoundBox(WD, new Vector3(2.15f, (PlatY + 1.5f) * 0.5f, 1.35f), new Vector3(0.12f, 1.5f - PlatY, 0.12f), I, 0.035f, 2, 0.005f, 850);
            }, false, true, true);
        }

        // ───────── 干し草・つた・葉 ─────────

        /// <summary>干し草の房：縄の点 top から下へ、葉 7 枚を束ねる。色は mat。</summary>
        static void HerbBundle(BaseMesh m, Vector3 top, int mat, int seed)
        {
            m.Stick(RP, top, top + Vector3.down * 0.07f, 0.01f, 6);
            var c = top + Vector3.down * 0.08f;
            m.Lathe(RP, c, I, P(0.03f, -0.02f, 0.035f, 0f, 0.03f, 0.02f), 10);
            for (int i = 0; i < 7; i++)
            {
                float az = seed * 40f + i * 51f;
                var dir = (PropKit.Dir(az) * Rs(seed, i, 0.2f, 0.45f) + Vector3.down).normalized;
                var up = PropKit.Dir(az);
                float len = Rs(seed, i + 10, 0.2f, 0.28f);
                m.Leaf(mat, c, dir, up, len, len * 0.55f);
            }
        }

        static void Garlands()
        {
            Part("Garlands", m =>
            {
                int[] mats = { HR, HO, LF, HY, HR, LF, HO };
                foreach (var (a, b, seed) in new[] { (new Vector3(0.38f, 3.5f, 1.9f), new Vector3(X1 - 0.15f, 3.58f, 0.55f), 1), (new Vector3(0.0f, 3.42f, 1.9f), new Vector3(-2.5f, 3.55f, 1.05f), 2) })
                {
                    Func<float, Vector3> line = t => Vector3.Lerp(a, b, t) + Vector3.down * (0.32f * 4f * t * (1f - t));
                    m.Tube(RP, line, _ => 0.022f, 28, 8);
                    int n = Mathf.RoundToInt((b - a).magnitude / 0.27f);
                    for (int i = 1; i < n; i++) HerbBundle(m, line(i / (float)n), mats[(i + seed) % mats.Length], seed * 50 + i);
                }
            }, false, false);
        }

        /// <summary>垂れるつた：start から下へ len、葉は茎に沿って左右に。</summary>
        static void Vine(BaseMesh m, Vector3 start, Vector3 drift, float len, int seed)
        {
            Func<float, Vector3> path = t => start + Vector3.down * (len * t) + drift * (t * t) + new Vector3(0.05f * Mathf.Sin(t * 7f + seed), 0f, 0.04f * Mathf.Cos(t * 5f + seed));
            m.Tube(LF, path, t => Mathf.Lerp(0.022f, 0.012f, t), Mathf.Max(4, (int)(len * 10f)), 6, false);
            int leaves = Mathf.Max(3, (int)(len * 12f));
            for (int i = 0; i < leaves; i++)
            {
                float t = (i + 0.5f) / leaves;
                var dir = (PropKit.Dir(seed * 50f + i * 137.5f) * 0.8f + Vector3.down * 0.4f + Vector3.back * 0.5f).normalized;
                float s = Rs(seed, i, 0.11f, 0.16f) * Mathf.Lerp(1f, 0.75f, t);
                m.Leaf(LF, path(t), dir, (Vector3.back + Vector3.up * 0.6f).normalized, s, s * 0.9f);
            }
        }

        /// <summary>葉のかたまり：線 a→b のまわりに count 枚。face は葉の表の向き（外）、葉は下へ垂れぎみ。</summary>
        static void IvyLine(BaseMesh m, Vector3 a, Vector3 b, Vector3 face, int count, int seed, float spread = 0.14f, float drop = 0.22f)
        {
            face.Normalize();
            var along = (b - a).normalized;
            var across = Vector3.Cross(face, along).normalized;
            for (int i = 0; i < count; i++)
            {
                var p = Vector3.Lerp(a, b, Hs(seed, i)) + across * Rs(seed, i + 1000, -spread, spread) + Vector3.down * (drop * Mathf.Pow(Hs(seed, i + 2000), 2f));
                float ang = Rs(seed, i + 3000, -150f, -30f) * Mathf.Deg2Rad;
                var dir = (along * Mathf.Cos(ang) + Vector3.Cross(along, face).normalized * Mathf.Sin(ang) * -1f + Vector3.down * 0.3f).normalized;
                var up = (face + new Vector3(Rs(seed, i + 4000, -0.4f, 0.4f), Rs(seed, i + 5000, -0.2f, 0.5f), Rs(seed, i + 6000, -0.4f, 0.4f))).normalized;
                float s = Rs(seed, i + 7000, 0.13f, 0.2f);
                m.Leaf(LF, p, dir, up, s, s * 0.95f);
            }
        }

        static void IvyArc(BaseMesh m, ArchSpec a, int count, int seed)
        {
            float r = a.w * 0.5f + 0.36f;
            for (int k = 0; k < 6; k++)
            {
                float b0 = Mathf.PI * (0.12f + 0.13f * k), b1 = b0 + Mathf.PI * 0.13f;
                var p0 = a.c + new Vector3(Mathf.Cos(b0) * r, a.spring + Mathf.Sin(b0) * r, -0.3f);
                var p1 = a.c + new Vector3(Mathf.Cos(b1) * r, a.spring + Mathf.Sin(b1) * r, -0.3f);
                IvyLine(m, p0, p1, new Vector3(0f, 0.4f, -1f), count / 6, seed + k);
            }
        }

        static void Ivy()
        {
            Part("Ivy", m =>
            {
                // 岩棚の上と前、垂れるつた
                IvyLine(m, new Vector3(-2.4f, LedgeY1 + 0.02f, 2.28f), new Vector3(X1 - 0.1f, LedgeY1 + 0.02f, 2.28f), new Vector3(0f, 0.6f, -1f), 330, 901, 0.1f, 0.26f);
                for (int i = 0; i < 11; i++)
                {
                    float x = -2.3f + i * 0.55f + Rs(902, i, -0.15f, 0.15f);
                    Vine(m, new Vector3(x, LedgeY1 - 0.1f, 2.22f), new Vector3(Rs(903, i, -0.1f, 0.1f), 0f, -0.05f), Rs(904, i, 0.3f, 1.0f), 910 + i);
                }
                IvyArc(m, JarArch, 110, 920);
                IvyArc(m, ShopArch, 150, 930);
                // 天窓のふち
                var h = SkyHole;
                IvyLine(m, new Vector3(h.xMin, Ceil - 0.15f, h.yMin), new Vector3(h.xMax, Ceil - 0.15f, h.yMin), Vector3.down + Vector3.back, 70, 940);
                IvyLine(m, new Vector3(h.xMin, Ceil - 0.15f, h.yMin), new Vector3(h.xMin, Ceil - 0.15f, h.yMax), Vector3.down + Vector3.left, 55, 941);
                for (int i = 0; i < 5; i++)
                    Vine(m, new Vector3(Mathf.Lerp(h.xMin, h.xMax, i / 4f), Ceil - 0.1f, h.yMin + 0.05f), Vector3.zero, Rs(942, i, 0.5f, 1.2f), 945 + i);
                // 3 階のふち
                IvyLine(m, new Vector3(G3X, G3Y - 0.12f, -0.3f), new Vector3(G3X, G3Y - 0.12f, 2.1f), Vector3.left + Vector3.down * 0.3f, 90, 950, 0.08f, 0.25f);
                for (int i = 0; i < 4; i++)
                    Vine(m, new Vector3(G3X - 0.03f, G3Y - 0.2f, -0.1f + i * 0.6f), new Vector3(-0.05f, 0f, 0f), Rs(951, i, 0.4f, 1.1f), 955 + i);
                // 滝の穴と受け台
                IvyLine(m, new Vector3(X1 - 0.05f, 2.82f, 0.85f), new Vector3(X1 - 0.05f, 2.82f, 1.7f), Vector3.left + Vector3.up * 0.3f, 55, 960);
                IvyLine(m, new Vector3(X1 - 0.05f, 2.2f, 0.8f), new Vector3(X1 - 0.05f, 1.3f, 0.8f), Vector3.left + Vector3.back, 22, 961);
                IvyLine(m, new Vector3(2.9f, 1.22f, 0.78f), new Vector3(2.9f, 1.22f, 1.83f), Vector3.left + Vector3.up * 0.4f, 20, 962);
                // 2 階の手すりから
                for (int i = 0; i < 5; i++)
                    Vine(m, new Vector3(-2.0f + i * 0.9f, GalY - 0.05f, GalZ - 0.02f), new Vector3(0f, 0f, -0.05f), Rs(970, i, 0.25f, 0.6f), 975 + i);
            }, false, false);
        }

        /// <summary>大きな葉の株：足元 p から count 枚、長さ len。</summary>
        static void Plant(BaseMesh m, Vector3 p, int count, float len, int seed)
        {
            for (int i = 0; i < count; i++)
            {
                float az = seed * 23f + i * 360f / count + Rs(seed, i, -15f, 15f);
                float el = Rs(seed, i + 50, 20f, 55f) * Mathf.Deg2Rad;
                var dir = PropKit.Dir(az) * Mathf.Cos(el) + Vector3.up * Mathf.Sin(el);
                float l = len * Rs(seed, i + 100, 0.75f, 1.1f);
                var root = p + Vector3.up * 0.03f;
                m.Tube(LF, t => root + dir * (l * 0.25f * t), _ => 0.018f, 2, 6, false);
                m.Leaf(LF, root + dir * (l * 0.22f), dir, (Vector3.up - dir * 0.3f).normalized, l, l * 0.62f);
            }
        }

        static void Foreground()
        {
            Part("Foreground", m =>
            {
                m.RoundBox(ST, new Vector3(-1.62f, 0.45f, -1.65f), new Vector3(0.95f, 0.9f, 0.85f), Q(4f, 18f, -3f), 0.32f, 4, 0.06f, 1001);
                m.RoundBox(ST, new Vector3(-0.75f, 0.2f, -1.25f), new Vector3(0.5f, 0.42f, 0.45f), Q(0f, -12f, 5f), 0.18f, 3, 0.04f, 1002);
                IvyLine(m, new Vector3(-1.95f, 0.88f, -1.55f), new Vector3(-1.25f, 0.85f, -1.45f), new Vector3(0.2f, 1f, -0.5f), 40, 1003, 0.15f, 0.35f);
                Plant(m, new Vector3(-0.62f, 0f, -1.62f), 7, 0.55f, 1010);
                Plant(m, new Vector3(1.65f, 0f, -1.35f), 6, 0.5f, 1011);
                Plant(m, new Vector3(-2.3f, 0f, -0.2f), 5, 0.4f, 1012);
                Plant(m, new Vector3(2.5f, PlatY, 1.5f), 5, 0.35f, 1013);
            }, false, false);
        }

        // ───────── 灯 ─────────

        /// <summary>灯（火の位置 p）。hang があればその高さから吊る（鎖）、wall なら壁の腕木から。</summary>
        static readonly (Vector3 p, float hang)[] LampAt =
        {
            (new Vector3(-0.6f, 3.0f, BeamZ), BeamY - 0.2f),       // 手前の梁
            (new Vector3(1.3f, 3.0f, BeamZ), BeamY - 0.2f),
            (new Vector3(-1.6f, 3.6f, 1.4f), Ceil),                // 天井から
            (new Vector3(-2.62f, 1.55f, 2.7f), float.NaN),         // 壺の横穴の左の腕木
            (new Vector3(0.95f, 1.42f, 3.3f), 1.86f),              // 工房のアーチ（左寄り。道具の壁をふさがない）
            (new Vector3(2.15f, 1.72f, 1.35f), float.NaN),         // 段の上の柱
        };

        static void Lantern(BaseMesh m, Vector3 p, float yaw)
        {
            var rot = Q(0f, yaw, 0f);
            m.Lathe(IR, p + Vector3.down * 0.19f, rot * Q(0f, 45f, 0f), P(0f, 0f, 0.13f, 0f, 0.14f, 0.04f, 0.11f, 0.06f, 0f, 0.06f), 4, 0f);
            m.Lathe(GL, p + Vector3.down * 0.14f, rot, P(0f, 0f, 0.085f, 0f, 0.095f, 0.14f, 0.085f, 0.28f, 0f, 0.28f), 24);
            for (int i = 0; i < 4; i++)
            {
                var o = rot * (PropKit.Dir(45f + i * 90f) * 0.115f);
                m.Stick(IR, p + o + Vector3.down * 0.15f, p + o + Vector3.up * 0.15f, 0.022f, 8);
            }
            m.Lathe(IR, p + Vector3.up * 0.14f, rot * Q(0f, 45f, 0f), P(0f, 0f, 0.16f, 0f, 0.15f, 0.04f, 0.06f, 0.13f, 0.045f, 0.16f, 0f, 0.16f), 4, 0f);
            m.Tube(IR, t => p + Vector3.up * 0.34f + rot * new Vector3(Mathf.Cos(Mathf.PI * t) * 0.065f, Mathf.Sin(Mathf.PI * t) * 0.065f, 0f), _ => 0.016f, 10, 8);
        }

        static void Lanterns()
        {
            Part("Lanterns", m =>
            {
                for (int i = 0; i < LampAt.Length; i++)
                {
                    var (p, hang) = LampAt[i];
                    Lantern(m, p, i * 23f);
                    var top = p + Vector3.up * 0.4f;
                    if (!float.IsNaN(hang)) m.Tube(IR, t => Vector3.Lerp(top, new Vector3(top.x, hang, top.z), t), _ => 0.012f, 3, 6);
                }
                var w = LampAt[3].p;
                m.Tube(IR, t => new Vector3(w.x, w.y + 0.4f + 0.1f * Mathf.Sin(Mathf.PI * t * 0.5f), Mathf.Lerp(ZB - 0.12f, w.z, t)), _ => 0.02f, 8, 8);
                m.RoundBox(IR, new Vector3(w.x, w.y + 0.42f, ZB - 0.14f), new Vector3(0.12f, 0.22f, 0.05f), I, 0.015f, 2);
            }, false, true);
        }
    }
}
