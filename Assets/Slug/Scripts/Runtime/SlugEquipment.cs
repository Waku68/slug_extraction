using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// Gear worn by a slug, built from code to fit its body and drawn with the slug's own toon shader, so it crawls,
    /// stands up and turns with it:
    ///  - helmet: a round steel pot with a short brim, longer over the eyes; the tentacles come out through two
    ///    grommeted holes (they hear well, but can be shot). It rides the head rigidly.
    ///  - shell plate armour: a domed oval of shell over the mantle (the hump behind the head, where real slugs keep the
    ///    remnant of their ancestors' shell), a chest plate in front and straps between them above the arms. It is cut
    ///    from the skin itself, so it bends exactly like the body.
    ///  - snail shell: a coiled shell on the back, its opening turned in against the body as a snail's rests on its
    ///    foot. The shell is what marks a snail (the scavs); a slug carries a bag.
    ///  - bag: a canvas backpack with a flap and a pocket, belted round the body above the arms. It sits on the armour
    ///    when both are worn. It rides the back rigidly.
    /// The armour wears the camouflage (a skin: the owner's look, whatever armour it is), woodland by default.
    /// </summary>
    [ExecuteAlways]
    public class SlugEquipment : MonoBehaviour
    {
        public enum Armor { None, ShellPlate, SnailShell }

        public SlugPop slug;
        [Tooltip("触角を出すヘルメット")] public bool helmet = true;
        [Tooltip("胴の防具（カタツムリの殻はカタツムリ用）")] public Armor armor = Armor.ShellPlate;
        [Tooltip("背中のバッグ（ナメクジ用）")] public bool bag;

        [Header("ヘルメット")]
        [Tooltip("鉄の色")] public Color helmetColor = new Color(0.3f, 0.32f, 0.24f);
        [Tooltip("ヘルメットにも迷彩をかける")] public bool helmetCamo;
        [Tooltip("頭とのすき間（m）")] public float helmetGap = 0.016f;
        [Tooltip("つばの長さ：目の上（m）と、ほかのところ（m）")] public float brimFront = 0.035f;
        public float brimSide = 0.014f;

        [Header("迷彩（スキン）")]
        [Tooltip("地の色（枯れ草）。体の苔色から浮くように、明るめにする")] public Color camoBase = new Color(0.62f, 0.57f, 0.38f);
        [Tooltip("ぶちの色 1（茶）")] public Color camo1 = new Color(0.43f, 0.28f, 0.15f);
        [Tooltip("ぶちの色 2（こげ茶）")] public Color camo2 = new Color(0.15f, 0.13f, 0.1f);
        [Tooltip("ぶちの色 3（苔）")] public Color camo3 = new Color(0.31f, 0.38f, 0.19f);
        [Tooltip("ぶちの細かさ")] public float camoScale = 19f;
        [Tooltip("ベルトの色")] public Color strapColor = new Color(0.22f, 0.16f, 0.11f);

        [Header("バッグ")]
        [Tooltip("布の色")] public Color bagColor = new Color(0.52f, 0.4f, 0.26f);
        [Tooltip("ふたとポケットの色")] public Color bagFlapColor = new Color(0.36f, 0.26f, 0.17f);
        [Tooltip("大きさ（幅、高さ、厚み の半分, m）")] public Vector3 bagSize = new Vector3(0.11f, 0.12f, 0.065f);
        /// <summary>How swollen the bag is with what is in it (1 = empty; set at run time by the bag, then <see cref="Build"/>).
        /// It grows out from the back most, taller a little less, wider least.</summary>
        [System.NonSerialized] public float bagSwell = 1f;
        [Tooltip("背中を覗いたとき、自分の画面だけでバッグを大きくする倍率")] public float bagLookScale = 1.6f;
        [Range(0f, 1f), Tooltip("背中を覗いたとき、自分の画面だけでバッグの口を目のほうへ傾ける割合（1 = 真っすぐ目に向ける）")] public float bagLookTilt = 0.45f;
        [Tooltip("ふたを開けきったときの角度（度）。ふたは背中から遠いほうのふちを軸に、外側へ開く")] public float bagLidAngle = 140f;
        [Range(0f, 1f), Tooltip("背中を覗いたとき、自分の画面でふたも大きくする割合（0 = 大きくしない、1 = バッグと同じだけ）")] public float bagLidGrow = 0.3f;

        readonly List<Object> _owned = new List<Object>();
        readonly List<Renderer> _helmetRenderers = new List<Renderer>();
        bool _pending;
        Camera _ownCamera;
        System.Func<bool> _ownCameraInside;

        /// <summary>Called after the gear is (re)built, so things fitted to it (hitboxes) can follow.</summary>
        public event System.Action Built;

        // what is on the back, for hitboxes (body space)
        public bool HasBackItem { get; private set; }
        public Vector3 BackItemCenter { get; private set; }
        public Vector3 BackItemNormal { get; private set; }
        public Quaternion BackItemRotation { get; private set; }
        public Vector3 BackItemHalf { get; private set; }
        public float BackItemT { get; private set; }
        /// <summary>The bag's mouth after the last <see cref="PoseBag"/> (world): its middle, its turn (x across, y out of
        /// the mouth toward the head, z out from the back) and how much the bag was grown.</summary>
        public Vector3 BagMouthPosition { get; private set; }
        public Quaternion BagMouthRotation { get; private set; }
        public float BagPoseScale { get; private set; } = 1f;

        // the bag's parts and where it sits (body space), for posing it
        GameObject _bagGo, _bagLid, _bagPocket, _bagMouth;
        Vector3 _bagP, _bagN, _bagC, _bagX, _bagUp, _bagOut, _bagSz, _bagHinge;
        float _bagT;

        /// <summary>
        /// The owner's own camera: while it sits inside the head (first person), the helmet only casts its shadow for
        /// that camera, so its brim never fills the view; every other camera sees it as usual.
        /// </summary>
        public void SetOwnCamera(Camera cam, System.Func<bool> inside)
        {
            _ownCamera = cam;
            _ownCameraInside = inside;
        }

        void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera cam)
        {
            bool hide = cam == _ownCamera && _ownCameraInside != null && _ownCameraInside();
            var mode = hide ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var r in _helmetRenderers) if (r) r.shadowCastingMode = mode;
        }

        void OnEnable()
        {
            if (!slug) slug = GetComponent<SlugPop>();
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            if (!slug) return;
            slug.Rebuilt += Build;
            Build();
        }

        void OnDisable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            if (slug) slug.Rebuilt -= Build;
            Clear();
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            if (_pending) return;
            _pending = true;
            UnityEditor.EditorApplication.delayCall += () => { _pending = false; if (this != null && isActiveAndEnabled) Build(); };
