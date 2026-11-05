using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    public static partial class CenoteBaseCodeBuilder
    {
        // 時計の針の向き（12時＝+Z北、3時＝+X東、6時＝-Z南、9時＝-X西）
        public static float ClockRad(float hour) => hour * Mathf.PI / 6f;
        public static Vector3 ClockDir(float hour) => new Vector3(Mathf.Sin(ClockRad(hour)), 0f, Mathf.Cos(ClockRad(hour)));
        public static Vector3 ClockPos(float hour, float r, float y) => new Vector3(Mathf.Sin(ClockRad(hour)) * r, y, Mathf.Cos(ClockRad(hour)) * r);

        static float DomeRadius(float t)
        {
            // t=0 (Y=1.8) -> 4.7m, t=0.45 (Y=4.4) -> 5.5m, t=1.0 (Y=7.6) -> 1.4m
            float bulge = Mathf.Sin(Mathf.Clamp01(t * 0.92f) * Mathf.PI);
            float topClose = Mathf.Pow(Mathf.Clamp01((t - 0.45f) / 0.55f), 1.65f);
            return Mathf.Lerp(4.7f + 0.85f * bulge, 1.35f, topClose);
        }

        public static void BuildCavePrefabs(Material[] mats)
        {
            System.IO.Directory.CreateDirectory(BaseDir);

            BuildShellAndOuter(mats);
            BuildFloorAndPool(mats);
            BuildRamp(mats);
            BuildGallery(mats);
            BuildPierAndBoat(mats);
            BuildDeckAndFrame(mats);
            BuildLiftParts(mats);
            BuildCaveColliders(mats);
        }

        static void BuildShellAndOuter(Material[] mats)
        {
            // 1. CenoteBase_Shell（内壁ドーム＋部屋くぼみの岩枠＋10部屋のアンカー）
            var kit = new NatureMeshKit(3); // 0: Limestone, 1: LimestoneWet, 2: Moss
            int segs = 48, vRings = 20;
            // ドームの高さプロファイル（少し低く太くずんぐり化：1階 Y=1.8 → 2階 Y=4.6 → 天井 Y=7.6）
            // 頂点を共有グリッドで生成してスムーズ法線＆ポリゴン継ぎ目ゼロにする
            int[,] grid = new int[vRings + 1, segs + 1];
            for (int iy = 0; iy <= vRings; iy++)
            {
                float t = iy / (float)vRings;
                float y = Mathf.Lerp(1.8f, 7.6f, t);
                float r = DomeRadius(t);
                Vector3 c = new Vector3(1.5f * Mathf.Pow(t, 1.6f), y, 0f);
                for (int ix = 0; ix <= segs; ix++)
                {
                    int ixWrap = ix % segs;
                    float a = ixWrap * Mathf.PI * 2f / segs;
                    float w = 1f + 0.04f * Mathf.Sin(a * 3f + t * 4f) + 0.03f * Mathf.Cos(a * 5f - t * 3f);
                    grid[iy, ix] = kit.Vert(c + new Vector3(Mathf.Sin(a) * r * w, 0f, Mathf.Cos(a) * r * w), new Vector2(ix / (float)segs, t));
                }
            }
            for (int iy = 0; iy < vRings; iy++)
            {
                float y0 = Mathf.Lerp(1.8f, 7.6f, iy / (float)vRings);
                int sub = y0 < 2.3f ? 1 : 0; // 水際は濡れた岩
                for (int ix = 0; ix < segs; ix++)
                {
                    float hMid = (ix + 0.5f) * 12f / segs;
                    // 海の口（約7時、Y < 4.2）とデッキ口（約5.4時、4.4 <= Y < 6.0）は抜く
                    if (y0 < 4.2f && hMid > 6.3f && hMid < 7.7f) continue;
                    if (y0 >= 4.4f && y0 < 6.0f && hMid > 4.9f && hMid < 5.9f) continue;

                    // 内側（洞窟の中心側）を向く法線（左手系で ix:+X, iy:+Y なので v00, v01, v11, v10 で内向き）
                    kit.Quad(sub, grid[iy, ix], grid[iy + 1, ix], grid[iy + 1, ix + 1], grid[iy, ix + 1]);
                }
            }

            // 壁のゴツゴツした太い岩柱＆各部屋のアルコーブ（くぼみ）の縁石と奥壁
            float[] nicheHours = { 6.0f, 4.0f, 12.0f, 1.5f, 3.0f, 4.25f };
            float[] nicheY     = { 2.0f, 2.0f, 4.6f,  4.6f, 4.6f, 4.6f };
            float[] nicheW     = { 2.6f, 2.0f, 2.6f,  2.0f, 2.0f, 2.2f };
            float[] nicheH     = { 1.6f, 1.4f, 1.5f,  1.4f, 1.4f, 1.4f };
            for (int i = 0; i < nicheHours.Length; i++)
            {
                float h = nicheHours[i], fy = nicheY[i], nw = nicheW[i], nh = nicheH[i];
                float r = fy < 3f ? 4.5f : 4.8f;
                Vector3 pos = ClockPos(h, r, fy);
                Quaternion rot = Quaternion.LookRotation(ClockDir(h), Vector3.up);
                // 左右の太い岩柱と上のまぐさ石、奥の岩壁で「洞窟の横穴」をくっきり見せる
                AddChunkyBlob(kit, 0, pos + rot * new Vector3(-nw * 0.52f, nh * 0.5f, 0.5f), new Vector3(0.55f, nh + 0.3f, 1.5f), rot, 5, 7, 0.6f, 0.7f, 0.08f, i + 1f);
                AddChunkyBlob(kit, 0, pos + rot * new Vector3( nw * 0.52f, nh * 0.5f, 0.5f), new Vector3(0.55f, nh + 0.3f, 1.5f), rot, 5, 7, 0.6f, 0.7f, 0.08f, i + 2f);
                AddChunkyBlob(kit, 0, pos + rot * new Vector3(0f, nh + 0.22f, 0.5f), new Vector3(nw + 0.8f, 0.5f, 1.5f), rot, 4, 7, 0.6f, 0.7f, 0.07f, i + 3f);
                AddChunkyBlob(kit, 0, pos + rot * new Vector3(0f, nh * 0.5f, 1.45f), new Vector3(nw + 0.4f, nh + 0.4f, 0.45f), rot, 4, 7, 0.6f, 0.7f, 0.06f, i + 4f);
                AddChunkyBlob(kit, 0, pos + rot * new Vector3(0f, -0.10f, 0.65f), new Vector3(nw + 0.4f, 0.24f, 1.6f), rot, 4, 7, 0.4f, 0.5f, 0.03f, i + 5f);
            }

            // 海の口のトンネルアーチ（7時方向、幅 2.8m・高さ 2.5m）
            Vector3 seaMouth = new Vector3(-2.5f, 2.0f, -4.4f);
            AddChunkyBlob(kit, 1, seaMouth + new Vector3(-1.55f, 1.2f, -0.6f), new Vector3(0.8f, 2.6f, 2.4f), Quaternion.identity, 5, 7);
            AddChunkyBlob(kit, 1, seaMouth + new Vector3( 1.55f, 1.2f, -0.6f), new Vector3(0.8f, 2.6f, 2.4f), Quaternion.identity, 5, 7);
            AddChunkyBlob(kit, 0, seaMouth + new Vector3( 0.00f, 2.65f, -0.6f), new Vector3(3.8f, 0.8f, 2.4f), Quaternion.identity, 5, 7);

            // 天窓の縁の鍾乳石（直径 2.5m、中心 (1.5, 7.5, 0)）
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Vector3 p = new Vector3(1.5f + Mathf.Cos(a) * 1.35f, 7.45f, Mathf.Sin(a) * 1.35f);
                AddChunkyBlob(kit, 0, p, new Vector3(0.65f, 0.9f, 0.65f), Quaternion.Euler(0f, i * 36f, 0f), 4, 6);
                if (i % 2 == 0)
                    AddChunkyBlob(kit, 2, p + Vector3.up * 0.45f, new Vector3(0.75f, 0.30f, 0.75f), Quaternion.identity, 3, 6);
            }

            var shellGo = CreateMeshChild("CenoteBase_Shell", null, Vector3.zero, Quaternion.identity, kit, BaseDir, mats, M_LIMESTONE, M_LIMESTONE_WET, M_MOSS);
            AddRoomAnchors(shellGo.transform);
            SaveGoPrefab(shellGo, $"{BaseDir}/CenoteBase_Shell.prefab");

            // 2. CenoteBase_ShellOuter（外側の石灰岩の小島、ずんぐりした岩塊の組み合わせ）
            var outKit = new NatureMeshKit(3);
            var rnd = new System.Random(42);
            for (int i = 0; i < 16; i++)
            {
                float h = i * 12f / 16f;
                // 南西の海の口と南東のデッキ前は開ける
                if (h > 5.2f && h < 7.6f) continue;
                Vector3 p = ClockPos(h, 7.6f + (i % 2) * 0.8f, 3.8f);
                Vector3 sz = new Vector3(4.6f, 7.8f + (i % 3) * 0.7f, 4.2f);
                AddChunkyBlob(outKit, 0, p, sz, Quaternion.Euler(0f, h * 30f, 0f), 5, 7, 0.65f, 0.75f, 0.08f, i);
                // 海面近くの濡れた岩裾
                AddChunkyBlob(outKit, 1, new Vector3(p.x * 1.12f, 0.6f, p.z * 1.12f), new Vector3(5.2f, 2.0f, 4.8f), Quaternion.Euler(0f, h * 30f, 0f), 4, 7, 0.6f, 0.7f, 0.08f, i + 20);
                // 頂上の苔・低木塊
                if (i % 2 == 0)
                    AddChunkyBlob(outKit, 2, p + new Vector3(0f, sz.y * 0.46f, 0f), new Vector3(3.4f, 0.9f, 3.2f), Quaternion.Euler(0f, i * 25f, 0f), 4, 6, 0.65f, 0.75f, 0.09f, i + 40);
            }
            // 海の口の上の崖（鳥の巣が乗る岩棚：(-2.5, 7.3, -6.3)）と南側の外壁岩柱
            AddChunkyBlob(outKit, 0, new Vector3(-2.5f, 6.2f, -5.8f), new Vector3(4.4f, 2.4f, 3.2f), Quaternion.identity, 5, 7, 0.55f, 0.7f, 0.06f, 99f);
            AddChunkyBlob(outKit, 0, new Vector3(-0.6f, 4.2f, -6.8f), new Vector3(2.2f, 6.2f, 2.0f), Quaternion.identity, 5, 7, 0.55f, 0.7f, 0.06f, 98f);
            AddChunkyBlob(outKit, 2, new Vector3(-2.5f, 7.42f, -6.2f), new Vector3(2.4f, 0.25f, 2.2f), Quaternion.identity, 4, 7, 0.5f, 0.6f, 0.05f, 100f);

            var outerGo = CreateMeshChild("CenoteBase_ShellOuter", null, Vector3.zero, Quaternion.identity, outKit, BaseDir, mats, M_LIMESTONE, M_LIMESTONE_WET, M_MOSS);
            SaveGoPrefab(outerGo, $"{BaseDir}/CenoteBase_ShellOuter.prefab");
        }

        static void AddRoomAnchors(Transform shell)
        {
            // CenoteBaseBuilder.cs と同じ名前：CenoteBase_Anchor_<Room>
            // 部屋のローカル座標は +Z が部屋の奥、+Y が上、+X が右
            MakeAnchor(shell, "MapTable",   new Vector3(-1.8f, 2.0f,  0.5f), Quaternion.identity);
            MakeAnchor(shell, "Nursery",    new Vector3( 1.8f, 2.0f,  1.9f), Quaternion.identity);
            MakeAnchor(shell, "Deck",       new Vector3( 1.6f, 4.6f, -7.2f), Quaternion.identity);
            MakeAnchor(shell, "Pier",       new Vector3(-2.5f, 2.0f, -5.8f), Quaternion.identity);

            MakeWallAnchor(shell, "Trade",      6.00f, 4.3f, 2.0f);
            MakeWallAnchor(shell, "Dye",        4.00f, 4.3f, 2.0f);
            MakeWallAnchor(shell, "Collection", 12.0f, 4.6f, 4.6f);
            MakeWallAnchor(shell, "Workshop",   1.50f, 4.6f, 4.6f);
            MakeWallAnchor(shell, "Drying",     3.00f, 4.6f, 4.6f);
            MakeWallAnchor(shell, "Refinery",   4.25f, 4.6f, 4.6f);
        }

        static void MakeAnchor(Transform parent, string room, Vector3 pos, Quaternion rot)
        {
            var go = new GameObject("CenoteBase_Anchor_" + room);
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(pos, rot);
        }

        static void MakeWallAnchor(Transform parent, string room, float clockHour, float radius, float y)
        {
            Vector3 dir = ClockDir(clockHour);
            MakeAnchor(parent, room, ClockPos(clockHour, radius, y), Quaternion.LookRotation(dir, Vector3.up));
        }

        static void BuildFloorAndPool(Material[] mats)
        {
            var kit = new NatureMeshKit(3); // 0: Limestone, 1: LimestoneWet, 2: Water
            // 1階の床（Y=2.0m、直径約 9.6m の平たい岩盤）
            AddChunkyBlob(kit, 0, new Vector3(-0.6f, 1.75f, 0.0f), new Vector3(9.6f, 0.52f, 9.6f), Quaternion.identity, 5, 16, 0.35f, 0.65f, 0.03f, 7f);
            // 海の口への通路床
            AddPlank(kit, 1, new Vector3(-2.5f, 1.88f, -4.6f), new Vector3(2.9f, 0.26f, 3.2f), Quaternion.identity);

            // 東の池（中心 (1.8, 2.0, 0)、半径 1.6m）の濡れた石縁と水面
            Vector3 poolC = new Vector3(1.8f, 2.0f, 0.0f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                Vector3 rimP = poolC + new Vector3(Mathf.Cos(a) * 1.65f, 0.02f, Mathf.Sin(a) * 1.65f);
                AddChunkyBlob(kit, 1, rimP, new Vector3(0.85f, 0.22f, 0.70f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), 4, 6, 0.55f, 0.7f, 0.05f, i);
            }
            // 池の水面
            AddChunkyBlob(kit, 2, poolC + new Vector3(0f, -0.02f, 0f), new Vector3(3.1f, 0.08f, 3.1f), Quaternion.identity, 3, 12, 0.3f, 0.8f, 0.01f, 0f);
            // 染め場（4時方向）へつながる細い水路
            AddPlank(kit, 2, new Vector3(2.8f, 1.99f, -1.1f), new Vector3(0.35f, 0.06f, 1.8f), Quaternion.Euler(0f, -52f, 0f));

            var go = CreateMeshChild("CenoteBase_Floor", null, Vector3.zero, Quaternion.identity, kit, BaseDir, mats, M_LIMESTONE, M_LIMESTONE_WET, M_WATER);
            SaveGoPrefab(go, $"{BaseDir}/CenoteBase_Floor.prefab");
        }

        static void BuildRamp(Material[] mats)
        {
            var kit = new NatureMeshKit(2); // 0: Driftwood, 1: Vine
            // 西の壁沿いに 8時 (Y=2.0) から時計回りに 11時 (Y=4.6) へ登る流木の坂（幅 1.1m、22段の太い流木板）
            int steps = 22;
            float rMid = 4.15f;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float h = Mathf.Lerp(7.85f, 11.05f, t);
                float y = Mathf.Lerp(2.02f, 4.60f, t);
                Vector3 pos = ClockPos(h, rMid, y);
                Vector3 radDir = ClockDir(h);
                Vector3 tanDir = new Vector3(radDir.z, 0f, -radDir.x); // 時計回り接線
                Quaternion rot = Quaternion.LookRotation(tanDir, Vector3.up);
                AddPlank(kit, 0, pos, new Vector3(1.15f, 0.14f, 0.42f), rot, 0f, 7);

                // 4段ごとに内側の手すり柱と壁への斜め支柱
                if (i % 4 == 0 || i == steps)
                {
                    Vector3 innerP = ClockPos(h, rMid - 0.52f, y);
                    AddLog(kit, 0, innerP, innerP + Vector3.up * 0.45f, 0.065f, 0.055f, 6);
                    AddLog(kit, 0, pos, ClockPos(h, rMid + 0.65f, y - 0.75f), 0.08f, 0.07f, 6);
                }
            }
            // 内側のつるの手すりロープ
            var railRows = new List<Vector3>();
            for (int i = 0; i <= 32; i++) railRows.Add(new Vector3(i / 32f, 1f, 1));
            kit.BeginTube();
            kit.Tube(t => ClockPos(Mathf.Lerp(7.85f, 11.05f, t), rMid - 0.52f, Mathf.Lerp(2.02f, 4.60f, t) + 0.38f), _ => 0.032f, railRows, 14, 1, 1);

            var go = CreateMeshChild("CenoteBase_Ramp", null, Vector3.zero, Quaternion.identity, kit, BaseDir, mats, M_DRIFTWOOD, M_VINE);
            SaveGoPrefab(go, $"{BaseDir}/CenoteBase_Ramp.prefab");
        }

        static void BuildGallery(Material[] mats)
        {
            var kit = new NatureMeshKit(1); // Driftwood
            // 11時から時計回りに 12時 → 3時 → 5時20分 までの馬蹄形回廊（Y=4.6m、幅 1.6m、中心半径 3.9m）
            int planks = 36;
            float rMid = 3.95f;
            for (int i = 0; i <= planks; i++)
            {
                float t = i / (float)planks;
                float h = Mathf.Lerp(-1.0f, 5.35f, t); // -1.0 (11時) .. 5.35 (5時20分)
                Vector3 pos = ClockPos(h, rMid, 4.54f);
                Vector3 radDir = ClockDir(h);
                Quaternion rot = Quaternion.LookRotation(radDir, Vector3.up);
                AddPlank(kit, 0, pos, new Vector3(0.48f, 0.16f, 1.68f), rot, 0f, 7);

                if (i % 4 == 0)
                {
                    // 壁から支える太い斜め流木（池の中には柱を立てない）
                    AddLog(kit, 0, ClockPos(h, 4.85f, 3.35f), ClockPos(h, 3.25f, 4.50f), 0.11f, 0.09f, 6);
                }
            }
            // 3時30分の飛び込み板（池の上へ 0.85m 突き出す太い板）
            Vector3 divePos = ClockPos(3.5f, 2.85f, 4.58f);
            Quaternion diveRot = Quaternion.LookRotation(-ClockDir(3.5f), Vector3.up);
            AddPlank(kit, 0, divePos, new Vector3(0.68f, 0.14f, 1.35f), diveRot, 0.02f, 7);

            var galGo = CreateMeshChild("CenoteBase_Gallery", null, Vector3.zero, Quaternion.identity, kit, BaseDir, mats, M_DRIFTWOOD);
            SaveGoPrefab(galGo, $"{BaseDir}/CenoteBase_Gallery.prefab");

            // 回廊の手すり（CenoteBase_GalleryRail：3時30分の飛び込み板の所だけ開ける）
            var railKit = new NatureMeshKit(2); // 0: Driftwood, 1: Vine
            float rInner = 3.20f;
            for (int i = 0; i <= 18; i++)
            {
                float h = Mathf.Lerp(-0.95f, 5.20f, i / 18f);
                if (h > 3.20f && h < 3.80f) continue; // 飛び込み板の開口
                Vector3 p = ClockPos(h, rInner, 4.60f);
                AddLog(railKit, 0, p, p + Vector3.up * 0.42f, 0.065f, 0.055f, 6);
            }
            AddArcRope(railKit, 1, -0.95f, 3.20f, rInner, 4.96f, 0.032f);
            AddArcRope(railKit, 1,  3.80f, 5.20f, rInner, 4.96f, 0.032f);

            var railGo = CreateMeshChild("CenoteBase_GalleryRail", null, Vector3.zero, Quaternion.identity, railKit, BaseDir, mats, M_DRIFTWOOD, M_VINE);
            SaveGoPrefab(railGo, $"{BaseDir}/CenoteBase_GalleryRail.prefab");
        }

        static void AddArcRope(NatureMeshKit kit, int sub, float h0, float h1, float r, float y, float tubeR)
        {
            var rows = new List<Vector3>();
            for (int i = 0; i <= 28; i++) rows.Add(new Vector3(i / 28f, 1f, sub));
            kit.BeginTube();
            kit.Tube(t => ClockPos(Mathf.Lerp(h0, h1, t), r, y - 0.03f * Mathf.Sin(t * Mathf.PI * 4f)), _ => tubeR, rows, 14, sub, sub);
        }

        static void BuildPierAndBoat(Material[] mats)
        {
            // 桟橋（CenoteBase_Pier：X=-2.5, Y=2.0, Z=-5.6〜-11.6 の 3.0×6.0m）
            var kit = new NatureMeshKit(2); // 0: Driftwood, 1: Coir
            for (int i = 0; i < 12; i++)
            {
                float z = Mathf.Lerp(-5.8f, -11.5f, i / 11f);
                AddPlank(kit, 0, new Vector3(-2.5f, 1.94f, z), new Vector3(3.05f, 0.16f, 0.50f), Quaternion.Euler(0f, (i % 2 - 0.5f) * 1.5f, 0f), 0f, 7);
            }
            // 海底から立つ太い流木の杭 6本
            float[] pz = { -6.2f, -8.8f, -11.2f };
            foreach (float z in pz)
            {
                AddLog(kit, 0, new Vector3(-3.85f, -0.4f, z), new Vector3(-3.85f, 2.35f, z), 0.15f, 0.12f, 7);
                AddLog(kit, 0, new Vector3(-1.15f, -0.4f, z), new Vector3(-1.15f, 2.15f, z), 0.15f, 0.12f, 7);
            }
            // 片側（西側 X=-3.85）の低い縄手すり
            AddLog(kit, 1, new Vector3(-3.85f, 2.30f, -6.2f), new Vector3(-3.85f, 2.30f, -11.2f), 0.038f, 0.038f, 6, -0.06f, Vector3.up);

            var pierGo = CreateMeshChild("CenoteBase_Pier", null, Vector3.zero, Quaternion.identity, kit, BaseDir, mats, M_DRIFTWOOD, M_COIR);
            SaveGoPrefab(pierGo, $"{BaseDir}/CenoteBase_Pier.prefab");

            // 商人の小舟（CenoteBase_Boat：桟橋の西脇 X=-4.9, Z=-8.8 に浮かぶアウトリガーカヌー）
            var boatKit = new NatureMeshKit(3); // 0: DriftwoodDark, 1: Driftwood, 2: Coir
            Vector3 bc = new Vector3(-4.95f, 0.15f, -8.8f);
            AddVessel(boatKit, 0, 1, bc, Quaternion.identity, 0.45f, 0.65f, 0.48f, 0.08f, 10);
            // 船体を前後に伸ばした丸太の舳先・艫とアウトリガー浮き木
            AddChunkyBlob(boatKit, 0, bc + new Vector3(0f, 0.22f, 0f), new Vector3(0.95f, 0.42f, 3.2f), Quaternion.identity, 4, 8, 0.6f, 0.7f, 0.04f, 5f, 0.25f);
            AddLog(boatKit, 1, bc + new Vector3(-1.15f, 0.05f, -1.1f), bc + new Vector3(-1.15f, 0.05f, 1.1f), 0.11f, 0.11f, 6);
            AddLog(boatKit, 1, bc + new Vector3(0f, 0.32f, -0.6f), bc + new Vector3(-1.15f, 0.12f, -0.6f), 0.05f, 0.04f, 6);
            AddLog(boatKit, 1, bc + new Vector3(0f, 0.32f,  0.6f), bc + new Vector3(-1.15f, 0.12f,  0.6f), 0.05f, 0.04f, 6);

            var boatGo = CreateMeshChild("CenoteBase_Boat", null, Vector3.zero, Quaternion.identity, boatKit, BaseDir, mats, M_DRIFTWOOD_DARK, M_DRIFTWOOD, M_COIR);
            SaveGoPrefab(boatGo, $"{BaseDir}/CenoteBase_Boat.prefab");
        }

        static void BuildDeckAndFrame(Material[] mats)
        {
            // 2階デッキ（CenoteBase_Deck：Y=4.6m、5時30分の開口から南へ張り出し、東側の鳥の発着場 (4.4, 4.6, -8.5) までつながる広い流木デッキ）
            var kit = new NatureMeshKit(2); // 0: Driftwood, 1: Vine
            // メイン展望デッキ (X: 0.0〜3.2, Z: -5.4〜-8.8) ＋ 東の発着ステージ (X: 2.8〜6.0, Z: -6.8〜-10.2)
            for (int i = 0; i < 8; i++)
            {
                float z = Mathf.Lerp(-5.5f, -8.7f, i / 7f);
                AddPlank(kit, 0, new Vector3(1.6f, 4.52f, z), new Vector3(3.4f, 0.18f, 0.48f), Quaternion.identity, 0f, 7);
            }
            for (int i = 0; i < 8; i++)
            {
                float z = Mathf.Lerp(-6.9f, -10.1f, i / 7f);
                AddPlank(kit, 0, new Vector3(4.4f, 4.52f, z), new Vector3(3.2f, 0.18f, 0.48f), Quaternion.identity, 0f, 7);
            }
            // 崖への太い斜め支柱
            AddLog(kit, 0, new Vector3(0.2f, 2.2f, -5.4f), new Vector3(0.2f, 4.48f, -8.4f), 0.14f, 0.11f, 7);
            AddLog(kit, 0, new Vector3(3.0f, 2.2f, -5.4f), new Vector3(3.0f, 4.48f, -8.4f), 0.14f, 0.11f, 7);
            AddLog(kit, 0, new Vector3(5.6f, 2.0f, -6.8f), new Vector3(5.6f, 4.48f, -9.8f), 0.14f, 0.11f, 7);
            // 低い手すり（西と北の縁）
            AddLog(kit, 0, new Vector3(0.05f, 4.6f, -8.6f), new Vector3(0.05f, 5.02f, -8.6f), 0.07f, 0.06f, 6);
            AddLog(kit, 0, new Vector3(0.05f, 4.6f, -6.0f), new Vector3(0.05f, 5.02f, -6.0f), 0.07f, 0.06f, 6);
            AddLog(kit, 1, new Vector3(0.05f, 4.95f, -6.0f), new Vector3(0.05f, 4.95f, -8.6f), 0.035f, 0.035f, 6);

            var deckGo = CreateMeshChild("CenoteBase_Deck", null, Vector3.zero, Quaternion.identity, kit, BaseDir, mats, M_DRIFTWOOD, M_VINE);
            SaveGoPrefab(deckGo, $"{BaseDir}/CenoteBase_Deck.prefab");

            // 鳥の籠の着き場の四角いヤシ縄枠（CenoteBase_DeckFrame：中心 (4.4, 4.6, -8.5)、2.6m 角）
            var frameKit = new NatureMeshKit(1); // Coir
            Vector3 fc = new Vector3(4.4f, 4.63f, -8.5f);
            float hs = 1.30f;
            AddLog(frameKit, 0, fc + new Vector3(-hs, 0f, -hs), fc + new Vector3( hs, 0f, -hs), 0.065f, 0.065f, 7);
            AddLog(frameKit, 0, fc + new Vector3( hs, 0f, -hs), fc + new Vector3( hs, 0f,  hs), 0.065f, 0.065f, 7);
            AddLog(frameKit, 0, fc + new Vector3( hs, 0f,  hs), fc + new Vector3(-hs, 0f,  hs), 0.065f, 0.065f, 7);
            AddLog(frameKit, 0, fc + new Vector3(-hs, 0f,  hs), fc + new Vector3(-hs, 0f, -hs), 0.065f, 0.065f, 7);

            var frameGo = CreateMeshChild("CenoteBase_DeckFrame", null, Vector3.zero, Quaternion.identity, frameKit, BaseDir, mats, M_COIR);
            SaveGoPrefab(frameGo, $"{BaseDir}/CenoteBase_DeckFrame.prefab");
        }

        static void BuildLiftParts(Material[] mats)
        {
            // CenoteLift.cs の2つの昇降位置：(2.05, y, -1.99) と (0.61, y, -2.47)、滑車高さ Y=6.6m
            Vector3 p0 = new Vector3(2.05f, 6.65f, -1.99f);
            Vector3 p1 = new Vector3(0.61f, 6.65f, -2.47f);

            var kit = new NatureMeshKit(3); // 0: Driftwood, 1: DriftwoodDark, 2: Coir
            // 天井近くの太い梁と2つの大きな木製滑車
            AddLog(kit, 0, p0 + new Vector3(0.9f, 0.25f, 0.4f), p1 + new Vector3(-0.9f, 0.25f, -0.4f), 0.16f, 0.15f, 7);
            AddPlank(kit, 1, p0, new Vector3(0.18f, 0.55f, 0.55f), Quaternion.Euler(0f, -20f, 0f), 0f, 8);
            AddPlank(kit, 1, p1, new Vector3(0.18f, 0.55f, 0.55f), Quaternion.Euler(0f, -20f, 0f), 0f, 8);
            AddLog(kit, 2, p0 + Vector3.up * 0.22f, p1 + Vector3.up * 0.22f, 0.035f, 0.035f, 6);

            var liftGo = CreateMeshChild("CenoteBase_Lift", null, Vector3.zero, Quaternion.identity, kit, BaseDir, mats, M_DRIFTWOOD, M_DRIFTWOOD_DARK, M_COIR);
            SaveGoPrefab(liftGo, $"{BaseDir}/CenoteBase_Lift.prefab");

            // 昇降ゴンドラ板（CenoteBase_LiftBoard：直径 0.95m の丸い流木板＋4本の吊り縄が Y=0.75m の輪に集まる）
            var bKit = new NatureMeshKit(2); // 0: Driftwood, 1: Vine
            AddChunkyBlob(bKit, 0, new Vector3(0f, 0.05f, 0f), new Vector3(0.96f, 0.12f, 0.96f), Quaternion.identity, 4, 10, 0.4f, 0.8f, 0.02f, 3f);
            AddTorus(bKit, 1, new Vector3(0f, 0.12f, 0f), Quaternion.identity, 0.44f, 0.44f, 0.04f, 10, 6);
            Vector3 topRing = new Vector3(0f, 0.75f, 0f);
            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 0.5f;
                AddLog(bKit, 1, new Vector3(Mathf.Cos(a) * 0.42f, 0.10f, Mathf.Sin(a) * 0.42f), topRing, 0.022f, 0.020f, 5);
            }
            AddTorus(bKit, 1, topRing, Quaternion.Euler(90f, 0f, 0f), 0.07f, 0.07f, 0.025f, 8, 5);
            var boardGo = CreateMeshChild("CenoteBase_LiftBoard", null, Vector3.zero, Quaternion.identity, bKit, BaseDir, mats, M_DRIFTWOOD, M_VINE);
            SaveGoPrefab(boardGo, $"{BaseDir}/CenoteBase_LiftBoard.prefab");

            // 縦のロープ（CenoteBase_LiftRope：原点 (0,0,0) から下 (0,-1,0) へ長さ 1m。CenoteLift.cs が Y スケールで伸縮する）
            var rKit = new NatureMeshKit(1);
            AddLog(rKit, 0, Vector3.zero, new Vector3(0f, -1.0f, 0f), 0.026f, 0.026f, 6);
            var ropeGo = CreateMeshChild("CenoteBase_LiftRope", null, Vector3.zero, Quaternion.identity, rKit, BaseDir, mats, M_COIR);
            SaveGoPrefab(ropeGo, $"{BaseDir}/CenoteBase_LiftRope.prefab");
        }

        static void BuildCaveColliders(Material[] mats)
        {
            // 当たり判定用のシンプルなメッシュプレハブ (*_COL)
            MakeBoxColPrefab("CenoteBase_Floor_COL",   new Vector3(0f, 1.90f, 0f),       new Vector3(10.5f, 0.20f, 10.5f), mats[M_LIMESTONE]);
            MakeBoxColPrefab("CenoteBase_Pier_COL",    new Vector3(-2.5f, 1.90f, -8.2f), new Vector3(3.1f, 0.20f, 7.2f),   mats[M_DRIFTWOOD]);
            MakeBoxColPrefab("CenoteBase_Deck_COL",    new Vector3(3.0f, 4.50f, -7.8f),  new Vector3(6.2f, 0.20f, 5.0f),   mats[M_DRIFTWOOD]);
            MakeBoxColPrefab("CenoteBase_Gallery_COL", new Vector3(1.8f, 4.50f, 1.2f),   new Vector3(7.5f, 0.20f, 7.5f),   mats[M_DRIFTWOOD]);
            MakeBoxColPrefab("CenoteBase_Ramp_COL",    new Vector3(-3.8f, 3.30f, 0.5f),  new Vector3(1.4f, 0.20f, 5.5f),   mats[M_DRIFTWOOD]);
            MakeBoxColPrefab("CenoteBase_Shell_COL",   new Vector3(0f, 4.5f, 5.2f),      new Vector3(9.5f, 5.0f, 0.5f),    mats[M_LIMESTONE]);
            MakeBoxColPrefab("CenoteBase_Rail_COL",    new Vector3(0.05f, 4.8f, -7.3f),  new Vector3(0.2f, 0.5f, 2.8f),    mats[M_VINE]);
        }

        static void MakeBoxColPrefab(string name, Vector3 center, Vector3 size, Material mat)
        {
            var kit = new NatureMeshKit(1);
            AddPlank(kit, 0, center, size, Quaternion.identity, 0f, 6);
            var go = CreateMeshChild(name, null, Vector3.zero, Quaternion.identity, kit, BaseDir, new[] { mat }, 0);
            SaveGoPrefab(go, $"{BaseDir}/{name}.prefab");
        }

        public static void SaveGoPrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
    }
}
