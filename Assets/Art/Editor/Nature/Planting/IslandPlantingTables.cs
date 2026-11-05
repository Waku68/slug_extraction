using System;
using System.Collections.Generic;
using SlugExtract;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlugDesign
{
    /// <summary>
    /// 島の植え直し ② の案ごとの割合の表（1か所に集約）と、区画・浜・川べり・空ける場所の判定（Nov 5 Argon）。
    /// なめらか版のみ使用（_Facet は使わない）。採る物・暮らしの跡・珍しい拾い物・ケルン・山の部品は置かない。
    /// </summary>
    public static partial class IslandPlanting
    {
        public const int IslandReplantSeed = 701;

        // ───────── 案が決まっていない物の割合表（A〜C を混ぜる。外すときは 0f にする） ─────────

        public static readonly (string path, float w)[] HegoTable =
        {
            ("Assets/Art/Nature/Hego/Hego_A.prefab", 0.34f),
            ("Assets/Art/Nature/Hego/Hego_B.prefab", 0.33f),
            ("Assets/Art/Nature/Hego/Hego_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] KuwazuimoTable =
        {
            ("Assets/Art/Nature/Kuwazuimo/Kuwazuimo_A.prefab", 0.34f),
            ("Assets/Art/Nature/Kuwazuimo/Kuwazuimo_B.prefab", 0.33f),
            ("Assets/Art/Nature/Kuwazuimo/Kuwazuimo_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] FernClumpTable =
        {
            ("Assets/Art/Nature/Understory/FernClump_A.prefab", 0.34f),
            ("Assets/Art/Nature/Understory/FernClump_B.prefab", 0.33f),
            ("Assets/Art/Nature/Understory/FernClump_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] LowBushTable =
        {
            ("Assets/Art/Nature/Understory/LowBush_A.prefab", 0.34f),
            ("Assets/Art/Nature/Understory/LowBush_B.prefab", 0.33f),
            ("Assets/Art/Nature/Understory/LowBush_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] DryGrassTable =
        {
            ("Assets/Art/Nature/Understory/DryGrass_A.prefab", 0.34f),
            ("Assets/Art/Nature/Understory/DryGrass_B.prefab", 0.33f),
            ("Assets/Art/Nature/Understory/DryGrass_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] DesertRosetteTable =
        {
            ("Assets/Art/Nature/DesertRosette/DesertRosette_A.prefab", 0.34f),
            ("Assets/Art/Nature/DesertRosette/DesertRosette_B.prefab", 0.33f),
            ("Assets/Art/Nature/DesertRosette/DesertRosette_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] FairyRingMushroomTable =
        {
            ("Assets/Art/Nature/FairyRingMushroom/FairyRingMushroom_A.prefab", 0.34f),
            ("Assets/Art/Nature/FairyRingMushroom/FairyRingMushroom_B.prefab", 0.33f),
            ("Assets/Art/Nature/FairyRingMushroom/FairyRingMushroom_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] PalmTable =
        {
            ("Assets/Art/Nature/Palm/Palm_A.prefab", 0.55f),
            ("Assets/Art/Nature/Palm/Palm_B.prefab", 0.45f),
        };

        public static readonly (string path, float w)[] DeadPalmTable =
        {
            ("Assets/Art/Nature/PalmRemains/DeadPalm_A.prefab", 0.34f),
            ("Assets/Art/Nature/PalmRemains/DeadPalm_B.prefab", 0.33f),
            ("Assets/Art/Nature/PalmRemains/DeadPalm_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] FallenFrondTable =
        {
            ("Assets/Art/Nature/PalmRemains/FallenFrond_A.prefab", 0.34f),
            ("Assets/Art/Nature/PalmRemains/FallenFrond_B.prefab", 0.33f),
            ("Assets/Art/Nature/PalmRemains/FallenFrond_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] CoconutTable =
        {
            ("Assets/Art/Nature/PalmRemains/Coconut_A.prefab", 0.34f),
            ("Assets/Art/Nature/PalmRemains/Coconut_B.prefab", 0.33f),
            ("Assets/Art/Nature/PalmRemains/Coconut_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] DeadBranchTable =
        {
            ("Assets/Art/Nature/Wood/DeadBranch_A.prefab", 0.34f),
            ("Assets/Art/Nature/Wood/DeadBranch_B.prefab", 0.33f),
            ("Assets/Art/Nature/Wood/DeadBranch_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] TermiteMoundTable =
        {
            ("Assets/Art/Nature/Rocks/TermiteMound_A.prefab", 0.34f),
            ("Assets/Art/Nature/Rocks/TermiteMound_B.prefab", 0.33f),
            ("Assets/Art/Nature/Rocks/TermiteMound_C.prefab", 0.33f),
        };

        public static readonly (string path, float w)[] ForestLitterTable =
        {
            ("Assets/Art/Nature/Grass/Grass_D_Forest.prefab", 0.26f),
            ("Assets/Art/Nature/GroundLitter/FallenLeaves_A.prefab", 0.08f),
            ("Assets/Art/Nature/GroundLitter/FallenLeaves_B.prefab", 0.08f),
            ("Assets/Art/Nature/GroundLitter/FallenLeaves_C.prefab", 0.08f),
            ("Assets/Art/Nature/GroundLitter/MossPatch_A.prefab", 0.07f),
            ("Assets/Art/Nature/GroundLitter/MossPatch_B.prefab", 0.07f),
            ("Assets/Art/Nature/GroundLitter/MossPatch_C.prefab", 0.07f),
            ("Assets/Art/Nature/GroundLitter/SmallMushrooms_A.prefab", 0.05f),
            ("Assets/Art/Nature/GroundLitter/SmallMushrooms_B.prefab", 0.05f),
            ("Assets/Art/Nature/GroundLitter/SmallMushrooms_C.prefab", 0.05f),
            ("Assets/Art/Nature/GroundLitter/Twigs_A.prefab", 0.05f),
            ("Assets/Art/Nature/GroundLitter/Twigs_B.prefab", 0.05f),
            ("Assets/Art/Nature/GroundLitter/Twigs_C.prefab", 0.04f),
        };

        public static readonly (string path, float w)[] BeachLitterTable =
        {
            ("Assets/Art/Nature/BeachLitter/Shells_A.prefab", 0.22f),
            ("Assets/Art/Nature/BeachLitter/Shells_B.prefab", 0.22f),
            ("Assets/Art/Nature/BeachLitter/Shells_C.prefab", 0.21f),
            ("Assets/Art/Nature/BeachLitter/CrabMolt_A.prefab", 0.12f),
            ("Assets/Art/Nature/BeachLitter/CrabMolt_B.prefab", 0.12f),
            ("Assets/Art/Nature/BeachLitter/CrabMolt_C.prefab", 0.11f),
        };

        public static readonly (string path, float w)[] SeaweedTable =
        {
            ("Assets/Art/Nature/BeachLitter/Seaweed_A.prefab", 0.34f),
            ("Assets/Art/Nature/BeachLitter/Seaweed_B.prefab", 0.33f),
            ("Assets/Art/Nature/BeachLitter/Seaweed_C.prefab", 0.33f),
        };

        // 単一プレハブのパス
        public const string ForestTreeAPath = "Assets/Art/Nature/ForestTree/ForestTree_A.prefab";
        public const string ForestTreeBPath = "Assets/Art/Nature/ForestTree/ForestTree_B.prefab";
        public const string ForestTreeBDryPath = "Assets/Art/Nature/ForestTree/ForestTree_B_Dry.prefab";
        public const string ForestTreeCPath = "Assets/Art/Nature/ForestTree/ForestTree_C.prefab";
        public const string ForestTreeDPath = "Assets/Art/Nature/ForestTree/ForestTree_D.prefab";
        public const string MassBushAPath = "Assets/Art/Nature/MassBush/MassBush_A.prefab";
        public const string MassBushBPath = "Assets/Art/Nature/MassBush/MassBush_B.prefab";
        public const string GrassAPath = "Assets/Art/Nature/Grass/Grass_A.prefab";
        public const string GrassBPath = "Assets/Art/Nature/Grass/Grass_B.prefab";
        public const string GrassCPath = "Assets/Art/Nature/Grass/Grass_C.prefab";
        public const string GrassDPath = "Assets/Art/Nature/Grass/Grass_D.prefab";
        public const string GrassDForestPath = "Assets/Art/Nature/Grass/Grass_D_Forest.prefab";
        public const string PebblesAPath = "Assets/Art/Nature/GroundLitter/Pebbles_A.prefab";
        public const string PebblesBPath = "Assets/Art/Nature/GroundLitter/Pebbles_B.prefab";
        public const string PebblesCPath = "Assets/Art/Nature/GroundLitter/Pebbles_C.prefab";

        // ───────── 空間インデックスと区画・空ける場所の判定 ─────────

        public class PlantingContext
        {
            public Scene island;
            public Terrain terrain;
            public TerrainData data;
            public Vector3 tp, tSize;
            public System.Random rnd;

            readonly Dictionary<long, List<Vector3>> _taken = new Dictionary<long, List<Vector3>>();
            readonly List<Vector2> _roadPts = new List<Vector2>();
            readonly List<(Vector2 center, float radius)> _clearZones = new List<(Vector2, float)>();
            readonly List<SlugWorld.MoistureZone> _riverZones = new List<SlugWorld.MoistureZone>();
            readonly Dictionary<string, (GameObject pref, int tris)> _prefabs = new Dictionary<string, (GameObject, int)>();
            public readonly Dictionary<string, int> counts = new Dictionary<string, int>();
            public readonly Dictionary<string, int> triCounts = new Dictionary<string, int>();

            // 海（gy <= 0.25m）までの距離グリッド（4m刻み、浜の判定用）
            const int SeaGridN = 241;
            const float SeaGridStep = 4f, SeaGridHalf = 480f;
            readonly float[,] _seaDist = new float[SeaGridN, SeaGridN];

            public PlantingContext(Scene island, Terrain terrain, int seed)
            {
                this.island = island;
                this.terrain = terrain;
                data = terrain.terrainData;
                tp = terrain.transform.position;
                tSize = data.size;
                rnd = new System.Random(seed);

                BuildSeaGrid();
                CollectSceneZones();
            }

            void BuildSeaGrid()
            {
                var q = new Queue<(int i, int j)>();
                for (int j = 0; j < SeaGridN; j++)
                {
                    for (int i = 0; i < SeaGridN; i++)
                    {
                        float x = -SeaGridHalf + i * SeaGridStep, z = -SeaGridHalf + j * SeaGridStep;
                        float gy = GroundY(x, z);
                        if (gy <= 0.25f)
                        {
                            _seaDist[j, i] = 0f;
                            q.Enqueue((i, j));
                        }
                        else _seaDist[j, i] = 999f;
                    }
                }
                int[] di = { 1, -1, 0, 0, 1, 1, -1, -1 };
                int[] dj = { 0, 0, 1, -1, 1, -1, 1, -1 };
                float[] dd = { 4f, 4f, 4f, 4f, 5.66f, 5.66f, 5.66f, 5.66f };
                while (q.Count > 0)
                {
                    var (ci, cj) = q.Dequeue();
                    float cd = _seaDist[cj, ci];
                    if (cd > 32f) continue;
                    for (int k = 0; k < 8; k++)
                    {
                        int ni = ci + di[k], nj = cj + dj[k];
                        if (ni < 0 || nj < 0 || ni >= SeaGridN || nj >= SeaGridN) continue;
                        float nd = cd + dd[k];
                        if (nd < _seaDist[nj, ni])
                        {
                            _seaDist[nj, ni] = nd;
                            q.Enqueue((ni, nj));
                        }
                    }
                }
            }

            void CollectSceneZones()
            {
                // キャンプ2つ・たまり池・泉（半径12m）、大ガジュマルの根の柱の間（半径13m）
                _clearZones.Add((IslandTest800Builder.World(new Vector2(250, 318)), CampClearRadius));
                _clearZones.Add((IslandTest800Builder.World(new Vector2(500, 300)), CampClearRadius));
                _clearZones.Add((IslandTest800Builder.World(new Vector2(368, 316)), CampClearRadius));
                _clearZones.Add((IslandTest800Builder.World(new Vector2(452, 203)), CampClearRadius));
                _clearZones.Add((IslandTest800Builder.World(new Vector2(500, 228)), 13f));

                foreach (var root in island.GetRootGameObjects())
                {
                    foreach (var lr in root.GetComponentsInChildren<LineRenderer>(true))
                    {
                        if (!lr.transform.parent || lr.transform.parent.name != "道") continue;
                        var pts = new Vector3[lr.positionCount];
                        lr.GetPositions(pts);
                        for (int i = 0; i < pts.Length; i++)
                            _roadPts.Add(new Vector2(pts[i].x, pts[i].z));
                    }

                    foreach (var mz in root.GetComponentsInChildren<SlugWorld.MoistureZone>(true))
                    {
                        if (mz.name.Contains("River_Zone"))
                            _riverZones.Add(mz);
                    }

                    foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                    {
                        string n = tr.name;
                        if (n == "Exit" || n.StartsWith("RockSalt") || n.StartsWith("RainTurnip") || n.StartsWith("Ugotake") ||
                            n.StartsWith("Label_溶岩洞の口") || n == "Label_岩塩の洞窟")
                        {
                            _clearZones.Add((new Vector2(tr.position.x, tr.position.z), 4.2f));
                        }
                    }
                }
            }

            public float GroundY(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + tp.y;
            public float SteepAt(float x, float z) => data.GetSteepness(Mathf.Clamp01((x - tp.x) / tSize.x), Mathf.Clamp01((z - tp.z) / tSize.z));
            public Vector3 NormalAt(float x, float z) => data.GetInterpolatedNormal(Mathf.Clamp01((x - tp.x) / tSize.x), Mathf.Clamp01((z - tp.z) / tSize.z));

            public float SeaDistance(float x, float z)
            {
                int i = Mathf.Clamp(Mathf.RoundToInt((x + SeaGridHalf) / SeaGridStep), 0, SeaGridN - 1);
                int j = Mathf.Clamp(Mathf.RoundToInt((z + SeaGridHalf) / SeaGridStep), 0, SeaGridN - 1);
                return _seaDist[j, i];
            }

            public bool IsBeach(float x, float z, float gy) => gy > 0.25f && gy <= 3.0f && SeaDistance(x, z) <= 25f;

            public bool IsRiverbank(float x, float z, float gy)
            {
                bool inBank = false;
                for (int i = 0; i < _riverZones.Count; i++)
                {
                    var mz = _riverZones[i];
                    if (!mz) continue;
                    Vector3 c = mz.transform.position;
                    Vector3 half = mz.size * 0.5f;
                    float dx = Mathf.Abs(x - c.x), dz = Mathf.Abs(z - c.z);
                    // 川の深い真ん中（水面より 0.2m 以上低い川底の中心）には置かない
                    if (dx < half.x * 0.45f && dz < half.z * 0.45f && gy < c.y - 0.18f)
                        return false;
                    float ox = Mathf.Max(0f, dx - half.x), oz = Mathf.Max(0f, dz - half.z);
                    if (ox * ox + oz * oz <= 4f * 4f)
                        inBank = true;
                }
                return inBank;
            }

            /// <summary>区画の境目を RegionBlendWidth（20m）で滑らかに混ぜた重み（0..9）。</summary>
            public float[] BlendedRegionAt(float x, float z)
            {
                float d = RegionBlendWidth * 0.5f;
                var c = IslandTest800Builder.RegionAt(x, z);
                var e = IslandTest800Builder.RegionAt(x + d, z);
                var w = IslandTest800Builder.RegionAt(x - d, z);
                var n = IslandTest800Builder.RegionAt(x, z + d);
                var s = IslandTest800Builder.RegionAt(x, z - d);
                var res = new float[c.Length];
                for (int i = 0; i < c.Length; i++)
                    res[i] = c[i] * 0.4f + (e[i] + w[i] + n[i] + s[i]) * 0.15f;
                return res;
            }

            public float RoadDistance(float x, float z)
            {
                Vector2 xz = new Vector2(x, z);
                float bestSq = float.MaxValue;
                for (int i = 0; i < _roadPts.Count; i++)
                {
                    float dSq = (xz - _roadPts[i]).sqrMagnitude;
                    if (dSq < bestSq) bestSq = dSq;
                }
                return Mathf.Sqrt(bestSq);
            }

            public bool CanPlace(float x, float z, float gy, float maxSlope, bool isGroundLitter, float checkRadius = 0.45f)
            {
                if (gy <= 0.25f || SteepAt(x, z) > maxSlope) return false;
                var rawReg = IslandTest800Builder.RegionAt(x, z);
                if (rawReg[8] > 0.3f || rawReg[5] > 0.6f) return false; // 塩田と岩塩の山（①で配置済み）は除く

                Vector2 xz = new Vector2(x, z);
                for (int i = 0; i < _clearZones.Count; i++)
                    if ((xz - _clearZones[i].center).sqrMagnitude < _clearZones[i].radius * _clearZones[i].radius)
                        return false;

                if (!isGroundLitter && RoadDistance(x, z) < RoadClearDist)
                    return false;

                // 上から下へ光線を撃って最初に当たったのが Terrain でなければ置かない（Zone は除く）
                var hits = Physics.RaycastAll(new Vector3(x, gy + 45f, z), Vector3.down, 55f, ~0, QueryTriggerInteraction.Ignore);
                float bestD = float.MaxValue;
                Collider firstCol = null;
                for (int i = 0; i < hits.Length; i++)
                {
                    var hc = hits[i].collider;
                    if (!hc || hc.isTrigger || hc.gameObject.scene != island) continue;
                    if (hc.name.Contains("Zone")) continue;
                    if (hits[i].distance < bestD) { bestD = hits[i].distance; firstCol = hc; }
                }
                if (!(firstCol is TerrainCollider)) return false;

                // 既存の岩・倒木・通路・大ガジュマルの根などにめり込まないか
                foreach (var c in Physics.OverlapSphere(new Vector3(x, gy + 0.4f, z), checkRadius, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!c || c.isTrigger || c is TerrainCollider || c.gameObject.scene != island) continue;
                    if (c.name.Contains("Zone")) continue;
                    return false;
                }
                return true;
            }

            static long CellKey(int a, int b) => ((long)a << 32) ^ (uint)b;

            public bool IsFree(Vector2 xz, float space)
            {
                int ci = Mathf.FloorToInt(xz.x / 8f), cj = Mathf.FloorToInt(xz.y / 8f), r = Mathf.CeilToInt(space / 8f) + 1;
                for (int a = -r; a <= r; a++)
                    for (int b = -r; b <= r; b++)
                        if (_taken.TryGetValue(CellKey(ci + a, cj + b), out var list))
                            for (int k = 0; k < list.Count; k++)
                            {
                                var q = list[k];
                                float minD = (space + q.z) * 0.5f;
                                float dx = xz.x - q.x, dz = xz.y - q.y;
                                if (dx * dx + dz * dz < minD * minD) return false;
                            }
                return true;
            }

            public void Take(Vector2 xz, float space)
            {
                long k = CellKey(Mathf.FloorToInt(xz.x / 8f), Mathf.FloorToInt(xz.y / 8f));
                if (!_taken.TryGetValue(k, out var list)) _taken[k] = list = new List<Vector3>();
                list.Add(new Vector3(xz.x, xz.y, space));
            }

            public string Pick((string path, float w)[] table)
            {
                float sum = 0f;
                for (int i = 0; i < table.Length; i++) sum += table[i].w;
                float r = (float)rnd.NextDouble() * sum, acc = 0f;
                for (int i = 0; i < table.Length; i++)
                {
                    acc += table[i].w;
                    if (r <= acc) return table[i].path;
                }
                return table[table.Length - 1].path;
            }

            public GameObject Spawn(Transform parent, string path, float x, float z, float scale, float normalAlign = 0.15f, float yOffset = -0.02f, float? fixedYaw = null)
            {
                if (!_prefabs.TryGetValue(path, out var info))
                {
                    var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    info = (p, p ? CountTriangles(p) : 0);
                    _prefabs[path] = info;
                }
                if (!info.pref) return null;
                float gy = GroundY(x, z);
                Vector3 n = NormalAt(x, z);
                float yaw = fixedYaw ?? (float)rnd.NextDouble() * 360f;
                var go = SpawnPlant(parent, info.pref, new Vector3(x, gy, z), n, yaw, scale, normalAlign, yOffset);
                string key = info.pref.name;
                counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
                triCounts[key] = triCounts.TryGetValue(key, out int tc) ? tc + info.tris : info.tris;
                return go;
            }
        }
    }
}
