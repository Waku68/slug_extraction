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
    public static partial class IslandPlanting
    {
        struct MountainCandidate
        {
            public Vector3 pos;
            public Vector3 normal;
            public float edgeTangentYaw;
            public bool isRockBase;
            public bool isEdge;
        }

        public static string BuildMountainGrass(Scene island)
        {
            var t = FindIslandTerrain(island);
            if (!t) return "Terrain not found in IslandTest_800";
            var map = t.transform.parent;
            var plantGroup = GetOrCreateGroup(map, PlantGroupName);
            ClearSubgroup(plantGroup, MountainGrassSubgroup);
            var root = GetOrCreateGroup(plantGroup, MountainGrassSubgroup);

            var prefA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Nature/Grass/Grass_A.prefab");
            var prefB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Nature/Grass/Grass_B.prefab");
            var prefD = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Nature/Grass/Grass_D.prefab");
            if (!prefA || !prefB || !prefD) return "Grass prefabs missing in Assets/Art/Nature/Grass/";

            Physics.SyncTransforms();

            // 1. 島シーン内の当たり判定を分類（岩・壁 vs 通路・階段・採る物）
            var wallRockSet = new HashSet<Collider>();
            var wallRockList = new List<Collider>();
            var avoidCols = new List<Collider>();
            foreach (var sceneRoot in island.GetRootGameObjects())
            {
                foreach (var c in sceneRoot.GetComponentsInChildren<Collider>(true))
                {
                    if (!c || !c.enabled || c is TerrainCollider || !c.gameObject.activeInHierarchy) continue;
                    if (c.isTrigger) continue;
                    string path = HierarchyPath(c.transform);
                    string n = c.name;
                    bool isWallOrRock =
                        path.Contains("段の壁の岩") ||
                        path.Contains("段の上の岩") ||
                        path.Contains("岩塩の山の岩") ||
                        path.Contains(RockGroupName) ||
                        n.StartsWith("Wall_") || n.StartsWith("Rock_") || n.StartsWith("Talus_") || n.StartsWith("SaltCrystal_") || n.Contains("岩塩の岩");
                    if (isWallOrRock)
                    {
                        wallRockSet.Add(c);
                        wallRockList.Add(c);
                    }
                    else if (path.Contains("通路") || path.Contains("往復の道") || path.Contains("斜めの道") ||
                             path.Contains("よじ登る段") || path.Contains("道しるべ") || path.Contains("RockSalt") ||
                             path.Contains("Exit") || n.StartsWith("StepBlock"))
                    {
                        avoidCols.Add(c);
                    }
                }
            }

            // 2. 道の線（真ん中を空ける用）と空ける場所
            var roadPts = new List<Vector2>();
            foreach (var sceneRoot in island.GetRootGameObjects())
            {
                foreach (var lr in sceneRoot.GetComponentsInChildren<LineRenderer>(true))
                {
                    if (!lr.transform.parent || lr.transform.parent.name != "道") continue;
                    var arr = new Vector3[lr.positionCount];
                    lr.GetPositions(arr);
                    for (int i = 0; i < arr.Length; i++)
                        roadPts.Add(new Vector2(arr[i].x, arr[i].z));
                }
            }

            var clearCenters = new List<Vector2>
            {
                IslandTest800Builder.World(new Vector2(250, 318)),
                IslandTest800Builder.World(new Vector2(500, 300)),
                IslandTest800Builder.World(new Vector2(368, 316)),
                IslandTest800Builder.World(new Vector2(452, 203)),
            };

            Vector2 mCenter = IslandTest800Builder.World(new Vector2(375f, 262f));
            var data = t.terrainData;
            Vector3 tp = t.transform.position, tSize = data.size;
            var rnd = new System.Random(MountainSeed);

            float GroundY(float x, float z) => t.SampleHeight(new Vector3(x, 0f, z)) + tp.y;
            float SteepAt(float x, float z) => data.GetSteepness(Mathf.Clamp01((x - tp.x) / tSize.x), Mathf.Clamp01((z - tp.z) / tSize.z));
            Vector3 NormalAt(float x, float z) => data.GetInterpolatedNormal(Mathf.Clamp01((x - tp.x) / tSize.x), Mathf.Clamp01((z - tp.z) / tSize.z));

            bool HitsTerrainFirst(float x, float z, float gy)
            {
                var hits = Physics.RaycastAll(new Vector3(x, gy + 35f, z), Vector3.down, 45f, ~0, QueryTriggerInteraction.Ignore);
                float bestDist = float.MaxValue;
                Collider bestCol = null;
                foreach (var h in hits)
                {
                    if (h.collider.gameObject.scene != island) continue;
                    if (h.distance < bestDist) { bestDist = h.distance; bestCol = h.collider; }
                }
                return bestCol is TerrainCollider;
            }

            bool InsideAnyObject(Vector3 p, float radius)
            {
                foreach (var c in Physics.OverlapSphere(p + Vector3.up * (radius + 0.1f), radius, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (c is TerrainCollider || c.gameObject.scene != island) continue;
                    return true;
                }
                return false;
            }

            bool TooCloseToAvoid(float x, float z, float gy)
            {
                Vector2 xz = new Vector2(x, z);
                foreach (var cc in clearCenters)
                    if ((xz - cc).sqrMagnitude < CampClearRadius * CampClearRadius) return true;
                // 道の真ん中（半径 1.3 m）は空ける（道のふちは OK）
                for (int i = 0; i < roadPts.Count; i++)
                    if ((xz - roadPts[i]).sqrMagnitude < 1.3f * 1.3f) return true;
                // 登り道・階段・通路・採る物から 1.8 m 以内は空ける
                Vector3 p = new Vector3(x, gy + 0.3f, z);
                foreach (var c in Physics.OverlapSphere(p, 1.8f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (c is TerrainCollider || c.gameObject.scene != island) continue;
                    if (!wallRockSet.Contains(c)) return true;
                }
                return false;
            }

            // 3. 候補点を探す（グリッド走査で「ふち」と「岩の根元」を両方拾う、密度2倍）
            var rockBaseCands = new List<MountainCandidate>();
            var edgeCands = new List<MountainCandidate>();

            const float step = 1.15f;
            for (float dz = -205f; dz <= 205f; dz += step)
            {
                for (float dx = -205f; dx <= 205f; dx += step)
                {
                    float x = mCenter.x + dx + ((float)rnd.NextDouble() - 0.5f) * step * 0.8f;
                    float z = mCenter.y + dz + ((float)rnd.NextDouble() - 0.5f) * step * 0.8f;
                    float gy = GroundY(x, z);
                    if (gy < MountainMinHeight || gy > MountainMaxHeight) continue;
                    float steep = SteepAt(x, z);
                    if (steep > MountainWalkMaxSlope) continue;

                    var reg = IslandTest800Builder.RegionAt(x, z);
                    if (reg[5] <= MountainRegionMin || reg[8] > 0.3f) continue;

                    if (!HitsTerrainFirst(x, z, gy)) continue;
                    Vector3 pt = new Vector3(x, gy, z);
                    if (InsideAnyObject(pt, 0.25f)) continue;
                    if (TooCloseToAvoid(x, z, gy)) continue;

                    // 8方向を調べて「ふち」か「岩・壁の根元（0.3〜1.0 m）」か判定
                    bool isRockBase = false;
                    bool isEdge = false;
                    int walkableDirs = 0;
                    float edgeDirAng = 0f;

                    for (int d = 0; d < 8; d++)
                    {
                        float ang = d * 45f * Mathf.Deg2Rad;
                        Vector2 dir2 = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                        Vector3 dir3 = new Vector3(dir2.x, 0f, dir2.y);

                        // 岩や壁の当たり判定までの距離（0.3〜1.0 m なら岩の根元、1.2 m 以内ならふち）
                        var hits = Physics.RaycastAll(pt + Vector3.up * 0.35f, dir3, EdgeProbeDist, ~0, QueryTriggerInteraction.Ignore);
                        foreach (var h in hits)
                        {
                            if (h.collider.gameObject.scene != island || !wallRockSet.Contains(h.collider)) continue;
                            if (h.distance >= RockBaseMinDist && h.distance <= RockBaseMaxDist)
                            {
                                isRockBase = true;
                                edgeDirAng = ang + Mathf.PI * 0.5f;
                            }
                            if (h.distance <= EdgeProbeDist)
                            {
                                isEdge = true;
                                if (edgeDirAng == 0f) edgeDirAng = ang + Mathf.PI * 0.5f;
                            }
                        }

                        // 1.2 m 離れた隣の地形の坂と段差
                        float nx = x + dir2.x * EdgeProbeDist, nz = z + dir2.y * EdgeProbeDist;
                        float ny = GroundY(nx, nz);
                        float nSteep = SteepAt(nx, nz);
                        if (nSteep > EdgeSteepSlope || Mathf.Abs(ny - gy) > EdgeStepHeight)
                        {
                            isEdge = true;
                            if (edgeDirAng == 0f) edgeDirAng = ang + Mathf.PI * 0.5f;
                        }
                        else if (nSteep <= 34f && Mathf.Abs(ny - gy) <= 0.35f)
                        {
                            walkableDirs++;
                        }
                    }

                    // 歩ける平場・道の端であることを確認（周囲に少なくとも2方向は歩ける平らな地面がある）
                    if (walkableDirs < 2) continue;
                    if (!isRockBase && !isEdge) continue;

                    var cand = new MountainCandidate
                    {
                        pos = pt,
                        normal = NormalAt(x, z),
                        edgeTangentYaw = edgeDirAng * Mathf.Rad2Deg,
                        isRockBase = isRockBase,
                        isEdge = isEdge
                    };
                    if (isRockBase) rockBaseCands.Add(cand);
                    else edgeCands.Add(cand);
                }
            }

            Shuffle(rockBaseCands, rnd);
            Shuffle(edgeCands, rnd);

            // 岩の根元と段のふちを半々ずつ交互に並べる
            var allCands = new List<MountainCandidate>(rockBaseCands.Count + edgeCands.Count);
            int maxLen = Mathf.Max(rockBaseCands.Count, edgeCands.Count);
            for (int i = 0; i < maxLen; i++)
            {
                if (i < rockBaseCands.Count) allCands.Add(rockBaseCands[i]);
                if (i < edgeCands.Count) allCands.Add(edgeCands[i]);
            }

            // 4. 標高に応じた間隔（15〜25 m、高い所ほどまばら）で配置
            var placedCenters = new List<Vector3>();
            int countClusters = 0, countA = 0, countB = 0, countD = 0;
            int countRockBase = 0, countLedgeEdge = 0;

            int trisA = CountTriangles(prefA), trisB = CountTriangles(prefB), trisD = CountTriangles(prefD);

            foreach (var c in allCands)
            {
                float h01 = Mathf.InverseLerp(18f, 96f, c.pos.y);
                float reqSpace = Mathf.Lerp(MountainSpaceLow, MountainSpaceHigh, h01) * Mathf.Lerp(0.88f, 1.12f, (float)rnd.NextDouble());

                bool clash = false;
                for (int i = 0; i < placedCenters.Count; i++)
                {
                    float minD = (reqSpace + placedCenters[i].z) * 0.5f;
                    float dx = c.pos.x - placedCenters[i].x, dz = c.pos.z - placedCenters[i].y;
                    if (dx * dx + dz * dz < minD * minD) { clash = true; break; }
                }
                if (clash) continue;

                placedCenters.Add(new Vector3(c.pos.x, c.pos.z, reqSpace));
                countClusters++;
                if (c.isRockBase) countRockBase++; else countLedgeEdge++;

                // 主役の株（Grass_A・Grass_B 自体に主役＋子株が入っているため 1 プレハブ＝1 群れ）
                bool mainIsA = rnd.NextDouble() < GrassARatio;
                float mainScale = mainIsA
                    ? Mathf.Lerp(GrassAMinScale, GrassAMaxScale, (float)rnd.NextDouble())
                    : Mathf.Lerp(GrassBMinScale, GrassBMaxScale, (float)rnd.NextDouble());
                float mainYaw = (float)rnd.NextDouble() * 360f;
                SpawnPlant(root, mainIsA ? prefA : prefB, c.pos, c.normal, mainYaw, mainScale, 0.2f, -0.02f);
                if (mainIsA) countA++; else countB++;

                // 岩の根元や広いふちでは、35% の確率で隣にもう1株（Grass_B）を添える
                if (rnd.NextDouble() < 0.35)
                {
                    float ang = (c.edgeTangentYaw + Mathf.Lerp(-25f, 25f, (float)rnd.NextDouble())) * Mathf.Deg2Rad;
                    float dist = Mathf.Lerp(0.55f, 0.85f, (float)rnd.NextDouble());
                    float sx = c.pos.x + Mathf.Cos(ang) * dist, sz = c.pos.z + Mathf.Sin(ang) * dist;
                    float sy = GroundY(sx, sz);
                    if (Mathf.Abs(sy - c.pos.y) <= 0.22f && SteepAt(sx, sz) <= 30f)
                    {
                        Vector3 sp = new Vector3(sx, sy, sz);
                        if (HitsTerrainFirst(sx, sz, sy) && !InsideAnyObject(sp, 0.25f))
                        {
                            float subScale = Mathf.Lerp(0.65f, 0.82f, (float)rnd.NextDouble());
                            SpawnPlant(root, prefB, sp, NormalAt(sx, sz), (float)rnd.NextDouble() * 360f, subScale, 0.2f, -0.02f);
                            countB++;
                        }
                    }
                }

                // 足元の芝 Grass_D（ところどころ：平らな場所 22° 以下のみ）
                if (SteepAt(c.pos.x, c.pos.z) <= 22f && rnd.NextDouble() < 0.35)
                {
                    float dScale = Mathf.Lerp(0.65f, 0.88f, (float)rnd.NextDouble());
                    SpawnPlant(root, prefD, c.pos, c.normal, (float)rnd.NextDouble() * 360f, dScale, 0.95f, -0.01f);
                    countD++;
                }
            }

            int totalTris = countA * trisA + countB * trisB + countD * trisD;
            var sb = new StringBuilder();
            sb.AppendLine($"Mountain Grass built: {countClusters} clusters (rock base {countRockBase}, ledge edge {countLedgeEdge})");
            sb.AppendLine($"- Grass_A: {countA} ({countA * trisA:N0} tris)");
            sb.AppendLine($"- Grass_B: {countB} ({countB * trisB:N0} tris)");
            sb.AppendLine($"- Grass_D: {countD} ({countD * trisD:N0} tris)");
            sb.Append($"- Total objects: {countA + countB + countD}, Total triangles: {totalTris:N0}");
            return sb.ToString();
        }

        static GameObject SpawnPlant(Transform parent, GameObject prefab, Vector3 pos, Vector3 normal, float yaw, float scale, float normalAlign, float yOffset)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Vector3 up = Vector3.Slerp(Vector3.up, normal, normalAlign).normalized;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0f, yaw, 0f);
            go.transform.SetPositionAndRotation(pos + Vector3.up * yOffset, rot);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        static string HierarchyPath(Transform tr)
        {
            var sb = new StringBuilder(tr.name);
            var p = tr.parent;
            while (p != null) { sb.Insert(0, p.name + "/"); p = p.parent; }
            return sb.ToString();
        }

        static void Shuffle<T>(List<T> list, System.Random rnd)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ───────── ① 岩塩の山の草の撮影（決まった3枚、max 768） ─────────

        public static string ShootMountainGrass(Scene island)
        {
            var t = FindIslandTerrain(island);
            var w = UnityEngine.Object.FindAnyObjectByType<WeatherPresenter>();
            if (!t || !w) return "Terrain or WeatherPresenter missing";
            if (!w.sun)
                foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                    if (l.gameObject.scene == island && l.type == LightType.Directional) { w.sun = l; break; }

            var map = t.transform.parent;
            var plantGroup = map ? map.Find($"{PlantGroupName}/{MountainGrassSubgroup}") : null;
            if (!plantGroup || plantGroup.childCount == 0)
            {
                BuildMountainGrass(island);
                plantGroup = map.Find($"{PlantGroupName}/{MountainGrassSubgroup}");
            }

            Directory.CreateDirectory(ShotDir);
            var sb = new StringBuilder();

            // 見栄えが良く、手前 2.6 m から目の高さ（0.47 m）で遮蔽物なく見通せる Grass_A の株を探す
            Transform heroClump = null;
            Vector3 bestFromA = Vector3.zero, bestLookA = Vector3.zero, bestTangent = Vector3.right;
            Vector2 mCenter = IslandTest800Builder.World(new Vector2(375f, 262f));
            float bestScore = float.MaxValue;
            var data = t.terrainData;
            Vector3 tp = t.transform.position, tSize = data.size;
            float GroundY(Vector3 p) => t.SampleHeight(p) + tp.y;
            float SteepAt(Vector3 p) => data.GetSteepness(Mathf.Clamp01((p.x - tp.x) / tSize.x), Mathf.Clamp01((p.z - tp.z) / tSize.z));

            if (plantGroup)
            {
                foreach (Transform c in plantGroup)
                {
                    if (!c.name.StartsWith("Grass_A")) continue;
                    Vector3 cp = c.position;
                    if (cp.y < 24f || cp.y > 72f) continue;
                    if (SteepAt(cp) > 22f) continue;

                    // 周囲 16 方向から 2.5 m 離れた平らな立ち位置を探す
                    for (int dir = 0; dir < 16; dir++)
                    {
                        float a = dir * (Mathf.PI * 2f / 16f);
                        Vector3 fwd = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        Vector3 camPos = cp - fwd * 2.5f;
                        float gy = GroundY(camPos);
                        if (Mathf.Abs(gy - cp.y) > 0.35f || SteepAt(camPos) > 20f) continue;
                        camPos.y = gy + 0.47f;
                        Vector3 target = cp + Vector3.up * 0.25f;
                        if (Physics.Linecast(camPos, target, out _, ~0, QueryTriggerInteraction.Ignore)) continue;

                        // 奥にも山の壁や段が見える向き（山側を向く）を高く評価
                        Vector2 toM = (mCenter - new Vector2(cp.x, cp.z)).normalized;
                        float intoMountain = Vector2.Dot(new Vector2(fwd.x, fwd.z), toM);
                        if (intoMountain < -0.1f) continue;

                        // 視界の奥（3〜18 m）に他の草株も見えるとさらに良い
                        int otherVisible = 0;
                        foreach (Transform o in plantGroup)
                        {
                            if (o == c) continue;
                            Vector3 d = o.position - camPos;
                            float dist = d.magnitude;
                            if (dist > 2.8f && dist < 22f && Vector3.Dot(d.normalized, fwd) > 0.78f)
                                otherVisible++;
                        }

                        float score = Mathf.Abs(cp.y - 42f) * 0.5f - intoMountain * 8f - otherVisible * 6f;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            heroClump = c;
                            bestFromA = camPos;
                            bestLookA = cp + fwd * 3.5f + Vector3.up * 0.35f;
                            bestTangent = fwd;
                        }
                    }
                }
            }

            Vector3 hp = heroClump ? heroClump.position : new Vector3(mCenter.x + 25f, GroundY(new Vector3(mCenter.x + 25f, 0f, mCenter.y - 65f)), mCenter.y - 65f);
            if (heroClump == null)
            {
                bestFromA = hp - new Vector3(2.4f, 0f, 0f);
                bestFromA.y = GroundY(bestFromA) + 0.47f;
                bestLookA = hp + Vector3.up * 0.3f;
                bestTangent = Vector3.right;
            }

            // A: ナメクジの目の高さ（0.47 m）から、段の道・ふちと壁際の草を望む
            string pathA = $"{ShotDir}/mountain_A_eye.png";
            SaveShot(w, pathA, bestFromA, bestLookA, 55f, 768, 512, true);
            sb.AppendLine(pathA);

            // B: 横から、大きさ見本のナメクジ（長さ 1.1 m・高さ 0.5 m のカプセル）と並べて
            var slug = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            SceneManager.MoveGameObjectToScene(slug, island);
            slug.transform.localScale = new Vector3(0.5f, 0.55f, 0.5f);
            Vector3 sideDir = new Vector3(-bestTangent.z, 0f, bestTangent.x).normalized;
            Vector3 slugPos = hp + bestTangent * 1.05f;
            slugPos.y = GroundY(slugPos) + 0.25f;
            slug.transform.SetPositionAndRotation(slugPos, Quaternion.LookRotation(bestTangent) * Quaternion.Euler(90f, 0f, 0f));
            var slugMat = new Material(Shader.Find("Slug/Toon") ?? Shader.Find("Universal Render Pipeline/Lit"));
            slugMat.SetColor("_BaseColor", new Color(0.82f, 0.58f, 0.28f));
            slug.GetComponent<Renderer>().sharedMaterial = slugMat;
            try
            {
                Vector3 midB = (hp + slugPos) * 0.5f + Vector3.up * 0.22f;
                Vector3 fromB = midB + sideDir * 2.5f + Vector3.up * 0.45f;
                if (Physics.Linecast(fromB, midB, out _, ~0, QueryTriggerInteraction.Ignore))
                    fromB = midB - sideDir * 2.5f + Vector3.up * 0.45f;
                string pathB = $"{ShotDir}/mountain_B_side.png";
                SaveShot(w, pathB, fromB, midB, 42f, 768, 512, true);
                sb.AppendLine(pathB);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(slug);
                UnityEngine.Object.DestroyImmediate(slugMat);
            }

            // C: 斜め上から岩塩の山の段々とふち・壁際の草の配置が分かる全体絵
            Vector3 focusC = new Vector3(mCenter.x + 8f, 38f, mCenter.y - 52f);
            Vector3 fromC = focusC + new Vector3(42f, 38f, -52f);
            string pathC = $"{ShotDir}/mountain_C_overview.png";
            SaveShot(w, pathC, fromC, focusC, 45f, 768, 512, false);
            sb.AppendLine(pathC);

            return sb.ToString().TrimEnd();
        }
    }
}