#endif
        }

        void Clear()
        {
            foreach (var o in _owned)
            {
                if (!o) continue;
                if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
            }
            _owned.Clear();
            _helmetRenderers.Clear();
            HasBackItem = false;
        }

        T Own<T>(T o) where T : Object { o.hideFlags = HideFlags.DontSave; _owned.Add(o); return o; }

        /// <summary>
        /// Poses the bag for the camera about to draw: the lid opened by <paramref name="open"/> (0..1) about its hinge
        /// (the edge away from the back, falling open outward), and by <paramref name="look"/> (0..1) the whole bag grown out from the back and tipped with
        /// its mouth toward <paramref name="eyeWorld"/> (for the owner's own eyes only: call it again with 0 for the
        /// others). Returns false when there is no bag.
        /// </summary>
        public bool PoseBag(float open, float look, Vector3 eyeWorld)
        {
            if (!_bagGo || !slug || slug.VisualRoot == null) return false;
            var vis = slug.VisualRoot;
            Vector3 attach = slug.FollowerLocal(_bagP, _bagT, _bagP, _bagN);
            Quaternion bend = slug.FollowerRotation(_bagT);
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            float s = 1f;
            if (look > 0f)
            {
                // grown about where it sits on the back (so it does not sink into the body), the mouth tipped to the eyes;
                // a swollen bag is grown less (it is already big), so it still fits in the view
                float swollen = _bagSz.y / Mathf.Max(bagSize.y, 1e-4f);
                s = Mathf.Lerp(1f, bagLookScale / Mathf.Pow(swollen, 0.75f), look);
                Vector3 mouthDir = bend * _bagUp;
                Vector3 toEye = vis.InverseTransformPoint(eyeWorld) - attach;
                rot = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(mouthDir, toEye.normalized), bagLookTilt * look);
                pos = attach - rot * (s * attach);
            }
            Place(_bagGo, pos, rot, s);
            Place(_bagPocket, pos, rot, s);
            Place(_bagMouth, pos, rot, s);
            var mouthR = _bagMouth ? _bagMouth.GetComponent<Renderer>() : null;
            if (mouthR && mouthR.enabled != open > 0.02f) mouthR.enabled = open > 0.02f;

            // the lid swings up about its hinge (its top edge away from the back) and falls open outward
            Vector3 hinge = slug.FollowerLocal(_bagHinge, _bagT, _bagP, _bagN);
            Quaternion swing = Quaternion.AngleAxis(bagLidAngle * open, bend * _bagX);
            // (in the own view the lid grows less than the bag, so it does not stand up across the mouth)
            float lid = Mathf.Lerp(1f, s, bagLidGrow) / s;
            Place(_bagLid, pos + rot * (s * (hinge - lid * (swing * hinge))), rot * swing, s * lid);

            Vector3 mouth = slug.FollowerLocal(_bagC + _bagUp * _bagSz.y, _bagT, _bagP, _bagN);
            BagMouthPosition = vis.TransformPoint(pos + rot * (s * mouth));
            BagMouthRotation = vis.rotation * rot * bend * Quaternion.LookRotation(_bagOut, _bagUp);
            BagPoseScale = s;
            return true;
        }

        static void Place(GameObject go, Vector3 pos, Quaternion rot, float s)
        {
            if (!go) return;
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = Vector3.one * s;
        }

        /// <summary>Puts the gear (back) on. Called on enable, on every slug rebuild and after a change in the inspector.</summary>
        public void Build()
        {
            Clear();
            if (!slug || slug.VisualRoot == null || slug.Shape == null) return;
            if (helmet) BuildHelmet();
            if (armor == Armor.ShellPlate) BuildShellPlate();
            else if (armor == Armor.SnailShell) BuildSnailShell();
            if (bag) BuildBag();
            Built?.Invoke();
        }

        Material Camo(Material m)
        {
            m.SetFloat("_CamoAmount", 1f);
            m.SetColor("_CamoColor1", camo1);
            m.SetColor("_CamoColor2", camo2);
            m.SetColor("_CamoColor3", camo3);
            m.SetFloat("_CamoScale", camoScale);
            return m;
        }

        void Add(string name, Mesh mesh, Material mat, bool helmetPart = false)
        {
            var go = AddGo(name, mesh, mat);
            if (go && helmetPart) _helmetRenderers.Add(go.GetComponent<Renderer>());
        }

        GameObject AddGo(string name, Mesh mesh, Material mat)
        {
            var go = slug.AddGear(name, mesh, mat, Vector3.zero);
            if (go) _owned.Add(go);
            return go;
        }

        // ------------------------------------------------------------------ helmet
        void BuildHelmet()
        {
            var shape = slug.Shape;
            // a dome over the rounded end of the head: from where the head is full width up past its top
            var baseRing = shape.FrameAt(0.8f);
            Vector3 c = baseRing.center;
            if (!shape.RaycastT(c, shape.FrameAt(0.9f).forward, out var top, out _, out float topT)) return;
            Vector3 axis = (top - c).normalized;
            Vector3 right = Vector3.ProjectOnPlane(baseRing.right, axis).normalized;
            Vector3 back = Vector3.Cross(axis, right).normalized;            // toward the back of the head; the face is -back
            if (Vector3.Dot(back, baseRing.up) < 0f) back = -back;
            float rx = baseRing.rx + helmetGap, ry = baseRing.ry + helmetGap, rz = Vector3.Distance(c, top) + helmetGap;
            const float thick = 0.008f;

            // how far down the dome comes at each side: low over the nape, high over the eyes
            float Reach(float phi)
            {
                float s = Mathf.Sin(phi);
                return (80f - 44f * Mathf.Max(0f, -s) + 2f * Mathf.Max(0f, s)) * Mathf.Deg2Rad;
            }
            Vector3 Dome(float theta, float phi, float grow)
                => c + axis * ((rz + grow) * Mathf.Cos(theta))
                     + (right * ((rx + grow) * Mathf.Cos(phi)) + back * ((ry + grow) * Mathf.Sin(phi))) * Mathf.Sin(theta);

            const int nT = 10, nP = 28;
            var mb = new GearMesh();
            var outer = new int[nT + 1, nP];
            var inner = new int[nT + 1, nP];
            for (int i = 0; i <= nT; i++)
            for (int j = 0; j < nP; j++)
            {
                float phi = j / (float)nP * Mathf.PI * 2f, theta = Reach(phi) * i / nT;
                outer[i, j] = mb.Vert(Dome(theta, phi, 0f));
                inner[i, j] = mb.Vert(Dome(theta, phi, -thick));
            }
            for (int i = 0; i < nT; i++)
            for (int j = 0; j < nP; j++)
            {
                int k = (j + 1) % nP;
                mb.Quad(outer[i, j], outer[i + 1, j], outer[i + 1, k], outer[i, k], c, true);
                mb.Quad(inner[i, j], inner[i + 1, j], inner[i + 1, k], inner[i, k], c, false);
            }
            // the brim: flares out from the rim and a little down, longest over the eyes
            var brimIn = new int[nP]; var brimOut = new int[nP]; var brimOutLow = new int[nP]; var rimIn = new int[nP];
            for (int j = 0; j < nP; j++)
            {
                float phi = j / (float)nP * Mathf.PI * 2f, theta = Reach(phi);
                Vector3 rim = Dome(theta, phi, 0f);
                Vector3 radial = (right * Mathf.Cos(phi) + back * Mathf.Sin(phi)).normalized;
                float len = Mathf.Lerp(brimSide, brimFront, Mathf.Max(0f, -Mathf.Sin(phi)));
                Vector3 edge = rim + (radial * 0.94f - axis * 0.34f).normalized * len;
                brimIn[j] = mb.Vert(rim);
                brimOut[j] = mb.Vert(edge);
                brimOutLow[j] = mb.Vert(edge - axis * thick);
                rimIn[j] = mb.Vert(Dome(theta, phi, -thick));
            }
            for (int j = 0; j < nP; j++)
            {
                int k = (j + 1) % nP;
                Vector3 hint = c + axis * rz * 2f;
                mb.Quad(brimIn[j], brimOut[j], brimOut[k], brimIn[k], c - axis * rz, true);             // top of the brim
                mb.Quad(brimOut[j], brimOutLow[j], brimOutLow[k], brimOut[k], c, true);                 // its edge
                mb.Quad(brimOutLow[j], rimIn[j], rimIn[k], brimOutLow[k], hint, true);                  // underneath
            }
            var mat = Own(slug.GearMaterial(helmetColor));
            mat.SetFloat("_RimAmount", 0.25f);
            if (helmetCamo) Camo(mat);
            var follower = slug.GearFollower(mat, Mathf.Min(topT, 0.97f), top, axis);
            Add("Helmet", Own(mb.ToMesh("Helmet")), follower, true);

            // grommets where the tentacles come out through the steel
            var steel = new GearMesh();
            foreach (float side in new[] { -1f, 1f })
            {
                slug.EarRoot(side, out var root, out var earAxis, out float earR, out float earLen);
                // walk up the tentacle until it leaves the dome
                float s = 0f;
                for (; s < earLen; s += 0.002f)
                {
                    Vector3 q = root + earAxis * s - c;
                    float x = Vector3.Dot(q, right) / rx, y = Vector3.Dot(q, back) / ry, z = Vector3.Dot(q, axis) / rz;
                    if (x * x + y * y + z * z > 1f) break;
                }
                float r = earR * (1f - 0.85f * s / Mathf.Max(earLen, 1e-3f)) + 0.003f;
                steel.Torus(root + earAxis * s, earAxis, r + 0.004f, 0.0045f, 14, 6);
            }
            var ringMat = Own(slug.GearMaterial(Color.Lerp(helmetColor, Color.black, 0.45f)));
            Add("HelmetGrommets", Own(steel.ToMesh("HelmetGrommets")), slug.GearFollower(ringMat, Mathf.Min(topT, 0.97f), top, axis), true);
        }

        // ------------------------------------------------------------------ shell plate armour (cut from the skin)
        /// <summary>The skin at (t, angle round the body: 0 = the middle of the back, 180 = the belly).</summary>
        bool Skin(float t, float angle, out Vector3 p, out Vector3 n, out float tHit)
        {
            var shape = slug.Shape;
            var f = shape.FrameAt(t);
            float a = angle * Mathf.Deg2Rad;
            Vector3 dir = (f.up * Mathf.Cos(a) + f.right * Mathf.Sin(a)).normalized;
            if (shape.RaycastT(f.center, dir, out p, out n, out tHit)) return true;
            p = f.center; n = dir; tHit = t;
            return false;
        }

        /// <summary>
        /// A plate following the skin: rows from t0 to t1, each spanning ±<paramref name="halfSpan"/>(t) degrees about
        /// <paramref name="centerAngle"/>, standing <paramref name="gap"/> off the skin and <paramref name="thick"/> thick.
        /// Its vertices carry the skin's own crawl data, so the body shader bends it with the body.
        /// </summary>
        void Plate(GearMesh mb, float t0, float t1, float centerAngle, System.Func<float, float> halfSpan, float gap, float thick, int nT = 8, int nA = 16, float dome = 0f)
        {
            var shape = slug.Shape;
            var outer = new int[nT + 1, nA + 1];
            var inner = new int[nT + 1, nA + 1];
            for (int i = 0; i <= nT; i++)
            {
                float u = i / (float)nT, t = Mathf.Lerp(t0, t1, u), span = halfSpan(u);
                for (int j = 0; j <= nA; j++)
                {
                    float ang = centerAngle + Mathf.Lerp(-span, span, j / (float)nA);
                    Skin(t, ang, out var p, out var n, out float th);
                    shape.RingInfo(Mathf.Clamp01(th), out var pivot, out float rho);
                    var uv = new Vector2(Mathf.Repeat(ang / 360f, 1f), th);
                    // the rim is a little thinner, so the plate reads as a rounded shell, not a slab
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u) * nT, Mathf.Min(j, nA - j) * nT / (float)nA);
                    float tk = thick * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(edge));
                    // a shield bulges in the middle
                    float du = u * 2f - 1f, dv = j / (float)nA * 2f - 1f;
                    tk += dome * Mathf.Clamp01(1f - du * du - dv * dv);
                    outer[i, j] = mb.Vert(p + n * (gap + tk), uv, rho, pivot);
                    inner[i, j] = mb.Vert(p + n * gap, uv, rho, pivot);
                }
            }
            Vector3 Center(int i) => shape.FrameAt(Mathf.Lerp(t0, t1, i / (float)nT)).center;
            for (int i = 0; i < nT; i++)
            for (int j = 0; j < nA; j++)
            {
                Vector3 ctr = Center(i);
                mb.Quad(outer[i, j], outer[i + 1, j], outer[i + 1, j + 1], outer[i, j + 1], ctr, true);
                mb.Quad(inner[i, j], inner[i + 1, j], inner[i + 1, j + 1], inner[i, j + 1], ctr, false);
            }
            // the rim all round: separate vertices, so the edge is crisp; it faces away from the middle of the plate
            Vector3 mid = Vector3.zero;
            foreach (int v in outer) mid += mb.Pos(v);
            mid /= outer.Length;
            void Edge(int a0, int a1, int b0, int b1)
                => mb.Quad(mb.Copy(outer[a0, a1]), mb.Copy(outer[b0, b1]), mb.Copy(inner[b0, b1]), mb.Copy(inner[a0, a1]), mid, true);
            for (int i = 0; i < nT; i++) { Edge(i, 0, i + 1, 0); Edge(i, nA, i + 1, nA); }
            for (int j = 0; j < nA; j++) { Edge(0, j, 0, j + 1); Edge(nT, j, nT, j + 1); }
        }

        const float ShieldFrom = 0.45f, ShieldTo = 0.745f, ShieldStandOff = 0.05f;   // how far the shield stands off the skin at most

        void BuildShellPlate()
        {
            var shells = new GearMesh();
            // the mantle shield: a big domed oval over the back, where a slug keeps the remnant of its ancestors' shell.
            // It ends below the helmet's rim, so the two never run into each other.
            System.Func<float, float> oval = u => 54f + 46f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(u * 2f - 1f, 2f)));
            Plate(shells, ShieldFrom, ShieldTo, 0f, oval, 0.012f, 0.016f, 16, 24, 0.022f);
            // the chest plate in front, below the face
            System.Func<float, float> chest = u => 38f + 18f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(u * 2f - 1f, 2f)));
            Plate(shells, 0.44f, 0.77f, 180f, chest, 0.008f, 0.012f, 12, 14, 0.008f);
            var mat = Camo(Own(slug.GearMaterial(camoBase)));
            mat.SetFloat("_RimAmount", 0.2f);
            Add("ShellPlate", Own(shells.ToMesh("ShellPlate")), mat);

            // straps across the sides, above where the arms come out, holding the front and back together
            var straps = new GearMesh();
            foreach (float side in new[] { -1f, 1f })
                Plate(straps, 0.72f, 0.75f, side * 105f, u => 32f, 0.004f, 0.006f, 2, 6);
            var strapMat = Own(slug.GearMaterial(strapColor));
            Add("ShellPlateStraps", Own(straps.ToMesh("ShellPlateStraps")), strapMat);
        }

        // ------------------------------------------------------------------ snail shell (the snails' mark)
        void BuildSnailShell()
        {
            const float t = 0.5f, rMax = 0.11f, turns = 3.2f, tubeRatio = 0.85f;
            Skin(t, 0f, out var p, out var nrm, out float th);
            var f = slug.Shape.FrameAt(t);
            Vector3 x = f.right, nOut = Vector3.ProjectOnPlane(nrm, x).normalized;
            Vector3 up = Vector3.ProjectOnPlane(f.forward, nOut).normalized;          // up the back, toward the head
            // The coil turns so that its last stretch runs in against the back: the opening faces into the body and a
            // little toward the head (where a snail's body comes out), and the coil sits up and out from it.
            const float thEnd = 0.75f * Mathf.PI + 2f * Mathf.PI * turns, thStart = 0.75f * Mathf.PI;
            float k = Mathf.Log(2.2f) / (2f * Mathf.PI);
            Vector3 Radial(float theta) => nOut * Mathf.Cos(theta) - up * Mathf.Sin(theta);
            Vector3 end = p + nOut * (rMax * tubeRatio * 0.3f);
            Vector3 o = end - Radial(thEnd) * rMax;
            const int steps = 150, ring = 12;
            var mb = new GearMesh();
            var rows = new List<int[]>();
            Vector3 Center(float theta, out Vector3 radial, out float tube)
            {
                float r = rMax * Mathf.Exp(k * (theta - thEnd));
                tube = r * tubeRatio;
                radial = Radial(theta);
                return o + radial * r + x * ((rMax - r) * 1.3f);                 // the spire stands out to the right
            }
            for (int i = 0; i <= steps; i++)
            {
                float theta = Mathf.Lerp(thStart, thEnd, i / (float)steps);
                Vector3 cc = Center(theta, out var radial, out float tube);
                var row = new int[ring];
                for (int j = 0; j < ring; j++)
                {
                    float b = j / (float)ring * Mathf.PI * 2f;
                    row[j] = mb.Vert(cc + (radial * Mathf.Cos(b) + x * Mathf.Sin(b)) * tube);
                }
                rows.Add(row);
            }
            for (int i = 0; i < steps; i++)
            for (int j = 0; j < ring; j++)
            {
                int k2 = (j + 1) % ring;
                float theta = Mathf.Lerp(thStart, thEnd, (i + 0.5f) / steps);
                Vector3 axisPt = Center(theta, out _, out _);
                mb.Quad(rows[i][j], rows[i + 1][j], rows[i + 1][k2], rows[i][k2], axisPt, true);
            }
            var mat = Camo(Own(slug.GearMaterial(camoBase)));
            mat.SetFloat("_RimAmount", 0.2f);
            Add("SnailShell", Own(mb.ToMesh("SnailShell")), slug.GearFollower(mat, th, p, nrm));
            SetBackItem(o + x * (rMax * 0.3f), nrm, th, Quaternion.LookRotation(nOut, up), new Vector3(rMax * 1.2f, rMax * 1.55f, rMax * 1.55f));

            // the pale lip round the opening, pressed against the back
            var lip = new GearMesh();
            Vector3 lipC = Center(thEnd, out var endRadial, out float endTube);
            Vector3 along = Vector3.Cross(endRadial, x).normalized;                   // the way the opening faces
            lip.Torus(lipC, along, endTube * 0.98f, 0.012f, 18, 6);
            Add("SnailShellLip", Own(lip.ToMesh("SnailShellLip")), slug.GearFollower(Own(slug.GearMaterial(new Color(0.86f, 0.8f, 0.66f))), th, p, nrm));
        }

        // ------------------------------------------------------------------ bag (the slugs' mark)
        void BuildBag()
        {
            const float t = 0.49f;
            Skin(t, 0f, out var p, out var nrm, out float th);
            var f = slug.Shape.FrameAt(t);
            Vector3 nOut = nrm.normalized;
            Vector3 up = Vector3.ProjectOnPlane(f.forward, nOut).normalized;
            Vector3 x = Vector3.Cross(up, nOut).normalized;
            float standOff = armor == Armor.ShellPlate ? ShieldStandOff : 0.008f;   // on top of the shield when worn
            Vector3 sz = SwollenSize(bagSize, bagSwell);
            Vector3 c = p + nOut * (standOff + sz.z);      // it stands on the back and piles up away from it

            var body = new GearMesh();
            body.RoundedBox(c, x, up, nOut, sz, SwollenRoundness(bagSwell));
            // the lid over the top (the end toward the head), a little bigger than the bag, hinged on its edge away
            // from the back; a pocket low on the outside; and the dark mouth under the lid
            Vector3 lidHalf = LidHalf(bagSize);              // (the lid and the mouth keep their size however full the bag is)
            var lid = new GearMesh();
            lid.RoundedBox(c + up * (sz.y * 0.79f) + nOut * 0.004f, x, up, nOut, lidHalf, 0.42f);
            var pocket = new GearMesh();
            pocket.RoundedBox(c + nOut * (sz.z * 0.92f) - up * (sz.y * 0.38f), x, up, nOut, new Vector3(sz.x * 0.58f, sz.y * 0.32f, 0.02f), 0.45f);
            var mouth = new GearMesh();
            mouth.RoundedBox(c + up * (sz.y * 0.97f), x, up, nOut, MouthHalf(bagSize), 0.5f);
            _bagGo = AddGo("Bag", Own(body.ToMesh("Bag")), slug.GearFollower(Own(slug.GearMaterial(bagColor)), th, p, nrm));
            SetBackItem(c, nrm, th, Quaternion.LookRotation(nOut, up), new Vector3(sz.x, sz.y, sz.z) * 1.05f);
            Material flapMat = Own(slug.GearMaterial(bagFlapColor));
            _bagLid = AddGo("BagLid", Own(lid.ToMesh("BagLid")), slug.GearFollower(flapMat, th, p, nrm));
            _bagPocket = AddGo("BagPocket", Own(pocket.ToMesh("BagPocket")), slug.GearFollower(flapMat, th, p, nrm));
            _bagMouth = AddGo("BagMouth", Own(mouth.ToMesh("BagMouth")), slug.GearFollower(Own(slug.GearMaterial(new Color(0.07f, 0.055f, 0.045f))), th, p, nrm));
            if (_bagMouth) _bagMouth.GetComponent<Renderer>().enabled = false;
            _bagP = p; _bagN = nrm; _bagT = th; _bagC = c; _bagX = x; _bagUp = up; _bagOut = nOut; _bagSz = sz;
            _bagHinge = c + up * (sz.y * 0.79f + lidHalf.y * 0.92f) + nOut * (lidHalf.z + 0.004f);

            // belts round the body, above the arms (and over the armour when worn)
            var belts = new GearMesh();
            float gap = armor == Armor.ShellPlate ? ShieldStandOff - 0.012f : 0.004f;
            Plate(belts, 0.66f, 0.685f, 0f, u => 180f, gap, 0.006f, 2, 36);
            Plate(belts, 0.41f, 0.435f, 0f, u => 180f, gap, 0.006f, 2, 36);
            Add("BagBelts", Own(belts.ToMesh("BagBelts")), Own(slug.GearMaterial(strapColor)));
        }

        /// <summary>
        /// The bag's half size swollen by its contents (1 = empty): a full one spreads wide across the back and piles up away
        /// from it (up, for a crawling slug), growing only a little along the body, so it sits on the back like a huge sack
        /// rather than swallowing the slug.
        /// </summary>
        public static Vector3 SwollenSize(Vector3 bagSize, float swell)
        {
            float sw = Mathf.Max(0.5f, swell) - 1f;
            return Vector3.Scale(bagSize, new Vector3(1f + 0.7f * sw, 1f + 0.35f * sw, 1f + 0.55f * sw));
        }

        /// <summary>The fuller, the rounder (a crammed bag bulges): the edges soften from boxy to puffy.</summary>
        public static float SwollenRoundness(float swell) => Mathf.Lerp(0.42f, 0.8f, Mathf.InverseLerp(1f, 6f, swell));

        /// <summary>
        /// The bag's own shapes (body, pocket, the dark mouth, and the lid flopped open backward) on their own, standing
        /// upright with its bottom at <paramref name="parent"/>'s origin and its pocket toward +z: a bag set down on the
        /// ground looks like the one on the back. <paramref name="swell"/> as on the back (1 = empty). Returns its half size.
        /// </summary>
        public static Vector3 BuildLooseBag(Transform parent, Vector3 bagSize, float swell, Material bagMat, Material flapMat, Material darkMat)
        {
            Vector3 sz = SwollenSize(bagSize, swell);
            Vector3 x = Vector3.right, up = Vector3.up, nOut = Vector3.forward;
            Vector3 c = up * sz.y;
            var body = new GearMesh();
            body.RoundedBox(c, x, up, nOut, sz, SwollenRoundness(swell));
            var pocket = new GearMesh();
            pocket.RoundedBox(c + nOut * (sz.z * 0.92f) - up * (sz.y * 0.38f), x, up, nOut, new Vector3(sz.x * 0.58f, sz.y * 0.32f, 0.02f), 0.45f);
            var mouth = new GearMesh();
            mouth.RoundedBox(c + up * (sz.y * 0.97f), x, up, nOut, MouthHalf(bagSize), 0.5f);
            Loose(parent, "Bag", body.ToMesh("Bag"), bagMat);
            Loose(parent, "BagPocket", pocket.ToMesh("BagPocket"), flapMat);
            Loose(parent, "BagMouth", mouth.ToMesh("BagMouth"), darkMat);
            // the lid, hinged on the back edge of the top and flopped open backward
            Vector3 lidHalf = LidHalf(bagSize);
            Vector3 hinge = c + up * (sz.y * 0.79f + lidHalf.y * 0.92f) - nOut * lidHalf.z;
            var lid = new GearMesh();
            lid.RoundedBox(c + up * (sz.y * 0.79f) + nOut * 0.004f - hinge, x, up, nOut, lidHalf, 0.42f);
            var pivot = new GameObject("BagLidHinge").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = hinge;
            pivot.localRotation = Quaternion.AngleAxis(-150f, x);
            Loose(pivot, "BagLid", lid.ToMesh("BagLid"), flapMat);
            return sz;
        }

        /// <summary>The lid's half size: as on an empty bag, however full it is.</summary>
        static Vector3 LidHalf(Vector3 bagSize) => new Vector3(bagSize.x * 0.98f, bagSize.y * 0.25f, bagSize.z * 1.04f);

        /// <summary>The dark mouth under the lid: as on an empty bag.</summary>
        static Vector3 MouthHalf(Vector3 bagSize) => new Vector3(bagSize.x * 0.8f, bagSize.y * 0.06f, bagSize.z * 0.78f);

        static void Loose(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        void SetBackItem(Vector3 center, Vector3 normal, float t, Quaternion rotation, Vector3 half)
        {
            HasBackItem = true;
            BackItemCenter = center; BackItemNormal = normal; BackItemT = t;
            BackItemRotation = rotation;
            // (x across the back, y up it, z out from it)
            BackItemHalf = new Vector3(half.x, half.y, half.z);
        }

        // ------------------------------------------------------------------ a small mesh builder for gear
        /// <summary>
        /// Vertices with the skin's crawl data (uv: round / along the body, uv1.x: ring radius, uv2: ring pivot), quads
        /// wound to face away from (or toward) a point, smooth normals within a surface and crisp edges between surfaces.
        /// </summary>
        class GearMesh
        {
            readonly List<Vector3> _v = new List<Vector3>();
            readonly List<Vector2> _uv = new List<Vector2>(), _uv1 = new List<Vector2>();
            readonly List<Vector3> _uv2 = new List<Vector3>();
            readonly List<int> _t = new List<int>();

            public int Vert(Vector3 p, Vector2 uv = default, float rho = 0f, Vector3 pivot = default)
            {
                _v.Add(p); _uv.Add(uv); _uv1.Add(new Vector2(rho, 0f)); _uv2.Add(pivot);
                return _v.Count - 1;
            }

            public int Copy(int i) => Vert(_v[i], _uv[i], _uv1[i].x, _uv2[i]);
            public Vector3 Pos(int i) => _v[i];

            /// <summary>A quad facing away from <paramref name="from"/> (<paramref name="away"/>), or toward it.</summary>
            public void Quad(int a, int b, int c, int d, Vector3 from, bool away)
            {
                Vector3 mid = (_v[a] + _v[b] + _v[c] + _v[d]) * 0.25f;
                Vector3 outward = (mid - from) * (away ? 1f : -1f);
                Tri(a, b, c, outward);
                Tri(a, c, d, outward);
            }

            void Tri(int a, int b, int c, Vector3 outward)
            {
                Vector3 n = Vector3.Cross(_v[b] - _v[a], _v[c] - _v[a]);
                if (n.sqrMagnitude < 1e-16f) return;
                if (Vector3.Dot(n, outward) < 0f) { int s = b; b = c; c = s; }   // a front face's edge cross product points out of it
                _t.Add(a); _t.Add(b); _t.Add(c);
            }

            public void Torus(Vector3 center, Vector3 axis, float major, float minor, int seg, int sides)
            {
                axis.Normalize();
                Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized, w = Vector3.Cross(axis, u);
                var idx = new int[seg, sides];
                for (int i = 0; i < seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f;
                    Vector3 dir = u * Mathf.Cos(a) + w * Mathf.Sin(a), ringC = center + dir * major;
                    for (int j = 0; j < sides; j++)
                    {
                        float b = j / (float)sides * Mathf.PI * 2f;
                        idx[i, j] = Vert(ringC + (dir * Mathf.Cos(b) + axis * Mathf.Sin(b)) * minor);
                    }
                }
                for (int i = 0; i < seg; i++)
                for (int j = 0; j < sides; j++)
                {
                    int i2 = (i + 1) % seg, j2 = (j + 1) % sides;
                    float a = (i + 0.5f) / seg * Mathf.PI * 2f;
                    Quad(idx[i, j], idx[i2, j], idx[i2, j2], idx[i, j2], center + (u * Mathf.Cos(a) + w * Mathf.Sin(a)) * major, true);
                }
            }

            /// <summary>A box with rounded edges (a superellipsoid): half sizes along x, y, z; smaller roundness = boxier.</summary>
            public void RoundedBox(Vector3 c, Vector3 x, Vector3 y, Vector3 z, Vector3 half, float roundness, int nLat = 12, int nLon = 24)
            {
                float P(float v) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), roundness);
                var idx = new int[nLat + 1, nLon];
                for (int i = 0; i <= nLat; i++)
                {
                    float lat = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, i / (float)nLat);
                    for (int j = 0; j < nLon; j++)
                    {
                        float lon = j / (float)nLon * Mathf.PI * 2f;
                        idx[i, j] = Vert(c + x * (half.x * P(Mathf.Cos(lat) * Mathf.Cos(lon))) + y * (half.y * P(Mathf.Sin(lat)))
                                           + z * (half.z * P(Mathf.Cos(lat) * Mathf.Sin(lon))));
                    }
                }
                for (int i = 0; i < nLat; i++)
                for (int j = 0; j < nLon; j++)
                    Quad(idx[i, j], idx[i + 1, j], idx[i + 1, (j + 1) % nLon], idx[i, (j + 1) % nLon], c, true);
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                m.SetVertices(_v);
                m.SetUVs(0, _uv);
                m.SetUVs(1, _uv1);
                m.SetUVs(2, _uv2);
                m.SetTriangles(_t, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }
    }
}
