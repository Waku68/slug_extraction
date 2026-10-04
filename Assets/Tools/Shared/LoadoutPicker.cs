using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>
    /// Choosing what to carry before setting out: one big gun for the back and a small gun or a knife for the hip (the
    /// pickaxe always goes on the back), and which bag to wear (or none: the folding eco bag is always on the belt). Until the base exists, it opens over the test scenes: at the start of a raid
    /// (<see cref="openOnStart"/>), or with Tab where changing at any time is allowed. While it is open the cursor is
    /// free and the slug does not move or use anything.
    /// </summary>
    public class LoadoutPicker : MonoBehaviour
    {
        public SlugLoadout loadout;
        public SlugCameraRig cameraRig;
        public SlugMoveController controller;
        [Tooltip("始まったときに開く（出撃前）")] public bool openOnStart;
        [Tooltip("Tab でいつでも開ける（試す用のシーン）")] public bool allowToggle = true;
        [Tooltip("決めるボタンの文字")] public string confirmLabel = "決定";

        public bool IsOpen { get; private set; }
        /// <summary>Called when the choice is made and the picker closes.</summary>
        public event System.Action Closed;

        GunKind _back;
        HipTool _hip;
        SlugExtract.BagKind _bag;
        SlugExtract.SlugBag _bagComp;
        static readonly SlugExtract.BagKind[] BagChoices =
            { SlugExtract.BagKind.Cloth, SlugExtract.BagKind.Leather, SlugExtract.BagKind.ShellBox, SlugExtract.BagKind.Net, SlugExtract.BagKind.None };
        bool _moveWas, _loadoutWas;
        GUIStyle _title, _label, _button, _note;

        void Start()
        {
            if (openOnStart) Open();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!IsOpen && allowToggle && kb.tabKey.wasPressedThisFrame) { Open(); return; }
            if (!IsOpen) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || (allowToggle && kb.tabKey.wasPressedThisFrame)) Close();
        }

        public void Open()
        {
            if (IsOpen || !loadout) return;
            IsOpen = true;
            _back = loadout.backGun;
            _hip = loadout.hip;
            _bagComp = loadout.GetComponent<SlugExtract.SlugBag>();
            if (_bagComp) _bag = _bagComp.kind;
            _moveWas = controller && controller.inputEnabled;
            _loadoutWas = loadout.inputEnabled;
            if (controller) controller.inputEnabled = false;
            loadout.inputEnabled = false;
            if (cameraRig) cameraRig.cursorFree = true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (controller) controller.inputEnabled = _moveWas;
            loadout.inputEnabled = _loadoutWas;
            if (cameraRig) cameraRig.cursorFree = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // the tool in hand is taken in hand again from where it now rides (which also keeps this click from firing it)
            loadout.SetLoadout(_back, _hip);
            if (_bagComp && _bagComp.kind != _bag) _bagComp.SetKind(_bag);
            Closed?.Invoke();
        }

        void Styles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, richText = true };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleLeft, richText = true };
            _note = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.UpperLeft, richText = true, wordWrap = true };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 16, richText = true };
            foreach (var st in new[] { _title, _label, _note }) st.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            if (!IsOpen || !loadout) return;
            Styles();
            GUI.depth = -100;      // over every other readout
            const float PW = 680f, PH = 460f;
            // a small game view: the whole panel shrinks to fit
            float fit = Mathf.Min(1f, Screen.width / (PW + 40f), Screen.height / (PH + 40f));
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(fit, fit, 1f));
            float w = Screen.width / fit, h = Screen.height / fit;
            var panel = new Rect((w - PW) * 0.5f, (h - PH) * 0.5f, PW, PH);
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.color = new Color(0.07f, 0.08f, 0.09f, 1f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Box(panel, GUIContent.none);
            float x = panel.x + 24f, y = panel.y + 16f;
            GUI.Label(new Rect(panel.x, y, PW, 40f), "出撃の準備", _title);
            y += 56f;

            // ---- the back: one big gun
            GUI.Label(new Rect(x, y, 200f, 30f), "<b>背中</b>（大きい銃）", _label);
            float bx = x + 190f;
            foreach (var g in loadout.guns)
            {
                if (!g || !g.gun || g.gun.Small) continue;
                bool on = g.gun.kind == _back;
                if (Choice(new Rect(bx, y - 2f, 148f, 34f), g.gun.displayName, on)) _back = g.gun.kind;
                bx += 154f;
            }
            y += 44f;
            GUI.Label(new Rect(x + 190f, y - 6f, PW - 230f, 24f), "<color=#c8c8c8>＋ ピッケル（いつも背中に持つ）</color>", _note);
            y += 34f;

            // ---- the hip: a small gun or the knife
            GUI.Label(new Rect(x, y, 200f, 30f), "<b>腰</b>（小さい銃かナイフ）", _label);
            bx = x + 190f;
            var pistol = FindSmall();
            if (pistol && Choice(new Rect(bx, y - 2f, 148f, 34f), pistol.gun.displayName, _hip == HipTool.Pistol)) _hip = HipTool.Pistol;
            if (pistol) bx += 154f;
            if (loadout.knife && Choice(new Rect(bx, y - 2f, 148f, 34f), "ナイフ", _hip == HipTool.Knife)) _hip = HipTool.Knife;
            y += 50f;

            // ---- the bag (cloth and leather take any amount; the others have a limit)
            if (_bagComp)
            {
                GUI.Label(new Rect(x, y, 200f, 30f), "<b>バッグ</b>", _label);
                bx = x + 190f;
                foreach (var k in BagChoices)
                {
                    string name = k == SlugExtract.BagKind.None ? "なし" : _bagComp.settings.Bag(k).displayName;
                    if (Choice(new Rect(bx, y - 2f, 88f, 34f), name, _bag == k)) _bag = k;
                    bx += 92f;
                }
                y += 40f;
                string note = _bag == SlugExtract.BagKind.None ? "エコバッグだけ（B 長押しで広げる）"
                    : BagNote(_bagComp.settings.Bag(_bag));
                GUI.Label(new Rect(x + 190f, y - 6f, PW - 230f, 24f), $"<color=#c8c8c8>{note}</color>", _note);
                y += 30f;
            }
            else y += 10f;

            // ---- how they are taken in hand
            var s = loadout.settings;
            string times = s ? $"背中の物は取り出すのに時間がかかる（銃 {s.backDrawTime:0.0#} 秒、ピッケル {s.pickaxeDrawTime:0.0#} 秒）。腰の物は速い（{s.hipDrawTime:0.0#} 秒）。" : "";
            GUI.Label(new Rect(x, y, PW - 48f, 60f), $"1 ピッケル ／ 2 背中の銃 ／ 3 腰の道具（ホイールでも）\n{times}", _note);
            y += 72f;

            bool confirm = GUI.Button(new Rect(panel.x + (PW - 220f) * 0.5f, y, 220f, 44f), $"<b>{confirmLabel}</b>（Enter）", _button);
            GUI.matrix = oldMatrix;
            if (confirm) Close();
        }

        static string BagNote(SlugExtract.BagDef d)
        {
            string cap = d.capacity > 0f ? $"かさ {d.capacity:0.#} まで" : "上限なし";
            string back = d.bodyDamage >= 0.99f ? "背中の弾は貫通" : d.bodyDamage <= 0.01f ? "背中の弾を止める" : $"背中の弾は体に ×{d.bodyDamage:0.#}";
            return $"{cap}・耐久 {d.durability:0}・{back}";
        }

        /// <summary>A button for one choice; the chosen one is lit.</summary>
        bool Choice(Rect r, string text, bool on)
        {
            var old = GUI.backgroundColor;
            if (on) GUI.backgroundColor = new Color(1f, 0.8f, 0.35f);
            bool pressed = GUI.Button(r, on ? $"<b><color=#ffe6a0>{text}</color></b>" : text, _button);
            GUI.backgroundColor = old;
            return pressed;
        }

        SlugGunHolder FindSmall()
        {
            foreach (var g in loadout.guns) if (g && g.gun && g.gun.Small) return g;
            return null;
        }
    }
}
