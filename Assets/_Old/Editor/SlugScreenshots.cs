using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SlugDesign
{
    /// <summary>Front / side / 3-4 screenshots of every candidate, rendered from the gallery scene.</summary>
    public static class SlugScreenshots
    {
        const int W = 1280, H = 960;
        const float FrozenTime = 1.3f;   // freeze the crawl wave so the stills are reproducible

        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));

        struct View { public string name; public float yaw, elev; }
        static readonly View[] Views =
        {
            new View { name = "front", yaw = 180f, elev = 6f },    // face toward the camera
            new View { name = "side",  yaw = 90f,  elev = 3f },    // right side, head to the right of the image
            new View { name = "angle", yaw = 145f, elev = 20f },   // 3/4 view from above
        };

        internal static void Save(Camera cam, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            cam.ResetAspect();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        internal static Bounds BoundsOf(GameObject go)
        {
            // Renderer.bounds can be stale right after activating / rotating, so rebuild from mesh corners.
            bool first = true;
            Bounds b = default;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.gameObject.activeInHierarchy || mf.sharedMesh == null) continue;
                var lb = mf.sharedMesh.bounds;
                var m = mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var w = m.MultiplyPoint3x4(c);
                    if (first) { b = new Bounds(w, Vector3.zero); first = false; } else b.Encapsulate(w);
                }
            }
            return b;
        }

        internal static void Frame(Camera cam, Bounds b, float elevDeg, float margin = 1.25f)
        {
            float vfov = cam.fieldOfView * Mathf.Deg2Rad;
            float aspect = cam.aspect;
            float half = Mathf.Tan(vfov * 0.5f);
            float dist = Mathf.Max(b.extents.y / half, b.extents.x / (half * aspect)) * margin + b.extents.z;
            float e = elevDeg * Mathf.Deg2Rad;
            Vector3 target = b.center;
            cam.transform.position = target + new Vector3(0f, Mathf.Sin(e), -Mathf.Cos(e)) * dist;
            cam.transform.LookAt(target);
        }

        internal static Camera MakeCamera(Camera like)
        {
            var go = new GameObject("Capture Camera");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(like);
            cam.fieldOfView = 24f;
            cam.nearClipPlane = 0.05f;
            go.AddComponent<UniversalAdditionalCameraData>();
            return cam;
        }

        [MenuItem("Slug/Capture Screenshots")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutDir);
            var scene = EditorSceneManager.OpenScene(SlugGallery.ScenePath, OpenSceneMode.Single);
            var gallery = GameObject.Find("Gallery").transform;
            var overview = GameObject.Find("Overview Camera").GetComponent<Camera>();

            var slugs = new List<GameObject>();
            var labels = new List<GameObject>();
            foreach (Transform c in gallery)
            {
                if (c.name.StartsWith("Slug_")) slugs.Add(c.gameObject);
                else if (c.name.StartsWith("Label_")) labels.Add(c.gameObject);
            }

            Shader.SetGlobalFloat("_SlugFreeze", 1f);
            Shader.SetGlobalFloat("_SlugTimeValue", FrozenTime);
            try
            {
                // ---- overview
                int ow = 4096, oh = Mathf.RoundToInt(4096 / SlugGallery.OverviewAspect);
                Save(overview, Path.Combine(OutDir, "00_overview.png"), ow, oh);

                // ---- per-design views
                var cam = MakeCamera(overview);
                var saved = new Dictionary<GameObject, Quaternion>();
                foreach (var s in slugs) saved[s] = s.transform.rotation;
                foreach (var l in labels) l.SetActive(false);

                foreach (var s in slugs)
                {
                    foreach (var other in slugs) other.SetActive(other == s);
                    string id = s.name.Substring("Slug_".Length);
                    foreach (var v in Views)
                    {
                        s.transform.rotation = Quaternion.Euler(0f, v.yaw, 0f);
                        Frame(cam, BoundsOf(s), v.elev);
                        Save(cam, Path.Combine(OutDir, $"{id}_{v.name}.png"), W, H);
                    }
                }

                // ---- extras: crawl wave at two moments, and hands put away / out
                string extra = Path.Combine(OutDir, "extra");
                Directory.CreateDirectory(extra);
                foreach (var s in slugs) s.SetActive(false);
                var wave = slugs.Find(g => g.name.Contains("02_ryusen"));
                wave.SetActive(true);
                wave.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                foreach (float t in new[] { 0f, 0.3f })
                {
                    Shader.SetGlobalFloat("_SlugTimeValue", t);
                    Frame(cam, BoundsOf(wave), 3f, 1.1f);
                    Save(cam, Path.Combine(extra, $"wave_t{t:0.0}.png"), W, H);
                }
                wave.SetActive(false);
                Shader.SetGlobalFloat("_SlugTimeValue", FrozenTime);

                foreach (var s in slugs.FindAll(g => g.name.Contains("01_") || g.name.Contains("06_")))
                {
                    s.SetActive(true);
                    s.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
                    var rig = s.GetComponent<SlugRig>();
                    foreach (float e in new[] { 0f, 0.5f, 1f })
                    {
                        rig.handsExtend = e; rig.Apply();
                        Frame(cam, BoundsOf(s), 12f);
                        Save(cam, Path.Combine(extra, $"{s.name.Substring(5)}_hands_{e:0.0}.png"), W, H);
                    }
                    rig.handsExtend = 1f; rig.Apply();
                    s.SetActive(false);
                }

                // ---- restore
                foreach (var s in slugs) { s.SetActive(true); s.transform.rotation = saved[s]; }
                foreach (var l in labels) l.SetActive(true);
                Object.DestroyImmediate(cam.gameObject);
            }
            finally
            {
                Shader.SetGlobalFloat("_SlugFreeze", 0f);
            }
            Debug.Log("[Slug] screenshots written to " + OutDir);
        }
    }
}
