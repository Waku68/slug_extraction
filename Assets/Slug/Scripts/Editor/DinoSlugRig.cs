using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// The two-legged slug with bones (Nov 2, わくさん：新しいキャラで動きを作り直す). The same shape as the photo rough
    /// (<see cref="DinoSlugPhotoRough"/>, its distance field), skinned to bones and moved by <see cref="DinoSlugBody"/>.
    ///   Bones: Hips → Spine → Chest → Neck → Head along the middle line of the leaning body (SpineKeys, photo px 870, 905,
    ///   935, 965, 1000); Hips → Tail0..Tail4 back along the tail (px 820, 750, 680, 610, 550); Thigh, Shin, Foot for each leg
    ///   (the hip turning from inside the body, 6 cm above the photo's; knee, ankle as the photo), Arm → Hand for each arm (shoulder, hand). Eyes, mouth and antennae ride on the head.
    ///   Weights: each point of the skin goes to the part it is nearest in the field (head-body-tail, thigh, knee-shin-foot,
    ///   arm), blended where they meet (thigh 12 mm, arm 8 mm, knee 5 mm: the knee keeps a crease). On the head-body-tail it
    ///   goes along the middle line, half and half at each joint, blending over 3 cm. The foot takes what is under 15-30 mm.
    /// Menu Slug/Character/Build Dino Slug Rig → Assets/Slug/Prefabs/DinoSlug.prefab (meshes and materials in
    /// Assets/Slug/Generated/DinoRig, kept under the same files when rebuilt).
    /// </summary>
    public static partial class DinoSlugPhotoRough
    {
        public const string RigPrefab = "Assets/Slug/Prefabs/DinoSlug.prefab";
        const string RigDir = "Assets/Slug/Generated/DinoRig";

        // the legs turn from inside the body, above and in from the photo's hip (Nov 2 わくさん：体の根本から), and the low
        // belly round the top of the thigh goes with them, up to ThighShare
        static readonly Vector3 HipRoot = HipJ + new Vector3(-0.015f, 0.06f, -0.005f);
        const float ThighShare = 0.7f;

        static float SegDist(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            return (p - (a + ab * Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude))).magnitude;
        }

        // photo px along the middle line where each spine and tail bone starts
        static readonly float[] SpinePx = { 870f, 905f, 935f, 965f, 1000f };
        static readonly float[] TailPx = { 820f, 750f, 680f, 610f, 550f };

        [MenuItem("Slug/Character/Build Dino Slug Rig")]
        public static void BuildRig()
        {
            Directory.CreateDirectory(RigDir);
            var root = new GameObject("DinoSlug");
            try
            {
                // ---- bones
                Transform Bone(string name, Transform parent, Vector3 at, Quaternion rot)
                {
                    var t = new GameObject(name).transform;
                    t.SetParent(parent, false);
                    t.SetPositionAndRotation(at, rot);
                    return t;
                }
                var sp = new Transform[SpinePx.Length];
                string[] spineNames = { "Hips", "Spine", "Chest", "Neck", "Head" };
                Transform parentT = root.transform;
                for (int i = 0; i < sp.Length; i++) parentT = sp[i] = Bone(spineNames[i], parentT, SpineAt(SpinePx[i]), Quaternion.identity);
                var tail = new Transform[TailPx.Length];
                parentT = sp[0];
                for (int i = 0; i < tail.Length; i++) parentT = tail[i] = Bone("Tail" + i, parentT, SpineAt(TailPx[i]), Quaternion.identity);

                var thighs = new Transform[2]; var shins = new Transform[2]; var feet = new Transform[2];
                var arms = new Transform[2]; var hands = new Transform[2];
                for (int s = 0; s < 2; s++)
                {
                    float sx = s == 0 ? -1f : 1f;
                    string n = s == 0 ? "_L" : "_R";
                    Vector3 Mir(Vector3 v) => new Vector3(v.x * sx, v.y, v.z);
                    Vector3 hip = Mir(HipRoot), knee = Mir(KneeJ), ankle = Mir(AnkleJ);
                    thighs[s] = Bone("Thigh" + n, sp[0], hip, Along(knee - hip));
                    shins[s] = Bone("Shin" + n, sp[0], knee, Along(ankle - knee));
                    feet[s] = Bone("Foot" + n, sp[0], ankle, Quaternion.identity);
                    arms[s] = Bone("Arm" + n, sp[2], Mir(Shoulder), Quaternion.identity);
                    hands[s] = Bone("Hand" + n, arms[s], Mir(Hand), Quaternion.identity);
                }

                // ---- skin
                var mesh = Mesher.Build(Body, new Vector3(-0.26f, -0.01f, -0.66f), new Vector3(0.26f, 0.62f, 0.42f), 0.006f);
                SkinUV(mesh);
                var bones = new List<Transform>(sp);
                bones.AddRange(tail);
                bones.AddRange(thighs); bones.AddRange(shins); bones.AddRange(feet); bones.AddRange(arms); bones.AddRange(hands);
                Weigh(mesh, bones, sp, tail, thighs, shins, feet, arms, hands);
                var bind = new Matrix4x4[bones.Count];
                for (int i = 0; i < bones.Count; i++) bind[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
                mesh.bindposes = bind;
                mesh.name = "DinoRig_Body";
                mesh = Keep(mesh, RigDir + "/DinoRig_Body.asset");

                var (skin, app) = SlugSkin();
                skin = KeepMat(skin, RigDir + "/DinoRig_Skin.mat");
                app = KeepMat(app, RigDir + "/DinoRig_SkinAntenna.mat");
                var dark = KeepMat(new Material(Shader.Find("Universal Render Pipeline/Lit")), RigDir + "/DinoRig_Eye.mat");
                dark.SetColor("_BaseColor", new Color(0.06f, 0.06f, 0.06f));
                dark.SetFloat("_Smoothness", 0.1f);

                var bodyGo = new GameObject("Body");
                bodyGo.transform.SetParent(root.transform, false);
                var smr = bodyGo.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh;
                smr.bones = bones.ToArray();
                smr.rootBone = sp[0];
                smr.sharedMaterial = skin;
                smr.updateWhenOffscreen = false;
                smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 1.6f);

                // ---- eyes, mouth, antennae: made in place, then carried by the head
                PartDir = RigDir; PartPrefix = "DinoRig_";
                var faceRoot = new GameObject("Face");
                var antennas = FaceParts(faceRoot, dark, app);
                foreach (var a in antennas) a.GetComponent<MeshRenderer>().sharedMaterial = app;
                var kids = new List<Transform>();
                foreach (Transform c in faceRoot.transform) kids.Add(c);
                foreach (var c in kids) c.SetParent(sp[4], true);
                Object.DestroyImmediate(faceRoot);

                // the eye anchor: between the eyes on the face, facing forward
                float eyeY = HP(1077, 507).y;
                var eye = Bone("EyeAnchor", sp[4], Surface(new Vector3(0f, eyeY, 0.4f), Vector3.back), Quaternion.identity);

                var body = root.AddComponent<DinoSlugBody>();
                body.hips = sp[0]; body.spine = sp[1]; body.chest = sp[2]; body.neck = sp[3]; body.head = sp[4];
                body.tail = tail; body.thighs = thighs; body.shins = shins; body.feet = feet; body.arms = arms;
                body.eyeAnchor = eye;

                Directory.CreateDirectory(Path.GetDirectoryName(RigPrefab));
                PrefabUtility.SaveAsPrefabAsset(root, RigPrefab);
                Debug.Log($"[DinoSlugRig] saved {RigPrefab}: {mesh.vertexCount} vertices, {bones.Count} bones");
            }
            finally
            {
                PartDir = MeshDir; PartPrefix = "DinoPhoto_";
                Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>A point on the body's middle line at photo x (the spine line, held straight past its ends).</summary>
        static Vector3 SpineAt(float px)
        {
            int n = SpineKeys.Length;
            if (px >= SpineKeys[n - 1].x) return P(px, SpineKeys[n - 1].y + (px - SpineKeys[n - 1].x) * (SpineKeys[n - 1].y - SpineKeys[n - 2].y) / (SpineKeys[n - 1].x - SpineKeys[n - 2].x));
            for (int i = 0; i < n - 1; i++)
                if (px <= SpineKeys[i + 1].x)
                    return P(px, Mathf.Lerp(SpineKeys[i].y, SpineKeys[i + 1].y, Mathf.InverseLerp(SpineKeys[i].x, SpineKeys[i + 1].x, px)));
            return P(px, SpineKeys[n - 1].y);
        }

        /// <summary>A rotation whose +Y runs along dir (the leg bones stretch along +Y), +Z as near forward as it can.</summary>
        static Quaternion Along(Vector3 dir)
        {
            dir.Normalize();
            Vector3 f = Vector3.ProjectOnPlane(Vector3.forward, dir);
            if (f.sqrMagnitude < 1e-6f) f = Vector3.ProjectOnPlane(Vector3.right, dir);
            return Quaternion.LookRotation(f.normalized, dir);
        }

        // ------------------------------------------------------------------ weights
        static void Weigh(Mesh mesh, List<Transform> bones, Transform[] sp, Transform[] tail, Transform[] thighs, Transform[] shins,
                          Transform[] feet, Transform[] arms, Transform[] hands)
        {
            // the middle line from the tail tip to over the head, and which bone owns each piece of it
            // (a piece turns with the bone that starts nearest the hips beyond it: tip..Tail4 with Tail4, Tail4..Tail3 with
            // Tail3, ..., Tail0..Hips and Hips..Spine with the hips, Spine..Chest with the spine, ..., past the head with it)
            var nodes = new List<Vector3> { SpineAt(512f) };
            var owners = new List<Transform> { tail[tail.Length - 1] };
            for (int i = tail.Length - 1; i >= 0; i--) { nodes.Add(SpineAt(TailPx[i])); owners.Add(i > 0 ? tail[i - 1] : sp[0]); }
            for (int i = 0; i < SpinePx.Length; i++) { nodes.Add(SpineAt(SpinePx[i])); if (i < SpinePx.Length - 1) owners.Add(sp[i]); }
            nodes.Add(SpineAt(1060f)); owners.Add(sp[SpinePx.Length - 1]);
            var cum = new float[nodes.Count];
            for (int i = 1; i < nodes.Count; i++) cum[i] = cum[i - 1] + Vector2.Distance(YZ(nodes[i]), YZ(nodes[i - 1]));
            const float blend = 0.03f;

            var index = new Dictionary<Transform, int>();
            for (int i = 0; i < bones.Count; i++) index[bones[i]] = i;
            var verts = mesh.vertices;
            var weights = new BoneWeight[verts.Length];
            var acc = new Dictionary<int, float>();
            for (int v = 0; v < verts.Length; v++)
            {
                acc.Clear();
                Vector3 p = verts[v];
                int side = p.x < 0f ? 0 : 1;
                Vector3 m = new Vector3(Mathf.Abs(p.x), p.y, p.z);
                float dc = Core(p), dt = Thigh(m), dl = Leg(m), da = Arm(m);
                float dmin = Mathf.Min(Mathf.Min(dc, dt), Mathf.Min(dl, da));
                float wc = Mathf.Exp(-(dc - dmin) / 0.01f);
                float wt = Mathf.Exp(-(dt - dmin) / 0.012f);
                float wl = Mathf.Exp(-(dl - dmin) / 0.005f);
                float wa = Mathf.Exp(-(da - dmin) / 0.008f);
                float sum = wc + wt + wl + wa;
                wc /= sum; wt /= sum; wl /= sum; wa /= sum;
                // the leg comes out of the body (Nov 2 わくさん：体の根本から足を動かす): the low belly round the top of the
                // thigh goes with the thigh too, most at the thigh, none 7.5 cm away or above the hip root
                float near = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.075f, 0.025f, SegDist(m, HipRoot, KneeJ)))
                           * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HipRoot.y + 0.02f, HipRoot.y - 0.03f, p.y));
                float moved = wc * ThighShare * near;
                wc -= moved; wt += moved;

                void Add(Transform b, float w) { if (w <= 1e-4f) return; int k = index[b]; acc[k] = (acc.TryGetValue(k, out var x) ? x : 0f) + w; }

                // head-body-tail: along the middle line
                if (wc > 1e-4f)
                {
                    Vector2 q = YZ(p);
                    float best = float.MaxValue, s = 0f;
                    for (int i = 0; i < nodes.Count - 1; i++)
                    {
                        Vector2 a = YZ(nodes[i]), ab = YZ(nodes[i + 1]) - a;
                        float f = Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                        float dd = (q - (a + ab * f)).sqrMagnitude;
                        if (dd < best) { best = dd; s = cum[i] + ab.magnitude * f; }
                    }
                    float tot = 0f;
                    var piece = new float[owners.Count];
                    for (int i = 0; i < owners.Count; i++)
                    {
                        float lo = i == 0 ? float.NegativeInfinity : cum[i], hi = i == owners.Count - 1 ? float.PositiveInfinity : cum[i + 1];
                        piece[i] = Mathf.Clamp01(Mathf.Min((s - lo) / blend + 0.5f, (hi - s) / blend + 0.5f));
                        tot += piece[i];
                    }
                    for (int i = 0; i < owners.Count; i++) Add(owners[i], wc * piece[i] / Mathf.Max(tot, 1e-5f));
                }
                Add(thighs[side], wt);
                // knee + shin + foot: the foot takes what is under the ankle
                float foot = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.03f, 0.015f, p.y));
                Add(shins[side], wl * (1f - foot));
                Add(feet[side], wl * foot);
                // arm: the hand ball goes with the hand
                float hand = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Hand.y + 0.03f, Hand.y + 0.005f, p.y));
                Add(arms[side], wa * (1f - hand));
                Add(hands[side], wa * hand);

                // the four biggest, normalised
                var list = new List<KeyValuePair<int, float>>(acc);
                list.Sort((x, y) => y.Value.CompareTo(x.Value));
                float total = 0f;
                for (int i = 0; i < Mathf.Min(4, list.Count); i++) total += list[i].Value;
                var bw = new BoneWeight();
                for (int i = 0; i < Mathf.Min(4, list.Count); i++)
                {
                    float w = list[i].Value / total;
                    switch (i)
                    {
                        case 0: bw.boneIndex0 = list[i].Key; bw.weight0 = w; break;
                        case 1: bw.boneIndex1 = list[i].Key; bw.weight1 = w; break;
                        case 2: bw.boneIndex2 = list[i].Key; bw.weight2 = w; break;
                        case 3: bw.boneIndex3 = list[i].Key; bw.weight3 = w; break;
                    }
                }
                weights[v] = bw;
            }
            mesh.boneWeights = weights;
        }

        static Vector2 YZ(Vector3 v) => new Vector2(v.y, v.z);

        // ------------------------------------------------------------------ assets kept under the same file (same GUID)
        static Mesh Keep(Mesh mesh, string path)
        {
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            // (CopySerialized drops the vertices of a mesh made in code: copy each part)
            old.Clear();
            old.indexFormat = mesh.indexFormat;
            old.vertices = mesh.vertices;
            old.normals = mesh.normals;
            old.uv = mesh.uv;
            old.boneWeights = mesh.boneWeights;
            old.bindposes = mesh.bindposes;
            old.triangles = mesh.triangles;
            old.RecalculateBounds();
            EditorUtility.SetDirty(old);
            return old;
        }

        static Material KeepMat(Material mat, string path)
        {
            var old = AssetDatabase.LoadAssetAtPath<Material>(path);
            var copy = new Material(mat) { name = Path.GetFileNameWithoutExtension(path) };
            if (old == null) { AssetDatabase.CreateAsset(copy, path); return copy; }
            old.shader = copy.shader;
            old.CopyPropertiesFromMaterial(copy);
            EditorUtility.SetDirty(old);
            return old;
        }
    }
}
