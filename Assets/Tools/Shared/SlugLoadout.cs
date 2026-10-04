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
    /// it cannot be used until it is in hand. Runs before the tools so the ones not held never show up on the first frame.
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
        [Tooltip("持ち物の欄を画面に出す")] public bool showHud = true;

        /// <summary>The slot in hand: <see cref="PickaxeSlot"/>, <see cref="BackSlot"/> or <see cref="HipSlot"/>.</summary>
        public int Current { get; private set; }
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
                var t = CurrentTool;
                if (t is SlugGunHolder g) return g.Ready ? g.Handling.EquipLinear : 0f;
                if (t is SlugToolHolder p) return p.DrawProgress;
                if (t is SlugKnifeHolder k) return k.DrawProgress;
                return 1f;
            }
        }

        void Awake() { Select(startWith, true, true); }

        void Update()
        {
            if (!inputEnabled || Cursor.lockState != CursorLockMode.Locked || (Rig && Rig.HandsBusy)) return;
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
            if (slot == Current && !redraw && Tool(slot).enabled) return;
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
                bool on = i == Current;
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
