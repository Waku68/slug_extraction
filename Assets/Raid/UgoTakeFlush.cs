using SlugWorld;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// ウゴタケ (雨後茸) on a fallen log come up after rain: <see cref="ExtractSettings.ugoTakeAfterRainGameHours"/> island
    /// hours after the rain stops, every mushroom on the log is there again (the picked ones too). They stay through rain,
    /// clouds and night, and are gone after <see cref="ExtractSettings.ugoTakeSunGameHours"/> island hours of unbroken sunny
    /// daylight. At the start it walks back over the last island day, so the log shows what the weather has made of it.
    /// </summary>
    public class UgoTakeFlush : IslandTimeFollower
    {
        [Tooltip("倒木の上のウゴタケ（摘める物）")] public Pickable cluster;
        float _dry, _sun;
        bool _out;

        void Start()
        {
            if (!WeatherPresenter.Current || !cluster) return;   // no weather in this scene: leave them as they were built
            Show(false);
            WalkFromPast(24f);
        }

        protected override void Step(float gameHours, IslandWeatherState weather, IslandTime time)
        {
            if (!cluster) return;
            if (Raining(weather)) { _dry = 0f; _sun = 0f; return; }
            float before = _dry;
            _dry += gameHours;
            _sun = weather.sky == Sky.Sunny && !time.Night ? _sun + gameHours : 0f;
            float after = settings.ugoTakeAfterRainGameHours;
            if (before < after && _dry >= after) { Show(true); _sun = 0f; }
            else if (_out && _sun >= settings.ugoTakeSunGameHours) Show(false);
        }

        void Show(bool on)
        {
            _out = on;
            foreach (var p in cluster.parts) if (p) p.SetActive(on);
            cluster.gameObject.SetActive(on);
        }
    }
}
