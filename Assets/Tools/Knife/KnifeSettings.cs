using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Tunables of the knife, in one asset (read every frame, so they can be changed in play mode).
    /// One stab: the hand draws back a little, the stretchy arm shoots the blade at the point under the crosshair (as
    /// far as the arm reaches), holds a moment, and comes back. Hold the button to keep stabbing.
    /// </summary>
    [CreateAssetMenu(menuName = "Slug/Knife Settings", fileName = "KnifeSettings")]
    public class KnifeSettings : ScriptableObject
    {
        [Header("突く")]
        [Tooltip("届く距離（m）。右腕の付け根から、刃先まで")] public float reach = 0.5f;
        [Tooltip("引く時間（秒）")] public float windupTime = 0.06f;
        [Tooltip("突き出す時間（秒）")] public float thrustTime = 0.075f;
        [Tooltip("突いたまま止まる時間（秒）")] public float holdTime = 0.05f;
        [Tooltip("戻す時間（秒）")] public float recoverTime = 0.2f;
        [Range(0f, 1f), Tooltip("ボタンを押し続けたとき、戻しのこの割合で次を突き始める")] public float chainAt = 0.55f;
        [Tooltip("突く前に手を引く距離（m）")] public float windupPull = 0.05f;
        [Tooltip("狙った面の、どれだけ内側を突くか（m）")] public float pierceDepth = 0.025f;
        [Tooltip("刃先の当たり判定の太さ（半径, m）")] public float tipRadius = 0.016f;
        [Tooltip("突くときに上半身を前へ倒す角度（度）")] public float lean = 7f;

        [Header("当たったとき")]
        [Tooltip("当たった瞬間に止まる時間（秒）")] public float hitStop = 0.055f;
        [Tooltip("硬いものに当たったとき、手が跳ね返る距離（m）")] public float bounce = 0.025f;
        [Tooltip("画面の揺れの強さ")] public float shake = 0.22f;
        [Tooltip("画面の揺れの長さ（秒）")] public float shakeDuration = 0.06f;

        [Header("構え（三人称）")]
        [Tooltip("右手の位置：右腕の付け根からのずれ（体の向き基準, m）")] public Vector3 readyHand = new Vector3(-0.015f, -0.005f, 0.085f);
        [Tooltip("刃の向き（体の向き基準）。前、少し上、少し内側")] public Vector3 readyDirection = new Vector3(-0.2f, 0.45f, 1f);
        [Tooltip("左手（空いている手）の位置：左腕の付け根からのずれ（体の向き基準, m）")] public Vector3 leftHand = new Vector3(0.025f, -0.02f, 0.06f);

        [Header("一人称の見え方")]
        [Tooltip("構えの位置（カメラ基準, m, 縮める前）。画面の右下")] public Vector3 fpReady = new Vector3(0.1f, -0.115f, 0.21f);
        [Tooltip("構えの刃の向き（カメラ基準）。前、少し上、内側")] public Vector3 fpReadyDirection = new Vector3(-0.35f, 0.4f, 1f);
        [Tooltip("刃を内側へ寝かせる角度（度）")] public float fpReadyRoll = -25f;
        [Tooltip("ナイフと腕を、目に向かって縮めて描く倍率")] public float fpScale = 0.5f;
        [Tooltip("一人称の腕の太さの倍率")] public float fpArmThickness = 0.75f;

        [Header("腕の形")]
        [Tooltip("腕の太さ（付け根の半径, m）")] public float armRadiusRoot = 0.022f;
        [Tooltip("腕の太さ（手首の半径, m）")] public float armRadiusHand = 0.015f;
        [Tooltip("握りこぶしの大きさ（m）")] public Vector3 fistSize = new Vector3(0.05f, 0.042f, 0.05f);
    }
}
