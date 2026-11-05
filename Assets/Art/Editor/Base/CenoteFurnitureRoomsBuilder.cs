using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    public static partial class CenoteBaseCodeBuilder
    {
        // ---- 部屋4：Dye（染め場：オオシャコガイの湯船・石臼・媒染壺・染め布） ----
        static void BuildRoomDye(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Dye");

            // Carved（奥の水路台）
            var cKit = new NatureMeshKit(1);
            AddPlank(cKit, 0, new Vector3(-0.55f, 0.08f, 0.95f), new Vector3(0.85f, 0.16f, 0.75f), Quaternion.identity);
            CreateMeshChild("Dye_Carved", root.transform, Vector3.zero, Quaternion.identity, cKit, FurnDir, mats, M_LIMESTONE);

            // S1（オオシャコガイの大きな湯船＋別オブジェクトの赤い水面 Dye_Water_Main＋溶岩のすり鉢）
            var s1 = new GameObject("Dye_S1").transform; s1.SetParent(root.transform, false);
            var s1Kit = new NatureMeshKit(5); // 0: ClamShell, 1: ClamInner, 2: LavaStone, 3: Driftwood, 4: DyeRed
            // 主役：オオシャコガイの湯船 (1.35 × 0.88m)
            AddVessel(s1Kit, 0, 1, new Vector3(0.05f, 0f, 0.05f), Quaternion.Euler(0f, 12f, 0f), 0.56f, 0.68f, 0.34f, 0.09f, 10);
            // 奥の小さなシャコガイ鉢
            AddVessel(s1Kit, 0, 1, new Vector3(-0.55f, 0.16f, 0.95f), Quaternion.identity, 0.25f, 0.30f, 0.20f, 0.05f, 8);
            AddChunkyBlob(s1Kit, 4, new Vector3(-0.55f, 0.28f, 0.95f), new Vector3(0.46f, 0.05f, 0.46f), Quaternion.identity, 3, 8);
            // 黒い溶岩のすり鉢と流木の杵
            AddVessel(s1Kit, 2, 2, new Vector3(0.68f, 0f, 0.55f), Quaternion.identity, 0.20f, 0.24f, 0.26f, 0.05f, 8);
            AddLog(s1Kit, 3, new Vector3(0.68f, 0.12f, 0.55f), new Vector3(0.82f, 0.48f, 0.48f), 0.05f, 0.065f, 6);
            CreateMeshChild("Dye_S1_Main", s1, Vector3.zero, Quaternion.identity, s1Kit, FurnDir, mats, M_CLAM, M_CLAM_IN, M_LAVA, M_DRIFTWOOD, M_DYE_RED);

            // 湯船の染め水面（別オブジェクト Dye_Water_Main）
            var wKit = new NatureMeshKit(1);
            AddChunkyBlob(wKit, 0, new Vector3(0.05f, 0.24f, 0.05f), new Vector3(1.10f, 0.06f, 0.86f), Quaternion.Euler(0f, 12f, 0f), 3, 10);
            CreateMeshChild("Dye_Water_Main", s1, Vector3.zero, Quaternion.identity, wKit, FurnDir, mats, M_DYE_RED);

            // S2（媒染の素焼き壺3つ：灰・岩塩・黒塩）
            var s2 = new GameObject("Dye_S2").transform; s2.SetParent(root.transform, false);
            var s2Kit = new NatureMeshKit(4); // 0: Clay, 1: Ash, 2: Salt, 3: BlackSalt
            float[] jx = { -0.45f, 0.0f, 0.45f };
            for (int i = 0; i < 3; i++)
            {
                Vector3 jp = new Vector3(jx[i], 0f, 1.05f);
                AddClayJar(s2Kit, 0, jp, 0.18f, 0.36f, true);
                AddPileMesh(s2Kit, i + 1, jp + new Vector3(0f, 0.39f, 0f), 0.10f, 0.09f);
            }
            CreateMeshChild("Dye_S2_Jars", s2, Vector3.zero, Quaternion.identity, s2Kit, FurnDir, mats, M_CLAY, M_ASH, M_SALT, M_BLACK_SALT);

            // S3（追加シャコガイ鉢2つ＋染め布の物干しロープ：赤・藍・黄土）
            var s3 = new GameObject("Dye_S3").transform; s3.SetParent(root.transform, false);
            var s3Kit = new NatureMeshKit(6); // 0: ClamShell, 1: DyeIndigo, 2: Driftwood, 3: ClothRed, 4: ClothIndigo, 5: ClothOchre
            AddVessel(s3Kit, 0, 0, new Vector3(-0.72f, 0f, 0.35f), Quaternion.identity, 0.24f, 0.28f, 0.22f, 0.05f, 8);
            AddChunkyBlob(s3Kit, 1, new Vector3(-0.72f, 0.16f, 0.35f), new Vector3(0.42f, 0.05f, 0.42f), Quaternion.identity, 3, 8);
            AddLog(s3Kit, 2, new Vector3(-0.85f, 0f, 0.85f), new Vector3(-0.85f, 1.15f, 0.85f), 0.06f, 0.05f, 6);
            AddLog(s3Kit, 2, new Vector3( 0.85f, 0f, 0.85f), new Vector3( 0.85f, 1.15f, 0.85f), 0.06f, 0.05f, 6);
            AddLog(s3Kit, 2, new Vector3(-0.85f, 1.08f, 0.85f), new Vector3( 0.85f, 1.08f, 0.85f), 0.03f, 0.03f, 6);
            AddPlank(s3Kit, 3, new Vector3(-0.45f, 0.84f, 0.85f), new Vector3(0.32f, 0.46f, 0.04f), Quaternion.Euler(0f, 5f, 0f));
            AddPlank(s3Kit, 4, new Vector3( 0.00f, 0.84f, 0.85f), new Vector3(0.32f, 0.46f, 0.04f), Quaternion.Euler(0f, -5f, 0f));
            AddPlank(s3Kit, 5, new Vector3( 0.45f, 0.84f, 0.85f), new Vector3(0.32f, 0.46f, 0.04f), Quaternion.Euler(0f, 4f, 0f));
            CreateMeshChild("Dye_S3_Line", s3, Vector3.zero, Quaternion.identity, s3Kit, FurnDir, mats, M_CLAM, M_DYE_INDIGO, M_DRIFTWOOD, M_CLOTH_RED, M_CLOTH_INDIGO, M_CLOTH_OCHRE);

            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Dye.prefab");
        }

        // ---- 部屋5：Collection（標本棚と展示枠、2階 12時） ----
        static void BuildRoomCollection(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Collection");

            // Carved（奥壁の大きな標本棚：デフォルメして5列×2段＝10個の大きく見やすいマス＋左の石台座）
            var cKit = new NatureMeshKit(3); // 0: Limestone, 1: BarkCloth, 2: Charcoal
            AddPlank(cKit, 0, new Vector3(0f, 0.68f, 1.05f), new Vector3(2.35f, 1.28f, 0.36f), Quaternion.identity);
            AddPlank(cKit, 0, new Vector3(-0.88f, 0.22f, 0.35f), new Vector3(0.55f, 0.44f, 0.55f), Quaternion.identity);
            for (int col = 0; col < 5; col++)
                for (int row = 0; row < 2; row++)
                {
                    float x = (col - 2) * 0.42f;
                    float y = 0.42f + row * 0.46f;
                    AddPlank(cKit, 1, new Vector3(x, y, 0.86f), new Vector3(0.32f, 0.34f, 0.04f), Quaternion.identity);
                    AddChunkyBlob(cKit, 2, new Vector3(x, y, 0.83f), new Vector3(0.18f, 0.20f, 0.03f), Quaternion.identity, 4, 6);
                }
            CreateMeshChild("Collection_Carved", root.transform, Vector3.zero, Quaternion.identity, cKit, FurnDir, mats, M_LIMESTONE, M_BARK, M_CHARCOAL);

            // S1（右の大きな流木額縁＋真鍮ランプ2つ）
            var s1 = new GameObject("Collection_S1").transform; s1.SetParent(root.transform, false);
            var k1 = new NatureMeshKit(4); // 0: Driftwood, 1: BarkCloth, 2: Brass, 3: Flame
            AddDisplayFrame(k1, 0, 1, new Vector3(0.88f, 0.65f, 0.45f), Quaternion.Euler(0f, -65f, 0f));
            AddBrassWallLampMesh(k1, 2, 3, 0, new Vector3(-1.05f, 1.05f, 0.65f), Quaternion.identity);
            AddBrassWallLampMesh(k1, 2, 3, 0, new Vector3( 1.05f, 1.05f, 0.65f), Quaternion.identity);
            CreateMeshChild("Collection_S1_Mesh", s1, Vector3.zero, Quaternion.identity, k1, FurnDir, mats, M_DRIFTWOOD, M_BARK, M_BRASS, M_FLAME);

            // S2（トロフィー掛けフック2本）
            var s2 = new GameObject("Collection_S2").transform; s2.SetParent(root.transform, false);
            var k2 = new NatureMeshKit(2);
            AddLog(k2, 0, new Vector3(-0.88f, 0.85f, 0.6f), new Vector3(-0.65f, 0.92f, 0.4f), 0.05f, 0.04f, 6);
            AddLog(k2, 0, new Vector3(-0.88f, 0.60f, 0.6f), new Vector3(-0.65f, 0.67f, 0.4f), 0.05f, 0.04f, 6);
            AddPlank(k2, 1, new Vector3(-0.72f, 0.50f, 0.45f), new Vector3(0.18f, 0.12f, 0.03f), Quaternion.Euler(0f, 45f, 0f));
            CreateMeshChild("Collection_S2_Mesh", s2, Vector3.zero, Quaternion.identity, k2, FurnDir, mats, M_DRIFTWOOD, M_BARK);

            // S3（さらに大きな展示枠2つ）
            var s3 = new GameObject("Collection_S3").transform; s3.SetParent(root.transform, false);
            var k3 = new NatureMeshKit(2);
            AddDisplayFrame(k3, 0, 1, new Vector3(0.55f, 0.45f, 0.15f), Quaternion.Euler(0f, -25f, 0f));
            AddDisplayFrame(k3, 0, 1, new Vector3(-0.45f, 0.45f, 0.15f), Quaternion.Euler(0f, 25f, 0f));
            CreateMeshChild("Collection_S3_Mesh", s3, Vector3.zero, Quaternion.identity, k3, FurnDir, mats, M_DRIFTWOOD, M_BARK);

            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Collection.prefab");
        }

        static void AddDisplayFrame(NatureMeshKit kit, int subWood, int subBack, Vector3 center, Quaternion rot)
        {
            AddPlank(kit, subWood, center, new Vector3(0.52f, 0.62f, 0.08f), rot);
            AddPlank(kit, subBack, center + rot * new Vector3(0f, 0f, -0.03f), new Vector3(0.38f, 0.48f, 0.04f), rot);
        }

        // ---- 部屋6：Workshop（工房：半割り丸太の作業台・発酵壺・黒曜石の鏡、2階 1:30） ----
        static void BuildRoomWorkshop(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Workshop");

            var cKit = new NatureMeshKit(1);
            AddPlank(cKit, 0, new Vector3(0.65f, 0.12f, 0.65f), new Vector3(0.65f, 0.24f, 0.95f), Quaternion.identity);
            CreateMeshChild("Workshop_Carved", root.transform, Vector3.zero, Quaternion.identity, cKit, FurnDir, mats, M_LIMESTONE);

            // S1（サンゴ塊の脚に乗った太い作業台＋下のCO2発酵壺＋工具壁＋黒曜石の全身鏡＋真鍮ランプ）
            var s1 = new GameObject("Workshop_S1").transform; s1.SetParent(root.transform, false);
            var k1 = new NatureMeshKit(6); // 0: DriftwoodDark, 1: Coral, 2: Clay, 3: Brass, 4: Obsidian, 5: Flame
            // サンゴ塊の脚2つと分厚い作業台 (1.55 × 0.62m, 高さ 0.32m)
            AddChunkyBlob(k1, 1, new Vector3(-0.52f, 0.14f, 0.05f), new Vector3(0.42f, 0.28f, 0.52f), Quaternion.identity, 4, 7);
            AddChunkyBlob(k1, 1, new Vector3( 0.42f, 0.14f, 0.05f), new Vector3(0.42f, 0.28f, 0.52f), Quaternion.identity, 4, 7);
            AddPlank(k1, 0, new Vector3(-0.05f, 0.30f, 0.05f), new Vector3(1.56f, 0.14f, 0.64f), Quaternion.identity);
            // 作業台の下の大きな発酵壺＋真鍮の口＋チューブ
            AddClayJar(k1, 2, new Vector3(-0.05f, 0f, 0.12f), 0.20f, 0.24f, false);
            AddChunkyBlob(k1, 3, new Vector3(-0.05f, 0.24f, 0.12f), new Vector3(0.14f, 0.08f, 0.14f), Quaternion.identity, 4, 6);
            AddLog(k1, 3, new Vector3(-0.05f, 0.26f, 0.12f), new Vector3(0.25f, 0.38f, 0.02f), 0.025f, 0.025f, 5, 0.06f, Vector3.up);
            // 奥壁の大きなピッケル・木槌と、右壁の黒曜石ミラー
            AddLog(k1, 0, new Vector3(-0.35f, 0.55f, 1.05f), new Vector3(-0.35f, 1.05f, 1.05f), 0.04f, 0.04f, 6);
            AddPlank(k1, 1, new Vector3(-0.35f, 0.98f, 1.02f), new Vector3(0.45f, 0.10f, 0.10f), Quaternion.identity);
            AddPlank(k1, 0, new Vector3(0.75f, 0.62f, 0.55f), new Vector3(0.10f, 0.76f, 0.56f), Quaternion.Euler(0f, -15f, 0f));
            AddPlank(k1, 4, new Vector3(0.71f, 0.62f, 0.55f), new Vector3(0.06f, 0.62f, 0.44f), Quaternion.Euler(0f, -15f, 0f));
            AddBrassWallLampMesh(k1, 3, 5, 0, new Vector3(-0.75f, 0.95f, 0.75f), Quaternion.identity);
            CreateMeshChild("Workshop_S1_Mesh", s1, Vector3.zero, Quaternion.identity, k1, FurnDir, mats, M_DRIFTWOOD_DARK, M_CORAL, M_CLAY, M_BRASS, M_OBSIDIAN, M_FLAME);

            // S2（苔玉プレス台）
            var s2 = new GameObject("Workshop_S2").transform; s2.SetParent(root.transform, false);
            var k2 = new NatureMeshKit(2);
            AddPlank(k2, 0, new Vector3(-0.68f, 0.20f, 0.68f), new Vector3(0.44f, 0.40f, 0.44f), Quaternion.identity);
            AddChunkyBlob(k2, 1, new Vector3(-0.68f, 0.46f, 0.68f), new Vector3(0.26f, 0.22f, 0.26f), Quaternion.identity, 4, 7);
            CreateMeshChild("Workshop_S2_Mesh", s2, Vector3.zero, Quaternion.identity, k2, FurnDir, mats, M_DRIFTWOOD, M_MOSS);

            // S3（嵐の実の殻を固定した彫刻台）
            var s3 = new GameObject("Workshop_S3").transform; s3.SetParent(root.transform, false);
            var k3 = new NatureMeshKit(2);
            AddPlank(k3, 0, new Vector3(0.15f, 0.22f, 0.75f), new Vector3(0.50f, 0.44f, 0.46f), Quaternion.identity);
            AddChunkyBlob(k3, 1, new Vector3(0.15f, 0.56f, 0.75f), new Vector3(0.38f, 0.36f, 0.38f), Quaternion.identity, 5, 8);
            CreateMeshChild("Workshop_S3_Mesh", s3, Vector3.zero, Quaternion.identity, k3, FurnDir, mats, M_DRIFTWOOD_DARK, M_COCONUT);

            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Workshop.prefab");
        }

        // ---- 部屋7：Drying（乾燥室：銅の敷居・つる網の乾燥棚・ヤシ葉の大うちわ、2階 3時） ----
        static void BuildRoomDrying(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Drying");

            // Carved（入口の銅の敷居帯＋外側の濡れ苔マット）
            var cKit = new NatureMeshKit(2); // 0: Copper, 1: Moss
            AddPlank(cKit, 0, new Vector3(0f, 0.02f, -0.02f), new Vector3(1.85f, 0.04f, 0.08f), Quaternion.identity);
            AddPlank(cKit, 1, new Vector3(0f, 0.025f, -0.26f), new Vector3(0.95f, 0.05f, 0.34f), Quaternion.identity);
            CreateMeshChild("Drying_Carved", root.transform, Vector3.zero, Quaternion.identity, cKit, FurnDir, mats, M_COPPER, M_MOSS);

            // S1（主役の流木＆つる網の乾燥棚＋天井から下がる薬草束3つ＋重石つき漬物壺）
            var s1 = new GameObject("Drying_S1").transform; s1.SetParent(root.transform, false);
            var k1 = new NatureMeshKit(4); // 0: Driftwood, 1: Vine, 2: Clay, 3: Limestone
            AddDryingTier(k1, 0, 1, 0f, 0.48f);
            for (int i = -1; i <= 1; i++)
                AddChunkyBlob(k1, 1, new Vector3(i * 0.45f, 1.02f, 0.35f), new Vector3(0.22f, 0.38f, 0.22f), Quaternion.identity, 4, 6);
            AddClayJar(k1, 2, new Vector3(0.68f, 0f, 0.72f), 0.20f, 0.38f, true);
            AddChunkyBlob(k1, 3, new Vector3(0.68f, 0.44f, 0.72f), new Vector3(0.24f, 0.12f, 0.20f), Quaternion.identity, 4, 6);
            CreateMeshChild("Drying_S1_Mesh", s1, Vector3.zero, Quaternion.identity, k1, FurnDir, mats, M_DRIFTWOOD, M_VINE, M_CLAY, M_LIMESTONE);

            // S2（乾燥棚が3段・高さ 1.0m に増築）
            var s2 = new GameObject("Drying_S2").transform; s2.SetParent(root.transform, false);
            var k2 = new NatureMeshKit(2);
            AddDryingTier(k2, 0, 1, 0.48f, 0.78f);
            AddDryingTier(k2, 0, 1, 0.78f, 1.06f);
            CreateMeshChild("Drying_S2_Mesh", s2, Vector3.zero, Quaternion.identity, k2, FurnDir, mats, M_DRIFTWOOD, M_VINE);

            // S3（天井ピボット Drying_FanPivot から下がる大きなヤシ葉のうちわ）
            var s3 = new GameObject("Drying_S3").transform; s3.SetParent(root.transform, false);
            var fanPiv = new GameObject("Drying_FanPivot").transform;
            fanPiv.SetParent(s3, false);
            fanPiv.localPosition = new Vector3(0f, 1.28f, 0.82f);
            var k3 = new NatureMeshKit(2); // 0: Thatch, 1: Driftwood
            AddLog(k3, 1, Vector3.zero, new Vector3(0f, -0.65f, 0f), 0.045f, 0.035f, 6);
            AddChunkyBlob(k3, 0, new Vector3(0f, -0.42f, 0f), new Vector3(0.88f, 0.64f, 0.08f), Quaternion.identity, 5, 8);
            CreateMeshChild("Drying_Fan", fanPiv, Vector3.zero, Quaternion.identity, k3, FurnDir, mats, M_THATCH, M_DRIFTWOOD);

            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Drying.prefab");
        }

        static void AddDryingTier(NatureMeshKit kit, int subWood, int subNet, float y0, float y1)
        {
            float[] x = { -0.68f, 0.68f, 0.68f, -0.68f };
            float[] z = { -0.18f, -0.18f, 0.32f,  0.32f };
            for (int i = 0; i < 4; i++)
                AddLog(kit, subWood, new Vector3(x[i], y0, z[i]), new Vector3(x[i], y1, z[i]), 0.055f, 0.050f, 6);
            AddPlank(kit, subNet, new Vector3(0f, y1 - 0.03f, 0.07f), new Vector3(1.38f, 0.06f, 0.52f), Quaternion.identity);
        }

        // ---- 部屋8：Refinery（塩の精製所：黒溶岩の石臼・粉砕台・脚付き塩箱3つ、2階 4:15） ----
        static void BuildRoomRefinery(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Refinery");

            // Carved（銅の敷居帯）
            var cKit = new NatureMeshKit(1);
            AddPlank(cKit, 0, new Vector3(0f, 0.02f, -0.02f), new Vector3(2.05f, 0.04f, 0.08f), Quaternion.identity);
            CreateMeshChild("Refinery_Carved", root.transform, Vector3.zero, Quaternion.identity, cKit, FurnDir, mats, M_COPPER);

            // S1（主役の黒溶岩の石臼＋回転ピボット Refinery_QuernPivot、粉砕ブロックと大木槌、奥壁の脚付き塩箱3つ、樹皮布の手袋）
            var s1 = new GameObject("Refinery_S1").transform; s1.SetParent(root.transform, false);
            var k1 = new NatureMeshKit(5); // 0: LavaStone, 1: Driftwood, 2: Salt, 3: BlackSalt, 4: BarkCloth
            // 石臼の台と下石 (X=-0.38, Z=0.05)
            Vector3 qPos = new Vector3(-0.38f, 0f, 0.05f);
            AddChunkyBlob(k1, 1, qPos + new Vector3(0f, 0.09f, 0f), new Vector3(0.76f, 0.18f, 0.76f), Quaternion.identity, 4, 8);
            AddChunkyBlob(k1, 0, qPos + new Vector3(0f, 0.24f, 0f), new Vector3(0.64f, 0.16f, 0.64f), Quaternion.identity, 4, 10);
            // 粉砕台＋大きな根こぶ木槌 (X=0.52, Z=0.12)
            AddPlank(k1, 0, new Vector3(0.52f, 0.12f, 0.12f), new Vector3(0.62f, 0.24f, 0.44f), Quaternion.identity);
            AddLog(k1, 1, new Vector3(0.52f, 0.26f, 0.12f), new Vector3(0.75f, 0.56f, 0.12f), 0.045f, 0.040f, 6);
            AddChunkyBlob(k1, 1, new Vector3(0.48f, 0.30f, 0.12f), new Vector3(0.24f, 0.20f, 0.20f), Quaternion.identity, 4, 7);
            // 奥壁の脚付き塩箱3つ（粗塩・黒塩・岩塩）
            AddSaltBinMesh(k1, 1, 2, new Vector3(-0.68f, 0f, 0.92f));
            AddSaltBinMesh(k1, 1, 3, new Vector3( 0.00f, 0f, 0.92f));
            AddSaltBinMesh(k1, 1, 2, new Vector3( 0.68f, 0f, 0.92f));
            // 入口の厚手袋
            AddChunkyBlob(k1, 4, new Vector3(-0.92f, 0.75f, 0.15f), new Vector3(0.18f, 0.26f, 0.12f), Quaternion.identity, 4, 6);
            CreateMeshChild("Refinery_S1_Mesh", s1, Vector3.zero, Quaternion.identity, k1, FurnDir, mats, M_LAVA, M_DRIFTWOOD, M_SALT, M_BLACK_SALT, M_BARK);

            // 上の回転する石臼（Refinery_QuernPivot）
            var qPiv = new GameObject("Refinery_QuernPivot").transform;
            qPiv.SetParent(s1, false);
            qPiv.localPosition = qPos + new Vector3(0f, 0.32f, 0f);
            var qKit = new NatureMeshKit(2);
            AddChunkyBlob(qKit, 0, new Vector3(0f, 0.06f, 0f), new Vector3(0.62f, 0.14f, 0.62f), Quaternion.identity, 4, 10);
            AddLog(qKit, 1, new Vector3(0.22f, 0.12f, 0f), new Vector3(0.22f, 0.45f, 0f), 0.045f, 0.040f, 6);
            CreateMeshChild("Refinery_QuernUpper", qPiv, Vector3.zero, Quaternion.identity, qKit, FurnDir, mats, M_LAVA, M_DRIFTWOOD);

            // S2（狙撃弾削り用の万力と貝刃）
            var s2 = new GameObject("Refinery_S2").transform; s2.SetParent(root.transform, false);
            var k2 = new NatureMeshKit(2);
            AddPlank(k2, 0, new Vector3(0.68f, 0.22f, 0.52f), new Vector3(0.38f, 0.44f, 0.36f), Quaternion.identity);
            AddPlank(k2, 1, new Vector3(0.68f, 0.48f, 0.52f), new Vector3(0.24f, 0.14f, 0.06f), Quaternion.Euler(0f, 20f, 0f));
            CreateMeshChild("Refinery_S2_Mesh", s2, Vector3.zero, Quaternion.identity, k2, FurnDir, mats, M_DRIFTWOOD_DARK, M_CLAM);

            // S3（2匹で回す巨大な黒溶岩の二本柄石臼）
            var s3 = new GameObject("Refinery_S3").transform; s3.SetParent(root.transform, false);
            var k3 = new NatureMeshKit(2);
            Vector3 bq = new Vector3(-0.15f, 0f, 0.48f);
            AddChunkyBlob(k3, 0, bq + new Vector3(0f, 0.18f, 0f), new Vector3(0.92f, 0.36f, 0.92f), Quaternion.identity, 4, 9);
            AddLog(k3, 1, bq + new Vector3(-0.55f, 0.32f, 0f), bq + new Vector3(0.55f, 0.32f, 0f), 0.055f, 0.055f, 6);
            CreateMeshChild("Refinery_S3_Mesh", s3, Vector3.zero, Quaternion.identity, k3, FurnDir, mats, M_LAVA, M_DRIFTWOOD);

            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Refinery.prefab");
        }

        // ---- 部屋9：Deck（展望台：ヤシ屋根・予報黒板・風見鳥・法螺貝＆望遠鏡） ----
        static void BuildRoomDeck(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Deck");

            // 常設（北半分のヤシ葉屋根＋半割り丸太ベンチ）
            var baseKit = new NatureMeshKit(2); // 0: Driftwood, 1: Thatch
            AddLog(baseKit, 0, new Vector3(-1.25f, 0f, 0.85f), new Vector3(-1.25f, 1.85f, 0.85f), 0.10f, 0.08f, 7);
            AddLog(baseKit, 0, new Vector3( 1.25f, 0f, 0.85f), new Vector3( 1.25f, 1.85f, 0.85f), 0.10f, 0.08f, 7);
            AddPlank(baseKit, 1, new Vector3(0f, 1.92f, 0.55f), new Vector3(2.90f, 0.18f, 1.65f), Quaternion.Euler(-12f, 0f, 0f));
            AddPlank(baseKit, 0, new Vector3(-0.65f, 0.16f, 0.65f), new Vector3(1.15f, 0.24f, 0.42f), Quaternion.identity);
            CreateMeshChild("Deck_Carved", root.transform, Vector3.zero, Quaternion.identity, baseKit, FurnDir, mats, M_DRIFTWOOD, M_THATCH);

            // S1（南向きの黒い溶岩の天気予報板＋別オブジェクト Deck_BoardFace＋雨量計の壺）
            var s1 = new GameObject("Deck_S1").transform; s1.SetParent(root.transform, false);
            var k1 = new NatureMeshKit(2); // 0: Driftwood, 1: Clay
            AddLog(k1, 0, new Vector3(0.25f, 0f, -0.35f), new Vector3(0.25f, 0.85f, -0.35f), 0.06f, 0.05f, 6);
            AddLog(k1, 0, new Vector3(0.95f, 0f, -0.35f), new Vector3(0.95f, 0.85f, -0.35f), 0.06f, 0.05f, 6);
            AddPlank(k1, 0, new Vector3(0.60f, 0.62f, -0.35f), new Vector3(0.88f, 0.58f, 0.10f), Quaternion.identity);
            AddClayJar(k1, 1, new Vector3(1.22f, 0f, -0.35f), 0.16f, 0.42f, false);
            AddLog(k1, 0, new Vector3(1.22f, 0.10f, -0.35f), new Vector3(1.22f, 0.68f, -0.35f), 0.025f, 0.025f, 5);
            CreateMeshChild("Deck_S1_Main", s1, Vector3.zero, Quaternion.identity, k1, FurnDir, mats, M_DRIFTWOOD, M_CLAY);

            var faceKit = new NatureMeshKit(1);
            AddPlank(faceKit, 0, new Vector3(0.60f, 0.62f, -0.40f), new Vector3(0.76f, 0.46f, 0.04f), Quaternion.identity);
            CreateMeshChild("Deck_BoardFace", s1, Vector3.zero, Quaternion.identity, faceKit, FurnDir, mats, M_LAVA);

            // S2（風見鳥：ポール＋ピボット Deck_VanePivot に乗る木彫りの鳥と赤い布尾）
            var s2 = new GameObject("Deck_S2").transform; s2.SetParent(root.transform, false);
            var k2 = new NatureMeshKit(1);
            AddLog(k2, 0, new Vector3(-1.15f, 0f, -0.85f), new Vector3(-1.15f, 1.55f, -0.85f), 0.055f, 0.045f, 6);
            CreateMeshChild("Deck_VanePole", s2, Vector3.zero, Quaternion.identity, k2, FurnDir, mats, M_DRIFTWOOD);

            var vanePiv = new GameObject("Deck_VanePivot").transform;
            vanePiv.SetParent(s2, false);
            vanePiv.localPosition = new Vector3(-1.15f, 1.58f, -0.85f);
            var vKit = new NatureMeshKit(2);
            AddChunkyBlob(vKit, 0, new Vector3(0f, 0.08f, 0.05f), new Vector3(0.22f, 0.20f, 0.38f), Quaternion.identity, 4, 7, 0.65f, 0.75f, 0.04f, 1f, 0.25f);
            AddPlank(vKit, 1, new Vector3(0f, 0.08f, -0.26f), new Vector3(0.06f, 0.18f, 0.32f), Quaternion.identity);
            CreateMeshChild("Deck_VaneBird", vanePiv, Vector3.zero, Quaternion.identity, vKit, FurnDir, mats, M_DRIFTWOOD, M_CLOTH_RED);

            // S3（回転台 Deck_TurnPivot 上の大法螺貝＆真鍮望遠鏡＋予報板の月齢円盤 Deck_MoonPivot）
            var s3 = new GameObject("Deck_S3").transform; s3.SetParent(root.transform, false);
            var turnPiv = new GameObject("Deck_TurnPivot").transform;
            turnPiv.SetParent(s3, false);
            turnPiv.localPosition = new Vector3(-0.35f, 0f, -0.85f);
            var tKit = new NatureMeshKit(3); // 0: Driftwood, 1: Conch, 2: Brass
            AddChunkyBlob(tKit, 0, new Vector3(0f, 0.08f, 0f), new Vector3(0.75f, 0.14f, 0.75f), Quaternion.identity, 4, 8);
            AddChunkyBlob(tKit, 1, new Vector3(-0.15f, 0.34f, 0f), new Vector3(0.38f, 0.34f, 0.62f), Quaternion.identity, 5, 8, 0.65f, 0.75f, 0.06f, 3f, 0.30f);
            AddLog(tKit, 0, new Vector3(0.18f, 0.10f, 0f), new Vector3(0.18f, 0.48f, 0f), 0.045f, 0.035f, 6);
            AddLog(tKit, 2, new Vector3(0.18f, 0.50f, 0.15f), new Vector3(0.18f, 0.56f, -0.35f), 0.065f, 0.045f, 7);
            CreateMeshChild("Deck_ObservatoryTurn", turnPiv, Vector3.zero, Quaternion.identity, tKit, FurnDir, mats, M_DRIFTWOOD, M_CONCH, M_BRASS);

            var moonPiv = new GameObject("Deck_MoonPivot").transform;
            moonPiv.SetParent(s3, false);
            moonPiv.localPosition = new Vector3(0.60f, 0.96f, -0.38f);
            var mKit = new NatureMeshKit(1);
            AddChunkyBlob(mKit, 0, Vector3.zero, new Vector3(0.32f, 0.32f, 0.06f), Quaternion.identity, 4, 8);
            CreateMeshChild("Deck_MoonDisc", moonPiv, Vector3.zero, Quaternion.identity, mKit, FurnDir, mats, M_DRIFTWOOD);

            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Deck.prefab");
        }

        // ---- 部屋10：Pier（桟橋の顔：クジラ肋骨の大アーチ門・4枚割れのれん・係留杭・鳥の餌台） ----
        static void BuildRoomPier(Material[] mats)
        {
            var root = new GameObject("CenoteFurniture_Pier");
            var kit = new NatureMeshKit(5); // 0: WhaleBone, 1: Coir, 2: Driftwood, 3: Noren, 4: Conch

            // 主役：クジラの白い大肋骨2本が交差する門（幅 2.6m、高さ 2.65m）
            for (int side = -1; side <= 1; side += 2)
            {
                var ribRows = new List<Vector3>();
                for (int r = 0; r <= 18; r++)
                {
                    float tr = r / 18f;
                    ribRows.Add(new Vector3(tr, Mathf.Lerp(1.15f, 0.70f, tr), 0));
                }
                kit.BeginTube();
                kit.Tube(
                    t => new Vector3(
                        Mathf.Lerp(side * 1.32f, -side * 0.28f, Mathf.Pow(t, 1.25f)),
                        Mathf.Sin(t * Mathf.PI * 0.62f) * 2.75f,
                        -0.35f + 0.12f * Mathf.Sin(t * Mathf.PI)
                    ),
                    _ => 0.13f,
                    ribRows, 16, 0, 0
                );
            }
            // 頂点のヤシ縄結び
            AddTorus(kit, 1, new Vector3(0f, 2.52f, -0.25f), Quaternion.Euler(90f, 0f, 0f), 0.22f, 0.18f, 0.055f, 8, 6);

            // 海の口アーチにかかる4枚割れのれん（幅 2.4m × 丈 0.85m、マテリアル Noren）
            AddLog(kit, 2, new Vector3(-1.35f, 2.42f, 0.10f), new Vector3(1.35f, 2.42f, 0.10f), 0.05f, 0.05f, 6);
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 0.58f;
                AddPlank(kit, 3, new Vector3(x, 1.98f, 0.10f), new Vector3(0.52f, 0.84f, 0.04f), Quaternion.Euler(3f * (i % 2 == 0 ? 1f : -1f), 0f, 0f));
            }

            // 係留杭2本、巻貝ランプ柱、先端の鳥の餌皿
            AddLog(kit, 2, new Vector3(-1.25f, 0f, -2.5f), new Vector3(-1.25f, 0.52f, -2.5f), 0.14f, 0.12f, 7);
            AddLog(kit, 2, new Vector3(-1.25f, 0f, -4.2f), new Vector3(-1.25f, 0.52f, -4.2f), 0.14f, 0.12f, 7);
            AddLog(kit, 2, new Vector3( 1.25f, 0f, -2.2f), new Vector3( 1.25f, 1.25f, -2.2f), 0.08f, 0.07f, 6);
            AddChunkyBlob(kit, 4, new Vector3(1.25f, 1.32f, -2.2f), new Vector3(0.32f, 0.22f, 0.26f), Quaternion.identity, 4, 7);
            // 先端の鳥の餌皿
            AddLog(kit, 2, new Vector3(1.15f, 0f, -5.2f), new Vector3(1.15f, 0.72f, -5.2f), 0.09f, 0.08f, 6);
            AddVessel(kit, 2, 2, new Vector3(1.15f, 0.70f, -5.2f), Quaternion.identity, 0.28f, 0.36f, 0.14f, 0.04f, 8);

            CreateMeshChild("Pier_Main", root.transform, Vector3.zero, Quaternion.identity, kit, FurnDir, mats, M_WHALE, M_COIR, M_DRIFTWOOD, M_NOREN, M_CONCH);
            SaveGoPrefab(root, $"{FurnDir}/CenoteFurniture_Pier.prefab");
        }
    }
}
