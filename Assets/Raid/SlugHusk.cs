using System.Collections.Generic;
using SlugDesign;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// What a fallen slug leaves (spec 8章, Oct 25): the body goes, and the helmet, the armour and the guns it wore drop to
    /// the ground where it fell and lie there, each as one thing (Oct 27: left hanging in the air where they were worn,
    /// they looked wrong). The bag drops as a bag on the ground (<see cref="SlugBag"/>).
    /// The small things scatter beside it: its moss balls of water and the rounds it had left (<see cref="FallenThing"/>,
    /// taken up by walking over them). Everything lies there for <see cref="ExtractSettings.droppedLifetime"/> and then goes.
    /// For now only a look: whether the gear can be worn or carried home is not decided.
    /// </summary>
    public static class SlugHusk
    {
        /// <summary>Leaves the husk of <paramref name="root"/> (call just before the slug is removed).</summary>
        public static GameObject Leave(GameObject root, float lifetime)
        {
            var pieces = new List<List<Renderer>>();
            var gear = root.GetComponentInChildren<SlugGearBase>(true);
            if (gear) gear.CollectGear(pieces, false);
            foreach (var gun in root.GetComponentsInChildren<SlugGunHolder>(true))
            {
                var m = gun.BodyModel;
                if (m && m.gameObject.activeInHierarchy) pieces.Add(new List<Renderer>(m.GetComponentsInChildren<Renderer>()));
            }
            var rack = root.GetComponentInChildren<SlugToolRack>(true);
            if (rack) rack.CollectGuns(pieces);

            var husk = new GameObject("Husk_" + root.name);
            husk.transform.position = root.transform.position;
            var owned = husk.AddComponent<HuskLeftovers>();
            var colliders = new List<Collider>();
            foreach (var piece in pieces)
                MakePiece(piece, husk.transform, root.transform, owned, colliders);
            // the pieces start inside one another: let them fall through each other rather than burst apart
            for (int i = 0; i < colliders.Count; i++)
            for (int j = i + 1; j < colliders.Count; j++)
                Physics.IgnoreCollision(colliders[i], colliders[j]);

            // the small things, scattered beside it
            Vector3 at = root.transform.position;
            var moisture = root.GetComponentInChildren<SlugMoisture>(true);
            int moss = moisture ? moisture.MossBalls : 2;           // (bots have no moisture yet: they carry the usual two)
            for (int i = 0; i < moss; i++) FallenThing.Drop(FallenThing.Kind.MossBall, GunKind.Rifle, 1, Beside(at), lifetime);
            foreach (var gun in Carried(root))
            {
                var h = gun.Handling;
                if (h == null || h.Unlimited || h.Carried <= 0 || !gun.gun) continue;
                FallenThing.Drop(FallenThing.Kind.Rounds, gun.gun.kind, h.Carried, Beside(at), lifetime);
            }

            Object.Destroy(husk, lifetime);
            return husk;
        }

        /// <summary>
        /// One piece of the husk (the helmet, the armour, one gun): copies of its meshes under a body that drops from where
        /// it was worn, tips a little outward from the middle of the slug, and lies where it comes to rest
        /// (<see cref="HuskPiece"/>). On Ignore Raycast, so it never blocks a shot, the aim or the ground probe.
        /// </summary>
        static void MakePiece(List<Renderer> renderers, Transform husk, Transform slug, HuskLeftovers owned, List<Collider> colliders)
        {
            Bounds b = default;
            bool any = false;
            foreach (var r in renderers)
            {
                if (!Usable(r)) continue;
                if (any) b.Encapsulate(r.bounds); else { b = r.bounds; any = true; }
            }
            if (!any) return;

            var piece = new GameObject(renderers[0].name);
            piece.layer = 2;
            piece.transform.SetParent(husk, false);
            piece.transform.SetPositionAndRotation(b.center, slug.rotation);
            foreach (var r in renderers)
            {
                if (!Usable(r)) continue;
                var mf = r.GetComponent<MeshFilter>();
                var copy = new GameObject(r.name);
                copy.layer = 2;
                copy.transform.SetParent(piece.transform, false);
                copy.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
                copy.transform.localScale = r.transform.lossyScale;
                var mesh = Object.Instantiate(mf.sharedMesh);        // the slug's own meshes go with it
                owned.Add(mesh);
                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (!mats[i]) continue;
                    mats[i] = new Material(mats[i]);
                    owned.Add(mats[i]);
                    // nobody inside: the crawl stops
                    foreach (var p in new[] { "_WaveAmp", "_WaveSpeed", "_JiggleSpeed", "_RuffleSpeed", "_SkirtAmp" })
                        if (mats[i].HasProperty(p)) mats[i].SetFloat(p, 0f);
                }
                var mr = copy.AddComponent<MeshRenderer>();
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                // its own outline to land on (a box when the mesh cannot be read)
                if (mesh.isReadable)
                {
                    var mc = copy.AddComponent<MeshCollider>();
                    mc.convex = true;
                    mc.sharedMesh = mesh;
                    colliders.Add(mc);
                }
                else
                {
                    var box = copy.AddComponent<BoxCollider>();
                    box.center = mesh.bounds.center;
                    box.size = Vector3.Max(mesh.bounds.size, Vector3.one * 0.01f);
                    colliders.Add(box);
                }
            }

            var rb = piece.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.8f;
            rb.maxDepenetrationVelocity = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Vector3 outward = Vector3.ProjectOnPlane(b.center - slug.position, Vector3.up);
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Random.insideUnitSphere;
            rb.linearVelocity = outward * Random.Range(0.15f, 0.35f) + Vector3.up * 0.15f;
            rb.angularVelocity = Vector3.Cross(Vector3.up, outward) * Random.Range(1.5f, 3f) + Random.insideUnitSphere;
            piece.AddComponent<HuskPiece>();
        }

        static bool Usable(Renderer r)
        {
            if (!r || !r.enabled || !r.gameObject.activeInHierarchy) return false;
            var mf = r.GetComponent<MeshFilter>();
            return mf && mf.sharedMesh;
        }

        /// <summary>The guns it went out with (the back one and the hip one), not every gun it could have chosen.</summary>
        public static IEnumerable<SlugGunHolder> Carried(GameObject root)
        {
            var loadout = root.GetComponentInChildren<SlugLoadout>(true);
            if (!loadout) yield break;
            if (loadout.BackGun) yield return loadout.BackGun;
            if (loadout.HipGun) yield return loadout.HipGun;
        }

        static Vector3 Beside(Vector3 at)
        {
            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(0.35f, 0.7f);
            Vector3 p = at + new Vector3(r.x, 0f, r.y);
            if (Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var hit, 4f, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore))
                p = hit.point;
            return p;
        }
    }

    /// <summary>
    /// A falling piece of a husk: once it lies still (or after a few seconds) it lets go of its body and outline and stays
    /// as a look only, so nothing crawling by bumps it. One that falls out of the world goes.
    /// </summary>
    public class HuskPiece : MonoBehaviour
    {
        float _age, _startY;

        void Start() => _startY = transform.position.y;

        void FixedUpdate()
        {
            _age += Time.fixedDeltaTime;
            if (transform.position.y < _startY - 30f) { Destroy(gameObject); return; }
            var rb = GetComponent<Rigidbody>();
            if (rb && !(_age > 6f || (_age > 0.4f && rb.IsSleeping()))) return;
            foreach (var c in GetComponentsInChildren<Collider>()) Destroy(c);
            if (rb) Destroy(rb);
            Destroy(this);
        }
    }

    /// <summary>Destroys the copied meshes and materials of a husk with it.</summary>
    public class HuskLeftovers : MonoBehaviour
    {
        readonly List<Object> _owned = new List<Object>();
        public void Add(Object o) => _owned.Add(o);
        void OnDestroy() { foreach (var o in _owned) if (o) Destroy(o); }
    }
}
