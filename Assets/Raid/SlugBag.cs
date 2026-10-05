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
    /// Everything goes through the hands (gloved, so salt does not burn them). In a raid: E takes what lies in front
    /// (a chunk just struck out of a rock, something dropped) in the hands, and with something in the hands E puts it in
    /// the bag (over the shoulder); G takes the top thing out of the bag into the hands, and with something in the hands
    /// G drops it (hold and let go to throw); X pushes what is in the hands into the body; Q (hold) turns the head round
    /// to look at the back and flips the bag's lid open (everyone sees both); in the own view the bag on the back is
    /// drawn big and tipped toward the eyes: in its mouth the top two things, drawn chunky and easy to tell apart
    /// (three, spilling over, when it is crammed), and the numbers. While the hands are full the tools are put away.
    /// C (hold) sets the bag down on the ground, mouth open, or shoulders one lying there: things go into and come out
    /// of a bag on the ground much faster (<see cref="GroundBag"/>), for anyone, so a fallen slug's bag (dropped where
    /// it fell) can be emptied into one's own quickly by setting one's own down beside it.
    /// Sorting is for the base.
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

        public enum CarryPhase { None, Grab, Hold, Stow, TakeOut, Hide, Unhide }
        /// <summary>What the hands are doing with a thing (the tools are put away meanwhile).</summary>
        public CarryPhase Carry { get; private set; }
        /// <summary>0..1 through grabbing, putting in, or taking out.</summary>
        public float CarryProgress { get; private set; }
        public bool Holding => Carry != CarryPhase.None;
        public ItemKind HeldKind { get; private set; }
        /// <summary>Where a grabbed thing lay (world), for the reach to it.</summary>
        public Vector3 GrabFrom { get; private set; }
        /// <summary>Where the thing in the hands is this frame (world; kept up to date by <see cref="SlugCarry"/>).</summary>
        [System.NonSerialized] public Vector3 heldWorld;
        /// <summary>0..1: how far the slug is bent round, looking at its back.</summary>
        public float Peek => _peek;

        readonly List<ItemKind> _items = new List<ItemKind>();
        SlugLoadout _loadout;
        SelfPortrait _portrait;
        // the things seen in the bag's mouth while peeking (drawn only for the own camera)
        readonly GameObject[] _shown = new GameObject[3];
        readonly ItemKind[] _shownKind = new ItemKind[3];
        readonly Renderer[][] _shownRenderers = { new Renderer[0], new Renderer[0], new Renderer[0] };
        Vector3 _ownMouth;
        bool _ownMouthValid;
        bool _ecoUsed;
        float _ecoOpening = -1f, _gHeld = -1f, _peek, _lastSwell = -1f;
        BagKind _lastLook = (BagKind)(-1);
        string _message;
        float _messageUntil;
        GUIStyle _big, _mid, _small;

        public bool IsPlayer => controller != null;
        Collider OwnCollider => controller ? controller.GetComponent<Collider>() : null;

        void Start()
        {
            _loadout = GetComponent<SlugLoadout>();
            _portrait = GetComponent<SelfPortrait>();
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            SetKind(kind);
            foreach (var k in startItems) TryAdd(k, 1);
            if (hitboxes) hitboxes.Hit += OnHit;
            _health = GetComponent<SlugHealth>() ?? GetComponentInParent<SlugHealth>();
            if (_health) _health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (hitboxes) hitboxes.Hit -= OnHit;
            if (_health) _health.Died -= OnDied;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            foreach (var g in _shown) if (g) Destroy(g);
        }

        /// <summary>
        /// Poses the bag on the back for each camera: the lid open while peeking (for everyone), and for the own camera
        /// only the bag grown and tipped toward the eyes, with the top things in its mouth.
        /// </summary>
        void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (!IsPlayer || !gear || !cameraRig) return;
            float e = Ease(_peek);
            bool own = camera == cameraRig.cam;
            bool posed = gear.PoseBag(e, own ? e : 0f, cameraRig.cam.transform.position);
            bool show = own && posed;
            if (own) { _ownMouthValid = posed; _ownMouth = gear.BagMouthPosition; }
            Vector3 half = gear.BackItemHalf / 1.05f;
            float s = gear.BagPoseScale;
            bool crammed = Crammed;
            for (int i = 0; i < _shown.Length; i++)
            {
                if (!_shown[i] || !_shown[i].activeSelf) continue;
                foreach (var r in _shownRenderers[i]) if (r) r.enabled = show;
                if (!show) continue;
                // chunky, the same size whatever the thing (easy to tell apart), half sunk in the mouth
                float size = half.x * (crammed ? 0.8f : 0.65f) * s;
                Vector3 at = crammed
                    ? (i == 0 ? new Vector3(0.05f, 0.45f, 0.1f) : i == 1 ? new Vector3(-0.42f, 0.25f, -0.05f) : new Vector3(0.48f, 0.2f, -0.12f))
                    : (i == 0 ? new Vector3(0.12f, 0.25f, 0.12f) : new Vector3(-0.35f, 0.05f, -0.1f));
                Vector3 local = new Vector3(at.x * half.x, at.y * size / s, at.z * half.z) * s;
                var t = _shown[i].transform;
                t.position = gear.BagMouthPosition + gear.BagMouthRotation * local;
                t.rotation = gear.BagMouthRotation * Quaternion.Euler(15f + 40f * i, 25f - 70f * i, 10f * i);
                t.localScale = Vector3.one * (size * ItemPickup.Scale(_shownKind[i]));
            }
        }

        static float Ease(float x) => x * x * (3f - 2f * x);

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
            if (Holding) w += settings.Def(HeldKind).weight;      // what is in the hands is carried too
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

        // ------------------------------------------------------------------ the hands
        void StartCarry(CarryPhase phase, ItemKind k)
        {
            _picking = false;
            _ground = null;
            HeldKind = k;
            Carry = phase;
            CarryProgress = 0f;
            if (_loadout) _loadout.SetHandsFull(true);
            Recount();
        }

        void EndCarry()
        {
            Carry = CarryPhase.None;
            CarryProgress = 0f;
            if (_loadout) _loadout.SetHandsFull(false);
            Recount();
        }

        /// <summary>Takes the thing lying there in the hands.</summary>
        public void Grab(ItemPickup p)
        {
            if (Holding || !p) return;
            GrabFrom = p.transform.position;
            StartCarry(CarryPhase.Grab, p.kind);
            Did?.Invoke("grab", p.kind);
            Destroy(p.gameObject);
        }

        bool _picking;      // the hands are picking a soft plant (slower than taking something lying there)
        GroundBag _ground;  // the bag on the ground being filled or emptied (null: the one on the back)
        float _bagAction = -1f;     // 0..1 through setting the bag down or shouldering one (C held)
        SlugHealth _health;

        /// <summary>Things go into or come out of a bag on the ground (quick and low, not over the shoulder).</summary>
        public bool GroundTransfer => _ground && (Carry == CarryPhase.Stow || Carry == CarryPhase.TakeOut);
        /// <summary>Setting the bag down or shouldering one (slow meanwhile).</summary>
        public bool BagAction => _bagAction >= 0f;
        bool CanSetDown => HasBag && Worn != BagKind.SnailShell;

        /// <summary>Something done with the hands or the bag, for sounds: (what, which item). What: grab, pick, takeout,
        /// stow, drop, throw, hide, unhide, burst, eco, peek.</summary>
        public event System.Action<string, ItemKind> Did;

        /// <summary>Picks one off a soft plant (a flower, a mushroom) into the hands.</summary>
        public void Pick(Pickable plant)
        {
            if (Holding || !plant || !plant.HasAny) return;
            GrabFrom = plant.NextPoint;
            var k = plant.kind;
            if (!plant.Take()) return;
            StartCarry(CarryPhase.Grab, k);
            _picking = true;
            Did?.Invoke("pick", k);
        }

        /// <summary>Reaches over the shoulder and takes the top thing out of the bag into the hands.</summary>
        public void TakeOut()
        {
            if (Holding) return;
            var g = IsPlayer ? GroundBag.Nearest(PickupPoint(), settings.groundReach) : null;
            if (g && !g.Empty)
            {
                GrabFrom = g.MouthWorld;
                StartCarry(CarryPhase.TakeOut, g.TakeTop());
                _ground = g;
                Did?.Invoke("takeout", HeldKind);
                return;
            }
            if (_items.Count == 0) { Say(HasBag ? "バッグは空" : NoBagHint()); return; }
            StartCarry(CarryPhase.TakeOut, TakeTop());
            Did?.Invoke("takeout", HeldKind);
        }

        /// <summary>Puts what is in the hands in the bag, over the shoulder (if it fits; else it stays in the hands).</summary>
        public void Stow()
        {
            if (Carry != CarryPhase.Hold) return;
            if (!HasBag)
            {
                var g = GroundBag.Nearest(PickupPoint(), settings.groundReach, this, true) ?? GroundBag.Nearest(PickupPoint(), settings.groundReach);
                if (g)
                {
                    if (!g.Fits(HeldKind)) { Say($"{g.Def.displayName}がいっぱい"); return; }
                    _ground = g;
                    Carry = CarryPhase.Stow;
                    CarryProgress = 0f;
                    return;
                }
            }
            if (!Fits(HeldKind)) { Say(HasBag ? $"{Def.displayName}がいっぱい" : NoBagHint()); return; }
            Carry = CarryPhase.Stow;
            CarryProgress = 0f;
        }

        /// <summary>Lets go of what is in the hands: drops it (<paramref name="power"/> 0) or throws it ahead (up to 1).</summary>
        public void DropHeld(float power)
        {
            if (Carry != CarryPhase.Hold) return;
            var k = HeldKind;
            Vector3 look = cameraRig ? Quaternion.Euler(cameraRig.Pitch, cameraRig.Yaw, 0f) * Vector3.forward : transform.forward;
            Vector3 flat = Vector3.ProjectOnPlane(look, Vector3.up).normalized;
            Vector3 vel = power <= 0f ? flat * 0.5f + Vector3.up * 0.3f : look * Mathf.Lerp(2.5f, 6.5f, power) + Vector3.up * 1.6f;
            ItemPickup.Spawn(settings, k, heldWorld, vel, OwnCollider);
            EndCarry();
            Did?.Invoke(power <= 0f ? "drop" : "throw", k);
            Say(power <= 0f ? $"{settings.Def(k).displayName}を落とした" : $"{settings.Def(k).displayName}を投げた");
        }

        /// <summary>Pushes what is in the hands into the body: it stays there whatever happens (never salt: it would melt the slug).</summary>
        public void HideHeld()
        {
            if (Carry != CarryPhase.Hold) return;
            var d = settings.Def(HeldKind);
            if (HasHidden) { Say($"体の中にはもう{settings.Def(Hidden).displayName}が入っている"); return; }
            if (!d.canHide) { Say($"{d.displayName}は体にしまえない（塩で溶ける）"); return; }
            Carry = CarryPhase.Hide;          // pressed into the belly over a moment (SlugCarry shows it)
            CarryProgress = 0f;
        }

        /// <summary>Pulls the thing hidden in the body back out into the hands.</summary>
        public void TakeOutHidden()
        {
            if (Holding || !HasHidden) return;
            var k = Hidden;
            HasHidden = false;
            StartCarry(CarryPhase.Unhide, k);
            Did?.Invoke("unhide", k);
        }

        void StepCarry(float dt)
        {
            if (!Holding || Carry == CarryPhase.Hold) return;
            float time = GroundTransfer ? settings.groundTransferTime : Carry == CarryPhase.Grab ? (_picking ? settings.pickTime : settings.grabTime) : Carry == CarryPhase.Stow ? settings.stowTime
                : Carry == CarryPhase.Hide || Carry == CarryPhase.Unhide ? settings.hideTime : settings.takeOutTime;
            CarryProgress = Mathf.MoveTowards(CarryProgress, 1f, dt / Mathf.Max(time, 0.05f));
            if (CarryProgress < 1f) return;
            if (Carry == CarryPhase.Stow && _ground)
            {
                var k = HeldKind;
                if (_ground.Fits(k))
                {
                    var g = _ground;
                    Carry = CarryPhase.None;
                    g.Add(k);
                    EndCarry();
                    Did?.Invoke("stow", k);
                    Say($"{settings.Def(k).displayName}を地面の{g.Def.displayName}に入れた");
                }
                else { Carry = CarryPhase.Hold; Recount(); }
                _ground = null;
                return;
            }
            if (Carry == CarryPhase.Stow)
            {
                var k = HeldKind;
                Carry = CarryPhase.None;            // out of the hands first (so the weight is not counted twice)
                if (TryAdd(k, 1) == 1) { EndCarry(); Did?.Invoke("stow", k); Say($"{settings.Def(k).displayName}をバッグに入れた"); }
                else { HeldKind = k; Carry = CarryPhase.Hold; Recount(); }   // it no longer fits (the bag burst meanwhile): still in hand
                return;
            }
            if (Carry == CarryPhase.Hide)
            {
                Hidden = HeldKind;
                HasHidden = true;
                EndCarry();
                Did?.Invoke("hide", Hidden);
                Say($"{settings.Def(Hidden).displayName}を体の中に突っ込んだ。もう失わない");
                return;
            }
            Carry = CarryPhase.Hold;
            CarryProgress = 0f;
            _ground = null;
        }

        // ------------------------------------------------------------------ the bag on the ground
        /// <summary>C held: sets the bag on the back down, or (with none on the back) shoulders the bag lying near.</summary>
        void StepBagAction(bool held, float dt)
        {
            bool down = CanSetDown;
            var near = down ? null : GroundBag.Nearest(PickupPoint(), settings.groundReach, this, true) ?? GroundBag.Nearest(PickupPoint(), settings.groundReach);
            if (!held || Holding || Opening || (!down && !near)) { _bagAction = -1f; return; }
            if (_bagAction < 0f) _bagAction = 0f;
            _bagAction += dt / Mathf.Max(down ? settings.bagDownTime : settings.bagUpTime, 0.1f);
            if (_bagAction < 1f) return;
            _bagAction = -1f;
            if (down) SetDown(); else Shoulder(near);
        }

        /// <summary>Sets the bag on the back down on the ground in front, mouth open, everything still in it.</summary>
        public void SetDown()
        {
            if (!CanSetDown) return;
            var t = controller ? controller.transform : transform;
            GroundBag.Create(settings, Worn, Durability, _items, t.position + t.forward * 0.45f, Quaternion.LookRotation(-t.forward), this, 0f);
            string name = Def.displayName;
            _items.Clear();
            Worn = BagKind.None;
            Durability = 0f;
            Recount();
            Did?.Invoke("down", ItemKind.RockSalt);
            Say($"{name}を地面に下ろした（C 長押しで背負う）");
        }

        /// <summary>Shoulders a bag lying on the ground (one's own or anyone's), with everything in it.</summary>
        public void Shoulder(GroundBag g)
        {
            if (HasBag || !g) return;
            Worn = g.kind;
            Durability = g.durability;
            _items.Clear();
            _items.AddRange(g.items);
            Destroy(g.gameObject);
            Recount();
            Did?.Invoke("up", ItemKind.RockSalt);
            Say($"{Def.displayName}を背負った");
        }

        /// <summary>A fallen slug lets go of everything: what was in the hands falls, the bag drops where it fell (for a while).</summary>
        void OnDied(string cause)
        {
            if (Holding)
            {
                ItemPickup.Spawn(settings, HeldKind, heldWorld, Vector3.up, OwnCollider);
                EndCarry();
            }
            if (!HasBag) return;
            var t = controller ? controller.transform : transform;
            GroundBag.Create(settings, Worn, Durability, _items, t.position - t.forward * 0.25f, t.rotation, null, settings.droppedLifetime);
            _items.Clear();
            Worn = BagKind.None;
            Durability = 0f;
            Recount();
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
            Did?.Invoke("burst", n > 0 ? _items[0] : ItemKind.RockSalt);
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
            if (peeking && _peek <= 0f) Did?.Invoke("peek", ItemKind.RockSalt);
            _peek = Mathf.MoveTowards(_peek, peeking ? 1f : 0f, dt / 0.22f);
            if (cameraRig)
            {
                cameraRig.peek = _peek;
                // the eyes look at the bag's mouth as the own view last drew it (grown and tipped up toward them)
                cameraRig.peekTarget = _ownMouthValid ? Vector3.Lerp(BagCenterWorld(), _ownMouth, Ease(_peek)) : BagCenterWorld();
                cameraRig.handsBusy = Opening;
            }

            StepCarry(dt);
            if (locked && kb != null)
            {
                // G: empty hands take the top thing out of the bag; with a thing in the hands, tap to drop it, hold and
                // let go to throw it
                if (kb.gKey.wasPressedThisFrame)
                {
                    if (!Holding) TakeOut();
                    else if (Carry == CarryPhase.Hold) _gHeld = 0f;
                }
                if (_gHeld >= 0f)
                {
                    _gHeld += dt;
                    if (!kb.gKey.isPressed)
                    {
                        DropHeld(_gHeld < 0.2f ? 0f : Mathf.Clamp01((_gHeld - 0.2f) / 0.5f));
                        _gHeld = -1f;
                    }
                }
                // E: empty hands take what lies in front; with a thing in the hands, put it in the bag
                if (kb.eKey.wasPressedThisFrame)
                {
                    if (Carry == CarryPhase.Hold) Stow();
                    else if (!Holding)
                    {
                        var p = ItemPickup.Nearest(PickupPoint(), settings.pickupReach);
                        if (p) Grab(p);
                        else
                        {
                            var plant = Pickable.Nearest(PickupPoint(), settings.pickupReach);
                            if (plant) Pick(plant);
                        }
                    }
                }
                StepBagAction(kb.cKey.isPressed, dt);
                // X: with a thing in the hands, push it into the body; with empty hands, pull the hidden thing out
                if (kb.xKey.wasPressedThisFrame)
                {
                    if (Carry == CarryPhase.Hold) HideHeld();
                    else if (!Holding && HasHidden) TakeOutHidden();
                }
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
                    Did?.Invoke("eco", ItemKind.RockSalt);
                    Recount();
                    Say("エコバッグを広げた");
                }
            }
            else _ecoOpening = -1f;

            float speed = settings.SpeedFor(Weight);
            if (_peek > 0f) speed *= Mathf.Lerp(1f, settings.peekSpeed, _peek);
            if (Opening || BagAction) speed *= 0.3f;
            controller.speedMultiplier = speed;
        }

        Vector3 PickupPoint()
        {
            var t = controller.transform;
            return t.position + t.forward * 0.3f + Vector3.up * 0.1f;
        }

        public Vector3 BagCenterWorld()
        {
            if (slug && gear && gear.HasBackItem) return slug.FollowPointWorld(gear.BackItemCenter, gear.BackItemT, gear.BackItemNormal);
            return transform.position + Vector3.up * 0.3f - transform.forward * 0.2f;
        }

        /// <summary>The mouth of the bag on the back (world): where things go in and come out.</summary>
        public Vector3 BagMouthWorld() => GroundTransfer ? _ground.MouthWorld : BagPointWorld(new Vector3(0f, 1f, 0.2f));

        /// <summary>A point of the bag (world), as fractions of its half size along its own axes (x across, y up, z out).</summary>
        Vector3 BagPointWorld(Vector3 frac)
        {
            if (!(slug && gear && gear.HasBackItem)) return BagCenterWorld() + Vector3.up * 0.15f;
            Vector3 local = gear.BackItemCenter + gear.BackItemRotation * Vector3.Scale(frac, gear.BackItemHalf);
            return slug.FollowPointWorld(local, gear.BackItemT, gear.BackItemNormal);
        }

        // ------------------------------------------------------------------ peeking: the bag on the back, opened
        /// <summary>Crammed: three things show, spilling over (a bag with a limit nearly at it, or past the weight threshold).</summary>
        bool Crammed => HasBag && (Def.capacity > 0f ? Volume >= Def.capacity * 0.8f : Weight > settings.weightThreshold);

        void LateUpdate()
        {
            if (!IsPlayer) return;
            // everyone sees the head turn round to look at its back (and the lid flip open: OnBeginCamera)
            controller.twistDegrees = settings.peekTwist * Ease(_peek);

            // the top two things (three when crammed), placed in the mouth for the own camera (OnBeginCamera)
            int n = _peek > 0.05f && HasBag ? (Crammed ? 3 : 2) : 0;
            for (int i = 0; i < _shown.Length; i++)
            {
                int index = _items.Count - 1 - i;              // 0 = the top one
                bool on = i < n && index >= 0;
                if (on && (_shown[i] == null || _shownKind[i] != _items[index]))
                {
                    if (_shown[i]) Destroy(_shown[i]);
                    _shown[i] = ItemPickup.Visual(settings, _items[index], null, false);
                    _shown[i].name = "Peek_" + _items[index];
                    _shownKind[i] = _items[index];
                    _shownRenderers[i] = _shown[i].GetComponentsInChildren<Renderer>(true);
                }
                if (_shown[i] && _shown[i].activeSelf != on) _shown[i].SetActive(on);
            }
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

            // ---- top left, always (beside the slug itself when it is shown there): the bag's silhouette swelling with
            //      its bulk, coloured by its weight; no numbers
            bool portrait = _portrait && _portrait.Shown;
            var guiMatrix = GUI.matrix;
            if (portrait) GUI.matrix = guiMatrix * Matrix4x4.Translate(new Vector3(_portrait.Area.xMax - 2f, 0f, 0f));
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
            GUI.matrix = guiMatrix;

            // ---- peeking: what is on top, and the numbers
            if (_peek > 0.6f)
            {
                // under the bag's shape (and the slug itself), clear of the back in view
                var r = new Rect(10, portrait ? Mathf.Max(128f, _portrait.Area.yMax + 10f) : 128f, 400, 150);
                GUI.Box(r, GUIContent.none);
                GUI.Box(r, GUIContent.none);
                string top = Top.HasValue ? settings.Def(Top.Value).displayName : "（空）";
                if (_items.Count >= 2) top += $"<size=16>　その下：{settings.Def(_items[_items.Count - 2]).displayName}</size>";
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
            if (Carry == CarryPhase.Hold)
            {
                var gIn = HasBag ? null : GroundBag.Nearest(PickupPoint(), settings.groundReach);
                string into = HasBag ? "E バッグに入れる" : gIn ? $"E 地面の{gIn.Def.displayName}に入れる" : (_ecoUsed ? "（バッグがない）" : "（バッグがない：B 長押しでエコバッグ）");
                string hide = settings.Def(HeldKind).canHide && !HasHidden ? "　X 体に突っ込む" : "";
                GUI.Label(new Rect(0, y, w, 26), $"手に{settings.Def(HeldKind).displayName}：{into}　G 落とす（長押しで投げる）{hide}", _small);
                y += 26f;
            }
            else if (!Holding && _peek < 0.99f)
            {
                var p = ItemPickup.Nearest(PickupPoint(), settings.pickupReach);
                if (p) { GUI.Label(new Rect(0, y, w, 26), $"E  つかむ：{settings.Def(p.kind).displayName}", _small); y += 26f; }
                else
                {
                    var plant = Pickable.Nearest(PickupPoint(), settings.pickupReach);
                    if (plant) { GUI.Label(new Rect(0, y, w, 26), $"E  摘む：{settings.Def(plant.kind).displayName}", _small); y += 26f; }
                }
                if (HasHidden) { GUI.Label(new Rect(0, y, w, 26), $"X  体から取り出す：{settings.Def(Hidden).displayName}", _small); y += 26f; }
                var gOut = GroundBag.Nearest(PickupPoint(), settings.groundReach);
                if (gOut && !gOut.Empty) { GUI.Label(new Rect(0, y, w, 26), $"G  地面の{gOut.Def.displayName}から出す（{gOut.items.Count}個）", _small); y += 26f; }
                if (CanSetDown) { GUI.Label(new Rect(0, y, w, 26), "C 長押し  バッグを地面に下ろす", _small); y += 26f; }
                else if (!HasBag && gOut) { GUI.Label(new Rect(0, y, w, 26), $"C 長押し  {gOut.Def.displayName}を背負う", _small); y += 26f; }
            }
            if (BagAction)
            {
                GUI.Box(new Rect(w * 0.5f - 100, y + 4, 200, 10), GUIContent.none);
                GUI.color = new Color(0.8f, 0.9f, 1f);
                GUI.DrawTexture(new Rect(w * 0.5f - 100, y + 4, 200 * _bagAction, 10), Texture2D.whiteTexture);
                GUI.color = old;
                y += 18f;
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
