using System.IO;
using SlugDesign;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Debug (play mode, DinoMoveTest): walks the two-legged slug by hand with a fixed input for each stretch of time and
    /// tiles a camera beside it (at the slug's eye height, following it) at the given times into one image. Frames do not
    /// depend on the editor's frame rate. Also logs where each foot is against the hip (for sliding feet).
    /// </summary>
    public static class DinoFilmStrip
    {
        public struct Leg { public float until; public Vector2 input; public float turn; public bool jump; }

        /// <param name="view">camera offset from the slug in its own frame at the start (right, up, forward)</param>
        public static string Capture(string path, Leg[] legs, float[] times, Vector3 view, int width = 400, int height = 300, int cols = 4)
        {
            var ctrl = Object.FindAnyObjectByType<SlugMoveController>();
            if (!ctrl) return "no SlugMoveController (play mode?)";
            var body = ctrl.body.GetComponent<DinoSlugBody>();
            var rig = ctrl.cameraRig;
            var camGo = new GameObject("FilmCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.02f;
            cam.clearFlags = CameraClearFlags.Skybox;
            int rows = Mathf.CeilToInt(times.Length / (float)cols);
            var sheet = new Texture2D(width * cols, height * rows, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var read = new Texture2D(width, height, TextureFormat.RGB24, false);

            const float step = 1f / 240f;
            for (int i = 0; i < 240; i++) { ctrl.Simulate(step, Vector2.zero, false); body.Step(step); }
            Quaternion frame = Quaternion.Euler(0f, ctrl.transform.eulerAngles.y, 0f);
            float t = 0f;
            int k = 0, leg = 0;
            var log = new System.Text.StringBuilder();
            foreach (float target in times)
            {
                while (t < target - 1e-5f)
                {
                    while (leg < legs.Length - 1 && t >= legs[leg].until) leg++;
                    if (rig) rig.AddLook(legs[leg].turn * step, 0f);
                    ctrl.Simulate(step, legs[leg].input, legs[leg].jump);
                    body.Step(step);
                    t += step;
                }
                Vector3 at = ctrl.transform.position;
                cam.transform.position = at + frame * view;
                cam.transform.rotation = Quaternion.LookRotation(at + Vector3.up * 0.25f - cam.transform.position);
                // the skin is only deformed once a frame: bake this pose into a stand-in for the shot
                var smr = body.GetComponentInChildren<SkinnedMeshRenderer>();
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                var stand = new GameObject("Baked");
                stand.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                stand.AddComponent<MeshFilter>().sharedMesh = baked;
                stand.AddComponent<MeshRenderer>().sharedMaterial = smr.sharedMaterial;
                smr.enabled = false;
                cam.targetTexture = rt;
                cam.aspect = width / (float)height;
                cam.Render();
                cam.targetTexture = null;
                smr.enabled = true;
                Object.DestroyImmediate(stand);
                Object.DestroyImmediate(baked);
                RenderTexture.active = rt;
                read.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                read.Apply();
                RenderTexture.active = null;
                sheet.SetPixels((k % cols) * width, (rows - 1 - k / cols) * height, width, height, read.GetPixels());
                k++;
                var fl = body.feet[0].position; var fr = body.feet[1].position;
                log.Append($"{target:F2}s v {ctrl.Speed:F2} L {fl.x:F3},{fl.y:F3},{fl.z:F3} R {fr.x:F3},{fr.y:F3},{fr.z:F3} | ");
            }
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(read);
            Object.DestroyImmediate(camGo);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            return log.ToString();
        }
    }
}
