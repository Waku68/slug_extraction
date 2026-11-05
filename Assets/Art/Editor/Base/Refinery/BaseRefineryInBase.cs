using System;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 精製所を灰色の拠点（BaseLook、BaseGrayboxBuilder）の下段の西に置く（Nov 5 わくさん「動線とかビジュ考慮して拠点になじむよう配置」）。
    /// 作り方は精製所の一角（BaseRefineryLook）と同じ：太い木と丸い石、つぎはぎ、テクスチャなしの Slug/Toon。
    /// 臼は拠点の決まりのまま（わくさん「拠点の決まりに戻す」）：受け石の上に上下 2 枚の平たい石臼。天井の下の主軸（y 4.3）の曲がり角に
    /// 歯車箱を吊り、そこから縦の軸で上の石を回す。
    /// 置き方（動線は docs/discussions/base_layout_v2.md）：
    /// - 出るとき：地図の台 → 中段からの板の階段を下りる → 右手（西）の型の台で弾を取る → 踊り場 → トンネル。型の台は通り道の側に皿と弾、
    ///   壁の側に押し棒（通る人のじゃまをしない）。ミニアモレの束も皿の横
    /// - 帰るとき：トンネルの段を上がって踊り場に出ると、左（西）に臼と入れ口、その奥の横穴に塩の壺の棚とアモレの山が見える
    ///   （塩の白がまとまって見える。入れ口は壁の側に寄せ、通り道と臼の間をあける）
    /// - 横穴（220°）の中：奥に壺の棚 2 段、左（212°）にアモレの箱と束、右（227°）に床置きの束。臼の板の床は横穴の口までつなぐ
    /// </summary>
    public static partial class BaseRefineryBuilder
    {
        /// <summary>拠点の側から渡す、置き場所の数字。</summary>
        public struct RefinerySite
        {
            public Vector3 mill;            // 臼の足元（板の床の上）
            public float axleY;             // 主軸の高さ
            public Vector3 hopperDir;       // 入れ口を寄せる向き（壁の側、水平）
            public Vector3 bench; public float benchYaw;   // 型の台の足元と向き（台の長い辺が local x）
            public Vector3 shelf; public float shelfYaw;   // 壺の棚の足元（奥の壁ぎわ）と向き（local +z が部屋の中）
            public Vector3 amore; public float amoreYaw;
            public Vector3 looseBars; public float looseYaw;
            public Rect deck; public float deckY;          // 板の床（x, z）と上の面の高さ
            public Func<float, float, float> ground;       // 岩の床の高さ
            public Material wood, rock, mark, lamp, moss, water;   // 拠点の材質にそろえる（null の所は精製所の一角のまま）。mark は塩・壺・布など中身
        }

        public static void PlaceInBase(Transform parent, RefinerySite s)
        {
            if (_mats == null || _mats.Length < SUBS || _mats[0] == null) Materials();
            _root = parent;
            var saved = _mats;
            // Nov 5 わくさん「BaseLook の木の見た目にそろえて」→「そろえて」（石・壺・塩も）
            _mats = (Material[])saved.Clone();
            void Use(Material m, params int[] subs) { if (m != null) foreach (int i in subs) _mats[i] = m; }
            // → わくさん「小物は色戻して」：そろえるのは木組み（木）と石だけ。壺・塩・延べ棒・弾・布・縄・鉄・灯・葉は精製所の一角の色のまま
            Use(s.wood, W, WD);
            Use(s.rock, ST, GR, DK);
            try {
            float Foot(float x, float z) => s.deck.Contains(new Vector2(x, z)) ? s.deckY : s.ground(x, z);

            // 板の床（つぎはぎ）と、岩へ下りる短い柱
            Part("Base_RefineryDeck", m =>
            {
                Deck(m, s.deck.xMin, s.deck.xMax, s.deck.yMin, s.deck.yMax, s.deckY, 501, false);
                foreach (float x in new[] { s.deck.xMin + 0.15f, (s.deck.xMin + s.deck.xMax) * 0.5f, s.deck.xMax - 0.15f })
                    foreach (float z in new[] { s.deck.yMin + 0.15f, s.deck.yMax - 0.15f })
                    {
                        float g = s.ground(x, z) - 0.15f, top = s.deckY - 0.11f;
                        if (top - g > 0.05f) m.RoundBox(WD, new Vector3(x, (g + top) * 0.5f, z), new Vector3(0.16f, top - g, 0.16f), Q(0f, Rs(502, (int)(x * 10 + z), -4f, 4f), 0f), 0.04f, 2, 0.006f, 503);
                    }
            }, false, true, true);

            // 臼：受け石・下の石・上の石（目の穴）、目のまわりと溝の塩、前の注ぎ口と桶、縦の軸と歯車箱
            var c = s.mill;
            var east = new Vector3(0.8f, 0f, -0.6f);   // 注ぎ口と桶：型の台と踊り場の間（南東）
            Part("Base_Mill", m =>
            {
                m.Lathe(GR, c + Vector3.down * 0.35f, I, P(0f, 0f, 0.82f, 0f, 0.84f, 0.3f, 0.8f, 0.4f, 0.8f, 0.61f, 0.76f, 0.66f, 0.69f, 0.67f, 0.65f, 0.63f, 0f, 0.63f), 56, 0.02f, 511);   // 受け石は岩まで下ろす
                m.Lathe(GR, c + Vector3.up * 0.28f, I, P(0f, 0f, 0.58f, 0f, 0.6f, 0.03f, 0.6f, 0.15f, 0.57f, 0.18f, 0.05f, 0.18f), 48, 0.015f, 512);
                m.Lathe(GR, c + Vector3.up * 0.47f, I, P(0f, 0f, 0.54f, 0f, 0.56f, 0.03f, 0.56f, 0.23f, 0.52f, 0.26f, 0.13f, 0.26f, 0.1f, 0.22f, 0.1f, 0.1f, 0f, 0.1f), 48, 0.02f, 513);
                m.Lathe(IR, c + Vector3.up * 0.58f, I, P(0.565f, -0.035f, 0.58f, 0f, 0.565f, 0.035f), 48);
                m.Lathe(SA, c + Vector3.up * 0.73f, I, P(0.46f, 0f, 0.32f, 0.07f, 0.2f, 0.08f, 0.12f, 0f), 32, 0.15f, 514);
                m.Lathe(SA, c + Vector3.up * 0.28f, I, P(0.76f, 0f, 0.71f, 0.04f, 0.64f, 0.05f, 0.61f, 0f), 48, 0.2f, 515);
                // 注ぎ口は型の台の側（東）へ、桶で受ける
                float yaw = Mathf.Atan2(east.x, east.z) * Mathf.Rad2Deg;
                var spout = c + east * 0.92f + Vector3.up * 0.24f;
                m.RoundBox(W, spout, new Vector3(0.22f, 0.07f, 0.34f), Q(14f, yaw, 0f), 0.025f, 2);
                var tub = c + east * 1.22f;
                m.Lathe(W, tub, I, P(0f, 0f, 0.21f, 0f, 0.25f, 0.24f, 0.28f, 0.27f, 0.21f, 0.27f, 0.18f, 0.06f, 0f, 0.06f), 28, 0.02f, 516);
                foreach (float y in new[] { 0.05f, 0.18f }) m.Lathe(IR, tub + Vector3.up * y, I, P(0.226f + y * 0.11f, -0.02f, 0.236f + y * 0.11f, 0f, 0.226f + y * 0.11f, 0.02f), 28);
                m.Lathe(SA, tub + Vector3.up * 0.18f, I, P(0.22f, 0f, 0.13f, 0.07f, 0f, 0.09f), 20, 0.2f, 517);
                // 縦の軸と歯車箱（主軸の曲がり角に吊る）
                float boxY = s.axleY - c.y;
                m.Tube(WD, t => c + Vector3.up * Mathf.Lerp(0.66f, boxY - 0.2f, t), _ => 0.085f, 10, 12);
                foreach (float y in new[] { 1.0f, boxY - 0.45f }) m.Lathe(IR, c + Vector3.up * y, I, P(0.095f, -0.03f, 0.11f, 0f, 0.095f, 0.03f), 16);
                m.RoundBox(WD, c + Vector3.up * boxY, new Vector3(0.5f, 0.52f, 0.5f), I, 0.08f, 2, 0.01f, 518);
                foreach (float dy in new[] { -0.17f, 0.17f }) m.RoundBox(IR, c + Vector3.up * (boxY + dy), new Vector3(0.53f, 0.05f, 0.53f), I, 0.015f, 2);
                // 箱を受ける腕木（壁の側から）
                var wa = c + s.hopperDir * 1.3f + Vector3.up * (boxY - 0.7f); var wb = c + s.hopperDir * 0.22f + Vector3.up * (boxY - 0.12f);
                m.RoundBox(WD, (wa + wb) * 0.5f, new Vector3(0.14f, 0.14f, (wb - wa).magnitude), Quaternion.LookRotation(wb - wa), 0.04f, 2, 0.006f, 519);
            }, false, true, true);

            // 入れ口：壁の側に 2 本の柱で立て、樋で目へ
            var hc = c + s.hopperDir * 0.55f;
            var tan = Vector3.Cross(Vector3.up, s.hopperDir).normalized;
            Part("Base_Hopper", m =>
            {
                var rot = Quaternion.LookRotation(s.hopperDir);
                for (int k = 0; k < 5; k++)
                {
                    float w = 0.15f + 0.07f * k, y = 1.1f + k * 0.11f;
                    for (int sg = -1; sg <= 1; sg += 2)
                    {
                        m.RoundBox(W, hc + rot * new Vector3(0f, y, sg * w), new Vector3(2f * w + 0.12f, 0.11f, 0.08f), rot * Q(0f, Rs(k, sg + 2, -1.5f, 1.5f), 0f), 0.032f, 2, 0.004f, 520 + k * 3 + sg);
                        m.RoundBox(W, hc + rot * new Vector3(sg * w, y, 0f), new Vector3(0.08f, 0.11f, 2f * w + 0.12f), rot * Q(0f, Rs(k, sg + 4, -1.5f, 1.5f), 0f), 0.032f, 2, 0.004f, 540 + k * 5 + sg);
                    }
                }
                var a = hc + Vector3.up * 1.07f; var b = c + s.hopperDir * 0.12f + Vector3.up * 0.8f;
                m.RoundBox(W, (a + b) * 0.5f, new Vector3(0.18f, 0.05f, (b - a).magnitude + 0.06f), Quaternion.LookRotation(b - a), 0.02f, 2);
                foreach (float sg in new[] { -0.95f, 0.95f })
                {
                    var p = hc + tan * sg;
                    float g = Foot(p.x, p.z) - c.y;
                    m.RoundBox(WD, p + Vector3.up * ((g + 1.62f) * 0.5f), new Vector3(0.17f, 1.62f - g, 0.17f), Q(0f, Rs(550, (int)(sg * 10), -3f, 3f), 0f), 0.05f, 2, 0.008f, 550);
                }
                foreach (float y in new[] { 0.98f, 1.6f })
                    m.RoundBox(WD, hc + Vector3.up * y, new Vector3(0.14f, 0.13f, 2.05f), Quaternion.LookRotation(tan), 0.045f, 2, 0.008f, 551);
                for (int i = 0; i < 9; i++)
                    m.RoundBox(SA, hc + rot * new Vector3(Rs(i, 1, -0.28f, 0.28f), 1.58f + Rs(i, 2, 0f, 0.08f), Rs(i, 3, -0.28f, 0.28f)), Vector3.one * Rs(i, 4, 0.13f, 0.19f),
                               Q(Rs(i, 5, 0f, 90f), Rs(i, 6, 0f, 90f), Rs(i, 7, 0f, 90f)), 0.045f, 3, 0.02f, 560 + i);
            }, false, true);

            // 型の台：通り道の側に弾の皿とミニアモレ、壁の側に押し棒
            Part("Base_MoldBench", m =>
            {
                var b = s.bench; var rot = Q(0f, s.benchYaw, 0f);
                float top = 0.55f;
                m.RoundBox(W, b + Vector3.up * (top - 0.06f), new Vector3(1.05f, 0.12f, 0.55f), rot, 0.04f, 2, 0.005f, 570);
                foreach (float x in new[] { -0.44f, 0.44f })
                    foreach (float z in new[] { -0.2f, 0.2f })
                        m.RoundBox(WD, b + rot * new Vector3(x, (top - 0.08f) * 0.5f, z), new Vector3(0.12f, top - 0.08f, 0.12f), rot, 0.035f, 2);
                m.RoundBox(WD, b + rot * new Vector3(0f, 0.12f, 0f), new Vector3(0.9f, 0.09f, 0.09f), rot, 0.03f, 2);
                var plate = b + rot * new Vector3(0.1f, top + 0.045f, 0.1f);
                m.RoundBox(GR, plate, new Vector3(0.4f, 0.09f, 0.3f), rot, 0.03f, 3, 0.003f, 571);
                for (int i = 0; i < 6; i++)
                    m.Lathe(SA, plate + rot * new Vector3(-0.11f + 0.11f * (i % 3), 0.04f, -0.07f + 0.14f * (i / 3)), I, P(0f, 0f, 0.038f, 0f, 0.042f, 0.03f, 0.03f, 0.055f, 0f, 0.062f), 18);
                var piv = b + rot * new Vector3(0.1f, top, 0.23f);
                m.RoundBox(WD, piv + Vector3.up * 0.25f, new Vector3(0.11f, 0.5f, 0.11f), rot, 0.035f, 2);
                var la = piv + Vector3.up * 0.48f; var lb = b + rot * new Vector3(-0.4f, top + 0.52f, 0.1f);
                m.Tube(W, t => Vector3.Lerp(la, lb, t) + Vector3.up * (0.03f * Mathf.Sin(Mathf.PI * t)), _ => 0.042f, 6, 12);
                m.Stick(WD, plate + Vector3.up * 0.14f, plate + Vector3.up * 0.44f, 0.05f, 12);
                var tray = b + rot * new Vector3(-0.28f, top, -0.1f);
                m.Lathe(W, tray, I, P(0f, 0f, 0.17f, 0f, 0.21f, 0.06f, 0.18f, 0.07f, 0.15f, 0.025f, 0f, 0.025f), 32, 0.02f, 572);
                for (int i = 0; i < 7; i++)
                    m.Lathe(SA, tray + new Vector3(Rs(573, i, -0.09f, 0.09f), 0.06f, Rs(574, i, -0.09f, 0.09f)), Q(Rs(575, i, 70f, 90f), Rs(576, i, 0f, 180f), 0f),
                            P(0f, -0.035f, 0.038f, -0.035f, 0.042f, 0f, 0.03f, 0.03f, 0f, 0.04f), 18);
                var mini = b + rot * new Vector3(0.38f, top, -0.14f);
                for (int i = 0; i < 7; i++)
                    m.Lathe(SA, mini + Vector3.up * (0.019f * i), Q(0f, i * 23f, 0f), P(0.016f, 0f, 0.042f, 0f, 0.046f, 0.009f, 0.042f, 0.018f, 0.016f, 0.018f, 0.016f, 0f), 24, 0.04f, i);
                Lantern(m, b + rot * new Vector3(-0.46f, top + 0.19f, 0.16f), s.benchYaw);
            }, false, true);

            // 横穴の壺の棚（2 段）と床の大きい壺
            Part("Base_SaltJars", m =>
            {
                var rot = Q(0f, s.shelfYaw, 0f);
                Vector3 L(float x, float y, float z) => s.shelf + rot * new Vector3(x, y, z);
                foreach (float x in new[] { -0.82f, 0f, 0.82f }) m.RoundBox(WD, L(x, 0.6f, 0.05f), new Vector3(0.1f, 1.2f, 0.1f), rot, 0.03f, 2, 0.005f, 580);
                float[] ys = { 0.42f, 0.92f };
                int[] counts = { 4, 3 };
                for (int sh = 0; sh < 2; sh++)
                {
                    m.RoundBox(W, L(0f, ys[sh] - 0.05f, 0.2f), new Vector3(1.8f, 0.1f, 0.44f), rot * Q(0f, 0f, Rs(581, sh, -0.8f, 0.8f)), 0.035f, 2, 0.005f, 581 + sh);
                    for (int j = 0; j < counts[sh]; j++)
                    {
                        int id = 600 + sh * 10 + j;
                        float x = Mathf.Lerp(-0.6f, 0.6f, counts[sh] == 1 ? 0.5f : j / (counts[sh] - 1f)) * (sh == 1 ? 0.7f : 1f);
                        Jar(m, L(x, ys[sh], 0.22f), Rs(id, 1, 0.13f, 0.15f), Rs(id, 2, 0.3f, 0.34f), id, Hs(id, 3) < 0.4f);
                    }
                }
                Jar(m, L(-1.05f, 0f, 0.45f), 0.19f, 0.46f, 620, false);
                Jar(m, L(1.0f, 0f, 0.5f), 0.16f, 0.4f, 621, true);
            }, false, true);

            // アモレ：箱の上に束 6、そばに床置きの束 4 とばらの延べ棒
            Part("Base_Amore", m =>
            {
                var rot = Q(0f, s.amoreYaw, 0f);
                Crate(m, s.amore, new Vector3(0.78f, 0.5f, 0.6f), 630, false);
                for (int k = 0; k < 6; k++)
                    Bundle(m, s.amore + rot * new Vector3(-0.25f + 0.25f * (k % 3), 0.54f + 0.135f * (k / 3), 0f), s.amoreYaw + 90f + Rs(631, k, -4f, 4f), 640 + k);
                var lr = Q(0f, s.looseYaw, 0f);
                for (int k = 0; k < 4; k++)
                    Bundle(m, s.looseBars + lr * new Vector3(0f, 0.135f * (k / 2), -0.14f + 0.28f * (k % 2)), s.looseYaw + Rs(632, k, -5f, 5f), 650 + k);
                m.RoundBox(SA, s.looseBars + lr * new Vector3(0.4f, 0.03f, 0.1f), new Vector3(0.085f, 0.06f, 0.24f), lr * Q(0f, 35f, 0f), 0.022f, 3, 0.003f, 660);
            }, false, true);
            }
            finally { _mats = saved; }
        }
    }
}
