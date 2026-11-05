using System.IO;
using UnityEditor;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Debug (play mode, DinoMoveTest): works the player's tools by script and tiles what some cameras see at the given
    /// times into one image (one row per camera). Like <see cref="DrawFilmStrip"/> the game runs at a fixed step meanwhile
    /// and the frames are taken over the next frames, so the skin is posed as the game drew it.
    /// </summary>
    public static class DinoToolStrip
    {
        /// <summary>From <c>at</c> seconds on: carry <c>back</c> on the back (null = keep), take <c>select</c> in hand
        /// (-1 = keep), hold the swing / trigger / aim, walk with <c>move</c> (and <c>run</c>). The cameras are best
        /// parented to the player when it moves. <c>pitch</c>: look up / down to this (degrees, + = down) as it starts.</summary>
        public struct Act { public float at; public int select; public bool swing, trigger, aim, run; public GunKind? back; public Vector2 move; public float? pitch; }

        /// <param name="probe">read out at each shot and added to the log (null = nothing)</param>
        public static string Start(string path, Act[] acts, float[] times, Camera[] cams, int width = 320, int height = 300,
                                   System.Func<string> probe = null)
        {
            var loadout = Object.FindAnyObjectByType<SlugLoadout>();
            if (!loadout) return "no SlugLoadout (play mode?)";
            int rows = cams.Length, cols = times.Length;
            var sheet = new Texture2D(width * cols, height * rows, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var read = new Texture2D(width, height, TextureFormat.RGB24, false);
            Time.captureDeltaTime = 1f / 120f;
            loadout.driven = true;
            if (loadout.pickaxe) loadout.pickaxe.driven = true;
            // walking: the controller is moved from here (one step per game frame) instead of by the keys
            var ctrl = Object.FindAnyObjectByType<SlugMoveController>();
            bool walks = System.Array.Exists(acts, x => x.move != Vector2.zero);
            if (walks && ctrl) ctrl.driven = true;
            int stepped = -1;
            float start = Time.time;
            int next = 0, done = -1;
            var log = new System.Text.StringBuilder();
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; Time.captureDeltaTime = 0f; return; }
                float t = Time.time - start;
                int a = -1;
                for (int i = 0; i < acts.Length; i++) if (acts[i].at <= t + 1e-4f) a = i;
                if (a >= 0)
                {
                    var act = acts[a];
                    if (a != done)
                    {
                        done = a;
                        if (act.back.HasValue) loadout.SetLoadout(act.back.Value, loadout.hip);
                        if (act.select >= 0) loadout.Select(act.select);
                        if (act.pitch.HasValue && ctrl && ctrl.cameraRig) ctrl.cameraRig.AddLook(0f, act.pitch.Value - ctrl.cameraRig.Pitch);
                    }
                    if (loadout.pickaxe) loadout.pickaxe.swingHeld = act.swing;
                    foreach (var g in loadout.guns) if (g) { g.forceTrigger = act.trigger && g.enabled; g.forceAim = act.aim && g.enabled; }
                    if (walks && ctrl && Time.frameCount != stepped)
                    {
                        stepped = Time.frameCount;
                        ctrl.runHeld = act.run;
                        ctrl.Simulate(Time.deltaTime, act.move, false);
                    }
                }
                if (t < times[next] - 1e-4f) return;
                log.Append($"{times[next]:F2}s hand {loadout.InHand} draw {loadout.DrawProgress:F2} {(probe != null ? probe() : "")} | ");
                for (int r = 0; r < rows; r++)
                {
                    var cam = cams[r];
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
                    sheet.SetPixels(next * width, (rows - 1 - r) * height, width, height, read.GetPixels());
                }
                if (++next < times.Length) return;
                EditorApplication.update -= tick;
                Time.captureDeltaTime = 0f;
                if (walks && ctrl) { ctrl.driven = false; ctrl.runHeld = false; }
                foreach (var g in loadout.guns) if (g) { g.forceTrigger = false; g.forceAim = false; }
                if (loadout.pickaxe) loadout.pickaxe.swingHeld = false;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(read);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);
                Debug.Log("DinoToolStrip " + path + ": " + log);
            };
            EditorApplication.update += tick;
            return "started";
        }
    }
}
