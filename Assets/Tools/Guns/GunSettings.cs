using UnityEngine;
using UnityEngine.Serialization;

namespace SlugMove
{
    public enum GunKind { Rifle, Shotgun, Sniper, Pistol }
    public enum FireMode { Auto, Semi }
    /// <summary>What has to be worked by hand after every shot.</summary>
    public enum GunAction { None, Pump, Bolt }
    /// <summary>How it is loaded: a whole magazine at once, or one round at a time.</summary>
    public enum ReloadStyle { Magazine, OneByOne, Pour }

    /// <summary>
    /// Tunables of one gun, in one asset per gun (read every frame, so they can be changed in play mode).
    /// Every gun shoots on CO2, like a paintball marker (no powder, so the rain never stops it).
    /// Carried across the chest; the trigger shoulders it (the butt on the right shoulder) and it fires salt: grains of
    /// refined salt (rifle, pistol), a handful of coarse salt (shotgun) or a chunk of rock salt (sniper). Every shot pushes it
    /// back into the soft shoulder, flips the muzzle and rocks the upper body; the eyes climb and come partly back.
    /// <see cref="Preset"/> gives the starting values of each kind.
    /// </summary>
    [CreateAssetMenu(menuName = "Slug/Gun Settings", fileName = "GunSettings")]
    public class GunSettings : ScriptableObject
    {
        [Header("銃の種類")]
        [Tooltip("形と手の動き")] public GunKind kind = GunKind.Rifle;
        /// <summary>A small gun (carried on the hip, quick to draw); the others are big and carried on the back.</summary>
        public bool Small => kind == GunKind.Pistol;
        [Tooltip("画面に出す名前")] public string displayName = "アサルトライフル";
        [Tooltip("弾の名前")] public string ammoName = "精製塩";
        [Tooltip("Auto = 押しっぱなしで連射、Semi = 1 回押すごとに 1 発")] public FireMode fireMode = FireMode.Auto;
        [Tooltip("撃つたびに手で動かすもの（ポンプ、ボルト）")] public GunAction action = GunAction.None;
        [Tooltip("ポンプ・ボルトを動かす時間（秒）。この間は撃てない")] public float cycleTime = 0.6f;
        [Tooltip("1 回で飛ぶ粒の数（散弾は多い）")] public int pellets = 1;
        [Tooltip("むき出しの体に何発当てると倒れるか（0 = 型B の値：ライフル 8、拳銃 12、散弾銃 2（粒を全部当てて）、狙撃銃 2）")]
        public float bodyHitsToFall = 0f;

        /// <summary>Shots on the bare body that bring a slug down (health_damage_discussion.md, type B).</summary>
        public float HitsToFall => bodyHitsToFall > 0f ? bodyHitsToFall
            : kind == GunKind.Pistol ? 12f : kind == GunKind.Shotgun ? 2f : kind == GunKind.Sniper ? 2f : 8f;
        /// <summary>The sniper alone brings a slug down with one round to the bare head.</summary>
        public bool HeadshotFalls => kind == GunKind.Sniper;
        [Tooltip("散弾の広がり（度）。粒ごとにこの円錐の中へ散る")] public float pelletSpread = 0f;

        [Header("射撃")]
        [Tooltip("連射の速さ（発/分）")] public float fireRate = 600f;
        [Tooltip("マガジンに入る塩の数")] public int magazineSize = 30;
        [Tooltip("塩の粒の速さ（m/s）")] public float muzzleSpeed = 140f;
        [Tooltip("重力の効き（1 = そのまま）。遠くを狙うと少し落ちる")] public float gravityScale = 1f;
        [Tooltip("塩の粒が消えるまでの時間（秒）")] public float roundLife = 1.2f;
        [Range(0f, 1f), Tooltip("肩付けがこの割合まで上がったら撃てる（0 = 下げたままでも撃てる）")] public float fireAtRaise = 0.7f;

        [Header("覗く（右クリック）")]
        [Tooltip("覗いたときの視野（度）。0 = 覗けない")] public float scopeFov = 0f;
        [Tooltip("覗くまでの時間（秒）")] public float scopeTime = 0.18f;
        [Range(0f, 1f), Tooltip("覗いているときのばらつきの倍率")] public float scopeSpread = 0.1f;
        [Tooltip("覗いたときの、目からスコープの接眼レンズまでの距離（m, 縮める前）")] public float scopeEyeRelief = 0.07f;

