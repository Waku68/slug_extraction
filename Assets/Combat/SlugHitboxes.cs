using System.Collections.Generic;
using SlugDesign;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// What a shot can hit on a slug (or a snail), part by part, kept on the body as it crawls, stands up, turns and
    /// stretches: the head (under the helmet, if worn), the two tentacles (the ears: a weak spot, out of the helmet),
    /// the chest (under the shell plate, if worn), the belly and tail, and what is on the back (the bag or the shell).
    /// The movement capsule is not a hitbox: rounds pass it and land on these.
    ///
    /// Hitboxes live on their own layer (<see cref="Layer"/>), which the slug's own movement checks leave out, so a
    /// slug never stands on or bumps into its own body. Each part carries a damage multiplier (the tentacles take more,
    /// armour less); how much damage a round does is not decided yet.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class SlugHitboxes : MonoBehaviour
    {
        public SlugPop slug;
        [Tooltip("当たり判定を画面に描く（調整用）")] public bool showGizmos = true;

        [Header("ダメージ倍率（仮）")]
        [Tooltip("むき出しの頭・胸・おなかに当たったときの倍率")] public float bareMultiplier = 1f;
        [Tooltip("触角（弱点）に当たったときの倍率")] public float tentacleMultiplier = 1.4f;
        [Tooltip("ヘルメットの上から頭に当たったときの倍率（防具ごとに変える予定）")] public float helmetMultiplier = 0.6f;
        [Tooltip("殻板の上から胸に当たったときの倍率（防具ごとに変える予定）")] public float plateMultiplier = 0.6f;
        [Tooltip("背中の物（バッグ／殻）に当たったときの倍率（未決定。塩が漏れる案あり）")] public float backItemMultiplier = 1f;

        /// <summary>Every hit on any slug's hitboxes (the part, the hit).</summary>
        public static event System.Action<SlugHitbox, RaycastHit> AnyHit;
        /// <summary>Every hit on this slug.</summary>
        public event System.Action<SlugHitbox, RaycastHit> Hit;

        static int _layer = -1;
        /// <summary>The Hitbox layer (8 if the project has none named so).</summary>
        public static int Layer
        {
            get
            {
                if (_layer < 0) { _layer = LayerMask.NameToLayer("Hitbox"); if (_layer < 0) _layer = 8; }
                return _layer;
            }
        }
        /// <summary>Raycast mask for the world without any slug's body (movement, cameras).</summary>
        public static int WorldMask => Physics.DefaultRaycastLayers & ~(1 << Layer);

        /// <summary>One capsule (or sphere, when a == b) riding the body between two points along it.</summary>
        class Bone
        {
            public SlugHitbox box;
            public CapsuleCollider capsule;
            public BoxCollider cube;
            public Vector3 a, b, na, nb;      // body space, rest pose (and outward normals for the skin offset)
            public float ta, tb, radius;
            public Vector3 half; public Quaternion rot;   // boxes
        }

        readonly List<Bone> _bones = new List<Bone>();
        SlugEquipment _gear;
        Transform _root;

        void Awake()
        {
            if (!slug) slug = GetComponentInChildren<SlugPop>();
        }

        void OnEnable()
        {
            if (!slug) return;
            slug.Rebuilt += Build;
            if (slug.Shape != null) Build();
        }

        void OnDisable()
        {
            if (slug) slug.Rebuilt -= Build;
            Clear();
        }

        void Clear()
        {
            if (_root) Destroy(_root.gameObject);
            _root = null;
            _bones.Clear();
        }

        /// <summary>Lays the hitboxes out on the body (called again whenever the slug is rebuilt or its gear changes).</summary>
        public void Build()
        {
            Clear();
            var shape = slug.Shape;
            if (shape == null || !Application.isPlaying) return;
            _gear = slug.GetComponent<SlugEquipment>();
            if (_gear) _gear.Built -= Build;
            if (_gear) _gear.Built += Build;
            _root = new GameObject("Hitboxes").transform;
            _root.SetParent(transform, false);
            bool helmet = _gear && _gear.helmet;
            bool plate = _gear && _gear.armor == SlugEquipment.Armor.ShellPlate;

            Vector3 S(float t) => shape.FrameAt(t).center;
            float R(float t) { var f = shape.FrameAt(t); return (f.rx + f.ry) * 0.5f; }
            Vector3 Up(float t) => shape.FrameAt(t).up;

            // the body: the tail and belly on the ground, the chest standing up, the head on top
            // (each sits inside the skin a little, so a shot that only grazes the outline misses)
            Capsule("おなか・尾", null, 0.16f, 0.45f, R(0.3f) * 0.85f, S, Up);
            Capsule("胸", plate ? "殻板" : null, 0.45f, 0.8f, R(0.62f) * 0.92f, S, Up);
            Capsule("頭", helmet ? "ヘルメット" : null, 0.86f, 0.93f, R(0.88f) * (helmet ? 1.02f : 0.9f), S, Up);

            // the tentacles: thin, out through the helmet (never covered)
            foreach (float side in new[] { -1f, 1f })
            {
                slug.EarRoot(side, out var root, out var axis, out float earR, out float len);
                var bone = NewBone("触角", null, false);
                bone.a = root; bone.b = root + axis * len;
                bone.na = bone.nb = axis;
                bone.ta = bone.tb = 0.93f;
                bone.radius = earR * 0.8f;
            }

            // what is on the back
            if (_gear && _gear.HasBackItem)
            {
                var bone = NewBone(_gear.armor == SlugEquipment.Armor.SnailShell ? "殻" : "バッグ", null, true);
                bone.a = bone.b = _gear.BackItemCenter;
                bone.na = bone.nb = _gear.BackItemNormal;
                bone.ta = bone.tb = _gear.BackItemT;
                bone.half = _gear.BackItemHalf;
                bone.rot = _gear.BackItemRotation;
            }

            // the slug's own movement capsule never bumps into its own body
            var cc = GetComponentInParent<CharacterController>();
            if (cc) foreach (var b in _bones) Physics.IgnoreCollision(cc, b.box.GetComponent<Collider>());
            Follow();
        }

        void Capsule(string part, string armour, float t0, float t1, float radius,
                     System.Func<float, Vector3> spine, System.Func<float, Vector3> up)
        {
            var bone = NewBone(part, armour, false);
            bone.ta = t0; bone.tb = t1;
            bone.a = spine(t0); bone.b = spine(t1);
            bone.na = up(t0); bone.nb = up(t1);
            bone.radius = radius;
        }

        Bone NewBone(string part, string armour, bool box)
        {
            var go = new GameObject(part) { layer = Layer };
            go.transform.SetParent(_root, false);
            var bone = new Bone();
            if (box) bone.cube = go.AddComponent<BoxCollider>();
            else bone.capsule = go.AddComponent<CapsuleCollider>();
            bone.box = go.AddComponent<SlugHitbox>();
            bone.box.owner = this;
            bone.box.part = part;
            bone.box.armour = armour;
            bone.box.multiplier = Multiplier(part, armour, box);
            _bones.Add(bone);
            return bone;
        }

        float Multiplier(string part, string armour, bool backItem)
        {
            if (backItem) return backItemMultiplier;
            if (part == "触角") return tentacleMultiplier;
            if (armour == "ヘルメット") return helmetMultiplier;
            if (armour == "殻板") return plateMultiplier;
            return bareMultiplier;
        }

        internal void Report(SlugHitbox box, RaycastHit hit)
        {
            Hit?.Invoke(box, hit);
            AnyHit?.Invoke(box, hit);
        }

        void LateUpdate() => Follow();

        /// <summary>Puts every hitbox where its part of the body is this frame.</summary>
        void Follow()
        {
            if (!slug || slug.VisualRoot == null) return;
            float scale = slug.VisualRoot.lossyScale.x;
            foreach (var b in _bones)
            {
                Vector3 pa = slug.FollowPointWorld(b.a, b.ta, b.na);
                var t = b.box.transform;
                if (b.cube)
                {
                    // a box riding the back: oriented by two more points carried the same way
                    Vector3 px = slug.FollowPointWorld(b.a + b.rot * Vector3.right * 0.1f, b.ta, b.na) - pa;
                    Vector3 py = slug.FollowPointWorld(b.a + b.rot * Vector3.up * 0.1f, b.ta, b.na) - pa;
                    t.SetPositionAndRotation(pa, Quaternion.LookRotation(Vector3.Cross(px, py), py));
                    b.cube.size = b.half * 2f * scale;
                    continue;
                }
                Vector3 pb = slug.FollowPointWorld(b.b, b.tb, b.nb);
                Vector3 d = pb - pa;
                float len = d.magnitude;
                t.SetPositionAndRotation((pa + pb) * 0.5f, len > 1e-5f ? Quaternion.FromToRotation(Vector3.up, d / len) : Quaternion.identity);
                b.capsule.direction = 1;
                b.capsule.radius = b.radius * scale;
                b.capsule.height = len + b.radius * 2f * scale;
            }
        }

        void OnDrawGizmos()
        {
            if (!showGizmos) return;
            foreach (var b in _bones)
            {
                if (!b.box) continue;
                Gizmos.color = b.box.Armoured ? new Color(0.3f, 0.6f, 1f, 0.6f) : b.box.part == "触角" ? new Color(1f, 0.3f, 0.3f, 0.6f) : new Color(1f, 0.85f, 0.2f, 0.6f);
                Gizmos.matrix = b.box.transform.localToWorldMatrix;
                if (b.cube) Gizmos.DrawWireCube(Vector3.zero, b.cube.size);
                else
                {
                    float h = Mathf.Max(0f, b.capsule.height * 0.5f - b.capsule.radius);
                    Gizmos.DrawWireSphere(Vector3.up * h, b.capsule.radius);
                    Gizmos.DrawWireSphere(Vector3.down * h, b.capsule.radius);
                }
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
