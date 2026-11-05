using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の精製所の一角を本気で作る試し（Nov 4〜5、わくさん「この写真くらいのクオリティで、精製所の一角だけ本気で」。
    /// 参考の絵は夕方の工房：暖かい木、白い石灰岩、灯の橙、天井の穴からの日の筋）。
    /// 灰色の箱（BaseGrayboxBuilder）とは別の見る場面。小物は BaseRefineryProps.cs、メッシュの道具は BaseMesh.cs。
    /// - 塗り：島と同じ Slug/Toon（WorldLook、テクスチャなし。Nov 5 わくさん「やっぱテクスチャなし。popさが欠ける」）。
    ///   かじれない硬い物（木・石・塩・壺・鉄）はカクカク、縄・布・葉はなめらか。質感は形の丸い角・ゆがみと、シェーダーのむらだけ
    /// - 光：Slug/Toon は太陽 1 つと環境光だけで、点光源と焼き込みは効かない。なので部屋の殻（壁・天井）は影を落とさず、
    ///   太陽は手前の右上から当てる（形の読める向き）。灯は光る色＋にじみ（後処理の Bloom）、天井の穴の日の筋は加算の板 3 枚、霧は暖かい色
    /// - メニュー Slug/Base/Refinery Corner/1 Build（作って撮る）・2 Shoot（撮り直すだけ）
    /// - 場面 Assets/Scenes/Preview/BaseRefineryLook.unity、絵 Screenshots/Base/refinery_hero.png（ナメクジの目の高さ）と refinery_sheet.png（4 枚）
    /// - BaseTextures.cs は光の筋の板の明るさの絵だけに使う。Assets/Art/Models/Base/Refinery/Textures の木・石などはテクスチャの試しの残り（消してよい）
    /// </summary>
    public static partial class BaseRefineryBuilder
    {
        const string Dir = "Assets/Art/Models/Base/Refinery";
        const string TexDir = Dir + "/Textures";
        const string ScenePath = "Assets/Scenes/Preview/BaseRefineryLook.unity";
        static readonly Vector3 SunDir = new Vector3(-0.45f, -0.8f, -0.4f).normalized;   // 天井の穴の日の筋（見た目だけ）
        static readonly Vector3 KeyDir = new Vector3(-0.4f, -0.75f, 0.55f).normalized;   // トゥーンの太陽：手前の右上から奥の左下へ

        static Material[] _mats;
        static Material _beam;
        static Transform _root;

        static readonly (string name, Vector3 pos, Vector3 look, float fov)[] Views =
        {
            ("hero", new Vector3(0.3f, 0.47f, -2.6f), new Vector3(-0.1f, 1.45f, 2.0f), 66f),
            ("right", new Vector3(-1.0f, 1.3f, -1.6f), new Vector3(2.7f, 1.8f, 1.6f), 64f),
            ("jars", new Vector3(-0.95f, 0.6f, 1.75f), new Vector3(-1.6f, 0.95f, 3.2f), 62f),
            ("shop", new Vector3(1.2f, 0.47f, 1.35f), new Vector3(1.65f, 1.05f, 3.6f), 64f),
        };

        [MenuItem("Slug/Base/Refinery Corner/1 Build")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Refinery] stop play first"); return; }
            if (Lightmapping.isRunning) Lightmapping.Cancel();
            System.IO.Directory.CreateDirectory(TexDir);
            Materials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("RefineryCorner").transform;
            Floor(); Shell(); Stones(); Wood(); Mill(); Turbine(); Jars(); Amore(); MoldBench(); Workshop(); Waterfall();
            Garlands(); Ivy(); Foreground(); Lanterns(); Sky(); Post(); Slugs(); MainCamera();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Refinery] " + Shoot());
        }

        [MenuItem("Slug/Base/Refinery Corner/2 Shoot")]
        public static void ShootMenu() => Debug.Log("[Refinery] " + Shoot());

        // ───────── 素材 ─────────

        static void Materials()
        {
            _mats = PropKit.Mats(Dir, "Refinery",
                new PropKit.Spec("Wood", "#A2683A", true).Mottle("#875430", 0.3f, 3f),
                new PropKit.Spec("WoodDark", "#6E4A30", true).Mottle("#5A3B26", 0.3f, 3f),
                new PropKit.Spec("Limestone", "#D9D2C3", true).Mottle("#BDB2A0", 0.35f, 0.8f),
                new PropKit.Spec("Granite", "#A7A196", true).Mottle("#7E786E", 0.4f, 6f),
                new PropKit.Spec("Clay", "#E4CC9E", true).Mottle("#CDAE7E", 0.3f, 4f),
                new PropKit.Spec("Salt", "#F4F2EA", true),
                new PropKit.Spec("Rope", "#C09A62", false),
                new PropKit.Spec("Iron", "#4A403B", true),
                new PropKit.Spec("Glass", "#FFA552", false),
                new PropKit.Spec("Leaf", "#6FA244", false, 0f, true),
                new PropKit.Spec("Cloth", "#EADFC6", false),
                new PropKit.Spec("HerbRed", "#B5432E", false, 0f, true),
                new PropKit.Spec("HerbOrange", "#E0892E", false, 0f, true),
                new PropKit.Spec("HerbOchre", "#CFAE4C", false, 0f, true),
                new PropKit.Spec("Water", "#8AD4E6", false),
                new PropKit.Spec("Sky", "#CDEBF5", false, 0f, true),
                new PropKit.Spec("Glaze", "#86A36C", true),
                new PropKit.Spec("LimestoneDark", "#9C9180", true).Mottle("#857A6A", 0.35f, 0.8f));
            _mats[WT].SetColor("_EmissionColor", new Color(0.12f, 0.28f, 0.32f));
            _mats[SKY].SetColor("_EmissionColor", new Color(0.75f, 0.9f, 1f) * 1.2f);
            _mats[SKY].SetFloat("_RimAmount", 0f);
            _mats[GL].SetColor("_EmissionColor", new Color(1f, 0.48f, 0.14f) * 1.6f);
            _mats[GL].SetFloat("_RimAmount", 0f);
            PropKit.CairnPaint(_mats[LF]);
            foreach (var m in _mats)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
                if (m != _mats[LF]) m.SetColor("_ShadeColor", new Color(0.6f, 0.55f, 0.6f));   // 陰は暖かい赤み（夕方の工房）
                EditorUtility.SetDirty(m);
            }

            var beam = BaseTextures.Make(TexDir, "Beam", BaseTextures.Beam, 0f);
            string bp = Dir + "/Refinery_SunBeam.mat";
            _beam = AssetDatabase.LoadAssetAtPath<Material>(bp);
            if (_beam == null) { _beam = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(_beam, bp); }
            _beam.SetTexture("_BaseMap", beam.albedo);
            _beam.SetColor("_BaseColor", new Color(0.55f, 0.45f, 0.32f, 1f));
            _beam.SetFloat("_Surface", 1f); _beam.SetFloat("_Blend", 2f);
            _beam.SetFloat("_SrcBlend", (float)BlendMode.One); _beam.SetFloat("_DstBlend", (float)BlendMode.One);
            _beam.SetFloat("_ZWrite", 0f); _beam.SetFloat("_Cull", 0f);
            _beam.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _beam.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(_beam);
        }

        // ───────── 部品を置く ─────────

        /// <summary>メッシュを組んで保存し、場面に置く。staticGI なら光を焼く（小さな葉や縄は光の点で照らす）。</summary>
        static GameObject Part(string name, Action<BaseMesh> build, bool recalcNormals = false, bool staticGI = true, bool collide = false)
        {
            var bm = new BaseMesh(SUBS);
            build(bm);
            var mesh = NatureAssets.SaveMesh(bm.ToMesh("Refinery_" + name, recalcNormals), $"{Dir}/Refinery_{name}_Mesh.asset");
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = _mats;
            if (staticGI) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            if (name == "Shell") r.shadowCastingMode = ShadowCastingMode.Off;   // 部屋の殻は影を落とさない（トゥーンは太陽 1 つで照らすので）
            if (name == "Sky") r.shadowCastingMode = ShadowCastingMode.Off;
            if (collide) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            Debug.Log($"[Refinery] {name}: {bm.Triangles} tris");
            return go;
        }

        // ───────── 灯・日・霧 ─────────

        static void Sky()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(_root, false);
            sun.transform.rotation = Quaternion.LookRotation(KeyDir);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.46f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.35f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.18f, 0.13f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.26f, 0.2f, 0.15f);
            RenderSettings.fogDensity = 0.03f;
            DynamicGI.UpdateEnvironment();

            // 天井の穴からの光の筋：日の向きに沿った加算の板 3 枚（真ん中が明るく、下へ薄れる）
            var top = new Vector3(SkyHole.center.x, Ceil, SkyHole.center.y);
            float len = Ceil / -SunDir.y;
            var bm = new BaseMesh(1);
            for (int k = 0; k < 3; k++)
            {
                var side = Quaternion.AngleAxis(k * 60f, SunDir) * Vector3.Cross(SunDir, Vector3.right).normalized * 0.6f;
                var n = Vector3.Cross(side, SunDir).normalized;
                int a = bm.Add(top - side, n, new Vector2(0f, 1f)), b = bm.Add(top + side, n, new Vector2(1f, 1f));
                int c = bm.Add(top + SunDir * len + side * 1.3f, n, new Vector2(1f, 0f)), d = bm.Add(top + SunDir * len - side * 1.3f, n, new Vector2(0f, 0f));
                bm.Quad(0, a, b, c, d);
            }
            var mesh = NatureAssets.SaveMesh(bm.ToMesh("Refinery_SunBeam"), $"{Dir}/Refinery_SunBeam_Mesh.asset");
            var go = new GameObject("SunBeam");
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _beam;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void Post()
        {
            string path = Dir + "/Refinery_Post.asset";
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (prof == null) { prof = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(prof, path); }
            foreach (var c in prof.components) UnityEngine.Object.DestroyImmediate(c, true);
            prof.components.Clear();
            T Add<T>() where T : VolumeComponent { var c = prof.Add<T>(true); c.name = typeof(T).Name; AssetDatabase.AddObjectToAsset(c, prof); return c; }
            var tm = Add<Tonemapping>(); tm.mode.Override(TonemappingMode.None);
            var bl = Add<Bloom>(); bl.threshold.Override(1.0f); bl.intensity.Override(0.7f); bl.scatter.Override(0.65f); bl.tint.Override(new Color(1f, 0.85f, 0.7f));
            var ca = Add<ColorAdjustments>(); ca.postExposure.Override(0f); ca.contrast.Override(6f); ca.saturation.Override(0f);
            var vg = Add<Vignette>(); vg.intensity.Override(0.3f); vg.smoothness.Override(0.45f);
            EditorUtility.SetDirty(prof);
            var vol = new GameObject("PostVolume").AddComponent<Volume>();
            vol.transform.SetParent(_root, false);
            vol.isGlobal = true;
            vol.sharedProfile = prof;
        }

        static void Slugs()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(DinoSlugPhotoRough.RigPrefab);
            if (asset == null) return;
            foreach (var (p, yaw) in new[] { (new Vector3(-0.45f, GalY, 2.55f), 165f) })
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                go.transform.SetParent(_root, false);
                go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
            }
        }

        static Camera MakeCamera(GameObject go)
        {
            var cam = go.AddComponent<Camera>();
            cam.allowHDR = true;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.06f, 0.05f);
            var acd = go.AddComponent<UniversalAdditionalCameraData>();
            acd.renderPostProcessing = true;
            acd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            acd.antialiasingQuality = AntialiasingQuality.High;
            return cam;
        }

        static void MainCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetParent(_root, false);
            var cam = MakeCamera(go);
            var v = Views[0];
            cam.fieldOfView = v.fov;
            go.transform.SetPositionAndRotation(v.pos, Quaternion.LookRotation(v.look - v.pos));
        }

        // ───────── 撮る ─────────

        /// <summary>開いている場面（BaseRefineryLook）を撮る：1 枚目は 1920×1080、4 枚を 2×2 にまとめた絵も。</summary>
        public static string Shoot()
        {
            var go = new GameObject("ShotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = MakeCamera(go);
            System.IO.Directory.CreateDirectory("Screenshots/Base");
            try
            {
                Render(cam, Views[0], 1920, 1080, "Screenshots/Base/refinery_hero.png");
                const int w = 960, h = 540;
                var sheet = new Texture2D(w * 2, h * 2, TextureFormat.RGB24, false);
                for (int k = 0; k < Views.Length; k++)
                {
                    var tex = RenderTex(cam, Views[k], w, h);
                    sheet.SetPixels(w * (k % 2), h * (1 - k / 2), w, h, tex.GetPixels());
                    UnityEngine.Object.DestroyImmediate(tex);
                }
                System.IO.File.WriteAllBytes("Screenshots/Base/refinery_sheet.png", sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            return "Screenshots/Base/refinery_hero.png, refinery_sheet.png";
        }

        static void Render(Camera cam, (string, Vector3, Vector3, float) v, int w, int h, string png)
        {
            var tex = RenderTex(cam, v, w, h);
            System.IO.File.WriteAllBytes(png, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static Texture2D RenderTex(Camera cam, (string name, Vector3 pos, Vector3 look, float fov) v, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            try
            {
                cam.targetTexture = rt;
                cam.fieldOfView = v.fov;
                cam.transform.SetPositionAndRotation(v.pos, Quaternion.LookRotation(v.look - v.pos));
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                return tex;
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
