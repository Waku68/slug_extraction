using System.Collections.Generic;
using SlugDesign;
using SlugMove;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugExtract
{
    /// <summary>
    /// What a slug carries on its back (the bag spec, docs/bag_spec.md):
    ///  - one bag, everything jumbled in it in the order it went in (the last one is "on top"). Cloth and leather bags
    ///    take any amount; the others stop at their capacity (by bulk). The heavier, the slower: a little up to the
    ///    threshold, then clearly slower (<see cref="ExtractSettings.SpeedFor"/>). The bulkier, the more the bag swells,
    ///    and the bigger the back's hitbox;
    ///  - durability: rounds in the back wear it down (and pass on to the body, or not, by the kind of bag); at zero it
    ///    bursts and everything in it spills on the ground for anyone to pick up;
    ///  - the folding eco bag on the belt: with no bag (burst, or set out without one), hold B for a few seconds to open
    ///    it; it takes very little;
    ///  - the hiding place inside the body: one thing (never salt) pushed into the body stays there whatever happens.
    /// In a raid only four things can be done with it: put in (harvesting puts in by itself; E picks up what lies
    /// about), drop (G; hold and let go to throw), peek (hold Q: bend round to look at the back, and see what is on top
    /// and the numbers), and push the top thing into the body (X). Sorting is for the base.
    /// A bag without a <see cref="controller"/> (a practice slug) takes no input and shows nothing.
    /// </summary>
    public class SlugBag : MonoBehaviour
    {
        public ExtractSettings settings;
        [Tooltip("動かすナメクジ。空なら操作も画面の表示もない（練習用ナメクジ）")] public SlugMoveController controller;
        public SlugCameraRig cameraRig;
        public SlugPop slug;
        [Tooltip("背中のバッグの見た目")] public SlugEquipment gear;
        [Tooltip("背中の当たり判定（バッグに当たった弾で耐久が減る）")] public SlugHitboxes hitboxes;
        [Tooltip("背負うバッグの種類（出撃前の画面でも選べる）")] public BagKind kind = BagKind.Cloth;
        [Tooltip("始めから入っている物（試す用）。上から順に入れる")] public ItemKind[] startItems = new ItemKind[0];
        [Tooltip("画面に表示する")] public bool showHud = true;
        /// <summary>Holds the peek key / the eco bag key without the keyboard (tests and tools).</summary>
        [System.NonSerialized] public bool forcePeek, forceEco;

        /// <summary>What is in the bag, in the order it went in (the last is on top).</summary>
        public IReadOnlyList<ItemKind> Items => _items;
        public bool HasHidden { get; private set; }
        public ItemKind Hidden { get; private set; }
        /// <summary>The bag on the back now (the eco bag once opened; None when burst or set out without one).</summary>
        public BagKind Worn { get; private set; }
        public bool HasBag => Worn != BagKind.None;
        public BagDef Def => settings.Bag(Worn);
        public float Durability { get; private set; }
        public float Weight { get; private set; }
        public float Volume { get; private set; }
        public int Value { get; private set; }
        public int Count(ItemKind k) { int n = 0; foreach (var i in _items) if (i == k) n++; return n; }
        public bool Opening => _ecoOpening >= 0f;

        readonly List<ItemKind> _items = new List<ItemKind>();
        bool _ecoUsed;
        float _ecoOpening = -1f, _gHeld = -1f, _peek, _lastSwell = -1f;
        BagKind _lastLook = (BagKind)(-1);
        string _message;
        float _messageUntil;
        GUIStyle _big, _mid, _small;

        bool IsPlayer => controller != null;
        Collider OwnCollider => controller ? controller.GetComponent<Collider>() : null;

        void Start()
        {
            SetKind(kind);
            foreach (var k in startItems) TryAdd(k, 1);
            if (hitboxes) hitboxes.Hit += OnHit;
        }

        void OnDestroy()
        {
            if (hitboxes) hitboxes.Hit -= OnHit;
        }

        /// <summary>Puts on a bag of this kind, empty and whole (before setting out).</summary>
        public void SetKind(BagKind k)
        {
            kind = k;
            Worn = k;
            Durability = k == BagKind.None ? 0f : settings.Bag(k).durability;
            _ecoUsed = false;
            Recount();
        }

        // ------------------------------------------------------------------ putting in and taking out
        /// <summary>Whether one more of <paramref name="k"/> goes in (there is a bag, and room in it).</summary>
        public bool Fits(ItemKind k) => FitsOn(Volume, k);

        bool FitsOn(float volume, ItemKind k)
        {
            if (!HasBag) return false;
            float cap = Def.capacity;
            return cap <= 0f || volume + settings.Def(k).volume <= cap + 1e-4f;
        }

        /// <summary>Adds up to <paramref name="amount"/> of <paramref name="k"/>, as many as go in. Returns how many went in.</summary>
        public int TryAdd(ItemKind k, int amount)
        {
            int n = 0;
            float volume = Volume;
            while (n < amount && FitsOn(volume, k)) { _items.Add(k); volume += settings.Def(k).volume; n++; }
            if (n > 0) Recount();
            else if (IsPlayer) Say(HasBag ? $"{Def.displayName}がいっぱい" : Opening ? "エコバッグを広げている…" : NoBagHint());
            return n;
        }

        string NoBagHint() => _ecoUsed ? "バッグがない" : "バッグがない（B 長押しでエコバッグを広げる）";

        void Recount()
        {
            float w = HasBag ? Def.weight : 0f, v = 0f;
            int value = 0;
            foreach (var i in _items)
            {
                var d = settings.Def(i);
                w += d.weight; v += d.volume; value += d.value;
            }
            Weight = w; Volume = v; Value = value;
            ApplyLook();
        }

        /// <summary>The bag on the back: its kind's colours, swollen by its bulk; the back's hitbox follows it.</summary>
        void ApplyLook()
        {
            if (!gear) return;
            float swell = HasBag ? Mathf.Min(1f + Volume * settings.swellPerVolume * Def.swell, settings.swellVisualMax) : 1f;
            if (Worn == _lastLook && Mathf.Abs(swell - _lastSwell) < 0.02f) return;
            _lastLook = Worn;
            _lastSwell = swell;
            gear.bag = HasBag && Worn != BagKind.SnailShell;
            if (HasBag) { gear.bagColor = Def.color; gear.bagFlapColor = Def.flapColor; }
            gear.bagSwell = Worn == BagKind.Eco ? swell * 0.75f : swell;
            if (hitboxes) hitboxes.backItemMultiplier = HasBag ? Def.bodyDamage : 1f;
            gear.Build();
        }

        ItemKind? Top => _items.Count > 0 ? _items[_items.Count - 1] : (ItemKind?)null;

        ItemKind TakeTop()
        {
            var k = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            Recount();
            return k;
        }

        /// <summary>Drops the top thing at the feet (<paramref name="power"/> 0), or throws it ahead (up to 1).</summary>
        public void DropTop(float power)
        {
            if (_items.Count == 0) { Say("バッグは空"); return; }
            var k = TakeTop();
            Vector3 look = cameraRig ? Quaternion.Euler(cameraRig.Pitch, cameraRig.Yaw, 0f) * Vector3.forward : transform.forward;
            Vector3 flat = Vector3.ProjectOnPlane(look, Vector3.up).normalized;
            Vector3 from = slug ? slug.FaceCenterWorld + flat * 0.12f : transform.position + Vector3.up * 0.4f;
            Vector3 vel = power <= 0f ? flat * 0.6f + Vector3.up * 0.4f : look * Mathf.Lerp(2.5f, 7f, power) + Vector3.up * 1.6f;
            ItemPickup.Spawn(settings, k, from, vel, OwnCollider);
            Say(power <= 0f ? $"{settings.Def(k).displayName}を落とした" : $"{settings.Def(k).displayName}を投げた");
        }

        /// <summary>Picks up the nearest thing lying in front of the face, if it fits.</summary>
        void PickUp(ItemPickup p)
        {
            if (TryAdd(p.kind, 1) == 0) return;
            Say($"{settings.Def(p.kind).displayName}を拾った");
            Destroy(p.gameObject);
        }

        /// <summary>Pushes the top thing into the body: it stays there whatever happens (never salt: it would melt the slug).</summary>
        public void HideTop()
        {
            if (HasHidden) { Say($"体の中にはもう{settings.Def(Hidden).displayName}が入っている"); return; }
            if (_items.Count == 0) { Say("バッグは空"); return; }
            var d = settings.Def(_items[_items.Count - 1]);
            if (!d.canHide) { Say($"{d.displayName}は体にしまえない（塩で溶ける）"); return; }
            Hidden = TakeTop();
            HasHidden = true;
            Say($"{d.displayName}を体の中に突っ込んだ。もう失わない");
        }

        // ------------------------------------------------------------------ rounds in the back
        void OnHit(SlugHitbox box, RaycastHit hit)
        {
            if (!HasBag || (box.part != "バッグ" && box.part != "殻")) return;
            float power = box.LastVelocity.magnitude / 140f;
            Durability -= settings.hitDamage * Mathf.Clamp(power, 0.3f, 3f);
            if (Durability <= 0f) Burst(hit.point);
        }

        /// <summary>The bag gives out: everything in it bursts out over the ground.</summary>
        void Burst(Vector3 at)
        {
            Vector3 center = at;
            if (gear && gear.HasBackItem && slug) center = slug.FollowPointWorld(gear.BackItemCenter, gear.BackItemT, gear.BackItemNormal);
            var ignore = OwnCollider;
            foreach (var k in _items)
            {
                Vector3 v = Random.insideUnitSphere * 1.6f + Vector3.up * Random.Range(1.2f, 2.4f);
                ItemPickup.Spawn(settings, k, center + Random.insideUnitSphere * 0.06f, v, ignore);
            }
            int n = _items.Count;
            _items.Clear();
            string name = Def.displayName;
            Worn = BagKind.None;
            Durability = 0f;
            Recount();
            if (IsPlayer) Say(n > 0 ? $"{name}が破れて、中身が{n}個こぼれた！" : $"{name}が破れた");
        }

        // ------------------------------------------------------------------ input (the player only)
        void Update()
        {
            if (!IsPlayer) return;
            bool locked = Cursor.lockState == CursorLockMode.Locked && controller.inputEnabled;
            var kb = Keyboard.current;
            float dt = Time.deltaTime;

            // peek: bend round to look at the back while Q is held
            bool peeking = forcePeek || (locked && kb != null && kb.qKey.isPressed && !Opening);
            _peek = Mathf.MoveTowards(_peek, peeking ? 1f : 0f, dt / 0.22f);
            if (cameraRig)
            {
                cameraRig.peek = _peek;
                cameraRig.peekTarget = BagCenterWorld();
                cameraRig.handsBusy = Opening;
            }

            if (locked && kb != null)
            {
                // drop (tap G) or throw (hold G, let go)
                if (kb.gKey.wasPressedThisFrame) _gHeld = 0f;
                if (_gHeld >= 0f)
                {
                    _gHeld += dt;
                    if (!kb.gKey.isPressed)
                    {
                        DropTop(_gHeld < 0.2f ? 0f : Mathf.Clamp01((_gHeld - 0.2f) / 0.5f));
                        _gHeld = -1f;
                    }
                }
                if (kb.eKey.wasPressedThisFrame)
                {
                    var p = ItemPickup.Nearest(PickupPoint(), settings.pickupReach);
                    if (p) PickUp(p);
                }
                if (kb.xKey.wasPressedThisFrame) HideTop();
            }
            else _gHeld = -1f;

            // the eco bag: hold B for a while to open it (only without a bag, and only once)
            bool wantEco = !HasBag && !_ecoUsed && (forceEco || (locked && kb != null && kb.bKey.isPressed));
            if (wantEco)
            {
                if (_ecoOpening < 0f) _ecoOpening = 0f;
                _ecoOpening += dt / Mathf.Max(settings.ecoOpenTime, 0.1f);
                if (_ecoOpening >= 1f)
                {
                    _ecoOpening = -1f;
                    _ecoUsed = true;
                    Worn = BagKind.Eco;
                    Durability = settings.eco.durability;
                    Recount();
                    Say("エコバッグを広げた");
                }
            }
            else _ecoOpening = -1f;

            float speed = settings.SpeedFor(Weight);
            if (_peek > 0f) speed *= Mathf.Lerp(1f, settings.peekSpeed, _peek);
            if (Opening) speed *= 0.3f;
            controller.speedMultiplier = speed;
        }

        Vector3 PickupPoint()
        {
            var t = controller.transform;
            return t.position + t.forward * 0.25f + Vector3.up * 0.1f;
        }

        Vector3 BagCenterWorld()
        {
            if (slug && gear && gear.HasBackItem) return slug.FollowPointWorld(gear.BackItemCenter, gear.BackItemT, gear.BackItemNormal);
            return transform.position + Vector3.up * 0.3f - transform.forward * 0.2f;
        }

        void Say(string text)
        {
            _message = text;
            _messageUntil = Time.time + 2.2f;
        }

        // ------------------------------------------------------------------ HUD: the bag's shape (always), the numbers (peeking), prompts
        void Styles()
        {
            if (_big != null) return;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, richText = true };
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.UpperLeft, richText = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, richText = true };
            foreach (var st in new[] { _big, _mid, _small }) st.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            if (!IsPlayer || !showHud) return;
            Styles();
            float w = Screen.width, h = Screen.height;
            var old = GUI.color;

            // ---- top left, always: the bag's silhouette swelling with its bulk, coloured by its weight; no numbers
            GUI.Box(new Rect(10, 10, 96, 112), GUIContent.none);
            if (HasBag)
            {
                float swell = Mathf.Min(1f + Volume * settings.swellPerVolume * Def.swell, settings.swellVisualMax);
                float bw = 34f * (1f + 0.3f * (swell - 1f)), bh = 40f * (1f + 0.55f * (swell - 1f));
                float over = Weight / Mathf.Max(settings.weightThreshold, 1e-3f);
                GUI.color = over <= 1f ? Color.Lerp(new Color(0.75f, 0.85f, 0.65f), new Color(0.95f, 0.8f, 0.45f), over)
                                       : Color.Lerp(new Color(1f, 0.6f, 0.35f), new Color(1f, 0.35f, 0.3f), Mathf.Clamp01(over - 1f));
                GUI.DrawTexture(new Rect(58 - bw * 0.5f, 92 - bh, bw, bh), Texture2D.whiteTexture);
                // how much of the bag is left (a thin line under it)
                float dur = Durability / Mathf.Max(Def.durability, 1f);
                GUI.color = dur > 0.35f ? new Color(1f, 1f, 1f, 0.8f) : new Color(1f, 0.4f, 0.35f, 0.95f);
                GUI.DrawTexture(new Rect(20, 100, 76f * dur, 4), Texture2D.whiteTexture);
                GUI.color = old;
                GUI.Label(new Rect(14, 10, 90, 22), $"<size=13>{Def.displayName}</size>", _mid);
            }
            else
            {
                GUI.color = old;
                GUI.Label(new Rect(14, 40, 90, 40), "<size=14><color=#ff9a8a>バッグなし</color></size>", _mid);
            }
            if (HasHidden)
            {
                GUI.color = new Color(0.75f, 0.95f, 1f, 0.95f);
                GUI.DrawTexture(new Rect(88, 14, 10, 10), Texture2D.whiteTexture);     // something is safe inside the body
                GUI.color = old;
            }

            // ---- peeking: what is on top, and the numbers
            if (_peek > 0.6f)
            {
                var r = new Rect(10, 128, 400, 150);          // under the bag's shape, clear of the back in view
                GUI.Box(r, GUIContent.none);
                GUI.Box(r, GUIContent.none);
                string top = Top.HasValue ? settings.Def(Top.Value).displayName : "（空）";
                GUI.Label(new Rect(r.x, r.y + 6, r.width, 34), $"<size=22>一番上：<b>{top}</b></size>", _big);
                string cap = HasBag && Def.capacity > 0f ? $" / {Def.capacity:0.#}" : "";
                string dur = HasBag ? $"{Mathf.CeilToInt(Durability)} / {Def.durability:0}" : "—";
                string hidden = HasHidden ? settings.Def(Hidden).displayName : "空";
                GUI.Label(new Rect(r.x + 14, r.y + 46, r.width - 20, 100),
                    $"重さ  {Weight:0.0}（閾値 {settings.weightThreshold:0}）  速さ ×{settings.SpeedFor(Weight):0.00}\n" +
                    $"中身  {_items.Count} 個（かさ {Volume:0.#}{cap}）  価値 {Value} 塩\n" +
                    $"耐久  {dur}　　体の中  {hidden}", _mid);
            }

            // ---- prompts and messages
            float y = Mathf.Min(h * 0.5f + 60f, h - 200f);      // above the tool slots
            if (_peek < 0.99f)
            {
                var p = ItemPickup.Nearest(PickupPoint(), settings.pickupReach);
                if (p) { GUI.Label(new Rect(0, y, w, 26), $"E  拾う：{settings.Def(p.kind).displayName}", _small); y += 26f; }
            }
            if (!HasBag && !_ecoUsed)
            {
                GUI.Label(new Rect(0, y, w, 26), "<color=#ffd9a0>B 長押しでエコバッグを広げる</color>", _small);
                y += 26f;
                if (Opening)
                {
                    GUI.Box(new Rect(w * 0.5f - 100, y + 4, 200, 10), GUIContent.none);
                    GUI.color = new Color(1f, 0.85f, 0.5f);
                    GUI.DrawTexture(new Rect(w * 0.5f - 100, y + 4, 200 * _ecoOpening, 10), Texture2D.whiteTexture);
                    GUI.color = old;
                    y += 18f;
                }
            }
            if (_message != null && Time.time < _messageUntil) GUI.Label(new Rect(0, y, w, 26), _message, _small);
        }
    }
}
