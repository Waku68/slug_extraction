using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// Gear worn by the crawling slug (<see cref="SlugPop"/>; the two-legged one's is <see cref="DinoEquipment"/>, the look
    /// is shared on <see cref="SlugGearBase"/>), built from code to fit its body and drawn with the slug's own toon shader, so it crawls,
    /// stands up and turns with it:
    ///  - helmet: a round steel pot with a short brim, longer over the eyes; the tentacles come out through two
    ///    grommeted holes (they hear well, but can be shot). It rides the head rigidly.
    ///  - shell plate armour: a domed oval of shell over the mantle (the hump behind the head, where real slugs keep the
    ///    remnant of their ancestors' shell), a chest plate in front and straps between them above the arms. It is cut
    ///    from the skin itself, so it bends exactly like the body.
    ///  - snail shell: a coiled shell on the back, its opening turned in against the body as a snail's rests on its
    ///    foot. The shell is what marks a snail (the scavs); a slug carries a bag.
    ///  - bag: a canvas backpack that swells with what is in it (slumped and folded when empty, round and taut when
    ///    crammed), with a lid buckled down the front, straps, a pocket and piped seams (<see cref="CanvasBag"/>), belted
    ///    round the body above the arms. It sits on the armour when both are worn. It rides the back rigidly. Looked into
    ///    (<see cref="bagOpen"/>), it tips up, its lid flips open and its mouth spreads to show the lining and the heap of
    ///    what is in it, for everyone to see;
    ///  - vest: a chest rig just under the face, a row of pouches with the moss balls and the magazines still carried in
    ///    them (<see cref="SetVestLoad"/>), so what is left shows on the body.
    /// The armour wears the camouflage (a skin: the owner's look, whatever armour it is), woodland by default.
    /// </summary>
    [ExecuteAlways]
    public class SlugEquipment : SlugGearBase
    {
        public SlugPop slug;

        public override SlugBodyBase Body => slug;

        readonly List<Object> _owned = new List<Object>();
        readonly List<Renderer> _helmetRenderers = new List<Renderer>();
        bool _pending;
        Camera _ownCamera;
        System.Func<bool> _ownCameraInside;

        /// <summary>The buckles' dull metal, also for a bag set down on the ground.</summary>
        public static readonly Color BuckleColor = new Color(0.3f, 0.29f, 0.26f);

        // the bag's parts (those sewn on it, and the lid with what goes with it) and where it sits (body space), for posing it;
        // its meshes shut and opened (the same vertices), blended by how far it is opened
        GameObject _bagGo, _bagHeap;
        readonly List<(Mesh mesh, Vector3[] shut, Vector3[] opened)> _bagShapes = new List<(Mesh, Vector3[], Vector3[])>();
        float _bagShaped, _bagPosedOpen;
        // the bag rocking out from the back and back again after a jolt (degrees about its across axis, and how fast)
        float _rock, _rockV;
        bool _rocking;
        readonly List<GameObject> _bagFixed = new List<GameObject>(), _bagLidParts = new List<GameObject>();
        Vector3 _bagP, _bagN, _bagX, _bagUp, _bagOut, _bagSz, _bagHinge, _bagHingeOpen, _bagInside, _bagInsideOpen, _bagFoot, _bagSeat;
        float _bagT;
        // what rides in the vest's pouches: one list of parts per pouch, each kind in order
        readonly List<GameObject[]> _vestMoss = new List<GameObject[]>(), _vestBig = new List<GameObject[]>(), _vestSmall = new List<GameObject[]>();
        int _mossPouches = 2, _bigPouches = 2, _smallPouches = 2;

        /// <summary>
        /// The owner's own camera: while it sits inside the head (first person), the helmet only casts its shadow for
        /// that camera, so its brim never fills the view; every other camera sees it as usual.
        /// </summary>
        public override void SetOwnCamera(Camera cam, System.Func<bool> inside)
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
            _bagFixed.Clear();
            _bagLidParts.Clear();
            _bagGo = _bagHeap = null;
            _bagShapes.Clear();
            _bagShaped = 0f;
            _vestMoss.Clear(); _vestBig.Clear(); _vestSmall.Clear();
            HasBackItem = false;
        }

        T Own<T>(T o) where T : Object { o.hideFlags = HideFlags.DontSave; _owned.Add(o); return o; }

        /// <summary>The renderers of the gear being worn, one list per piece that falls as one thing (the helmet; the
        /// armour with the bag's belts), the bag itself only with <paramref name="withBag"/>. For leaving the gear behind
        /// when the slug falls.</summary>
        public override void CollectGear(List<List<Renderer>> pieces, bool withBag)
        {
            var helmet = new List<Renderer>();
            var rest = new List<Renderer>();
            foreach (var o in _owned)
            {
                if (!(o is GameObject go) || !go) continue;
                if (!withBag && (_bagFixed.Contains(go) || _bagLidParts.Contains(go))) continue;
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    (_helmetRenderers.Contains(r) ? helmet : rest).Add(r);
            }
            if (helmet.Count > 0) pieces.Add(helmet);
            if (rest.Count > 0) pieces.Add(rest);
        }

        /// <summary>
        /// Jolts the bag (a tool pulled free of the straps over it, + = its top swings out from the back; pushed home, -):
        /// it rocks about where it sits on the back and settles in about half a second.
        /// </summary>
        public override void Jostle(float degreesPerSecond)
        {
            if (!_bagGo) return;
            _rockV += degreesPerSecond;
            _rocking = true;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (_rocking)
            {
                // a soft spring (4 Hz, lightly damped), stepped finely so a long frame does not throw it
                float dt = Time.deltaTime;
                int n = Mathf.Clamp(Mathf.CeilToInt(dt * 240f), 1, 32);
                float w = 2f * Mathf.PI * 4f;
                for (int i = 0; i < n; i++)
                {
                    _rockV += (-w * w * _rock - 2f * 0.3f * w * _rockV) * (dt / n);
                    _rock += _rockV * (dt / n);
                }
                if (Mathf.Abs(_rock) < 0.02f && Mathf.Abs(_rockV) < 0.5f) { _rock = _rockV = 0f; _rocking = false; }
            }
            // posed for everyone (rocking, opened, or just shut again)
            if (_rocking || bagOpen > 0f || _bagPosedOpen > 0f) PoseBag(bagOpen);
        }

        /// <summary>
        /// Poses the bag opened by <paramref name="open"/> (0..1), for everyone: the lid swung about its hinge (the edge away
        /// from the back) and falling open outward, the mouth spread and the bottom drawn in (<see cref="bagPeekMouth"/>,
        /// <see cref="bagPeekBottom"/>), the lining and the heap of what is in it showing in the mouth, and the whole bag
        /// tipped up onto its bottom, standing where it lay, until its mouth faces <see cref="bagPeekZenith"/> from straight
        /// up, over to the right and a little to the front (<see cref="bagPeekLean"/>). Returns false when there is no bag.
        /// </summary>
        public bool PoseBag(float open)
        {
            if (!_bagGo || !slug || slug.VisualRoot == null) return false;
            _bagPosedOpen = open;
            Shape(open);
            var vis = slug.VisualRoot;
            Quaternion bend = slug.FollowerRotation(_bagT);
            Vector3 foot = slug.FollowerLocal(_bagFoot, _bagT, _bagP, _bagN), seat = slug.FollowerLocal(_bagSeat, _bagT, _bagP, _bagN);
            Quaternion rot = Quaternion.identity;
            if (open > 0f)
            {
                // (the body's x is its right, z its front)
                Vector3 to = PeekMouthDirection(vis.InverseTransformDirection(Vector3.up), Vector3.right, Vector3.forward);
                rot = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(bend * _bagUp, to), open);
            }
            Vector3 pos = Vector3.Lerp(foot, seat, open) - rot * foot;
            if (_rock != 0f)
            {
                // rocked about where it sits (after a jolt from a tool pulled off or put on over it)
                Vector3 attach = slug.FollowerLocal(_bagP, _bagT, _bagP, _bagN);
                Quaternion rock = Quaternion.AngleAxis(_rock, bend * _bagX);
                Vector3 pivot = pos + rot * attach;
                rot = rot * rock;
                pos = pivot - rot * attach;
            }
            foreach (var go in _bagFixed) Place(go, pos, rot, 1f);
            if (_bagHeap)
            {
                var heapR = _bagHeap.GetComponent<Renderer>();
                bool heap = open > 0.02f && bagHasContents;
                if (heapR.enabled != heap) heapR.enabled = heap;
            }

            // the lid (with the straps over it) swings about its hinge across the front and falls open outward
            Vector3 hinge = slug.FollowerLocal(Vector3.Lerp(_bagHinge, _bagHingeOpen, open), _bagT, _bagP, _bagN);
            Quaternion swing = Quaternion.AngleAxis(bagLidAngle * open, bend * _bagX);
            foreach (var go in _bagLidParts) Place(go, pos + rot * (hinge - swing * hinge), rot * swing, 1f);

            Vector3 inside = slug.FollowerLocal(Vector3.Lerp(_bagInside, _bagInsideOpen, open), _bagT, _bagP, _bagN);
            BagInsidePosition = vis.TransformPoint(pos + rot * inside);
            BagMouthRotation = vis.rotation * rot * bend * Quaternion.LookRotation(_bagOut, _bagUp);
            return true;
        }

        /// <summary>The bag's meshes blended between shut and opened (only when that changed).</summary>
        void Shape(float open)
        {
            if (Mathf.Abs(open - _bagShaped) < 1e-3f && (open > 0f || _bagShaped == 0f)) return;
            _bagShaped = open;
            foreach (var (mesh, shut, opened) in _bagShapes)
            {
                if (!mesh) continue;
                var v = new Vector3[shut.Length];
                for (int i = 0; i < v.Length; i++) v[i] = Vector3.LerpUnclamped(shut[i], opened[i], open);
                mesh.vertices = v;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
        }

        static void Place(GameObject go, Vector3 pos, Quaternion rot, float s)
        {
            if (!go) return;
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = Vector3.one * s;
        }

        /// <summary>Puts the gear (back) on. Called on enable, on every slug rebuild and after a change in the inspector.</summary>
        public override void Build()
        {
            Clear();
            if (!slug || slug.VisualRoot == null || slug.Shape == null) return;
            if (helmet) BuildHelmet();
            if (armor == Armor.ShellPlate) BuildShellPlate();
            else if (armor == Armor.SnailShell) BuildSnailShell();
            if (bag) BuildBag();
            if (vest) BuildVest();
            RaiseBuilt();
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

            if (tentacleHelmet) { BuildHorns(mat); return; }

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

        /// <summary>
        /// The tentacle helmet's two horns: steel shells the shape of the tentacles, from their roots inside the pot to a
        /// little past their tips. The tentacle moves inside, so each horn droops and draws in with it (SlugPop.RideEar).
        /// </summary>
        void BuildHorns(Material mat)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                slug.EarRoot(side, out var root, out var earAxis, out _, out _);
                var mesh = Own(slug.EarShell(side, hornGap, hornExtend, side < 0 ? "HelmetHorn_L" : "HelmetHorn_R"));
                var go = slug.AddGear(mesh.name, mesh, slug.GearFollower(mat, 0.93f, root, earAxis), root);
                if (!go) continue;
                _owned.Add(go);
                _helmetRenderers.Add(go.GetComponent<Renderer>());
                slug.RideEar(side, mesh);
            }
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

            // made twice, shut and opened (the mouth spread, the bottom drawn in, the top sunk into the lining), and blended
            var frame = new CanvasBag.Frame(c, x, up, nOut);
            var bag = CanvasBag.Make(frame, bagSize, sz, Fill(bagSwell));
            // (a slack bag's top is thin: its mouth is opened deeper, to about 0.7 of its width, so it gapes rather than slits)
            // (the fuller, the wider it gapes)
            float mouthW = Mathf.Clamp(bagPeekMouthMax / Mathf.Max(sz.x, 1e-3f), 1f, Mathf.Lerp(bagPeekMouth, bagPeekMouthFull, Fill(bagSwell)));
            float rimW = 0f, rimZ0 = float.MaxValue, rimZ1 = float.MinValue;
            foreach (var v in bag.top.Positions())
            {
                rimW = Mathf.Max(rimW, Mathf.Abs(Vector3.Dot(v - c, x)));
                float vz = Vector3.Dot(v - c, nOut);
                rimZ0 = Mathf.Min(rimZ0, vz); rimZ1 = Mathf.Max(rimZ1, vz);
            }
            float mouthD = Mathf.Clamp(0.7f * rimW * mouthW * 2f / Mathf.Max(rimZ1 - rimZ0, 1e-3f), mouthW, 5f);
            frame.Taper(sz.y, sz.z, bagPeekBottom, mouthW, mouthD);
            var opened = CanvasBag.Make(frame, bagSize, sz, Fill(bagSwell), true);
            // tipped up, it stands on its bottom where its back panel lay on the back, its lower part settled onto the body
            _bagFoot = c - up * sz.y;
            _bagSeat = p + nOut * standOff;
            Drape(opened, c, up, sz, standOff);
            float heapW = 0f, heapZ0 = float.MaxValue, heapZ1 = float.MinValue;
            foreach (var v in opened.heap.Positions())
            {
                heapW = Mathf.Max(heapW, Mathf.Abs(Vector3.Dot(v - c, x)));
                float vz = Vector3.Dot(v - c, nOut);
                heapZ0 = Mathf.Min(heapZ0, vz); heapZ1 = Mathf.Max(heapZ1, vz);
            }
            BagHeapHalf = new Vector2(heapW, (heapZ1 - heapZ0) * 0.5f);
            GameObject Part(string name, GearMesh mesh, GearMesh open, Material mat, List<GameObject> into)
            {
                var m = Own(mesh.ToMesh(name));
                var go = AddGo(name, m, slug.GearFollower(mat, th, p, nrm));
                if (go && into != null) into.Add(go);
                _bagShapes.Add((m, m.vertices, open.Positions()));
                return go;
            }
            Material flapMat = Own(slug.GearMaterial(bagFlapColor)), beltMat = Own(slug.GearMaterial(strapColor));
            _bagGo = Part("Bag", bag.body, opened.body, Own(slug.GearMaterial(bagColor)), _bagFixed);
            SetBackItem(c, nrm, th, Quaternion.LookRotation(nOut, up), new Vector3(sz.x, sz.y, sz.z) * 1.05f);
            // the top of the sack, the lining inside when it is opened (the cloth's inner side, a shade darker)
            Part("BagInside", bag.top, opened.top, Own(slug.GearMaterial(Color.Lerp(bagColor, Color.black, 0.3f))), _bagFixed);
            _bagHeap = Part("BagHeap", bag.heap, opened.heap, Own(slug.GearMaterial(bagHeapColor)), _bagFixed);
            if (_bagHeap) _bagHeap.GetComponent<Renderer>().enabled = false;
            Part("BagSeams", bag.trim, opened.trim, Own(slug.GearMaterial(Color.Lerp(bagFlapColor, Color.black, 0.3f))), _bagFixed);
            Part("BagPocket", bag.pocket, opened.pocket, flapMat, _bagFixed);
            Part("BagStraps", bag.straps, opened.straps, beltMat, _bagFixed);
            Part("BagBuckles", bag.buckles, opened.buckles, Own(slug.GearMaterial(buckleColor)), _bagFixed);
            Part("BagLid", bag.lid, opened.lid, flapMat, _bagLidParts);
            Part("BagLidStraps", bag.lidStraps, opened.lidStraps, beltMat, _bagLidParts);
            _bagP = p; _bagN = nrm; _bagT = th; _bagX = x; _bagUp = up; _bagOut = nOut; _bagSz = sz;
            _bagHinge = bag.hinge; _bagHingeOpen = opened.hinge;
            _bagInside = bag.inside; _bagInsideOpen = opened.inside;
            _bagPosedOpen = 0f;
            if (bagOpen > 0f) PoseBag(bagOpen);

            // belts round the body, above the arms (and over the armour when worn)
            var belts = new GearMesh();
            float gap = armor == Armor.ShellPlate ? ShieldStandOff - 0.012f : 0.004f;
            Plate(belts, 0.66f, 0.685f, 0f, u => 180f, gap, 0.006f, 2, 36);
            Plate(belts, 0.41f, 0.435f, 0f, u => 180f, gap, 0.006f, 2, 36);
            Add("BagBelts", Own(belts.ToMesh("BagBelts")), beltMat);
        }

        /// <summary>
        /// Settles the opened bag's lower part onto the back: posed as it is when looked into (tipped up onto its bottom, its
        /// mouth over to the right and front), every point of its lower half that hangs a little off the body is drawn in onto it, so
        /// no gap shows between the bag and the back; points far off it (the front of a big bag) and those in the body stay.
        /// </summary>
        void Drape(CanvasBag.Parts opened, Vector3 c, Vector3 up, Vector3 sz, float standOff)
        {
            var shape = slug.Shape;
            // (as the pose does it in the visual's space; the body's x is its right, y its up and z its front)
            Quaternion r = Quaternion.FromToRotation(up, PeekMouthDirection(Vector3.up, Vector3.right, Vector3.forward)), back = Quaternion.Inverse(r);
            var centers = new List<Vector3>();
            for (int i = 15; i <= 86; i++) centers.Add(shape.FrameAt(i * 0.01f).center);
            float S(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
            Vector3 Settle(Vector3 v)
            {
                float w = 1f - S((Vector3.Dot(v - c, up) / sz.y + 1f) * 0.5f / 0.55f);
                if (w <= 0f) return v;
                Vector3 q = r * (v - _bagFoot) + _bagSeat, mid = centers[0];
                foreach (var e in centers) if ((q - e).sqrMagnitude < (q - mid).sqrMagnitude) mid = e;
                Vector3 dir = (q - mid).normalized;
                if (!shape.RaycastT(mid, dir, out var skin, out _, out _)) return v;
                float gap = Vector3.Distance(q, mid) - Vector3.Distance(skin, mid) - standOff;
                if (gap <= 0f) return v;
                float pull = gap * w * (1f - S((gap - 0.05f) / 0.13f));
                return back * (q - dir * pull - _bagSeat) + _bagFoot;
            }
            foreach (var m in new[] { opened.body, opened.trim, opened.pocket, opened.straps, opened.buckles }) m.Move(Settle);
        }

        // ------------------------------------------------------------------ vest (a chest rig of pouches: magazines and moss balls)
        // The panel lies over the chest just under the face, between the arms (they come out at about t 0.82, some 60°
        // round from the front), its lower part on the chest plate when that is worn; the pouches stand in one row along
        // its lower part, so what rides in them sticks up out of them toward the face.
        const float VestFrom = 0.71f, VestTo = 0.81f, VestRow = 0.738f;

        /// <summary>
        /// The chest rig: a canvas panel over the chest held by a strap round the body, with one row of pouches
        /// (from the right of the chest to the left: the moss balls, the big gun's magazines, the small gun's magazines; in the
        /// own view while peeking that reads moss balls on the left, magazines on the right, as in the sketch).
        /// Each pouch has its thing in it (shown or not by <see cref="SetVestLoad"/>); an empty pouch shows its dark mouth.
        /// </summary>
        void BuildVest()
        {
            float gap = armor == Armor.ShellPlate ? 0.026f : 0.006f;
            const float thick = 0.01f;
            Skin(VestRow, 180f, out var at, out var atN, out float atT);
            Material Follow(Color c) => slug.GearFollower(Own(slug.GearMaterial(c)), atT, at, atN);
            GameObject Part(string name, GearMesh mesh, Material mat)
            {
                return AddGo(name, Own(mesh.ToMesh(name)), mat);
            }

            // the panel and its harness bend with the skin (the chest turns a little with the head); the pouches ride rigidly
            // at their row
            var panel = new GearMesh();
            Plate(panel, VestFrom, VestTo, 180f, u => 40f + 6f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(u * 2f - 1f, 2f))), gap, thick, 8, 18, 0.003f);
            Part("Vest", panel, Own(slug.GearMaterial(vestColor)));
            // the harness: a strap round the body along the panel's lower edge (one higher up would run under the face)
            var harness = new GearMesh();
            Plate(harness, VestFrom, VestFrom + 0.016f, 180f, u => 180f, gap - 0.004f, 0.005f, 2, 36);
            Part("VestHarness", harness, Own(slug.GearMaterial(strapColor)));

            // the pouches, laid out across the chest by their widths
            var mag = new Vector3(0.016f, 0.05f, 0.013f);
            var small = new Vector3(0.012f, 0.038f, 0.009f);
            const float mossR = 0.033f, space = 0.008f;
            var slots = new List<(int kind, float width)>();
            for (int i = 0; i < _mossPouches; i++) slots.Add((0, (mossR + 0.005f) * 2f));
            for (int i = 0; i < _bigPouches; i++) slots.Add((1, (mag.x + 0.005f) * 2f));
            for (int i = 0; i < _smallPouches; i++) slots.Add((2, (small.x + 0.005f) * 2f));
            float total = -space;
            foreach (var s in slots) total += s.width + space;
            float ringR = slug.Shape.FrameAt(VestRow).rx + gap + thick;
            var pouches = new GearMesh();
            var mouths = new GearMesh();
            Material pouchMat = Follow(vestPouchColor), dark = Follow(new Color(0.07f, 0.06f, 0.05f));
            Material steel = Follow(magColor), salt = Follow(saltColor), moss = Follow(mossColor);
            float along = -total * 0.5f;
            foreach (var s in slots)
            {
                along += s.width * 0.5f;
                // + along = toward the left of the chest (angle 180 + d)
                Skin(VestRow, 180f + along / ringR * Mathf.Rad2Deg, out var p, out var n, out _);
                var f = slug.Shape.FrameAt(VestRow);
                Vector3 up = Vector3.ProjectOnPlane(f.forward, n).normalized;
                Vector3 x = Vector3.Cross(up, n).normalized;
                Vector3 surf = p + n * (gap + thick);
                if (s.kind == 0)
                {
                    // a short open cup with the moss ball sat in it, more than half of it showing
                    var h = new Vector3(mossR + 0.005f, 0.022f, mossR * 0.85f);
                    Vector3 c = surf + n * h.z;
                    pouches.RoundedBox(c, x, up, n, h, 0.35f, 8, 16);
                    mouths.RoundedBox(c + up * h.y, x, up, n, new Vector3(h.x * 0.8f, 0.002f, h.z * 0.75f), 0.4f, 4, 12);
                    var ball = new GearMesh();
                    ball.RoundedBox(c + up * (h.y + 0.006f), x, up, n, Vector3.one * mossR, 1f, 10, 16);
                    _vestMoss.Add(new[] { Part("VestMoss" + _vestMoss.Count, ball, moss) });
                }
                else
                {
                    // a tall pouch with a magazine standing in it, its top (and the white salt at its lips) above the pouch
                    var m = s.kind == 1 ? mag : small;
                    var h = new Vector3(m.x + 0.005f, m.y * 0.72f, m.z + 0.006f);
                    Vector3 c = surf + n * h.z;
                    pouches.RoundedBox(c, x, up, n, h, 0.3f, 8, 16);
                    mouths.RoundedBox(c + up * h.y, x, up, n, new Vector3(h.x * 0.85f, 0.002f, h.z * 0.75f), 0.4f, 4, 12);
                    Vector3 top = c + up * (h.y + m.y * 0.6f);
                    var body = new GearMesh();
                    body.RoundedBox(top - up * m.y, x, up, n, m, 0.25f, 8, 16);
                    var lips = new GearMesh();
                    lips.RoundedBox(top + up * 0.002f, x, up, n, new Vector3(m.x * 0.75f, 0.005f, m.z * 0.6f), 0.5f, 4, 12);
                    string name = (s.kind == 1 ? "VestMag" : "VestSmallMag") + (s.kind == 1 ? _vestBig.Count : _vestSmall.Count);
                    (s.kind == 1 ? _vestBig : _vestSmall).Add(new[] { Part(name, body, steel), Part(name + "Salt", lips, salt) });
                }
                along += s.width * 0.5f + space;
            }
            Part("VestPouches", pouches, pouchMat);
            Part("VestPouchMouths", mouths, dark);
        }

        /// <summary>
        /// What rides on the vest: how many pouches of each kind it has (moss balls, the big gun's magazines, the small
        /// gun's), and how many of them are filled. It is rebuilt only when the pouches change.
        /// </summary>
        public override void SetVestLoad(int mossPouches, int moss, int bigPouches, int big, int smallPouches, int small)
        {
            if (mossPouches != _mossPouches || bigPouches != _bigPouches || smallPouches != _smallPouches)
            {
                _mossPouches = mossPouches; _bigPouches = bigPouches; _smallPouches = smallPouches;
                Build();
            }
            Show(_vestMoss, moss);
            Show(_vestBig, big);
            Show(_vestSmall, small);
        }

        static void Show(List<GameObject[]> pouches, int filled)
        {
            // the last pouches are emptied first
            for (int i = 0; i < pouches.Count; i++)
                foreach (var go in pouches[i])
                    if (go && go.activeSelf != i < filled) go.SetActive(i < filled);
        }

        /// <summary>How full the bag is by its swell: 0 = empty (1), 1 = crammed (6).</summary>
        public static float Fill(float swell) => Mathf.InverseLerp(1f, 6f, swell);

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

        /// <summary>
        /// The bag's own shapes on their own, its lid flopped open forward over the pocket, standing upright with its bottom
        /// at <paramref name="parent"/>'s origin and its front toward +z: a bag set down on the ground looks like the one on
        /// the back. <paramref name="swell"/> as on the back (1 = empty). Returns its half size, and the middle of its mouth
        /// (<paramref name="mouth"/>, local: a slack bag's top slumps below its full height).
        /// </summary>
        public static Vector3 BuildLooseBag(Transform parent, Vector3 bagSize, float swell, Material bagMat, Material flapMat, Material darkMat,
                                            Material strapMat, Material metalMat, out Vector3 mouth)
        {
            Vector3 sz = SwollenSize(bagSize, swell);
            var bag = CanvasBag.Make(new CanvasBag.Frame(Vector3.up * sz.y, Vector3.right, Vector3.up, Vector3.forward), bagSize, sz, Fill(swell));
            if (!strapMat) strapMat = flapMat;
            if (!metalMat) metalMat = darkMat;
            Loose(parent, "Bag", bag.body.ToMesh("Bag"), bagMat);
            Loose(parent, "BagTop", bag.top.ToMesh("BagTop"), bagMat);
            Loose(parent, "BagSeams", bag.trim.ToMesh("BagSeams"), flapMat);
            Loose(parent, "BagPocket", bag.pocket.ToMesh("BagPocket"), flapMat);
            Loose(parent, "BagStraps", bag.straps.ToMesh("BagStraps"), strapMat);
            Loose(parent, "BagBuckles", bag.buckles.ToMesh("BagBuckles"), metalMat);
            Loose(parent, "BagMouth", bag.mouth.ToMesh("BagMouth"), darkMat);
            var pivot = new GameObject("BagLidHinge").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = bag.hinge;
            pivot.localRotation = Quaternion.AngleAxis(120f, Vector3.right);
            Loose(pivot, "BagLid", bag.lid.ToMesh("BagLid", -bag.hinge), flapMat);
            Loose(pivot, "BagLidStraps", bag.lidStraps.ToMesh("BagLidStraps", -bag.hinge), strapMat);
            mouth = bag.mouthCenter;
            return sz;
        }

        static void Loose(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // ------------------------------------------------------------------ a small mesh builder for gear
        /// <summary>
        /// Vertices with the skin's crawl data (uv: round / along the body, uv1.x: ring radius, uv2: ring pivot), quads
        /// wound to face away from (or toward) a point, smooth normals within a surface and crisp edges between surfaces.
        /// </summary>
        internal class GearMesh
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
            public Vector3[] Positions() => _v.ToArray();
            public void Move(System.Func<Vector3, Vector3> to) { for (int i = 0; i < _v.Count; i++) _v[i] = to(_v[i]); }

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

            /// <summary>The mesh, its vertices moved by <paramref name="shift"/> (to hang it from a pivot).</summary>
            public Mesh ToMesh(string name, Vector3 shift = default)
            {
                var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                if (shift != Vector3.zero) for (int i = 0; i < _v.Count; i++) _v[i] += shift;
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
