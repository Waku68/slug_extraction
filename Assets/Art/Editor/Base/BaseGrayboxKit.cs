using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の灰色の箱（BaseGrayboxBuilder）の部品：格子の面（洞窟の殻）、扇形の床（岩棚・坂）、水の帯、箱と筒、灯、札、撮影。
    /// 灰色の箱の間だけ URP/Lit（灯の点光源が効くように）。色の段階で Slug/Toon に移す。
    /// </summary>
    public static partial class BaseGrayboxBuilder
    {
        static Material _rock, _floor, _wood, _water, _mark, _lamp, _slugMat, _moss, _life;
        static Transform _root;

        static void Materials()
        {
            _rock = Mat("Rock", "#B9B6AE", 0.05f);
            _floor = Mat("Floor", "#9E9B93", 0.25f);
            _wood = Mat("Wood", "#8C8478", 0.1f);
            _water = Mat("Water", "#6C8C99", 0.85f);
            _mark = Mat("Mark", "#C9B892", 0.1f);       // 布・地図・札など「中身」の印（灰色の中で見分ける）
            _lamp = Mat("Lamp", "#FFB860", 0.1f, "#FFA040");
            _slugMat = Mat("Stand", "#7F8A70", 0.3f);   // 行商人・鳥の立ち姿の仮
            _moss = Mat("Moss", "#7D8C5E", 0.15f);      // 苔・シダ・つる（生きている感じだけ少し色を付ける）
            _life = Mat("Life", "#C46A55", 0.3f);       // 池の小エビ
        }

        static Material Mat(string name, string hex, float smooth, string emission = null)
        {
            string path = $"{Dir}/Graybox_{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", PropKit.Hex(hex));
            m.SetFloat("_Smoothness", smooth);
            if (emission != null)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", PropKit.Hex(emission) * 3f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // ───────── 置き方 ─────────

        /// <summary>広間の真ん中から見た向き（度。東 0、北 90、西 180、南 270）と半径の床の点。</summary>
        static Vector3 Polar(float deg, float r, float y) =>
            new Vector3(r * Mathf.Cos(deg * Mathf.Deg2Rad), y, r * Mathf.Sin(deg * Mathf.Deg2Rad));

        /// <summary>その角度の壁ぎわから、広間の真ん中を向く Y 回転。</summary>
        static float FaceCenter(float deg) =>
            Mathf.Atan2(-Mathf.Cos(deg * Mathf.Deg2Rad), -Mathf.Sin(deg * Mathf.Deg2Rad)) * Mathf.Rad2Deg;

        static Transform Group(string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_root, false);
            return t;
        }

        /// <summary>箱・筒・玉。筒の当たり判定は形どおりのメッシュに替える（カプセルにしない）。</summary>
        static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material m,
                               float yaw = 0f, Vector3? euler = null, bool collide = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler ?? new Vector3(0f, yaw, 0f)));
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            var col = go.GetComponent<Collider>();
            if (type == PrimitiveType.Cylinder || !collide)
            {
                UnityEngine.Object.DestroyImmediate(col);
                if (collide) go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            return go;
        }

        /// <summary>下の角 min と上の角 max の間の箱（回さない）。</summary>
        static GameObject Box(Transform parent, string name, Vector3 min, Vector3 max, Material m, bool collide = true) =>
            Prim(PrimitiveType.Cube, parent, name, (min + max) * 0.5f, max - min, m, 0f, null, collide);

        /// <summary>足元の真ん中 p に立つ筒（半径 r、高さ h）。</summary>
        static GameObject Cyl(Transform parent, string name, Vector3 p, float r, float h, Material m, bool collide = true) =>
            Prim(PrimitiveType.Cylinder, parent, name, p + Vector3.up * h * 0.5f, new Vector3(r * 2f, h * 0.5f, r * 2f), m, 0f, null, collide);

        /// <summary>a から b へ渡す棒（軸・柱・手すり）。</summary>
        static GameObject Rod(Transform parent, string name, Vector3 a, Vector3 b, float r, Material m)
        {
            var go = Prim(PrimitiveType.Cylinder, parent, name, (a + b) * 0.5f, new Vector3(r * 2f, (b - a).magnitude * 0.5f, r * 2f), m, 0f, null, false);
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }

        static void Lamp(Transform parent, Vector3 p)
        {
            // 油の灯（Nov 4）：素焼きの皿に火。壁ぎわの灯は腕木で壁から出す（広間の壁ぎわ・トンネル。桟橋と外は柱や縄）
            Prim(PrimitiveType.Sphere, parent, "Lamp", p, new Vector3(0.1f, 0.14f, 0.1f), _lamp, 0f, null, false);
            Cyl(parent, "LampDish", p + Vector3.down * 0.1f, 0.12f, 0.05f, _rock, false);
            float r = new Vector2(p.x, p.z).magnitude;
            Vector3 wall = Vector3.zero;
            if (p.z < -6f && p.z > MouthZ && Mathf.Abs(p.x) < 4f) wall = new Vector3(Mathf.Sign(p.x), 0f, 0f);
            else if (p.z >= -6f && r > 4f && r < 6.5f) wall = new Vector3(p.x, 0f, p.z) / r;
            if (wall != Vector3.zero)
            {
                var a = p + Vector3.down * 0.1f;
                Rod(parent, "LampArm", a, a + wall * 0.42f, 0.02f, _wood);
                Rod(parent, "LampArmBrace", a + wall * 0.15f, a + wall * 0.42f + Vector3.down * 0.25f, 0.015f, _wood);
            }
            var l = new GameObject("LampLight").AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.position = p;
            l.type = LightType.Point;
            l.color = new Color(1f, 0.72f, 0.42f);
            l.range = 5.5f;
            l.intensity = 2.2f;
            l.shadows = LightShadows.None;
        }

        static void Label(string text, Vector3 p, float yaw, float size = 1.6f)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(_root.Find("Labels") ?? Group("Labels"), false);
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
            var tmp = go.AddComponent<TextMeshPro>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansJP SDF.asset");
            if (font) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.12f, 0.12f, 0.14f);
            tmp.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
        }

        // ───────── メッシュ ─────────

        /// <summary>
        /// 格子の面 P(u, v)（u, v は 0〜1）。skip(四角の真ん中) が true の四角は抜く（入口・天窓の穴）。
        /// 面の向きは四角ごとに face(真ん中) の向きへそろえる（殻は内向き、外の崖は外向き）。
        /// </summary>
        static GameObject Grid(Transform parent, string name, Func<float, float, Vector3> P, int nu, int nv,
                               Func<Vector3, bool> skip, Func<Vector3, Vector3> face, Material m, bool collide = true)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            int cols = nu + 1;
            for (int j = 0; j <= nv; j++)
                for (int i = 0; i <= nu; i++) verts.Add(P(i / (float)nu, j / (float)nv));
            for (int j = 0; j < nv; j++)
                for (int i = 0; i < nu; i++)
                {
                    int a = j * cols + i, b = a + 1, c = a + cols + 1, d = a + cols;
                    var ctr = (verts[a] + verts[b] + verts[c] + verts[d]) * 0.25f;
                    if (skip != null && skip(ctr)) continue;
                    var n = Vector3.Cross(verts[b] - verts[a], verts[d] - verts[a]);
                    if (Vector3.Dot(n, face(ctr)) >= 0f) tris.AddRange(new[] { a, b, c, a, c, d });
                    else tris.AddRange(new[] { a, c, b, a, d, c });
                }
            var mesh = new Mesh { name = name + "_Mesh",indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh = NatureAssets.SaveMesh(mesh, $"{Dir}/{name}_Mesh.asset");

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            if (collide) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        /// <summary>扇形の床（岩棚・らせんの坂）：角度 a0→a1、半径 r0〜r1、高さ y0→y1（坂ならまっすぐ上がる）、厚み th。</summary>
        static void Sector(Transform parent, string name, float a0, float a1, float r0, float r1, float y0, float y1, float th, Material m, bool visible = true)
        {
            int n = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 4f));
            Func<float, float, Vector3> top = (u, v) => Polar(Mathf.Lerp(a0, a1, u), Mathf.Lerp(r0, r1, v), Mathf.Lerp(y0, y1, u));
            var go = Grid(parent, name, top, n, 2, null, _ => Vector3.up, m);
            if (!visible) { go.GetComponent<Renderer>().enabled = false; return; }
            // 内側のふちの面と裏（見た目だけ）
            Grid(parent, name + "_Edge", (u, v) => top(u, 0f) + Vector3.down * th * v, n, 1, null, p => -new Vector3(p.x, 0f, p.z), m, false);
            Grid(parent, name + "_Under", (u, v) => top(u, v) + Vector3.down * th, n, 2, null, _ => Vector3.down, m, false);
        }

        /// <summary>折れ線に沿った水の帯（幅 w）。y は点ごと（滝は下がる）。当たり判定なし。</summary>
        static void WaterStrip(Transform parent, string name, Vector3[] pts, float w)
        {
            int n = (pts.Length - 1) * 4;
            Func<float, float, Vector3> P = (u, v) =>
            {
                float f = u * (pts.Length - 1);
                int i = Mathf.Min(pts.Length - 2, Mathf.FloorToInt(f));
                var p = Vector3.Lerp(pts[i], pts[i + 1], f - i);
                var dir = pts[i + 1] - pts[i]; dir.y = 0f;
                var side = Vector3.Cross(Vector3.up, dir.normalized);
                return p + side * (v - 0.5f) * w;
            };
            Grid(parent, name, P, n, 1, null, _ => Vector3.up, _water, false);
        }

        // ───────── 撮る ─────────

        const int ShotW = 960, ShotH = 540;

        /// <summary>
        /// 保存した場面を見えない場面に移して撮る（開いている場面の物は写らない）。
        /// cut が true の絵は殻（Shell_ の付く物）を隠して中を見せる。6枚を1枚の 3×2 にまとめる。
        /// </summary>
        static string Shots(Scene scene, (string name, Vector3 pos, Vector3 look, float fov, bool cut)[] views, string outPng,
                            (string name, Vector3 pos, Vector3 look, float fov, bool cut)[] extraViews = null, string extraPng = null,
                            (string name, Vector3 pos, Vector3 look, float fov, bool cut)[] views3 = null, string png3 = null)
        {
            var prev = EditorSceneManager.NewPreviewScene();
            var sheet = new Texture2D(ShotW * 3, ShotH * 2, TextureFormat.RGB24, false);
            var rt = new RenderTexture(ShotW, ShotH, 24) { antiAliasing = 4 };
            try
            {
                Camera cam = null;
                var shells = new List<Renderer>();
                void Sheet((string name, Vector3 pos, Vector3 look, float fov, bool cut)[] vs, string png)
                {
                    for (int k = 0; k < vs.Length && k < 6; k++)
                    {
                        var v = vs[k];
                        foreach (var r in shells) r.enabled = !v.cut;
                        cam.fieldOfView = v.fov;
                        cam.transform.SetPositionAndRotation(v.pos, Quaternion.LookRotation(v.look - v.pos));
                        cam.Render();
                        RenderTexture.active = rt;
                        var tex = new Texture2D(ShotW, ShotH, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, ShotW, ShotH), 0, 0);
                        tex.Apply();
                        sheet.SetPixels(ShotW * (k % 3), ShotH * (1 - k / 3), ShotW, ShotH, tex.GetPixels());
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                    RenderTexture.active = null;
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(png));
                    System.IO.File.WriteAllBytes(png, sheet.EncodeToPNG());
                }

                foreach (var go in scene.GetRootGameObjects()) EditorSceneManager.MoveGameObjectToScene(go, prev);
                foreach (var go in prev.GetRootGameObjects())
                {
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                        if (r.name.StartsWith("Shell_")) shells.Add(r);
                    foreach (var c in go.GetComponentsInChildren<Camera>(true)) c.enabled = false;
                }

                cam = new GameObject("ShotCam").AddComponent<Camera>();
                EditorSceneManager.MoveGameObjectToScene(cam.gameObject, prev);
                cam.scene = prev;
                cam.targetTexture = rt;
                cam.nearClipPlane = 0.03f;
                cam.farClipPlane = 400f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.7f, 0.8f, 0.88f);

                Sheet(views, outPng);
                if (extraViews != null && extraPng != null) Sheet(extraViews, extraPng);
                if (views3 != null && png3 != null) Sheet(views3, png3);
                return outPng;
            }
            finally
            {
                RenderTexture.active = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.ClosePreviewScene(prev);
            }
        }
    }
}
