using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>
    /// Which tool the slug has in hand: 1 = pickaxe, 2 and up = the guns in order (rifle, shotgun, sniper); the mouse
    /// wheel steps through them. Only the held tool's component is enabled; putting one away hides it, taking a gun
    /// draws it up from below. Runs before the tools so the ones not held never show up on the first frame.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SlugLoadout : MonoBehaviour
    {
        public SlugToolHolder pickaxe;
        [Tooltip("持ち替えられる銃（2 キーから順に）")] public SlugGunHolder[] guns = new SlugGunHolder[0];
        [Tooltip("最初に持つ道具（0 = ピッケル、1 から = 銃）")] public int startWith;
        [Tooltip("false の間は持ち替えられない（試合の終了後など）")] public bool inputEnabled = true;

        /// <summary>0 = the pickaxe, 1.. = guns[Current - 1].</summary>
        public int Current { get; private set; }
        public int Count => 1 + guns.Length;
        /// <summary>The gun in hand, or null while holding the pickaxe.</summary>
        public SlugGunHolder CurrentGun => Current > 0 && Current <= guns.Length ? guns[Current - 1] : null;

        void Awake() { Select(startWith); }

        void Update()
        {
            if (!inputEnabled || Cursor.lockState != CursorLockMode.Locked) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key };
                for (int i = 0; i < Mathf.Min(keys.Length, Count); i++)
                    if (keys[i].wasPressedThisFrame) Select(i);
            }
            var mouse = Mouse.current;
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(scroll) > 0.1f) Select((Current + (scroll < 0f ? 1 : -1) + Count) % Count);
        }

        public void Select(int tool)
        {
            Current = Mathf.Clamp(tool, 0, Count - 1);
            if (pickaxe) pickaxe.enabled = Current == 0;
            for (int i = 0; i < guns.Length; i++)
                if (guns[i]) guns[i].enabled = Current == i + 1;
        }
    }
}
