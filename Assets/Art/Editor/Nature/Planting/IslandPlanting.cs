using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SlugExtract;
using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlugDesign
{
    /// <summary>
    /// 島の植え直し（docs/modeling/island_planting.md、Nov 5 Argon）。
    /// 1. 岩塩の山の草（1 Mountain Grass）：RegionAt[5] &gt; 0.6、坂 40° 以下の歩ける所の「ふち」と「岩・壁の根元（0.3〜1.0 m）」だけ。
    ///    ふち 15〜25 m に 1 株（標高が高いほどまばら、100 m より上は置かない）。主役 Grass_A（0.6〜0.8 倍）または Grass_B に、子株と足元の Grass_D を添える。
    /// 2. 島全体の植え直し（2 Replant Island）：森の木は約 8 m おき、区画の境目は約 20 m 幅で混ぜ、足元の層は道の脇・木の根元・目印のまわりだけ。
    /// 3. 岩と倒木の差し替え（3 Swap Rocks And Logs）：古い「隠れる物」の岩・倒木・流木を同じ場所・向きで新しいプレハブに差し替える。
    /// </summary>
    public static partial class IslandPlanting
    {
        public const string IslandScenePath = "Assets/Scenes/Game/IslandTest_800.unity";
        public const string ShotDir = "Screenshots/Planting";
        public const string PlantGroupName = "植物（Argon）";
        public const string RockGroupName = "岩と倒木（Argon）";
        public const string MountainGrassSubgroup = "岩塩の山の草";

        // ───────── ① 岩塩の山の草の数字（m・度・割合） ─────────
        public const int MountainSeed = 501;
        public const float MountainRegionMin = 0.6f;     // RegionAt[5]（岩塩の山）の下限
        public const float MountainMaxHeight = 100f;     // 山頂の頭（100 m より上）には置かない
        public const float MountainMinHeight = 12f;      // 山のすその下限
        public const float MountainWalkMaxSlope = 40f;   // 歩ける坂の上限（度）
        public const float EdgeProbeDist = 1.2f;         // ふち判定の距離（8方向）
        public const float EdgeSteepSlope = 45f;         // ふち判定：隣がこれより急
        public const float EdgeStepHeight = 0.45f;       // ふち判定：隣との段差がこれより大きい
        public const float RockBaseMinDist = 0.3f;       // 岩・壁の根元からの最短距離
        public const float RockBaseMaxDist = 1.0f;       // 岩・壁の根元からの最長距離
        public const float MountainSpaceLow = 10.5f;     // 低い所での株の間隔（m、密度2倍）
        public const float MountainSpaceHigh = 17.5f;    // 高い所での株の間隔（m、密度2倍）
        public const float GrassAMinScale = 0.6f;        // Grass_A の大きさ（下限）
        public const float GrassAMaxScale = 0.8f;        // Grass_A の大きさ（上限）
        public const float GrassBMinScale = 0.85f;       // Grass_B の大きさ（下限）
        public const float GrassBMaxScale = 1.05f;       // Grass_B の大きさ（上限）
        public const float GrassARatio = 0.55f;          // 主役に Grass_A を選ぶ割合（残りは Grass_B）

        // ───────── ② 島全体の数字（わくさん指定：密度2倍、境目 20m・足元は道脇と根元） ─────────
        public const float ForestTreeSpacing = 5.6f;     // 森の木の間隔（m、8m の密度2倍）
        public const float RegionBlendWidth = 20f;       // 区画の境目の混ぜ幅（m）
        public const float RoadClearDist = 2.5f;         // 道から隠れる物・塊の層を空ける距離（m）
        public const float WalkwayClearDist = 2.0f;      // 通路・橋から空ける距離（m）
        public const float CampClearRadius = 12f;        // キャンプ・たまり池・泉を空ける半径（m）

        // ───────── メニュー ─────────

        [MenuItem("Slug/Nature/Planting/1 Mountain Grass")]
        public static void MenuMountainGrass()
        {
            string log = WithIsland(scene => BuildMountainGrass(scene), save: true);
            Debug.Log($"[IslandPlanting] {log}");
        }

        [MenuItem("Slug/Nature/Planting/2 Replant Island")]
        public static void MenuReplantIsland()
        {
            string log = WithIsland(scene => ReplantIsland(scene), save: true);
            Debug.Log($"[IslandPlanting] {log}");
        }

        [MenuItem("Slug/Nature/Planting/3 Swap Rocks And Logs")]
        public static void MenuSwapRocksAndLogs()
        {
            string log = WithIsland(scene => SwapRocksAndLogs(scene, forceRebuild: true), save: true);
            Debug.Log($"[IslandPlanting] {log}");
        }

        [MenuItem("Slug/Nature/Planting/4 Shots")]
        public static void MenuShots()
        {
            string log = WithIsland(scene => ShootAll(scene), save: false);
            Debug.Log($"[IslandPlanting] Shots saved:\n{log}");
        }

        [MenuItem("Slug/Nature/Planting/5 Bake Cairn Ground")]
        public static void MenuBakeCairnGround()
        {
            string log = WithIsland(scene => IslandToonGround.On(), save: true);
            Debug.Log($"[IslandPlanting] Cairn ground baked: {log}");
        }

        // ───────── シーンを安全に開いて閉じる共通処理 ─────────

        public static string WithIsland(Func<Scene, string> work, bool save)
        {
            var prev = EditorSceneManager.GetActiveScene();
            var island = SceneManager.GetSceneByPath(IslandScenePath);
            bool wasOpen = island.IsValid() && island.isLoaded;
            if (!wasOpen)
                island = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Additive);

            // 他の開いているシーンのライトと描画を一時的に伏せる（絵や光線に混ざらないように・dirtyは変えない）
            var hiddenRenderers = new List<Renderer>();
            var hiddenLights = new List<Light>();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var sc = SceneManager.GetSceneAt(s);
                if (sc == island || !sc.isLoaded) continue;
                foreach (var root in sc.GetRootGameObjects())
                {
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                        if (!r.forceRenderingOff) { r.forceRenderingOff = true; hiddenRenderers.Add(r); }
                    foreach (var l in root.GetComponentsInChildren<Light>(true))
                        if (l.enabled) { l.enabled = false; hiddenLights.Add(l); }
                }
            }

            SceneManager.SetActiveScene(island);
            try
            {
                return work(island);
            }
            finally
            {
                foreach (var r in hiddenRenderers) if (r) r.forceRenderingOff = false;
                foreach (var l in hiddenLights) if (l) l.enabled = true;
                if (save)
                {
                    EditorSceneManager.MarkSceneDirty(island);
                    EditorSceneManager.SaveScene(island);
                }
                if (prev.IsValid() && prev != island)
                    SceneManager.SetActiveScene(prev);
                if (!wasOpen && prev.IsValid() && prev != island)
                    EditorSceneManager.CloseScene(island, true);
                if (!wasOpen)
                    CairnTryShots.ClearAir();
            }
        }

        // ───────── 共通ヘルパー ─────────

        public static Terrain FindIslandTerrain(Scene island)
        {
            foreach (var root in island.GetRootGameObjects())
            {
                var t = root.GetComponentInChildren<Terrain>(true);
                if (t) return t;
            }
            return null;
        }

        public static Transform GetOrCreateGroup(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == name) return c;
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        public static void ClearSubgroup(Transform parent, string name)
        {
            var toDelete = new List<GameObject>();
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == name) toDelete.Add(c.gameObject);
            }
            foreach (var go in toDelete)
                UnityEngine.Object.DestroyImmediate(go);
        }

        public static int CountTriangles(GameObject go)
        {
            int tris = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh) tris += mf.sharedMesh.triangles.Length / 3;
            return tris;
        }

        // ───────── 共通撮影ヘルパーと島全体撮影（1ファイル500行以内を守るためここに配置） ─────────

        public static void SaveShot(WeatherPresenter w, string path, Vector3 pos, Vector3 lookAt, float fov, int width, int height, bool air)
        {
            bool keepAir = w.airLook;
            w.airLook = air;
            w.ShowNow(13f, IslandSettings.DevSky.Sunny);
            var go = new GameObject("PlantingShotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 3000f;
            cam.clearFlags = RenderSettings.skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = w.SkyColor;
            go.transform.position = pos;
            go.transform.LookAt(lookAt);
            var rt = RenderTexture.GetTemporary(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(go);
            w.airLook = keepAir;

            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        public static string ShootAll(Scene island)
        {
            var t = FindIslandTerrain(island);
            var w = UnityEngine.Object.FindAnyObjectByType<WeatherPresenter>();
            if (!t || !w) return "Terrain or WeatherPresenter missing";
            if (!w.sun)
                foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                    if (l.gameObject.scene == island && l.type == LightType.Directional) { w.sun = l; break; }

            Directory.CreateDirectory(ShotDir);
            var sb = new StringBuilder();
            sb.AppendLine(ShootMountainGrass(island));

            Vector3 OnGround(Vector2 svg, float up = 0f)
            {
                Vector2 p = IslandTest800Builder.World(svg);
                return new Vector3(p.x, t.SampleHeight(new Vector3(p.x, 0f, p.y)) + t.transform.position.y + up, p.y);
            }

            string fForest = $"{ShotDir}/island_A1_forest_eye.png";
            SaveShot(w, fForest, OnGround(new Vector2(532f, 294f), 0.47f), OnGround(new Vector2(512f, 278f), 1.6f), 58f, 768, 512, true);
            sb.AppendLine(fForest);

            string fMeadow = $"{ShotDir}/island_A2_meadow_eye.png";
            SaveShot(w, fMeadow, OnGround(new Vector2(342f, 378f), 0.47f), OnGround(new Vector2(362f, 336f), 3.0f), 58f, 768, 512, true);
            sb.AppendLine(fMeadow);

            var beachGroup = t.transform.parent.Find($"{PlantGroupName}/砂漠と浜");
            Transform heroPalm = null;
            if (beachGroup)
            {
                foreach (Transform c in beachGroup)
                    if (c.name.StartsWith("Palm_A") && c.position.z < 0f) { heroPalm = c; break; }
                if (!heroPalm)
                    foreach (Transform c in beachGroup)
                        if (c.name.StartsWith("Palm_A")) { heroPalm = c; break; }
            }
            Vector3 palmPos = heroPalm ? heroPalm.position : OnGround(new Vector2(215f, 358f));
            Vector3 fromBeach = palmPos + new Vector3(6.5f, 0f, -6.5f);
            fromBeach.y = t.SampleHeight(fromBeach) + t.transform.position.y + 0.47f;
            string fBeach = $"{ShotDir}/island_A3_beach_eye.png";
            SaveShot(w, fBeach, fromBeach, palmPos + Vector3.up * 2.2f, 58f, 768, 512, true);
            sb.AppendLine(fBeach);

            var forestGroup = t.transform.parent.Find($"{PlantGroupName}/森");
            Transform heroTree = null;
            if (forestGroup)
                foreach (Transform c in forestGroup)
                    if (c.name.StartsWith("ForestTree_A")) { heroTree = c; break; }
            Vector3 tpPos = heroTree ? heroTree.position : OnGround(new Vector2(525f, 290f));
            var slug = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            SceneManager.MoveGameObjectToScene(slug, island);
            slug.transform.localScale = new Vector3(0.5f, 0.55f, 0.5f);
            Vector3 slugPos = tpPos + new Vector3(2.2f, 0f, 0f);
            slugPos.y = t.SampleHeight(slugPos) + t.transform.position.y + 0.25f;
            slug.transform.SetPositionAndRotation(slugPos, Quaternion.Euler(0f, 0f, 90f));
            var slugMat = new Material(Shader.Find("Slug/Toon") ?? Shader.Find("Universal Render Pipeline/Lit"));
            slugMat.SetColor("_BaseColor", new Color(0.82f, 0.58f, 0.28f));
            slug.GetComponent<Renderer>().sharedMaterial = slugMat;
            try
            {
                Vector3 midB = (tpPos + slugPos) * 0.5f + Vector3.up * 2.2f;
                string fSide = $"{ShotDir}/island_B_side.png";
                SaveShot(w, fSide, midB + new Vector3(0f, 0.8f, -11.5f), midB, 45f, 768, 512, true);
                sb.AppendLine(fSide);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(slug);
                UnityEngine.Object.DestroyImmediate(slugMat);
            }

            Vector3 center = OnGround(new Vector2(366f, 262f));
            string fOver = $"{ShotDir}/island_C_overview.png";
            SaveShot(w, fOver, center + new Vector3(340f, 230f, -380f), center + Vector3.up * 10f, 45f, 768, 512, false);
            sb.AppendLine(fOver);

            return sb.ToString().TrimEnd();
        }
    }
}
