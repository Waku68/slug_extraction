using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// A node that is struck with the pickaxe (Fortnite-style): every hit knocks one thing out of it (a chunk of rock
    /// salt about 30 cm across, a turnip) that pops out toward the one who struck it and lands on the ground, to be taken
    /// in the hands and put in the bag; the node jolts and sheds chips, and the last hit gives a bonus and breaks it.
    /// With <see cref="stages"/> (the salt mushroom: full, half-mined, stump) it changes shape each time something comes
    /// out instead, and the last stage stays behind when it is mined out. With <see cref="planted"/> (a rain turnip in its
    /// mound) the thing in the ground is itself what comes out: each hit loosens it, and the last one pops it out whole.
    /// </summary>
    public class Harvestable : MonoBehaviour, IPickaxeTarget, IPickaxeSurface
    {
        /// <summary>Rock salt is hard (the pickaxe bounces off), crops are soft (it sinks in).</summary>
        public PickaxeMaterial PickaxeMaterial => kind == ItemKind.RockSalt ? PickaxeMaterial.Hard : PickaxeMaterial.Soft;

        public ExtractSettings settings;
        public ItemKind kind;
        [Tooltip("壊れるまでに叩く回数")] public int hits = 4;
        [Tooltip("1 回叩くごとに採れる数")] public int yieldPerHit = 1;
        [Tooltip("最後の一撃で追加で採れる数")] public int breakBonus = 1;
        [Tooltip("何回叩くごとに出てくるか（塩のきのこは 2：6回で3個）")] public int hitsPerYield = 1;
        [Tooltip("形の段階（満タン、掘りかけ、切り株…）。出てくるたびに次へ。最後の段階は掘りきっても残る。空なら壊れて消える")]
        public GameObject[] stages = new GameObject[0];
        [Tooltip("植わっている物そのもの（雨カブの畑）。叩くたびに少しずつ持ち上がり、出てくるときはこれが同じ向き・同じ大きさで飛び出す")]
        public Transform planted;
        [Tooltip("植わっている物が土に埋まっている深さ（m）。叩くたびにこれを回数で割った分だけ持ち上がる")]
        public float plantedDepth;

        int _left;
        float _punch;           // 0..1, decays: the jolt of the last hit
        Vector3 _baseScale;
        bool _breaking;
        float _breakT;

        /// <summary>Called after every hit: (item, how many came out, false).</summary>
        public static event System.Action<ItemKind, int, bool> Harvested;

        void Awake()
        {
            _left = hits;
            _baseScale = transform.localScale;
            ShowStage(0);
        }

        bool HasStages => stages != null && stages.Length > 0;

        /// <summary>Shows one stage and fits the box collider (if any) round it, so the pickaxe meets what is there.</summary>
        void ShowStage(int index)
        {
            if (!HasStages) return;
            index = Mathf.Clamp(index, 0, stages.Length - 1);
            for (int i = 0; i < stages.Length; i++) if (stages[i]) stages[i].SetActive(i == index);
            var box = GetComponent<BoxCollider>();
            if (box && stages[index]) FitBox(box, stages[index], transform);
        }

        /// <summary>
        /// Fits a box collider round the meshes under <paramref name="shown"/>, in <paramref name="space"/>'s own units
        /// (the corners of every mesh are carried into that space, so a turned node still gets a box that hugs it).
        /// </summary>
        public static void FitBox(BoxCollider box, GameObject shown, Transform space, float shrink = 0.85f)
        {
            bool any = false;
            var b = new Bounds();
            foreach (var mf in shown.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.sharedMesh) continue;
                var mb = mf.sharedMesh.bounds;
                var m = space.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            if (!any) return;
            box.center = b.center;
            box.size = b.size * shrink;
        }

        public void OnPickaxeHit(GameObject hitter, RaycastHit hit)
        {
            if (_breaking || _left <= 0) return;
            Chips(hit.point, hit.normal, 6);
            _punch = 1f;
            _left--;
            int done = hits - _left;
            int step = Mathf.Max(1, hitsPerYield);
            int amount = (done % step == 0 ? yieldPerHit : 0) + (_left == 0 ? breakBonus : 0);
            if (HasStages) ShowStage(done / step);
            // it pops out of the struck face toward the one who struck it, and falls to the ground
            Vector3 toward = Vector3.ProjectOnPlane(hitter.transform.position - transform.position, Vector3.up).normalized;
            var ignore = hitter.GetComponent<Collider>();
            float size = settings.Def(kind).size;
            Harvested?.Invoke(kind, amount, false);
            if (_left == 0 && !HasStages) _breaking = true;
            if (planted && planted.gameObject.activeSelf)
            {
                if (amount > 0)
                {
                    // the very one that was in the ground comes out, as it was, with a little toss toward the digger
                    Vector3 up = Vector3.up * Random.Range(2.2f, 2.6f) + toward * Random.Range(0.6f, 0.9f);
                    var p = ItemPickup.Spawn(settings, kind, planted.position + Vector3.up * plantedDepth / Mathf.Max(1, hits), up, ignore, planted.rotation);
                    var pc = p.GetComponent<Collider>();
                    foreach (var c in GetComponents<Collider>()) Physics.IgnoreCollision(pc, c);   // it starts inside the mound
                    planted.gameObject.SetActive(false);
                    amount--;
                }
                else planted.position += Vector3.up * plantedDepth / Mathf.Max(1, hits);   // dug a little looser
            }
            for (int i = 0; i < amount; i++)
            {
                Vector3 from = hit.point + hit.normal * (size * 0.6f) + Vector3.up * (size * 0.3f);
                Vector3 v = (toward * Random.Range(1f, 1.6f) + Vector3.up * Random.Range(1.6f, 2.2f) + Random.insideUnitSphere * 0.4f);
                ItemPickup.Spawn(settings, kind, from, v, ignore);
            }
        }

        void Update()
        {
            if (_breaking)
            {
                _breakT += Time.deltaTime / 0.35f;
                transform.localScale = _baseScale * Mathf.Max(0f, 1f - _breakT * _breakT);
                if (_breakT >= 1f) Destroy(gameObject);
                return;
            }
            if (_punch > 0f)
            {
                _punch = Mathf.Max(0f, _punch - Time.deltaTime * 6f);
                float k = Mathf.Sin(_punch * Mathf.PI * 3f) * _punch;
                transform.localScale = new Vector3(_baseScale.x * (1f + 0.12f * k), _baseScale.y * (1f - 0.1f * k), _baseScale.z * (1f + 0.12f * k));
            }
        }

        void Chips(Vector3 point, Vector3 normal, int count)
            => PickaxeEffects.Chips(point, normal, settings.Def(kind).chipColor, count);
    }
}
