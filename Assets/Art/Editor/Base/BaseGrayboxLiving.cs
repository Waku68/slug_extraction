using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の灰色の箱：暮らしの跡と、残りの設備の中身と、外まわり（Nov 4、わくさん「さっきの全部」）。色はまだ、形だけ。当たり判定はなし（見た目）。
    /// 硬派（道具と使い込んだ跡。かわいい小物は置かない、spec 10章）。
    /// - 暮らしの跡：帰ってきた踊り場に籠 3 つと下ろしたバッグ、トンネルの西の壁の釘に巻いた縄 2 つ、ヤシの葉のほうき、地図の台座のふちに丸太の腰掛け 2 本、池の板の道に水がめ
    ///   灯の形（油の皿と火、壁の腕木）は BaseGrayboxKit.cs の Lamp
    /// - 乾燥室（2階）：干し棚に吊った束、棚の間の干し縄、塩漬けの壺 3 つ
    /// - 染め場（上段の東）：媒染の壺 3 つ（灰・岩塩・黒い塩）、花をつぶす鉢と杵、土の籠 2 つ、染め池のかき混ぜ棒
    /// - 苗床：くされの山（苗床の北）、水の桶、植え箱に挿した小さな鍬
    /// - 問屋の窓口：天井から吊った干し草の束 4 つ、口の上の看板の板
    /// - 桟橋：浮いていた灯 2 つに柱、繋ぎ杭 2 本と行商人の舟への舫い綱、巻いた綱、荷の箱 2 つ。舟に葉の屋根と荷と竿
    /// - カタツムリの小屋：葉の屋根、西向きの戸口、前に苔の敷物と水の皿
    /// </summary>
    public static partial class BaseGrayboxBuilder
    {
        static void Living()
        {
            var g = Group("Living");
            Traces(g);
            DryRoomDetails(g);
            DyeDetails(g);
            NurseryDetails(g);
            ShopDetails(g);
            PierDetails(g);
        }

        // ───────── 暮らしの跡 ─────────

        static void Traces(Transform g)
        {
            // 帰ってきた踊り場（下段の南）の東のはし：籠 3 つと、下ろしたバッグ
            var land = new Vector3(0.95f, LDeck, -5.85f);
            Basket(g, land, 0.2f, 0.28f);
            Basket(g, land + new Vector3(0.0f, 0.28f, 0.0f), 0.17f, 0.22f);
            Basket(g, land + new Vector3(0.0f, 0f, 0.45f), 0.18f, 0.24f);
            Prim(PrimitiveType.Sphere, g, "DroppedBag", land + new Vector3(-0.5f, 0.2f, 0.15f), new Vector3(0.45f, 0.4f, 0.35f), _mark, 25f, null, false);
            Rod(g, "DroppedBagStrap", land + new Vector3(-0.68f, 0.32f, 0.05f), land + new Vector3(-0.85f, 0.02f, -0.15f), 0.02f, _wood);

            // トンネルの西の壁（段の途中）の釘に巻いた縄 2 つ
            foreach (float z in new[] { -6.9f, -7.6f })
            {
                var peg = new Vector3(-2.4f, LDeck + 0.5f, z);
                Rod(g, "WallPeg", peg + Vector3.left * 0.1f, peg + Vector3.right * 0.08f, 0.015f, _wood);
                Prim(PrimitiveType.Cylinder, g, "RopeCoil", peg + new Vector3(0.05f, -0.18f, 0f), new Vector3(0.36f, 0.03f, 0.36f), _wood, 0f, new Vector3(0f, 0f, 80f), false);
            }
            // ヤシの葉のほうき（踊り場の西の壁に立てかける）
            var bf = new Vector3(-2.05f, LDeck, -5.95f);
            var bt = new Vector3(-2.35f, LDeck + 1.05f, -6.05f);
            Rod(g, "BroomHandle", bf + Vector3.up * 0.25f, bt, 0.018f, _wood);
            Prim(PrimitiveType.Cube, g, "BroomHead", bf + new Vector3(0.02f, 0.14f, 0f), new Vector3(0.3f, 0.3f, 0.06f), _moss, 0f, new Vector3(0f, 90f, 16f), false);

            // 地図の台座のふち（直径 4.6 m の板の上）に丸太の腰掛け 2 本（4 人が立つ所をよける）
            foreach (float a in new[] { 160f, 255f })
            {
                var p = MapAt + Polar(a, 1.95f, 0.13f);
                var t = Polar(a + 90f, 0.45f, 0f);
                Rod(g, "LogSeat", p - t, p + t, 0.13f, _wood);
            }
            // 池の板の道の南のふちに水がめ（ふたにヤシの葉）
            var jar = new Vector3(0.3f, UDeck, 1.58f);
            Prim(PrimitiveType.Sphere, g, "WaterJar", jar + Vector3.up * 0.2f, new Vector3(0.34f, 0.4f, 0.34f), _rock, 0f, null, false);
            Cyl(g, "WaterJarNeck", jar + Vector3.up * 0.36f, 0.1f, 0.08f, _rock, false);
            Prim(PrimitiveType.Cube, g, "WaterJarLid", jar + Vector3.up * 0.45f, new Vector3(0.3f, 0.015f, 0.12f), _moss, 30f, null, false);
        }

        /// <summary>素焼きの壺（ふくらんだ胴と短い首。筒にすると巻いた紙に見えるので丸く）。p は底、r は胴の半径、h は首の上まで。</summary>
        static void Jar(Transform g, Vector3 p, float r, float h)
        {
            float body = h - r * 0.35f;
            Prim(PrimitiveType.Sphere, g, "Jar", p + Vector3.up * body * 0.5f, new Vector3(r * 2f, body, r * 2f), _rock, 0f, null, false);
            Cyl(g, "JarNeck", p + Vector3.up * (body - 0.02f), r * 0.55f, h - body + 0.02f, _rock, false);
        }

        /// <summary>編んだ籠（丸い筒と、ふちの輪）。p は底。</summary>
        static void Basket(Transform g, Vector3 p, float r, float h)
        {
            Cyl(g, "Basket", p, r, h, _wood, false);
            Cyl(g, "BasketRim", p + Vector3.up * (h - 0.02f), r + 0.02f, 0.04f, _wood, false);
        }

        // ───────── 乾燥室（2階の木の回廊） ─────────

        static void DryRoomDetails(Transform g)
        {
            // 干し棚（228°・245°、半径 5.75、高さ F2Y+0.05〜1.25）に吊った束
            foreach (float a in new[] { 228f, 245f })
            {
                var (right, inward) = WallFrame(a);
                var rack = Polar(a, 5.75f, F2Y + 1.15f) + inward * 0.08f;
                for (int i = 0; i < 4; i++)
                {
                    var top = rack + right * (-0.45f + 0.3f * i);
                    float len = 0.25f + 0.12f * PropKit.H(i + (int)a, 91);
                    Rod(g, "DryCord", top, top + Vector3.down * 0.1f, 0.005f, _wood);
                    Prim(PrimitiveType.Cube, g, "DryBundle", top + Vector3.down * (0.1f + len * 0.5f), new Vector3(0.1f, len, 0.06f), i % 2 == 0 ? _moss : _mark,
                         FaceCenter(a) + 10f * i, null, false);
                }
            }
            // 棚の間の干し縄（236.5°の上を渡す）とキノコの串
            var r0 = Polar(229f, 5.6f, F2Y + 1.55f); var r1 = Polar(244f, 5.6f, F2Y + 1.55f);
            Vector3 prev = r0;
            for (int k = 1; k <= 6; k++)
            {
                float t = k / 6f;
                var p = Vector3.Lerp(r0, r1, t) + Vector3.down * 0.08f * 4f * t * (1f - t);
                Rod(g, "DryLine", prev, p, 0.008f, _wood);
                if (k < 6)
                    for (int m = 0; m < 3; m++)
                        Prim(PrimitiveType.Sphere, g, "DryMushroom", p + Vector3.down * (0.06f + 0.06f * m), new Vector3(0.07f, 0.04f, 0.07f), _mark, 0f, null, false);
                prev = p;
            }
            // 塩漬けの壺 3 つ（250°の床、ふたに重しの石）
            for (int i = 0; i < 3; i++)
            {
                var p = Polar(249f + i * 2.2f, 5.35f - 0.25f * (i % 2), F2Y);
                Jar(g, p, 0.17f, 0.34f);
                Cyl(g, "PickleLid", p + Vector3.up * 0.34f, 0.1f, 0.03f, _wood, false);
                Prim(PrimitiveType.Sphere, g, "PickleWeight", p + Vector3.up * 0.4f, new Vector3(0.12f, 0.08f, 0.1f), _rock, i * 50f, null, false);
            }
        }

        // ───────── 染め場（上段の東） ─────────

        static void DyeDetails(Transform g)
        {
            // 板の台（x 2.2〜3.5、z 0.95〜2.05）の西のはし：媒染の壺 3 つ
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(2.38f, UDeck, 1.1f + 0.3f * i);
                Jar(g, p, 0.12f, 0.24f);
                Prim(PrimitiveType.Sphere, g, "MordantFill", p + Vector3.up * 0.25f, new Vector3(0.1f, 0.03f, 0.1f), i == 1 ? _mark : _floor, 0f, null, false);
            }
            // 花をつぶす鉢と杵
            var mortar = new Vector3(3.0f, UDeck, 1.85f);
            Cyl(g, "DyeMortar", mortar, 0.18f, 0.16f, _rock, false);
            Rod(g, "DyePestle", mortar + new Vector3(0f, 0.12f, 0f), mortar + new Vector3(0.15f, 0.45f, -0.08f), 0.03f, _wood);
            // 島から持ち帰った土の籠 2 つ（台の南、中段の岩の上。① と ② の間）
            foreach (var q in new[] { new Vector2(3.6f, 0.1f), new Vector2(3.25f, 0.05f) })
            {
                var p = new Vector3(q.x, HallGround(q.x, q.y), q.y);
                Basket(g, p, 0.17f, 0.2f);
                Prim(PrimitiveType.Sphere, g, "Soil", p + Vector3.up * 0.2f, new Vector3(0.3f, 0.1f, 0.3f), _floor, 0f, null, false);
            }
            // 1 つ目の染め池のかき混ぜ棒（ふちに立てかける）
            var b = DyeBasins[0];
            Rod(g, "DyeStick", new Vector3(b.x + 0.1f, UpY - 0.12f, b.y), new Vector3(b.x + 0.5f, UpY + 0.7f, b.y + 0.15f), 0.02f, _wood);
        }

        // ───────── 苗床 ─────────

        static void NurseryDetails(Transform g)
        {
            // くされの山（苗床の北、壁ぎわ）：低い木の囲いと、中のこんもり
            var c = new Vector3(-2.6f, HallGround(-2.6f, 3.95f), 3.95f);
            for (int k = 0; k < 4; k++)
            {
                float yaw = k * 90f;
                var off = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.12f, 0.33f);
                Prim(PrimitiveType.Cube, g, "CompostBoard", c + off, new Vector3(0.7f, 0.24f, 0.04f), _wood, yaw, null, false);
            }
            Prim(PrimitiveType.Sphere, g, "Compost", c + Vector3.up * 0.18f, new Vector3(0.6f, 0.3f, 0.6f), _floor, 0f, null, false);
            // 水の桶（くされの山の東）と小さな鍬（植え箱に挿す）
            var bucket = new Vector3(-1.85f, HallGround(-1.85f, 3.8f), 3.8f);
            Cyl(g, "WaterBucket", bucket, 0.14f, 0.22f, _wood, false);
            Rod(g, "BucketHandle", bucket + new Vector3(-0.14f, 0.22f, 0f), bucket + new Vector3(0.14f, 0.22f, 0f), 0.01f, _wood);
            var hoe = new Vector3(-2.3f, UpY + 0.3f, 2.4f);
            Rod(g, "HoeHandle", hoe, hoe + new Vector3(0.12f, 0.45f, -0.08f), 0.015f, _wood);
            Prim(PrimitiveType.Cube, g, "HoeBlade", hoe + Vector3.down * 0.02f, new Vector3(0.1f, 0.08f, 0.02f), _rock, 0f, null, false);
        }

        // ───────── 問屋の窓口 ─────────

        static void ShopDetails(Transform g)
        {
            float ceil = TunY + 2.1f;
            foreach (var q in new[] { new Vector2(-3.3f, -8.95f), new Vector2(-3.8f, -9.1f), new Vector2(-3.4f, -10.45f), new Vector2(-3.9f, -10.3f) })
            {
                var top = new Vector3(q.x, ceil, q.y);
                Rod(g, "HerbCord", top, top + Vector3.down * 0.15f, 0.006f, _wood);
                Prim(PrimitiveType.Cube, g, "HerbBundle", top + Vector3.down * 0.35f, new Vector3(0.14f, 0.4f, 0.1f), _moss, q.x * 90f, null, false);
            }
            // 口の上の看板の板（トンネルから読める向き）
            var sign = new Vector3(-2.62f, ceil - 0.15f, -9.7f);
            Box(g, "ShopSign", sign + new Vector3(0f, -0.13f, -0.5f), sign + new Vector3(0.04f, 0.13f, 0.5f), _wood, false);
            Prim(PrimitiveType.Cube, g, "ShopSignMark", sign + new Vector3(0.045f, 0f, 0f), new Vector3(0.01f, 0.12f, 0.12f), _mark, 0f, new Vector3(45f, 0f, 0f), false);
        }

        // ───────── 桟橋・舟・カタツムリの小屋 ─────────

        static void PierDetails(Transform g)
        {
            float top = TunY + Board;
            // 浮いていた灯 2 つ（Lamps の (3.0, -15.4) と (-3.0, -19.6)）に柱と腕木
            foreach (var (x, z) in new[] { (3.0f, -15.4f), (-3.0f, -19.6f) })
            {
                var post = new Vector3(x, top, z);
                Rod(g, "LampPost", post, post + Vector3.up * 1.25f, 0.05f, _wood);
            }
            // 繋ぎ杭 2 本と、行商人の舟への舫い綱、杭に巻いた綱
            foreach (var (z, bz) in new[] { (-16.9f, -17.1f), (-19.0f, -19.2f) })
            {
                var b = new Vector3(3.0f, top, z);
                Cyl(g, "Bollard", b, 0.09f, 0.32f, _wood, false);
                Cyl(g, "BollardRope", b + Vector3.up * 0.16f, 0.11f, 0.06f, _wood, false);
                var boat = new Vector3(3.55f, 0.45f, bz);
                var mid = (b + Vector3.up * 0.19f + boat) * 0.5f + Vector3.down * 0.12f;
                Rod(g, "MooringRope", b + Vector3.up * 0.19f, mid, 0.012f, _wood);
                Rod(g, "MooringRope", mid, boat, 0.012f, _wood);
            }
            Prim(PrimitiveType.Cylinder, g, "RopeCoilPier", new Vector3(2.5f, top + 0.03f, -16.6f), new Vector3(0.45f, 0.03f, 0.45f), _wood, 0f, null, false);
            // 荷の箱 2 つ（桟橋の東の手前）
            Box(g, "PierCrate", new Vector3(1.75f, top, -15.25f), new Vector3(2.25f, top + 0.4f, -14.8f), _wood, false);
            Box(g, "PierCrate", new Vector3(1.85f, top + 0.4f, -15.15f), new Vector3(2.2f, top + 0.7f, -14.85f), _wood, false);
            // 舟（x 3.4〜4.6、z -20〜-16.5、ふち y 0.45）：後ろ半分に葉の屋根、荷、竿
            foreach (var (x, z) in new[] { (3.5f, -19.8f), (4.5f, -19.8f), (3.5f, -18.4f), (4.5f, -18.4f) })
                Rod(g, "BoatPost", new Vector3(x, 0.45f, z), new Vector3(x, 1.25f, z), 0.03f, _wood);
            Prim(PrimitiveType.Cube, g, "BoatRoof", new Vector3(4.0f, 1.3f, -19.1f), new Vector3(1.4f, 0.08f, 1.7f), _moss, 0f, new Vector3(0f, 0f, 6f), false);
            Box(g, "BoatCargo", new Vector3(3.7f, 0.45f, -17.6f), new Vector3(4.3f, 0.8f, -17.0f), _wood, false);
            Basket(g, new Vector3(4.1f, 0.45f, -16.85f), 0.15f, 0.2f);
            Rod(g, "BoatPole", new Vector3(4.5f, 0.45f, -16.7f), new Vector3(4.3f, 2.4f, -18.0f), 0.025f, _wood);

            // カタツムリの小屋（x 4.3〜5.9、z -15.7〜-14.3、高さ 1.2）：葉の屋根、西向きの戸口、苔の敷物と水の皿
            Prim(PrimitiveType.Cube, g, "SnailHutRoof", new Vector3(5.1f, top + 1.3f, -15.0f), new Vector3(1.9f, 0.1f, 1.7f), _moss, 0f, new Vector3(0f, 0f, -10f), false);
            Box(g, "SnailHutDoor", new Vector3(4.285f, top, -15.25f), new Vector3(4.3f, top + 0.75f, -14.75f), _floor, false);
            Prim(PrimitiveType.Sphere, g, "SnailMat", new Vector3(3.85f, top + 0.01f, -15.0f), new Vector3(0.7f, 0.04f, 0.6f), _moss, 0f, null, false);
            Cyl(g, "SnailWaterDish", new Vector3(3.75f, top, -15.55f), 0.13f, 0.05f, _rock, false);
            Cyl(g, "SnailWater", new Vector3(3.75f, top + 0.045f, -15.55f), 0.11f, 0.01f, _water, false);
        }
    }
}
