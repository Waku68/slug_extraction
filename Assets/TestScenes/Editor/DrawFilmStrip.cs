using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Debug (play mode): switches the player's tools and tiles what some cameras see at the given times after the switch
    /// (one row per camera). The game runs at a fixed step meanwhile (Time.captureDeltaTime), so the frames do not depend
    /// on the editor's frame rate. Runs over the next frames; the image is written when the last time is passed.
    /// </summary>
    public static class DrawFilmStrip
    {
        public static string Start(string path, int slot, float[] times, Camera[] cams, int width = 360, int height = 300)
        {
            var loadout = Object.FindAnyObjectByType<SlugLoadout>();
            if (!loadout) return "no SlugLoadout (play mode?)";
            int rows = cams.Length, cols = times.Length;
            var sheet = new Texture2D(width * cols, height * rows, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var read = new Texture2D(width, height, TextureFormat.RGB24, false);
            Time.captureDeltaTime = 1f / 120f;
            loadout.Select(slot);
            float start = Time.time;
            int next = 0;
            var log = new System.Text.StringBuilder();
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; Time.captureDeltaTime = 0f; return; }
                if (Time.time - start < times[next] - 1e-4f) return;
                log.Append($"{times[next]:F2}s in hand {loadout.InHand} draw {loadout.DrawProgress:F2} | ");
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
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(read);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);
                Debug.Log("DrawFilmStrip " + path + ": " + log);
            };
            EditorApplication.update += tick;
            return "started";
        }

        /// <summary>A camera looking at the player from <paramref name="offset"/> (the player's own turn: x right, y up, z ahead).</summary>
        public static Camera Look(string name, Vector3 offset, Vector3 lookAt, float fov = 40f)
        {
            var loadout = Object.FindAnyObjectByType<SlugLoadout>();
            var go = GameObject.Find(name);
            if (!go) go = new GameObject(name);
            var cam = go.GetComponent<Camera>();
            if (!cam) cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.02f;
            var t = loadout.transform;
            go.transform.position = t.TransformPoint(offset);
            go.transform.LookAt(t.TransformPoint(lookAt));
            return cam;
        }
    }
}
