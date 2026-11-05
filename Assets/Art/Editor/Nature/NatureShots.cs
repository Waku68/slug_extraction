using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 植物の案を、決まった 3 枚（docs/modeling/README.md 3章）で撮って 1 枚に並べる（Oct 27）。
    /// 行が案（上から A・B・C…）、列が A ナメクジの目の高さ 0.47 m から ／ B 横からナメクジと並べて ／ C 斜め上から。
    /// 開いているシーンには触らない（プレビュー用の別の場面で撮る。ほかのセッションと Unity を分け合っても壊さない）。
    /// gray = true で灰色ラフ（全部の部品を灰色にする）。
    /// </summary>
    public static class NatureShots
    {
        const int W = 512, H = 384;

        public static string Grid(string[] prefabPaths, string outPng, bool gray)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var tex = new Texture2D(W * 3, H * prefabPaths.Length, TextureFormat.RGB24, false);
            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            Material grayMat = null, groundMat = null, slugMat = null;
            try
            {
                var lightGo = new GameObject("Sun");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.2f; light.shadows = LightShadows.Soft;
                lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
                SceneManager_Move(lightGo, scene);

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.transform.localScale = Vector3.one * 3f;
                groundMat = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Nature/Tsuyuri/Tsuyuri_Stem.mat") ?? new Material(Shader.Find("Slug/Toon")));
                groundMat.SetColor("_BaseColor", new Color(0.62f, 0.58f, 0.5f));
                groundMat.SetColor("_OutlineColor", new Color(0.62f, 0.58f, 0.5f));
                groundMat.SetFloat("_OutlineWidth", 0f);
                groundMat.SetFloat("_WindAmount", 0f);
                ground.GetComponent<Renderer>().sharedMaterial = groundMat;
                SceneManager_Move(ground, scene);

                var camGo = new GameObject("Cam");
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.targetTexture = rt; cam.fieldOfView = 35f; cam.nearClipPlane = 0.01f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.72f, 0.82f, 0.9f);
                SceneManager_Move(camGo, scene);

                // the comparison slug: a capsule 1.1 m long and 0.5 m high (the README's stand-in)
                slugMat = new Material(groundMat);
                slugMat.SetColor("_BaseColor", new Color(0.55f, 0.6f, 0.45f));

                for (int row = 0; row < prefabPaths.Length; row++)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[row]);
                    var plant = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                    var rend = plant.GetComponent<Renderer>();
                    if (gray)
                    {
                        if (grayMat == null)
                        {
                            grayMat = new Material(rend.sharedMaterials[0]);
                            grayMat.SetColor("_BaseColor", new Color(0.78f, 0.78f, 0.78f));
                            grayMat.SetColor("_OutlineColor", new Color(0.3f, 0.3f, 0.3f));
                        }
                        var ms = new Material[rend.sharedMaterials.Length];
                        for (int i = 0; i < ms.Length; i++) ms[i] = grayMat;
                        rend.sharedMaterials = ms;
                    }
                    var b = rend.bounds;
                    float s = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    Vector3 c = b.center;

                    // A: slug eye height, a little way off
                    float dA = Mathf.Max(0.55f, s * 2.2f);
                    Shoot(cam, new Vector3(c.x, 0.47f, c.z - dA), c, tex, 0, row, prefabPaths.Length);

                    // B: from the side, the slug beside it
                    var slug = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    slug.transform.localScale = new Vector3(0.5f, 0.55f, 0.5f);
                    slug.transform.SetPositionAndRotation(new Vector3(b.max.x + 0.75f, 0.25f, c.z + 0.2f), Quaternion.Euler(0f, 0f, 90f));
                    slug.GetComponent<Renderer>().sharedMaterial = slugMat;
                    SceneManager_Move(slug, scene);
                    // (back far enough to frame tall things like trees too; small plants keep the old 2.6 m)
                    var mid = new Vector3(c.x + (b.extents.x + 0.75f) * 0.5f, Mathf.Max(0.28f, b.size.y * 0.42f), c.z);
                    float dB = Mathf.Max(2.6f, Mathf.Max(b.size.x + 1.5f, b.size.y) * 1.7f);
                    Shoot(cam, mid + new Vector3(0f, 0.15f, -dB), mid, tex, 1, row, prefabPaths.Length);
                    Object.DestroyImmediate(slug);

                    // C: from above at a slant
                    Shoot(cam, c + new Vector3(0f, s * 2.4f + 0.3f, -s * 1.6f - 0.25f), c, tex, 2, row, prefabPaths.Length);
                    Object.DestroyImmediate(plant);
                }
                tex.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(outPng));
                File.WriteAllBytes(outPng, tex.EncodeToPNG());
                return outPng;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                if (grayMat != null) Object.DestroyImmediate(grayMat);
                if (groundMat != null) Object.DestroyImmediate(groundMat);
                if (slugMat != null) Object.DestroyImmediate(slugMat);
            }
        }

        /// <summary>
        /// 1 つの物を、決まった 3 枚のほかの向きから撮って横に並べる（Nov 3。木の下の「部屋」から見上げる、など）。
        /// from[i] から at[i] を見る（物の原点からの m）。比べるナメクジを slugAt に置く（null なら置かない）。
        /// </summary>
        public static string Views(string prefabPath, Vector3[] from, Vector3[] at, string outPng, Vector3? slugAt = null, float fov = 50f, bool wall = false)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var tex = new Texture2D(W * from.Length, H, TextureFormat.RGB24, false);
            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            Material groundMat = null, slugMat = null, wallMat = null;
            try
            {
                var lightGo = new GameObject("Sun");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.2f; light.shadows = LightShadows.Soft;
                lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
                SceneManager_Move(lightGo, scene);
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.transform.localScale = Vector3.one * 5f;
                groundMat = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Nature/Tsuyuri/Tsuyuri_Stem.mat") ?? new Material(Shader.Find("Slug/Toon")));
                groundMat.SetColor("_BaseColor", new Color(0.62f, 0.58f, 0.5f));
                groundMat.SetColor("_OutlineColor", new Color(0.62f, 0.58f, 0.5f));
                groundMat.SetFloat("_OutlineWidth", 0f);
                groundMat.SetFloat("_WindAmount", 0f);
                ground.GetComponent<Renderer>().sharedMaterial = groundMat;
                SceneManager_Move(ground, scene);
                var camGo = new GameObject("Cam");
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.targetTexture = rt; cam.fieldOfView = fov; cam.nearClipPlane = 0.01f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.72f, 0.82f, 0.9f);
                SceneManager_Move(camGo, scene);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), scene);
                if (wall)
                {
                    // a dark wet rock wall through the origin (for things that grow on walls; they bulge toward +Z)
                    var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    slab.transform.SetPositionAndRotation(new Vector3(0f, 1f, -0.05f), Quaternion.identity);
                    slab.transform.localScale = new Vector3(4f, 2.5f, 0.1f);
                    wallMat = new Material(groundMat);
                    wallMat.SetColor("_BaseColor", new Color(0.36f, 0.35f, 0.33f));
                    slab.GetComponent<Renderer>().sharedMaterial = wallMat;
                    SceneManager_Move(slab, scene);
                }
                if (slugAt.HasValue)
                {
                    slugMat = new Material(groundMat);
                    slugMat.SetColor("_BaseColor", new Color(0.55f, 0.6f, 0.45f));
                    var slug = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    slug.transform.localScale = new Vector3(0.5f, 0.55f, 0.5f);
                    slug.transform.SetPositionAndRotation(slugAt.Value + Vector3.up * 0.25f, Quaternion.Euler(0f, 0f, 90f));
                    slug.GetComponent<Renderer>().sharedMaterial = slugMat;
                    SceneManager_Move(slug, scene);
                }
                for (int i = 0; i < from.Length; i++) Shoot(cam, from[i], at[i], tex, i, 0, 1);
                tex.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(outPng));
                File.WriteAllBytes(outPng, tex.EncodeToPNG());
                return outPng;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                if (groundMat != null) Object.DestroyImmediate(groundMat);
                if (slugMat != null) Object.DestroyImmediate(slugMat);
                if (wallMat != null) Object.DestroyImmediate(wallMat);
            }
        }

        static void SceneManager_Move(GameObject go, UnityEngine.SceneManagement.Scene scene) =>
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

        static void Shoot(Camera cam, Vector3 pos, Vector3 look, Texture2D into, int col, int row, int rows)
        {
            cam.transform.position = pos;
            cam.transform.LookAt(look);
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = cam.targetTexture;
            into.ReadPixels(new Rect(0, 0, W, H), col * W, (rows - 1 - row) * H);
            RenderTexture.active = prev;
        }
    }
}
