using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SlugDesign
{
    /// <summary>Builds meshes, materials and prefabs for every slug candidate.</summary>
    public static class SlugBuilder
    {
        public const string Root = "Assets/Slug/Generated";
        public const string ShaderName = "Slug/Toon";

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        [MenuItem("Slug/Build All Designs")]
        public static void BuildAll()
        {
            Pending.Clear();
            if (AssetDatabase.IsValidFolder(Root)) AssetDatabase.DeleteAsset(Root);
            EnsureFolder(Root);
            EnsureFolder(Root + "/Common");
            EnsureFolder(Root + "/Prefabs");

            var sphere = SlugMeshFactory.Sphere("Sphere", 1f, 14, 11);
            AssetDatabase.CreateAsset(sphere, Root + "/Common/Sphere.asset");

            foreach (var spec in SlugDesigns.All())
            {
                var go = Build(spec, sphere, $"{Root}/{spec.id}");
                PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/Slug_{spec.id}.prefab");
                Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Slug] built " + SlugDesigns.All().Count + " designs");
        }

        // ------------------------------------------------------------------ materials
        static readonly List<(Material mat, string path)> Pending = new List<(Material, string)>();

        static Material NewMat(string path, Shader sh)
        {
            var m = new Material(sh) { name = Path.GetFileNameWithoutExtension(path) };
            Pending.Add((m, path));   // written to disk only after it is fully configured
            return m;
        }

        static void Plain(Material m, Color baseColor)
        {
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_ShadeColor", new Color(0.7f, 0.7f, 0.85f));
            m.SetFloat("_BellyAmount", 0f);
            m.SetFloat("_PatternType", 0f);
            m.SetFloat("_OutlineWidth", 0f);
            m.SetFloat("_RimAmount", 0f);
            m.SetFloat("_WaveAmp", 0f);
            m.SetFloat("_AmbientStrength", 0.5f);
        }

        static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        // ------------------------------------------------------------------ one slug
        static Vector3 W(Ring fr, Vector3 local, float side)
            => fr.right * (local.x * side) + fr.up * local.y + fr.forward * local.z;

        static GameObject Part(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            return go;
        }

        /// <summary>
        /// Builds one slug. The body, ears and hands are identical for every look; only colours and eyes
        /// change when a <paramref name="look"/> is given (null = the original v1 pop look).
        /// </summary>
        public static GameObject Build(SlugDesignSpec d, Mesh sphere, string dir, SlugLook look = null)
        {
            EnsureFolder(dir);
            var pal = look?.palette;
            var es = look?.eye;
            var shader = Shader.Find(ShaderName);
            var shape = SlugMeshFactory.Body(d.id + "_Body", d.body);
            AssetDatabase.CreateAsset(shape.mesh, dir + "/Body.asset");

            // ---------- materials
            var body = NewMat(dir + "/Body.mat", shader);
            d.applyBody(body, d);
            if (pal != null) PaletteApply.Body(body, pal);
            float L = d.body.length;
            float C = 2f * Mathf.PI * 0.5f * (shape.Profile(0.5f, true) + shape.Profile(0.5f, false));
            body.SetFloat("_BodyLength", L);
            body.SetVector("_PatternScale", new Vector4(d.patternCells, d.patternCells * L / C, 0, 0));
            body.SetFloat("_StitchDash", L / d.stitchDashLen);
            if (d.transparent) MakeTransparent(body);

            var app = NewMat(dir + "/Appendage.mat", shader);
            app.CopyPropertiesFromMaterial(body);
            app.SetFloat("_WaveAmp", 0f);
            app.SetFloat("_RuffleAmp", 0f);
            app.SetFloat("_BellyAmount", 0f);
            app.SetFloat("_PatternType", 0f);
            app.SetFloat("_StitchDash", 5f);
            app.SetFloat("_BumpFreq", 9f);
            d.applyAppendage?.Invoke(app, d);
            if (pal != null) PaletteApply.Appendage(app, pal);
            if (d.transparent) MakeTransparent(app);

            var eyeMat = NewMat(dir + "/Eye.mat", shader);
            Plain(eyeMat, es != null ? es.eyeColor : d.eyeColor);
            var hiMat = NewMat(dir + "/EyeHighlight.mat", shader);
            Plain(hiMat, Color.white);
            hiMat.SetColor("_ShadeColor", Color.white);
            hiMat.SetColor("_EmissionColor", (es != null ? new Color(1f, 0.97f, 0.9f) * 0.75f : Color.white * 0.9f));
            var cheekMat = NewMat(dir + "/Cheek.mat", shader);
            Plain(cheekMat, pal != null ? Color.Lerp(pal.body, pal.cheek, 0.55f) : d.cheekColor);
            cheekMat.SetColor("_ShadeColor", new Color(0.85f, 0.6f, 0.75f));
            var mouthMat = NewMat(dir + "/Mouth.mat", shader);
            Plain(mouthMat, pal != null ? Palette.Darken(pal.body, 0.3f) : new Color(0.32f, 0.1f, 0.16f));

            Mesh lidMesh = null;
            Material lidMat = null;
            if (es != null && es.lid)
            {
                lidMesh = SlugMeshFactory.Lid(d.id + "_Lid", es.lidCut, 14, 8);
                AssetDatabase.CreateAsset(lidMesh, dir + "/Lid.asset");
                lidMat = NewMat(dir + "/Lid.mat", shader);
                Color lidBase = pal != null ? Color.Lerp(pal.body, Color.black, 0.14f) : Color.Lerp(body.GetColor("_BaseColor"), Color.black, 0.14f);
                Plain(lidMat, lidBase);
                lidMat.SetColor("_ShadeColor", pal != null ? pal.shade : new Color(0.7f, 0.65f, 0.75f));
            }

            // ---------- hierarchy
            var root = new GameObject("Slug_" + d.id);
            var rig = root.AddComponent<SlugRig>();
            Part(root.transform, "Body", shape.mesh, body);

            // face frame
            var f = d.face;
            Ring fr = shape.FrameAt(f.headT);
            float tilt = f.faceTilt * Mathf.Deg2Rad;
            Vector3 faceF = fr.forward * Mathf.Cos(tilt) - fr.up * Mathf.Sin(tilt);
            Vector3 faceU = fr.up * Mathf.Cos(tilt) + fr.forward * Mathf.Sin(tilt);
            Vector3 faceR = fr.right;
            Vector3 origin = fr.center + faceU * (fr.ry * 0.05f);
            System.Func<float, float, Vector3> dirOf = (yaw, pitch) =>
            {
                float y = yaw * Mathf.Deg2Rad, p = pitch * Mathf.Deg2Rad;
                return (faceF * Mathf.Cos(p) * Mathf.Cos(y) + faceR * Mathf.Cos(p) * Mathf.Sin(y) + faceU * Mathf.Sin(p)).normalized;
            };

            // eyes: a big dark dome with two matte highlights
            foreach (float side in new[] { -1f, 1f })
            {
                string tag = side < 0 ? "L" : "R";
                if (!shape.Raycast(origin, dirOf(side * f.eyeYaw, f.eyePitch), out var p, out var n)) continue;
                float k = side < 0 ? f.eyeScaleL : f.eyeScaleR;
                Vector3 size = f.eyeSize * k;
                if (es != null)
                {
                    if (es.dot) { float r = (size.x + size.y) * 0.5f * es.dotRadius; size = new Vector3(r, r, r * 0.8f); }
                    else size *= es.scale;
                }
                Quaternion rot = Quaternion.LookRotation(n, faceU);
                var eye = Part(root.transform, "Eye_" + tag, sphere, eyeMat);
                eye.transform.SetPositionAndRotation(p - n * (size.z * 0.4f), rot);
                eye.transform.localScale = size;
                if (es == null)
                {
                    AddHighlight(root.transform, "Eye_" + tag + "_Highlight", sphere, hiMat, eye.transform, size, new Vector3(0.30f, 0.34f, 0.89f), 0.30f);
                    AddHighlight(root.transform, "Eye_" + tag + "_Highlight2", sphere, hiMat, eye.transform, size, new Vector3(-0.30f, -0.32f, 0.9f), 0.14f);
                }
                else
                {
                    int gi = 0;
                    foreach (var g in es.glints)
                        AddHighlight(root.transform, "Eye_" + tag + "_Highlight" + (gi++ == 0 ? "" : "2"), sphere, hiMat, eye.transform, size, g.unit, g.rel);
                }
                if (lidMesh != null)
                {
                    var lid = Part(root.transform, "Eye_" + tag + "_Lid", lidMesh, lidMat);
                    lid.transform.SetPositionAndRotation(eye.transform.position, rot * Quaternion.Euler(0f, 0f, -side * es.lidTiltDeg));
                    lid.transform.localScale = new Vector3(size.x * 1.06f, size.y * 1.06f, size.z * 1.12f);
                }

                if (f.cheeks && shape.Raycast(origin, dirOf(side * f.cheekYaw, f.cheekPitch), out var pc, out var nc))
                {
                    var cheek = Part(root.transform, "Cheek_" + tag, sphere, cheekMat);
                    cheek.transform.SetPositionAndRotation(pc - nc * 0.004f, Quaternion.LookRotation(nc, faceU));
                    float cs = f.cheekSize * (pal != null ? 0.6f : 1f);   // v2: just a faint blush
                    cheek.transform.localScale = new Vector3(cs * 1.25f, cs * 0.8f, cs * 0.3f);
                }
            }

            if (f.mouth && shape.Raycast(origin, dirOf(0f, f.mouthPitch), out var pm, out var nm))
            {
                var smile = SlugMeshFactory.Smile(d.id + "_Mouth", f.mouthHalfW, f.mouthDepth, f.mouthThick, 6);
                AssetDatabase.CreateAsset(smile, dir + "/Mouth.asset");
                var mouth = Part(root.transform, "Mouth", smile, mouthMat);
                mouth.transform.SetPositionAndRotation(pm - nm * (f.mouthThick * 0.3f), Quaternion.LookRotation(nm, faceU));
            }

            // ears (the antennae are ears)
            int earIndex = 0;
            foreach (var e in d.ears)
            {
                foreach (float side in e.mirrored ? new[] { -1f, 1f } : new[] { 1f })
                {
                    Ring ef = shape.FrameAt(e.t);
                    Quaternion roll = Quaternion.AngleAxis(e.roll, ef.forward);
                    Vector3 d0 = roll * W(ef, e.d0, side), d1 = roll * W(ef, e.d1, side);
                    Vector3 hint = roll * (ef.right * side);
                    Vector3 start = ef.center;
                    shape.Raycast(start, d0.normalized, out var pe, out _);
                    Vector3 rootPos = pe - d0.normalized * (e.baseR * 0.9f);
                    var mesh = SlugMeshFactory.Ear($"{d.id}_Ear{earIndex}", rootPos, d0, d1, hint, e.length, e.baseR, e.tipR, e.flat, e.bulbR, e.radial);
                    AssetDatabase.CreateAsset(mesh, $"{dir}/Ear{earIndex}.asset");
                    var mat = app;
                    if (e.tipColor.HasValue && pal == null)
                    {
                        mat = NewMat($"{dir}/Ear{earIndex}.mat", shader);
                        mat.CopyPropertiesFromMaterial(app);
                        mat.SetColor("_BaseColor", e.tipColor.Value);
                    }
                    string nm2 = e.mirrored ? (side < 0 ? "Ear_L" : "Ear_R") : "Tuft";
                    Part(root.transform, nm2 + (e.mirrored ? "" : earIndex.ToString()), mesh, mat);
                    earIndex++;
                }
            }

            // hands: finger-less round mitts that can be put away
            var h = d.hand;
            var handMesh = SlugMeshFactory.Hand(d.id + "_Hand", h.armLength, h.armR, h.handR, h.radial);
            AssetDatabase.CreateAsset(handMesh, dir + "/Hand.asset");
            Ring hf = shape.FrameAt(h.t);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 rayDir = W(hf, h.rayDir, side).normalized;
                Vector3 outDir = W(hf, h.outDir, side).normalized;
                if (!shape.Raycast(hf.center, rayDir, out var ph, out var nh)) continue;
                var hand = Part(root.transform, side < 0 ? "Hand_L" : "Hand_R", handMesh, app);
                hand.transform.SetPositionAndRotation(ph - nh * (h.armR * 1.1f), Quaternion.LookRotation(outDir, hf.up));
                if (side < 0) rig.handL = hand.transform; else rig.handR = hand.transform;
            }
            rig.handsExtend = 1f;
            rig.Apply();

            foreach (var (m, path) in Pending) AssetDatabase.CreateAsset(m, path);
            Pending.Clear();

            // centre the pivot on the footprint so the slug can be rotated around its middle
            var b = new Bounds(shape.mesh.bounds.center, Vector3.zero);
            b.Encapsulate(shape.mesh.bounds);
            Vector3 c = new Vector3(b.center.x, 0f, b.center.z);
            foreach (Transform ch in root.transform) ch.localPosition -= c;
            return root;
        }

        static void AddHighlight(Transform parent, string name, Mesh sphere, Material mat, Transform eye, Vector3 size, Vector3 unit, float rel)
        {
            var go = Part(parent, name, sphere, mat);
            Vector3 local = new Vector3(unit.x * size.x, unit.y * size.y, unit.z * size.z);
            go.transform.SetPositionAndRotation(eye.position + eye.rotation * local, eye.rotation);
            float r = rel * (size.x + size.y) * 0.5f;
            go.transform.localScale = new Vector3(r, r, r * 0.5f);
        }
    }
}
