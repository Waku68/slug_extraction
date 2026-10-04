using System.IO;
using System.Reflection;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Debug (play mode): drives the slug's movement by hand (Jump held from <c>holdFrom</c> to <c>holdUntil</c>
    /// seconds, a fixed move input) and tiles what the player's camera (and optionally a second camera) sees at the
    /// given times into one image. Frames do not depend on the editor's frame rate.
    /// </summary>
    public static class MoveFilmStrip
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        public static string Capture(string path, float[] times, float holdFrom, float holdUntil, Vector2 input,
                                     Camera other = null, int width = 480, int height = 270, int cols = 4)
        {
            var ctrl = Object.FindAnyObjectByType<SlugMoveController>();
            if (!ctrl) return "no SlugMoveController (play mode?)";
            var rig = ctrl.cameraRig;
            var lateRig = typeof(SlugCameraRig).GetMethod("LateUpdate", Private);
            var lateGun = typeof(SlugGunHolder).GetMethod("LateUpdate", Private);
            var latePick = typeof(SlugToolHolder).GetMethod("LateUpdate", Private);

            var cams = other ? new[] { rig.cam, other } : new[] { rig.cam };
            int frames = times.Length * cams.Length;
            int rows = Mathf.CeilToInt(frames / (float)cols);
            var sheet = new Texture2D(width * cols, height * rows, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var read = new Texture2D(width, height, TextureFormat.RGB24, false);

            const float step = 1f / 240f;
            for (int i = 0; i < 240; i++) ctrl.Simulate(step, Vector2.zero, false);   // settle
            Vector3 start = ctrl.transform.position;
            float t = 0f;
            int k = 0;
            var log = new System.Text.StringBuilder();
            foreach (float target in times)
            {
                while (t < target - 1e-5f)
                {
                    ctrl.Simulate(step, input, t >= holdFrom && t < holdUntil);
                    t += step;
                }
                lateRig.Invoke(rig, null);
                foreach (var g in Object.FindObjectsByType<SlugGunHolder>()) if (g.isActiveAndEnabled) lateGun.Invoke(g, null);
                foreach (var p in Object.FindObjectsByType<SlugToolHolder>()) if (p.isActiveAndEnabled) latePick.Invoke(p, null);
                Vector3 d = ctrl.transform.position - start;
                log.Append($"{target:F2}s charge {ctrl.LungeCharge:F2} lunging {ctrl.Lunging} moved {new Vector2(d.x, d.z).magnitude:F2}m up {d.y:F2} scale {ctrl.body.localScale.z:F2} | ");
                foreach (var cam in cams)
                {
                    var prev = cam.targetTexture;
                    float aspect = cam.aspect;
                    cam.targetTexture = rt;
                    cam.aspect = width / (float)height;
                    cam.Render();
                    cam.targetTexture = prev;
                    cam.aspect = aspect;
                    RenderTexture.active = rt;
                    read.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    read.Apply();
                    RenderTexture.active = null;
                    sheet.SetPixels((k % cols) * width, (rows - 1 - k / cols) * height, width, height, read.GetPixels());
                    k++;
                }
            }
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(read);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            return log.ToString();
        }
    }
}
