using System.Collections.Generic;
using UnityEngine;

namespace SlugMove
{
    /// <summary>Something a salt round can hit and react to (a practice target, later another slug).</summary>
    public interface IShotTarget
    {
        void OnShot(RaycastHit hit, Vector3 velocity);
    }

    /// <summary>
    /// A grain of refined salt from the rifle. The real round flies from the eye along the aim, with gravity, and is
    /// swept against the world every frame. What is drawn starts at the muzzle of the rifle the viewer sees (shrunk
    /// toward the eye in first person) and joins the real path within <see cref="GunSettings.convergeDistance"/>,
    /// as a short bright streak. Where it lands it pops a few salt chips and leaves a small splat for a while
    /// (refined salt is white, coarse salt greyer, rock salt pink).
    /// </summary>
    public class SaltRound : MonoBehaviour
    {
        const int MaxMarks = 160;
        const float MarkLife = 15f;

        static readonly Stack<SaltRound> Pool = new Stack<SaltRound>();
        static readonly Queue<GameObject> Marks = new Queue<GameObject>();
        static readonly Dictionary<Color, Material> Mats = new Dictionary<Color, Material>();
        static readonly Dictionary<Color, Material> MarkMats = new Dictionary<Color, Material>();
        static Mesh _markMesh;

        GunSettings _s;
        Transform _owner;
        Vector3 _pos, _vel, _offset, _prevVisual;
        float _age, _travel, _startScale;
        System.Action<RaycastHit> _onHit;

        /// <summary>
        /// Fires a round from <paramref name="origin"/> (the eye) with <paramref name="velocity"/>. It is drawn from
        /// <paramref name="visualStart"/> at <paramref name="startScale"/> of its size, growing to full size as it joins
        /// the real path. Colliders under <paramref name="owner"/> are never hit.
        /// </summary>
        public static void Fire(Vector3 origin, Vector3 velocity, Vector3 visualStart, float startScale,
                                GunSettings settings, Transform owner, System.Action<RaycastHit> onHit)
        {
            SaltRound r = null;
            while (r == null && Pool.Count > 0) r = Pool.Pop();
            if (r == null) r = Create();
            r.GetComponent<MeshRenderer>().sharedMaterial = GrainMaterial(settings.roundColor);
            r.gameObject.SetActive(true);
            r._s = settings;
            r._owner = owner;
            r._pos = origin;
            r._vel = velocity;
            r._offset = visualStart - origin;
            r._prevVisual = visualStart;
            r._age = 0f;
            r._travel = 0f;
            r._startScale = startScale;
            r._onHit = onHit;
            r.Draw(visualStart, visualStart, startScale);
        }

        static SaltRound Create()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SaltRound";
            go.layer = 2;                                       // Ignore Raycast: never blocks anything
            DestroyImmediate(go.GetComponent<Collider>());
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.AddComponent<SaltRound>();
        }

        /// <summary>The unlit material of a grain of this colour (one per colour, shared).</summary>
        static Material GrainMaterial(Color c)
        {
            if (Mats.TryGetValue(c, out var m) && m) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "SaltGrain", hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", c);
            Mats[c] = m;
            return m;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 g = Physics.gravity * _s.gravityScale;
            Vector3 next = _pos + _vel * dt + 0.5f * dt * dt * g;
            Vector3 step = next - _pos;
            float len = step.magnitude;
            if (len > 1e-6f && Sweep(_pos, step / len, len, out var hit))
            {
                Land(hit);
                return;
            }
            _pos = next;
            _vel += g * dt;
            _travel += len;
            _age += dt;
            if (_age > _s.roundLife) { Release(); return; }

            // the drawn grain: from the muzzle onto the real path, growing to full size on the way
            float k = Mathf.Clamp01(_travel / Mathf.Max(_s.convergeDistance, 0.01f));
            k = k * k * (3f - 2f * k);
            Vector3 visual = _pos + _offset * (1f - k);
            Draw(_prevVisual, visual, Mathf.Lerp(_startScale, 1f, k));
            _prevVisual = visual;
        }

        bool Sweep(Vector3 from, Vector3 dir, float dist, out RaycastHit best)
        {
            best = default;
            float bestDist = float.MaxValue;
            foreach (var h in Physics.RaycastAll(from, dir, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (h.distance >= bestDist || (_owner && h.collider.transform.IsChildOf(_owner))) continue;
                if (h.collider is CharacterController) continue;        // a movement capsule: rounds land on the hitboxes
                best = h;
                bestDist = h.distance;
            }
            return bestDist < float.MaxValue;
        }

        /// <summary>A streak from where the grain was to where it is, no longer than the tracer length.</summary>
        void Draw(Vector3 from, Vector3 to, float scale)
        {
            Vector3 d = to - from;
            float len = d.magnitude, max = _s.tracerLength * scale;
            if (len > max) { from = to - d / len * max; len = max; }
            float w = _s.grainSize * scale;
            len = Mathf.Max(len, w);
            transform.position = (from + to) * 0.5f;
            if (d.sqrMagnitude > 1e-10f) transform.rotation = Quaternion.LookRotation(d);
            transform.localScale = new Vector3(w, w, len);
        }

        void Land(RaycastHit hit)
        {
            var target = hit.collider.GetComponentInParent<IShotTarget>();
            target?.OnShot(hit, _vel);
            if (_s.chips > 0) PickaxeEffects.Chips(hit.point, hit.normal, _s.roundColor, _s.chips, 0.55f);
            if (!(target is SlugHitbox)) Mark(hit, target as Component, _s.markScale, _s.roundColor);   // no splat in the air round a body
            _onHit?.Invoke(hit);
            Release();
        }

        void Release()
        {
            _onHit = null;
            gameObject.SetActive(false);
            Pool.Push(this);
        }

        /// <summary>The lit material of a salt splat of this colour (one per colour, shared).</summary>
        static Material MarkMaterial(Color c)
        {
            if (MarkMats.TryGetValue(c, out var m) && m) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SaltMark", hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.4f);
            MarkMats[c] = m;
            return m;
        }

        /// <summary>A small splat of salt on the surface (it rides along on a target, stays put elsewhere).</summary>
        static void Mark(RaycastHit hit, Component target, float scale, Color color)
        {
            if (_markMesh == null)
            {
                var disc = new List<Vector2>();
                for (int i = 0; i < 7; i++)
                {
                    float a = i / 7f * Mathf.PI * 2f, r = i % 2 == 0 ? 0.01f : 0.0075f;
                    disc.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
                }
                _markMesh = new ProfileMesh { uvScale = 1f }.Flat(disc, Vector3.zero, Vector3.right, Vector3.up, false).ToMesh("SaltMark");
            }
            while (Marks.Count >= MaxMarks)
            {
                var old = Marks.Dequeue();
                if (old) Destroy(old);
            }
            var go = new GameObject("SaltMark") { layer = 2 };
            go.AddComponent<MeshFilter>().sharedMesh = _markMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MarkMaterial(color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // on a target it sits a little proud of the surface, over the painted rings
            go.transform.SetPositionAndRotation(hit.point + hit.normal * (target ? 0.006f : 0.0015f),
                Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            go.transform.localScale = Vector3.one * (Random.Range(0.8f, 1.3f) * scale);
            if (target) go.transform.SetParent(target.transform, true);
            Marks.Enqueue(go);
            Destroy(go, MarkLife);
        }
    }
}
