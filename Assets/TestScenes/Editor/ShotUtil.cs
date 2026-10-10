using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Debug: renders the scene from a given point into a PNG (edit or play mode), for checking looks from tools.
    /// Between BeginSheet and EndSheet, every Capture is drawn small into one grid image instead of its own file
    /// (one image to look at instead of many: any Shoot menu can be run this way unchanged).
    /// </summary>
    public static class ShotUtil
    {
        static Texture2D _sheet;
        static string _sheetPath;
        static int _tileW, _tileH, _cols, _rows, _count;
        static readonly List<string> _names = new List<string>();

        public static string Capture(string path, Vector3 position, Vector3 lookAt, float fov = 40f, int width = 1200, int height = 600, float far = 1000f)
        {
            if (_sheet != null)
            {
                if (_count >= _cols * _rows) return path + " (left out: the sheet is full)";
                var small = Render(position, lookAt, fov, _tileW, _tileH, far);
                int c = _count % _cols, r = _count / _cols;
                _sheet.SetPixels(c * _tileW, (_rows - 1 - r) * _tileH, _tileW, _tileH, small.GetPixels());   // (rows from the top)
                Object.DestroyImmediate(small);
                _count++;
                _names.Add(Path.GetFileNameWithoutExtension(path));
                return $"{_count}: {Path.GetFileNameWithoutExtension(path)}";
            }
            var tex = Render(position, lookAt, fov, width, height, far);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return path;
        }

        /// <summary>Starts collecting captures into one image: tiles of tileW × tileH, numbered left to right, top to bottom.</summary>
        public static void BeginSheet(string path, int columns = 3, int maxShots = 12, int tileW = 400, int tileH = 200)
        {
            _sheetPath = path; _tileW = tileW; _tileH = tileH; _cols = Mathf.Max(1, columns);
            _rows = Mathf.Max(1, Mathf.CeilToInt(maxShots / (float)_cols)); _count = 0; _names.Clear();
            _sheet = new Texture2D(_cols * tileW, _rows * tileH, TextureFormat.RGB24, false);
            var fill = new Color[_sheet.width * _sheet.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color(0.12f, 0.12f, 0.12f);
            _sheet.SetPixels(fill);
        }

        /// <summary>Writes the sheet (cropped to the rows used) and returns "path: 1 name, 2 name, ...".</summary>
        public static string EndSheet()
        {
            if (_sheet == null) return "no sheet";
            int used = Mathf.Max(1, Mathf.CeilToInt(_count / (float)_cols));
            var outTex = new Texture2D(_sheet.width, used * _tileH, TextureFormat.RGB24, false);
            outTex.SetPixels(_sheet.GetPixels(0, (_rows - used) * _tileH, _sheet.width, used * _tileH));
            outTex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(_sheetPath));
            File.WriteAllBytes(_sheetPath, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);
            Object.DestroyImmediate(_sheet);
            _sheet = null;
            var sb = new System.Text.StringBuilder(_sheetPath + ":");
            for (int i = 0; i < _names.Count; i++) sb.Append($" {i + 1} {_names[i]}{(i + 1 < _names.Count ? "," : "")}");
            return sb.ToString();
        }

        static Texture2D Render(Vector3 position, Vector3 lookAt, float fov, int width, int height, float far)
        {
            var go = new GameObject("ShotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = fov;
            cam.nearClipPlane = far > 1000f ? 0.3f : 0.02f;
            cam.farClipPlane = far;
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
    }
}
