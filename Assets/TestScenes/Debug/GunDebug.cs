using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>
    /// Tools for tuning the guns: slow motion (T), a readout of the gun in hand (how it is held, the action and the
    /// reload, the spread, the burst), the last hit (what, how far, which ring) and a running score on the targets
    /// (C clears it). A shotgun blast counts each grain. Knife stabs show what they struck.
    /// </summary>
    public class GunDebug : MonoBehaviour
    {
        public SlugLoadout loadout;

        static readonly float[] Speeds = { 1f, 0.35f, 0.1f };
        int _speed;
        int _shots, _onTarget, _scoreSum;
        string _last = "まだ当てていない";
        GUIStyle _style;

        void Start()
        {
            foreach (var g in loadout.guns)
            {
                g.Fired += OnFired;
                g.RoundHit += OnHit;
            }
            ShootingTarget.Scored += OnScored;
            SlugHitboxes.AnyHit += OnBodyHit;
            if (loadout.knife) loadout.knife.Struck += OnStab;
        }

        void OnStab(RaycastHit hit, IShotTarget target)
        {
            if (target is SlugHitbox) _last = "ナイフ：" + _last;     // the part is already written
            else if (target != null) _last = $"ナイフ：{(hit.collider.GetComponentInParent<ShootingTarget>() ? "的" : hit.collider.name)}";
            else _last = $"ナイフ：{hit.collider.name}";
        }

        void OnBodyHit(SlugHitbox box, RaycastHit hit)
        {
            string cover = box.Armoured ? $"（{box.armour}で守られている）" : box.part == "触角" ? "（弱点！）" : "（むき出し）";
            _last = $"{(box.owner ? box.owner.name : "")}の<b>{box.part}</b>{cover}　×{box.multiplier:0.##}";
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (loadout)
                foreach (var g in loadout.guns)
                    if (g) { g.Fired -= OnFired; g.RoundHit -= OnHit; }
            ShootingTarget.Scored -= OnScored;
            SlugHitboxes.AnyHit -= OnBodyHit;
            if (loadout && loadout.knife) loadout.knife.Struck -= OnStab;
        }

        void OnFired(Vector3 eye, Vector3 dir) => _shots++;

        void OnHit(RaycastHit hit)
        {
            var gun = loadout.CurrentGun;
            float d = gun ? Vector3.Distance(hit.point, gun.cameraRig.cam.transform.position) : 0f;
            if (hit.collider.GetComponent<SlugHitbox>()) { _last += $"　{d:0.0} m"; return; }   // the part is already written
            string what = hit.collider.GetComponentInParent<ShootingTarget>() ? "的" : hit.collider.name;
            _last = $"{what}　{d:0.0} m";
        }

        void OnScored(ShootingTarget t, int score)
        {
            _onTarget++;
            _scoreSum += score;
            _last += $"　<b>{score} 点</b>";
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tKey.wasPressedThisFrame)
            {
                _speed = (_speed + 1) % Speeds.Length;
                Time.timeScale = Speeds[_speed];
            }
            if (kb.cKey.wasPressedThisFrame) { _shots = _onTarget = _scoreSum = 0; _last = "まだ当てていない"; }
        }

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 17, richText = true };
                _style.normal.textColor = Color.white;
            }
            var gun = loadout.CurrentGun;
            string line1, line2;
            if (gun == null || !gun.Ready)
            {
                line1 = loadout.DrawProgress < 1f ? $"構え  <b>{loadout.SlotName(loadout.Current)}を取り出している</b>"
                      : $"構え  <b>{loadout.SlotName(loadout.Current)}を持っている</b>";
                line2 = "";
            }
            else
            {
                var h = gun.Handling;
                string state = h.Equip < 1f ? "持ち替え中"
                    : h.Reloading ? ReloadText(h)
                    : h.Cycling ? (gun.gun.action == GunAction.Pump ? "ポンプを引く" : "ボルトを引く")
                    : h.Scope > 0.95f ? "覗いている"
                    : h.Raise > 0.95f ? "肩付け"
                    : h.Raise > 0.05f ? "持ち上げ中" : "胸の前で抱える";
                line1 = $"{gun.gun.displayName}  <b>{state}</b>";
                line2 = $"弾  {h.Ammo} / {gun.gun.magazineSize}（{gun.gun.ammoName}）　　ばらつき {gun.CurrentSpread:0.0}°　　{h.ShotsInBurst} 発目";
            }
            float avg = _onTarget > 0 ? _scoreSum / (float)_onTarget : 0f;
            string text =
                $"{line1}　　時間 ×{Speeds[_speed]:0.##}\n" +
                $"{line2}\n" +
                $"最後の当たり  {_last}\n" +
                $"的に当たった粒  {_onTarget} / {_shots}　　平均 {avg:0.0} 点\n" +
                "<size=14>左クリック 撃つ・突く ／ 右クリック 覗く（狙撃銃）／ R リロード ／ 1 ピッケル・2 背中の銃・3 腰の道具（ホイールでも）／ Tab 持ち物\n" +
                "スペース 縮んで伸びる（長押しで大きく。よける）／ T スロー ／ C 記録を消す ／ V 視点切替</size>";
            GUI.Box(new Rect(10, 10, 760, 150), GUIContent.none);
            GUI.Label(new Rect(20, 16, 750, 150), text, _style);
        }

        static string ReloadText(GunHandling h) => h.ReloadStage switch
        {
            GunHandling.Stage.Magazine => $"マガジン交換 {h.StageProgress * 100f:0}%",
            GunHandling.Stage.Open => "込める準備",
            GunHandling.Stage.Insert => "1 発ずつ込めている",
            GunHandling.Stage.Pour => "上から塩を注いでいる",
            _ => "構え直し",
        };
    }
}
