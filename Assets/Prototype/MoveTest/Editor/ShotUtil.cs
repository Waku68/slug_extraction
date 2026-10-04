using System.IO;
using UnityEngine;

namespace SlugMove
{
    /// <summary>Debug: renders the scene from a given point into a PNG (edit or play mode), for checking looks from tools.</summary>
    public static class ShotUtil
    {
        public static string Capture(string path, Vector3 position, Vector3 lookAt, float fov = 40f, int width = 1200, int height = 600)
        {
            var go = new GameObject("ShotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.02f;
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
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            return path;
        }
    }
}
