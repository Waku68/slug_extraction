using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// A practice target for the rifle: a round wooden board with rings on a post. A hit rocks the board back on its
    /// hinge (and twists it when off-centre), flashes it, and is scored by ring (10 in the middle, 5 at the rim).
    /// The salt splats stick to the board (it is not scaled, so they keep their shape).
    /// </summary>
    public class ShootingTarget : MonoBehaviour, IShotTarget
    {
        [Tooltip("板の半径（m）")] public float radius = 0.25f;

        /// <summary>Every hit on the board: (the target, ring 5..10).</summary>
        public static event System.Action<ShootingTarget, int> Scored;

        public int Hits { get; private set; }
        public int LastScore { get; private set; }

        Transform _swing;
        float _tilt, _tiltV, _twist, _twistV, _flash;
        Renderer[] _faces;
        MaterialPropertyBlock _block;

        /// <summary>
        /// A target standing on the ground at <paramref name="position"/>, facing <paramref name="facing"/>, the middle at
        /// <paramref name="height"/>. The materials are given (saved assets when built into a scene; emission enabled for the flash).
        /// </summary>
        public static ShootingTarget Create(Transform parent, Vector3 position, Vector3 facing, float height, float radius,
                                            Material wood, Material postMat, Material red, Material white)
        {
            var root = new GameObject("Target").transform;
            root.SetParent(parent, false);
            root.position = position;
            root.rotation = Quaternion.LookRotation(-facing, Vector3.up);       // the face (−Z side of the board) looks along facing
            var t = root.gameObject.AddComponent<ShootingTarget>();
            t.radius = radius;

            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post";
            post.transform.SetParent(root, false);
            post.transform.localScale = new Vector3(0.05f, height - radius * 0.6f, 0.05f);
            post.transform.localPosition = new Vector3(0f, (height - radius * 0.6f) * 0.5f, 0.03f);
            post.GetComponent<Renderer>().sharedMaterial = postMat;

            // the board swings on a hinge at its lower edge
            t._swing = new GameObject("Swing").transform;
            t._swing.SetParent(root, false);
            t._swing.localPosition = new Vector3(0f, height - radius * 0.6f, 0f);
            var board = Disc(t._swing, "Board", radius, 0.03f, new Vector3(0f, radius * 0.6f, 0f), wood, true);
            var faces = new System.Collections.Generic.List<Renderer> { board };
            float[] rings = { 0.8f, 0.6f, 0.4f, 0.2f };
            for (int i = 0; i < rings.Length; i++)
                faces.Add(Disc(t._swing, "Ring" + i, radius * rings[i], 0.002f, new Vector3(0f, radius * 0.6f, -0.016f - 0.0012f * i), i % 2 == 0 ? red : white, false));
            t._faces = faces.ToArray();
            return t;
        }

        static Renderer Disc(Transform parent, string name, float r, float thick, Vector3 pos, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (!collider) DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);         // the cylinder's axis along Z
            go.transform.localScale = new Vector3(r * 2f, thick * 0.5f, r * 2f);
            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = mat;
            return rend;
        }

        void Awake()
        {
            if (!_swing) _swing = transform.Find("Swing");
            if (_faces == null && _swing) _faces = _swing.GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        public void OnShot(RaycastHit hit, Vector3 velocity)
        {
            Hits++;
            // the ring: distance from the middle of the board, in its own plane
            Vector3 center = _swing.TransformPoint(new Vector3(0f, radius * 0.6f, 0f));
            float off = Vector3.ProjectOnPlane(hit.point - center, _swing.forward).magnitude / Mathf.Max(radius, 1e-3f);
            LastScore = off <= 1f ? 10 - Mathf.Min(5, Mathf.FloorToInt(off * 5f)) : 0;
            if (LastScore > 0) Scored?.Invoke(this, LastScore);

            // rock back on the hinge, harder the higher it is hit; twist when hit off to the side
            Vector3 local = _swing.InverseTransformPoint(hit.point);
            float push = Mathf.Clamp(velocity.magnitude / 140f, 0.2f, 1.5f);
            _tiltV += 260f * push * Mathf.Clamp(local.y / (radius * 1.6f), 0.2f, 1f);
            _twistV += 300f * push * Mathf.Clamp(local.x / radius, -1f, 1f);
            _flash = 1f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < 4; i++)
            {
                Spring(ref _tilt, ref _tiltV, 3.5f, 0.25f, dt * 0.25f);
                Spring(ref _twist, ref _twistV, 4f, 0.3f, dt * 0.25f);
            }
            _swing.localRotation = Quaternion.Euler(_tilt, _twist, 0f);
            if (_flash > 0f || _block == null)
            {
                _flash = Mathf.Max(0f, _flash - dt * 8f);
                foreach (var r in _faces)
                {
                    if (!r) continue;
                    r.GetPropertyBlock(_block);
                    _block.SetColor("_EmissionColor", Color.white * (_flash * 0.6f));
                    r.SetPropertyBlock(_block);
                }
            }
        }

        static void Spring(ref float x, ref float v, float hz, float damping, float dt)
        {
            float w = 2f * Mathf.PI * hz;
            v += (-w * w * x - 2f * damping * w * v) * dt;
            x += v * dt;
        }
    }
}
