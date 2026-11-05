using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 3階の縁側と飛びこみ台（Nov 4、docs/discussions/base_3f_walkway_ideas.md の「無難」）。
    /// - 縁側：3階の板の棚の北の端（156°）から広間の内側へ、池の真ん中に向けて張り出す板の床。長さ 2.4 m・幅 1.4 m（人なら 8.6 m × 5 m）、
    ///   高さは3階と同じ y 6.25（下段の床から 3.85 m、人なら 14 m）。真下は苗床なので柱は立てず、丸天井から縄で吊り、根元は壁へ斜めの梁。
    ///   手すりは低く 0.3 m（ふちに座って足をぶらぶら）。見た目だけで当たり判定はない
    /// - 飛びこみ台：縁側の先から同じ向きに細い板。長さ 2.3 m・幅 0.45 m（人なら 8.3 m × 1.6 m）、手すりなし。
    ///   先は池の真ん中から 0.4 m、水面（y 3.46）まで 2.8 m（人なら 10 m）。そこの池の深さは 0.37 m（ナメクジの目の高さより少し浅い）
    /// - 天井の梁 3 本は y 6.8 → 7.6 に上げた（縁側と飛びこみ台の上を 1.35 m あける。BeamY・BeamR）。吊るす灯の高さはそのまま
    /// </summary>
    public static partial class BaseGrayboxBuilder
    {
        const float EngawaDeg = 156f, EngawaLen = 2.4f, EngawaW = 1.4f, DiveLen = 2.3f, DiveW = 0.45f;
        const float BeamY = 7.6f, BeamR = 5.3f;

        /// <summary>広間の丸天井の高さ（真ん中からの距離 r）。</summary>
        static float DomeY(float r) => FloorY - 0.2f + WallH + (HallH - WallH) * Mathf.Sqrt(Mathf.Max(0f, 1f - r * r / (HallR * HallR)));

        /// <summary>縁側の根元（3階の棚の内側のふち）と、池へ向かう向き。</summary>
        static (Vector3 root, Vector3 dir) EngawaFrame()
        {
            var a = Polar(EngawaDeg, 4.6f, F3Y);
            return (a, new Vector3(SkyC.x - a.x, 0f, SkyC.y - a.z).normalized);
        }

        static void Engawa()
        {
            var g = Group("Engawa");
            var (a, d) = EngawaFrame();
            var side = Vector3.Cross(Vector3.up, d);
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            var root = a - d * 0.1f + Vector3.down * 0.01f;    // 3階の棚の下へ 0.1 m もぐらせる（1 cm 低くして面が重ならないように）
            var end = root + d * EngawaLen;

            Planks(g, "Engawa", root + d * (EngawaLen * 0.5f), yaw, EngawaLen, EngawaW, false, 0.12f);
            Planks(g, "DiveBoard", end + d * (DiveLen * 0.5f), yaw, DiveLen, DiveW, false, 0.05f);
            // 飛びこみ台の根太は縁側の下まで 1 m もどして押さえる（先はしなる）
            foreach (float s in new[] { -0.17f, 0.17f })
                Rod(g, "DiveJoist", end - d * 1.0f + side * s + Vector3.down * 0.15f, end + d * 0.4f + side * s + Vector3.down * 0.15f, 0.05f, _wood);
            Prim(PrimitiveType.Cube, g, "DiveTip", end + d * (DiveLen - 0.06f) + Vector3.up * 0.01f, new Vector3(DiveW, 0.02f, 0.12f), _mark, yaw, null, false);

            // 丸天井から吊る縄（4 本）と、根元から壁へ斜めの梁（2 本）
            float hw = EngawaW * 0.5f - 0.05f;
            foreach (var p in new[] { root + d * 1.1f + side * hw, root + d * 1.1f - side * hw, end - d * 0.1f + side * hw, end - d * 0.1f - side * hw })
                Rod(g, "HangRope", p + Vector3.down * 0.15f, new Vector3(p.x, DomeY(new Vector2(p.x, p.z).magnitude) + 0.15f, p.z), 0.025f, _wood);
            foreach (float s in new[] { -0.5f, 0.5f })
            {
                var under = root + d * 0.7f + side * s + Vector3.down * 0.15f;
                var wall = Polar(EngawaDeg, 5.95f, F3Y - 1.3f) + side * s;
                Rod(g, "EngawaBrace", under, wall, 0.06f, _wood);
            }

            // 低い手すり（0.3 m）：長い辺の両側と、先の飛びこみ台の両わき
            void LowRail(Vector3 p0, Vector3 p1)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt((p1 - p0).magnitude / 0.6f));
                for (int i = 0; i <= n; i++)
                {
                    var p = Vector3.Lerp(p0, p1, i / (float)n);
                    Rod(g, "LowRailPost", p, p + Vector3.up * 0.3f, 0.025f, _wood);
                }
                Rod(g, "LowRail", p0 + Vector3.up * 0.3f, p1 + Vector3.up * 0.3f, 0.022f, _wood);
            }
            float rw = EngawaW * 0.5f - 0.04f;
            var r0 = root + d * 0.15f; var r1 = end - d * 0.04f;
            LowRail(r0 + side * rw, r1 + side * rw);
            LowRail(r0 - side * rw, r1 - side * rw);
            LowRail(r1 + side * rw, r1 + side * (DiveW * 0.5f + 0.08f));
            LowRail(r1 - side * rw, r1 - side * (DiveW * 0.5f + 0.08f));

            Label("縁側（座れる・低い手すり）", root + d * 1.2f + Vector3.up * 1.0f, -30f, 1.1f);
            Label("飛びこみ台（水面まで 2.8 m）", end + d * 1.6f + Vector3.up * 0.9f, -30f, 1.1f);
        }

        /// <summary>縁側と飛びこみ台をナメクジの目の高さから（Screenshots/Base/base_3f_engawa.png）。</summary>
        static (string, Vector3, Vector3, float, bool)[] EngawaViews()
        {
            var (a, d) = EngawaFrame();
            var end = a + d * (EngawaLen - 0.1f);
            var tip = end + d * DiveLen;
            var eye = Vector3.up * 0.47f;
            var pool = new Vector3(SkyC.x, UpY - 0.1f, SkyC.y);
            return new[]
            {
                ("ledge", Polar(184f, 5.15f, F3Y) + eye, end + Vector3.up * 0.2f, 70f, false),                   // 3階の棚から北を見る
                ("engawa", a + d * 0.3f + eye, tip + d * 2f + Vector3.down * 0.3f, 70f, false),                 // 縁側に出たところ
                ("tip", tip - d * 0.8f + eye, new Vector3(tip.x, UpY, tip.z) + d * 1.4f, 75f, false),             // 飛びこみ台の先から池を見下ろす
                ("pool", new Vector3(1.6f, UDeck, 1.75f) + eye, end + d * 0.8f + Vector3.up * 0.3f, 70f, false), // 池の南のふちから見上げる
                ("map", MapAt + new Vector3(0.6f, 0f, -0.4f) + eye, end + Vector3.up * 0.4f, 70f, false),         // 地図の台から見上げる
                ("cut", new Vector3(6.5f, 10.5f, -6.5f), new Vector3(-1.6f, 5.0f, 2.4f), 45f, true),             // 殻を外して斜め上から
            };
        }
    }
}
