using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// Gear worn by the two-legged slug (<see cref="DinoSlugBody"/>, Nov 3 わくさん：装備まで作り直す), built from code
    /// to fit its skin at rest and carried rigidly by its bones, drawn with the slug's own toon shader. The look (what is
    /// worn, its colours) is set on <see cref="SlugGearBase"/>, the same as the crawling slug's gear:
    ///  - helmet (on the head bone): a round steel pot over the top of the head, down to just above the eyes in front and
    ///    to the nape behind, with a short brim, longer over the eyes. The antennae come out through two grommeted holes;
    ///    the tentacle helmet instead sheathes each antenna in a horn that rides the antenna (it droops with it).
    ///  - shell plate armour (on the chest bone; わくさん：背中の殻＋胸当て): a domed oval of shell over the upper back
    ///    (the mantle), a smaller plate on the chest, and straps over the shoulders above the arms between them (the bag's
    ///    straps instead, when one is worn). The legs and the tail stay bare.
    ///  - snail shell (the snails'): a coiled shell standing on the upper back, its opening turned in against it.
    ///  - bag (on the chest bone; わくさん：肩で背負う): the canvas backpack (<see cref="CanvasBag"/>) high on the back,
    ///    over the shell when worn, hanging nearly upright, held by two straps over the shoulders and a belt round the
    ///    waist under it. Looked
    ///    into, it tips up on its bottom toward the right as the crawling slug's does, the lid flips open and the mouth
    ///    spreads.
    ///  - vest (on the chest bone): a panel on the chest under the mouth, above the arms, with one row of pouches (moss
    ///    balls, magazines).
    /// Where things sit is measured on the skin at rest (body space: +Y up, +Z forward, feet at 0) round the spine line
    /// (hips → neck): a height on that line and an angle round it (0 = the middle of the back, 90 = the right side,
    /// 180 = the front), as <see cref="DinoSlugBody.CarrySpot"/> does. The heights below are in metres (eyes at 0.46 m).
    /// The meshes that follow the skin are made once per body mesh and shared by every slug wearing them (bots too).
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [DefaultExecutionOrder(-5)]   // after the body poses its bones (LateUpdate, -10), before the bag and the camera read the bag (-4, 0)
    public class DinoEquipment : SlugGearBase
    {
        public DinoSlugBody slug;

        public override SlugBodyBase Body => slug;

        // where the pieces sit on the body (heights on the spine line, m)
        const float ShieldFrom = 0.17f, ShieldTo = 0.335f;           // the back shield, over the upper back (below the neck)
        const float PlateFrom = 0.26f, PlateTo = 0.385f;             // the chest plate, under the mouth (the arms come out at 0.25-0.32)
        const float ShoulderH = 0.338f;                              // the straps over the shoulders (the arms end at 0.32)
        const float ShieldStandOff = 0.042f;                         // how far the shield stands off the back at most
        const float BagAt = 0.25f;                                   // the bag's middle on the back
        const float WaistH = 0.2f;                                   // the belt under the bag
        const float VestFrom = 0.3f, VestTo = 0.375f, VestRow = 0.318f;     // the vest panel and its row of pouches, above the arms
        const float BagPeekOut = 0.03f;                              // looked into, the bag stands this much further off the back

        readonly List<Object> _owned = new List<Object>();
        readonly List<Renderer> _helmetRenderers = new List<Renderer>();
        bool _pending;
        Camera _ownCamera;
        System.Func<bool> _ownCameraInside;

        // the bag: a root under the chest bone (posed when it is opened or rocked), its parts, its lid on a hinge
        Transform _bagRoot, _bagLid;
        GameObject _bagHeap;
        readonly List<GameObject> _bagParts = new List<GameObject>();
        readonly List<(Mesh mesh, Vector3[] shut, Vector3[] opened)> _bagShapes = new List<(Mesh, Vector3[], Vector3[])>();
        float _bagShaped, _bagPosedOpen, _rock, _rockV;
        bool _rocking;
        // bag frame, in the chest bone's space: across, up the bag, out from the back; its foot, where it stands opened,
        // where it is strapped on, the lid's hinge and the inside (shut and opened)
        Vector3 _bagX, _bagUp, _bagOut, _bagMid, _bagAttach, _bagHinge, _bagHingeOpen, _bagInside, _bagInsideOpen;
        Matrix4x4 _chestRest;   // the chest bone at rest, in body space
        // the vest's pouches: one list of parts per pouch, each kind in order
        readonly List<GameObject[]> _vestMoss = new List<GameObject[]>(), _vestBig = new List<GameObject[]>(), _vestSmall = new List<GameObject[]>();
        int _mossPouches = 2, _bigPouches = 2, _smallPouches = 2;

        // ------------------------------------------------------------------ life
        void OnEnable()
        {
            if (!slug) slug = GetComponent<DinoSlugBody>();
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            Build();
        }

        void OnDisable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
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

        void Clear()
        {
            foreach (var o in _owned)
            {
                if (!o) continue;
                if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
            }
            _owned.Clear();
            _helmetRenderers.Clear();
            _bagParts.Clear();
            _bagShapes.Clear();
            _bagRoot = _bagLid = null;
            _bagHeap = null;
            _bagShaped = 0f;
            _vestMoss.Clear(); _vestBig.Clear(); _vestSmall.Clear();
            HasBackItem = false;
        }

        T Own<T>(T o) where T : Object { o.hideFlags = HideFlags.DontSave; _owned.Add(o); return o; }

        public override void CollectGear(List<List<Renderer>> pieces, bool withBag)
        {
            var helmetR = new List<Renderer>();
            var rest = new List<Renderer>();
            foreach (var o in _owned)
            {
                if (!(o is GameObject go) || !go) continue;
                if (!withBag && go.transform == _bagRoot) continue;
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    (_helmetRenderers.Contains(r) ? helmetR : rest).Add(r);
            }
            if (helmetR.Count > 0) pieces.Add(helmetR);
            if (rest.Count > 0) pieces.Add(rest);
        }

        // ------------------------------------------------------------------ build
        public override void Build()
        {
            Clear();
            if (!slug || !slug.head || !slug.chest || !Rest()) return;
            if (helmet) BuildHelmet();
            if (armor == Armor.ShellPlate) BuildShellPlate();
            else if (armor == Armor.SnailShell) BuildSnailShell();
            if (bag && armor != Armor.SnailShell) BuildBag();
            if (vest) BuildVest();
            if (bagOpen > 0f) PoseBag(bagOpen);
            RaiseBuilt();
        }

        // the skin at rest: the body mesh (body space), its bind poses, the spine line
        SkinnedMeshRenderer _skin;
        Mesh _mesh;
        Vector3[] _v;
        int[] _tri;
        Vector3[] _spine;   // hips, spine, chest, neck at rest (body space)

        bool Rest()
        {
            _skin = slug.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (!_skin || !_skin.sharedMesh || !_skin.sharedMesh.isReadable) return false;
            if (_mesh != _skin.sharedMesh)
            {
                _mesh = _skin.sharedMesh;
                _v = _mesh.vertices;
                _tri = _mesh.triangles;
                _grid = null;
            }
            _spine = new[] { BonePos(slug.hips), BonePos(slug.spine), BonePos(slug.chest), BonePos(slug.neck) };
            return true;
        }

        /// <summary>Body space (at rest) to a bone's own space, from the bind poses.</summary>
        Matrix4x4 ToBone(Transform bone)
        {
            int i = System.Array.IndexOf(_skin.bones, bone);
            return i >= 0 ? _mesh.bindposes[i] : bone.worldToLocalMatrix * slug.transform.localToWorldMatrix;
        }

        Vector3 BonePos(Transform bone) => ToBone(bone).inverse.MultiplyPoint3x4(Vector3.zero);

        /// <summary>The spine line at height <paramref name="h"/> (body space), and its direction there.</summary>
        Vector3 Axis(float h, out Vector3 line)
        {
            int i = 0;
            while (i < _spine.Length - 2 && _spine[i + 1].y < h) i++;
            Vector3 a = _spine[i], b = _spine[i + 1];
            line = (b - a).normalized;
            return Vector3.LerpUnclamped(a, b, (h - a.y) / Mathf.Max(b.y - a.y, 1e-4f));
        }

        /// <summary>Out from the spine line at height h, <paramref name="angle"/> round it (0 = the back, 90 = the right, 180 = the front).</summary>
        Vector3 Round(float h, float angle, out Vector3 from)
        {
            from = Axis(h, out var line);
            Vector3 back = Vector3.Cross(line, Vector3.right).normalized;   // up and back on the leaning body
            float a = angle * Mathf.Deg2Rad;
            return (back * Mathf.Cos(a) + Vector3.right * Mathf.Sin(a)).normalized;
        }

        /// <summary>The skin at height h, <paramref name="angle"/> round the spine line, and its outward normal.</summary>
        bool Skin(float h, float angle, out Vector3 p, out Vector3 n)
        {
            Vector3 dir = Round(h, angle, out var from);
            if (Ray(from, dir, out p, out n)) return true;
            p = from + dir * 0.12f; n = dir;
            return false;
        }

        // the skin's triangles sorted into 2 cm cells (body space at rest), once per body mesh, so a ray only tests those
        // in the cells it passes through
        const float Cell = 0.02f;
        static readonly Dictionary<Mesh, Dictionary<Vector3Int, List<int>>> Grids = new Dictionary<Mesh, Dictionary<Vector3Int, List<int>>>();
        Dictionary<Vector3Int, List<int>> _grid;
        int[] _stamp;
        int _ray;

        static Vector3Int CellOf(Vector3 p) => new Vector3Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell), Mathf.FloorToInt(p.z / Cell));

        Dictionary<Vector3Int, List<int>> Grid()
        {
            if (Grids.TryGetValue(_mesh, out var g)) return g;
            g = new Dictionary<Vector3Int, List<int>>();
            for (int t = 0; t < _tri.Length; t += 3)
            {
                Vector3 a = _v[_tri[t]], b = _v[_tri[t + 1]], c = _v[_tri[t + 2]];
                Vector3Int lo = CellOf(Vector3.Min(a, Vector3.Min(b, c))), hi = CellOf(Vector3.Max(a, Vector3.Max(b, c)));
                for (int x = lo.x; x <= hi.x; x++)
                for (int y = lo.y; y <= hi.y; y++)
                for (int z = lo.z; z <= hi.z; z++)
                {
                    var k = new Vector3Int(x, y, z);
                    if (!g.TryGetValue(k, out var list)) g[k] = list = new List<int>();
                    list.Add(t);
                }
            }
            Grids[_mesh] = g;
            return g;
        }

        /// <summary>The first place a ray from inside the body leaves the skin at rest (within 0.5 m), and the face's normal there (outward).</summary>
        bool Ray(Vector3 from, Vector3 dir, out Vector3 hit, out Vector3 normal)
        {
            hit = normal = Vector3.zero;
            if (_grid == null) { _grid = Grid(); _stamp = new int[_tri.Length / 3]; }
            _ray++;
            float best = float.MaxValue;
            for (float s = 0f; s < 0.5f && s < best + Cell * 2f; s += Cell * 0.5f)
            {
                var k = CellOf(from + dir * s);
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (!_grid.TryGetValue(new Vector3Int(k.x + dx, k.y + dy, k.z + dz), out var list)) continue;
                    foreach (int t in list)
                    {
                        if (_stamp[t / 3] == _ray) continue;
                        _stamp[t / 3] = _ray;
                        TestTriangle(t, from, dir, ref best, ref normal);
                    }
                }
            }
            if (best == float.MaxValue) return false;
            hit = from + dir * best;
            if (Vector3.Dot(normal, dir) < 0f) normal = -normal;
            return true;
        }

        void TestTriangle(int t, Vector3 from, Vector3 dir, ref float best, ref Vector3 normal)
        {
            {
                Vector3 a = _v[_tri[t]], b = _v[_tri[t + 1]], c = _v[_tri[t + 2]];
                Vector3 e1 = b - a, e2 = c - a, pv = Vector3.Cross(dir, e2);
                float det = Vector3.Dot(e1, pv);
                if (Mathf.Abs(det) < 1e-12f) return;
                float inv = 1f / det;
                Vector3 tv = from - a;
                float u = Vector3.Dot(tv, pv) * inv;
                if (u < 0f || u > 1f) return;
                Vector3 qv = Vector3.Cross(tv, e1);
                float w = Vector3.Dot(dir, qv) * inv;
                if (w < 0f || u + w > 1f) return;
                float d = Vector3.Dot(e2, qv) * inv;
                if (d <= 1e-5f || d >= best) return;
                best = d;
                normal = Vector3.Cross(e1, e2).normalized;
            }
        }

        // meshes that follow the skin, made once per body mesh (and gear setting) and shared by every slug
        static readonly Dictionary<string, Mesh> Shared = new Dictionary<string, Mesh>();

        Mesh SharedMesh(string key, System.Func<SlugEquipment.GearMesh> make, Matrix4x4 toParent)
        {
            key = _mesh.GetHashCode() + "/" + key;
            if (Shared.TryGetValue(key, out var m) && m) return m;
            var mb = make();
            mb.Move(p => toParent.MultiplyPoint3x4(p));
            m = mb.ToMesh(key);
            m.hideFlags = HideFlags.DontSave;
            Shared[key] = m;
            return m;
        }

        GameObject Add(string name, Mesh mesh, Material mat, Transform parent, bool helmetPart = false)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave, layer = slug.gameObject.layer };
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            _owned.Add(go);
            if (helmetPart) _helmetRenderers.Add(r);
            return go;
        }

        /// <summary>A material for gear: the body's toon shader (shade, rim, outline), without the skin's pattern.</summary>
        Material GearMaterial(Color c)
        {
            var src = _skin.sharedMaterial;
            var m = Own(src ? new Material(src) : new Material(Shader.Find("Slug/Toon")));
            m.name = "Gear";
            void F(string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
            void C(string p, Color v) { if (m.HasProperty(p)) m.SetColor(p, v); }
            C("_BaseColor", c);
            C("_ShadeColor", new Color(0.66f, 0.66f, 0.72f));
            C("_OutlineColor", Color.Lerp(c, Color.black, 0.6f));
            C("_RimColor", Color.Lerp(c, Color.white, 0.35f));
            F("_EyeKind", 0f); F("_BellyAmount", 0f); F("_PatternType", 0f); F("_WrinkleAmount", 0f); F("_MottleAmount", 0f);
            F("_BumpAmp", 0f); F("_FollowMode", 0f); F("_SwayAmp", 0f); F("_SkirtAmp", 0f); F("_WaveAmp", 0f); F("_CamoAmount", 0f); F("_IslandLook", 0f);
            if (m.HasProperty("_FaceC")) m.SetVector("_FaceC", Vector4.zero);
            if (m.HasProperty("_FaceMouth")) m.SetVector("_FaceMouth", Vector4.zero);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
            return m;
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

        // ------------------------------------------------------------------ helmet
        void HeadBall(out Vector3 c, out Vector3 r)
        {
            // the head: the skin carried by the head bone
            int hb = System.Array.IndexOf(_skin.bones, slug.head);
            var w = _mesh.boneWeights;
            var b = new Bounds();
            bool first = true;
            for (int i = 0; i < _v.Length; i++)
            {
                if (w[i].boneIndex0 != hb || w[i].weight0 < 0.6f) continue;
                if (first) { b = new Bounds(_v[i], Vector3.zero); first = false; } else b.Encapsulate(_v[i]);
            }
            c = b.center; r = b.extents;
        }

        /// <summary>The antenna on side s (-1 left, +1 right): its root and tip (body space at rest), the stalk's and the ball's radii.</summary>
        bool Antenna(float s, out Transform t, out Vector3 root, out Vector3 tip, out float stalk, out float ball)
        {
            t = slug.head.Find(s < 0f ? "Antenna_L" : "Antenna_R");
            root = tip = Vector3.zero; stalk = ball = 0f;
            if (!t || !t.TryGetComponent<MeshFilter>(out var mf) || !mf.sharedMesh || !mf.sharedMesh.isReadable) return false;
            // (the antenna's mesh is in body space at rest: it was made in place and its transform undoes the head's)
            var v = mf.sharedMesh.vertices;
            float minY = float.MaxValue;
            foreach (var p in v) minY = Mathf.Min(minY, p.y);
            Vector3 sum = Vector3.zero; int k = 0;
            foreach (var p in v) if (p.y < minY + 0.006f) { sum += p; k++; }
            root = sum / Mathf.Max(k, 1);
            foreach (var p in v) if ((p - root).sqrMagnitude > (tip - root).sqrMagnitude) tip = p;
            Vector3 axis = (tip - root).normalized;
            float len = Vector3.Distance(root, tip);
            float sr = 0f, sb = 0f; int ks = 0;
            foreach (var p in v)
            {
                float along = Vector3.Dot(p - root, axis);
                float off = (p - root - axis * along).magnitude;
                if (along > 0.01f && along < len * 0.5f) { sr += off; ks++; }
                sb = Mathf.Max(sb, off);
            }
            stalk = ks > 0 ? sr / ks : 0.006f;
            ball = sb;
            return true;
        }

        void BuildHelmet()
        {
            HeadBall(out var c, out var hr);
            Vector3 axis = Vector3.up, right = Vector3.right, back = Vector3.back;   // the face is -back
            const float thick = 0.008f;
            float gap = helmetGap;
            // how far down the dome comes at each side (from the top): to just above the eyes in front, to the nape behind
            float Reach(float phi)
            {
                float s = Mathf.Sin(phi);
                return (86f - 26f * Mathf.Max(0f, -s) + 16f * Mathf.Max(0f, s)) * Mathf.Deg2Rad;
            }
            Vector3 Dir(float theta, float phi) => (axis * (hr.y * Mathf.Cos(theta))
                                                  + (right * (hr.x * Mathf.Cos(phi)) + back * (hr.z * Mathf.Sin(phi))) * Mathf.Sin(theta)).normalized;
            // the skin under each point of the dome (smoothed: the helmet is a pot, not a cast of the head)
            const int nT = 10, nP = 28;
            var reachR = new float[nT + 1, nP];
            Vector3 Dome(int i, int j, float grow)
            {
                float phi = j / (float)nP * Mathf.PI * 2f, theta = Reach(phi) * i / nT;
                return c + Dir(theta, phi) * (reachR[i, j] + gap + grow);
            }
            var mesh = SharedMesh($"Helmet/{gap:F3}/{brimFront:F3}/{brimSide:F3}", () =>
            {
                for (int i = 0; i <= nT; i++)
                for (int j = 0; j < nP; j++)
                {
                    float phi = j / (float)nP * Mathf.PI * 2f, theta = Reach(phi) * i / nT;
                    Vector3 d = Dir(theta, phi);
                    reachR[i, j] = Ray(c, d, out var hit, out _) ? Vector3.Distance(c, hit) : Mathf.Max(hr.x, hr.y);
                }
                // a light smoothing round each ring and down each meridian
                for (int pass = 0; pass < 2; pass++)
                {
                    var sm = (float[,])reachR.Clone();
                    for (int i = 0; i <= nT; i++)
                    for (int j = 0; j < nP; j++)
                    {
                        float sum = reachR[i, j] * 2f + reachR[i, (j + 1) % nP] + reachR[i, (j + nP - 1) % nP];
                        int n = 4;
                        if (i > 0) { sum += reachR[i - 1, j]; n++; }
                        if (i < nT) { sum += reachR[i + 1, j]; n++; }
                        sm[i, j] = sum / n;
                    }
                    reachR = sm;
                }
                var mb = new SlugEquipment.GearMesh();
                var outer = new int[nT + 1, nP];
                var inner = new int[nT + 1, nP];
                for (int i = 0; i <= nT; i++)
                for (int j = 0; j < nP; j++)
                {
                    outer[i, j] = mb.Vert(Dome(i, j, 0f));
                    inner[i, j] = mb.Vert(Dome(i, j, -thick));
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
                    float phi = j / (float)nP * Mathf.PI * 2f;
                    Vector3 rim = Dome(nT, j, 0f);
                    Vector3 radial = Vector3.ProjectOnPlane(rim - c, axis).normalized;
                    float len = Mathf.Lerp(brimSide, brimFront, Mathf.Max(0f, -Mathf.Sin(phi)));
                    Vector3 edge = rim + (radial * 0.94f - axis * 0.34f).normalized * len;
                    brimIn[j] = mb.Vert(rim);
                    brimOut[j] = mb.Vert(edge);
                    brimOutLow[j] = mb.Vert(edge - axis * thick);
                    rimIn[j] = mb.Vert(Dome(nT, j, -thick));
                }
                for (int j = 0; j < nP; j++)
                {
                    int k = (j + 1) % nP;
                    mb.Quad(brimIn[j], brimOut[j], brimOut[k], brimIn[k], c - axis * 0.3f, true);           // top of the brim
                    mb.Quad(brimOut[j], brimOutLow[j], brimOutLow[k], brimOut[k], c, true);                 // its edge
                    mb.Quad(brimOutLow[j], rimIn[j], rimIn[k], brimOutLow[k], c + axis * 0.3f, true);       // underneath
                }
                return mb;
            }, ToBone(slug.head));
            var mat = GearMaterial(helmetColor);
            mat.SetFloat("_RimAmount", 0.25f);
            if (helmetCamo) Camo(mat);
            Add("Helmet", mesh, mat, slug.head, true);

            foreach (float side in new[] { -1f, 1f })
            {
                if (!Antenna(side, out var at, out var root, out var tip, out float stalk, out float ball)) continue;
                Vector3 earAxis = (tip - root).normalized;
                float earLen = Vector3.Distance(root, tip);
                // walk up the antenna until it leaves the dome
                float s = 0f;
                for (; s < earLen; s += 0.002f)
                {
                    Vector3 q = root + earAxis * s - c;
                    Vector3 d = q.normalized;
                    float r = Ray(c, d, out var hit, out _) ? Vector3.Distance(c, hit) + gap : 0f;
                    if (q.magnitude > r) break;
                }
                string sideName = side < 0f ? "L" : "R";
                if (tentacleHelmet)
                {
                    // a horn sheathing the antenna from the dome to past its ball; it rides the antenna (droops with it)
                    var horn = SharedMesh($"Horn{sideName}/{hornGap:F3}/{hornExtend:F3}", () =>
                    {
                        var mb = new SlugEquipment.GearMesh();
                        Vector3 a0 = root + earAxis * s, a1 = tip - earAxis * ball + earAxis * hornExtend * 0.5f;
                        Tube(mb, a0, a1, stalk + hornGap + 0.004f, stalk + hornGap + 0.003f, 14, 6);
                        mb.RoundedBox(a1, Vector3.right, earAxis, Vector3.forward, Vector3.one * (ball + hornGap + 0.004f), 1f, 10, 16);
                        return mb;
                    }, Matrix4x4.identity);
                    Add("HelmetHorn_" + sideName, horn, mat, at, true);
                }
                else
                {
                    // a grommet where the antenna comes out through the steel
                    var ring = SharedMesh($"Grommet{sideName}/{gap:F3}", () =>
                    {
                        var mb = new SlugEquipment.GearMesh();
                        mb.Torus(root + earAxis * s, earAxis, stalk + 0.007f, 0.0045f, 14, 6);
                        return mb;
                    }, ToBone(slug.head));
                    Add("HelmetGrommet_" + sideName, ring, GearMaterial(Color.Lerp(helmetColor, Color.black, 0.45f)), slug.head, true);
                }
            }
        }

        /// <summary>A tube from a to b, radius ra to rb.</summary>
        static void Tube(SlugEquipment.GearMesh mb, Vector3 a, Vector3 b, float ra, float rb, int seg, int rows)
        {
            Vector3 ax = (b - a).normalized;
            Vector3 u = Vector3.Cross(ax, Mathf.Abs(ax.y) < 0.9f ? Vector3.up : Vector3.right).normalized, w = Vector3.Cross(ax, u);
            var idx = new int[rows + 1, seg];
            for (int i = 0; i <= rows; i++)
            {
                float k = i / (float)rows;
                Vector3 c = Vector3.Lerp(a, b, k);
                float r = Mathf.Lerp(ra, rb, k);
                for (int j = 0; j < seg; j++)
                {
                    float t = j / (float)seg * Mathf.PI * 2f;
                    idx[i, j] = mb.Vert(c + (u * Mathf.Cos(t) + w * Mathf.Sin(t)) * r);
                }
            }
            for (int i = 0; i < rows; i++)
            for (int j = 0; j < seg; j++)
                mb.Quad(idx[i, j], idx[i + 1, j], idx[i + 1, (j + 1) % seg], idx[i, (j + 1) % seg], Vector3.Lerp(a, b, (i + 0.5f) / rows), true);
        }

        // ------------------------------------------------------------------ plates on the skin
        /// <summary>
        /// A plate following the skin at rest: rows from height h0 to h1, each spanning ±<paramref name="halfSpan"/>(u)
        /// degrees about <paramref name="centerAngle"/>, standing <paramref name="gap"/> off the skin and
        /// <paramref name="thick"/> thick, bulging by <paramref name="dome"/> in the middle.
        /// </summary>
        void Plate(SlugEquipment.GearMesh mb, float h0, float h1, float centerAngle, System.Func<float, float> halfSpan, float gap, float thick,
                   int nT = 8, int nA = 16, float dome = 0f)
        {
            var outer = new int[nT + 1, nA + 1];
            var inner = new int[nT + 1, nA + 1];
            for (int i = 0; i <= nT; i++)
            {
                float u = i / (float)nT, h = Mathf.Lerp(h0, h1, u), span = halfSpan(u);
                for (int j = 0; j <= nA; j++)
                {
                    float ang = centerAngle + Mathf.Lerp(-span, span, j / (float)nA);
                    Skin(h, ang, out var p, out var n);
                    Vector3 o = Round(h, ang, out _);
                    n = (n + o).normalized;   // between the face's normal and straight out: smoother over little bumps
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u) * nT, Mathf.Min(j, nA - j) * nT / (float)nA);
                    float tk = thick * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(edge));
                    float du = u * 2f - 1f, dv = j / (float)nA * 2f - 1f;
                    tk += dome * Mathf.Clamp01(1f - du * du - dv * dv);
                    outer[i, j] = mb.Vert(p + n * (gap + tk));
                    inner[i, j] = mb.Vert(p + n * gap);
                }
            }
            Vector3 Center(int i) => Axis(Mathf.Lerp(h0, h1, i / (float)nT), out _);
            for (int i = 0; i < nT; i++)
            for (int j = 0; j < nA; j++)
            {
                Vector3 ctr = Center(i);
                mb.Quad(outer[i, j], outer[i + 1, j], outer[i + 1, j + 1], outer[i, j + 1], ctr, true);
                mb.Quad(inner[i, j], inner[i + 1, j], inner[i + 1, j + 1], inner[i, j + 1], ctr, false);
            }
            Vector3 mid = Vector3.zero;
            foreach (int v in outer) mid += mb.Pos(v);
            mid /= outer.Length;
            void Edge(int a0, int a1, int b0, int b1)
                => mb.Quad(mb.Copy(outer[a0, a1]), mb.Copy(outer[b0, b1]), mb.Copy(inner[b0, b1]), mb.Copy(inner[a0, a1]), mid, true);
            for (int i = 0; i < nT; i++) { Edge(i, 0, i + 1, 0); Edge(i, nA, i + 1, nA); }
            for (int j = 0; j < nA; j++) { Edge(0, j, 0, j + 1); Edge(nT, j, nT, j + 1); }
        }

        /// <summary>A flat strap laid on the skin along <paramref name="path"/> (height, angle) points, <paramref name="half"/> wide each side.</summary>
        void Strap(SlugEquipment.GearMesh mb, IList<Vector2> path, float half, float gap, float thick, bool level = false)
        {
            int n = path.Count;
            var pts = new Vector3[n]; var nrm = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                // (level: round a vertical line through the spine line at that height, so a belt lies flat round the waist)
                Vector3 dir = Round(path[i].x, path[i].y, out var from);
                if (level)
                {
                    float a = path[i].y * Mathf.Deg2Rad;
                    dir = new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a));
                }
                if (!Ray(from, dir, out pts[i], out var fn)) { pts[i] = from + dir * 0.12f; fn = dir; }
                nrm[i] = (fn + dir).normalized;
            }
            var rows = new int[n, 4];
            for (int i = 0; i < n; i++)
            {
                Vector3 tan = (pts[Mathf.Min(i + 1, n - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 side = Vector3.Cross(nrm[i], tan).normalized * half;
                Vector3 b = pts[i] + nrm[i] * gap, t = b + nrm[i] * thick;
                rows[i, 0] = mb.Vert(b - side); rows[i, 1] = mb.Vert(b + side);
                rows[i, 2] = mb.Vert(t + side); rows[i, 3] = mb.Vert(t - side);
            }
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 inside = (pts[i] + pts[i + 1]) * 0.5f - (nrm[i] + nrm[i + 1]) * 0.05f;
                for (int k = 0; k < 4; k++)
                {
                    int k2 = (k + 1) % 4;
                    mb.Quad(rows[i, k], rows[i + 1, k], rows[i + 1, k2], rows[i, k2], inside, k != 0);
                }
            }
        }

        // ------------------------------------------------------------------ shell plate armour
        void BuildShellPlate()
        {
            var toChest = ToBone(slug.chest);
            // the mantle shield: a big domed oval over the upper back, kept off the arms at the sides
            var shells = SharedMesh("ShellPlate", () =>
            {
                var mb = new SlugEquipment.GearMesh();
                Plate(mb, ShieldFrom, ShieldTo, 0f, u => 40f + 36f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(u * 2f - 1f, 2f))), 0.012f, 0.016f, 14, 22, 0.02f);
                // the chest plate in front, under the mouth
                Plate(mb, PlateFrom, PlateTo, 180f, u => 30f + 16f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(u * 2f - 1f, 2f))), 0.008f, 0.012f, 12, 14, 0.008f);
                return mb;
            }, toChest);
            var mat = Camo(GearMaterial(camoBase));
            mat.SetFloat("_RimAmount", 0.2f);
            Add("ShellPlate", shells, mat, slug.chest);

            // straps over the shoulders, above the arms, holding the front and the back together (the bag's own straps
            // take their place when it is worn)
            if (bag) return;
            var straps = SharedMesh("ShellPlateStraps", () =>
            {
                var mb = new SlugEquipment.GearMesh();
                foreach (float side in new[] { -1f, 1f })
                {
                    var path = new List<Vector2>();
                    for (int i = 0; i <= 8; i++) path.Add(new Vector2(ShoulderH, side * Mathf.Lerp(55f, 150f, i / 8f)));
                    Strap(mb, path, 0.011f, 0.006f, 0.005f);
                }
                return mb;
            }, toChest);
            Add("ShellPlateStraps", straps, GearMaterial(strapColor), slug.chest);
        }

        // ------------------------------------------------------------------ snail shell
        void BuildSnailShell()
        {
            const float h = 0.3f, rMax = 0.11f, turns = 3.2f, tubeRatio = 0.85f;
            Skin(h, 0f, out var p, out var nrm);
            Axis(h, out var line);
            Vector3 x = Vector3.right, nOut = Vector3.ProjectOnPlane(nrm, x).normalized;
            Vector3 up = Vector3.ProjectOnPlane(line, nOut).normalized;
            const float thEnd = 0.75f * Mathf.PI + 2f * Mathf.PI * turns, thStart = 0.75f * Mathf.PI;
            float k = Mathf.Log(2.2f) / (2f * Mathf.PI);
            Vector3 Radial(float theta) => nOut * Mathf.Cos(theta) - up * Mathf.Sin(theta);
            Vector3 end = p + nOut * (rMax * tubeRatio * 0.3f);
            Vector3 o = end - Radial(thEnd) * rMax;
            Vector3 Center(float theta, out Vector3 radial, out float tube)
            {
                float r = rMax * Mathf.Exp(k * (theta - thEnd));
                tube = r * tubeRatio;
                radial = Radial(theta);
                return o + radial * r + x * ((rMax - r) * 1.3f);
            }
            var toChest = ToBone(slug.chest);
            var shell = SharedMesh("SnailShell", () =>
            {
                const int steps = 150, ring = 12;
                var mb = new SlugEquipment.GearMesh();
                var rows = new List<int[]>();
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
                    float theta = Mathf.Lerp(thStart, thEnd, (i + 0.5f) / steps);
                    mb.Quad(rows[i][j], rows[i + 1][j], rows[i + 1][(j + 1) % ring], rows[i][(j + 1) % ring], Center(theta, out _, out _), true);
                }
                return mb;
            }, toChest);
            var mat = Camo(GearMaterial(camoBase));
            mat.SetFloat("_RimAmount", 0.2f);
            Add("SnailShell", shell, mat, slug.chest);
            var lip = SharedMesh("SnailShellLip", () =>
            {
                var mb = new SlugEquipment.GearMesh();
                Vector3 lipC = Center(thEnd, out var endRadial, out float endTube);
                mb.Torus(lipC, Vector3.Cross(endRadial, x).normalized, endTube * 0.98f, 0.012f, 18, 6);
                return mb;
            }, toChest);
            Add("SnailShellLip", lip, GearMaterial(new Color(0.86f, 0.8f, 0.66f)), slug.chest);
            SetBackItem(o + x * (rMax * 0.3f), nrm, 1f, Quaternion.LookRotation(nOut, up), new Vector3(rMax * 1.2f, rMax * 1.55f, rMax * 1.55f));
        }

        // ------------------------------------------------------------------ bag (shouldered)
        void BuildBag()
        {
            Skin(BagAt, 0f, out var p, out var nrm);
            Axis(BagAt, out var line);
            Vector3 nOut = nrm.normalized;
            // it hangs nearly upright from the shoulders, whatever the body's lean
            Vector3 up = Vector3.ProjectOnPlane(Vector3.Lerp(line, Vector3.up, 0.6f), nOut).normalized;
            Vector3 x = Vector3.Cross(up, nOut).normalized;
            float standOff = armor == Armor.ShellPlate ? ShieldStandOff : 0.008f;   // on top of the shield when worn
            Vector3 sz = SlugEquipment.SwollenSize(bagSize, bagSwell);
            Vector3 c = p + nOut * (standOff + sz.z);
            SetBackItem(c, nrm, 1f, Quaternion.LookRotation(nOut, up), sz * 1.05f);

            // everything about the bag in the chest bone's space (the bag rides it)
            var toChest = ToBone(slug.chest);
            Vector3 P(Vector3 v) => toChest.MultiplyPoint3x4(v);
            Vector3 D(Vector3 v) => toChest.MultiplyVector(v).normalized;
            Vector3 cL = P(c), xL = D(x), upL = D(up), outL = D(nOut);
            var frame = new CanvasBag.Frame(cL, xL, upL, outL);
            float fill = SlugEquipment.Fill(bagSwell);
            var shut = CanvasBag.Make(frame, bagSize, sz, fill);
            float mouthW = Mathf.Clamp(bagPeekMouthMax / Mathf.Max(sz.x, 1e-3f), 1f, Mathf.Lerp(bagPeekMouth, bagPeekMouthFull, fill));
            float rimW = 0f, rimZ0 = float.MaxValue, rimZ1 = float.MinValue;
            foreach (var v in shut.top.Positions())
            {
                rimW = Mathf.Max(rimW, Mathf.Abs(Vector3.Dot(v - cL, xL)));
                float vz = Vector3.Dot(v - cL, outL);
                rimZ0 = Mathf.Min(rimZ0, vz); rimZ1 = Mathf.Max(rimZ1, vz);
            }
            float mouthD = Mathf.Clamp(0.7f * rimW * mouthW * 2f / Mathf.Max(rimZ1 - rimZ0, 1e-3f), mouthW, 5f);
            frame.Taper(sz.y, sz.z, bagPeekBottom, mouthW, mouthD);
            var opened = CanvasBag.Make(frame, bagSize, sz, fill, true);
            float heapW = 0f, heapZ0 = float.MaxValue, heapZ1 = float.MinValue;
            foreach (var v in opened.heap.Positions())
            {
                heapW = Mathf.Max(heapW, Mathf.Abs(Vector3.Dot(v - cL, xL)));
                float vz = Vector3.Dot(v - cL, outL);
                heapZ0 = Mathf.Min(heapZ0, vz); heapZ1 = Mathf.Max(heapZ1, vz);
            }
            BagHeapHalf = new Vector2(heapW, (heapZ1 - heapZ0) * 0.5f);
            _bagX = xL; _bagUp = upL; _bagOut = outL;
            _bagMid = cL;
            _bagAttach = P(p + nOut * standOff);
            _chestRest = toChest.inverse;
            _bagHinge = shut.hinge; _bagHingeOpen = opened.hinge;
            _bagInside = shut.inside; _bagInsideOpen = opened.inside;

            var root = new GameObject("Bag") { hideFlags = HideFlags.DontSave, layer = slug.gameObject.layer };
            root.transform.SetParent(slug.chest, false);
            _owned.Add(root);
            _bagRoot = root.transform;
            var lid = new GameObject("BagLid") { hideFlags = HideFlags.DontSave, layer = slug.gameObject.layer };
            lid.transform.SetParent(_bagRoot, false);
            _bagLid = lid.transform;
            GameObject Part(string name, SlugEquipment.GearMesh mesh, SlugEquipment.GearMesh open, Material mat, Transform parent)
            {
                var m = Own(mesh.ToMesh(name));
                var go = new GameObject(name) { hideFlags = HideFlags.DontSave, layer = slug.gameObject.layer };
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                _bagParts.Add(go);
                _bagShapes.Add((m, m.vertices, open.Positions()));
                return go;
            }
            Material flapMat = GearMaterial(bagFlapColor), beltMat = GearMaterial(strapColor);
            Part("BagBody", shut.body, opened.body, GearMaterial(bagColor), _bagRoot);
            Part("BagInside", shut.top, opened.top, GearMaterial(Color.Lerp(bagColor, Color.black, 0.3f)), _bagRoot);
            _bagHeap = Part("BagHeap", shut.heap, opened.heap, GearMaterial(bagHeapColor), _bagRoot);
            _bagHeap.GetComponent<Renderer>().enabled = false;
            Part("BagSeams", shut.trim, opened.trim, GearMaterial(Color.Lerp(bagFlapColor, Color.black, 0.3f)), _bagRoot);
            Part("BagPocket", shut.pocket, opened.pocket, flapMat, _bagRoot);
            Part("BagStraps", shut.straps, opened.straps, beltMat, _bagRoot);
            Part("BagBuckles", shut.buckles, opened.buckles, GearMaterial(buckleColor), _bagRoot);
            Part("BagLidCloth", shut.lid, opened.lid, flapMat, _bagLid);
            Part("BagLidStraps", shut.lidStraps, opened.lidStraps, beltMat, _bagLid);
            _bagPosedOpen = 0f;

            // shoulder straps from the top of the bag over the shoulders and down the front to under the arms, and a
            // belt round the waist under the bag (over the armour when worn)
            float gap = armor == Armor.ShellPlate ? 0.016f : 0.004f;
            var belts = SharedMesh($"BagHarness/{gap:F3}", () =>
            {
                var mb = new SlugEquipment.GearMesh();
                foreach (float side in new[] { -1f, 1f })
                {
                    var path = new List<Vector2>();
                    for (int i = 0; i <= 12; i++)
                    {
                        float k = i / 12f;
                        path.Add(new Vector2(Mathf.Lerp(0.33f, 0.3f, k) + 0.02f * Mathf.Sin(Mathf.PI * k), side * Mathf.Lerp(30f, 148f, k)));
                    }
                    Strap(mb, path, 0.012f, gap + 0.004f, 0.005f);
                }
                var waist = new List<Vector2>();
                for (int i = 0; i <= 36; i++) waist.Add(new Vector2(WaistH, Mathf.Lerp(-180f, 180f, i / 36f)));
                Strap(mb, waist, 0.012f, 0.004f, 0.005f, true);
                return mb;
            }, toChest);
            Add("BagHarness", belts, beltMat, slug.chest);
        }

        public override void Jostle(float degreesPerSecond)
        {
            if (!_bagRoot) return;
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
            if (_rocking || bagOpen > 0f || _bagPosedOpen > 0f) PoseBag(bagOpen);
        }

        /// <summary>
        /// Poses the bag opened by <paramref name="open"/> (0..1), for everyone: tipped up about its middle, a little off
        /// the back, until its mouth faces <see cref="SlugGearBase.bagPeekZenith"/> from straight up, over to the right and
        /// a little to the front; the lid swung open about its hinge, the mouth spread with the heap showing.
        /// </summary>
        void PoseBag(float open)
        {
            if (!_bagRoot) return;
            _bagPosedOpen = open;
            Shape(open);
            var chest = slug.chest;
            Quaternion rot = Quaternion.identity;
            if (open > 0f)
            {
                // (in the chest bone's space: the world's up, the body's right and front)
                Vector3 to = PeekMouthDirection(chest.InverseTransformDirection(Vector3.up), chest.InverseTransformDirection(slug.transform.right),
                                                chest.InverseTransformDirection(slug.transform.forward));
                rot = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(_bagUp, to), open);
            }
            // tipped about its middle, and a little out from the back, so it never swings into the body (the back is
            // upright here: tipped on its bottom like the crawling slug's, it would sink into it)
            Vector3 pos = _bagMid + _bagOut * (BagPeekOut * open) - rot * _bagMid;
            if (_rock != 0f)
            {
                Quaternion rock = Quaternion.AngleAxis(_rock, _bagX);
                Vector3 pivot = pos + rot * _bagAttach;
                rot = rot * rock;
                pos = pivot - rot * _bagAttach;
            }
            _bagRoot.localPosition = pos;
            _bagRoot.localRotation = rot;
            if (open > 0f)
            {
                // opened, it is held still against the body rather than bobbing and twisting with the chest at each step,
                // so the eyes looking into it stay steady while walking
                Matrix4x4 still = slug.transform.localToWorldMatrix * _chestRest;
                Vector3 stillPos = still.MultiplyPoint3x4(pos);
                Quaternion stillRot = slug.transform.rotation * _chestRest.rotation * rot;
                _bagRoot.SetPositionAndRotation(Vector3.Lerp(_bagRoot.position, stillPos, open), Quaternion.Slerp(_bagRoot.rotation, stillRot, open));
            }
            if (_bagHeap)
            {
                var heapR = _bagHeap.GetComponent<Renderer>();
                bool heap = open > 0.02f && bagHasContents;
                if (heapR.enabled != heap) heapR.enabled = heap;
            }
            Vector3 hinge = Vector3.Lerp(_bagHinge, _bagHingeOpen, open);
            Quaternion swing = Quaternion.AngleAxis(bagLidAngle * open, _bagX);
            _bagLid.localPosition = hinge - swing * hinge;
            _bagLid.localRotation = swing;
            BagInsidePosition = _bagRoot.TransformPoint(Vector3.Lerp(_bagInside, _bagInsideOpen, open));
            BagMouthRotation = _bagRoot.rotation * Quaternion.LookRotation(_bagOut, _bagUp);
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

        // ------------------------------------------------------------------ vest
        /// <summary>
        /// The chest rig: a canvas panel on the chest under the mouth held by a strap round the chest, with one row of
        /// pouches (from the right of the chest to the left: the moss balls, the big gun's magazines, the small gun's).
        /// Each pouch has its thing in it (shown or not by <see cref="SetVestLoad"/>); an empty pouch shows its dark mouth.
        /// </summary>
        void BuildVest()
        {
            float gap = armor == Armor.ShellPlate ? 0.024f : 0.006f;
            const float thick = 0.01f;
            var toChest = ToBone(slug.chest);
            var panel = SharedMesh($"Vest/{gap:F3}", () =>
            {
                var mb = new SlugEquipment.GearMesh();
                Plate(mb, VestFrom, VestTo, 180f, u => 38f + 6f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(u * 2f - 1f, 2f))), gap, thick, 8, 18, 0.003f);
                return mb;
            }, toChest);
            Add("Vest", panel, GearMaterial(vestColor), slug.chest);
            // the pouches, laid out across the chest by their widths
            var mag = new Vector3(0.016f, 0.05f, 0.013f);
            var small = new Vector3(0.012f, 0.038f, 0.009f);
            const float mossR = 0.03f, space = 0.006f;
            var slots = new List<(int kind, float width)>();
            for (int i = 0; i < _mossPouches; i++) slots.Add((0, (mossR + 0.005f) * 2f));
            for (int i = 0; i < _bigPouches; i++) slots.Add((1, (mag.x + 0.005f) * 2f));
            for (int i = 0; i < _smallPouches; i++) slots.Add((2, (small.x + 0.005f) * 2f));
            float total = -space;
            foreach (var s in slots) total += s.width + space;
            Skin(VestRow, 180f, out var front, out _);
            float ringR = Vector3.Distance(Axis(VestRow, out _), front) + gap + thick;
            Material pouchMat = GearMaterial(vestPouchColor), dark = GearMaterial(new Color(0.07f, 0.06f, 0.05f));
            Material steel = GearMaterial(magColor), salt = GearMaterial(saltColor), moss = GearMaterial(mossColor);
            string key = $"{gap:F3}/{_mossPouches}{_bigPouches}{_smallPouches}";
            var pouchMeshes = new List<(string name, Mesh mesh, int kind, int part)>();
            float along = -total * 0.5f;
            int idx = 0;
            var pouches = new SlugEquipment.GearMesh();
            var mouths = new SlugEquipment.GearMesh();
            var things = new List<(string name, SlugEquipment.GearMesh mesh, int kind, Material mat)>();
            bool made = Shared.ContainsKey(_mesh.GetHashCode() + "/VestPouches/" + key);
            foreach (var s in slots)
            {
                along += s.width * 0.5f;
                // + along = toward the left of the chest (angle 180 + d)
                float ang = 180f + along / ringR * Mathf.Rad2Deg;
                Skin(VestRow, ang, out var p, out var n);
                n = (n + Round(VestRow, ang, out _)).normalized;
                Axis(VestRow, out var line);
                Vector3 up = Vector3.ProjectOnPlane(line, n).normalized;
                Vector3 x = Vector3.Cross(up, n).normalized;
                Vector3 surf = p + n * (gap + thick);
                if (s.kind == 0)
                {
                    var h = new Vector3(mossR + 0.005f, 0.02f, mossR * 0.85f);
                    Vector3 c = surf + n * h.z;
                    if (!made)
                    {
                        pouches.RoundedBox(c, x, up, n, h, 0.35f, 8, 16);
                        mouths.RoundedBox(c + up * h.y, x, up, n, new Vector3(h.x * 0.8f, 0.002f, h.z * 0.75f), 0.4f, 4, 12);
                    }
                    var ball = new SlugEquipment.GearMesh();
                    ball.RoundedBox(c + up * (h.y + 0.006f), x, up, n, Vector3.one * mossR, 1f, 10, 16);
                    things.Add(("VestMoss" + idx, ball, 0, moss));
                }
                else
                {
                    var m = s.kind == 1 ? mag : small;
                    var h = new Vector3(m.x + 0.005f, m.y * 0.72f, m.z + 0.006f);
                    Vector3 c = surf + n * h.z;
                    if (!made)
                    {
                        pouches.RoundedBox(c, x, up, n, h, 0.3f, 8, 16);
                        mouths.RoundedBox(c + up * h.y, x, up, n, new Vector3(h.x * 0.85f, 0.002f, h.z * 0.75f), 0.4f, 4, 12);
                    }
                    Vector3 top = c + up * (h.y + m.y * 0.6f);
                    var body = new SlugEquipment.GearMesh();
                    body.RoundedBox(top - up * m.y, x, up, n, m, 0.25f, 8, 16);
                    var lips = new SlugEquipment.GearMesh();
                    lips.RoundedBox(top + up * 0.002f, x, up, n, new Vector3(m.x * 0.75f, 0.005f, m.z * 0.6f), 0.5f, 4, 12);
                    things.Add(("VestMag" + idx, body, s.kind, steel));
                    things.Add(("VestMag" + idx + "Salt", lips, s.kind, salt));
                }
                along += s.width * 0.5f + space;
                idx++;
            }
            Add("VestPouches", SharedMesh("VestPouches/" + key, () => pouches, toChest), pouchMat, slug.chest);
            Add("VestPouchMouths", SharedMesh("VestPouchMouths/" + key, () => mouths, toChest), dark, slug.chest);
            // what rides in the pouches, one or two parts per pouch, in order
            var current = new List<GameObject>();
            int lastKind = -1;
            string lastPouch = null;
            foreach (var t in things)
            {
                string pouch = t.name.EndsWith("Salt") ? t.name.Substring(0, t.name.Length - 4) : t.name;
                var go = Add(t.name, SharedMesh(t.name + "/" + key, () => t.mesh, toChest), t.mat, slug.chest);
                if (pouch != lastPouch && current.Count > 0) { PouchList(lastKind).Add(current.ToArray()); current.Clear(); }
                current.Add(go);
                lastPouch = pouch; lastKind = t.kind;
            }
            if (current.Count > 0) PouchList(lastKind).Add(current.ToArray());
        }

        List<GameObject[]> PouchList(int kind) => kind == 0 ? _vestMoss : kind == 1 ? _vestBig : _vestSmall;

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
    }
}
