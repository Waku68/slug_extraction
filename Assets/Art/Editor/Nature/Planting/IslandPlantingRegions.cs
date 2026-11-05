using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SlugExtract;
using SlugWorld;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlugDesign
{
    /// <summary>
    /// 島の植え直し ② の本体（区画・気候ごとの植物配置と全体撮影、Nov 5 Argon）。
    /// </summary>
    public static partial class IslandPlanting
    {
        static readonly string[] RegionSubgroups =
        {
            "森", "川べり", "花の草原と菌輪", "砂漠と浜", "赤い台地と谷", "溶岩原と火口", "低い土地"
        };

        public static string ReplantIsland(Scene island)
        {
            var t = FindIslandTerrain(island);
            if (!t) return "Terrain not found in IslandTest_800";
            var map = t.transform.parent;

            // 1. 古い「隠れる物」の岩・倒木・流木がまだ差し替えられていなければ先に差し替える
            SwapRocksAndLogs(island, forceRebuild: false);

            // 2. 山の草（①）がまだ無ければ作る。区画ごとのサブグループは消して作り直す
            var plantRoot = GetOrCreateGroup(map, PlantGroupName);
            if (!plantRoot.Find(MountainGrassSubgroup))
                BuildMountainGrass(island);

            foreach (var sg in RegionSubgroups)
                ClearSubgroup(plantRoot, sg);

            var gForest = GetOrCreateGroup(plantRoot, "森");
            var gRiver = GetOrCreateGroup(plantRoot, "川べり");
            var gMeadow = GetOrCreateGroup(plantRoot, "花の草原と菌輪");
            var gDesertBeach = GetOrCreateGroup(plantRoot, "砂漠と浜");
            var gMesaCanyon = GetOrCreateGroup(plantRoot, "赤い台地と谷");
            var gLava = GetOrCreateGroup(plantRoot, "溶岩原と火口");
            var gLow = GetOrCreateGroup(plantRoot, "低い土地");

            Physics.SyncTransforms();
            var ctx = new PlantingContext(island, t, IslandReplantSeed);

            // ───────── Pass 1: 菌輪（輪の上にキノコを輪の向きに続け、足元に濃い芝 Grass_D_Forest、密度2倍） ─────────
            Vector2 meadowCenter = IslandTest800Builder.World(new Vector2(346f, 372f));
            for (float dz = -95f; dz <= 95f; dz += 1.6f)
            {
                for (float dx = -95f; dx <= 95f; dx += 1.6f)
                {
                    float x = meadowCenter.x + dx + ((float)ctx.rnd.NextDouble() - 0.5f) * 0.9f;
                    float z = meadowCenter.y + dz + ((float)ctx.rnd.NextDouble() - 0.5f) * 0.9f;
                    var raw = IslandTest800Builder.RegionAt(x, z);
                    if (raw[9] < 0.32f) continue;
                    float gy = ctx.GroundY(x, z);
                    if (!ctx.CanPlace(x, z, gy, 32f, false, 0.3f)) continue;
                    Vector2 xz = new Vector2(x, z);
                    if (!ctx.IsFree(xz, 1.95f)) continue;
                    ctx.Take(xz, 1.95f);

                    Vector2 toRing = (xz - meadowCenter).normalized;
                    float ringYaw = Mathf.Atan2(toRing.x, toRing.y) * Mathf.Rad2Deg + 90f;
                    string mushPath = ctx.Pick(FairyRingMushroomTable);
                    ctx.Spawn(gMeadow, mushPath, x, z, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.25f, -0.02f, ringYaw + Mathf.Lerp(-20f, 20f, (float)ctx.rnd.NextDouble()));
                    if (ctx.rnd.NextDouble() < 0.75)
                        ctx.Spawn(gMeadow, GrassDForestPath, x, z, Mathf.Lerp(0.8f, 1.1f, (float)ctx.rnd.NextDouble()), 0.9f, -0.01f);
                }
            }

            // ───────── Pass 2: 川べり（アシ Grass_C 主役、森ならクワズイモ・シダ、足元に Pebbles_B、密度2倍） ─────────
            for (float z = -430f; z <= 430f; z += 3.2f)
            {
                for (float x = -430f; x <= 430f; x += 3.2f)
                {
                    float px = x + ((float)ctx.rnd.NextDouble() - 0.5f) * 2.4f;
                    float pz = z + ((float)ctx.rnd.NextDouble() - 0.5f) * 2.4f;
                    float gy = ctx.GroundY(px, pz);
                    if (!ctx.IsRiverbank(px, pz, gy)) continue;
                    if (!ctx.CanPlace(px, pz, gy, 35f, false, 0.35f)) continue;
                    Vector2 xz = new Vector2(px, pz);
                    float sp = Mathf.Lerp(4.5f, 6.7f, (float)ctx.rnd.NextDouble());
                    if (!ctx.IsFree(xz, sp)) continue;
                    ctx.Take(xz, sp);

                    var reg = ctx.BlendedRegionAt(px, pz);
                    double r = ctx.rnd.NextDouble();
                    if (reg[0] > 0.45f && r < 0.32)
                    {
                        SpawnMainAndChild(ctx, gRiver, KuwazuimoTable, px, pz, 0.9f, 1.2f, 0.55f, 0.78f, 1.1f);
                    }
                    else if (reg[0] > 0.35f && r < 0.52)
                    {
                        ctx.Spawn(gRiver, ctx.Pick(FernClumpTable), px, pz, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.25f, -0.02f);
                    }
                    else
                    {
                        // 主役のアシ＋小さい子株
                        ctx.Spawn(gRiver, GrassCPath, px, pz, Mathf.Lerp(0.92f, 1.15f, (float)ctx.rnd.NextDouble()), 0.15f, -0.03f);
                        float ang = (float)ctx.rnd.NextDouble() * Mathf.PI * 2f;
                        float cx = px + Mathf.Cos(ang) * 0.75f, cz = pz + Mathf.Sin(ang) * 0.75f;
                        if (ctx.CanPlace(cx, cz, ctx.GroundY(cx, cz), 35f, true, 0.25f))
                            ctx.Spawn(gRiver, GrassCPath, cx, cz, Mathf.Lerp(0.65f, 0.82f, (float)ctx.rnd.NextDouble()), 0.15f, -0.03f);
                    }

                    if (ctx.rnd.NextDouble() < 0.45)
                        ctx.Spawn(gRiver, PebblesBPath, px + 0.4f, pz - 0.3f, Mathf.Lerp(0.75f, 1.05f, (float)ctx.rnd.NextDouble()), 0.85f, -0.01f);
                }
            }

            // ───────── Pass 3: 浜（ヤシ 2〜4本の群れ＋下にヤシの実・落ち葉・貝・カニ、潮の線に海藻、密度2倍） ─────────
            for (float z = -450f; z <= 450f; z += 3.9f)
            {
                for (float x = -450f; x <= 450f; x += 3.9f)
                {
                    float px = x + ((float)ctx.rnd.NextDouble() - 0.5f) * 3.0f;
                    float pz = z + ((float)ctx.rnd.NextDouble() - 0.5f) * 3.0f;
                    float gy = ctx.GroundY(px, pz);
                    if (!ctx.IsBeach(px, pz, gy)) continue;
                    if (!ctx.CanPlace(px, pz, gy, 30f, false, 0.45f)) continue;
                    Vector2 xz = new Vector2(px, pz);

                    // 潮の線（高さ 0.32〜1.1 m）：打ち上げられた海藻と貝殻
                    if (gy <= 1.1f)
                    {
                        if (!ctx.IsFree(xz, 10.5f)) continue;
                        ctx.Take(xz, 10.5f);
                        ctx.Spawn(gDesertBeach, ctx.Pick(SeaweedTable), px, pz, Mathf.Lerp(0.85f, 1.2f, (float)ctx.rnd.NextDouble()), 0.9f, -0.01f);
                        if (ctx.rnd.NextDouble() < 0.5)
                            ctx.Spawn(gDesertBeach, ctx.Pick(BeachLitterTable), px + 0.8f, pz + 0.5f, Mathf.Lerp(0.8f, 1.1f, (float)ctx.rnd.NextDouble()), 0.9f, -0.01f);
                        continue;
                    }

                    // 浜のヤシの群れ（密度2倍：12.5〜18 m おき）
                    float space = Mathf.Lerp(12.5f, 18.0f, (float)ctx.rnd.NextDouble());
                    if (!ctx.IsFree(xz, space)) continue;
                    ctx.Take(xz, space);

                    if (gy > 2.1f && ctx.rnd.NextDouble() < 0.22)
                    {
                        ctx.Spawn(gDesertBeach, ForestTreeBPath, px, pz, Mathf.Lerp(0.85f, 1.08f, (float)ctx.rnd.NextDouble()), 0.1f, -0.05f);
                        continue;
                    }

                    // ヤシ 2〜4本の群れ（主役1本＋子1〜3本）
                    int palmCount = 2 + ctx.rnd.Next(3);
                    for (int p = 0; p < palmCount; p++)
                    {
                        float ox = p == 0 ? 0f : Mathf.Cos(p * 2.1f + (float)ctx.rnd.NextDouble()) * Mathf.Lerp(1.8f, 3.4f, (float)ctx.rnd.NextDouble());
                        float oz = p == 0 ? 0f : Mathf.Sin(p * 2.1f + (float)ctx.rnd.NextDouble()) * Mathf.Lerp(1.8f, 3.4f, (float)ctx.rnd.NextDouble());
                        float tx = px + ox, tz = pz + oz, ty = ctx.GroundY(tx, tz);
                        if (p > 0 && !ctx.CanPlace(tx, tz, ty, 30f, true, 0.35f)) continue;
                        float pScale = p == 0 ? Mathf.Lerp(0.95f, 1.12f, (float)ctx.rnd.NextDouble()) : Mathf.Lerp(0.75f, 0.95f, (float)ctx.rnd.NextDouble());
                        ctx.Spawn(gDesertBeach, ctx.Pick(PalmTable), tx, tz, pScale, 0.12f, -0.05f);
                    }

                    // ヤシの下だけ：ヤシの実（Coconut）1〜2個と落ちた葉（FallenFrond）1枚、貝殻・カニの殻
                    int cocoN = 1 + ctx.rnd.Next(2);
                    for (int c = 0; c < cocoN; c++)
                    {
                        float a = (float)ctx.rnd.NextDouble() * Mathf.PI * 2f, d = Mathf.Lerp(0.9f, 2.0f, (float)ctx.rnd.NextDouble());
                        float cx = px + Mathf.Cos(a) * d, cz = pz + Mathf.Sin(a) * d;
                        if (ctx.CanPlace(cx, cz, ctx.GroundY(cx, cz), 28f, true, 0.2f))
                            ctx.Spawn(gDesertBeach, ctx.Pick(CoconutTable), cx, cz, Mathf.Lerp(0.9f, 1.1f, (float)ctx.rnd.NextDouble()), 0.5f, -0.01f);
                    }
                    if (ctx.rnd.NextDouble() < 0.7)
                    {
                        float a = (float)ctx.rnd.NextDouble() * Mathf.PI * 2f;
                        float fx = px + Mathf.Cos(a) * 1.8f, fz = pz + Mathf.Sin(a) * 1.8f;
                        if (ctx.CanPlace(fx, fz, ctx.GroundY(fx, fz), 28f, true, 0.25f))
                            ctx.Spawn(gDesertBeach, ctx.Pick(FallenFrondTable), fx, fz, Mathf.Lerp(0.85f, 1.1f, (float)ctx.rnd.NextDouble()), 0.7f, -0.01f);
                    }
                    if (ctx.rnd.NextDouble() < 0.55)
                    {
                        float a = (float)ctx.rnd.NextDouble() * Mathf.PI * 2f;
                        float sx = px + Mathf.Cos(a) * 2.4f, sz = pz + Mathf.Sin(a) * 2.4f;
                        if (ctx.CanPlace(sx, sz, ctx.GroundY(sx, sz), 28f, true, 0.2f))
                            ctx.Spawn(gDesertBeach, ctx.Pick(BeachLitterTable), sx, sz, Mathf.Lerp(0.85f, 1.1f, (float)ctx.rnd.NextDouble()), 0.9f, -0.01f);
                    }
                }
            }

            // ───────── Pass 4: 低い土地の目印「シロアリ塚」（密度2倍：約65 m に 1 つ） ─────────
            var moundCenters = new List<Vector2>();
            for (float z = -380f; z <= 380f; z += 13f)
            {
                for (float x = -380f; x <= 380f; x += 13f)
                {
                    float px = x + ((float)ctx.rnd.NextDouble() - 0.5f) * 9f;
                    float pz = z + ((float)ctx.rnd.NextDouble() - 0.5f) * 9f;
                    float gy = ctx.GroundY(px, pz);
                    if (gy < 3.5f || gy > 22f || ctx.IsBeach(px, pz, gy) || ctx.IsRiverbank(px, pz, gy)) continue;
                    var reg = IslandTest800Builder.RegionAt(px, pz);
                    float maxR = 0f;
                    for (int i = 0; i < reg.Length; i++) if (reg[i] > maxR) maxR = reg[i];
                    if (maxR > 0.42f) continue; // 低い土地（区画の間）のみ

                    Vector2 xz = new Vector2(px, pz);
                    bool nearMound = false;
                    for (int i = 0; i < moundCenters.Count; i++)
                        if ((xz - moundCenters[i]).sqrMagnitude < 64f * 64f) { nearMound = true; break; }
                    if (nearMound || !ctx.CanPlace(px, pz, gy, 24f, false, 1.2f) || !ctx.IsFree(xz, 8.5f)) continue;

                    moundCenters.Add(xz);
                    ctx.Take(xz, 7.5f);
                    ctx.Spawn(gLow, ctx.Pick(TermiteMoundTable), px, pz, Mathf.Lerp(0.95f, 1.18f, (float)ctx.rnd.NextDouble()), 0.1f, -0.05f);
                    ctx.Spawn(gLow, PebblesAPath, px + 1.2f, pz + 0.6f, 0.95f, 0.85f, -0.01f);
                }
            }

            // ───────── Pass 5: 島全体の区画別配置（20m幅ブレンド、密度2倍） ─────────
            var gridPts = new List<Vector2>(90000);
            const float step = 2.95f;
            for (float z = -450f; z <= 450f; z += step)
                for (float x = -450f; x <= 450f; x += step)
                    gridPts.Add(new Vector2(
                        x + ((float)ctx.rnd.NextDouble() - 0.5f) * step * 0.85f,
                        z + ((float)ctx.rnd.NextDouble() - 0.5f) * step * 0.85f));
            Shuffle(gridPts, ctx.rnd);

            for (int idx = 0; idx < gridPts.Count; idx++)
            {
                Vector2 xz = gridPts[idx];
                float x = xz.x, z = xz.y;
                float gy = ctx.GroundY(x, z);
                if (ctx.IsBeach(x, z, gy) || ctx.IsRiverbank(x, z, gy)) continue;
                if (!ctx.CanPlace(x, z, gy, 38f, false, 0.4f)) continue;

                var reg = ctx.BlendedRegionAt(x, z);
                // 0 森, 1 草原, 2 砂漠, 3 台地, 4 溶岩, 6 谷底, および 低い土地
                int biome = ChooseBiome(reg, ctx.rnd);

                if (biome == 0) // 森
                {
                    bool isTree = ctx.rnd.NextDouble() < 0.46;
                    float sp = isTree ? ForestTreeSpacing * Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble())
                                      : Mathf.Lerp(4.0f, 5.5f, (float)ctx.rnd.NextDouble());
                    if (!ctx.IsFree(xz, sp)) continue;
                    ctx.Take(xz, sp);

                    if (isTree)
                    {
                        double r = ctx.rnd.NextDouble();
                        if (r < 0.54)
                        {
                            // 主：板根の木 ForestTree_A ＋ 木の下のクワズイモ/シダ ＋ 根元の足元層
                            ctx.Spawn(gForest, ForestTreeAPath, x, z, Mathf.Lerp(0.85f, 1.1f, (float)ctx.rnd.NextDouble()), 0.08f, -0.06f);
                            SpawnNearBase(ctx, gForest, x, z, 1.4f, 2.5f, ctx.rnd.NextDouble() < 0.55 ? KuwazuimoTable : FernClumpTable, 0.8f, 1.1f, false);
                            SpawnNearBase(ctx, gForest, x, z, 0.9f, 2.0f, ForestLitterTable, 0.8f, 1.1f, true);
                        }
                        else if (r < 0.68 || reg[0] < 0.55f)
                        {
                            ctx.Spawn(gForest, ForestTreeBPath, x, z, Mathf.Lerp(0.85f, 1.1f, (float)ctx.rnd.NextDouble()), 0.08f, -0.05f);
                            SpawnNearBase(ctx, gForest, x, z, 0.8f, 1.8f, ForestLitterTable, 0.8f, 1.1f, true);
                        }
                        else if (r < 0.84)
                        {
                            SpawnMainAndChild(ctx, gForest, HegoTable, x, z, 0.9f, 1.15f, 0.6f, 0.8f, 1.3f);
                        }
                        else
                        {
                            SpawnMainAndChild(ctx, gForest, KuwazuimoTable, x, z, 0.9f, 1.2f, 0.55f, 0.78f, 1.15f);
                        }
                    }
                    else
                    {
                        double r = ctx.rnd.NextDouble();
                        if (r < 0.38)
                            ctx.Spawn(gForest, ctx.Pick(FernClumpTable), x, z, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                        else if (r < 0.74)
                            SpawnMassBushCluster(ctx, gForest, x, z);
                        else
                            ctx.Spawn(gForest, GrassBPath, x, z, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                    }
                }
                else if (biome == 1) // 花の草原
                {
                    // ふちに ForestTree_B
                    if (reg[1] < 0.68f && ctx.rnd.NextDouble() < 0.14)
                    {
                        if (!ctx.IsFree(xz, 13.5f)) continue;
                        ctx.Take(xz, 13.5f);
                        ctx.Spawn(gMeadow, ForestTreeBPath, x, z, Mathf.Lerp(0.85f, 1.08f, (float)ctx.rnd.NextDouble()), 0.1f, -0.05f);
                        ctx.Spawn(gMeadow, GrassDPath, x + 0.8f, z + 0.5f, 0.95f, 0.9f, -0.01f);
                        continue;
                    }
                    float sp = Mathf.Lerp(4.0f, 5.4f, (float)ctx.rnd.NextDouble());
                    if (!ctx.IsFree(xz, sp)) continue;
                    ctx.Take(xz, sp);

                    if (ctx.rnd.NextDouble() < 0.82)
                    {
                        ctx.Spawn(gMeadow, GrassAPath, x, z, Mathf.Lerp(0.88f, 1.15f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                        if (ctx.rnd.NextDouble() < 0.25)
                            ctx.Spawn(gMeadow, GrassDPath, x + 0.5f, z - 0.4f, Mathf.Lerp(0.75f, 1.0f, (float)ctx.rnd.NextDouble()), 0.9f, -0.01f);
                    }
                    else
                    {
                        ctx.Spawn(gMeadow, ctx.Pick(LowBushTable), x, z, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                    }
                }
                else if (biome == 2) // 砂漠
                {
                    bool isTall = ctx.rnd.NextDouble() < 0.22;
                    float sp = isTall ? Mathf.Lerp(14f, 19.5f, (float)ctx.rnd.NextDouble()) : Mathf.Lerp(7.8f, 11.2f, (float)ctx.rnd.NextDouble());
                    if (!ctx.IsFree(xz, sp)) continue;
                    ctx.Take(xz, sp);

                    if (isTall)
                    {
                        if (ctx.rnd.NextDouble() < 0.58)
                            ctx.Spawn(gDesertBeach, ForestTreeCPath, x, z, Mathf.Lerp(0.85f, 1.12f, (float)ctx.rnd.NextDouble()), 0.1f, -0.05f);
                        else
                            ctx.Spawn(gDesertBeach, ctx.Pick(DeadPalmTable), x, z, Mathf.Lerp(0.85f, 1.12f, (float)ctx.rnd.NextDouble()), 0.12f, -0.04f);
                        SpawnNearBase(ctx, gDesertBeach, x, z, 1.0f, 2.2f, DeadBranchTable, 0.75f, 1.05f, true);
                    }
                    else if (ctx.rnd.NextDouble() < 0.62)
                    {
                        SpawnMainAndChild(ctx, gDesertBeach, DesertRosetteTable, x, z, 0.9f, 1.2f, 0.55f, 0.75f, 0.95f);
                    }
                    else
                    {
                        ctx.Spawn(gDesertBeach, ctx.Pick(DryGrassTable), x, z, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                    }
                }
                else if (biome == 3) // 赤い台地
                {
                    bool isTall = ctx.rnd.NextDouble() < 0.20;
                    float sp = isTall ? Mathf.Lerp(15.5f, 21f, (float)ctx.rnd.NextDouble()) : Mathf.Lerp(9.0f, 12.5f, (float)ctx.rnd.NextDouble());
                    if (!ctx.IsFree(xz, sp)) continue;
                    ctx.Take(xz, sp);

                    if (isTall)
                    {
                        ctx.Spawn(gMesaCanyon, ForestTreeCPath, x, z, Mathf.Lerp(0.82f, 1.08f, (float)ctx.rnd.NextDouble()), 0.1f, -0.05f);
                        SpawnNearBase(ctx, gMesaCanyon, x, z, 1.0f, 2.0f, DeadBranchTable, 0.75f, 1.0f, true);
                    }
                    else if (ctx.rnd.NextDouble() < 0.55)
                        ctx.Spawn(gMesaCanyon, ctx.Pick(DryGrassTable), x, z, Mathf.Lerp(0.85f, 1.12f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                    else
                        SpawnMainAndChild(ctx, gMesaCanyon, DesertRosetteTable, x, z, 0.85f, 1.12f, 0.5f, 0.72f, 0.9f);
                }
                else if (biome == 4) // 溶岩原・火口の丘
                {
                    bool isTall = ctx.rnd.NextDouble() < 0.55;
                    float sp = isTall ? Mathf.Lerp(10.5f, 15.5f, (float)ctx.rnd.NextDouble()) : Mathf.Lerp(9.8f, 14f, (float)ctx.rnd.NextDouble());
                    if (!ctx.IsFree(xz, sp)) continue;
                    ctx.Take(xz, sp);

                    if (isTall)
                    {
                        if (ctx.rnd.NextDouble() < 0.68)
                            ctx.Spawn(gLava, ForestTreeDPath, x, z, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.12f, -0.05f);
                        else
                            SpawnMainAndChild(ctx, gLava, HegoTable, x, z, 0.85f, 1.1f, 0.55f, 0.75f, 1.2f);
                        if (ctx.rnd.NextDouble() < 0.65)
                            ctx.Spawn(gLava, PebblesCPath, x + 0.9f, z - 0.6f, Mathf.Lerp(0.8f, 1.1f, (float)ctx.rnd.NextDouble()), 0.85f, -0.01f);
                    }
                    else
                    {
                        ctx.Spawn(gLava, ctx.Pick(DryGrassTable), x, z, Mathf.Lerp(0.8f, 1.08f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                    }
                }
                else if (biome == 6) // 枯れた谷の底
                {
                    float sp = Mathf.Lerp(10.5f, 15.5f, (float)ctx.rnd.NextDouble());
                    if (!ctx.IsFree(xz, sp)) continue;
                    ctx.Take(xz, sp);

                    if (ctx.rnd.NextDouble() < 0.65)
                        ctx.Spawn(gMesaCanyon, ctx.Pick(DryGrassTable), x, z, Mathf.Lerp(0.8f, 1.1f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                    else
                        ctx.Spawn(gMesaCanyon, ctx.Pick(DeadBranchTable), x, z, Mathf.Lerp(0.8f, 1.1f, (float)ctx.rnd.NextDouble()), 0.4f, -0.02f);
                }
                else // 低い土地（区画の間、西ほど乾く）
                {
                    float west01 = Mathf.InverseLerp(90f, -150f, x);
                    bool isTree = ctx.rnd.NextDouble() < 0.24;
                    float sp = isTree ? Mathf.Lerp(11.2f, 16.8f, (float)ctx.rnd.NextDouble()) : Mathf.Lerp(6.7f, 9.8f, (float)ctx.rnd.NextDouble());
                    if (!ctx.IsFree(xz, sp)) continue;
                    ctx.Take(xz, sp);

                    if (isTree)
                    {
                        string treePath = ctx.rnd.NextDouble() < west01 * 0.8f ? ForestTreeBDryPath : ForestTreeBPath;
                        ctx.Spawn(gLow, treePath, x, z, Mathf.Lerp(0.85f, 1.1f, (float)ctx.rnd.NextDouble()), 0.1f, -0.05f);
                        string baseLitter = west01 > 0.5f ? PebblesAPath : GrassDPath;
                        ctx.Spawn(gLow, baseLitter, x + 0.8f, z - 0.5f, 0.9f, 0.9f, -0.01f);
                    }
                    else
                    {
                        double r = ctx.rnd.NextDouble();
                        if (r < west01 * 0.55f)
                            ctx.Spawn(gLow, ctx.Pick(DryGrassTable), x, z, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                        else if (r < 0.45)
                            ctx.Spawn(gLow, ctx.Pick(LowBushTable), x, z, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                        else if (r < 0.72)
                            SpawnMassBushCluster(ctx, gLow, x, z);
                        else
                            ctx.Spawn(gLow, GrassAPath, x, z, Mathf.Lerp(0.8f, 1.05f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
                    }
                }
            }

            // ───────── Pass 6: 道の脇（2.2〜4.8 m）の足元層（密度2倍） ─────────
            for (int idx = 0; idx < gridPts.Count; idx++)
            {
                Vector2 xz = gridPts[idx];
                float x = xz.x, z = xz.y;
                float rd = ctx.RoadDistance(x, z);
                if (rd < 2.2f || rd > 4.8f) continue;
                float gy = ctx.GroundY(x, z);
                if (!ctx.CanPlace(x, z, gy, 28f, true, 0.25f)) continue;
                if (!ctx.IsFree(xz, 3.8f)) continue;
                ctx.Take(xz, 3.8f);

                var reg = ctx.BlendedRegionAt(x, z);
                int b = ChooseBiome(reg, ctx.rnd);
                if (b == 0) ctx.Spawn(gForest, ctx.Pick(ForestLitterTable), x, z, Mathf.Lerp(0.8f, 1.05f, (float)ctx.rnd.NextDouble()), 0.9f, -0.01f);
                else if (b == 1) ctx.Spawn(gMeadow, GrassDPath, x, z, Mathf.Lerp(0.8f, 1.05f, (float)ctx.rnd.NextDouble()), 0.9f, -0.01f);
                else if (b == 2 || b == 3 || b == 6) ctx.Spawn(gDesertBeach, ctx.Pick(DeadBranchTable), x, z, Mathf.Lerp(0.75f, 1.0f, (float)ctx.rnd.NextDouble()), 0.5f, -0.01f);
                else if (b == 4) ctx.Spawn(gLava, PebblesCPath, x, z, Mathf.Lerp(0.8f, 1.05f, (float)ctx.rnd.NextDouble()), 0.9f, -0.01f);
                else ctx.Spawn(gLow, x > 0f ? GrassDPath : PebblesAPath, x, z, Mathf.Lerp(0.8f, 1.0f, (float)ctx.rnd.NextDouble()), 0.9f, -0.01f);
            }

            // 置いた数と三角形の合計を種類ごとに集計
            int totalObjs = 0, totalTris = 0;
            var sb = new StringBuilder();
            sb.AppendLine($"Island Replanted into Map/{PlantGroupName}:");
            foreach (var kv in ctx.counts)
            {
                totalObjs += kv.Value;
                totalTris += ctx.triCounts[kv.Key];
                sb.AppendLine($"- {kv.Key}: {kv.Value} ({ctx.triCounts[kv.Key]:N0} tris)");
            }
            sb.Append($"- Total replanted objects: {totalObjs:N0}, Total triangles: {totalTris:N0}");
            return sb.ToString();
        }

        static int ChooseBiome(float[] reg, System.Random rnd)
        {
            int[] candidates = { 0, 1, 2, 3, 4, 6 };
            float sum = 0f, maxW = 0f;
            for (int i = 0; i < candidates.Length; i++)
            {
                float w = Mathf.Max(0f, reg[candidates[i]] - 0.15f);
                sum += w;
                if (reg[candidates[i]] > maxW) maxW = reg[candidates[i]];
            }
            float lowWeight = Mathf.Clamp01(0.55f - maxW) * 1.4f;
            float total = sum + lowWeight;
            if (total <= 1e-4f) return -1;
            float pick = (float)rnd.NextDouble() * total, acc = 0f;
            for (int i = 0; i < candidates.Length; i++)
            {
                float w = Mathf.Max(0f, reg[candidates[i]] - 0.15f);
                acc += w;
                if (pick <= acc) return candidates[i];
            }
            return -1; // 低い土地
        }

        static void SpawnMainAndChild(PlantingContext ctx, Transform parent, (string path, float w)[] table,
            float x, float z, float mainMin, float mainMax, float childMin, float childMax, float childDist)
        {
            ctx.Spawn(parent, ctx.Pick(table), x, z, Mathf.Lerp(mainMin, mainMax, (float)ctx.rnd.NextDouble()), 0.15f, -0.03f);
            int children = 1 + (ctx.rnd.NextDouble() < 0.4 ? 1 : 0);
            for (int i = 0; i < children; i++)
            {
                float a = (float)ctx.rnd.NextDouble() * Mathf.PI * 2f;
                float d = childDist * Mathf.Lerp(0.75f, 1.2f, (float)ctx.rnd.NextDouble());
                float cx = x + Mathf.Cos(a) * d, cz = z + Mathf.Sin(a) * d;
                if (ctx.CanPlace(cx, cz, ctx.GroundY(cx, cz), 36f, true, 0.25f))
                    ctx.Spawn(parent, ctx.Pick(table), cx, cz, Mathf.Lerp(childMin, childMax, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
            }
        }

        static void SpawnMassBushCluster(PlantingContext ctx, Transform parent, float x, float z)
        {
            ctx.Spawn(parent, MassBushAPath, x, z, Mathf.Lerp(0.85f, 1.15f, (float)ctx.rnd.NextDouble()), 0.18f, -0.03f);
            int sub = 1 + (ctx.rnd.NextDouble() < 0.45 ? 1 : 0);
            for (int i = 0; i < sub; i++)
            {
                float a = (float)ctx.rnd.NextDouble() * Mathf.PI * 2f;
                float d = Mathf.Lerp(0.85f, 1.35f, (float)ctx.rnd.NextDouble());
                float cx = x + Mathf.Cos(a) * d, cz = z + Mathf.Sin(a) * d;
                if (ctx.CanPlace(cx, cz, ctx.GroundY(cx, cz), 36f, true, 0.25f))
                    ctx.Spawn(parent, MassBushBPath, cx, cz, Mathf.Lerp(0.75f, 1.0f, (float)ctx.rnd.NextDouble()), 0.2f, -0.02f);
            }
        }

        static void SpawnNearBase(PlantingContext ctx, Transform parent, float x, float z, float minD, float maxD,
            (string path, float w)[] table, float minS, float maxS, bool isGroundLitter)
        {
            float a = (float)ctx.rnd.NextDouble() * Mathf.PI * 2f;
            float d = Mathf.Lerp(minD, maxD, (float)ctx.rnd.NextDouble());
            float cx = x + Mathf.Cos(a) * d, cz = z + Mathf.Sin(a) * d;
            if (ctx.CanPlace(cx, cz, ctx.GroundY(cx, cz), 34f, isGroundLitter, 0.22f))
                ctx.Spawn(parent, ctx.Pick(table), cx, cz, Mathf.Lerp(minS, maxS, (float)ctx.rnd.NextDouble()), isGroundLitter ? 0.9f : 0.2f, -0.01f);
        }
    }
}
