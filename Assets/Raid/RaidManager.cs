using System.Collections.Generic;
using SlugMove;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SlugExtract
{
    /// <summary>
    /// One solo raid: choose what to carry, harvest, carry, and reach the exit. The island's clock and weather
    /// (<see cref="SlugWorld.WeatherPresenter"/>) and the body's moisture set the pace: drying out to nothing brings the
    /// slug down (<see cref="health"/>), and everything carried is lost. The old time limit ("before the rain stops") can
    /// still be switched on. Shows the result; R starts over. The stash (everything brought home) lasts until play mode ends.
    /// </summary>
    public class RaidManager : MonoBehaviour
    {
        enum State { Playing, Extracted, Failed }

        public ExtractSettings settings;
        public SlugMoveController controller;
        public SlugToolHolder tool;
        public SlugGunHolder[] guns = new SlugGunHolder[0];
        public SlugLoadout loadout;
        [Tooltip("出撃前に持ち物を選ぶ画面（選び終わるまで時間は進まない）")] public LoadoutPicker picker;
        public SlugBag bag;
        [Tooltip("体力（乾いて倒れたら失敗）")] public SlugHealth health;
        [Tooltip("昔の「雨が止むまで」の制限時間を使う（島の時計に置き換えたので、ふだんは使わない）")] public bool useTimeLimit;
        [Tooltip("脱出口の中心")] public Transform exitPoint;

        static int _stashValue, _stashSalt, _stashCrop, _runs;
        string _savedText, _cause;

        State _state = State.Playing;
        float _timeLeft, _extractProgress;
        readonly List<(string text, float until)> _log = new List<(string, float)>();
        GUIStyle _big, _mid, _small;

        void OnEnable() { Harvestable.Harvested += OnHarvested; }
        void OnDisable() { Harvestable.Harvested -= OnHarvested; }

        void Start()
        {
            _timeLeft = settings.timeLimit;
            if (health) health.Died += cause => { if (_state == State.Playing) { _cause = cause; End(State.Failed); } };
        }

        void OnHarvested(ItemKind kind, int added, bool full)
        {
            var name = settings.Def(kind).displayName;
            if (added > 0) Log($"{name}が出た（E でつかむ）");
        }

        void Log(string text)
        {
            _log.Add((text, Time.time + 2f));
            if (_log.Count > 5) _log.RemoveAt(0);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (_state != State.Playing)
            {
                if (kb != null && kb.rKey.wasPressedThisFrame) SceneManager.LoadScene(gameObject.scene.name);
                return;
            }

            if (picker && picker.IsOpen) return;     // still choosing what to carry
            if (useTimeLimit)
            {
                _timeLeft -= Time.deltaTime;
                if (_timeLeft <= 0f) { _timeLeft = 0f; _cause = "time"; End(State.Failed); return; }
            }

            // stay inside the exit for a moment to get out
            Vector3 d = controller.transform.position - exitPoint.position; d.y = 0f;
            bool inside = d.magnitude <= settings.extractRadius;
            _extractProgress = inside ? _extractProgress + Time.deltaTime : 0f;
            if (_extractProgress >= settings.extractTime) End(State.Extracted);
        }

        void End(State s)
        {
            _state = s;
            controller.inputEnabled = false;
            tool.inputEnabled = false;
            foreach (var g in guns) if (g) g.inputEnabled = false;
            if (loadout && loadout.knife) loadout.knife.inputEnabled = false;
            if (loadout) loadout.inputEnabled = false;
            _runs++;
            if (s == State.Extracted)
            {
                _stashValue += bag.Value;
                _stashSalt += bag.Count(ItemKind.RockSalt);
                _stashCrop += bag.Count(ItemKind.Crop);
            }
            // what is in the hands at the exit comes home too
            if (s == State.Extracted && bag.Holding)
            {
                var held = settings.Def(bag.HeldKind);
                _stashValue += held.value;
                if (bag.HeldKind == ItemKind.RockSalt) _stashSalt++; else _stashCrop++;
            }
            // what was pushed into the body always comes home
            _savedText = null;
            if (bag.HasHidden)
            {
                var d = settings.Def(bag.Hidden);
                _stashValue += d.value;
                if (bag.Hidden == ItemKind.RockSalt) _stashSalt++; else _stashCrop++;
                _savedText = d.displayName;
            }
        }

        void Styles()
        {
            if (_big != null) return;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, richText = true };
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperLeft, richText = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.UpperCenter, richText = true };
            foreach (var st in new[] { _big, _mid, _small }) st.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            Styles();
            float w = Screen.width, h = Screen.height;

            // ---- top right: the old rain timer, only when it is switched on (the island's clock shows no numbers)
            var salt = settings.rockSalt; var crop = settings.crop;
            if (useTimeLimit)
            {
                int sec = Mathf.CeilToInt(_timeLeft);
                string timeColor = _timeLeft <= 30f ? "#ff8080" : "#a0d8ff";
                GUI.Box(new Rect(w - 300, 10, 290, 40), GUIContent.none);
                GUI.Label(new Rect(w - 290, 16, 280, 30), $"雨が止むまで  <color={timeColor}><b>{sec / 60}:{sec % 60:00}</b></color>", _mid);
            }

            // ---- pickup log and the crosshair
            float y = h * 0.5f + 30f;
            foreach (var (text, until) in _log)
            {
                if (Time.time > until) continue;
                GUI.Label(new Rect(w * 0.5f - 150, y, 300, 24), text, _small);
                y += 24f;
            }
            GUI.Label(new Rect(w * 0.5f - 10, h * 0.5f - 14, 20, 24), "+", _small);

            if (_state == State.Playing)
            {
                if (_extractProgress > 0f)
                    GUI.Label(new Rect(0, h * 0.3f, w, 50), $"脱出中… {Mathf.Clamp01(_extractProgress / settings.extractTime) * 100f:0}%", _big);
                GUI.Label(new Rect(0, h - 40, w, 30), "左クリック 叩く・撃つ・突く（1 ピッケル / 2 背中の銃 / 3 腰の道具）／ E つかむ・バッグに入れる / G バッグから出す・落とす / Q 覗く / X 体に突っ込む / Z 伏せ / F 水の苔玉 ／ 光の柱が脱出口", _small);
                return;
            }

            // ---- result
            GUI.Box(new Rect(w * 0.5f - 300, h * 0.25f, 600, 300), GUIContent.none);
            string title = _state == State.Extracted ? "<color=#a0ffa0>脱出成功</color>"
                : _cause == "time" ? "<color=#ff9090>雨が止んで、乾いてしまった</color>"
                : _cause == "dried" ? "<color=#ff9090>乾ききって、倒れてしまった</color>"
                : "<color=#ff9090>倒れてしまった</color>";
            string body = _state == State.Extracted
                ? $"持ち帰り：{salt.displayName} ×{bag.Count(ItemKind.RockSalt)}、{crop.displayName} ×{bag.Count(ItemKind.Crop)}（{bag.Value} 塩）"
                : _savedText != null ? $"持ち物はすべて失った（体の中の{_savedText}だけは持ち帰った）" : "持ち物はすべて失った";
            GUI.Label(new Rect(w * 0.5f - 300, h * 0.25f + 20, 600, 60), title, _big);
            GUI.Label(new Rect(w * 0.5f - 300, h * 0.25f + 100, 600, 30), body, _small);
            GUI.Label(new Rect(w * 0.5f - 300, h * 0.25f + 150, 600, 60),
                $"倉庫：{salt.displayName} {_stashSalt}、{crop.displayName} {_stashCrop}（合計 {_stashValue} 塩、{_runs} 回出撃）", _small);
            GUI.Label(new Rect(w * 0.5f - 300, h * 0.25f + 230, 600, 30), "R でもう一度", _small);
        }
    }
}
