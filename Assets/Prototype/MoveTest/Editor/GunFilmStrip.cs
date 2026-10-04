using System.IO;
using System.Reflection;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Debug (play mode): drives the rifle by hand and tiles what the player's camera (and optionally a second camera)
    /// sees at the given times into one image. The trigger is held from 0 to <c>fireUntil</c> seconds and a reload starts
    /// at <c>reloadAt</c> (negative = none). Frames do not depend on the editor's frame rate.
    /// </summary>
    public static class GunFilmStrip
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        public static string Capture(string path, float[] times, float fireUntil, float reloadAt, Camera other = null,
                                     int width = 480, int height = 270, int cols = 4, float aimFrom = -1f, float aimUntil = -1f)
        {
            SlugGunHolder holder = null;
            foreach (var g in Object.FindObjectsByType<SlugGunHolder>())
                if (g.isActiveAndEnabled && g.Ready) holder = g;
            if (!holder) return "no gun in hand (play mode?)";
            var rig = holder.cameraRig;
            var ctrl = holder.controller;
            var lateHolder = typeof(SlugGunHolder).GetMethod("LateUpdate", Private);
            var lateRig = typeof(SlugCameraRig).GetMethod("LateUpdate", Private);
            var align = typeof(SlugMoveController).GetMethod("AlignBody", Private);

            var cams = other ? new[] { rig.cam, other } : new[] { rig.cam };
            int frames = times.Length * cams.Length;
            int rows = Mathf.CeilToInt(frames / (float)cols);
            var sheet = new Texture2D(width * cols, height * rows, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var read = new Texture2D(width, height, TextureFormat.RGB24, false);

            const float step = 1f / 240f;
            // settle first: lowered, no recoil left
            for (int i = 0; i < 240 * 2; i++) { holder.Simulate(step, false, false); align.Invoke(ctrl, new object[] { step }); }
            float t = 0f;
            int k = 0;
            bool reloaded = false;
            var log = new System.Text.StringBuilder();
            foreach (float target in times)
            {
                while (t < target - 1e-5f)
                {
                    bool reload = !reloaded && reloadAt >= 0f && t >= reloadAt;
                    if (reload) reloaded = true;
                    holder.Simulate(step, t < fireUntil, reload, t >= aimFrom && t < aimUntil);
                    align.Invoke(ctrl, new object[] { step });
                    t += step;
                }
                lateRig.Invoke(rig, null);
                lateHolder.Invoke(holder, null);
                var h = holder.Handling;
                log.Append($"{target:F2}s raise {h.Raise:F2} ammo {h.Ammo} cyc {h.Cycle:F2} {h.ReloadStage} {h.StageProgress:F2} scope {h.Scope:F2} kick {h.Kick * 100f:F1}cm | ");
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
