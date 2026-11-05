using System.Collections.Generic;
using System.IO;
using slug = SlugBase;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugDesign
{
    public static partial class CarrierBirdBuilder
    {
        // サブメッシュ番号（翼の3色マテリアル用）
        const int W_CREAM = 0, W_MID = 1, W_TIP = 2;

        static Mesh MakeFoldedWing(BirdSpec s, float side, out int tris)
        {
            var kit = new NatureMeshKit(3);
            Vector3 bodyExt = s.bodySize * 0.5f;

            // 1つの連続したしずく形の翼パッドを胴のわきに添わせ、前後方向に3色（クリーム→黄土→炭色）と小さな段差をつける
            int lenSegs = 28;
            int ringSegs = 24;
            int start = kit.VertexCount;

            for (int i = 0; i <= lenSegs; i++)
            {
                float t = i / (float)lenSegs; // 0: 肩（前） → 1: 翼先（うしろ）
                float z = Mathf.Lerp(bodyExt.z * 0.34f, -bodyExt.z * 0.72f, Mathf.Sin(t * Mathf.PI * 0.5f));
                float env = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI)); // 0..1..0
                float halfH = bodyExt.y * Mathf.Lerp(0.44f, 0.13f, t * t) * Mathf.Max(0.05f, Mathf.Sin(Mathf.Clamp01(t * 0.96f) * Mathf.PI + 0.06f));
                float centerY = Mathf.Lerp(0.04f, -0.02f, t) * bodyExt.y;

                // 色の段ごとの小さな段差（羽の重なり感）
                float stepOut = t < 0.46f ? 0.11f : (t < 0.75f ? 0.09f : 0.07f);

                for (int j = 0; j <= ringSegs; j++)
                {
                    float u = j / (float)ringSegs, th = Mathf.PI * 2f * u;
                    float cy = Mathf.Cos(th); // -1(下縁) .. +1(上縁)
                    float sx = Mathf.Sin(th); // +1(外側) .. -1(胴側)

                    float y = centerY + cy * halfH;
                    float ny = y / bodyExt.y;
                    float nz = z / bodyExt.z;
                    float r2 = Mathf.Clamp01(1f - nz * nz * 0.85f - ny * ny * 0.85f);
                    float backTaper = nz < 0f ? 1f + 0.34f * nz : 1f - 0.04f * nz;
                    float bodySurfX = Mathf.Sqrt(r2) * bodyExt.x * backTaper * 0.95f;

                    float outThickness = sx >= 0f ? sx * stepOut * Mathf.Pow(env, 0.35f) : sx * 0.08f;
                    float x = side * (bodySurfX + outThickness);
                    kit.Vert(new Vector3(x, y, z), new Vector2(u, t));
                }
            }

            for (int i = 0; i < lenSegs; i++)
            {
                float tMid = (i + 0.5f) / lenSegs;
                int sub = tMid < 0.48f ? W_CREAM : (tMid < 0.76f ? W_MID : W_TIP);
                for (int j = 0; j < ringSegs; j++)
                {
                    int a = start + i * (ringSegs + 1) + j, b = a + ringSegs + 1;
                    if (side > 0f) kit.Quad(sub, a, a + 1, b + 1, b);
                    else kit.Quad(sub, a, b, b + 1, a + 1);
                }
            }

            tris = kit.TriangleCount;
            return kit.ToMesh("WingFolded");
        }

        static Mesh MakeOpenWing(BirdSpec s, float side, out int tris)
        {
            var kit = new NatureMeshKit(3);
            int spanSegs = 32;
            int ringSegs = 28;
            int start = kit.VertexCount;

            for (int i = 0; i <= spanSegs; i++)
            {
                float ts = i / (float)spanSegs;
                float xDist = Mathf.Lerp(-0.22f, s.wingSpanHalf, Mathf.Sin(ts * Mathf.PI * 0.5f));
                float tipCap = Mathf.Cos(ts * Mathf.PI * 0.485f);
                float chord = Mathf.Lerp(s.wingChordRoot, s.wingChordTip, ts) * tipCap;
                float archY = Mathf.Sin(ts * Mathf.PI) * 0.09f - ts * ts * 0.05f;
                float scallop = (ts > 0.08f && ts < 0.92f) ? 0.050f * Mathf.Sin(ts * Mathf.PI * 5f) : 0f;

                float zFront = chord * 0.42f;
                float zBack = -chord * 0.58f - scallop;
                float zMid = (zFront + zBack) * 0.5f;
                float zHalf = Mathf.Max(0.015f, (zFront - zBack) * 0.5f);
                float hHalf = s.wingThick * 0.5f * Mathf.Lerp(1.15f, 0.45f, ts) * tipCap;

                for (int j = 0; j <= ringSegs; j++)
                {
                    float u = j / (float)ringSegs, th = Mathf.PI * 2f * u;
                    float cz = Mathf.Cos(th), sy = Mathf.Sin(th);
                    float stepH = cz > 0.15f ? 1.06f : (cz > -0.45f ? 0.96f : 0.84f);
                    float z = zMid + cz * zHalf;
                    float y = archY + sy * hHalf * stepH;
                    kit.Vert(new Vector3(side * xDist, y, z), new Vector2(u, ts));
                }
            }

            for (int i = 0; i < spanSegs; i++)
            {
                float ts = (i + 0.5f) / spanSegs;
                for (int j = 0; j < ringSegs; j++)
                {
                    float u = (j + 0.5f) / ringSegs;
                    float cz = Mathf.Cos(Mathf.PI * 2f * u);
                    int sub;
                    if (ts > 0.78f || cz < -0.42f) sub = W_TIP;
                    else if (ts > 0.52f || cz < 0.12f) sub = W_MID;
                    else sub = W_CREAM;

                    int a = start + i * (ringSegs + 1) + j, b = a + ringSegs + 1;
                    if (side < 0f) kit.Quad(sub, a, a + 1, b + 1, b);
                    else kit.Quad(sub, a, b, b + 1, a + 1);
                }
            }

            tris = kit.TriangleCount;
            return kit.ToMesh("WingOpen");
        }

        static Mesh MakeTail(BirdSpec s, out int tris)
        {
            var kit = new NatureMeshKit(1);
            int lenSegs = 16, ringSegs = 22;
            int start = kit.VertexCount;
            float pitchRad = 25f * Mathf.Deg2Rad;
            for (int i = 0; i <= lenSegs; i++)
            {
                float t = i / (float)lenSegs;
                float dist = s.tailLen * Mathf.Sin(t * Mathf.PI * 0.5f);
                float tipCap = Mathf.Cos(t * Mathf.PI * 0.48f);
                float halfW = s.tailW * 0.5f * Mathf.Lerp(0.75f, 1.15f, Mathf.Sin(t * Mathf.PI * 0.7f)) * tipCap;
                float halfH = 0.060f * (1f - 0.5f * t) * tipCap;

                for (int j = 0; j <= ringSegs; j++)
                {
                    float u = j / (float)ringSegs, th = Mathf.PI * 2f * u;
                    float x = Mathf.Cos(th) * halfW;
                    float localY = Mathf.Sin(th) * halfH;
                    float wave = (t > 0.35f) ? 0.032f * Mathf.Cos(Mathf.Cos(th) * Mathf.PI * 2.5f) * t : 0f;
                    float d = dist + wave;
                    Vector3 p = new Vector3(
                        x,
                        d * Mathf.Sin(pitchRad) + localY * Mathf.Cos(pitchRad),
                        -d * Mathf.Cos(pitchRad) + localY * Mathf.Sin(pitchRad)
                    );
                    kit.Vert(p, new Vector2(u, t));
                }
            }
            GridQuads(kit, 0, start, lenSegs, ringSegs);
            tris = kit.TriangleCount;
            return kit.ToMesh("Tail");
        }

        static Mesh MakeLeg(BirdSpec s, float side, out int tris)
        {
            var kit = new NatureMeshKit(1);
            float x = side * s.legX;
            var rows = new List<Vector3>
            {
                new Vector3(0.00f, 1.12f, 0),
                new Vector3(0.20f, 1.00f, 0),
                new Vector3(0.45f, 0.96f, 0),
                new Vector3(0.70f, 1.02f, 0),
                new Vector3(0.88f, 1.12f, 0),
                new Vector3(1.00f, 1.25f, 0),
            };
            kit.BeginTube();
            kit.Tube(
                t => new Vector3(x, Mathf.Lerp(0.05f, s.legH + 0.14f, t), 0.01f * Mathf.Sin(t * Mathf.PI)),
                _ => s.legR,
                rows, 16, 0, 0
            );
            tris = kit.TriangleCount;
            return kit.ToMesh("Leg");
        }

        static Mesh MakeFoot(BirdSpec s, float side, out int tris)
        {
            var kit = new NatureMeshKit(1);
            float x0 = side * s.legX;
            int lenSegs = 16, ringSegs = 22;
            int start = kit.VertexCount;
            for (int i = 0; i <= lenSegs; i++)
            {
                float t = i / (float)lenSegs;
                float z = Mathf.Lerp(-0.08f, s.footLen - 0.08f, Mathf.Sin(t * Mathf.PI * 0.5f));
                float tipCap = Mathf.Cos(t * Mathf.PI * 0.48f);
                float halfW = Mathf.Lerp(s.legR * 1.3f, s.footW * 0.5f, Mathf.Pow(Mathf.Max(0f, t), 0.65f)) * tipCap;
                float h = Mathf.Lerp(0.070f, 0.038f, t) * tipCap;

                for (int j = 0; j <= ringSegs; j++)
                {
                    float u = j / (float)ringSegs, th = Mathf.PI * 2f * u;
                    float cx = Mathf.Cos(th), sy = Mathf.Sin(th);
                    float toeWave = (t > 0.45f) ? 0.028f * Mathf.Cos(cx * Mathf.PI * 2.2f) * (t - 0.45f) : 0f;
                    float y = sy >= 0f ? 0.026f + sy * h : Mathf.Max(0f, 0.026f + sy * 0.026f);
                    kit.Vert(new Vector3(x0 + cx * halfW, y, z + toeWave), new Vector2(u, t));
                }
            }
            GridQuadsFlip(kit, 0, start, lenSegs, ringSegs);
            tris = kit.TriangleCount;
            return kit.ToMesh("Foot");
        }

        static void AddEllipsoid(NatureMeshKit kit, int sub, Vector3 center, Vector3 radii, Quaternion rot, int rings, int segs)
        {
            int start = kit.VertexCount;
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings, phi = Mathf.PI * v;
                for (int x = 0; x <= segs; x++)
                {
                    float u = x / (float)segs, th = Mathf.PI * 2f * u;
                    Vector3 n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), -Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    kit.Vert(center + rot * Vector3.Scale(n, radii), new Vector2(u, v));
                }
            }
            GridQuads(kit, sub, start, rings, segs);
        }

        static void GridQuads(NatureMeshKit kit, int sub, int start, int rings, int segs)
        {
            for (int y = 0; y < rings; y++)
                for (int x = 0; x < segs; x++)
                {
                    int a = start + y * (segs + 1) + x, b = a + segs + 1;
                    kit.Quad(sub, a, b, b + 1, a + 1);
                }
        }

        static void GridQuadsFlip(NatureMeshKit kit, int sub, int start, int rings, int segs)
        {
            for (int y = 0; y < rings; y++)
                for (int x = 0; x < segs; x++)
                {
                    int a = start + y * (segs + 1) + x, b = a + segs + 1;
                    kit.Quad(sub, a, a + 1, b + 1, b);
                }
        }

        // ---- 見るシーン CarrierBirdLook.unity と撮影 ----

        public static void BuildLookScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreviewScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sunGo = new GameObject("Directional Light");
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, 150f, 0f); // 前（+Z）から照らす

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.72f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.36f, 0.34f);

            var groundMat = WorldLook.GetOrCreate($"{Dir}/PreviewGround.mat");
            ColorUtility.TryParseHtmlString("#9E9585", out var gCol);
            WorldLook.Apply(groundMat, gCol, 0.05f, 0f);
            groundMat.SetFloat("_OutlineWidth", 0f);

            var slugMat = WorldLook.GetOrCreate($"{Dir}/PreviewSlug.mat");
            ColorUtility.TryParseHtmlString("#7BA368", out var sCol);
            WorldLook.Apply(slugMat, sCol, 0.2f, 0f);
            slugMat.SetFloat("_Facet", 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // 本番 CarrierBird（立つ姿＋飛ぶ姿）と 3案（A・B・C）を並べる
            PlaceBirdInstance("CarrierBird", new Vector3(0f, 0f, 0f), false);
            PlaceBirdInstance("CarrierBird", new Vector3(4.8f, 1.5f, 0f), true);
            MakeSlugProxy("SlugProxy", new Vector3(1.45f, 0f, 0.2f), slugMat);

            // 後ろの列に A・B・C の比較用
            PlaceBirdInstance("CarrierBird_A", new Vector3(-4.5f, 0f, -5.5f), false);
            PlaceBirdInstance("CarrierBird_B", new Vector3(0f, 0f, -5.5f), false);
            PlaceBirdInstance("CarrierBird_C", new Vector3(4.5f, 0f, -5.5f), false);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 42f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.68f, 0.77f, 0.84f);
            camGo.transform.position = new Vector3(-0.3f, 0.47f, 3.8f);
            camGo.transform.LookAt(new Vector3(0.2f, 0.95f, 0f));

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
        }

        static GameObject PlaceBirdInstance(string prefabName, Vector3 pos, bool flying)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{prefabName}.prefab");
            if (!asset) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = flying ? $"{prefabName}_Flying" : prefabName;
            go.transform.position = pos;
            if (flying)
            {
                var foldL = FindChild(go.transform, "CarrierBird_WingFolded_L");
                var foldR = FindChild(go.transform, "CarrierBird_WingFolded_R");
                var openL = FindChild(go.transform, "CarrierBird_WingOpen_L");
                var openR = FindChild(go.transform, "CarrierBird_WingOpen_R");
                if (foldL) foldL.gameObject.SetActive(false);
                if (foldR) foldR.gameObject.SetActive(false);
                if (openL) openL.gameObject.SetActive(true);
                if (openR) openR.gameObject.SetActive(true);
            }
            return go;
        }

        static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        public static GameObject MakeSlugProxy(string name, Vector3 groundPos, Material mat)
        {
            var root = new GameObject(name);
            root.transform.position = groundPos;
            var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cap.name = "SlugBody_1.1x0.5m";
            cap.transform.SetParent(root.transform, false);
            // 体長 1.1 m (Z)、高さ 0.5 m (Y)
            cap.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            cap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cap.transform.localScale = new Vector3(0.50f, 0.55f, 0.50f);
            if (mat) cap.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            return root;
        }
    }
}
