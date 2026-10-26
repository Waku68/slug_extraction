using UnityEngine;

namespace SlugWorld
{
    /// <summary>
    /// How the slug's body fares on the island: its moisture (docs/discussions/moisture_discussion.md) and its health
    /// (health_damage_discussion.md). These are the makers' values: the player never sees them as numbers, only in how the
    /// body looks and moves. Read every frame, so it can be tuned in play mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Slug/Condition Settings", fileName = "ConditionSettings")]
    public class ConditionSettings : ScriptableObject
    {
        [Header("湿り気（0〜満タン）")]
        [Tooltip("満タン（出撃はここから）")] public float moistureMax = 100f;
        [Tooltip("雨と水場でためられる上限")] public float moistureOverfill = 120f;
        [Tooltip("満タンを超えた分が抜ける速さ（1分あたり）")] public float overfillDrain = 5f;

        [Header("1分あたりの増減（昼）")]
        [Tooltip("晴れ・日なた")] public float sunnyInSun = -20f;
        [Tooltip("晴れ・日陰（巨大な木や岩の影）")] public float sunnyInShade = -4f;
        [Tooltip("曇り（雨の中でも屋根の下ならこれ）")] public float cloudy = -5f;
        [Tooltip("夜（晴れ・曇り）。露で止まる")] public float night = 0f;
        [Tooltip("小雨（空の下）")] public float lightRain = 20f;
        [Tooltip("大雨（空の下）")] public float heavyRain = 50f;
        [Tooltip("水に触れている（水たまり・沢・濡れた苔）：1秒あたり")] public float waterPerSecond = 10f;

        [Header("塩の上（塩田、岩塩の洞窟）")]
        [Tooltip("減り方の倍率")] public float saltDrainMultiplier = 2f;
        [Tooltip("雨の戻り方の倍率")] public float saltRainMultiplier = 0.5f;
        [Tooltip("夜の減り方（1分あたり）。夜でも塩は体の水を吸う（満タンから4分）")] public float saltNight = -25f;

        [Header("乾いたとき")]
        [Tooltip("これより少ないと、見た目が乾いてくる（つやが消え、くすみ、少し縮む）")] public float looksDryBelow = 30f;
        [Tooltip("ゼロのときの這う速さの倍率")] public float zeroSpeed = 0.8f;
        [Tooltip("ゼロのとき、体力が満タンから倒れるまでの秒数")] public float zeroSecondsToFall = 90f;
        [Tooltip("これより少ないと、画面の端がかすむ（危ないことを数字なしで伝える）")] public float warnBelow = 12f;

        [Header("水の苔玉（ベルト）")]
        [Tooltip("ベルトに付けられる数")] public int mossBalls = 2;
        [Tooltip("1つで戻る湿り気")] public float mossAmount = 40f;
        [Tooltip("使うのにかかる秒数（その間は道具を使えない）")] public float mossUseSeconds = 2f;
        [Tooltip("水場に触れたまま、これだけたつと苔玉が水を吸い直す（秒）")] public float mossRefillSeconds = 3f;

        [Header("体力")]
        [Tooltip("満タン")] public float healthMax = 100f;
        [Tooltip("湿り気がこれ以上あると、体力がだんだん戻る")] public float regenAbove = 70f;
        [Tooltip("倒れる寸前から満タンまで戻る秒数（180 = 3分）")] public float regenSecondsToFull = 180f;
        [Tooltip("戻るときに使う湿り気（1分あたり）")] public float regenMoistureCost = 3f;
    }
}
