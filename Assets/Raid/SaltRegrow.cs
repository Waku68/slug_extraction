using SlugWorld;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The salt mushroom grows back in dry weather (salt melts in the rain and grows as it dries): after
    /// <see cref="ExtractSettings.saltRegrowGameHours"/> island hours without rain it grows one stage (stump → half-mined →
    /// full). It does not grow while it rains, and starts counting again whenever it is struck.
    /// </summary>
    [RequireComponent(typeof(Harvestable))]
    public class SaltRegrow : IslandTimeFollower
    {
        Harvestable _node;
        int _done;
        float _dry;

        void Awake() => _node = GetComponent<Harvestable>();

        protected override void Step(float gameHours, IslandWeatherState weather, IslandTime time)
        {
            if (_node.Done != _done)
            {
                if (_node.Done > _done) _dry = 0f;     // struck: count again from now
                _done = _node.Done;
            }
            if (_done == 0) { _dry = 0f; return; }
            if (Raining(weather)) return;              // salt does not grow in the rain
            _dry += gameHours;
            if (_dry < settings.saltRegrowGameHours) return;
            _dry -= settings.saltRegrowGameHours;
            _node.GrowBack();
            _done = _node.Done;
        }
    }
}
