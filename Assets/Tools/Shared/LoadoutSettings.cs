using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// What the slug carries and how it takes it in hand, in one asset (read every frame, so it can be tuned in play mode).
    /// The back holds a big gun and the pickaxe, the hip a small gun or a knife. Taking something off the back is slow
    /// (an arm reaches over the shoulder for it), off the hip quick. Tools not in hand ride on the body where they are
    /// carried (<see cref="SlugToolRack"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Slug/Loadout Settings", fileName = "LoadoutSettings")]
    public class LoadoutSettings : ScriptableObject
    {
        [Header("持ち替え（秒）")]
        [Tooltip("背中の銃を取って構えるまでの時間。腕が肩越しに回って取ってくる")] public float backDrawTime = 0.8f;
        [Tooltip("ピッケル（背中）を取って構えるまでの時間")] public float pickaxeDrawTime = 0.65f;
        [Tooltip("腰の物（小さい銃・ナイフ）を取って構えるまでの時間")] public float hipDrawTime = 0.3f;
        [Range(0.1f, 0.7f), Tooltip("そのうち、手が道具に届くまでの割合。残りの時間で構えの位置まで持ってくる")] public float reachPart = 0.4f;

        [Header("背中の銃（バッグの上に斜めに留める）")]
        [Tooltip("体の長さ方向の位置（0 = 尾、1 = 頭）")] public float backGunT = 0.52f;
        [Tooltip("斜めの角度（度）。+ = 銃口が右肩の上へ")] public float backGunTilt = 32f;
        [Tooltip("横へのずらし（m）。+ = 右")] public float backGunShift = 0.01f;
        [Tooltip("銃の長さのうち、留める位置より下（銃床側）にある割合")] public float backGunAlong = 0.5f;
        [Tooltip("背中（バッグがあればバッグの外側）から浮かせる距離（m）")] public float backGunStandOff = 0.03f;

        [Header("ピッケル（背中に、銃と交差させて留める）")]
        [Tooltip("体の長さ方向の位置")] public float pickaxeT = 0.5f;
        [Tooltip("斜めの角度（度）。- = 頭が左肩の上へ")] public float pickaxeTilt = -34f;
        [Tooltip("横へのずらし（m）")] public float pickaxeShift = -0.01f;
        [Tooltip("柄の長さのうち、留める位置より下にある割合")] public float pickaxeAlong = 0.45f;
        [Tooltip("背中（バッグの外側）から浮かせる距離（m）。銃より内側")] public float pickaxeStandOff = 0.008f;

        [Header("腰（右の腰のベルトに、下向きに差す）")]
        [Tooltip("体の長さ方向の位置。ベルトの高さ")] public float hipT = 0.44f;
        [Tooltip("体の周りの角度（度）。0 = 背中、90 = 右の真横、180 = おなか")] public float hipAngle = 105f;
        [Tooltip("差す向きを前へ傾ける角度（度）")] public float hipTilt = 18f;
        [Tooltip("皮膚から浮かせる距離（m）")] public float hipStandOff = 0.006f;
        [Tooltip("銃・ナイフの握るところを、ベルトからどれだけ上に出すか（m）")] public float hipGripOut = 0.035f;

        [Header("一人称で取り出すとき（カメラ基準, m, 縮める前）")]
        [Tooltip("背中から：取り出し始める位置。画面の右上の外から下りてくる")] public Vector3 fpBackFrom = new Vector3(0.24f, 0.3f, 0.02f);
        [Tooltip("背中から：取り出し始めるときの向き（構えからの回転, 度）。銃口が上を向いている")] public Vector3 fpBackTurn = new Vector3(-80f, 25f, 35f);
        [Tooltip("腰から：取り出し始める位置。画面の右下の外から上がってくる")] public Vector3 fpHipFrom = new Vector3(0.12f, -0.3f, 0.06f);
        [Tooltip("腰から：取り出し始めるときの向き（度）。下を向いている")] public Vector3 fpHipTurn = new Vector3(70f, 10f, 10f);

        /// <summary>How long taking a tool in hand takes from where it is carried.</summary>
        public float DrawTime(bool fromBack, bool pickaxe) => pickaxe ? pickaxeDrawTime : fromBack ? backDrawTime : hipDrawTime;
    }
}
