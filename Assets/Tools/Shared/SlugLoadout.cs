using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>What the slug carries on the hip.</summary>
    public enum HipTool { Pistol, Knife }

    /// <summary>
    /// What the slug carries and which of it is in hand. The back holds a big gun and the pickaxe, the hip a small gun or
    /// a knife (chosen before setting out, <see cref="SetLoadout"/>). 1 = the pickaxe, 2 = the back gun, 3 = the hip tool;
    /// the mouse wheel steps through them. Only the held tool's component is enabled; the others ride on the body
    /// (<see cref="SlugToolRack"/>). Taking a tool off the back is slow, off the hip quick (<see cref="LoadoutSettings"/>);
    /// it cannot be used until it is in hand. A tool from the back is put back there first, the way it came
    /// (<see cref="BackDraw"/>), and only then is the next one taken. Runs before the tools so the ones not held never show up on the first frame.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SlugLoadout : MonoBehaviour
    {
        public const int PickaxeSlot = 0, BackSlot = 1, HipSlot = 2;

        public LoadoutSettings settings;
        public SlugToolHolder pickaxe;
        [Tooltip("持って出られる銃（背中の大きい銃と、腰の小さい銃）")] public SlugGunHolder[] guns = new SlugGunHolder[0];
        public SlugKnifeHolder knife;

        [Header("持って出る物")]
        [Tooltip("背中の銃")] public GunKind backGun = GunKind.Rifle;
        [Tooltip("腰の道具")] public HipTool hip = HipTool.Pistol;
        [Tooltip("最初に持つ道具（0 = ピッケル、1 = 背中の銃、2 = 腰の道具）")] public int startWith;
        [Tooltip("false の間は持ち替えられない（試合の終了後など）")] public bool inputEnabled = true;
        /// <summary>Switched by something else (a bot, later the network) through <see cref="Select"/>: no keys are read.</summary>
        [System.NonSerialized] public bool driven;
        [Tooltip("持ち物の欄を画面に出す")] public bool showHud = true;

        /// <summary>The slot in hand: <see cref="PickaxeSlot"/>, <see cref="BackSlot"/> or <see cref="HipSlot"/>.</summary>
        public int Current { get; private set; }
        /// <summary>Something else is in the hands (a chunk of salt being carried): every tool is put away and none can be taken.</summary>
        public bool HandsFull { get; private set; }
        public const int Count = 3;
        /// <summary>Called when the carried things or the one in hand change.</summary>
        public event System.Action Changed;

        /// <summary>The big gun on the back (null if none of that kind is carried).</summary>
        public SlugGunHolder BackGun => Find(backGun, false);
        /// <summary>The small gun on the hip, when the hip carries one.</summary>
        public SlugGunHolder HipGun => hip == HipTool.Pistol ? Find(GunKind.Pistol, true) : null;
        /// <summary>The gun in hand, or null (pickaxe or knife).</summary>
        public SlugGunHolder CurrentGun => Tool(Current) as SlugGunHolder;
        /// <summary>The component of what is in hand.</summary>
        public MonoBehaviour CurrentTool => Tool(Current);

        GUIStyle _style, _small;
        SlugCameraRig _rig;
        SlugCameraRig Rig => _rig ? _rig : (_rig = GetComponent<SlugCameraRig>());

        SlugGunHolder Find(GunKind kind, bool small)
        {
            foreach (var g in guns) if (g && g.gun && g.gun.kind == kind && g.gun.Small == small) return g;
            return null;
        }

        /// <summary>The component in a slot (null if nothing is carried there).</summary>
        public MonoBehaviour Tool(int slot) => slot switch
        {
            PickaxeSlot => pickaxe,
            BackSlot => BackGun,
            HipSlot => hip == HipTool.Knife ? knife : HipGun,
            _ => null,
        };

        /// <summary>The name shown for a slot.</summary>
        public string SlotName(int slot)
        {
            var t = Tool(slot);
            if (t is SlugGunHolder g) return g.gun.displayName;
            if (t is SlugKnifeHolder) return "ナイフ";
            if (t is SlugToolHolder) return "ピッケル";
            return "なし";
        }

        /// <summary>0..1: how far the tool in hand has been taken in hand (1 = ready to use).</summary>
        public float DrawProgress
        {
            get
            {
                if (_stowTool) return 0f;      // the one before is still being put away
                var t = CurrentTool;
                if (t is SlugGunHolder g) return g.Ready ? g.Handling.EquipLinear : 0f;
                if (t is SlugToolHolder p) return p.DrawProgress;
                if (t is SlugKnifeHolder k) return k.DrawProgress;
                return 1f;
            }
        }

        /// <summary>The slot whose tool is in the hands right now: the one being put away while that lasts, else <see cref="Current"/>.</summary>
        public int InHand => _stowTool ? _stowSlot : Current;

        /// <summary>0..1: how far <paramref name="holder"/>'s tool has been put away onto the back (0 when it is not being put away).</summary>
        public float PutAway(MonoBehaviour holder) => holder && holder == _stowTool ? _stow : 0f;
        /// <summary><paramref name="holder"/>'s tool is on its way back onto the body (it cannot be used meanwhile).</summary>
        public bool PuttingAway(MonoBehaviour holder) => holder && holder == _stowTool;

        // a tool off the back is put away before the next one is taken: back along the way it came, then the hand comes back
        MonoBehaviour _stowTool;
        int _stowSlot;
        float _stow;
        bool _stowUndo;      // changed mind: the one going away comes back into the hand

        void Awake() { Select(startWith, true, true); }

        void Update()
        {
            StepStow(Time.deltaTime);
            if (driven || !inputEnabled || HandsFull || Cursor.lockState != CursorLockMode.Locked || (Rig && Rig.HandsBusy)) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key };
                for (int i = 0; i < keys.Length; i++)
                    if (keys[i].wasPressedThisFrame) Select(i);
            }
            var mouse = Mouse.current;
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(scroll) > 0.1f)
            {
                // step to the next slot that has something in it
                int dir = scroll < 0f ? 1 : -1;
                for (int k = 1; k < Count; k++)
                {
                    int next = ((Current + dir * k) % Count + Count) % Count;
                    if (Tool(next)) { Select(next); break; }
                }
            }
        }

        /// <summary>
        /// Takes the tool in <paramref name="slot"/> in hand (from where it is carried, which takes a moment;
        /// <paramref name="instant"/> skips that). The same slot again does nothing unless <paramref name="redraw"/>.
        /// </summary>
        public void Select(int slot, bool instant = false, bool redraw = false)
        {
            slot = Mathf.Clamp(slot, 0, Count - 1);
            if (!Tool(slot)) return;
            if (_stowTool)
            {
                if (instant || redraw) FinishStow(false);      // put away at once, then taken as usual below
                else
                {
                    // while putting away: the same one again brings it back into the hand, another one waits its turn
                    _stowUndo = slot == _stowSlot;
                    if (Current != slot) { Current = slot; Changed?.Invoke(); }
                    return;
                }
            }
            if (slot == Current && !redraw && Tool(slot).enabled) return;
            // what is in hand now goes back first if it rides on the back (the hip is quick: put away at once)
            var from = Tool(Current);
            if (!instant && !HandsFull && settings && settings.backStowTime > 0f && Current != HipSlot && from && from.enabled && from != Tool(slot))
            {
                _stow = 1f - DrawProgress;      // still on its way out: it goes back from where it got to
                _stowTool = from;
                _stowSlot = Current;
                _stowUndo = false;
                Current = slot;
                Changed?.Invoke();
                return;
            }
            Take(slot, instant, redraw);
        }

        void StepStow(float dt)
        {
            if (!_stowTool) return;
            _stow = Mathf.MoveTowards(_stow, _stowUndo ? 0f : 1f, dt / Mathf.Max(settings ? settings.backStowTime : 0f, 1e-3f));
            if (_stowUndo && _stow <= 0f)
            {
                // back in the hand as it was, ready at once
                _stowTool = null;
                Current = _stowSlot;
                Changed?.Invoke();
            }
            else if (!_stowUndo && _stow >= 1f) FinishStow(true);
        }

        /// <summary>The tool being put away is on the back now; the chosen one is taken in hand (<paramref name="take"/>).</summary>
        void FinishStow(bool take)
        {
            var t = _stowTool;
            _stowTool = null;
            if (t) t.enabled = false;
            if (take) Take(Current, false, false);
        }

        void Take(int slot, bool instant, bool redraw)
        {
            Current = slot;
            var held = Tool(slot);
            float time = instant ? 0f : DrawTime(slot);
            if (held is SlugGunHolder gun) { gun.drawTime = time; gun.drawFromBack = slot == BackSlot; }
            if (held is SlugToolHolder pick) pick.drawTime = time;
            if (held is SlugKnifeHolder kn) kn.drawTime = time;
            // put everything else away first (a holder that is re-enabled starts its draw again)
            if (pickaxe && pickaxe != held) pickaxe.enabled = false;
            if (knife && knife != held) knife.enabled = false;
            foreach (var g in guns) if (g && g != held) g.enabled = false;
            if (redraw && held.enabled) held.enabled = false;
            held.enabled = true;
            Changed?.Invoke();
        }

        float DrawTime(int slot)
        {
            if (!settings) return -1f;
            return settings.DrawTime(slot != HipSlot, slot == PickaxeSlot);
        }

        /// <summary>
        /// The hands take something (true): every tool is put away where it rides. Free again (false): the tool that was in
        /// hand is taken in hand again (from the back, that takes its moment).
        /// </summary>
        public void SetHandsFull(bool full)
        {
            if (full == HandsFull) return;
            HandsFull = full;
            if (full)
            {
                _stowTool = null;
                if (pickaxe) pickaxe.enabled = false;
                if (knife) knife.enabled = false;
                foreach (var g in guns) if (g) g.enabled = false;
                Changed?.Invoke();
            }
            else Select(Current, false, true);
        }

        /// <summary>Sets what is carried (before setting out). The tool in hand is taken in hand again from where it now rides.</summary>
        public void SetLoadout(GunKind back, HipTool hipTool)
        {
            backGun = back;
            hip = hipTool;
            Select(Tool(Current) ? Current : PickaxeSlot, false, true);
        }

        // ------------------------------------------------------------------ HUD: the three slots, the one in hand lit, its draw progress under it
        void OnGUI()
        {
            if (!showHud) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter, richText = true };
                _style.normal.textColor = Color.white;
                _small = new GUIStyle(_style) { fontSize = 12 };
            }
            const float W = 150f, H = 44f, Gap = 6f;
            float x0 = Screen.width * 0.5f - (W * Count + Gap * (Count - 1)) * 0.5f, y = Screen.height - 112f;
            var old = GUI.color;
            for (int i = 0; i < Count; i++)
            {
                var r = new Rect(x0 + i * (W + Gap), y, W, H);
                bool on = i == Current && !HandsFull;
                GUI.color = on ? new Color(1f, 0.92f, 0.6f, 1f) : new Color(1f, 1f, 1f, 0.55f);
                GUI.Box(r, GUIContent.none);
                string where = i == HipSlot ? "腰" : "背中";
                GUI.Label(new Rect(r.x, r.y + 2f, r.width, 26f), $"<b>{i + 1}</b>  {SlotName(i)}", _style);
                GUI.Label(new Rect(r.x, r.y + 24f, r.width, 16f), where, _small);
                if (on && DrawProgress < 1f)
                {
                    GUI.color = new Color(1f, 0.85f, 0.4f, 0.9f);
                    GUI.DrawTexture(new Rect(r.x + 4f, r.yMax - 5f, (r.width - 8f) * DrawProgress, 3f), Texture2D.whiteTexture);
                }
            }
            GUI.color = old;
        }
    }
}
