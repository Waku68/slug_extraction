using System.IO;
using SlugWorld;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Development: renders the player's view (and a view from outside) at several times of day and skies, side by side,
    /// into Screenshots/Weather. Run in play mode (ExtractTest), e.g. from the MCP:
    /// <c>WeatherFilmStrip.Shoot("day_cycle", new[] { 7f, 12f, 18.2f, 23f }, null)</c>.
    /// </summary>
    public static class WeatherFilmStrip
    {
        public static string Shoot(string name, float[] gameHours, IslandSettings.DevSky[] skies, bool outside = true, int width = 640, int height = 360)
        {
            var presenter = WeatherPresenter.Current;
            if (!presenter) return "no WeatherPresenter";
            SlugMoisture player = null;
            foreach (var m in Object.FindObjectsByType<SlugMoisture>()) player = m;
            var eye = player && player.cameraRig ? player.cameraRig.cam : Camera.main;
            int n = Mathf.Max(gameHours != null ? gameHours.Length : 0, skies != null ? skies.Length : 0);
            int rows = outside ? 2 : 1;
            var sheet = new Texture2D(width * n, height * rows, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var read = new Texture2D(width, height, TextureFormat.RGB24, false);
            var go = new GameObject("FilmCam");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = 50f;
            go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            for (int i = 0; i < n; i++)
            {
                float h = gameHours != null && gameHours.Length > 0 ? gameHours[Mathf.Min(i, gameHours.Length - 1)] : presenter.Time.GameHour;
                var sky = skies != null && skies.Length > 0 ? skies[Mathf.Min(i, skies.Length - 1)] : IslandSettings.DevSky.Auto;
                presenter.ShowNow(h, sky);
                for (int row = 0; row < rows; row++)
                {
                    Camera use = row == 0 ? eye : cam;
                    if (row == 1 && player)
                    {
                        var t = player.controller.transform;
                        Vector3 c = t.position + Vector3.up * 0.4f;
                        go.transform.position = c - t.forward * 4.5f + t.right * 3f + Vector3.up * 2.2f;
                        go.transform.LookAt(c + t.forward * 4f);
                    }
                    if (use == cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = presenter.SkyColor; }
                    var prevT = use.targetTexture;
                    float asp = use.aspect;
                    use.targetTexture = rt;
                    use.aspect = width / (float)height;
                    use.Render();
                    use.targetTexture = prevT;
                    use.aspect = asp;
                    RenderTexture.active = rt;
                    read.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    read.Apply();
                    RenderTexture.active = null;
                    sheet.SetPixels(i * width, (rows - 1 - row) * height, width, height, read.GetPixels());
                }
            }
            presenter.ShowNow(presenter.Time.GameHour, IslandSettings.DevSky.Auto);
            RenderTexture.ReleaseTemporary(rt);
            Object.Destroy(go);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "Weather"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + ".png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            return path;
        }
    }
}
