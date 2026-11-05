using SlugWorld;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Something that follows the island's clock and weather: every half second it walks the island's time from where it
    /// last looked up to now (the scene's <see cref="WeatherPresenter"/>, so the developer's clock keys, fast-forward and
    /// fixed sky count too) in small steps, and hands each step's weather to <see cref="Step"/>. The weather is worked out
    /// from the time alone, so walking it gives the same answer as working it out afresh from the moment something was
    /// last struck or picked. Without weather in the scene it does nothing.
    /// </summary>
    public abstract class IslandTimeFollower : MonoBehaviour
    {
        public ExtractSettings settings;

        const double StepGameHours = 0.1;       // 6 island minutes
        const int MaxSteps = 720;               // a long jump of the clock is walked from at most 72 island hours back
        double _last = double.NaN;
        float _wait;

        /// <summary>One step of the island's time: how long it was (island hours), the weather and the time of day then.</summary>
        protected abstract void Step(float gameHours, IslandWeatherState weather, IslandTime time);

        protected virtual void OnEnable() => _wait = Random.value * 0.5f;   // spread the walks over the frames

        /// <summary>Walks the island's time from <paramref name="gameHoursBack"/> island hours ago up to now (to start in the state the weather has made).</summary>
        protected void WalkFromPast(float gameHoursBack)
        {
            var wp = WeatherPresenter.Current;
            if (!wp) return;
            _last = wp.ShownHours - gameHoursBack / 24.0 * wp.settings.dayHours;
            Follow();
        }

        protected virtual void Update()
        {
            _wait -= Time.deltaTime;
            if (_wait > 0f) return;
            _wait = 0.5f;
            Follow();
        }

        void Follow()
        {
            var wp = WeatherPresenter.Current;
            if (!wp || !settings) return;
            double now = wp.ShownHours;
            if (double.IsNaN(_last) || now < _last) { _last = now; return; }   // the first look, or the clock was turned back
            double step = StepGameHours / 24.0 * wp.settings.dayHours;
            int n = (int)((now - _last) / step);
            if (n > MaxSteps) { _last = now - MaxSteps * step; n = MaxSteps; }
            Vector3 pos = transform.position;
            for (int i = 0; i < n; i++)
            {
                _last += step;
                Step((float)StepGameHours, wp.WeatherAt(_last, pos), IslandClock.At(wp.settings, _last));
            }
        }

        protected static bool Raining(IslandWeatherState w) => w.sky >= Sky.LightRain;
    }
}
