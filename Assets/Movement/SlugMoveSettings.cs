using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Tunables of the first-person crawl prototype, in one asset. Every value is read each frame,
    /// so it can be changed in play mode (changes made in play mode stay, because this is an asset).
    /// The defaults lean slow and heavy. The slug is about 1.1 m long with its eyes about 0.47 m above the ground.
    /// </summary>
    [CreateAssetMenu(menuName = "Slug/Move Settings", fileName = "SlugMoveSettings")]
    public class SlugMoveSettings : ScriptableObject
    {
        [Header("移動")]
        [Tooltip("最高速度（m/s）")] public float maxSpeed = 1.2f;
        [Tooltip("加速（m/s²）。小さいほど動き出しと方向転換が重い")] public float acceleration = 1.5f;
        [Tooltip("入力を離したときの一定のブレーキ（m/s²）。滑りとは別にかかる")] public float deceleration = 0.3f;
        [Tooltip("止まるときの滑り（秒）。大きいほど、ぬるっと長く滑る")] public float slideTime = 0.5f;
        [Range(0f, 1f), Tooltip("横移動（A/D）の速さの倍率")] public float strafeMultiplier = 0.6f;
        [Range(0f, 1f), Tooltip("後ろ移動（S）の速さの倍率")] public float backwardMultiplier = 0.5f;
        [Tooltip("体の向きが視点の向きに追いつく速さ（度/秒）")] public float bodyTurnSpeed = 120f;

        [Header("向きを変えるとき（お尻が遅れてついてくる）")]
        [Tooltip("止まっているときに、お尻が体の向きに追いつく速さ（Hz）。小さいほど、ぬるっと遅れてついてくる")] public float tailFollowHz = 0.75f;
        [Tooltip("這う速さ 1 m/s あたりに足す、追いつく速さ（Hz）。進むと、お尻は通った跡をなぞるように早くそろう")] public float tailFollowPerSpeed = 1.2f;
        [Range(0.2f, 2f), Tooltip("追いつき方の減衰。1 = 行き過ぎずに止まる、小さいほど少し行き過ぎて戻る（ぷるん）")] public float tailFollowDamping = 0.8f;
        [Range(0f, 120f), Tooltip("お尻が遅れられる最大の角度（度）。これ以上は引きずられる")] public float tailMaxLag = 80f;

        [Header("坂")]
        [Range(0f, 89f), Tooltip("登れる最大の角度（度）。これより急な坂は登れない")] public float slopeLimit = 60f;
        [Range(0f, 1f), Tooltip("登り坂での減速。限界角度の坂をまっすぐ登ると速度が (1 - この値) 倍になる")] public float uphillSlowdown = 0.6f;
        [Range(0f, 1f), Tooltip("下り坂での加速。限界角度の坂をまっすぐ下ると速度が (1 + この値) 倍になる")] public float downhillSpeedup = 0.2f;
        [Tooltip("登れない急坂に乗ったときにずり落ちる速さ（m/s）")] public float steepSlideSpeed = 1.2f;
        [Tooltip("体を地面の傾きに合わせる速さ（大きいほど早く合う）")] public float slopeAlignSpeed = 6f;
        [Range(0f, 1f), Tooltip("坂や壁で体が傾いても、上半身（持ち上げた前側）を起こしておく割合。1 = まっすぐ立てる")] public float uprightAmount = 1f;

        [Header("段差")]
        [Tooltip("そのまま乗り越えられる段差の高さ（m）。これより高い段差はよじ登る")] public float stepHeight = 0.15f;

        [Header("よじ登り（壁を這い上がる）")]
        [Tooltip("よじ登れる段差の高さ（m）。目の高さは約 0.47 m")] public float climbHeight = 0.45f;
        [Tooltip("よじ登る速さ（m/s、上向き）")] public float climbSpeed = 0.3f;
        [Range(0f, 90f), Tooltip("よじ登るときに体を起こす最大の角度（度）")] public float climbTilt = 50f;

        [Header("縮んで伸びる（スペース長押しで縮み、離すと前へ伸びる）")]
        [Tooltip("いちばん縮むまでの時間（秒）。長く溜めるほど遠くへ伸びる")] public float lungeChargeTime = 0.45f;
        [Tooltip("溜めずに離したときの、伸びる速さ（m/s）")] public float lungeSpeedMin = 1.6f;
        [Tooltip("溜めきって離したときの、伸びる速さ（m/s）")] public float lungeSpeedMax = 3.3f;
        [Tooltip("伸びるときに浮く速さ（m/s、溜めきったとき）。地面から少し離れる")] public float lungeUp = 1.15f;
        [Tooltip("伸びたあと、次に縮めるまでの時間（秒）")] public float lungeCooldown = 0.7f;
        [Range(0f, 1f), Tooltip("縮んでいる間の移動の速さの倍率。溜めている間はほぼ止まる（狙われやすい）")] public float lungeChargeMove = 0.25f;
        [Tooltip("伸びた勢いが抜けるまでの時間（秒、地面に着いてから）。大きいほど、ぬるっと長く滑る")] public float lungeSlideTime = 0.42f;
        [Range(0f, 0.5f), Tooltip("溜めたときに体が縮む割合（前後の長さ）")] public float lungeSquash = 0.28f;
        [Range(0f, 0.5f), Tooltip("伸びるときに体が伸びる割合（前後の長さ）")] public float lungeStretch = 0.3f;
        [Tooltip("体の伸び縮みが戻るばねの速さ（Hz）と減衰。減衰が小さいほど、着地でぷるんと揺れる")] public float lungeSpringHz = 3f;
        public float lungeSpringDamping = 0.35f;

        [Header("這う動き（体の波）")]
        [Tooltip("止まっているときの波の進み（1 = シェーダー本来の速さ）")] public float idleCrawlRate = 0.2f;
        [Tooltip("速度 1 m/s あたりに足す波の進み")] public float crawlRatePerSpeed = 1.1f;

        [Header("視点")]
        [Tooltip("マウス感度（度/ピクセル）")] public float mouseSensitivity = 0.08f;
        [Tooltip("視野角（度）")] public float fieldOfView = 70f;
        [Tooltip("目の位置から前へずらす量（m）。顔の中にカメラが埋まらないように")] public float eyeForward = 0.04f;
        [Tooltip("目の位置から上へずらす量（m）")] public float eyeUp = 0f;
        [Range(-89f, 0f)] public float minPitch = -80f;
        [Range(0f, 89f)] public float maxPitch = 80f;

        [Header("カメラ揺れ")]
        [Range(0f, 3f), Tooltip("揺れ全体の強さ（0 で揺れなし）。速度に比例してかかる")] public float bobStrength = 1f;
        [Tooltip("揺れの周期。体の波 1 周期あたり何回揺れるか（1 = 体の波と同じ）")] public float bobFrequency = 1f;
        [Tooltip("上下の揺れ幅（m）")] public float bobVertical = 0.01f;
        [Tooltip("左右の揺れ幅（m）。上下の半分の周期で揺れる")] public float bobLateral = 0.008f;
        [Tooltip("左右の傾き（度）")] public float bobRoll = 0.6f;
        [Range(0f, 1f), Tooltip("体の伸び縮みに合わせてカメラが前後する割合（1 = 頭と同じだけ動く）")] public float stretchFollow = 0.5f;

        [Header("三人称（V キー）")]
        [Tooltip("体からカメラまでの距離（m）")] public float thirdPersonDistance = 1.8f;
        [Tooltip("注視点の高さ（m）")] public float thirdPersonHeight = 0.45f;
    }
}
