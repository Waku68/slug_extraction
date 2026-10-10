using System.Collections.Generic;
using SlugDesign;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// A slug that plays by itself (docs/discussions/who_what_scope_discussion.md: first a bot, then two people). It drives
    /// the same body as the player (movement, eyes, pickaxe, guns, loadout, all with <c>driven</c> set), so it crawls,
    /// climbs, swings and shoots exactly as a person would. It mines rock salt; when it sees the player (or hears a shot,
    /// a pickaxe, a crawl close by) it turns, gets ready over a moment, then shoots in bursts from where it stands,
    /// ducks behind cover when hurt, and goes to look where the player was last seen. Later the snail NPCs grow out of it.
    /// </summary>
    public class SlugBot : MonoBehaviour
    {
        public enum Mode { Mine, Look, Fight, Cover, Search }

        [Header("体")]
        public SlugMoveController controller;
        public SlugCameraRig cameraRig;
        public SlugToolHolder pickaxe;
        public SlugLoadout loadout;
        public SlugBag bag;
        public SlugHealth health;
        public SlugPop slug;

        [Header("気づく")]
        [Tooltip("見える距離（m）。晴れの昼の目安。雨や夜で短くする予定")] public float sightRange = 80f;
        [Tooltip("前に見える角度（度）。この外は振り向かないと見えない")] public float sightAngle = 150f;
        [Tooltip("この距離より近ければ、後ろでも気づく（m）")] public float feelRange = 3f;
        [Tooltip("見えてから撃ち始めるまで（秒）。近いとき〜遠いとき")] public Vector2 reactTime = new Vector2(0.5f, 1.8f);
        [Tooltip("銃声が聞こえる距離（m）")] public float hearShot = 150f;
        [Tooltip("ピッケルの音が聞こえる距離（m）")] public float hearPickaxe = 35f;
        [Tooltip("這う音が聞こえる距離（m）")] public float hearCrawl = 5f;
        [Tooltip("見失ってから探すのをやめるまで（秒）")] public float giveUpTime = 25f;

        [Header("撃つ")]
        [Tooltip("狙いが合うまでのずれ（度）。見つけた直後のずれ。遠いほど大きい")] public float aimError = 4f;
        [Tooltip("ずれが半分になるまで（秒）")] public float aimSettle = 0.8f;
        [Tooltip("首を回す速さ（度/秒）")] public float turnSpeed = 220f;
        [Tooltip("このずれ（度）より小さければ撃つ")] public float fireCone = 2.5f;
        [Tooltip("一度に撃つ数")] public Vector2Int burst = new Vector2Int(3, 6);
        [Tooltip("撃つ合間（秒）")] public Vector2 burstPause = new Vector2(0.35f, 0.8f);

        [Header("隠れる")]
        [Tooltip("撃たれてから物陰へ入るまでの、体力の減り（割合）")] public float coverWhenHurt = 0.15f;
        [Tooltip("物陰で待つ時間（秒）")] public Vector2 coverWait = new Vector2(2f, 4f);
        [Tooltip("物陰を探す距離（m）")] public float coverSearch = 12f;

        [Header("掘る")]
        [Tooltip("岩塩を探す距離（m）")] public float mineRange = 120f;
        [Tooltip("歩き回る場所の中心と半径（m）。0 = 生まれた場所")] public Vector3 homeCenter;
        public float homeRadius = 80f;

        [Header("テスト")]
        [Tooltip("始まるたびに、山の岩塩のどれかのそばへ移る（居場所が分からないように）")] public bool randomStart = true;
        [Tooltip("プレイヤーを、ボットから見えない 60〜100 m 先から始めさせる（たどり着くまでを短く）")] public bool startPlayerNear = true;
        [Tooltip("頭の上に今していることを出す（調整用。F5 で切り替え）")] public bool showState;

        public Mode State { get; private set; } = Mode.Mine;
        /// <summary>0 = nothing noticed .. 1 = sure, shooting.</summary>
        public float Alert { get; private set; }

        Transform _player;
        SlugPop _playerSlug;
        SlugMoveController _playerMove;
        Vector3 _lastKnown, _lastSeenFrom;
        float _lastSeenTime = -999f, _err, _errYaw, _errPitch;
        float _yaw, _pitch;
        bool _sees;
        float _nextLook;
        int _shotsLeft;
        float _pauseUntil, _coverUntil, _hurtFrom, _lastHealth;
        Vector3? _goal;
        float _stuckFor, _sideUntil, _sideSign = 1f;
        Harvestable _mining;
        readonly List<Object> _heard = new List<Object>();

        SlugGunHolder Gun => loadout ? loadout.BackGun : null;
        Vector3 Eye => slug ? slug.FaceCenterWorld : transform.position + Vector3.up * 0.45f;

        void Awake()
        {
            controller.driven = true;
            cameraRig.driven = true;
            if (pickaxe) pickaxe.driven = true;
            if (loadout)
            {
                loadout.driven = true;
                loadout.showHud = false;
                foreach (var g in loadout.guns) if (g) { g.driven = true; g.showHud = false; }
            }
            if (bag) bag.showHud = false;
            if (homeCenter == Vector3.zero) homeCenter = transform.position;
        }

        void Start()
        {
            _lastHealth = health ? health.Current : 0f;
            foreach (var b in FindObjectsByType<SlugBag>())
            {
                if (!b.IsPlayer) continue;
                _player = b.controller.transform;
                _playerMove = b.controller;
                _playerSlug = b.slug;
                foreach (var g in _player.GetComponents<SlugGunHolder>()) { g.Fired += OnPlayerShot; _heard.Add(g); }
                var t = _player.GetComponent<SlugToolHolder>();
                if (t) { t.Struck += OnPlayerPickaxe; _heard.Add(t); }
            }
            // the first bot to start spreads them all out, then sets the player down out of everyone's sight
            if (Time.frameCount != _spreadFrame)
            {
                _spreadFrame = Time.frameCount;
                Random.InitState(System.Environment.TickCount);   // different spots every play
                var bots = FindObjectsByType<SlugBot>();
                var taken = new List<Vector3>();
                foreach (var b in bots)
                    if (b.randomStart) { var at = b.RandomSaltSpot(taken); taken.Add(at); b.MoveTo(at); }
                if (startPlayerNear && _player) PlacePlayer(bots);
            }
            _yaw = cameraRig.Yaw;
        }

        static int _spreadFrame = -1;

        /// <summary>Beside one of the mountain's rock salt, at least 30 m from the spots already <paramref name="taken"/>.</summary>
        Vector3 RandomSaltSpot(List<Vector3> taken)
        {
            var spots = new List<Harvestable>();
            foreach (var h in FindObjectsByType<Harvestable>())
            {
                if (h.kind != ItemKind.RockSalt || Flat(h.transform.position - homeCenter) >= homeRadius) continue;
                bool near = false;
                foreach (var t in taken) if (Flat(h.transform.position - t) < 30f) near = true;
                if (!near) spots.Add(h);
            }
            if (spots.Count == 0) return transform.position;
            var p = spots[Random.Range(0, spots.Count)].transform.position;
            Vector2 o = Random.insideUnitCircle.normalized * 2f;
            return p + new Vector3(o.x, 0.3f, o.y);
        }

        static void MoveTo(Transform t, Vector3 at)
        {
            var cc = t.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            if (Physics.Raycast(at + Vector3.up * 5f, Vector3.down, out var hit, 20f, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore))
                at = hit.point + Vector3.up * 0.05f;
            t.position = at;
            if (cc) cc.enabled = true;
        }

        void MoveTo(Vector3 at)
        {
            MoveTo(transform, at);
            float yaw = Random.Range(0f, 360f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraRig.SetLook(yaw, 5f);
        }

        /// <summary>The player starts 60–100 m off, on ground about as high, where this slug cannot see it, facing the mountain.</summary>
        void PlacePlayer(SlugBot[] bots)
        {
            for (int i = 0; i < 80; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(60f, 100f);
                Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                if (!Physics.Raycast(p + Vector3.up * 200f, Vector3.down, out var hit, 400f, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (hit.normal.y < 0.8f || Mathf.Abs(hit.point.y - transform.position.y) > 15f || hit.point.y < 1f) continue;
                bool seen = false;
                foreach (var b in bots)
                    if (Flat(b.transform.position - hit.point) < 60f || Clear(b.Eye, hit.point + Vector3.up * 0.45f)) seen = true;
                if (seen) continue;
                MoveTo(_player, hit.point);
                var rig = _player.GetComponent<SlugCameraRig>();
                Vector3 d = homeCenter - hit.point;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                _player.rotation = Quaternion.Euler(0f, yaw, 0f);     // the rig takes its yaw from here when it starts
                if (rig) rig.SetLook(yaw, 0f);
                return;
            }
        }

        void OnDestroy()
        {
            foreach (var o in _heard)
            {
                if (o is SlugGunHolder g && g) g.Fired -= OnPlayerShot;
                if (o is SlugToolHolder t && t) t.Struck -= OnPlayerPickaxe;
            }
        }

        // ------------------------------------------------------------------ hearing
        void OnPlayerShot(Vector3 eye, Vector3 dir) => Hear(eye, hearShot, 0.7f);
        void OnPlayerPickaxe(RaycastHit hit, IPickaxeTarget target) => Hear(hit.point, hearPickaxe, 0.4f);

        void Hear(Vector3 at, float range, float alert)
        {
            if (!this || (health && health.Dead)) return;
            float d = Vector3.Distance(at, transform.position);
            if (d > range) return;
            // where it came from, roughly (further = vaguer)
            Vector2 off = Random.insideUnitCircle * d * 0.12f;
            _lastKnown = at + new Vector3(off.x, 0f, off.y);
            _lastSeenTime = Mathf.Max(_lastSeenTime, Time.time - 2f);
            Alert = Mathf.Max(Alert, alert);
            if (State == Mode.Mine) Enter(Mode.Look);
        }

        // ------------------------------------------------------------------ the loop
        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || (health && health.Dead) || !_player) { Drive(dt, Vector2.zero); return; }

            Look(dt);
            CheckHurt();

            Vector2 move = Vector2.zero;
            bool trigger = false;
            switch (State)
            {
                case Mode.Mine: move = Mine(dt); break;
                case Mode.Look: move = LookAround(dt); break;
                case Mode.Fight: move = Fight(dt, out trigger); break;
                case Mode.Cover: move = Cover(dt, out trigger); break;
                case Mode.Search: move = Search(dt); break;
            }

            // eyes: toward the wanted direction, no faster than a neck turns (recoil climbs, and comes back by this)
            _yaw = Mathf.MoveTowardsAngle(cameraRig.Yaw, _wantYaw, turnSpeed * dt);
            _pitch = Mathf.MoveTowards(cameraRig.Pitch, _wantPitch, turnSpeed * 0.6f * dt);
            cameraRig.SetLook(_yaw, _pitch);

            var gun = Gun;
            if (gun && gun.enabled)
            {
                bool reload = gun.Handling.Ammo == 0 && !gun.Handling.Reloading;
                gun.Simulate(dt, trigger, reload);
            }
            Drive(dt, move);
        }

        float _wantYaw, _wantPitch;

        void Face(Vector3 point)
        {
            Vector3 d = point - Eye;
            _wantYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            _wantPitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
        }

        void FaceAlong(Vector3 dir)
        {
            if (new Vector2(dir.x, dir.z).sqrMagnitude < 1e-4f) return;
            _wantYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            _wantPitch = 8f;
        }

        void Enter(Mode m)
        {
            if (State == m) return;
            State = m;
            _goal = null;
            if (pickaxe) pickaxe.swingHeld = false;
            if (m == Mode.Fight || m == Mode.Cover) { if (loadout.Current != SlugLoadout.BackSlot) loadout.Select(SlugLoadout.BackSlot); }
            if (m == Mode.Fight) { _err = aimError; RollError(); _shotsLeft = 0; _pauseUntil = Time.time + 0.2f; }
        }

        // ------------------------------------------------------------------ seeing
        void Look(float dt)
        {
            if (Time.time < _nextLook) return;
            float step = 0.1f;
            _nextLook = Time.time + step;
            _sees = CanSee(out Vector3 seen);
            if (_sees)
            {
                _lastKnown = seen;
                _lastSeenTime = Time.time;
                _lastSeenFrom = transform.position;
                float d = Vector3.Distance(seen, Eye);
                float react = Mathf.Lerp(reactTime.x, reactTime.y, Mathf.Clamp01(d / sightRange));
                // a slug lying still is harder to make out
                bool still = _playerMove && _playerMove.Speed < 0.15f;
                Alert = Mathf.Min(1f, Alert + step / react * (still ? 0.5f : 1f));
                if (Alert >= 1f && State != Mode.Cover) Enter(Mode.Fight);
                else if (State == Mode.Mine && Alert > 0.25f) Enter(Mode.Look);
            }
            else
            {
                // a crawl close by is heard
                if (_playerMove && _playerMove.Speed > 0.3f && Vector3.Distance(_player.position, transform.position) < hearCrawl)
                    Hear(_player.position, hearCrawl, 0.5f);
                if (State == Mode.Fight && Time.time - _lastSeenTime > 1.5f) Enter(Mode.Search);
                if (State == Mode.Mine || State == Mode.Look) Alert = Mathf.Max(0f, Alert - step * 0.05f);
            }
        }

        bool CanSee(out Vector3 seen)
        {
            seen = default;
            if (!_player) return false;
            Vector3 eye = Eye;
            Vector3 head = _playerSlug ? _playerSlug.FaceCenterWorld : _player.position + Vector3.up * 0.45f;
            Vector3 body = _player.position + Vector3.up * 0.25f;
            float d = Vector3.Distance(eye, head);
            if (d > sightRange) return false;
            Vector3 fwd = Quaternion.Euler(0f, cameraRig.Yaw, 0f) * Vector3.forward;
            Vector3 to = head - eye;
            if (d > feelRange && Vector3.Angle(fwd, new Vector3(to.x, 0f, to.z)) > sightAngle * 0.5f) return false;
            if (Clear(eye, head)) { seen = head; return true; }
            if (Clear(eye, body)) { seen = body; return true; }
            return false;
        }

        /// <summary>Nothing of the world between (the player's own colliders and this slug's don't count).</summary>
        bool Clear(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            foreach (var h in Physics.RaycastAll(from, d / len, len, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (_player && h.collider.transform.IsChildOf(_player)) continue;
                if (h.collider.GetComponentInParent<ItemPickup>()) continue;
                return false;
            }
            return true;
        }

        void CheckHurt()
        {
            if (!health) return;
            float now = health.Current;
            if (now < _lastHealth - 0.01f)
            {
                if (_hurtFrom <= 0f) _hurtFrom = _lastHealth;
                // shot from somewhere: it knows roughly where the player is
                if (State == Mode.Mine || State == Mode.Look || State == Mode.Search)
                {
                    _lastKnown = _player.position;
                    _lastSeenTime = Time.time;
                    Alert = 1f;
                    Enter(Mode.Fight);
                }
                if (State == Mode.Fight && (_hurtFrom - now) / health.Max >= coverWhenHurt && FindCover(out var c))
                {
                    _goal = c;
                    State = Mode.Cover;
                    _coverUntil = -1f;
                }
            }
            _lastHealth = now;
        }

        // ------------------------------------------------------------------ what it does
        Vector2 Mine(float dt)
        {
            if (!_mining || _mining.Done >= _mining.hits || !_mining.isActiveAndEnabled) { _mining = NearestSalt(); }
            if (bag && !bag.Fits(ItemKind.RockSalt)) _mining = null;
            Collect();
            if (!_mining)
            {
                pickaxe.swingHeld = false;
                if (_goal == null || Flat(transform.position - _goal.Value) < 2f) _goal = RandomHome();
                return Steer(_goal.Value, true);
            }
            if (loadout.Current != SlugLoadout.PickaxeSlot) loadout.Select(SlugLoadout.PickaxeSlot);
            Vector3 at = _mining.transform.position;
            float dist = Flat(transform.position - at);
            if (dist > 0.95f)
            {
                pickaxe.swingHeld = false;
                return Steer(at, true);
            }
            Face(at + Vector3.up * 0.2f);
            pickaxe.swingHeld = Mathf.Abs(Mathf.DeltaAngle(cameraRig.Yaw, _wantYaw)) < 15f;
            return Vector2.zero;
        }

        Vector2 LookAround(float dt)
        {
            pickaxe.swingHeld = false;
            Face(_lastKnown);
            if (Alert <= 0.05f) Enter(Mode.Mine);
            if (Time.time - _lastSeenTime > 6f) Enter(Mode.Search);
            return Vector2.zero;
        }

        Vector2 Fight(float dt, out bool trigger)
        {
            trigger = false;
            var gun = Gun;
            Vector3 target = _sees ? Lead(_lastKnown, gun) : _lastKnown;
            // the aim wanders off and settles while it keeps looking
            if (_sees) _err *= Mathf.Pow(0.5f, dt / Mathf.Max(aimSettle, 0.05f));
            Face(target);
            _wantYaw += _errYaw * _err;
            _wantPitch += _errPitch * _err;

            float off = Mathf.Abs(Mathf.DeltaAngle(cameraRig.Yaw, _wantYaw)) + Mathf.Abs(cameraRig.Pitch - _wantPitch);
            if (_sees && gun && gun.Ready && Time.time >= _pauseUntil && off < fireCone + _err)
            {
                if (_shotsLeft <= 0) _shotsLeft = Random.Range(burst.x, burst.y + 1);
                trigger = true;
                if (gun.Handling.SinceShot < dt) _shotsLeft--;
                if (_shotsLeft <= 0)
                {
                    _pauseUntil = Time.time + Random.Range(burstPause.x, burstPause.y);
                    RollError();
                }
            }
            if (!_sees) return Steer(_lastKnown, false);     // step out to where it was seen
            return Vector2.zero;
        }

        Vector2 Cover(float dt, out bool trigger)
        {
            trigger = false;
            Face(_lastKnown);
            if (_goal == null) { Enter(Mode.Fight); return Vector2.zero; }
            if (Flat(transform.position - _goal.Value) > 0.6f && _coverUntil < 0f)
                return Steer(_goal.Value, false);
            if (_coverUntil < 0f) _coverUntil = Time.time + Random.Range(coverWait.x, coverWait.y);
            if (Time.time > _coverUntil)
            {
                _hurtFrom = 0f;
                _goal = null;
                State = Mode.Search;          // back out to where it last saw the player from
                _searchFrom = _lastSeenFrom;
            }
            return Vector2.zero;
        }

        Vector3? _searchFrom;

        Vector2 Search(float dt)
        {
            if (Time.time - _lastSeenTime > giveUpTime) { Alert = 0.2f; Enter(Mode.Mine); return Vector2.zero; }
            if (loadout.Current != SlugLoadout.BackSlot) loadout.Select(SlugLoadout.BackSlot);
            Vector3 goal = _searchFrom ?? _lastKnown;
            if (Flat(transform.position - goal) < 1.2f)
            {
                if (_searchFrom != null) { _searchFrom = null; return Vector2.zero; }
                // there: turn slowly to look about
                _wantYaw += 50f * dt;
                _wantPitch = 5f;
                return Vector2.zero;
            }
            var m = Steer(goal, false);
            Face(_lastKnown);
            return m;
        }

        // ------------------------------------------------------------------ helpers
        /// <summary>Where to aim so a round meets a slug moving like this: ahead of it, and up by the drop.</summary>
        Vector3 Lead(Vector3 at, SlugGunHolder gun)
        {
            if (!gun) return at;
            float speed = Mathf.Max(gun.gun.muzzleSpeed, 1f);
            float t = Vector3.Distance(Eye, at) / speed;
            Vector3 vel = _playerMove ? _playerMove.GetComponent<CharacterController>().velocity : Vector3.zero;
            float drop = 0.5f * -Physics.gravity.y * gun.gun.gravityScale * t * t;
            return at + vel * t + Vector3.up * drop;
        }

        void RollError()
        {
            float d = _player ? Vector3.Distance(_player.position, transform.position) : 20f;
            float k = 1f + d / 40f;
            Vector2 r = Random.insideUnitCircle.normalized * k;
            _errYaw = r.x;
            _errPitch = r.y;
            _err = Mathf.Max(_err, aimError * 0.35f);
        }

        void Collect()
        {
            if (!bag) return;
            for (int i = ItemPickup.All.Count - 1; i >= 0; i--)
            {
                var p = ItemPickup.All[i];
                if (!p || Flat(p.transform.position - transform.position) > 2.5f) continue;
                if (bag.TryAdd(p.kind, 1) == 1) Destroy(p.gameObject);
            }
        }

        Harvestable NearestSalt()
        {
            Harvestable best = null;
            float bestD = mineRange;
            foreach (var h in FindObjectsByType<Harvestable>())
            {
                if (h.kind != ItemKind.RockSalt || h.Done >= h.hits) continue;
                float d = Vector3.Distance(h.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
        }

        Vector3 RandomHome()
        {
            for (int i = 0; i < 20; i++)
            {
                Vector2 r = Random.insideUnitCircle * homeRadius;
                Vector3 p = homeCenter + new Vector3(r.x, 0f, r.y);
                if (Physics.Raycast(p + Vector3.up * 300f, Vector3.down, out var hit, 600f, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore)
                    && hit.normal.y > 0.8f)
                    return hit.point;
            }
            return homeCenter;
        }

        /// <summary>A spot near by, on ground it can stand on, where the player's eyes cannot see it.</summary>
        bool FindCover(out Vector3 spot)
        {
            spot = default;
            Vector3 threat = (_playerSlug ? _playerSlug.FaceCenterWorld : _lastKnown);
            float best = float.MaxValue;
            for (int ring = 1; ring <= 4; ring++)
            {
                float r = coverSearch * ring / 4f;
                for (int i = 0; i < 16; i++)
                {
                    float a = i * Mathf.PI / 8f;
                    Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                    if (!Physics.Raycast(p + Vector3.up * 6f, Vector3.down, out var hit, 14f, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.normal.y < 0.7f || hit.collider.transform.IsChildOf(transform)) continue;
                    Vector3 eyeThere = hit.point + Vector3.up * 0.45f;
                    if (Clear(eyeThere, threat)) continue;                     // still seen there
                    Vector3 from = transform.position + Vector3.up * 0.3f, to = hit.point + Vector3.up * 0.3f;
                    if (Physics.SphereCast(from, 0.15f, (to - from).normalized, out _, (to - from).magnitude, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    float cost = r + Vector3.Distance(hit.point, threat) * -0.05f;
                    if (cost < best) { best = cost; spot = hit.point; }
                }
                if (best < float.MaxValue) return true;
            }
            return false;
        }

        /// <summary>The stick input that crawls toward <paramref name="goal"/>, side-stepping when stuck.</summary>
        Vector2 Steer(Vector3 goal, bool faceIt)
        {
            Vector3 d = goal - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.04f) return Vector2.zero;
            Vector3 dir = d.normalized;
            if (Time.time < _sideUntil) dir = (Quaternion.Euler(0f, 75f * _sideSign, 0f) * dir).normalized;
            if (faceIt) FaceAlong(dir);
            // stuck: crawling and not getting anywhere (a step, a rock, too steep)
            if (controller.Speed < 0.12f || controller.TooSteep) _stuckFor += Time.deltaTime; else _stuckFor = 0f;
            if (_stuckFor > 1.2f)
            {
                _stuckFor = 0f;
                _sideSign = Random.value < 0.5f ? -1f : 1f;
                _sideUntil = Time.time + Random.Range(0.8f, 1.6f);
                if (State == Mode.Mine) { _mining = null; _goal = null; }
            }
            Vector3 local = Quaternion.Euler(0f, -cameraRig.Yaw, 0f) * dir;
            return new Vector2(local.x, local.z);
        }

        void Drive(float dt, Vector2 move) => controller.Simulate(dt, Vector2.ClampMagnitude(move, 1f), false);

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        void LateUpdate()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f5Key.wasPressedThisFrame) showState = !showState;
        }

        void OnGUI()
        {
            if (!showState || !Camera.main) return;
            Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.1f);
            if (sp.z <= 0f || sp.z > 60f) return;
            string label = State switch
            {
                Mode.Mine => "掘る", Mode.Look => "気配", Mode.Fight => "撃つ", Mode.Cover => "隠れる", _ => "探す",
            };
            GUI.Label(new Rect(sp.x - 40f, Screen.height - sp.y - 20f, 120f, 22f), $"{label} {Alert:0.0}");
        }
    }
}
