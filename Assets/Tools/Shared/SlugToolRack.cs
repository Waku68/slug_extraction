using System.Collections.Generic;
using SlugDesign;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// The tools that are not in hand, riding on the body where they are carried: the big gun slung across the back
    /// (over the bag, if one is worn) with the pickaxe crossed under it, and the small gun in a holster or the knife in its
    /// sheath on the right hip. They move with the body as it crawls, stands up and turns, like the hitboxes do.
    ///
    /// The holders take a tool from here (<see cref="TryStowPose"/>): the hand reaches for it where it rides, and it
    /// comes off the body into the hand, so nothing pops in or out. Everyone else sees what a slug carries; for the
    /// player's own camera in first person they only cast their shadow (they would poke into the view).
    /// Runs after the body has moved and before the holders.
    /// </summary>
    [DefaultExecutionOrder(95)]
    public class SlugToolRack : MonoBehaviour
    {
        public SlugLoadout loadout;
        public SlugBodyBase slug;
        public SlugCameraRig cameraRig;

        /// <summary>A place on the body (body space at rest, and the skin's t and normal there for following it).</summary>
        struct Seat
        {
            public Vector3 position, normal;
            public Quaternion rotation;
            public float t;
        }

        SlugPickaxe _pickaxe;
        readonly Dictionary<GunKind, SlugGunModel> _guns = new Dictionary<GunKind, SlugGunModel>();
        SlugKnife _knife;
        Seat _pickSeat, _knifeSeat;
        readonly Dictionary<GunKind, Seat> _gunSeats = new Dictionary<GunKind, Seat>();
        readonly Dictionary<Object, ToolPose> _poses = new Dictionary<Object, ToolPose>();
        readonly List<Renderer> _renderers = new List<Renderer>();
        object _seatedFor;
        float _reseatTimer;
        bool _ready;
        static Material _leather;
        static Mesh _holsterMesh;

        LoadoutSettings Settings => loadout ? loadout.settings : null;

        void OnEnable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        void OnDisable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;

        void Start()
        {
            if (!loadout || !slug) return;
            if (loadout.pickaxe) _pickaxe = SlugPickaxe.Create("Stowed_Pickaxe", null, true);
            foreach (var g in loadout.guns)
            {
                if (!g || !g.gun || _guns.ContainsKey(g.gun.kind)) continue;
                var model = SlugGunModel.Create(g.gun.kind, "Stowed_" + g.gun.kind, null, true);
                if (g.gun.Small) Holster(model.transform);
                _guns[g.gun.kind] = model;
            }
            if (loadout.knife)
            {
                _knife = SlugKnife.Create("Stowed_Knife", null, true);
                SlugKnife.Sheath(_knife.transform, true);
            }
            foreach (var go in Models())
            {
                SlugBodyPart.Mark(go, slug.transform);
                _renderers.AddRange(go.GetComponentsInChildren<Renderer>(true));
                go.SetActive(false);
            }
            _ready = true;
        }

        void OnDestroy()
        {
            foreach (var go in Models()) if (go) Destroy(go);
        }

        IEnumerable<GameObject> Models()
        {
            if (_pickaxe) yield return _pickaxe.gameObject;
            foreach (var m in _guns.Values) if (m) yield return m.gameObject;
            if (_knife) yield return _knife.gameObject;
        }

        void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (!_ready || !cameraRig) return;
            var mode = camera == cameraRig.cam && cameraRig.OwnToolsHidden
                ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var r in _renderers) if (r) r.shadowCastingMode = mode;
        }

        /// <summary>
        /// Where <paramref name="holder"/>'s tool rides on the body this frame (world, in the tool model's own space).
        /// False before the body is built.
        /// </summary>
        public bool TryStowPose(Object holder, out ToolPose pose) => _poses.TryGetValue(holder, out pose);

        /// <summary>The gear worn (the bag or shell on the back), if any.</summary>
        public SlugGearBase Gear => _gear ? _gear : (_gear = slug ? slug.GetComponent<SlugGearBase>() : null);
        SlugGearBase _gear;
        // the box of what is worn on the back this frame (world; x across the back, y up it, z out from it)
        bool _hasBox;
        Vector3 _boxCenter, _boxHalf;
        Quaternion _boxRotation;

        /// <summary>
        /// How far <paramref name="world"/> has to move to come out of what is worn on the back (the bag as swollen as it
        /// is now, or the shell), keeping <paramref name="margin"/> off it. Anything over it is lifted onto its outside (away
        /// from the back), so a hand or tool passing over a big bag goes over it rather than round its edge. Zero when the
        /// point is clear of it, or nothing is worn.
        /// </summary>
        public Vector3 OutOfBack(Vector3 world, float margin)
        {
            if (!_hasBox) return Vector3.zero;
            Vector3 p = Quaternion.Inverse(_boxRotation) * (world - _boxCenter);
            Vector3 h = _boxHalf + Vector3.one * margin;
            if (Mathf.Abs(p.x) >= h.x || Mathf.Abs(p.y) >= h.y || p.z >= h.z || p.z <= -_boxHalf.z) return Vector3.zero;
            return _boxRotation * new Vector3(0f, 0f, h.z - p.z);
        }

        /// <summary>Follows the box of what is worn on the back with the body (it bends and stretches with the skin).</summary>
        void FollowBox()
        {
            var gear = Gear;
            _hasBox = gear && gear.HasBackItem;
            if (!_hasBox) return;
            var s = new Seat { position = gear.BackItemCenter, rotation = gear.BackItemRotation, normal = gear.BackItemNormal, t = gear.BackItemT };
            var pose = Follow(s);
            _boxCenter = pose.position;
            _boxRotation = pose.rotation;
            _boxHalf = gear.BackItemHalf * (slug.VisualRoot ? slug.VisualRoot.lossyScale.x : 1f);
        }

        /// <summary>The renderers of the guns riding on the body now (not the pickaxe or the knife), one list per gun,
        /// for leaving them behind when the slug falls.</summary>
        public void CollectGuns(List<List<Renderer>> pieces)
        {
            foreach (var g in _guns.Values)
                if (g && g.gameObject.activeInHierarchy) pieces.Add(new List<Renderer>(g.GetComponentsInChildren<Renderer>()));
        }

        // ------------------------------------------------------------------ every frame: follow the body
        void LateUpdate()
        {
            _poses.Clear();
            if (!_ready || !Settings || slug.ShapeKey == null) { Hide(); return; }
            _reseatTimer -= Time.deltaTime;
            if (_seatedFor != slug.ShapeKey || _reseatTimer <= 0f) Reseat();
            FollowBox();

            // with something else in the hands, every tool rides on the body; one being put away is still in the hand
            int held = loadout.HandsFull ? -1 : loadout.InHand;
            if (loadout.pickaxe)
            {
                var pose = Follow(_pickSeat);
                _poses[loadout.pickaxe] = pose;
                Show(_pickaxe ? _pickaxe.gameObject : null, held != SlugLoadout.PickaxeSlot, pose);
            }
            var back = loadout.BackGun;
            var hipGun = loadout.HipGun;
            foreach (var g in loadout.guns)
            {
                if (!g || !g.gun || !_gunSeats.TryGetValue(g.gun.kind, out var seat)) continue;
                var pose = Follow(seat);
                _poses[g] = pose;
            }
            foreach (var kv in _guns)
            {
                bool carried = (back && back.gun.kind == kv.Key && held != SlugLoadout.BackSlot)
                               || (hipGun && hipGun.gun.kind == kv.Key && held != SlugLoadout.HipSlot);
                Show(kv.Value.gameObject, carried, _gunSeats.TryGetValue(kv.Key, out var seat) ? Follow(seat) : default);
            }
            if (loadout.knife)
            {
                var pose = Follow(_knifeSeat);
                _poses[loadout.knife] = pose;
                Show(_knife ? _knife.gameObject : null, loadout.hip == HipTool.Knife && held != SlugLoadout.HipSlot, pose);
            }
        }

        void Hide()
        {
            foreach (var go in Models()) if (go.activeSelf) go.SetActive(false);
        }

        static void Show(GameObject go, bool on, ToolPose pose)
        {
            if (!go) return;
            if (go.activeSelf != on) go.SetActive(on);
            if (on) go.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        /// <summary>A seat carried with the skin: its origin and two axis points follow the body, which gives the turn.</summary>
        ToolPose Follow(Seat s)
        {
            Vector3 p = slug.FollowPointWorld(s.position, s.t, s.normal);
            Vector3 z = slug.FollowPointWorld(s.position + s.rotation * Vector3.forward * 0.1f, s.t, s.normal) - p;
            Vector3 y = slug.FollowPointWorld(s.position + s.rotation * Vector3.up * 0.1f, s.t, s.normal) - p;
            return new ToolPose { position = p, rotation = z.sqrMagnitude > 1e-10f ? Quaternion.LookRotation(z, y) : slug.transform.rotation };
        }

        // ------------------------------------------------------------------ where each thing rides (body space at rest)
        void Reseat()
        {
            var s = Settings;
            _seatedFor = slug.ShapeKey;
            _reseatTimer = 0.25f;      // cheap enough to pick up tuning in play mode
            var gear = slug.GetComponent<SlugGearBase>();

            // ---- the back: over the bag (or the shell) when one is worn, else on the skin
            if (slug.CarrySpot(s.backGunT, 0f, out var bp, out var bn, out var along, out var right, out _, out var bt))
            {
                float outer = gear && gear.HasBackItem ? Mathf.Max(0f, Vector3.Dot(gear.BackItemCenter - bp, bn) + gear.BackItemHalf.z) : 0f;
                foreach (var g in loadout.guns)
                {
                    if (!g || !g.gun || g.gun.Small || !_guns.TryGetValue(g.gun.kind, out var model)) continue;
                    // slung across the back, the right side of the gun facing out, the muzzle up past the right shoulder
                    Vector3 diag = (along * Mathf.Cos(s.backGunTilt * Mathf.Deg2Rad) + right * Mathf.Sin(s.backGunTilt * Mathf.Deg2Rad)).normalized;
                    Vector3 up = Vector3.Cross(diag, bn);
                    Vector3 center = bp + right * s.backGunShift + bn * (outer + s.backGunStandOff + 0.024f);
                    Vector3 origin = center - diag * (model.MuzzleLocal.z * s.backGunAlong) - up * (model.BoreY * 0.5f);
                    _gunSeats[g.gun.kind] = new Seat { position = origin, rotation = Quaternion.LookRotation(diag, up), normal = bn, t = bt };
                }
            }
            if (slug.CarrySpot(s.pickaxeT, 0f, out var pp, out var pn, out var pAlong, out var pRight, out _, out var pt))
            {
                float outer = gear && gear.HasBackItem ? Mathf.Max(0f, Vector3.Dot(gear.BackItemCenter - pp, pn) + gear.BackItemHalf.z) : 0f;
                // the handle the other way across the back (an X with the gun), the head flat against it over the left shoulder
                Vector3 handle = (pAlong * Mathf.Cos(s.pickaxeTilt * Mathf.Deg2Rad) + pRight * Mathf.Sin(s.pickaxeTilt * Mathf.Deg2Rad)).normalized;
                Vector3 head = Vector3.Cross(pn, handle);
                Vector3 origin = pp + pRight * s.pickaxeShift + pn * (outer + s.pickaxeStandOff + 0.018f) - handle * (SlugPickaxe.HandleLength * s.pickaxeAlong);
                _pickSeat = new Seat { position = origin, rotation = Quaternion.LookRotation(head, handle), normal = pn, t = pt };
            }

            // ---- the right hip, at the belt: pointing down the body, tilted a little forward, the grip out above the belt
            if (slug.CarrySpot(s.hipT, s.hipAngle, out var hp, out var hn, out var hAlong, out _, out var belly, out var ht))
            {
                Vector3 front = Vector3.ProjectOnPlane(belly, hn).normalized;
                float a = s.hipTilt * Mathf.Deg2Rad;
                Vector3 down = (-hAlong * Mathf.Cos(a) + front * Mathf.Sin(a)).normalized;
                Vector3 up = Vector3.Cross(down, hn);
                foreach (var g in loadout.guns)
                {
                    if (!g || !g.gun || !g.gun.Small) continue;
                    Vector3 origin = hp + hn * (s.hipStandOff + 0.017f) - down * s.hipGripOut;
                    _gunSeats[g.gun.kind] = new Seat { position = origin, rotation = Quaternion.LookRotation(down, up), normal = hn, t = ht };
                }
                Vector3 knifeOrigin = hp + hn * (s.hipStandOff + 0.009f) - down * (s.hipGripOut + 0.02f);
                _knifeSeat = new Seat { position = knifeOrigin, rotation = Quaternion.LookRotation(down, up), normal = hn, t = ht };
            }
        }

        /// <summary>A leather holster over the slide and barrel of a small gun (in the gun's own space).</summary>
        static void Holster(Transform gun)
        {
            if (_holsterMesh == null)
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                _leather = new Material(lit) { hideFlags = HideFlags.DontSave };
                _leather.SetColor("_BaseColor", new Color(0.36f, 0.24f, 0.15f));
                _leather.SetFloat("_Smoothness", 0.2f);
                var m = new ProfileMesh();
                m.Extrude(new List<Vector2> { new Vector2(0.04f, -0.006f), new Vector2(0.212f, -0.006f), new Vector2(0.212f, 0.044f),
                                              new Vector2(0.2f, 0.053f), new Vector2(0.04f, 0.053f) }, 0.017f, 0.004f);
                // the belt loop at the back
                m.Extrude(new List<Vector2> { new Vector2(0.05f, 0.0f), new Vector2(0.09f, 0.0f), new Vector2(0.09f, 0.048f), new Vector2(0.05f, 0.048f) },
                          0.004f, 0.001f, -0.02f);
                _holsterMesh = m.ToMesh("Holster");
            }
            var go = new GameObject("Holster");
            go.transform.SetParent(gun, false);
            go.AddComponent<MeshFilter>().sharedMesh = _holsterMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = _leather;
        }
    }
}