        [Header("ばらつき（度）")]
        [Tooltip("止まって 1 発目を撃ったときのばらつき")] public float spreadBase = 0.3f;
        [Tooltip("1 発ごとに増えるばらつき")] public float spreadPerShot = 0.3f;
        [Tooltip("連射で増えるばらつきの上限")] public float spreadMax = 3f;
        [Tooltip("撃つのをやめてから、ばらつきが戻る速さ（度/秒）")] public float spreadRecovery = 8f;
        [Tooltip("全速で這っているときに増えるばらつき")] public float moveSpread = 1.5f;

        [Header("反動・視点（度）")]
        [Tooltip("1 発ごとに視点が上がる角度")] public float climbPerShot = 0.55f;
        [Tooltip("1 発ごとに視点が左右にぶれる角度（ランダム）")] public float climbYaw = 0.3f;
        [Range(-1f, 1f), Tooltip("左右のぶれの偏り（+ = 右に流れやすい）")] public float climbYawBias = 0.25f;
        [Range(0f, 1f), Tooltip("撃つのをやめたあと、上がった視点が戻る割合（自分で下げた分は差し引く）")] public float climbRecovery = 0.65f;
        [Tooltip("視点が戻る速さ（度/秒）")] public float climbRecoverySpeed = 9f;
        [Tooltip("画面の揺れの強さ（1 発ごと）")] public float shake = 0.22f;
        [Tooltip("画面の揺れの長さ（秒）")] public float shakeDuration = 0.07f;

        [Header("反動・銃と体")]
        [Tooltip("1 発ごとに銃が肩へ押し込まれる量（m, おおよそ）")] public float kickBack = 0.022f;
        [Tooltip("1 発ごとに銃口が跳ね上がる角度（度, おおよそ）")] public float kickPitch = 3.5f;
        [Tooltip("1 発ごとに銃口が左右にぶれる角度（度, ランダム）")] public float kickYaw = 1.2f;
        [Tooltip("1 発ごとに銃がねじれる角度（度, ランダム）")] public float kickRoll = 2.5f;
        [Tooltip("1 発ごとに上半身が後ろへ揺れる角度（度）。やわらかい体が反動を受け止める")] public float shoulderLean = 2.5f;
        [Tooltip("1 発ごとにこぶしがぎゅっと縮む量（割合）")] public float fistSqueeze = 0.12f;
        [Tooltip("銃が戻るばねの速さ（Hz）と減衰")] public float kickSpringHz = 9f;
        public float kickDamping = 0.5f;
        [Tooltip("上半身が戻るばねの速さ（Hz）と減衰")] public float leanSpringHz = 4.5f;
        public float leanDamping = 0.45f;

        [Header("構え")]
        [Tooltip("肩付けまでの時間（秒）")] public float raiseTime = 0.12f;
        [Tooltip("下げるまでの時間（秒）")] public float lowerTime = 0.3f;
        [Tooltip("最後に撃ってから、肩付けのまま待つ時間（秒）")] public float holdTime = 0.9f;
        [Tooltip("肩付けのときに上半身を前へ倒す角度（度）")] public float readyLean = 4f;
        [Tooltip("銃床の位置：右腕の付け根からのずれ（体の向き基準, m）。ここが右肩")] public Vector3 buttOffset = new Vector3(-0.012f, 0.022f, 0.022f);
        [Tooltip("三人称で撃っていないとき（胸の前で斜めに抱える）：右手の位置。右腕の付け根からのずれ（体の向き基準, m）")] public Vector3 carryRightHand = new Vector3(-0.04f, -0.03f, 0.1f);
        [Tooltip("三人称で撃っていないとき：銃口の向き（体の向き基準）。左下へ")] public Vector3 carryDirection = new Vector3(-0.85f, -0.6f, 0.15f);
        [Tooltip("三人称で撃っていないとき：銃の上側が向く方向（体の向き基準）。前上へ向けると、マガジンが胸の側に来る")] public Vector3 carryUp = new Vector3(0f, 0.7f, 1f);
        [Tooltip("一人称で下げているとき：銃口を下へ向ける角度（度）")] public float lowPitch = 30f;
        [Tooltip("一人称で下げているとき：銃口を内側（左）へ向ける角度（度）")] public float lowYaw = 22f;
        [Tooltip("一人称で下げているとき：銃を傾ける角度（度）")] public float lowRoll = 12f;
        [Tooltip("一人称で下げているとき：銃床をずらす量（体の向き基準, m）")] public Vector3 lowButtOffset = new Vector3(0.01f, -0.035f, 0.035f);
        [Tooltip("銃が目線に遅れてついてくる速さ（Hz）。小さいほど振り回したときに遅れる")] public float aimLagHz = 11f;
        [Tooltip("銃口を向ける点の近さの下限（m）。目の前に壁があっても、銃が顔の前を横切らない")] public float minAimDistance = 2.5f;
        [Tooltip("照準の先に何もないときに銃口を向ける距離（m）")] public float farAimDistance = 60f;

