using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の灰色の箱の中身：設備の置き場所（仮の箱）、外（桟橋・発着場・崖の中腹）、灯、大きさの見本のナメクジ。
    /// 角度は広間の真ん中から（東 0°、北 90°、西 180°、南 270°＝トンネル）。置き場所の理由は docs/discussions/base_layout_v2.md。
    /// </summary>
    public static partial class BaseGrayboxBuilder
    {
        static Vector3 MapAt => new Vector3(-0.5f, MDeck, -0.95f);       // 中段のまん中（集会所）
        static Vector3 MillAt => new Vector3(-2.9f, LDeck, -3.9f);       // 下段の西（出る道で弾を取る）
        static Vector3 GrindAt => Polar(174f, 6.0f, MDeck);              // 中段の西の横穴（主軸の先）

        static float LabelYaw(float deg) => FaceCenter(deg) + 180f;

        /// <summary>半径 r の弧に沿った低い手すり（高さ 0.5 m）。y1 を渡すと坂に沿って上がる。</summary>
        static void Railing(Transform g, float r, float a0, float a1, float y0, float step, float y1 = float.NaN)
        {
            if (float.IsNaN(y1)) y1 = y0;
            Vector3 last = Vector3.zero;
            for (float a = a0; a <= a1 + 0.01f; a += step)
            {
                float y = Mathf.Lerp(y0, y1, (a - a0) / (a1 - a0));
                var p = Polar(a, r, y);
                Rod(g, "RailPost", p, p + Vector3.up * 0.5f, 0.03f, _wood);
                if (a > a0) Rod(g, "Rail", last + Vector3.up * 0.5f, p + Vector3.up * 0.5f, 0.025f, _wood);
                last = p;
            }
        }

        // ───────── 設備（仮の箱） ─────────

        static void Rooms()
        {
            var g = Group("Rooms");

            // 地図の台（中段のまん中の円い台座の上。友達と囲む集会所。足元の板の下を ③ がくぐる）
            Cyl(g, "MapTable", MapAt, 1.0f, 0.4f, _wood);
            Prim(PrimitiveType.Cube, g, "MapSheet", MapAt + Vector3.up * 0.41f, new Vector3(1.4f, 0.02f, 0.95f), _mark, 15f, null, false);
            Prim(PrimitiveType.Cube, g, "Ledger", MapAt + new Vector3(0.62f, 0.44f, -0.35f), new Vector3(0.3f, 0.06f, 0.22f), _wood, -20f, null, false);
            Label("地図の台（台座 直径 4.6 m）", MapAt + Vector3.up * 1.5f, 0f);
            Label("池（湧き水）", new Vector3(SkyC.x, UpY + 1.3f, SkyC.y), 0f);
            Label("① 本流", new Vector3(3.0f, MidY + 0.7f, -1.1f), 0f, 1.1f);
            Label("② 染め水", new Vector3(4.2f, MidY + 0.7f, 0.0f), 0f, 1.1f);
            Label("③ 苗床の水", new Vector3(-1.4f, MidY + 0.7f, 0.5f), 0f, 1.1f);

            // 下段の東の壁（① の橋を渡った先）の小さな横穴：飾り枠と図鑑の棚
            Prim(PrimitiveType.Cube, g, "Kazari", Polar(304f, 6.4f, LDeck + 0.5f), new Vector3(1.0f, 1.0f, 0.3f), _wood, FaceCenter(304f));
            Prim(PrimitiveType.Sphere, g, "KazariShell", Polar(304f, 6.1f, LDeck + 1.2f), Vector3.one * 0.5f, _mark, 0f, null, false);
            Label("飾り枠", Polar(304f, 5.4f, LDeck + 1.9f), LabelYaw(304f), 1.2f);
            Prim(PrimitiveType.Cube, g, "Zukan", Polar(317f, 6.45f, LDeck + 0.65f), new Vector3(1.0f, 1.3f, 0.3f), _wood, FaceCenter(317f));
            Label("図鑑", Polar(317f, 5.4f, LDeck + 1.9f), LabelYaw(317f), 1.2f);

            // カタツムリの寝床（下段の東、① の横の湿った横穴。苔を敷いて、殻にこもって眠る）
            Prim(PrimitiveType.Sphere, g, "MossBed", Polar(335f, 6.5f, LDeck - 0.02f), new Vector3(1.3f, 0.22f, 1.0f), _moss, FaceCenter(335f), null, false);
            Prim(PrimitiveType.Sphere, g, "SleepingSnail", Polar(335f, 6.55f, LDeck + 0.28f), new Vector3(0.6f, 0.55f, 0.5f), _slugMat, FaceCenter(335f), null, false);
            Label("カタツムリの寝床", Polar(335f, 5.4f, LDeck + 1.6f), LabelYaw(335f), 1.2f);

            // 精製所（下段の西）：本物の形（太い木と丸い石、Slug/Toon）。作りは Refinery/BaseRefineryInBase.cs、置き場所と動線の理由もそちら
            var hop = new Vector3(MillAt.x, 0f, MillAt.z).normalized;   // 入れ口は壁の側（外向き）
            BaseRefineryBuilder.PlaceInBase(Group("Refinery"), new BaseRefineryBuilder.RefinerySite
            {
                mill = MillAt, axleY = HallAxleY, hopperDir = hop,
                bench = new Vector3(-1.45f, LDeck, -3.9f), benchYaw = -90f,
                shelf = Polar(220f, 6.95f, LDeck), shelfYaw = FaceCenter(220f),
                amore = Polar(212f, 6.25f, LDeck), amoreYaw = FaceCenter(212f),
                looseBars = Polar(227f, 6.2f, LDeck), looseYaw = FaceCenter(227f),
                deck = Rect.MinMaxRect(-4.9f, -4.5f, -1.0f, -2.85f), deckY = LDeck,
                ground = HallGround, wood = _wood, rock = _rock, mark = _mark, lamp = _lamp, moss = _moss, water = _water,
            });
            Label("精製所（臼・型）", MillAt + new Vector3(0.4f, 2.1f, 0f), 0f, 1.3f);
            Label("塩の壺の棚・アモレ（横穴）", Polar(220f, 5.3f, LDeck + 1.75f), LabelYaw(220f), 1.1f);

            // 細工場（中段の西の横穴）：砥ぎ車（主軸の先）・作業台・奥の壁の道具掛け
            var gp = GrindAt + Vector3.up * 0.6f;
            Prim(PrimitiveType.Cylinder, g, "Grindstone", gp, new Vector3(0.9f, 0.08f, 0.9f), _rock, 0f,
                 Quaternion.LookRotation(Vector3.up, (gp - new Vector3(0f, gp.y, 0f)).normalized).eulerAngles, false);
            Rod(g, "GrindShaft", gp, new Vector3(gp.x, HallAxleY, gp.z), 0.06f, _wood);
            Prim(PrimitiveType.Cube, g, "WorkBench", Polar(167f, 6.9f, MDeck + 0.3f), new Vector3(1.3f, 0.6f, 0.55f), _wood, FaceCenter(167f));
            Prim(PrimitiveType.Cube, g, "ToolRack", Polar(172f, 7.5f, MDeck + 1.1f), new Vector3(1.9f, 1.1f, 0.08f), _wood, FaceCenter(172f), null, false);
            Label("細工場（横穴・砥ぎ車）", Polar(172f, 5.0f, MDeck + 1.7f), LabelYaw(172f), 1.3f);

            // 乾燥室（2階の木の回廊）：臼の上から縦の軸で回る、ヤシの葉の羽根車と干し棚
            Turbine(g, MillAt);
            Prim(PrimitiveType.Cube, g, "DryRack", Polar(228f, 5.75f, F2Y + 0.65f), new Vector3(1.3f, 1.2f, 0.1f), _wood, FaceCenter(228f), null, false);
            Prim(PrimitiveType.Cube, g, "DryRack", Polar(245f, 5.75f, F2Y + 0.65f), new Vector3(1.3f, 1.2f, 0.1f), _wood, FaceCenter(245f), null, false);
            Label("乾燥室（ヤシの羽根車）", Polar(240f, 4.5f, F2Y + 2.0f), LabelYaw(240f), 1.3f);
            Label("2階 → 3階 → 崖の中腹", Polar(212f, 4.2f, F2Y + 0.9f), LabelYaw(212f), 1.1f);

            // 染め場（上段の東）：② が通る浅い池 3 つ（Ground の溝）と、染めた布を干す竿
            Rod(g, "ClothPost", Polar(5f, 5.3f, UpY - 0.2f), Polar(5f, 5.3f, UpY + 1.6f), 0.05f, _wood);
            Rod(g, "ClothPost", Polar(35f, 5.3f, UpY - 0.2f), Polar(35f, 5.3f, UpY + 1.6f), 0.05f, _wood);
            Rod(g, "ClothBar", Polar(5f, 5.3f, UpY + 1.6f), Polar(35f, 5.3f, UpY + 1.6f), 0.03f, _wood);
            for (int i = 0; i < 3; i++)
                Prim(PrimitiveType.Cube, g, "Cloth", Polar(12f + i * 8f, 5.3f, UpY + 1.15f), new Vector3(0.5f, 0.8f, 0.03f), _mark, FaceCenter(12f + i * 8f), null, false);
            Label("染め場", new Vector3(3.6f, UpY + 1.5f, 1.9f), 0f, 1.3f);

            // 苗床（上段の西、池のふちを平らに削った台）：同じ大きさの植え箱を2列3つずつそろえて並べる。2列の間を ③ がまっすぐ通る
            var bed = NurseryBed;
            foreach (float z in new[] { 2.35f, 3.15f })
                for (int i = 0; i < 3; i++)
                {
                    var p = new Vector3(-3.0f + i * 0.7f, UpY + 0.15f, z);
                    Prim(PrimitiveType.Cube, g, "Planter", p, new Vector3(0.55f, 0.3f, 0.5f), _wood);
                    Prim(PrimitiveType.Sphere, g, "Sprout", p + Vector3.up * 0.18f, new Vector3(0.3f, 0.12f, 0.25f), _moss, 0f, null, false);
                    Prim(PrimitiveType.Cube, g, "Tag", p + new Vector3(0f, 0.2f, z < 2.75f ? -0.27f : 0.27f), new Vector3(0.14f, 0.1f, 0.02f), _mark, 0f, null, false);
                }
            // 台のふちの低い石の縁（四辺）
            Box(g, "BedCurb", new Vector3(bed.xMin, UpY - 0.1f, bed.yMin), new Vector3(bed.xMax, UpY + 0.08f, bed.yMin + 0.12f), _rock, false);
            Box(g, "BedCurb", new Vector3(bed.xMin, UpY - 0.1f, bed.yMax - 0.12f), new Vector3(bed.xMax, UpY + 0.08f, bed.yMax), _rock, false);
            Box(g, "BedCurb", new Vector3(bed.xMin, UpY - 0.1f, bed.yMin), new Vector3(bed.xMin + 0.12f, UpY + 0.08f, bed.yMax), _rock, false);
            Label("苗床（そろえて2列）", new Vector3(-2.3f, UpY + 1.1f, 2.75f), 0f, 1.3f);
        }

        /// <summary>ヤシの葉を蒸気タービンのように放射状に並べた羽根車（2階、広間へ風を送る向き）。</summary>
        static void Turbine(Transform g, Vector3 foot)
        {
            var hub = new Vector3(foot.x, F2Y + 1.0f, foot.z);
            var n = -new Vector3(foot.x, 0f, foot.z).normalized;   // 広間の真ん中を向く
            var side = Vector3.Cross(n, Vector3.up);
            Rod(g, "TurbineHub", hub - n * 0.15f, hub + n * 0.2f, 0.14f, _wood);
            for (int k = 0; k < 10; k++)
            {
                float t = k * Mathf.PI * 2f / 10f;
                var dir = Mathf.Cos(t) * Vector3.up + Mathf.Sin(t) * side;
                var rot = Quaternion.LookRotation(n, dir) * Quaternion.AngleAxis(32f, Vector3.up);
                Prim(PrimitiveType.Cube, g, "TurbineLeaf", hub + dir * 0.5f, new Vector3(0.24f, 0.72f, 0.03f), _mark, 0f, rot.eulerAngles, false);
            }
        }

        // ───────── 外：桟橋・発着場・カタツムリの小屋・崖の中腹 ─────────

        static void Outside()
        {
            var g = Group("Outside");
            float top = TunY + Board;
            Box(g, "Pier", new Vector3(-3.2f, top - 0.25f, -21f), new Vector3(3.2f, top, MouthZ - 0.4f), _wood);
            for (float z = -15f; z > -21.5f; z -= 2f)
                foreach (float x in new[] { -3f, 3f }) Rod(g, "PierLeg", new Vector3(x, -0.6f, z), new Vector3(x, top - 0.25f, z), 0.1f, _wood);
            var basket = new Vector3(0f, top, -18.3f);
            Cyl(g, "RoundBasket", basket, 1.25f, 0.42f, _wood);
            for (int i = 0; i < 4; i++) Rod(g, "BasketRope", basket + Polar(45f + 90f * i, 1.2f, 0.42f), basket + Vector3.up * 2.4f, 0.02f, _wood);
            Label("発着場（丸い籠）", basket + Vector3.up * 2.9f, 0f, 1.4f);
            var bird = new Vector3(2.3f, top, -19.4f);
            Prim(PrimitiveType.Sphere, g, "BirdBody", bird + Vector3.up * 1.15f, new Vector3(1.0f, 1.3f, 1.3f), _slugMat, 0f, null, false);
            Prim(PrimitiveType.Sphere, g, "BirdHead", bird + new Vector3(0f, 1.85f, 0.35f), Vector3.one * 0.55f, _slugMat, 0f, null, false);
            Prim(PrimitiveType.Cube, g, "BirdBeak", bird + new Vector3(0f, 1.7f, 0.85f), new Vector3(0.18f, 0.16f, 0.75f), _mark, 0f, null, false);
            Label("鳥（立って 2.1 m）", bird + Vector3.up * 2.6f, 0f, 1.2f);
            Box(g, "BoatHull", new Vector3(3.4f, -0.2f, -20f), new Vector3(4.6f, 0.45f, -16.5f), _wood, false);
            Label("行商人の舟", new Vector3(4.0f, 1.3f, -18.2f), 0f, 1.2f);

            Box(g, "SnailLedge", new Vector3(3.2f, 0.4f, -16.2f), new Vector3(6.4f, top, MouthZ + 0.1f), _rock);
            Box(g, "SnailHut", new Vector3(4.3f, top, -15.7f), new Vector3(5.9f, top + 1.2f, -14.3f), _wood);
            Label("カタツムリの小屋", new Vector3(5.1f, top + 1.8f, -15.0f), 0f, 1.2f);

            // 3階から西へ抜ける短い通路と、崖の中腹の台（観測所と鳥の巣）
            Grid(g, "Shell_Passage", (u, v) =>
            {
                float a = Mathf.PI * u;
                return new Vector3(Mathf.Lerp(-5.4f, -9.0f, v), F3Y - 0.2f + 2.1f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(a)), 0.5f), -Mathf.Cos(a) * 0.85f);
            }, 10, 6, null, p => new Vector3(p.x, F3Y + 0.8f, 0f) - p, _rock);
            Planks(g, "PassageFloor", new Vector3(-7.2f, F3Y, 0f), 90f, 3.6f, 1.6f, false, 0.4f);
            Planks(g, "Terrace", new Vector3(-10.45f, F3Y, -0.5f), 0f, 5.4f, 4.3f, false, 2.5f);
            Box(g, "ObsHut", new Vector3(-12.3f, F3Y, 0.6f), new Vector3(-10.9f, F3Y + 1.7f, 2.0f), _wood);
            Rod(g, "Vane", new Vector3(-12.0f, F3Y, -2.6f), new Vector3(-12.0f, F3Y + 2.2f, -2.6f), 0.04f, _wood);
            Prim(PrimitiveType.Cube, g, "VaneTail", new Vector3(-12.0f, F3Y + 2.2f, -2.6f), new Vector3(0.08f, 0.25f, 0.7f), _mark, 30f, null, false);
            var nest = new Vector3(-10.0f, F3Y, -2.3f);
            Cyl(g, "Nest", nest, 0.85f, 0.35f, _wood);
            for (float z = -3.2f; z < 2.3f; z += 0.9f) Rod(g, "TerracePost", new Vector3(-12.55f, F3Y, z), new Vector3(-12.55f, F3Y + 0.55f, z), 0.03f, _wood);
            for (float x = -12.55f; x < -8.3f; x += 0.9f) Rod(g, "TerracePost", new Vector3(x, F3Y, -3.15f), new Vector3(x, F3Y + 0.55f, -3.15f), 0.03f, _wood);
            Label("観測所", new Vector3(-11.6f, F3Y + 2.3f, 1.3f), 180f, 1.3f);
            Label("鳥の巣", nest + Vector3.up * 1.0f, 180f, 1.3f);
        }

        // ───────── 灯・見本のナメクジ ─────────

        static void Lamps()
        {
            var g = Group("Lamps");
            foreach (float a in new[] { 236f, 300f, 348f, 20f, 50f, 80f, 115f, 150f, 196f })
            {
                var p = Polar(a, 5.75f, 0f);
                Lamp(g, new Vector3(p.x, TerraceBase(p.x, p.z) + 1.7f, p.z));
            }
            foreach (float a in new[] { 225f, 247f }) Lamp(g, Polar(a, 5.85f, F2Y + 1.3f));
            Lamp(g, Polar(160f, 5.75f, F3Y + 1.1f));
            // 天井の梁から吊るす灯
            foreach (var (a, b, t) in new[] { (230f, 50f, 0.35f), (280f, 100f, 0.5f), (320f, 140f, 0.62f) })
            {
                var p = Vector3.Lerp(Polar(a, BeamR, BeamY), Polar(b, BeamR, BeamY), t);
                Rod(g, "LampRope", p, p + Vector3.down * (BeamY - 5.5f), 0.015f, _wood);
                Lamp(g, p + Vector3.down * (BeamY - 5.4f));
            }
            Lamp(g, new Vector3(2.3f, TunY + 2.0f, -7.8f));
            Lamp(g, new Vector3(-2.3f, TunY + 2.0f, -7.6f));
            Lamp(g, new Vector3(2.3f, TunY + 2.0f, -10.8f));
            Lamp(g, new Vector3(-3.5f, TunY + 1.9f, -9.0f));
            Lamp(g, new Vector3(3.0f, TunY + 1.6f, -15.4f));
            Lamp(g, new Vector3(-3.0f, TunY + 1.6f, -19.6f));
            Lamp(g, new Vector3(-12.3f, F3Y + 1.4f, -2.9f));
        }

        static void Stand()
        {
            var g = Group("Slugs");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(DinoSlugPhotoRough.RigPrefab);
            if (asset == null) return;
            void Put(Vector3 p, float yaw)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                go.transform.SetParent(g, false);
                go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
            }
            foreach (float a in new[] { 30f, 120f, 210f, 300f })
            {
                var p = MapAt + Polar(a, 1.5f, 0f);
                Put(p, Mathf.Atan2(MapAt.x - p.x, MapAt.z - p.z) * Mathf.Rad2Deg);
            }
            Put(new Vector3(3.0f, LDeck, -4.2f), 60f);
            Put(new Vector3(0.4f, TunY + Board, -12.9f), 180f);
            Put(Polar(236f, 5.1f, F2Y), FaceCenter(236f));
            Put(new Vector3(-10.8f, F3Y, -1.0f), 200f);
        }
    }
}
