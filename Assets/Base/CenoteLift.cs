using UnityEngine;

namespace SlugBase
{
    /// <summary>
    /// The lift (tsurube) of the cenote base: two boards on one rope over two pulleys, so one is always down on the
    /// ground floor while the other waits up at the gallery. Up only: a slug that stays on the low board for a moment
    /// is carried up (the other board comes down at the same time). Boards hang under their own pulley and only move
    /// up and down. The vertical ropes stretch from the pulleys to the boards.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // the boards move before the slug's own movement, so it lands on where they are now
    public class CenoteLift : MonoBehaviour
    {
        public Transform[] boards = new Transform[2];
        public Transform[] ropes = new Transform[2];
        [Tooltip("板の原点（下面）の高さ：1階・2階（m、ワールド）")] public float lowY = 2.0f, highY = 4.55f;
        [Tooltip("縦の綱の上の端の高さ（滑車の下）")] public float ropeTopY = 6.6f;
        [Tooltip("板の輪（縄が集まる所）の、板の原点からの高さ")] public float ringHeight = 0.75f;
        [Tooltip("乗ってから動き出すまで（秒）")] public float boardDelay = 0.35f;
        [Tooltip("1階から2階まで（秒）")] public float rideTime = 3.2f;
        [Tooltip("いま上にいる板（0 か 1）")] public int upper = 0;

        float _t = -1f, _onFor;
        readonly Collider[] _hits = new Collider[32];   // the slug's own hit boxes come up too

        public bool Moving => _t >= 0f;

        void Start() => Place(0f);

        void Update() => Step(Time.deltaTime);

        /// <summary>One step (the walk test calls it by hand).</summary>
        public void Step(float dt)
        {
            int lower = 1 - upper;
            if (_t < 0f)
            {
                // start only when the slug is mostly on the board, not when its nose touches the edge
                var rider = Rider(boards[lower]);
                bool on = rider && Flat(rider.transform.position - boards[lower].position).magnitude < 0.3f;
                _onFor = on ? _onFor + dt : 0f;
                if (_onFor >= boardDelay) { _t = 0f; _onFor = 0f; }
                return;
            }
            float before = Height(lower, Ease(_t));
            _t = Mathf.Min(1f, _t + dt / Mathf.Max(0.1f, rideTime));
            float k = Ease(_t);
            float dy = Height(lower, k) - before;
            var cc = Rider(boards[lower]);
            if (cc) cc.Move(new Vector3(0f, dy, 0f));     // the rider first, then the board under it
            Place(k);
            if (_t >= 1f) { upper = lower; _t = -1f; Place(0f); }
        }

        float Height(int board, float k)
        {
            bool goingUp = board != upper;
            return goingUp ? Mathf.Lerp(lowY, highY, k) : Mathf.Lerp(highY, lowY, k);
        }

        void Place(float k)
        {
            for (int i = 0; i < 2; i++)
            {
                if (!boards[i]) continue;
                var p = boards[i].position;
                p.y = Height(i, k);
                boards[i].position = p;
                // move the collider in the physics world now too (a transform alone waits for the next physics sync)
                var rb = boards[i].GetComponent<Rigidbody>();
                if (rb) rb.position = p;
                if (ropes[i])
                {
                    var r = ropes[i];
                    r.position = new Vector3(p.x, ropeTopY, p.z);
                    float len = Mathf.Max(0.05f, ropeTopY - (p.y + ringHeight));
                    r.localScale = new Vector3(1f, len, 1f);
                }
            }
        }

        static float Ease(float x) => x * x * (3f - 2f * x);

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>The slug standing on this board, if any.</summary>
        CharacterController Rider(Transform board)
        {
            if (!board) return null;
            int n = Physics.OverlapBoxNonAlloc(board.position + Vector3.up * 0.3f, new Vector3(0.45f, 0.25f, 0.45f), _hits, board.rotation,
                                               ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var cc = _hits[i] as CharacterController;
                if (!cc) cc = _hits[i].GetComponentInParent<CharacterController>();
                if (cc) return cc;
            }
            return null;
        }
    }
}