        [Header("持ち替え・リロード")]
        [Tooltip("持ち替えて構えるまでの時間（秒）。持ち物の設定（LoadoutSettings）があれば、背中・腰から取る時間のほうを使う")] public float equipTime = 0.35f;
        [Tooltip("Magazine = マガジンごと替える、OneByOne = 1 発ずつ込める、Pour = 上の注ぎ口から塩を注ぐ（ライフル）")] public ReloadStyle reloadStyle = ReloadStyle.Pour;
        [Tooltip("注ぐとき：1 秒に注げる塩の粒の数")] public float pourRate = 30f;
        [Tooltip("マガジンを替える時間（秒）")] public float reloadTime = 1.5f;
        [Tooltip("1 発ずつ込める・注ぐとき：構え直してから込め始める（注ぎ始める）までの時間（秒）")] public float reloadOpenTime = 0.35f;
        [Tooltip("1 発ずつ込めるとき：1 発を込める時間（秒）")] public float insertTime = 0.45f;
        [Tooltip("1 発ずつ込める・注ぐとき：込め終わってから構えに戻るまでの時間（秒）")] public float reloadCloseTime = 0.3f;
        [Tooltip("リロードで銃を傾ける角度（度）。右手で作業しやすいように")] public float reloadRoll = 28f;
        [Tooltip("リロードで銃口を上げる角度（度）")] public float reloadPitch = -10f;
        [Tooltip("リロードで銃床をずらす量（体の向き基準, m）")] public Vector3 reloadButtOffset = new Vector3(-0.02f, -0.02f, 0.03f);
        [Tooltip("マガジンを抜く距離（マガジンの向きに, m）")] public float magazineDrop = 0.09f;
        [Tooltip("抜いたマガジンや、込める弾を取りに行く先（銃の向き基準, m）。右手が持っていって、戻ってくる")] public Vector3 magazineAway = new Vector3(0.08f, -0.14f, -0.08f);
        [Tooltip("ポンプ・ボルトを引く距離（m）")] public float actionTravel = 0.07f;

        [Header("塩の粒の見え方")]
        [Tooltip("粒の色")] public Color roundColor = new Color(1f, 0.98f, 0.92f);
        [Tooltip("撃った粒は目から飛ぶ。見た目の粒は銃口から出て、この距離（m）までに本当の軌道に合流する")] public float convergeDistance = 4f;
        [Tooltip("粒の光の筋の長さ（m）")] public float tracerLength = 0.45f;
        [Tooltip("粒の太さ（m）")] public float grainSize = 0.012f;
        [Tooltip("当たった跡の大きさの倍率")] public float markScale = 1f;
        [Tooltip("当たったときに跳ねる欠片の数（1 粒ごと）")] public int chips = 2;
        [Tooltip("撃ったときに銃口から炭酸ガスが吹いている時間（秒）")] public float flashTime = 0.035f;
        [Tooltip("銃口から吹くガスの大きさの倍率")] public float flashSize = 1f;
        [Tooltip("撃ったときの光の強さ。炭酸ガスは光らないので 0")] public float flashLight = 0f;

