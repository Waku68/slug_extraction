using SlugMove;
using SlugWorld;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// One ツユリ plant (spec 6: a rare white flower that opens for one island hour when the sun comes out after heavy
    /// rain). Most of the time it stands as a closed bud. When the heavy rain has stopped and the sun is out in the day,
    /// a share of the plants (<see cref="ExtractSettings.tsuyuriBloomShare"/>, a different few each time) open for
    /// <see cref="ExtractSettings.tsuyuriOpenGameHours"/>, each cup still holding a raindrop: then the flower can be
    /// picked by hand (E, <see cref="flower"/>), leaving the cut stem until it opens again. Struck with the pickaxe
    /// instead, the plant is dug up whole: two hits, and its bulb pops out (cheaper, but it keeps), and nothing grows
    /// there again (until the season turns, which the prototype does not have yet). Without weather in the scene the
    /// flower stays open as it was built, so it can be tried.
    /// </summary>
    public class TsuyuriPlant : IslandTimeFollower, IPickaxeTarget, IPickaxeSurface
    {
        public PickaxeMaterial PickaxeMaterial => PickaxeMaterial.Soft;

        [Tooltip("咲いた花（手で摘む物。摘むと消える）")] public Pickable flower;
        [Tooltip("つぼみ（咲いていない間の姿）")] public GameObject bud;
        [Tooltip("花を摘んだあとの姿（切れた茎と葉）")] public GameObject picked;
        [Tooltip("掘り出すまでに叩く回数")] public int digHits = 2;

        bool _rained, _open, _dug;
        float _openLeft;
        int _blooms;
        int _hitsLeft;
        float _punch;
        Vector3 _baseScale;

        void Awake()
        {
            _baseScale = transform.localScale;
            _hitsLeft = digHits;
        }

        void Start()
        {
            if (!WeatherPresenter.Current || !flower) return;   // no weather in this scene: leave it as built (open)
            _open = false;
            Show();
            WalkFromPast(24f);
        }

        protected override void Step(float gameHours, IslandWeatherState weather, IslandTime time)
        {
            if (_dug || !flower) return;
            if (weather.sky == Sky.HeavyRain) _rained = true;
            if (_open)
            {
                _openLeft -= gameHours;
                if (_openLeft <= 0f || Raining(weather)) { _open = false; Show(); }
                return;
            }
            // after heavy rain, the first sunny daylight: a few of the plants open
            if (_rained && weather.sky == Sky.Sunny && !time.Night)
            {
                _rained = false;
                _blooms++;
                if (Chosen())
                {
                    _open = true;
                    _openLeft = settings.tsuyuriOpenGameHours;
                    flower.gameObject.SetActive(true);
                    foreach (var p in flower.parts) if (p) p.SetActive(true);   // opens again, picked or not
                    Show();
                }
            }
        }

        /// <summary>Whether this plant opens this time (a different share of them each time the sun comes out).</summary>
        bool Chosen()
        {
            Vector3 p = transform.position;
            unchecked
            {
                uint h = (uint)Mathf.RoundToInt(p.x * 10f) * 73856093u ^ (uint)Mathf.RoundToInt(p.z * 10f) * 19349663u ^ (uint)_blooms * 83492791u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h & 0xFFFF) / 65535f < settings.tsuyuriBloomShare;
            }
        }

        /// <summary>Open and unpicked: the flower. Open and picked, or closed after being picked: the cut stem. Otherwise the bud.</summary>
        void Show()
        {
            bool flowerLeft = flower && flower.HasAny;
            if (!_open && flower) flower.gameObject.SetActive(false);
            if (bud) bud.SetActive(!_dug && !_open && flowerLeft);
            if (picked) picked.SetActive(!_dug && !flowerLeft);
            if (_open && flowerLeft) flower.gameObject.SetActive(true);
        }

        public void OnPickaxeHit(GameObject hitter, RaycastHit hit)
        {
            if (_dug) return;
            PickaxeEffects.Chips(hit.point, hit.normal, settings.Def(ItemKind.TsuyuriBulb).chipColor, 6);
            _punch = 1f;
            if (--_hitsLeft > 0) return;
            // dug up: the bulb pops out toward the digger, and the plant is gone (the flower goes with it)
            _dug = true;
            _open = false;
            if (flower) flower.gameObject.SetActive(false);
            Show();
            foreach (var c in GetComponents<Collider>()) c.enabled = false;
            Vector3 toward = Vector3.ProjectOnPlane(hitter.transform.position - transform.position, Vector3.up).normalized;
            Vector3 v = Vector3.up * Random.Range(2f, 2.4f) + toward * Random.Range(0.6f, 0.9f);
            ItemPickup.Spawn(settings, ItemKind.TsuyuriBulb, transform.position + Vector3.up * 0.06f, v, hitter.GetComponent<Collider>());
        }

        protected override void Update()
        {
            base.Update();
            // picked by hand since the last look: show the cut stem
            if (!_dug && flower && !flower.HasAny && picked && !picked.activeSelf) Show();
            if (_punch <= 0f) return;
            _punch = Mathf.Max(0f, _punch - Time.deltaTime * 6f);
            float k = Mathf.Sin(_punch * Mathf.PI * 3f) * _punch;
            transform.localScale = new Vector3(_baseScale.x * (1f + 0.12f * k), _baseScale.y * (1f - 0.1f * k), _baseScale.z * (1f + 0.12f * k));
        }
    }
}
