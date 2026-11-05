using System.IO;
using System.Text;
using SlugWorld;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// ケルン風の見た目を試すときの、前と後の絵（docs/ideas/cairn_terrain_ideas.md）。IslandTest_800 を開いて、
    /// 同じ場所・同じ時刻・晴れでナメクジの目の高さから撮り、Screenshots/CairnTry/&lt;tag&gt;/ に置く。
    /// Compare で before と after を上下に並べた絵を作る。天気は撮るときだけ止めて当てる（シーンは保存しない）。
    /// </summary>
    public static class CairnTryShots
    {
        const string Dir = "Screenshots/CairnTry";

        struct Shot { public string file; public Vector2 from, to; public float up, lookUp, hour; }

        // 場所は IslandTest800Builder の Views と同じ地図の座標。hour: 0..24 のゲームの時刻（負なら朝夕を探す：-1 朝、-2 夕方）
        static readonly Shot[] Shots =
        {
            new Shot { file = "1_canyon_mouth", from = new Vector2(228, 232), to = new Vector2(300, 252), up = 0.47f, lookUp = 12f, hour = 13f },
            new Shot { file = "2_summit_west", from = new Vector2(365, 260), to = new Vector2(200, 245), up = 0.47f, lookUp = -70f, hour = 13f },
            new Shot { file = "3_canyon_rim", from = new Vector2(302, 236), to = new Vector2(306, 262), up = 0.47f, lookUp = -45f, hour = 13f },
            new Shot { file = "4_meadow_mountain", from = new Vector2(346, 378), to = new Vector2(368, 316), up = 0.47f, lookUp = 10f, hour = 13f },
            new Shot { file = "5_forest_camp_mountain", from = new Vector2(500, 300), to = new Vector2(375, 262), up = 0.47f, lookUp = 60f, hour = 13f },
            new Shot { file = "6_meadow_dusk", from = new Vector2(346, 378), to = new Vector2(368, 316), up = 0.47f, lookUp = 10f, hour = -2f },
            new Shot { file = "7_summit_morning", from = new Vector2(365, 260), to = new Vector2(200, 245), up = 0.47f, lookUp = -70f, hour = -1f },
        };

        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Shoot Before")] public static string ShootBefore() => WithIsland(() => Shoot("before"), false);
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Shoot After")] public static string ShootAfter() => WithIsland(() => Shoot("after"), false);

        const string IslandScene = "Assets/Scenes/Game/IslandTest_800.unity";

        /// <summary>
        /// Runs <paramref name="work"/> with IslandTest_800 as the active scene without disturbing what is open (the editor is
        /// shared): opens it beside the open scene if needed, makes it active, then gives the active scene back and closes
        /// it again. <paramref name="save"/> keeps what the work changed in the island scene.
        /// </summary>
        public static string WithIsland(System.Func<string> work, bool save)
        {
            var prev = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var island = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(IslandScene);
            bool wasOpen = island.IsValid() && island.isLoaded;
            if (!wasOpen) island = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(IslandScene, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(island);
            string r;
            try { r = work(); }
            finally
            {
                if (save) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(island);
                if (prev.IsValid() && prev != island) UnityEngine.SceneManagement.SceneManager.SetActiveScene(prev);
                if (!wasOpen && prev.IsValid() && prev != island) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(island, true);
                if (!wasOpen) ClearAir();
            }
            return r;
        }

        /// <summary>
        /// The weather's shader globals stay set after the island scene closes in the editor (its OnDisable does not run out
        /// of play mode), which would haze other scenes' pictures. Clears them.
        /// </summary>
        public static void ClearAir()
        {
            foreach (var g in new[] { "_SlugAir", "_SlugShadeTint", "_SlugMist", "_SlugGlowRange", "_SlugGroundRect" })
                Shader.SetGlobalVector(g, Vector4.zero);
        }

        public static string Shoot(string tag)
        {
            var t = Object.FindAnyObjectByType<Terrain>();
            var w = Object.FindAnyObjectByType<WeatherPresenter>();
            if (!t || !w) return "open IslandTest_800 first";
            if (!w.sun)
                foreach (var l in Object.FindObjectsByType<Light>())
                    if (l.type == LightType.Directional) { w.sun = l; break; }
            var sb = new StringBuilder();
            foreach (var s in Shots)
            {
                float hour = s.hour >= 0f ? s.hour : FindHour(w, s.hour < -1.5f ? 18.5f : 6.5f, s.hour < -1.5f ? 4f : 7f);
                w.ShowNow(hour, IslandSettings.DevSky.Sunny);
                Vector3 from = Ground(t, s.from) + Vector3.up * s.up, to = Ground(t, s.to) + Vector3.up * s.lookUp;
                string path = $"{Dir}/{tag}/{s.file}.png";
                Save(path, Render(w, from, to, 60f, 1200, 600, 3000f));
                sb.AppendLine($"{path}  (hour {hour:0.0})");
            }
            return sb.ToString();
        }

        /// <summary>Before above after, one picture per place (Screenshots/CairnTry/compare_*.png).</summary>
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Compare")]
        public static string Compare()
        {
            var sb = new StringBuilder();
            foreach (var s in Shots)
            {
                string a = $"{Dir}/before/{s.file}.png", b = $"{Dir}/after/{s.file}.png";
                if (!File.Exists(a) || !File.Exists(b)) continue;
                var ta = new Texture2D(2, 2); ta.LoadImage(File.ReadAllBytes(a));
                var tb = new Texture2D(2, 2); tb.LoadImage(File.ReadAllBytes(b));
                var o = new Texture2D(ta.width, ta.height + tb.height + 6, TextureFormat.RGB24, false);
                var fill = new Color[o.width * o.height];
                for (int i = 0; i < fill.Length; i++) fill[i] = Color.white;
                o.SetPixels(fill);
                o.SetPixels(0, tb.height + 6, ta.width, ta.height, ta.GetPixels());
                o.SetPixels(0, 0, tb.width, tb.height, tb.GetPixels());
                o.Apply();
                string path = $"{Dir}/compare_{s.file}.png";
                Save(path, o);
                Object.DestroyImmediate(ta); Object.DestroyImmediate(tb);
                sb.AppendLine(path);
            }
            return sb.ToString();
        }

        /// <summary>One picture from anywhere (world positions), at a game hour, clear sky.</summary>
        public static string ShotAt(string file, Vector3 from, Vector3 to, float hour = 13f, float fov = 60f, bool air = true)
        {
            var w = Object.FindAnyObjectByType<WeatherPresenter>();
            if (!w) return "open IslandTest_800 first";
            if (!w.sun)
                foreach (var l in Object.FindObjectsByType<Light>())
                    if (l.type == LightType.Directional) { w.sun = l; break; }
            bool keep = w.airLook;
            w.airLook = air;                                    // (a view from high above reads better without the haze)
            w.ShowNow(hour, IslandSettings.DevSky.Sunny);
            string path = $"{Dir}/{file}.png";
            Save(path, Render(w, from, to, fov, 1200, 650, 3000f));
            w.airLook = keep;
            return path;
        }

        /// <summary>The game hour near <paramref name="guess"/> when the sun stands <paramref name="degrees"/> above the horizon.</summary>
        static float FindHour(WeatherPresenter w, float guess, float degrees)
        {
            float best = guess, bestErr = float.MaxValue, target = Mathf.Sin(degrees * Mathf.Deg2Rad);
            for (float h = guess - 2.5f; h <= guess + 2.5f; h += 0.05f)
            {
                w.ShowNow(h, IslandSettings.DevSky.Sunny);
                float err = Mathf.Abs(-w.Time.sunDirection.y - target);
                if (err < bestErr) { bestErr = err; best = h; }
            }
            return best;
        }

        static Vector3 Ground(Terrain t, Vector2 svg)
        {
            var p = IslandTest800Builder.World(svg);
            return new Vector3(p.x, t.SampleHeight(new Vector3(p.x, 0f, p.y)) + t.transform.position.y, p.y);
        }

        static Texture2D Render(WeatherPresenter w, Vector3 position, Vector3 lookAt, float fov, int width, int height, float far)
        {
            var go = new GameObject("CairnTryCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = far;
            cam.clearFlags = RenderSettings.skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = w.SkyColor;
            go.transform.position = position;
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
            Object.DestroyImmediate(go);
            return tex;
        }

        static void Save(string path, Texture2D tex)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