        [Header("一人称の見え方")]
        [Tooltip("一人称の銃床（右肩）の位置（カメラ基準, m, 縮める前）。本当の肩は目のすぐ下で、そのままだと機関部が画面を覆うので、右下に離して構える。銃口は照準の点に向く")] public Vector3 fpShoulder = new Vector3(0.13f, -0.15f, -0.01f);
        [Tooltip("銃と腕を、目に向かって縮めて描く倍率。画面での見え方は同じまま、壁にめり込みにくい")] public float fpScale = 0.42f;
        [Range(0f, 1f), Tooltip("一人称で下げているときに、上の角度を使う割合。小さいほど画面に残る")] public float fpLowAngles = 0.45f;
        [Tooltip("一人称で下げているときに、画面の中でずらす量（カメラ基準, m, 縮めた後）")] public Vector3 fpLowOffset = new Vector3(0.006f, -0.008f, 0f);
        [FormerlySerializedAs("fpReloadWell")]
        [Tooltip("一人称でリロードするときに、弾を入れる所（マガジンの差し込み口など）を置く位置（カメラ基準, m, 縮める前）。画面の下の真ん中あたり")] public Vector3 fpReloadFocus = new Vector3(0.05f, -0.115f, 0.3f);
        [Tooltip("一人称でリロードするときに、弾を入れる所を中心に銃を回す角度（カメラ基準, 度）。銃口を左へ振って、右側面を目に向ける")] public Vector3 fpReloadTurn = new Vector3(-6f, -40f, 0f);
        [Tooltip("目の前に壁があるとき、一人称の銃を手前へ引く。銃口と壁の間に残す隙間（m）")] public float fpWallMargin = 0.03f;

        [Header("腕の形")]
        [Tooltip("腕の太さ（付け根の半径, m）")] public float armRadiusRoot = 0.022f;
        [Tooltip("腕の太さ（手首の半径, m）")] public float armRadiusHand = 0.015f;
        [Tooltip("握りこぶしの大きさ（m）")] public Vector3 fistSize = new Vector3(0.05f, 0.042f, 0.05f);
        [Tooltip("一人称の腕の太さの倍率。左腕が画面の下を横切るので、少し細くして銃を見せる")] public float fpArmThickness = 0.7f;

