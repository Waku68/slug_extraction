using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    public static partial class CenoteBaseCodeBuilder
    {
        public static void BuildFurniturePrefabs(Material[] mats)
        {
            System.IO.Directory.CreateDirectory(FurnDir);

            BuildKitPrefab(mats);
            BuildBirdBasketAndNest(mats);
            BuildCaveLamps(mats);

            BuildRoomMapTable(mats);
            BuildRoomTrade(mats);
            BuildRoomNursery(mats);
            BuildRoomDye(mats);
            BuildRoomCollection(mats);
            BuildRoomWorkshop(mats);
            BuildRoomDrying(mats);
            BuildRoomRefinery(mats);
            BuildRoomDeck(mats);
            BuildRoomPier(mats);
        }

        // ---- 部品箱（12種の基本パーツ） ----
        static void BuildKitPrefab(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Kit");
            // 1. Plank, 2. Post, 3. Disc, 4. VineLoop, 5. CoirRope, 6. ClayJar, 7. Basket, 8. SaltBin, 9. Tag, 10. ConchLamp, 11. BrassWallLamp, 12. Pile
            var k1 = new NatureMeshKit(1); AddPlank(k1, 0, new Vector3(0f, 0.08f, 0f), new Vector3(1.2f, 0.14f, 0.36f), Quaternion.identity);
            CreateMeshChild("Kit_01_Plank", root.transform, new Vector3(-5.5f, 0f, 0f), Quaternion.identity, k1, FurnDir, mats, M_DRIFTWOOD);

            var k2 = new NatureMeshKit(1);
            AddLog(k2, 0, Vector3.zero, new Vector3(0f, 0.7f, 0f), 0.08f, 0.07f, 6);
            AddLog(k2, 0, new Vector3(0f, 0.55f, 0f), new Vector3(-0.2f, 0.9f, 0f), 0.06f, 0.05f, 6);
            AddLog(k2, 0, new Vector3(0f, 0.55f, 0f), new Vector3( 0.2f, 0.9f, 0f), 0.06f, 0.05f, 6);
            CreateMeshChild("Kit_02_YPost", root.transform, new Vector3(-4.5f, 0f, 0f), Quaternion.identity, k2, FurnDir, mats, M_DRIFTWOOD);

            var k3 = new NatureMeshKit(1); AddChunkyBlob(k3, 0, new Vector3(0f, 0.2f, 0f), new Vector3(0.4f, 0.4f, 0.12f), Quaternion.identity, 4, 8);
            CreateMeshChild("Kit_03_Disc", root.transform, new Vector3(-3.5f, 0f, 0f), Quaternion.identity, k3, FurnDir, mats, M_DRIFTWOOD);

            var k4 = new NatureMeshKit(1); AddTorus(k4, 0, new Vector3(0f, 0.2f, 0f), Quaternion.identity, 0.22f, 0.22f, 0.04f, 8, 6);
            CreateMeshChild("Kit_04_VineLoop", root.transform, new Vector3(-2.5f, 0f, 0f), Quaternion.identity, k4, FurnDir, mats, M_VINE);

            var k5 = new NatureMeshKit(1); AddLog(k5, 0, Vector3.zero, new Vector3(0f, 0.8f, 0f), 0.045f, 0.045f, 6);
            CreateMeshChild("Kit_05_CoirRope", root.transform, new Vector3(-1.5f, 0f, 0f), Quaternion.identity, k5, FurnDir, mats, M_COIR);

            var k6 = new NatureMeshKit(1); AddClayJar(k6, 0, Vector3.zero, 0.22f, 0.45f, true);
            CreateMeshChild("Kit_06_ClayJar", root.transform, new Vector3(-0.5f, 0f, 0f), Quaternion.identity, k6, FurnDir, mats, M_CLAY);

            var k7 = new NatureMeshKit(2); AddVessel(k7, 0, 1, Vector3.zero, Quaternion.identity, 0.24f, 0.28f, 0.35f, 0.05f, 8);
            CreateMeshChild("Kit_07_BasketM", root.transform, new Vector3(0.5f, 0f, 0f), Quaternion.identity, k7, FurnDir, mats, M_VINE, M_RUG);

            var k8 = new NatureMeshKit(2); AddSaltBinMesh(k8, 0, 1, Vector3.zero);
            CreateMeshChild("Kit_08_SaltBin", root.transform, new Vector3(1.5f, 0f, 0f), Quaternion.identity, k8, FurnDir, mats, M_DRIFTWOOD, M_SALT);

            var k9 = new NatureMeshKit(2);
            AddPlank(k9, 0, new Vector3(0f, 0.15f, 0f), new Vector3(0.22f, 0.14f, 0.03f), Quaternion.identity);
            AddTorus(k9, 1, new Vector3(0f, 0.23f, 0f), Quaternion.Euler(90f, 0f, 0f), 0.04f, 0.04f, 0.015f, 6, 5);
            CreateMeshChild("Kit_09_Tag", root.transform, new Vector3(2.5f, 0f, 0f), Quaternion.identity, k9, FurnDir, mats, M_BARK, M_VINE);

            var k10 = new NatureMeshKit(2); AddConchLampMesh(k10, 0, 1, Vector3.zero);
            CreateMeshChild("Kit_10_ConchLamp", root.transform, new Vector3(3.5f, 0f, 0f), Quaternion.identity, k10, FurnDir, mats, M_CONCH, M_FLAME);

            var k11 = new NatureMeshKit(3); AddBrassWallLampMesh(k11, 0, 1, 2, new Vector3(0f, 0.35f, 0f), Quaternion.identity);
            CreateMeshChild("Kit_11_BrassLamp", root.transform, new Vector3(4.5f, 0f, 0f), Quaternion.identity, k11, FurnDir, mats, M_BRASS, M_FLAME, M_VINE);

            var k12 = new NatureMeshKit(1); AddPileMesh(k12, 0, Vector3.zero, 0.50f, 0.45f);
            CreateMeshChild("Kit_12_Pile", root.transform, new Vector3(5.5f, 0f, 0f), Quaternion.identity, k12, FurnDir, mats, M_PILE);

            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Kit.prefab");
        }

        // ---- 鳥の四角い籠（CenoteFurniture_BirdBasket）と崖の巣（CenoteFurniture_Nest） ----
        static void BuildBirdBasketAndNest(Material[] mats)
        {
            // 四角い籠：内寸 2.4×2.4m、壁高さ 0.40m、四隅の流木柱＋上縁レール＋つる編みの壁＋4本のヤシ縄が上 Y=1.60m の輪に集まる
            var bKit = new NatureMeshKit(3); // 0: Driftwood, 1: Vine, 2: Coir
            float half = 1.20f, wallH = 0.40f, ringY = 1.60f;
            // 厚みのある四角い底板
            AddPlank(bKit, 0, new Vector3(0f, 0.04f, 0f), new Vector3(2.52f, 0.08f, 2.52f), Quaternion.identity, 0f, 8);
            AddPlank(bKit, 1, new Vector3(0f, 0.085f, 0f), new Vector3(2.28f, 0.03f, 2.28f), Quaternion.identity, 0f, 8);

            // 四隅の太い流木柱と上へ集まるヤシ縄
            Vector3 ringCenter = new Vector3(0f, ringY, 0f);
            Vector3[] corners =
            {
                new Vector3(-half, 0f, -half), new Vector3( half, 0f, -half),
                new Vector3( half, 0f,  half), new Vector3(-half, 0f,  half),
            };
            for (int i = 0; i < 4; i++)
            {
                Vector3 c = corners[i];
                Vector3 cNext = corners[(i + 1) % 4];
                AddLog(bKit, 0, c, c + Vector3.up * (wallH + 0.08f), 0.11f, 0.095f, 7);
                // 上縁の流木レール
                AddLog(bKit, 0, c + Vector3.up * wallH, cNext + Vector3.up * wallH, 0.075f, 0.075f, 6);
                // つるの壁（3段の太い編み帯）
                for (int r = 1; r <= 3; r++)
                {
                    float y = r * 0.105f;
                    AddLog(bKit, 1, c + Vector3.up * y, cNext + Vector3.up * y, 0.052f, 0.052f, 6, 0.03f * (r % 2 == 0 ? 1f : -1f), (c + cNext).normalized);
                }
                // 四隅から上の輪（Y=1.60m）へ集まる太いヤシ縄
                AddLog(bKit, 2, c + Vector3.up * (wallH + 0.05f), ringCenter + new Vector3(c.x * 0.10f, -0.04f, c.z * 0.10f), 0.045f, 0.040f, 6, -0.06f, Vector3.up);
            }
            // いちばん上の太い編み輪（鳥の足＝原点がここをつかむ、高さ 1.60m）
            AddTorus(bKit, 1, ringCenter, Quaternion.identity, 0.24f, 0.24f, 0.065f, 10, 7);

            var basketGo = CreateMeshChild("CenoteFurniture_BirdBasket", null, Vector3.zero, Quaternion.identity, bKit, FurnDir, mats, M_DRIFTWOOD, M_VINE, M_COIR);
            SaveGoPrefab(basketGo, $"{FurnDir}/CenoteFurniture_BirdBasket.prefab");

            // 鳥の巣（CenoteFurniture_Nest：直径 1.6m の太い流木とつるの皿形＋白い羽）
            var nKit = new NatureMeshKit(3); // 0: Driftwood, 1: Vine, 2: Feather
            AddVessel(nKit, 1, 1, Vector3.zero, Quaternion.identity, 0.65f, 0.85f, 0.36f, 0.18f, 10);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Quaternion rot = Quaternion.Euler(12f * Mathf.Sin(i * 2f), i * 36f + 78f, 0f);
                AddPlank(nKit, 0, new Vector3(Mathf.Cos(a) * 0.72f, 0.22f, Mathf.Sin(a) * 0.72f), new Vector3(0.22f, 0.16f, 0.68f), rot, 0f, 6);
            }
            // 中の柔らかい羽 3枚
            AddPlank(nKit, 2, new Vector3(-0.18f, 0.12f,  0.10f), new Vector3(0.18f, 0.05f, 0.36f), Quaternion.Euler(5f,  25f, 0f), 0f, 6);
            AddPlank(nKit, 2, new Vector3( 0.20f, 0.12f, -0.12f), new Vector3(0.16f, 0.05f, 0.32f), Quaternion.Euler(4f, -40f, 0f), 0f, 6);
            AddPlank(nKit, 2, new Vector3( 0.02f, 0.13f,  0.22f), new Vector3(0.15f, 0.05f, 0.30f), Quaternion.Euler(6f,  75f, 0f), 0f, 6);

            var nestGo = CreateMeshChild("CenoteFurniture_Nest", null, Vector3.zero, Quaternion.identity, nKit, FurnDir, mats, M_DRIFTWOOD, M_VINE, M_FEATHER);
            SaveGoPrefab(nestGo, $"{FurnDir}/CenoteFurniture_Nest.prefab");
        }

        // ---- 洞窟内の灯り（CenoteFurniture_CaveLamps） ----
        static void BuildCaveLamps(Material[] mats)
        {
            var kit = new NatureMeshKit(4); // 0: Brass, 1: Flame, 2: Conch, 3: Driftwood
            // 地図テーブル脇の柱ランプ
            AddLog(kit, 3, new Vector3(-3.0f, 2.0f, 0.9f), new Vector3(-3.0f, 3.1f, 0.9f), 0.08f, 0.06f, 6);
            AddBrassWallLampMesh(kit, 0, 1, 3, new Vector3(-2.9f, 2.95f, 0.9f), Quaternion.Euler(0f, 90f, 0f));
            // 坂と回廊のブラケットランプ
            AddBrassWallLampMesh(kit, 0, 1, 3, ClockPos(9.5f, 4.5f, 3.6f), Quaternion.LookRotation(-ClockDir(9.5f), Vector3.up));
            AddBrassWallLampMesh(kit, 0, 1, 3, ClockPos(2.2f, 4.6f, 5.4f), Quaternion.LookRotation(-ClockDir(2.2f), Vector3.up));
            // 1階と回廊の巻貝フロアランプ 5個
            AddConchLampMesh(kit, 2, 1, new Vector3( 0.2f, 2.02f,  1.6f));
            AddConchLampMesh(kit, 2, 1, new Vector3( 0.4f, 2.02f, -2.2f));
            AddConchLampMesh(kit, 2, 1, new Vector3(-2.2f, 2.02f, -3.5f));
            AddConchLampMesh(kit, 2, 1, ClockPos(0.8f, 3.4f, 4.62f));
            AddConchLampMesh(kit, 2, 1, ClockPos(4.8f, 3.4f, 4.62f));

            var go = CreateMeshChild("CenoteFurniture_CaveLamps", null, Vector3.zero, Quaternion.identity, kit, FurnDir, mats, M_BRASS, M_FLAME, M_CONCH, M_DRIFTWOOD);
            SaveGoPrefab(go, $"{FurnDir}/CenoteFurniture_CaveLamps.prefab");
        }

        // ---- 部屋1：MapTable（1階中央西の丸い地図テーブル） ----
        static void BuildRoomMapTable(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_MapTable");

            // 天板は別オブジェクト MapTable_Top（CenoteBaseBuilder.cs が当たり判定をつける）
            var topKit = new NatureMeshKit(1);
            AddChunkyBlob(topKit, 0, new Vector3(0f, 0.36f, 0f), new Vector3(2.16f, 0.08f, 2.16f), Quaternion.identity, 4, 14, 0.35f, 0.85f, 0.01f, 1f);
            CreateMeshChild("MapTable_Top", root.transform, Vector3.zero, Quaternion.identity, topKit, FurnDir, mats, M_MAP);

            // 脚（タコ足のように広がる大流木の根株）＋縁リング＋食事段＋ヤシ殻ボウル4個＋ナメクジ駒4個＋敷物（Rug）
            var kit = new NatureMeshKit(5); // 0: DriftwoodDark, 1: Driftwood, 2: Rug, 3: Coconut, 4:ClothRed
            // 敷物（3.0 × 2.8 × 0.04m）
            AddPlank(kit, 2, new Vector3(0f, 0.02f, 0f), new Vector3(3.0f, 0.04f, 2.8f), Quaternion.identity, 0f, 10);
            // 中央の太い根株と6本のタコ足根
            AddLog(kit, 0, new Vector3(0f, 0f, 0f), new Vector3(0f, 0.35f, 0f), 0.42f, 0.52f, 8);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                AddLog(kit, 0, new Vector3(0f, 0.18f, 0f) + dir * 0.28f, dir * 0.88f + new Vector3(0f, 0.04f, 0f), 0.15f, 0.07f, 6, 0.05f, Vector3.up);
            }
            // テーブル上縁の低い囲い（高さ 0.06m）と1段下の食事リング
            AddTorus(kit, 0, new Vector3(0f, 0.41f, 0f), Quaternion.identity, 1.06f, 1.06f, 0.055f, 14, 6);
            AddTorus(kit, 1, new Vector3(0f, 0.27f, 0f), Quaternion.identity, 1.18f, 1.18f, 0.09f, 14, 6);
            // ヤシ殻ボウル4個と木彫りのナメクジ駒4個
            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 0.5f;
                Vector3 bp = new Vector3(Mathf.Cos(a) * 1.18f, 0.31f, Mathf.Sin(a) * 1.18f);
                AddVessel(kit, 3, 3, bp, Quaternion.identity, 0.07f, 0.10f, 0.08f, 0.02f, 7);
                Vector3 sp = new Vector3(Mathf.Cos(a + 0.3f) * 0.55f, 0.42f, Mathf.Sin(a + 0.3f) * 0.55f);
                AddChunkyBlob(kit, 1, sp, new Vector3(0.08f, 0.07f, 0.14f), Quaternion.Euler(0f, i * 75f, 0f), 4, 6);
                AddChunkyBlob(kit, 4, sp + new Vector3(0.12f, 0.03f, 0f), new Vector3(0.05f, 0.09f, 0.05f), Quaternion.identity, 3, 5);
            }
            CreateMeshChild("MapTable_Body", root.transform, Vector3.zero, Quaternion.identity, kit, FurnDir, mats, M_DRIFTWOOD_DARK, M_DRIFTWOOD, M_RUG, M_COCONUT, M_CLOTH_RED);
            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_MapTable.prefab");
        }

        // ---- 部屋2：Trade（取引コーナー：大天秤・岩カウンター・植物籠） ----
        static void BuildRoomTrade(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Trade");

            // Carved（岩彫りカウンター＋奥の2段岩棚）
            var cKit = new NatureMeshKit(2); // 0: Limestone, 1: Thatch
            AddPlank(cKit, 0, new Vector3(-0.45f, 0.16f, 0.25f), new Vector3(1.55f, 0.32f, 0.55f), Quaternion.identity);
            AddPlank(cKit, 0, new Vector3( 0.00f, 0.18f, 1.10f), new Vector3(2.30f, 0.36f, 0.48f), Quaternion.identity);
            AddPlank(cKit, 0, new Vector3( 0.00f, 0.68f, 1.18f), new Vector3(2.30f, 0.16f, 0.42f), Quaternion.identity);
            // カウンター上のヤシ葉ひさし
            AddPlank(cKit, 1, new Vector3(-0.45f, 1.25f, 0.15f), new Vector3(1.65f, 0.10f, 0.75f), Quaternion.Euler(14f, 0f, 0f));
            CreateMeshChild("Trade_Carved", root.transform, Vector3.zero, Quaternion.identity, cKit, FurnDir, mats, M_LIMESTONE, M_THATCH);

            // S1（主役の大きなY字天秤＋ピボット、塩の延べ棒、帳面、大きな植物籠6個）
            var s1 = new GameObject("Trade_S1").transform; s1.SetParent(root.transform, false);
            var s1Kit = new NatureMeshKit(5); // 0: Driftwood, 1: Vine, 2: Salt, 3: BarkCloth, 4: ClamShell
            Vector3 scalePos = new Vector3(0.68f, 0f, -0.15f);
            AddLog(s1Kit, 0, scalePos, scalePos + new Vector3(0f, 0.72f, 0f), 0.09f, 0.08f, 6);
            AddLog(s1Kit, 0, scalePos + new Vector3(0f, 0.58f, 0f), scalePos + new Vector3(-0.18f, 0.92f, 0f), 0.07f, 0.06f, 6);
            AddLog(s1Kit, 0, scalePos + new Vector3(0f, 0.58f, 0f), scalePos + new Vector3( 0.18f, 0.92f, 0f), 0.07f, 0.06f, 6);
            // 塩の延べ棒4本と帳面
            for (int i = 0; i < 4; i++)
                AddPlank(s1Kit, 2, new Vector3(0.28f, 0.04f + i * 0.06f, -0.10f), new Vector3(0.24f, 0.055f, 0.10f), Quaternion.Euler(0f, i * 6f, 0f));
            AddPlank(s1Kit, 4, new Vector3(-0.45f, 0.36f, 0.25f), new Vector3(0.32f, 0.12f, 0.22f), Quaternion.Euler(-20f, 10f, 0f));
            AddPlank(s1Kit, 3, new Vector3(-0.45f, 0.42f, 0.23f), new Vector3(0.34f, 0.08f, 0.24f), Quaternion.Euler(-20f, 10f, 0f));

            // 棚の上のデカい植物籠 6個（下段3・上段3、中に山盛り Pile 入り）
            for (int i = 0; i < 6; i++)
            {
                float x = (i % 3 - 1) * 0.72f;
                float y = (i / 3) == 0 ? 0.36f : 0.76f;
                Vector3 bp = new Vector3(x, y, 1.08f);
                AddVessel(s1Kit, 1, 1, bp, Quaternion.identity, 0.24f, 0.28f, 0.26f, 0.05f, 8);
                AddPileMesh(s1Kit, 3, bp + Vector3.up * 0.10f, 0.22f, 0.20f);
            }
            CreateMeshChild("Trade_S1_Main", s1, Vector3.zero, Quaternion.identity, s1Kit, FurnDir, mats, M_DRIFTWOOD, M_VINE, M_SALT, M_BARK, M_CLAM);

            // 天秤の動く腕と皿（Trade_BalancePivot, Trade_PanPivot_L, Trade_PanPivot_R）
            var balPivot = new GameObject("Trade_BalancePivot").transform;
            balPivot.SetParent(s1, false);
            balPivot.localPosition = scalePos + new Vector3(0f, 0.88f, 0f);
            var beamKit = new NatureMeshKit(2);
            AddLog(beamKit, 0, new Vector3(-0.55f, 0f, 0f), new Vector3(0.55f, 0f, 0f), 0.05f, 0.05f, 6);
            AddTorus(beamKit, 1, Vector3.zero, Quaternion.Euler(0f, 90f, 0f), 0.07f, 0.07f, 0.025f, 6, 5);
            CreateMeshChild("Trade_Beam", balPivot, Vector3.zero, Quaternion.identity, beamKit, FurnDir, mats, M_DRIFTWOOD, M_VINE);

            for (int side = -1; side <= 1; side += 2)
            {
                var panPiv = new GameObject(side < 0 ? "Trade_PanPivot_L" : "Trade_PanPivot_R").transform;
                panPiv.SetParent(balPivot, false);
                panPiv.localPosition = new Vector3(side * 0.50f, 0f, 0f);
                var panKit = new NatureMeshKit(2);
                AddLog(panKit, 0, Vector3.zero, new Vector3(-0.14f, -0.38f, 0f), 0.018f, 0.018f, 5);
                AddLog(panKit, 0, Vector3.zero, new Vector3( 0.14f, -0.38f, 0f), 0.018f, 0.018f, 5);
                AddVessel(panKit, 1, 1, new Vector3(0f, -0.44f, 0f), Quaternion.identity, 0.16f, 0.22f, 0.08f, 0.03f, 8);
                CreateMeshChild("Pan", panPiv, Vector3.zero, Quaternion.identity, panKit, FurnDir, mats, M_VINE, M_CLAM);
            }

            // S2・S3（籠の嵩上げリング）
            var s2 = new GameObject("Trade_S2").transform; s2.SetParent(root.transform, false);
            var s2Kit = new NatureMeshKit(1);
            var s3 = new GameObject("Trade_S3").transform; s3.SetParent(root.transform, false);
            var s3Kit = new NatureMeshKit(1);
            for (int i = 0; i < 6; i++)
            {
                float x = (i % 3 - 1) * 0.72f, y = (i / 3) == 0 ? 0.36f : 0.76f;
                AddTorus(s2Kit, 0, new Vector3(x, y + 0.28f, 1.08f), Quaternion.identity, 0.27f, 0.27f, 0.045f, 8, 6);
                AddTorus(s3Kit, 0, new Vector3(x, y + 0.35f, 1.08f), Quaternion.identity, 0.28f, 0.28f, 0.045f, 8, 6);
            }
            CreateMeshChild("Trade_S2_Bands", s2, Vector3.zero, Quaternion.identity, s2Kit, FurnDir, mats, M_RUG);
            CreateMeshChild("Trade_S3_Bands", s3, Vector3.zero, Quaternion.identity, s3Kit, FurnDir, mats, M_COIR);

            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Trade.prefab");
        }

        // ---- 部屋3：Nursery（池の北縁の苗床プランター） ----
        static void BuildRoomNursery(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Nursery");

            // S1：中央3つのプランター箱＋池からの半割り丸太樋＋ヤシ殻ひしゃく＋堆肥囲い
            var s1 = new GameObject("Nursery_S1").transform; s1.SetParent(root.transform, false);
            var k1 = new NatureMeshKit(5); // 0: Driftwood, 1: Soil, 2: Moss, 3: Vine, 4: Compost
            float[] x1 = { -0.92f, 0f, 0.92f };
            float[] yaw1 = { -14f, 0f, 14f };
            for (int i = 0; i < 3; i++)
                AddPlanterBox(k1, 0, 1, 2, new Vector3(x1[i], 0f, -Mathf.Abs(x1[i]) * 0.12f), Quaternion.Euler(0f, yaw1[i], 0f));
            // 樋と堆肥囲い
            AddPlank(k1, 0, new Vector3(-0.35f, 0.18f, -0.55f), new Vector3(0.22f, 0.12f, 1.15f), Quaternion.Euler(8f, 15f, 0f));
            AddVessel(k1, 3, 4, new Vector3(1.65f, 0f, 0.15f), Quaternion.identity, 0.36f, 0.38f, 0.32f, 0.06f, 8);
            AddPileMesh(k1, 4, new Vector3(1.65f, 0.08f, 0.15f), 0.32f, 0.28f);
            CreateMeshChild("Nursery_S1_Mesh", s1, Vector3.zero, Quaternion.identity, k1, FurnDir, mats, M_DRIFTWOOD, M_SOIL, M_MOSS, M_VINE, M_COMPOST);

            // S2：両脇に2箱追加
            var s2 = new GameObject("Nursery_S2").transform; s2.SetParent(root.transform, false);
            var k2 = new NatureMeshKit(3);
            AddPlanterBox(k2, 0, 1, 2, new Vector3(-1.80f, 0f, -0.35f), Quaternion.Euler(0f, -26f, 0f));
            AddPlanterBox(k2, 0, 1, 2, new Vector3( 1.80f, 0f, -0.55f), Quaternion.Euler(0f,  26f, 0f));
            CreateMeshChild("Nursery_S2_Mesh", s2, Vector3.zero, Quaternion.identity, k2, FurnDir, mats, M_DRIFTWOOD, M_SOIL, M_MOSS);

            // S3：さらに両端に2箱追加
            var s3 = new GameObject("Nursery_S3").transform; s3.SetParent(root.transform, false);
            var k3 = new NatureMeshKit(3);
            AddPlanterBox(k3, 0, 1, 2, new Vector3(-2.55f, 0f, -0.75f), Quaternion.Euler(0f, -38f, 0f));
            AddPlanterBox(k3, 0, 1, 2, new Vector3( 2.55f, 0f, -0.95f), Quaternion.Euler(0f,  38f, 0f));
            CreateMeshChild("Nursery_S3_Mesh", s3, Vector3.zero, Quaternion.identity, k3, FurnDir, mats, M_DRIFTWOOD, M_SOIL, M_MOSS);

            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Nursery.prefab");
        }

        // ---- 小部品の共通メッシュ関数 ----
        static void AddPlanterBox(NatureMeshKit kit, int subWood, int subSoil, int subMoss, Vector3 pos, Quaternion rot)
        {
            AddPlank(kit, subWood, pos + rot * new Vector3(0f, 0.13f, 0f), new Vector3(0.82f, 0.26f, 0.54f), rot);
            AddPlank(kit, subSoil, pos + rot * new Vector3(0f, 0.23f, 0f), new Vector3(0.70f, 0.10f, 0.42f), rot);
            AddChunkyBlob(kit, subMoss, pos + rot * new Vector3(0.32f, 0.05f, 0.22f), new Vector3(0.35f, 0.10f, 0.28f), rot, 3, 6);
            // 苗の名札杭
            AddPlank(kit, subWood, pos + rot * new Vector3(-0.22f, 0.34f, 0f), new Vector3(0.08f, 0.22f, 0.04f), rot * Quaternion.Euler(0f, 0f, 8f));
        }

        public static void AddClayJar(NatureMeshKit kit, int sub, Vector3 bottomPos, float radius, float height, bool withLid)
        {
            AddVessel(kit, sub, sub, bottomPos, Quaternion.identity, radius * 0.72f, radius * 0.82f, height, 0.04f, 8);
            if (withLid)
                AddChunkyBlob(kit, sub, bottomPos + new Vector3(0f, height + 0.02f, 0f), new Vector3(radius * 1.75f, 0.09f, radius * 1.75f), Quaternion.identity, 3, 8);
        }

        public static void AddSaltBinMesh(NatureMeshKit kit, int subWood, int subSalt, Vector3 bottomPos)
        {
            // 塩はナメクジに毒なので高さ 0.28m の脚付き箱にする
            float[] lx = { -0.20f, 0.20f, 0.20f, -0.20f };
            float[] lz = { -0.15f, -0.15f, 0.15f, 0.15f };
            for (int i = 0; i < 4; i++)
                AddLog(kit, subWood, bottomPos + new Vector3(lx[i], 0f, lz[i]), bottomPos + new Vector3(lx[i], 0.28f, lz[i]), 0.045f, 0.045f, 5);
            AddPlank(kit, subWood, bottomPos + new Vector3(0f, 0.40f, 0f), new Vector3(0.54f, 0.28f, 0.44f), Quaternion.identity);
            AddPileMesh(kit, subSalt, bottomPos + new Vector3(0f, 0.48f, 0f), 0.21f, 0.16f);
        }

        public static void AddConchLampMesh(NatureMeshKit kit, int subConch, int subFlame, Vector3 pos)
        {
            AddChunkyBlob(kit, subConch, pos + new Vector3(0f, 0.11f, 0f), new Vector3(0.32f, 0.22f, 0.24f), Quaternion.Euler(0f, 25f, 12f), 4, 7, 0.65f, 0.75f, 0.06f, 2f, 0.30f);
            AddChunkyBlob(kit, subFlame, pos + new Vector3(0.05f, 0.22f, 0f), new Vector3(0.11f, 0.16f, 0.11f), Quaternion.identity, 4, 6);
        }

        public static void AddBrassWallLampMesh(NatureMeshKit kit, int subBrass, int subFlame, int subVine, Vector3 pos, Quaternion rot)
        {
            AddLog(kit, subBrass, pos, pos + rot * new Vector3(0f, 0.08f, -0.26f), 0.035f, 0.030f, 6);
            AddVessel(kit, subBrass, subBrass, pos + rot * new Vector3(0f, 0.04f, -0.28f), rot, 0.08f, 0.12f, 0.12f, 0.025f, 7);
            AddChunkyBlob(kit, subFlame, pos + rot * new Vector3(0f, 0.19f, -0.28f), new Vector3(0.10f, 0.16f, 0.10f), rot, 4, 6);
            AddTorus(kit, subVine, pos + rot * new Vector3(0f, 0.04f, -0.12f), rot * Quaternion.Euler(90f, 0f, 0f), 0.042f, 0.042f, 0.016f, 6, 5);
        }

        public static void AddPileMesh(NatureMeshKit kit, int sub, Vector3 bottomCenter, float radius, float height)
        {
            AddChunkyBlob(kit, sub, bottomCenter + new Vector3(0f, height * 0.35f, 0f), new Vector3(radius * 2f, height, radius * 2f), Quaternion.identity, 4, 8, 0.65f, 0.80f, 0.05f, bottomCenter.x * 5f);
        }
    }
}
