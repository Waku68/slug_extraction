using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>
    /// Development: crawl many times faster to look round a big test map (IslandTest). F4 turns it on and off; while it
    /// is on, a small note shows in the top-right corner.
    /// </summary>
    public class DevFastCrawl : MonoBehaviour
    {
        public SlugMoveController controller;
        [Tooltip("速く這うときの倍率")] public float fast = 10f;
        [Tooltip("始めから速くしておく")] public bool startFast = true;

        bool _on;
        GUIStyle _style;

        void Start() => Set(startFast);

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f4Key.wasPressedThisFrame) Set(!_on);
        }

        void OnDisable() { if (controller) controller.devSpeed = 1f; }

        void Set(bool on)
        {
            _on = on;
            if (controller) controller.devSpeed = on ? fast : 1f;
        }

        void OnGUI()
        {
            if (!_on) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(Screen.width - 260f, 8f, 250f, 24f), $"{fast:0}倍速で這う（F4）", _style);
        }
    }
}
