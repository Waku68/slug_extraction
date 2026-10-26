using System.Collections.Generic;
using SlugDesign;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// What a fallen slug leaves (spec 8章, Oct 25): the body goes, but the helmet, the armour and the guns stay where they
    /// were worn, in the shape the slug fell in, like a shed skin (the bag drops as a bag on the ground, <see cref="SlugBag"/>).
    /// The small things scatter beside it: its moss balls of water and the rounds it had left (<see cref="FallenThing"/>,
    /// taken up by walking over them). Everything lies there for <see cref="ExtractSettings.droppedLifetime"/> and then goes.
    /// For now only a look: whether the gear can be worn or carried home is not decided.
    /// </summary>
    public static class SlugHusk
    {
        /// <summary>Leaves the husk of <paramref name="root"/> (call just before the slug is removed).</summary>
        public static GameObject Leave(GameObject root, float lifetime)
        {
            var renderers = new List<Renderer>();
            var gear = root.GetComponentInChildren<SlugEquipment>(true);
            if (gear) gear.CollectGear(renderers, false);
            foreach (var gun in root.GetComponentsInChildren<SlugGunHolder>(true))
            {
                var m = gun.BodyModel;
                if (m && m.gameObject.activeInHierarchy) renderers.AddRange(m.GetComponentsInChildren<Renderer>());
            }
            var rack = root.GetComponentInChildren<SlugToolRack>(true);
            if (rack) rack.CollectGuns(renderers);

            var husk = new GameObject("Husk_" + root.name);
            husk.transform.position = root.transform.position;
            var owned = husk.AddComponent<HuskLeftovers>();
            foreach (var r in renderers)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                var copy = new GameObject(r.name);
                copy.transform.SetParent(husk.transform, false);
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
            }

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

    /// <summary>Destroys the copied meshes and materials of a husk with it.</summary>
    public class HuskLeftovers : MonoBehaviour
    {
        readonly List<Object> _owned = new List<Object>();
        public void Add(Object o) => _owned.Add(o);
        void OnDestroy() { foreach (var o in _owned) if (o) Destroy(o); }
    }
}
