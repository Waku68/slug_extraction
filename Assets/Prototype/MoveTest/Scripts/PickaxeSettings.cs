using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Tunables of the pickaxe swing, in one asset (read every frame, so they can be changed in play mode).
    /// The swing: wind up to the upper right behind with the arms stretched, bring the head down on the aimed point
    /// (the arms stretch to reach it), stop on contact (bounce off hard salt, sink into soft crops, swing through
    /// on a miss), then the arms spring back to the ready pose.
    /// </summary>
    [CreateAssetMenu(menuName = "Slug/Pickaxe Settings", fileName = "PickaxeSettings")]
    public class PickaxeSettings : ScriptableObject
    {
        [Header("タイミング（秒）")]
        [Tooltip("振りかぶる時間")] public float windupTime = 0.22f;
        [Tooltip("振りかぶった頂点でためる時間")] public float windupHold = 0.06f;
        [Tooltip("振り下ろす時間（照準の点に届くまで）")] public float strikeTime = 0.16f;
        [Tooltip("振り下ろしの加速の強さ。大きいほど、振り始めがゆっくりで最後に速い")] public float strikeEase = 2f;
        [Tooltip("当たってから構えに戻り始めるまでの時間")] public float impactTime = 0.18f;
        [Tooltip("空振りしてから構えに戻り始めるまでの時間")] public float followTime = 0.22f;
        [Tooltip("構えに戻る速さ（Hz）。大きいほど早く戻る")] public float recoverFrequency = 2.2f;
        [Range(0f, 1f), Tooltip("押しっぱなしのとき、戻りの途中で次を振り始めるタイミング（構えまでの残りの割合。0 = 完全に戻ってから）")] public float chainAt = 0.45f;

        [Header("振りの軌道")]
        [Tooltip("振りかぶった位置から照準の点までの角度（度）")] public float swingSpan = 115f;
        [Tooltip("振り下ろしが照準の点より手前（岩のてっぺんなど）にこの距離（m）以上離れて当たりそうなら、振りかぶりを浅くして狙った所に届く軌道を選び直す")] public float aimTolerance = 0.1f;
        [Tooltip("軌道は照準の点の面よりこの深さ（m）だけ内側を狙う。縦の面にも頭の先が食い込む向きで当たる")] public float aimDepth = 0.06f;
        [Tooltip("振りの面の傾き（度）。大きいほど右から振り下ろす")] public float swingSlant = 30f;
        [Tooltip("頂点でためるときに、さらに後ろへ引く角度（度）")] public float anticipation = 8f;
        [Tooltip("振りかぶったときの手首の返し（度）。振り下ろしの最後にほどけて、頭が鞭のように走る")] public float wristCock = 25f;
        [Tooltip("振りかぶったときの腕の長さ（肩から右手まで, m）")] public float windupArm = 0.15f;
        [Tooltip("腕の長さの下限・上限（m）。照準の点に届くように、この範囲で伸び縮みする")] public float minArm = 0.04f;
        public float maxArm = 0.28f;
        [Tooltip("当たり判定だけを、ピッケルの先より外へ伸ばせる長さ（m）。腕を伸ばしきっても届かない所は、この分だけ判定が届く（見た目の頭は少し手前で止まる）")] public float hitReach = 0.15f;
        [Tooltip("振り下ろす間に右手（頭側）が柄をすべり下りて、当たる瞬間に左手（柄の下）とこの間隔（m）までそろう")] public float handsTogetherGap = 0.07f;
        [Tooltip("空振りしたときに照準の点を越えて振り抜く角度（度）")] public float followAngle = 30f;
        [Tooltip("照準の先に何もないときに振り下ろす距離（目から, m）")] public float missDistance = 0.9f;
        [Tooltip("振りの中心（両肩の真ん中からのずれ、体の向き基準, m）")] public Vector3 pivotOffset = new Vector3(0.07f, 0.02f, -0.02f);

        [Header("構え・三人称（腕を縮めて、体に引き寄せて持つ）")]
        [Tooltip("左手（柄の下の端）の位置：左腕の付け根からのずれ（体の向き基準, m）")] public Vector3 tpLeftHand = new Vector3(0.04f, -0.03f, 0.08f);
        [Tooltip("右手の位置：右腕の付け根からのずれ（体の向き基準, m）。左手より少し上にして、頭が上側に来る")] public Vector3 tpRightHand = new Vector3(-0.02f, 0.06f, 0.08f);
        [Tooltip("ピッケルの頭の先が向く方向（体の向き基準）")] public Vector3 tpHeadDir = new Vector3(0f, 0.4f, 1f);
        [Header("構え・一人称（振っていないとき）")]
        [Tooltip("左手（柄の下の端を持つ手）の位置（両肩の真ん中から、視線の向き基準, m）。体に引き寄せて持つ")] public Vector3 restGrip = new Vector3(-0.04f, -0.08f, 0.1f);
        [Tooltip("柄の向き（視線の向き基準）。左下から右上へ、頭が右肩の上に来る")] public Vector3 restHandleDir = new Vector3(0.3f, 0.9f, 0.12f);
        [Tooltip("ピッケルの頭の先が向く方向（視線の向き基準）")] public Vector3 restHeadDir = new Vector3(-0.3f, 0f, 1f);
        [Tooltip("構えているときの右手の位置（柄の下の端からの距離, m）。頭のすぐ下を持つ。振り下ろすと左手のそばまですべり下りる")] public float restUpperHand = 0.3f;
        [Range(0f, 1f), Tooltip("上下を見たときに構えがついてくる割合")] public float aimPitchFollow = 0.7f;

        [Header("当たったとき")]
        [Range(0f, 1f), Tooltip("振り始めてからこの割合を過ぎたら当たり判定をする")] public float sweepStart = 0.15f;
        [Tooltip("ピッケルの頭の当たり判定の太さ（m）")] public float tipRadius = 0.035f;
        [Range(0f, 1f), Tooltip("硬いもの（岩塩）：跳ね返りの強さ（速さの何割で戻るか）")] public float hardRestitution = 0.5f;
        [Tooltip("硬いもの：ヒットストップ（当たった瞬間に止まる時間, 秒）")] public float hardHitStop = 0.07f;
        [Tooltip("柔らかいもの（作物）：めり込む深さ（m）")] public float softPenetration = 0.05f;
        [Tooltip("柔らかいもの：ヒットストップ（秒）")] public float softHitStop = 0.035f;
        [Range(0f, 1f), Tooltip("そのほか（地面・壁）：跳ね返りの強さ")] public float otherRestitution = 0.3f;
        [Tooltip("そのほか：ヒットストップ（秒）")] public float otherHitStop = 0.05f;
        [Tooltip("当たった衝撃で腕が縮む速さ（m/s、全力で当てたとき）")] public float armSquish = 1.2f;
        [Tooltip("当たった衝撃で柄が震える強さ（度/s、全力で当てたとき）")] public float handleQuiver = 500f;
        [Tooltip("画面の揺れの強さ（全力で当てたとき）")] public float shake = 1f;
        [Tooltip("画面の揺れの長さ（秒）")] public float shakeDuration = 0.2f;

        [Header("体の動き")]
        [Tooltip("振りかぶったときに上半身を反らす角度（度）")] public float leanBack = 12f;
        [Tooltip("振り下ろしたときに上半身を前へ倒す角度（度）")] public float leanForward = 16f;

        [Header("一人称の見え方")]
        [Tooltip("構えているとき：ピッケルと腕を目に近づけて小さく描く倍率（壁にめり込まない）")] public float fpScale = 0.5f;
        [Tooltip("振り下ろして当たるとき：手が来る画面の位置（x, y は画面の割合、z は目からの距離 m）。頭は当たる点、手は画面の右下の端になるように回して描く（腕はほとんど画面の外）")] public Vector3 fpStrikeHands = new Vector3(0.82f, 0f, 0.26f);
        [Tooltip("空振りのとき：照準の点に向かって小さく描く倍率")] public float fpStrikeScale = 0.55f;
        [Tooltip("一人称の腕の太さの倍率")] public float fpArmThickness = 0.6f;
        [Tooltip("構えているときに、画面の中でずらす量（カメラ基準, m）。右下に下げて、ピッケルの頭と右手のこぶしだけが見える。振り下ろすと 0 になり、照準の点に当たって見える")] public Vector3 fpIdleOffset = new Vector3(0.135f, -0.14f, 0.22f);
        [Tooltip("振りかぶったときに、さらに画面の中でずらす量（カメラ基準, m）。ピッケルは画面の右へ抜けて、伸ばした腕は見せない")] public Vector3 fpWindupOffset = new Vector3(0.05f, 0.04f, 0f);
        [Tooltip("一人称の右腕の付け根（カメラ基準, m）。画面の右下の外に置く（見える腕が短くなる）")] public Vector3 fpArmAnchor = new Vector3(0.2f, -0.22f, 0.1f);
        [Tooltip("一人称の左腕の付け根（カメラ基準, m）。右腕と同じく画面の右下の外に置く")] public Vector3 fpArmAnchorLeft = new Vector3(0.1f, -0.26f, 0.1f);
        [Range(0.05f, 1f), Tooltip("振り下ろしのどこまでで、一人称のずらしが 0 になるか")] public float fpStrikeBlend = 0.7f;
        [Tooltip("振り下ろしを、画面の右下の手を中心に回して描くときに、当たる位置から振り上げて見せる角度の上限（度）。これ以上はカメラの後ろへ回らず、画面の上で待つ")] public float fpRaiseLimit = 70f;

        [Header("腕の形")]
        [Tooltip("腕の太さ（付け根の半径, m）")] public float armRadiusRoot = 0.022f;
        [Tooltip("腕の太さ（手首の半径, m）")] public float armRadiusHand = 0.015f;
        [Tooltip("握りこぶしの大きさ（m）")] public Vector3 fistSize = new Vector3(0.05f, 0.042f, 0.05f);

        [Header("ばね（細かい調整用）")]
        public float impactSpringHz = 4.5f;
        public float impactDamping = 0.3f;
        public float armSpringHz = 6f;
        public float armDamping = 0.3f;
        public float quiverHz = 11f;
        public float quiverDamping = 0.12f;
        public float followSpringHz = 6f;
        public float followDamping = 1f;
        public float leanSpringHz = 4f;
        public float leanDamping = 0.55f;
    }
}
