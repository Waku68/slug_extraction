using SlugDesign;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// A small thing a fallen slug let go of, lying beside its husk (<see cref="SlugHusk"/>): a moss ball of water, or the
    /// rounds it had left for one gun. Taken up without a key by a slug crawling over it (spec 4章 Oct 18: a moss ball lying
    /// near is taken onto the belt when there is room; the rounds, spec 8章【仮】, when it carries a gun that shoots that
    /// salt). Goes after <see cref="ExtractSettings.droppedLifetime"/>.
    /// </summary>
    public class FallenThing : MonoBehaviour
    {
        public enum Kind { MossBall, Rounds }

        public Kind kind;
        public GunKind gunKind;
        public int count = 1;
        [Tooltip("これより近くを這うと拾う（m）")] public float reach = 0.9f;

        Mesh _mesh;
        Material _mat;
        float _next;

        void OnDestroy()
        {
            if (_mesh) Destroy(_mesh);
            if (_mat) Destroy(_mat);
        }

        public static FallenThing Drop(Kind kind, GunKind gunKind, int count, Vector3 at, float lifetime)
        {
            var go = new GameObject(kind == Kind.MossBall ? "Fallen_MossBall" : "Fallen_Rounds_" + gunKind);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var t = go.AddComponent<FallenThing>();
            t.kind = kind;
            t.gunKind = gunKind;
            t.count = count;
            t.BuildLook();
            Destroy(go, lifetime);
            return t;
        }

        /// <summary>A moss ball: a round green ball, damp-dark (8 cm). Rounds: a little tied pouch of white salt.</summary>
        void BuildLook()
        {
            var shader = Shader.Find("Slug/Toon") ?? Shader.Find("Universal Render Pipeline/Lit");
            _mat = new Material(shader) { hideFlags = HideFlags.DontSave };
            bool moss = kind == Kind.MossBall;
            Color c = moss ? new Color(0.36f, 0.5f, 0.27f) : new Color(0.82f, 0.78f, 0.68f);
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
            if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", c);
            float r = moss ? 0.04f : 0.035f;
            _mesh = SlugMeshFactory.Sphere(name, r, 14, 10);
            var look = new GameObject("Look");
            look.transform.SetParent(transform, false);
            look.transform.localPosition = Vector3.up * r * (moss ? 0.9f : 0.75f);
            look.transform.localScale = moss ? Vector3.one : new Vector3(1.1f, 0.8f, 1f);
            look.AddComponent<MeshFilter>().sharedMesh = _mesh;
            look.AddComponent<MeshRenderer>().sharedMaterial = _mat;
            if (!moss)
            {
                // the knot on top of the pouch
                var knot = new GameObject("Knot");
                knot.transform.SetParent(look.transform, false);
                knot.transform.localPosition = Vector3.up * r * 1.05f;
                knot.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
                knot.AddComponent<MeshFilter>().sharedMesh = _mesh;
                knot.AddComponent<MeshRenderer>().sharedMaterial = _mat;
            }
        }

        void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 0.2f;
            foreach (var move in FindObjectsByType<SlugMoveController>())
            {
                if (!move || (transform.position - move.transform.position).sqrMagnitude > reach * reach) continue;
                if (TryGive(move.transform.root.gameObject)) { Destroy(gameObject); return; }
            }
        }

        bool TryGive(GameObject slug)
        {
            var health = slug.GetComponentInChildren<SlugHealth>();
            if (health && health.Dead) return false;
            if (kind == Kind.MossBall)
            {
                var m = slug.GetComponentInChildren<SlugMoisture>();
                return m && m.AddMossBall();
            }
            foreach (var gun in SlugHusk.Carried(slug))
            {
                if (gun.gun.kind != gunKind || gun.Handling == null || gun.Handling.Unlimited) continue;
                gun.Handling.AddRounds(count);
                return true;
            }
            return false;
        }
    }
}
