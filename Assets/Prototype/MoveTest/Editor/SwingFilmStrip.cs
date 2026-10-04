using System.IO;
using System.Reflection;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Debug (play mode): starts one swing and steps it by hand, tiling what the player's camera (and optionally a
    /// second camera) sees at the given times into one image. Frames do not depend on the editor's frame rate.
    /// </summary>
    public static class SwingFilmStrip
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        public static string Capture(string path, float[] times, Camera other = null, int width = 480, int height = 270, int cols = 4)
        {
            var holder = Object.FindAnyObjectByType<SlugToolHolder>();
            if (!holder) return "no SlugToolHolder (play mode?)";
            var rig = holder.cameraRig;
            var ctrl = holder.controller;
            var begin = typeof(SlugToolHolder).GetMethod("BeginSwing", Private);
            var lateHolder = typeof(SlugToolHolder).GetMethod("LateUpdate", Private);
            var lateRig = typeof(SlugCameraRig).GetMethod("LateUpdate", Private);
            var align = typeof(SlugMoveController).GetMethod("AlignBody", Private);

            var cams = other ? new[] { rig.cam, other } : new[] { rig.cam };
            int frames = times.Length * cams.Length;
            int rows = Mathf.CeilToInt(frames / (float)cols);
            var sheet = new Texture2D(width * cols, height * rows, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var read = new Texture2D(width, height, TextureFormat.RGB24, false);

            const float step = 1f / 240f;
            // let a swing in progress settle first, so every strip starts from the same ready pose
            for (int i = 0; i < 240 * 4 && holder.Swing.Phase != SwingPhase.Idle; i++) holder.Simulate(step);
            begin.Invoke(holder, null);
            var view = rig.cam.transform.Find("ViewPickaxe");
            float t = 0f;
            int k = 0;
            var log = new System.Text.StringBuilder();
            foreach (float target in times)
            {
                while (t < target - 1e-5f)
                {
                    holder.Simulate(step);
                    align.Invoke(ctrl, new object[] { step });
                    t += step;
                }
                lateRig.Invoke(rig, null);
                lateHolder.Invoke(holder, null);
                log.Append($"{target:F2}s {holder.Swing.Phase}");
                if (view && view.gameObject.activeInHierarchy)
                {
                    Vector3 g = rig.cam.WorldToViewportPoint(view.TransformPoint(ToolPose.GripLowLocal));
                    Vector3 hd = rig.cam.WorldToViewportPoint(view.TransformPoint(SlugPickaxe.HeadLocal));
                    log.Append($" grip({g.x:F2},{g.y:F2}) head({hd.x:F2},{hd.y:F2}) x{view.localScale.x:F2}");
                }
                log.Append(" | ");
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