        /// <summary>The starting values of each kind (an asset is made from this once and then tuned by hand).</summary>
        public void Preset(GunKind k)
        {
            kind = k;
            switch (k)
            {
                case GunKind.Shotgun:
                    // coarse salt: a handful of grains, close range, a pump after every shot, loaded one shell at a time
                    displayName = "散弾銃"; ammoName = "粗塩";
                    fireMode = FireMode.Semi; action = GunAction.Pump; cycleTime = 0.55f;
                    pellets = 9; pelletSpread = 4.5f;
                    fireRate = 150f; magazineSize = 6; muzzleSpeed = 85f; gravityScale = 1.6f; roundLife = 0.45f;
                    spreadBase = 0.6f; spreadPerShot = 0.6f; spreadMax = 2f; spreadRecovery = 4f; moveSpread = 1f;
                    climbPerShot = 3f; climbYaw = 0.8f; climbYawBias = 0.1f; climbRecovery = 0.75f; climbRecoverySpeed = 14f;
                    shake = 0.7f; shakeDuration = 0.12f;
                    kickBack = 0.045f; kickPitch = 9f; kickYaw = 2f; kickRoll = 3f; shoulderLean = 6f; fistSqueeze = 0.22f;
                    kickSpringHz = 7f; kickDamping = 0.55f;
                    holdTime = 1.1f;
                    reloadStyle = ReloadStyle.OneByOne; reloadOpenTime = 0.28f; insertTime = 0.42f; reloadCloseTime = 0.3f;
                    reloadRoll = 32f; reloadPitch = -6f; actionTravel = 0.075f;
                    magazineAway = new Vector3(0.09f, -0.12f, -0.1f);
                    roundColor = new Color(0.93f, 0.9f, 0.86f); grainSize = 0.009f; tracerLength = 0.22f; convergeDistance = 2.5f;
                    markScale = 0.7f; chips = 1; flashTime = 0.06f; flashSize = 1.6f; flashLight = 0f;
                    fpReloadFocus = new Vector3(0.05f, -0.11f, 0.3f); fpReloadTurn = new Vector3(-6f, -38f, 0f);
                    break;
                case GunKind.Sniper:
                    // a chunk of rock salt: one heavy shot, a bolt after every shot, a scope on the right button
                    displayName = "狙撃銃"; ammoName = "岩塩の塊";
                    fireMode = FireMode.Semi; action = GunAction.Bolt; cycleTime = 0.85f;
                    pellets = 1; pelletSpread = 0f;
                    fireRate = 120f; magazineSize = 5; muzzleSpeed = 300f; gravityScale = 1f; roundLife = 1.6f;
                    scopeFov = 14f; scopeTime = 0.2f; scopeSpread = 0.05f; scopeEyeRelief = 0.07f;
                    spreadBase = 2.2f; spreadPerShot = 1.5f; spreadMax = 4f; spreadRecovery = 3f; moveSpread = 3f;
                    climbPerShot = 2.6f; climbYaw = 0.5f; climbYawBias = 0f; climbRecovery = 0.85f; climbRecoverySpeed = 10f;
                    shake = 0.55f; shakeDuration = 0.12f;
                    kickBack = 0.05f; kickPitch = 7f; kickYaw = 1f; kickRoll = 2f; shoulderLean = 5f; fistSqueeze = 0.2f;
                    kickSpringHz = 6f; kickDamping = 0.55f;
                    holdTime = 1.4f;
                    reloadStyle = ReloadStyle.OneByOne; reloadOpenTime = 0.55f; insertTime = 0.4f; reloadCloseTime = 0.5f;
                    reloadRoll = 22f; reloadPitch = -8f; actionTravel = 0.075f;
                    magazineAway = new Vector3(0.09f, -0.06f, -0.12f);
                    roundColor = new Color(0.98f, 0.82f, 0.84f); grainSize = 0.02f; tracerLength = 1.4f; convergeDistance = 6f;
                    markScale = 1.8f; chips = 5; flashTime = 0.06f; flashSize = 1.4f; flashLight = 0f;
                    farAimDistance = 120f;
                    // the scope rides high above the bore: shoulder it lower in view, so its eyepiece stays out of the eye
                    fpShoulder = new Vector3(0.14f, -0.2f, 0f);
                    fpReloadFocus = new Vector3(0.04f, -0.1f, 0.28f); fpReloadTurn = new Vector3(-10f, -36f, 0f);
                    break;
                case GunKind.Pistol:
                    // refined salt from a small gun on the hip: one shot per click, held out in both hands (no shoulder),
                    // a quick magazine change through the grip
                    displayName = "拳銃"; ammoName = "精製塩";
                    fireMode = FireMode.Semi; action = GunAction.None;
                    pellets = 1; pelletSpread = 0f;
                    fireRate = 380f; magazineSize = 10; muzzleSpeed = 110f; gravityScale = 1.2f; roundLife = 0.9f;
                    fireAtRaise = 0.6f;
                    spreadBase = 0.6f; spreadPerShot = 0.55f; spreadMax = 3f; spreadRecovery = 6f; moveSpread = 1.2f;
                    climbPerShot = 1.3f; climbYaw = 0.45f; climbYawBias = 0.15f; climbRecovery = 0.8f; climbRecoverySpeed = 12f;
                    shake = 0.18f; shakeDuration = 0.06f;
                    kickBack = 0.012f; kickPitch = 9f; kickYaw = 1.5f; kickRoll = 2f; shoulderLean = 1.2f; fistSqueeze = 0.16f;
                    kickSpringHz = 8f; kickDamping = 0.5f; leanSpringHz = 5f;
                    raiseTime = 0.1f; lowerTime = 0.25f; holdTime = 0.8f; readyLean = 2f;
                    // held out in front of the chest, both hands on the grip (the "butt" is the back of the grip)
                    buttOffset = new Vector3(-0.075f, 0.035f, 0.17f);
                    carryRightHand = new Vector3(-0.05f, -0.035f, 0.1f);
                    carryDirection = new Vector3(-0.15f, -0.75f, 0.65f);
                    carryUp = new Vector3(0f, 0.65f, 0.75f);
                    lowPitch = 18f; lowYaw = 6f; lowRoll = 4f; lowButtOffset = new Vector3(-0.005f, -0.012f, -0.02f);
                    equipTime = 0.3f;
                    reloadStyle = ReloadStyle.Magazine; reloadTime = 1.15f;
                    reloadRoll = 26f; reloadPitch = -4f; reloadButtOffset = new Vector3(-0.01f, 0.005f, -0.03f);
                    magazineDrop = 0.07f; magazineAway = new Vector3(-0.07f, -0.1f, -0.02f);
                    roundColor = new Color(1f, 0.98f, 0.92f); grainSize = 0.01f; tracerLength = 0.35f; convergeDistance = 3f;
                    markScale = 0.8f; chips = 2; flashTime = 0.04f; flashSize = 0.75f; flashLight = 0f;
                    fpShoulder = new Vector3(0.055f, -0.085f, 0.24f);
                    fpScale = 0.45f; fpLowAngles = 0.5f; fpLowOffset = new Vector3(0.004f, -0.006f, 0f);
                    fpReloadFocus = new Vector3(0.035f, -0.075f, 0.25f); fpReloadTurn = new Vector3(-6f, -32f, 0f);
                    break;
            }
        }
    }
}
