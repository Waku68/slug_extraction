using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 足のあるナメクジ（小さな恐竜のような2本足）のラフ。Nov 1、わくさんの参考の絵から。
    ///   決まり：足2本で歩く、体は絵のとおり（上半身を起こした丸い頭、ぺたっとした足の裏はなし）、
    ///   大きさは今のまま（目の高さ 0.47 m）。
    ///   B ずんぐり（選んだ案）：胴は太く短い、尾は短く地面に付く。手・顔・模様は今のナメクジ（SlugPop に
    ///     ApplyCurrent を当てた物）の部品をそのまま写して顔と胸へ動かす。足は今の手と同じ太さ・長さで
    ///     太もも＋すね（ひざは前へ曲がる）。触角は小さめで先に玉
    ///   C 前のめり（灰色のまま）：上半身を前へ傾け、尾を高く長く。足はひざを深く曲げる
    ///   A ふつう・横に置いた今のナメクジは、わくさんの指示で消した
    /// 座標：+Z が前、+Y が上、地面 y = 0。メニュー Slug/Character/Build Dino Rough。
    /// </summary>
    public static class DinoSlugRough
    {
        const string ScenePath = "Assets/Scenes/Preview/DinoSlugRough.unity";
        const string ShotDir = "Screenshots/Character";
        const float EyeY = 0.47f;

        class Spec
        {
            public string name;
            public Vector3[] spine;         // tail tip -> head top (centre line)
            public float[] radS, radR;      // body radius keys along the spine (0 tail .. 1 head)
            public float width = 1.05f;     // side radius / up radius
            public Vector3 hip, knee, ankle;// right leg; mirrored for the left (knee unused when dressed)
            public float thighR, shinR;
            public float footLen = 0.11f, footW = 0.075f;
            public float armDrop = 0.17f;   // arm root this far below the head centre
            public float armOut = 0.11f;    // arm root this far to the side (on the body surface)
            public float antLen = 0.13f, antR0 = 0.018f, antR1 = 0.011f, antBall = 0.024f;
            public bool dress;              // take the hands, face and pattern from the current slug
        }

        static Spec[] Specs() => new[]
        {
            new Spec
            {
                name = "B_Chunky",
                // Nov 1 わくさん：手を少し上、足の太さ 1/2、足を腕に近づける、尾は地面に付けて短く
                // → 手・顔・模様は今のもの、足は手の太さと長さで太もも＋すね、触角は小さく細く玉も小さく
                spine = new[] { V(0.02f, -0.52f), V(0.09f, -0.34f), V(0.19f, -0.16f), V(0.29f, -0.02f), V(0.42f, 0.07f), V(0.63f, 0.12f) },   // bend spread out: no crease on the back
                radS = new[] { 0f, 0.30f, 0.55f, 0.72f, 0.86f, 1f },
                radR = new[] { 0.016f, 0.095f, 0.170f, 0.190f, 0.165f, 0.155f },
                width = 1.12f,
                hip = new Vector3(0.12f, 0.15f, 0.05f), ankle = new Vector3(0.13f, 0.03f, 0.05f),
                footLen = 0.085f, footW = 0.055f,
                antLen = 0.09f, antR0 = 0.010f, antR1 = 0.007f, antBall = 0.015f,
                dress = true,
            },
            new Spec
            {
                name = "C_Lean",
                spine = new[] { V(0.24f, -0.82f), V(0.24f, -0.52f), V(0.25f, -0.24f), V(0.29f, -0.02f), V(0.38f, 0.12f), V(0.50f, 0.20f), V(0.61f, 0.24f) },
                radS = new[] { 0f, 0.30f, 0.55f, 0.72f, 0.86f, 1f },
                radR = new[] { 0.008f, 0.070f, 0.145f, 0.160f, 0.145f, 0.140f },
                hip = new Vector3(0.11f, 0.19f, -0.08f), knee = new Vector3(0.13f, 0.12f, 0.03f), ankle = new Vector3(0.13f, 0.035f, -0.04f),
                thighR = 0.062f, shinR = 0.044f,
            },
        };

        static Vector3 V(float y, float z) => new Vector3(0f, y, z);

        [MenuItem("Slug/Character/Build Dino Rough")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var gray = NewMat(new Color(0.62f, 0.62f, 0.60f));
            var dark = NewMat(new Color(0.08f, 0.08f, 0.08f));
            var groundMat = NewMat(new Color(0.40f, 0.46f, 0.50f));

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2f, 1f, 2f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // the current slug is only the source of the hands, face and materials; removed afterwards
            var pop = AssetDatabase.LoadAssetAtPath<GameObject>(SlugLookbook.PrefabPath);
            var src = (GameObject)PrefabUtility.InstantiatePrefab(pop);
            var srcSlug = src.GetComponent<SlugPop>();
            SlugLookbook.ApplyCurrent(srcSlug);                 // the game applies the current values to the prefab, so do the same
            srcSlug.Rebuild();                                  // OnEnable only schedules the build
            src.transform.position = new Vector3(5f, 0f, 0f);  // out of the way while copying

            // in a row along Z (heads to +Z) with a gap between bounds
            var row = new List<GameObject>();
            foreach (var spec in Specs()) row.Add(BuildOne(spec, gray, dark, spec.dress ? src : null));
            UnityEngine.Object.DestroyImmediate(src);
            float cursor = 0f;
            foreach (var go in row)
            {
                go.transform.position += Vector3.forward * (cursor - Bounds(go).min.z);
                cursor = Bounds(go).max.z + 0.3f;
            }

            var sun = GameObject.Find("Directional Light");
            if (sun != null) sun.transform.rotation = Quaternion.Euler(40f, -120f, 0f);

            Directory.CreateDirectory("Assets/Scenes/Preview");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Directory.CreateDirectory(ShotDir);
            var all = Bounds(row[0]);
            foreach (var go in row) all.Encapsulate(Bounds(go));
            ShotFramed("dino_rough", all, 2.4f);                // all in a row
            ShotFramed("dino_rough_B", Bounds(row[0]), 1.4f);   // the one being refined
            Debug.Log("[DinoSlugRough] built " + ScenePath + ", shots in " + ShotDir);
        }

        // ------------------------------------------------------------------ one figure
        static GameObject BuildOne(Spec s, Material gray, Material dark, GameObject src)
        {
            var root = new GameObject("Dino_" + s.name);
            var pts = Resample(CatmullRom(s.spine, 12), 56);
            float len = 0f;
            for (int i = 1; i < pts.Count; i++) len += Vector3.Distance(pts[i - 1], pts[i]);
            float rEnd = s.radR[s.radR.Length - 1];
            Func<float, float> ry = t =>
            {
                float r = Keys(s.radS, s.radR, t);
                float d = (1f - t) * len;                       // round head cap
                if (d < rEnd) r = Mathf.Min(r, Mathf.Sqrt(Mathf.Max(0f, rEnd * rEnd - (rEnd - d) * (rEnd - d))));
                return r;
            };
            Func<float, float> rx = t => ry(t) * s.width;

            // materials: the current slug's own (still: no wave) when dressed, plain grey otherwise
            Material bodyMat = gray, limbMat = gray, antMat = gray;
            Transform srcVis = src != null ? src.transform.Find("Visual") : null;
            if (srcVis != null)
            {
                bodyMat = Still(srcVis.Find("Body"));
                limbMat = Still(srcVis.Find("Hand_R"));
                antMat = Still(srcVis.Find("Ear_R"));
            }
            var body = Part(root, "Body", SlugMeshFactory.PathLoft("Body", pts, rx, ry, new Vector3(0f, 1f, -1f).normalized, 20), bodyMat);

            Vector3 head = pts[pts.Count - 1] - Vector3.up * rEnd;
            float handR = s.thighR, handLen = 0f;
            if (srcVis != null)
            {
                // face and hands: copies of the current slug's parts, moved so they sit on this body the same way
                var bodyCol = body.AddComponent<MeshCollider>();
                var srcCol = srcVis.Find("Body").gameObject.AddComponent<MeshCollider>();
                Physics.SyncTransforms();
                // each eye (with its highlight) and the mouth find this body's surface at their own spot
                Vector3 srcEye = srcVis.Find("Eye_R").GetComponent<Renderer>().bounds.center;
                float lift = EyeY - srcEye.y;
                foreach (var group in new[] { new[] { "Eye_L", "Eye_L_Highlight" }, new[] { "Eye_R", "Eye_R_Highlight" }, new[] { "Mouth" } })
                {
                    Vector3 at = srcVis.Find(group[0]).GetComponent<Renderer>().bounds.center;
                    Vector3 shift = FrontHit(bodyCol, root.transform.position + new Vector3(at.x - src.transform.position.x, at.y + lift, 0f))
                                  - FrontHit(srcCol, at);
                    foreach (var n in group) CopyPart(srcVis.Find(n), root.transform, shift);
                }
                Transform srcHand = srcVis.Find("Hand_R");
                Vector3 handRoot = srcHand.position;
                Vector3 handShift = FrontHit(bodyCol, root.transform.position + new Vector3(handRoot.x - src.transform.position.x, handRoot.y, 0f))
                                  - FrontHit(srcCol, handRoot);
                CopyPart(srcVis.Find("Hand_L"), root.transform, handShift);
                CopyPart(srcHand, root.transform, handShift);
                UnityEngine.Object.DestroyImmediate(bodyCol);
                UnityEngine.Object.DestroyImmediate(srcCol);

                // the hand's thickness and length (from its own mesh) become the leg's
                var hm = srcHand.GetComponent<MeshFilter>().sharedMesh;
                handR = hm.bounds.size.x * 0.5f;
                foreach (var v in hm.vertices) handLen = Mathf.Max(handLen, v.magnitude);
                Debug.Log($"[DinoSlugRough] hand radius {handR:F3} m, length {handLen:F3} m -> thigh and shin");
            }

            for (int side = -1; side <= 1; side += 2)
            {
                if (srcVis == null)
                {
                    var eye = Part(root, side < 0 ? "Eye_L" : "Eye_R", SlugMeshFactory.Sphere("Eye", 0.022f, 12, 8), dark);
                    float eyeZ = head.z + Mathf.Sqrt(Mathf.Max(0f, rEnd * rEnd - 0.06f * 0.06f - (EyeY - head.y) * (EyeY - head.y))) - 0.005f;
                    eye.transform.localPosition = new Vector3(side * 0.06f, EyeY, eyeZ);

                    var arm = Part(root, side < 0 ? "Arm_L" : "Arm_R", SlugMeshFactory.ConeHand("Arm", 0.10f, 0.030f, 0.016f, 0.4f, 10), gray);
                    arm.transform.localPosition = head + new Vector3(side * s.armOut, -s.armDrop, 0.02f);
                    arm.transform.localRotation = Quaternion.Euler(25f, side * 18f, 0f);
                }

                // antenna with a ball tip (like the reference)
                Vector3 a0 = head + new Vector3(side * 0.05f, rEnd * 0.85f, -0.01f);
                var ant = new List<Vector3>();
                float k0 = s.antLen / 0.13f;
                for (int k = 0; k < 8; k++) { float u = k / 7f; ant.Add(a0 + new Vector3(side * 0.03f * u, 0.13f * u, 0.02f * u * u) * k0); }
                Part(root, side < 0 ? "Antenna_L" : "Antenna_R",
                     SlugMeshFactory.PathLoft("Antenna", ant, u => Mathf.Lerp(s.antR0, s.antR1, u), u => Mathf.Lerp(s.antR0, s.antR1, u), Vector3.forward, 10), antMat);
                var ball = Part(root, "AntennaBall", SlugMeshFactory.Sphere("Ball", s.antBall, 12, 8), antMat);
                ball.transform.localPosition = ant[ant.Count - 1];

                // leg: thigh forward-down to the knee, shin back-down to the ankle, flat oval foot
                Vector3 m = new Vector3(side, 1f, 1f);
                Vector3 h = Vector3.Scale(s.hip, m), an = Vector3.Scale(s.ankle, m), k2;
                float tr = s.thighR, sr = s.shinR;
                if (srcVis != null)
                {
                    k2 = Knee(h, an, handLen);                  // thigh and shin each as long as the hand
                    tr = sr = handR;
                }
                else
                {
                    k2 = Vector3.Scale(s.knee, m);
                    var thigh = Part(root, "Thigh", SlugMeshFactory.Sphere("Thigh", s.thighR * 1.25f, 14, 10), limbMat);
                    thigh.transform.localPosition = h + new Vector3(side * 0.01f, 0f, 0f);   // dino-like thigh bulge on the hip
                }
                var leg = Resample(CatmullRom(new[] { h, k2, an }, 8), 16);
                Func<float, float> lr = u => Mathf.Lerp(tr, sr, u);
                Part(root, side < 0 ? "Leg_L" : "Leg_R", SlugMeshFactory.PathLoft("Leg", leg, lr, lr, Vector3.forward, 14), limbMat);
                var knee = Part(root, "Knee", SlugMeshFactory.Sphere("Knee", Mathf.Lerp(tr, sr, 0.5f), 12, 8), limbMat);
                knee.transform.localPosition = k2;
                var foot = Part(root, side < 0 ? "Foot_L" : "Foot_R", SlugMeshFactory.Sphere("Foot", 0.5f, 14, 10), limbMat);
                foot.transform.localPosition = new Vector3(an.x, 0.022f, an.z + s.footLen * 0.25f);
                foot.transform.localScale = new Vector3(s.footW, 0.044f, s.footLen);
            }
            return root;
        }

        /// <summary>Two equal segments from hip to ankle; the knee bends forward (+Z).</summary>
        static Vector3 Knee(Vector3 hip, Vector3 ankle, float segLen)
        {
            Vector3 d = ankle - hip;
            float dist = d.magnitude;
            Vector3 mid = (hip + ankle) * 0.5f;
            float off = Mathf.Sqrt(Mathf.Max(0f, segLen * segLen - dist * dist * 0.25f));
            Vector3 fwd = Vector3.Cross(Vector3.right, d.normalized);   // perpendicular in the Y-Z plane
            if (fwd.z < 0f) fwd = -fwd;
            return mid + fwd.normalized * off;
        }

        /// <summary>Where a ray from straight in front (at this x, y) first meets the collider.</summary>
        static Vector3 FrontHit(Collider c, Vector3 at)
        {
            var ray = new Ray(new Vector3(at.x, at.y, c.bounds.max.z + 1f), Vector3.back);
            return c.Raycast(ray, out var hit, 10f) ? hit.point : new Vector3(at.x, at.y, c.bounds.max.z);
        }

        /// <summary>A copy of a current-slug part (mesh and still material), moved by the world shift.</summary>
        static void CopyPart(Transform srcPart, Transform parent, Vector3 shift)
        {
            var go = new GameObject(srcPart.name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(srcPart.position + shift, srcPart.rotation);
            go.transform.localScale = srcPart.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = UnityEngine.Object.Instantiate(srcPart.GetComponent<MeshFilter>().sharedMesh);
            go.AddComponent<MeshRenderer>().sharedMaterial = Still(srcPart);
        }

        /// <summary>A copy of the part's Slug/Toon material with the crawl motion turned off.</summary>
        static Material Still(Transform part)
        {
            var m = new Material(part.GetComponent<Renderer>().sharedMaterial);
            foreach (var p in new[] { "_WaveAmp", "_Stretch", "_JiggleAmp", "_RuffleAmp" })
                if (m.HasProperty(p)) m.SetFloat(p, 0f);
            return m;
        }

        static GameObject Part(GameObject root, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Material NewMat(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(sh);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.1f);
            return m;
        }

        // ------------------------------------------------------------------ curves
        static float Keys(float[] s, float[] v, float t)
        {
            if (t <= s[0]) return v[0];
            for (int i = 1; i < s.Length; i++)
                if (t <= s[i]) return Mathf.Lerp(v[i - 1], v[i], Mathf.SmoothStep(0f, 1f, (t - s[i - 1]) / (s[i] - s[i - 1])));
            return v[v.Length - 1];
        }

        static List<Vector3> CatmullRom(IList<Vector3> c, int perSeg)
        {
            var o = new List<Vector3>();
            for (int i = 0; i < c.Count - 1; i++)
            {
                Vector3 p0 = c[Mathf.Max(i - 1, 0)], p1 = c[i], p2 = c[i + 1], p3 = c[Mathf.Min(i + 2, c.Count - 1)];
                for (int k = 0; k < perSeg; k++)
                {
                    float t = (float)k / perSeg, t2 = t * t, t3 = t2 * t;
                    o.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            o.Add(c[c.Count - 1]);
            return o;
        }

        static List<Vector3> Resample(List<Vector3> p, int n)
        {
            var acc = new float[p.Count];
            for (int i = 1; i < p.Count; i++) acc[i] = acc[i - 1] + Vector3.Distance(p[i - 1], p[i]);
            float total = acc[p.Count - 1];
            var o = new List<Vector3>();
            int j = 1;
            for (int k = 0; k < n; k++)
            {
                float d = total * k / (n - 1);
                while (j < p.Count - 1 && acc[j] < d) j++;
                float f = Mathf.InverseLerp(acc[j - 1], acc[j], d);
                o.Add(Vector3.Lerp(p[j - 1], p[j], f));
            }
            return o;
        }

        // ------------------------------------------------------------------ shots
        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>A side view (orthographic, heads to the right) and a front-right three-quarter view of the bounds.</summary>
        static void ShotFramed(string name, Bounds b, float aspect)
        {
            Vector3 c = b.center;
            float half = Mathf.Max(b.extents.y, b.extents.z / aspect) * 1.2f;
            int W = 1600, H = Mathf.RoundToInt(W / aspect);
            Shot(name + "_side.png", new Vector3(4f, c.y, c.z), c, half, W, H);
            float dist = half * 2f / Mathf.Tan(15f * Mathf.Deg2Rad) * 0.85f;
            Vector3 dir = new Vector3(0.75f, 0.28f, 0.6f).normalized;
            Shot(name + "_34.png", c + dir * dist, c, 0f, W, H);
        }

        /// <summary>orthoSize &gt; 0 = orthographic.</summary>
        static void Shot(string file, Vector3 camPos, Vector3 look, float orthoSize, int W, int H)
        {
            var camGo = new GameObject("ShotCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.13f, 0.17f);
            cam.nearClipPlane = 0.02f;
            cam.orthographic = orthoSize > 0f;
            cam.orthographicSize = orthoSize;
            cam.fieldOfView = 30f;
            cam.transform.position = camPos;
            cam.transform.LookAt(look);
            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = (float)W / H;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(camGo);
        }
    }
}
