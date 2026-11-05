using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlugDesign
{
    /// <summary>
    /// 島の植え直し ② のうち「岩・倒木・流木の差し替え」（docs/modeling/island_planting.md 7章）。
    /// 古い Map/隠れる物 を非表示にして名前を「隠れる物（古い・消してOK）」に変え、
    /// その下の岩・倒木・流木を同じ場所・同じ向き・高さが近くなる倍率（0.6〜1.4倍）で Map/岩と倒木（Argon） に差し替える。
    /// </summary>
    public static partial class IslandPlanting
    {
        public const string OldCoverName = "隠れる物";
        public const string OldCoverRenamed = "隠れる物（古い・消してOK）";
        public const int RockSwapSeed = 602;

        // 案ごとの割合（外しやすいように冒頭の表1か所にまとめる）
        public static readonly (string path, float weight)[] ForestRockWeights =
        {
            ("Assets/Art/Nature/Rocks/Rock_A.prefab", 0.60f),
            ("Assets/Art/Nature/Rocks/Rock_B.prefab", 0.20f),
            ("Assets/Art/Nature/Rocks/Rock_C.prefab", 0.20f),
        };

        public static readonly (string path, float weight)[] GeneralRockWeights =
        {
            ("Assets/Art/Nature/Rocks/Rock_A.prefab", 0.20f),
            ("Assets/Art/Nature/Rocks/Rock_B.prefab", 0.40f),
            ("Assets/Art/Nature/Rocks/Rock_C.prefab", 0.40f),
        };

        public static readonly (string path, float weight)[] LavaRockWeights =
        {
            ("Assets/Art/Nature/Rocks/LavaRock_A.prefab", 0.34f),
            ("Assets/Art/Nature/Rocks/LavaRock_B.prefab", 0.33f),
            ("Assets/Art/Nature/Rocks/LavaRock_C.prefab", 0.33f),
        };

        public static readonly (string path, float weight)[] MountainRockWeights =
        {
            ("Assets/Art/Nature/Rocks/SaltCrystal_C.prefab", 0.50f),
            ("Assets/Art/Nature/Rocks/Rock_B.prefab", 0.50f),
        };

        public static readonly (string path, float weight)[] TalusWeights =
        {
            ("Assets/Art/Nature/Rocks/Talus_B.prefab", 1.00f),
        };

        public static readonly (string path, float weight)[] FallenLogWeights =
        {
            ("Assets/Art/Nature/Wood/FallenLog_A.prefab", 0.28f),
            ("Assets/Art/Nature/Wood/FallenLog_B.prefab", 0.26f),
            ("Assets/Art/Nature/Wood/FallenLog_C.prefab", 0.26f),
            ("Assets/Art/Nature/Wood/RootPlate_A.prefab", 0.07f),
            ("Assets/Art/Nature/Wood/RootPlate_B.prefab", 0.07f),
            ("Assets/Art/Nature/Wood/RootPlate_C.prefab", 0.06f),
        };

        public static readonly (string path, float weight)[] StumpWeights =
        {
            ("Assets/Art/Nature/Wood/Stump_A.prefab", 0.34f),
            ("Assets/Art/Nature/Wood/Stump_B.prefab", 0.33f),
            ("Assets/Art/Nature/Wood/Stump_C.prefab", 0.33f),
        };

        public static readonly (string path, float weight)[] DriftwoodWeights =
        {
            ("Assets/Art/Nature/Wood/Driftwood_A.prefab", 0.34f),
            ("Assets/Art/Nature/Wood/Driftwood_B.prefab", 0.33f),
            ("Assets/Art/Nature/Wood/Driftwood_C.prefab", 0.33f),
        };

        public static string SwapRocksAndLogs(Scene island, bool forceRebuild = true)
        {
            var t = FindIslandTerrain(island);
            if (!t) return "Terrain not found in IslandTest_800";
            var map = t.transform.parent;

            // 古い「隠れる物」を探して非表示＋改名
            Transform oldCover = map.Find(OldCoverRenamed) ?? map.Find(OldCoverName);
            if (!oldCover) return "Map/隠れる物 not found";
            oldCover.name = OldCoverRenamed;

            var existingGroup = map.Find(RockGroupName);
            if (existingGroup && existingGroup.childCount > 0 && !forceRebuild)
            {
                oldCover.gameObject.SetActive(false);
                return $"Map/{RockGroupName} already exists ({existingGroup.childCount} groups), kept as-is.";
            }

            ClearSubgroup(map, RockGroupName);
            var rockRoot = GetOrCreateGroup(map, RockGroupName);

            var rnd = new System.Random(RockSwapSeed);
            var counts = new Dictionary<string, int>();
            var triCounts = new Dictionary<string, int>();
            var prefCache = new Dictionary<string, (GameObject pref, float height, int tris)>();

            (GameObject pref, float height, int tris) LoadPref(string path)
            {
                if (prefCache.TryGetValue(path, out var info)) return info;
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                float h = 1f;
                int tr = 0;
                if (p)
                {
                    tr = CountTriangles(p);
                    var mf = p.GetComponentInChildren<MeshFilter>(true);
                    if (mf && mf.sharedMesh) h = Mathf.Max(0.25f, mf.sharedMesh.bounds.size.y);
                }
                info = (p, h, tr);
                prefCache[path] = info;
                return info;
            }

            string Pick((string path, float weight)[] table)
            {
                float sum = 0f;
                for (int i = 0; i < table.Length; i++) sum += table[i].weight;
                float r = (float)rnd.NextDouble() * sum, acc = 0f;
                for (int i = 0; i < table.Length; i++)
                {
                    acc += table[i].weight;
                    if (r <= acc) return table[i].path;
                }
                return table[table.Length - 1].path;
            }

            void Record(string key, int tris)
            {
                counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
                triCounts[key] = triCounts.TryGetValue(key, out int tc) ? tc + tris : tris;
            }

            Vector3 tp = t.transform.position;
            float GroundY(float x, float z) => t.SampleHeight(new Vector3(x, 0f, z)) + tp.y;

            // bounds を正しく読むため一時的に表示して計算し、最後に非表示にする
            bool wasActive = oldCover.gameObject.activeSelf;
            oldCover.gameObject.SetActive(true);
            try
            {
                foreach (Transform cat in oldCover)
                {
                    string catName = cat.name;
                    // 植物の仮オブジェクト（森の木・下草・草むら・茂み・乾いた茂み）はここでは差し替えない
                    if (catName == "森の木" || catName == "下草" || catName == "草むら" || catName == "茂み" || catName == "乾いた茂み")
                        continue;

                    var subGroup = GetOrCreateGroup(rockRoot, catName);

                    // 台地の岩の柱・塩田の大岩：合う物がないので古いまま複製して新しいグループへ入れる
                    if (catName == "台地の岩の柱" || catName == "塩田の大岩")
                    {
                        foreach (Transform item in cat)
                        {
                            var copy = UnityEngine.Object.Instantiate(item.gameObject, subGroup, true);
                            copy.name = item.name;
                            copy.SetActive(true);
                            Record(catName + "（元の形を維持）", CountTriangles(copy));
                        }
                        continue;
                    }

                    (string path, float weight)[] table = null;
                    bool isLog = false, isDrift = false;
                    if (catName == "森の岩") table = ForestRockWeights;
                    else if (catName == "草原の岩" || catName == "低い土地の岩" || catName == "砂漠の岩") table = GeneralRockWeights;
                    else if (catName == "溶岩の岩") table = LavaRockWeights;
                    else if (catName == "岩塩の山の岩") table = MountainRockWeights;
                    else if (catName == "谷の落石") table = TalusWeights;
                    else if (catName == "倒木") { table = FallenLogWeights; isLog = true; }
                    else if (catName == "流木") { table = DriftwoodWeights; isDrift = true; }
                    else continue;

                    foreach (Transform item in cat)
                    {
                        string path = Pick(table);
                        var (pref, prefH, tris) = LoadPref(path);
                        if (!pref) continue;

                        Vector3 pos = item.position;
                        float gy = GroundY(pos.x, pos.z);
                        var rend = item.GetComponentInChildren<Renderer>(true);
                        float oldH = rend ? rend.bounds.size.y : item.localScale.y;

                        // 高さが近くなるように 0.6〜1.4 倍
                        float scale = Mathf.Clamp(oldH / prefH, 0.6f, 1.4f);
                        if (isLog || isDrift)
                            scale = Mathf.Clamp(Mathf.Lerp(0.75f, 1.2f, (float)rnd.NextDouble()), 0.6f, 1.4f);

                        // わくさんが手で動かした高さを尊重しつつ、底が地面に馴染む位置に置く
                        float baseY = gy;
                        if (rend && Mathf.Abs(rend.bounds.min.y - gy) > 1.2f)
                            baseY = rend.bounds.min.y;

                        float yaw = item.eulerAngles.y;
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(pref, subGroup);
                        go.transform.SetPositionAndRotation(new Vector3(pos.x, baseY - 0.04f, pos.z), Quaternion.Euler(0f, yaw, 0f));
                        go.transform.localScale = Vector3.one * scale;
                        Record(pref.name, tris);

                        // 森の倒木の近くには切り株（Stump）を添える
                        if (isLog && rnd.NextDouble() < 0.65)
                        {
                            string stumpPath = Pick(StumpWeights);
                            var (stumpPref, _, stumpTris) = LoadPref(stumpPath);
                            if (stumpPref)
                            {
                                float ang = (yaw + Mathf.Lerp(-35f, 35f, (float)rnd.NextDouble())) * Mathf.Deg2Rad;
                                float dist = Mathf.Lerp(2.4f, 3.8f, (float)rnd.NextDouble());
                                float sx = pos.x + Mathf.Cos(ang) * dist, sz = pos.z + Mathf.Sin(ang) * dist;
                                float sy = GroundY(sx, sz);
                                if (Mathf.Abs(sy - baseY) < 1.2f)
                                {
                                    var st = (GameObject)PrefabUtility.InstantiatePrefab(stumpPref, subGroup);
                                    float stScale = Mathf.Lerp(0.8f, 1.2f, (float)rnd.NextDouble());
                                    st.transform.SetPositionAndRotation(new Vector3(sx, sy - 0.03f, sz), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f));
                                    st.transform.localScale = Vector3.one * stScale;
                                    Record(stumpPref.name, stumpTris);
                                }
                            }
                        }

                        // 密度2倍：各岩・倒木・流木の近く（4.0〜8.5 m）に相棒の岩・倒木・流木をもう1つ添える（道・水没・急斜面は避ける）
                        for (int attempt = 0; attempt < 5; attempt++)
                        {
                            float cAng = (float)rnd.NextDouble() * Mathf.PI * 2f;
                            float cDist = Mathf.Lerp(4.0f, 8.5f, (float)rnd.NextDouble());
                            float cx = pos.x + Mathf.Cos(cAng) * cDist, cz = pos.z + Mathf.Sin(cAng) * cDist;
                            float cy = GroundY(cx, cz);
                            if (cy <= 0.3f || Mathf.Abs(cy - gy) > 2.2f) continue;
                            float steep = t.terrainData.GetSteepness(
                                Mathf.Clamp01((cx - tp.x) / t.terrainData.size.x),
                                Mathf.Clamp01((cz - tp.z) / t.terrainData.size.z));
                            if (steep > 34f) continue;

                            string cPath = Pick(table);
                            var (cPref, _, cTris) = LoadPref(cPath);
                            if (!cPref) break;
                            float cScale = Mathf.Clamp(scale * Mathf.Lerp(0.72f, 1.08f, (float)rnd.NextDouble()), 0.6f, 1.4f);
                            var cGo = (GameObject)PrefabUtility.InstantiatePrefab(cPref, subGroup);
                            cGo.transform.SetPositionAndRotation(new Vector3(cx, cy - 0.04f, cz), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f));
                            cGo.transform.localScale = Vector3.one * cScale;
                            Record(cPref.name, cTris);
                            break;
                        }
                    }
                }
            }
            finally
            {
                // 古い「隠れる物」は消さずに非表示にする
                oldCover.gameObject.SetActive(false);
            }

            int totalObjs = 0, totalTris = 0;
            var sb = new StringBuilder();
            sb.AppendLine($"Rocks & Logs swapped into Map/{RockGroupName} (Map/{OldCoverRenamed} hidden):");
            foreach (var kv in counts)
            {
                totalObjs += kv.Value;
                totalTris += triCounts[kv.Key];
                sb.AppendLine($"- {kv.Key}: {kv.Value} ({triCounts[kv.Key]:N0} tris)");
            }
            sb.Append($"- Total rock/log objects: {totalObjs}, Total triangles: {totalTris:N0}");
            return sb.ToString();
        }
    }
}
