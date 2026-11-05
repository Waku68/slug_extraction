using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// The two-legged slug's body (Nov 2, わくさん：新しいキャラで動きを作り直す), moved by its bones every frame from how the
    /// body itself moves: whoever moves it (the player's controller, a bot) only moves and turns this transform and sets
    /// the few poses below. Built by <c>DinoSlugRig</c> (menu Slug/Character/Build Dino Slug Rig).
    ///
    ///   Feet: each foot stays planted where it stood (no sliding feet) and steps when the body has moved on: one foot at a
    ///     time, landing half a step ahead of where it hangs under the hip. A step is short when slow and long when fast
    ///     (strideSlow -> strideFast per step, at fastSpeed), and takes at least minStepTime: at full speed (1.6 m/s) about
    ///     7 steps a second of 24 cm. The legs turn from inside the body (the hip root is 6 cm up in it), so the thigh
    ///     and the low belly swing with them. Turning on the spot steps too; when it stops, the feet settle back under the hips.
    ///     The legs reach for the feet with a two-bone IK (the knee bends forward) and, being slug legs, stretch up to
    ///     legStretchMax when a long step needs more than they have.
    ///   Body (Nov 2 わくさん：もう少し躍動感): the hips bounce up as a foot swings and drop onto each landing foot, roll
    ///     over the standing foot and twist toward the leg going forward, the chest twisting back; the upper body nods
    ///     forward on each landing and leans into the speed (leanAtSpeed); the head stays nearly level. The arms swing
    ///     against the legs.
    ///   Tail: lags behind a turn (it swings out the other way, then comes back with a little overshoot), sways with each
    ///     step in a wave running down to the tip, whips up and down with the bounce and lifts a little with speed.
    ///   Running (Nov 3 わくさん：Shift で早く走る, set from outside by run): laid over the walk, which stays as it was. A step
    ///     lands every runStepTime (0.23 s; at 2.4 m/s 55 cm), but the short legs touch down for only (1 - runFlight) of it:
    ///     each foot pushes off under the hip and the rest of the step is a little flight with both feet up, so a 55 cm step
    ///     needs the legs only to stretch to legStretchRun (measured: 2.41 m/s, a foot lands every 0.233 s, both feet up
    ///     44 % of the time). The hips tip forward (hipPitchRun: the body goes forward, the tail comes up straight behind,
    ///     a running dinosaur; the eyes come down from 0.40 to 0.33 m), sink at mid-stance and rise in the flight
    ///     (hipBobRun), the feet lift higher and leave the back sooner, the arms pump further, held a little forward and
    ///     out, and the antennae sweep back.
    ///   Out of breath (Nov 3, set from outside by breathless): the shoulders heave (the chest rises and falls at breathHz),
    ///     the body hunches and the head hangs, the arms dangle forward and the antennae droop out and back.
    ///   For what comes later on the upper body (tools held, aiming): the run reaches the upper body only through the lean
    ///     and twist of spine and chest, the arm swing (stopped on a side whose stretchy arm is out) and the head; Run01,
    ///     GaitPhase, GaitAmount and Breath01 give how far each is, so a pose laid on top can take some of it back.
    ///   Poses set from outside: crouch (shrinking before a hop), stretch (in the air), prone (lying low), twist (the head
    ///     turned round on the neck to look at the back), lean (degrees, + = forward), airborne (the feet tuck under),
    ///     run (0..1, running), breathless (0..1, out of breath). run and breathless are eased in here.
    ///   Tools (Nov 2, 6章の3 伸びる腕を肩へ; <see cref="SlugBodyBase"/>): a held tool's stretchy arms grow from the
    ///     shoulders (the arm bones, armRootOut out along the side). While a side's stretchy arm is shown, that side's short
    ///     arm draws into the shoulder (scaled to armHiddenScale over armHideTime) and its swing stops; empty-handed, the
    ///     short arms swing with the walk as before.
    ///   Holding a tool (Nov 3 わくさん：持っているときは両腕をもう少し体の正面に。手がめり込んでいる): while a side's
    ///     stretchy arm is out, it grows not from the side of the round body but from the front of the chest, armFrontAngle
    ///     (45°) round from straight ahead, so the arms reach forward to the tool instead of wrapping round the belly. The
    ///     hands are where each tool's settings put them from there. Holding, the chest no longer twists back with each
    ///     step (holdSteady): the shoulders stay square over the hips, and the tool rides steady on the walk and the run.
    ///   Aiming a long gun (Nov 3 わくさんの絵：肩に担ぐのではなく、銃床を肩の前にしっかり押し当てる。上半身を斜めに、
    ///     左目を閉じる; <see cref="SetGunStance"/>): the upper body turns aimTwist (28°) to the right, the left shoulder
    ///     forward and the right one back; the hips take aimHipShare of it. The head stays facing the aim, leans down
    ///     onto the stock (aimCheek, aimHeadDown) and the left eye shuts (<see cref="SetWink"/>, everyone sees it). The
    ///     gun comes up under the right eye (<see cref="SightEyeWorld"/>): the eyes are at the very front of the round
    ///     face, so lined up on the eye itself the gun would float out in front of the body; aimEye puts the sights
    ///     4.5 cm under it and 11 cm back, the gun against the chest and the head down on it (Nov 3 わくさん：銃をもっと
    ///     体によせて、くっつけてもいい). Carried across the chest, and the pistol held out, the hands come gunPullIn
    ///     (7 cm) closer than the gun's settings, so the gun lies against the chest. Shouldered without the
    ///     sights, aimShouldered of the turn. The pistol and
    ///     the shotgun have no sights to look along (no ADS); firing, they turn aimShouldered of it (the pistol only
    ///     aimTwistPistol).
    ///   Swinging the pickaxe (Nov 3 わくさん：腕だけでなく上半身も使う。無難＝胸と肩; <see cref="SetSwingTurn"/>): the
    ///     chest and shoulders wind up with it, turned swingTwist (35°) right, the right shoulder up (swingRoll), arched
    ///     back swingArch more than the swing's own lean, and come round with the blow, square where it lands and
    ///     swingFollow on through; the hips take only swingHipShare and the legs stay. The head keeps facing the blow.
    ///     Nov 3 わくさん（近くを叩いても遠くを叩いても腕の伸びをあまり変えない。もっと振りかぶって、右腕が伸びすぎない）:
    ///     the pickaxe is drawn wound deeper (PickaxeSettings.windupExtra, the look only), the right hand slides down the
    ///     handle rather than reach past rightArmMax, and where the hands are further than swingArm from the arm roots
    ///     the body comes to them (<see cref="SetSwingHands"/>; during the blow, to where they will land): the hips step
    ///     toward them up to swingHipShift (the arm roots sit in front of the back bones, so a lean moves them mostly up
    ///     and down, not forward), the upper body leans up to swingLeanMax (forward and down to a low blow, back and up
    ///     under the wind-up) and the hips sink up to swingSink for a low one. Nov 3 わくさん（たたいた時、前後しすぎ）:
    ///     the first-person view does not go with any of this (the step, the lean toward the hands, the arch, the turn):
    ///     the camera stands at <see cref="ViewEyeWorld"/>, the eye as it would be without the swing, going forward only
    ///     as far as the face comes (else it would be inside the head); and the step in is only swingHipShift 5 cm.
    ///     The swing's own lean (PickaxeSettings leanBack / leanForward, through lean) still moves it.
    ///   Carried tools ride on spots of the skin (<see cref="CarrySpot"/>): LoadoutSettings' t along the body (made for the
    ///     crawling slug, 0 = tail tip, 1 = head) is read over carryT, from the hips bone (x) up the back to the chest bone
    ///     (y): the belt (hipT 0.44) comes 20 % of the way up, the back gun (backGunT 0.52) 60 %. A ray from the middle line
    ///     finds the skin at rest; the spot then rides on the nearest of the hips, spine and chest bones.
    /// Bones: spine, tail, neck and head bones have the body's axes at rest (+Z forward, +Y up); the leg bones point
    /// their +Y along the bone (so they can stretch), and are not parented to one another (each is placed in the world).
    /// </summary>
    [DefaultExecutionOrder(-10)]   // after the movement (Update), before the camera rig places the eyes (LateUpdate)
    public class DinoSlugBody : SlugBodyBase
    {
        [Header("骨（ビルダーが入れる）")]
        public Transform hips;
        public Transform spine, chest, neck, head;
        [Tooltip("尾の骨。付け根から先へ")] public Transform[] tail;
        [Tooltip("左、右の順")] public Transform[] thighs = new Transform[2], shins = new Transform[2], feet = new Transform[2];
        [Tooltip("左、右の順")] public Transform[] arms = new Transform[2];
        [Tooltip("目と目のあいだ、顔の表面。前 = +Z。一人称のカメラはここ")] public Transform eyeAnchor;

        [Header("歩き方")]
        [Tooltip("ゆっくりのときの1歩（m）")] public float strideSlow = 0.10f;
        [Tooltip("速いときの1歩（m）")] public float strideFast = 0.24f;
        [Tooltip("1歩が strideFast になる速さ（m/s）")] public float fastSpeed = 1.6f;
        [Tooltip("1歩にかける時間のいちばん短い・長い（秒）")] public float minStepTime = 0.12f, maxStepTime = 0.24f;
        [Tooltip("足を上げる高さ（m）")] public float stepLift = 0.03f;
        [Tooltip("足が伸びられる倍率（ナメクジの足なので少し伸びる）")] public float legStretchMax = 1.35f;

        [Header("体")]
        [Tooltip("1歩ごとに腰が弾む量（m、速いとき）")] public float hipBob = 0.018f;
        [Tooltip("速いときに腰を落とす量（m）。短い足が遠くまで届く")] public float hipDropAtSpeed = 0.02f;
        [Tooltip("1歩ごとに腰が立っている足の側へ傾く（度）")] public float hipRoll = 6f;
        [Tooltip("1歩ごとに腰が前へ出る足の側へひねる（度）。胸は逆へひねり返す")] public float hipTwist = 9f;
        [Tooltip("1歩ごとに上半身がうなずく（度）。足が着くたびに前へ沈む")] public float stepNod = 3f;
        [Tooltip("速いときに上半身が前へ傾く（度）")] public float leanAtSpeed = 8f;
        [Tooltip("腕を振る（度、速いとき）")] public float armSwing = 32f;
        [Tooltip("頭が体の傾きを打ち消す割合（1 = いつも水平）")] public float headLevel = 0.8f;

        [Header("走り（Nov 3、Shift 長押し。歩きの上に重ねる）")]
        [Tooltip("走りの形がいちばん大きくなる速さ（m/s）。遅くなっていても、走っていれば走りの形")] public float runSpeed = 2.4f;
        [Tooltip("走るときに足が着く間隔（秒）。2.4 m/s で 1歩 約 55 cm（人なら約 2 m の大股）")] public float runStepTime = 0.23f;
        [Range(0f, 0.8f), Tooltip("1歩のうち、両足が浮いている割合。足が短いので、地面を蹴る間は短く、残りは跳んで進む")] public float runFlight = 0.45f;
        [Tooltip("走るときに足を上げる高さ（m）。上げるのも早い")] public float stepLiftRun = 0.07f;
        [Tooltip("走るときに足が伸びられる倍率")] public float legStretchRun = 1.5f;
        [Tooltip("走るときの腰の弾み（m）。蹴った足の上で沈み、跳んでいる間に浮く")] public float hipBobRun = 0.028f;
        [Tooltip("走るときに腰を落とす量（m、歩きの分に足す）")] public float hipDropRun = 0.01f;
        [Tooltip("走るときに腰ごと前へ倒す（度）。胴は前へ、尾は上がって後ろへまっすぐ（恐竜の走り）")] public float hipPitchRun = 10f;
        [Tooltip("走るときに上半身がさらに前へ傾く（度、歩きの分に足す）。傾けるほど目が低くなる（いまは腰の倒しだけで、目は歩きより 7 cm 低い 0.33 m）")] public float leanRun = 0f;
        [Tooltip("走るときの腰のひねり（度）と、立っている足の側への傾き（度）")] public float hipTwistRun = 14f, hipRollRun = 4f;
        [Tooltip("走るときに足が着くたびのうなずき（度）")] public float stepNodRun = 5f;
        [Tooltip("走るときの腕振り（度）")] public float armSwingRun = 62f;
        [Tooltip("走るとき、腕を前へ（度）・外へ（度）構える")] public float armRunForward = 22f, armRunOut = 14f;
        [Tooltip("走るときの1歩ごとの尾の振れ（度）。まっすぐ後ろへ伸ばすので小さい")] public float tailSwayRun = 6f;
        [Tooltip("走るときに尾が上がる（度）")] public float tailLiftRun = 22f;
        [Tooltip("走るときに触角が後ろへなびく（度）")] public float antennaRunBack = 22f;
        [Tooltip("走りの形になる・戻る時間（秒）")] public float runBlendTime = 0.15f;

        [Header("息切れ（Nov 3、走りすぎて息が切れたとき）")]
        [Tooltip("肩で息をする速さ（Hz、息が切れた瞬間）。戻るにつれてゆっくりに")] public float breathHz = 1.6f;
        [Tooltip("息を吸うたびに胸が上がる量（m）")] public float breathHeave = 0.02f;
        [Tooltip("前へかがむ（度）と、頭が垂れる（度）")] public float breathHunch = 22f, breathHeadDrop = 18f;
        [Tooltip("触角が外と後ろへ垂れる（度）")] public float antennaDroop = 75f;
        [Tooltip("息切れの形が抜ける時間（秒）")] public float breathFadeTime = 0.8f;

        [Header("尾")]
        [Tooltip("向きを変えたときの尾の遅れ（度 / 回る速さ 度毎秒）")] public float tailLagPerTurn = 0.12f;
        [Tooltip("尾が遅れられる最大（度）")] public float tailMaxLag = 45f;
        [Tooltip("尾が戻るばね（Hz）と減衰（1 = 行き過ぎない）")] public float tailHz = 2.2f, tailDamping = 0.55f;
        [Tooltip("1歩ごとの尾の振れ（度）。付け根から先へ波になって伝わる")] public float tailSway = 14f;
        [Tooltip("速いときに尾が上がる（度）")] public float tailLift = 8f;

        [Header("道具（伸びる腕）")]
        [Tooltip("伸びる腕を出しているとき、短い腕を肩へ縮める大きさ（1 = そのまま）")] public float armHiddenScale = 0.15f;
        [Tooltip("短い腕を縮める・戻す時間（秒）")] public float armHideTime = 0.1f;
        [Tooltip("伸びる腕の付け根を、腕の骨（肩）から体の外へずらす（m）")] public float armRootOut = 0.025f;
        [Tooltip("道具の置き場所の t（LoadoutSettings、0 = 尾の先、1 = 頭）を、腰の骨（x）から胸の骨（y）までに当てる")] public Vector2 carryT = new Vector2(0.40f, 0.60f);

        [Header("道具を構える（Nov 3）")]
        [Range(0f, 90f), Tooltip("持っているとき、伸びる腕が生える所：胸の真ん前から横へ回した角度（度）。90 = 体の真横（短い腕の肩）、0 = 胸の真ん中")] public float armFrontAngle = 45f;
        [Range(0f, 1f), Tooltip("持っているとき、歩みで胸がひねり返すのを止める割合（1 = 肩は腰の上でまっすぐ、道具が揺れない）")] public float holdSteady = 1f;
        [Tooltip("長い銃を覗くとき、上半身を右へ回す（度）。左肩が前、右肩が後ろで、銃床が右肩の前に来る")] public float aimTwist = 28f;
        [Tooltip("拳銃を覗くとき、上半身を右へ回す（度）。両腕をまっすぐ前へ")] public float aimTwistPistol = 10f;
        [Range(0f, 1f), Tooltip("覗くときの回しのうち、腰が受け持つ割合（残りは背と胸）")] public float aimHipShare = 0.3f;
        [Tooltip("覗くとき、頭を右へ傾けて頬を銃床に寄せる（度）と、頭を下げる（度）。長い銃だけ")] public float aimCheek = 12f, aimHeadDown = 14f;
        [Range(0f, 1f), Tooltip("覗かずに肩に構えている（撃っている）ときの、覗く形の割合")] public float aimShouldered = 0.5f;
        [Tooltip("長い銃を覗くとき、照準（照門・スコープの接眼）が来る所：右目の真ん中からのずれ（頭の向き基準：右、上、前, m）。目は丸い顔のいちばん前にあるので、少し下の奥（あごの下）にして、銃が胸にくっつき、頭が銃に乗る（三人称の見た目だけ）")] public Vector3 aimEye = new Vector3(0f, -0.045f, -0.11f);
        [Tooltip("銃を胸の前で抱えるとき・拳銃を構えるとき、手を体へ寄せる（m）。銃の設定の位置から、これだけ後ろへ。胸にくっつく")] public float gunPullIn = 0.07f;
        [Tooltip("ピッケルを振りかぶったとき、胸と肩を右へ回す（度）")] public float swingTwist = 35f;
        [Tooltip("ピッケルを振りかぶったとき、さらに上半身を反らす（度。振りの反らしに足す）")] public float swingArch = 10f;
        [Tooltip("ピッケルを振るとき、腕（付け根から手まで, m）がこれより伸びそうなら、体のほうを手へ寄せる。近くを叩いても遠くを叩いても腕の長さがあまり変わらない")] public float swingArm = 0.15f;
        [Range(0f, 1f), Tooltip("腕の足りない分のうち、体で寄せる割合")] public float swingReach = 1f;
        [Tooltip("体で寄せるとき：腰を手の方へずらす（m、いちばん大きく。踏み込み）・上半身を倒す（度、いちばん大きく。手が低ければ前へ、高ければ後ろへ）・手が低いときに腰を落とす（m、いちばん大きく）")] public float swingHipShift = 0.05f, swingLeanMax = 30f, swingSink = 0.04f;
        [Tooltip("ピッケルを振り抜いたとき、胸と肩が左へ回り過ぎる（度）")] public float swingFollow = 10f;
        [Tooltip("ピッケルを振りかぶったとき、右肩を上げる（度）")] public float swingRoll = 8f;
        [Range(0f, 1f), Tooltip("振りの回しのうち、腰が受け持つ割合（無難：胸と肩が主で、腰と足はほぼそのまま）")] public float swingHipShare = 0.15f;

        [Header("外から決める形（毎フレーム）")]
        [Range(0f, 1f)] public float crouch;
        [Range(0f, 1f)] public float stretch;
        [Range(0f, 1f)] public float prone;
        [Tooltip("頭を右へ回す（度）")] public float twist;
        [Tooltip("上半身の前後の傾き（度、+ = 前）")] public float lean;
        public bool airborne;
        [Range(0f, 1f), Tooltip("走っている（1）。ここで滑らかにする")] public float run;
        [Range(0f, 1f), Tooltip("息が切れている（1）。ここで滑らかにする")] public float breathless;

        /// <summary>0..1: how far the body runs (eased in, and only while it really moves).</summary>
        public float Run01 => _runAmt;
        /// <summary>Once round per two steps: 0..pi from the right foot's lift-off to the left's, pi..2pi from the left's.</summary>
        public float GaitPhase => _gaitPhase;
        /// <summary>0..1: how strongly the steps move the body now.</summary>
        public float GaitAmount => _gaitAmp;
        /// <summary>0..1: how far out of breath (eased).</summary>
        public float Breath01 => _breath;
        /// <summary>A foot comes down at the end of its step (where): the footstep sound.</summary>
        public event System.Action<Vector3> Stepped;

        public Vector3 EyeWorld => eyeAnchor ? eyeAnchor.position : transform.position + transform.up * 0.47f;
        /// <summary>The eye for the first-person view: where it would be if the pickaxe's swing did not move the body
        /// (the step and lean toward the hands, the arch and turn), but never behind the face; everyone else sees the
        /// body move.</summary>
        public Vector3 ViewEyeWorld
        {
            get
            {
                // forward it goes along as far as the face comes, or it would be inside the head
                Vector3 ahead = Vector3.Project(_viewEyeShift, EyeForwardWorld);
                return EyeWorld - _viewEyeShift + (Vector3.Dot(ahead, EyeForwardWorld) > 0f ? ahead : Vector3.zero);
            }
        }
        public Vector3 EyeForwardWorld => eyeAnchor ? eyeAnchor.forward : transform.forward;
        public Vector3 EyeUpWorld => eyeAnchor ? eyeAnchor.up : transform.up;
        /// <summary>The eye before the head's turn on the neck (peeking looks from here).</summary>
        public Vector3 EyeUnturnedWorld => transform.TransformPoint(_eyeRestLocal);
        public Vector3 TailTipWorld => tail != null && tail.Length > 0 && tail[tail.Length - 1] ? tail[tail.Length - 1].position : transform.position;

        // rest pose
        Vector3 _hipsPos;
        Quaternion _hipsRot, _spineRot, _chestRot, _neckRot, _headRot;
        Quaternion[] _tailRot, _armRot;
        Vector3[] _ankleHome = new Vector3[2];   // ankle at rest, in this transform's space
        Quaternion[] _thighRot = new Quaternion[2], _shinRot = new Quaternion[2], _footRot = new Quaternion[2];
        float[] _len1 = new float[2], _len2 = new float[2];
        float _ankleUp;
        Vector3 _eyeRestLocal, _chestPos;
        // antennae: the meshes on the head, turned about their roots (head space)
        Transform[] _antennae = new Transform[0];
        Vector3[] _antPivot, _antPos;
        Quaternion[] _antRot;

        // feet
        readonly Vector3[] _plant = new Vector3[2], _from = new Vector3[2], _foot = new Vector3[2], _footN = { Vector3.up, Vector3.up };
        readonly bool[] _swing = new bool[2];
        readonly float[] _t = new float[2];
        bool _ready;

        // motion
        Vector3 _lastPos, _vel;
        float _lastYaw, _yawRate, _tailAngle, _tailVel, _gaitAmp, _gaitPhase, _speed;
        float _run, _runAmt, _flight, _breath, _breathPhase;
        Transform _owner;
        readonly RaycastHit[] _hits = new RaycastHit[8];

        // tools: the stretchy arms on each side, how far each short arm is drawn in (0..1), the carry bones at rest
        readonly List<GameObject>[] _stretchy = { new List<GameObject>(), new List<GameObject>() };
        readonly float[] _armIn = new float[2];
        Transform[] _carryBones;
        Matrix4x4[] _carryRestInv;
        Vector3[] _carryRestPos;
        SkinnedMeshRenderer _skin;
        bool _rest;
        // holding: where a stretchy arm grows on the front of the chest (chest space, found at rest for _frontAngle)
        readonly Vector3[] _frontRoot = new Vector3[2], _frontN = new Vector3[2], _armRestBody = new Vector3[2];
        float _frontAngle = -1f;
        // set from outside each frame by the tool in hand
        float _gunShoulder, _gunSight, _swingTurn, _swingW;
        readonly Vector3[] _swingHand = new Vector3[2], _frontRestBody = new Vector3[2];   // the hands (world), the arm roots at rest (body space)
        Vector3 _reach;                                          // how far the body brings the arm roots toward the hands (body space, eased)
        Vector3 _viewEyeShift;                                   // how far the swing moved the eye this frame (world)
        bool _gunLong = true;
        // the left eye, shut by SetWink: squashed top to bottom about its middle (head space)
        Transform _eyeL;
        Vector3 _eyeLPos, _eyeLScale, _eyeLMid;
        Vector3 _eyeRHead;                                       // the right eye's middle (head space), the one along the sights
        float _wink;

        void Awake()
        {
            _owner = GetComponentInParent<CharacterController>() ? GetComponentInParent<CharacterController>().transform : transform.root;
            _hipsPos = hips.localPosition;
            _hipsRot = hips.localRotation;
            _spineRot = spine.localRotation; _chestRot = chest.localRotation; _neckRot = neck.localRotation; _headRot = head.localRotation;
            _chestPos = chest.localPosition;
            FindAntennae();
            _tailRot = new Quaternion[tail.Length];
            for (int i = 0; i < tail.Length; i++) _tailRot[i] = tail[i].localRotation;
            _armRot = new Quaternion[2];
            for (int s = 0; s < 2; s++)
            {
                _armRot[s] = arms[s].localRotation;
                _ankleHome[s] = transform.InverseTransformPoint(feet[s].position);
                _thighRot[s] = Quaternion.Inverse(transform.rotation) * thighs[s].rotation;
                _shinRot[s] = Quaternion.Inverse(transform.rotation) * shins[s].rotation;
                _footRot[s] = Quaternion.Inverse(transform.rotation) * feet[s].rotation;
                _len1[s] = Vector3.Distance(thighs[s].position, shins[s].position);
                _len2[s] = Vector3.Distance(shins[s].position, feet[s].position);
            }
            _ankleUp = _ankleHome[0].y;
            _eyeRestLocal = transform.InverseTransformPoint(EyeWorld);
            _carryBones = new[] { hips, spine, chest };
            _carryRestInv = new Matrix4x4[_carryBones.Length];
            _carryRestPos = new Vector3[_carryBones.Length];
            for (int i = 0; i < _carryBones.Length; i++)
            {
                _carryRestInv[i] = (transform.worldToLocalMatrix * _carryBones[i].localToWorldMatrix).inverse;
                _carryRestPos[i] = transform.InverseTransformPoint(_carryBones[i].position);
            }
            _skin = GetComponentInChildren<SkinnedMeshRenderer>(true);
            _rest = true;
            for (int s = 0; s < 2; s++) _armRestBody[s] = transform.InverseTransformPoint(arms[s].position);
            FindFrontRoots();
            _eyeL = head.Find("Eye_L");
            if (_eyeL && _eyeL.TryGetComponent<MeshFilter>(out var eyeMf) && eyeMf.sharedMesh)
            {
                _eyeLPos = _eyeL.localPosition;
                _eyeLScale = _eyeL.localScale;
                _eyeLMid = eyeMf.sharedMesh.bounds.center;
            }
            else _eyeL = null;
            var eyeR = head.Find("Eye_R");
            _eyeRHead = eyeR && eyeR.TryGetComponent<Renderer>(out var eyeRr) ? head.InverseTransformPoint(eyeRr.bounds.center)
                                                                          : head.InverseTransformPoint(EyeWorld) + Vector3.right * 0.03f;
        }

        /// <summary>
        /// Where the stretchy arms grow while a tool is held: on the skin at the shoulders' height, armFrontAngle round
        /// from straight ahead of the middle line (found at rest, kept in the chest's space so it turns with it).
        /// </summary>
        void FindFrontRoots()
        {
            _frontAngle = armFrontAngle;
            float a = armFrontAngle * Mathf.Deg2Rad;
            Matrix4x4 toChest = _carryRestInv[2];                 // body space at rest -> the chest bone's space
            for (int s = 0; s < 2; s++)
            {
                float sign = s == 1 ? 1f : -1f;
                Vector3 shoulder = _armRestBody[s];
                Vector3 dir = new Vector3(Mathf.Sin(a) * sign, 0f, Mathf.Cos(a));
                if (!SkinRay(new Vector3(0f, shoulder.y, shoulder.z), dir, out var p, out var n)) { p = shoulder; n = new Vector3(sign, 0.3f, 0f).normalized; }
                _frontRoot[s] = toChest.MultiplyPoint3x4(p);
                _frontN[s] = toChest.MultiplyVector(n).normalized;
                _frontRestBody[s] = p + n * armRootOut;
            }
        }

        void OnEnable() => _ready = false;

        void LateUpdate() => Step(Time.deltaTime);

        /// <summary>One step of the body's motion (LateUpdate runs it; tools step it by hand).</summary>
        public void Step(float dt)
        {
            if (dt <= 0f) return;
            if (!_ready) Settle();

            // ---- how the body moves
            Vector3 v = (transform.position - _lastPos) / dt;
            v.y = 0f;
            if (v.magnitude > 6f) v = Vector3.zero;   // teleported
            _lastPos = transform.position;
            _vel = Vector3.Lerp(_vel, v, 1f - Mathf.Exp(-14f * dt));
            _speed = _vel.magnitude;
            float yaw = transform.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / dt;
            _lastYaw = yaw;
            _yawRate = Mathf.Lerp(_yawRate, Mathf.Clamp(yawRate, -720f, 720f), 1f - Mathf.Exp(-10f * dt));
            float fast = Mathf.Clamp01(_speed / Mathf.Max(fastSpeed, 0.01f));
            _run = Mathf.MoveTowards(_run, Mathf.Clamp01(run), dt / Mathf.Max(runBlendTime, 1e-3f));
            _runAmt = _run * Mathf.Clamp01((_speed - 0.3f) / 0.6f);       // held against a wall: no running on the spot
            _breath = Mathf.MoveTowards(_breath, Mathf.Clamp01(breathless), dt / (breathless > _breath ? 0.15f : Mathf.Max(breathFadeTime, 1e-3f)));

            Feet(dt, fast);
            Body(dt, fast);
            Legs();
        }

        /// <summary>Feet back under the hips at once (spawned, enabled, teleported).</summary>
        void Settle()
        {
            for (int s = 0; s < 2; s++)
            {
                _plant[s] = Ground(transform.TransformPoint(_ankleHome[s]), out _footN[s]);
                _foot[s] = _plant[s];
                _swing[s] = false;
            }
            _lastPos = transform.position;
            _lastYaw = transform.eulerAngles.y;
            _vel = Vector3.zero;
            _ready = true;
        }

        // ------------------------------------------------------------------ feet
        void Feet(float dt, float fast)
        {
            float stride = Mathf.Lerp(strideSlow, strideFast, fast);
            float stepTime = Mathf.Clamp(stride / Mathf.Max(_speed, 0.01f), minStepTime, maxStepTime);
            // running: a step every runStepTime (the stride grows with the speed), each foot down for only part of it and
            // the rest a flight with both feet up. Walking (no flight) a foot is down for one step and swings for one
            stepTime = Mathf.Lerp(stepTime, runStepTime, _runAmt);
            _flight = runFlight * _runAmt * Mathf.Clamp01(_speed / Mathf.Max(runSpeed, 0.01f));
            float stance = stepTime * (1f - _flight), swingTime = stepTime * (1f + _flight);
            // a step lands half a stance ahead of the hip, so the planted foot passes under it halfway through
            Vector3 lead = _vel * (stance * 0.5f);
            float threshold = Mathf.Max(_speed * stance, 0.025f);
            float lift = Mathf.Lerp(stepLift * Mathf.Lerp(0.6f, 1f, fast), stepLiftRun, _runAmt);
            bool moving = _speed > 0.04f || Mathf.Abs(_yawRate) > 25f;

            var want = new Vector3[2];
            var wantN = new Vector3[2];
            float[] off = new float[2];
            for (int s = 0; s < 2; s++)
            {
                Vector3 home = transform.TransformPoint(_ankleHome[s] - Vector3.up * _ankleUp);
                if (airborne)
                {
                    // tucked under, trailing a little
                    want[s] = transform.TransformPoint(_ankleHome[s] - Vector3.up * _ankleUp + new Vector3(0f, 0.03f, -0.03f - 0.03f * stretch));
                    wantN[s] = transform.up;
                }
                else want[s] = Ground(home + lead, out wantN[s]);
                Vector3 d = want[s] - _plant[s];
                d.y = 0f;
                off[s] = d.magnitude;
            }

            for (int s = 0; s < 2; s++)
            {
                if (airborne)
                {
                    _swing[s] = false;
                    _foot[s] = Vector3.Lerp(_foot[s], want[s], 1f - Mathf.Exp(-20f * dt));
                    _plant[s] = _foot[s];
                    _footN[s] = wantN[s];
                    continue;
                }
                if (_swing[s])
                {
                    _t[s] += dt / swingTime;
                    float u = Mathf.Clamp01(_t[s]);
                    // running, the foot leaves the back sooner (it does not trail behind, stretched)
                    float e = Mathf.Lerp(u * u * (3f - 2f * u), Mathf.Sin(u * Mathf.PI * 0.5f), _runAmt * 0.7f);
                    // running, the foot comes up sooner (kicked up behind) and reaches forward late
                    float arc = Mathf.Sin(Mathf.PI * Mathf.Pow(u, Mathf.Lerp(1f, 0.65f, _runAmt)));
                    _foot[s] = Vector3.Lerp(_from[s], want[s], e) + Vector3.up * (arc * lift);
                    _footN[s] = Vector3.Slerp(_footN[s], wantN[s], e);
                    if (u >= 1f) { _swing[s] = false; _plant[s] = want[s]; _foot[s] = want[s]; Stepped?.Invoke(want[s]); }
                    continue;
                }
                _foot[s] = _plant[s];
                int o = 1 - s;
                // running, a foot pushes off while the other is still in the air (late in its swing): the flight
                bool otherDown = !_swing[o] || (_flight > 0.01f && _t[o] >= 1f / (1f + _flight) - 0.03f);
                bool needed = moving ? off[s] > threshold : off[s] > 0.012f;   // stopped: settle back under the hips
                bool first = off[s] >= off[o] || _swing[o] || off[o] <= (moving ? threshold : 0.012f);
                if ((needed && otherDown && first) || off[s] > threshold * 2.2f + 0.04f)
                {
                    _swing[s] = true;
                    _t[s] = 0f;
                    _from[s] = _plant[s];
                }
            }

            // a phase that runs once round per two steps: 0..pi from the right foot's lift-off to the left's, pi..2pi from
            // the left's (walking: while the right foot swings, then the left). It follows the foot that lifted last
            float target = 0f;
            int last = _swing[0] && _swing[1] ? (_t[0] < _t[1] ? 0 : 1) : _swing[1] ? 1 : _swing[0] ? 0 : -1;
            if (last >= 0)
            {
                float half = Mathf.Clamp01(_t[last] * (1f + _flight));
                _gaitPhase = Mathf.PI * (last == 1 ? half : 1f + half);
                target = 1f;
            }
            _gaitAmp = Mathf.Lerp(_gaitAmp, target * Mathf.Max(fast, 0.35f), 1f - Mathf.Exp(-8f * dt));
        }

        /// <summary>The ground under a point (the body's own colliders left out), and its normal.</summary>
        Vector3 Ground(Vector3 p, out Vector3 normal)
        {
            Vector3 up = Vector3.up;
            int n = Physics.RaycastNonAlloc(p + up * 0.3f, -up, _hits, 0.6f, Physics.DefaultRaycastLayers & ~(1 << 2), QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 at = p;
            normal = transform.up;
            for (int i = 0; i < n; i++)
            {
                var c = _hits[i].collider;
                if (_owner && c.transform.IsChildOf(_owner)) continue;
                if (c.gameObject.layer == SlugMove.SlugHitboxes.Layer) continue;
                if (_hits[i].distance < best) { best = _hits[i].distance; at = _hits[i].point; normal = _hits[i].normal; }
            }
            return at;
        }

        // ------------------------------------------------------------------ body
        void Body(float dt, float fast)
        {
            float r = _runAmt;
            float sway = Mathf.Sin(_gaitPhase) * _gaitAmp;             // + while the right foot swings
            // walking, a bounce: up mid-swing, dropping sharply onto each landing foot. Running, the hips sink over the foot
            // pushing off (mid-stance) and rise in the flight, which is round the other foot's lift-off (phase 0 and pi)
            float walkBob = Mathf.Pow(Mathf.Abs(Mathf.Sin(_gaitPhase)), 0.6f);
            float runBob = 0.5f + 0.5f * Mathf.Cos(2f * (_gaitPhase - Mathf.PI * _flight * 0.5f));
            float bob = Mathf.Lerp(walkBob * hipBob, runBob * hipBobRun, r) * _gaitAmp;
            // a nod forward just after each landing (the body's weight coming down on the foot; running, it lands late)
            float nod = Mathf.Max(0f, Mathf.Cos(2f * (_gaitPhase - Mathf.PI * _flight))) * _gaitAmp * Mathf.Lerp(stepNod, stepNodRun, r);
            float crouchE = crouch * crouch * (3f - 2f * crouch);
            float proneE = prone * prone * (3f - 2f * prone);
            float twistAmt = Mathf.Lerp(hipTwist, hipTwistRun, r), rollAmt = Mathf.Lerp(hipRoll, hipRollRun, r);

            // out of breath: heaving, faster at first and slowing as the breath comes back
            float heave = 0f;
            if (_breath > 0f)
            {
                _breathPhase += dt * 2f * Mathf.PI * Mathf.Lerp(breathHz * 0.55f, breathHz, _breath);
                heave = Mathf.Sin(_breathPhase) * _breath;              // + breathing in
            }
            else _breathPhase = 0f;

            // a tool in hand: how far the stretchy arms are out (they grow from the front of the chest, and the chest
            // stops twisting back with the steps), the gun's stance and the pickaxe's swing
            for (int s = 0; s < 2; s++)
                _armIn[s] = Mathf.MoveTowards(_armIn[s], StretchyShown(s) ? 1f : 0f, dt / Mathf.Max(armHideTime, 1e-3f));
            if (!Mathf.Approximately(_frontAngle, armFrontAngle)) FindFrontRoots();
            float hold = Mathf.Max(ArmOut(0), ArmOut(1)) * holdSteady;
            float aim = Mathf.Max(_gunShoulder * aimShouldered, _gunSight);
            float aimTurn = aim * (_gunLong ? aimTwist : aimTwistPistol);
            float swingYaw = _swingTurn >= 0f ? _swingTurn * swingTwist : _swingTurn * swingFollow;
            float upperRoll = _swingTurn * swingRoll;               // wound up, the right shoulder rises
            float cheek = _gunLong ? aim : 0f;
            // swinging the pickaxe: where the arms would have to stretch past swingArm (measured from the arm roots at
            // rest, so the lean does not feed back), the body comes to the hands instead: the hips shift toward them,
            // the upper body leans (forward to a far blow, back under the wind-up) and sinks for a low one
            Vector3 reachWant = Vector3.zero;
            if (_swingW > 0f)
            {
                float most = 0f;
                for (int s = 0; s < 2; s++)
                {
                    Vector3 d = transform.InverseTransformPoint(_swingHand[s]) - _frontRestBody[s];
                    float over = d.magnitude - swingArm;
                    if (over > most) { most = over; reachWant = d.normalized * over; }
                }
                reachWant *= swingReach * _swingW;
            }
            _reach = Vector3.Lerp(_reach, reachWant, 1f - Mathf.Exp(-25f * dt));
            // the arm roots sit out in front of the back bones, so leaning moves them mostly up and down: the hips step
            // the body toward the hands (a lunge), the lean takes them up or down (forward and down to a low blow, back
            // and up under an overhead wind-up), sinking the hips helps a low one
            Vector3 hipShift = Vector3.ClampMagnitude(new Vector3(_reach.x, 0f, _reach.z), swingHipShift);
            float reachSink = Mathf.Clamp(-_reach.y * 0.5f, 0f, swingSink);
            float reachLean = Mathf.Clamp(Mathf.Atan2(-(_reach.y + reachSink), 0.17f) * Mathf.Rad2Deg, -swingLeanMax, swingLeanMax);
            if (hips.parent) hipShift = hips.parent.InverseTransformVector(transform.TransformVector(hipShift));

            // the trunk and head, with (k = 1) or without (k = 0) what the pickaxe's swing does to them: the step and lean
            // toward the hands, the arch and the turn
            void Trunk(float k)
            {
                float swingTurnK = swingYaw * k, hipTurnK = aimTurn * aimHipShare + swingTurnK * swingHipShare;
                float upperTurnK = aimTurn + swingTurnK - hipTurnK, upperRollK = upperRoll * k;   // the back and the chest take the rest of the turn
                hips.localPosition = _hipsPos + hipShift * k + Vector3.up * (bob - fast * hipDropAtSpeed - r * hipDropRun - crouchE * 0.04f - proneE * 0.06f - _breath * 0.012f - reachSink * k);
                // roll over the standing foot: the left one stands while the right swings
                // ... and twist toward the leg going forward (the right one while it swings)
                // running, the hips tip forward: the body goes forward and the tail comes up straight behind
                hips.localRotation = _hipsRot * Quaternion.Euler(hipPitchRun * r, sway * twistAmt + hipTurnK, sway * rollAmt);

                float forward = leanAtSpeed * fast + leanRun * r + lean + crouchE * 18f + stretch * 35f + proneE * 45f + nod
                                + _breath * breathHunch - heave * 3f + (reachLean - swingArch * Mathf.Max(_swingTurn, 0f)) * k;
                // the chest twists back against the hips, so the shoulders go with the swinging arms (holding a tool, only as
                // far as keeps the shoulders square: the tool rides steady); out of breath it rises and falls with each breath
                // (the shoulders heave: the arms hang from it)
                float spineBack = Mathf.Lerp(0.7f, 0.5f, hold), chestBack = Mathf.Lerp(0.6f, 0.5f, hold);
                spine.localRotation = _spineRot * Quaternion.Euler(forward * 0.4f, -sway * twistAmt * spineBack + upperTurnK * 0.45f, -sway * rollAmt * 0.5f + upperRollK * 0.4f);
                chest.localRotation = _chestRot * Quaternion.Euler(forward * 0.6f, -sway * twistAmt * chestBack + upperTurnK * 0.55f, -sway * rollAmt * 0.3f + upperRollK * 0.6f);
                chest.localPosition = _chestPos + Vector3.up * (heave * breathHeave);
                // head: level against the lean and the roll; turned round on the neck to look back; hanging when out of breath;
                // facing the aim or the blow whichever way the upper body turns, leaning down onto the stock
                float level = -forward * headLevel;
                float faceBack = -(aimTurn + swingTurnK) + sway * twistAmt * 0.3f * (1f - hold);
                neck.localRotation = _neckRot * Quaternion.Euler(level * 0.4f + _breath * breathHeadDrop * 0.4f + cheek * aimHeadDown * 0.4f, twist * 0.4f + faceBack * 0.4f, 0f);
                head.localRotation = _headRot * Quaternion.Euler(level * 0.6f + _breath * breathHeadDrop * 0.6f + cheek * aimHeadDown * 0.6f, twist * 0.6f + faceBack * 0.6f,
                                                                 sway * rollAmt * 0.2f * headLevel - upperRollK * headLevel - cheek * aimCheek);
            }
            // (Nov 3 わくさん：たたいた時、前後しすぎ) the first-person eyes do not go with the swing's body: the eye is
            // found once without it, and the view is put back by how far the swing moved it (ViewEyeWorld)
            bool swinging = _swingW > 0f || _swingTurn != 0f || _reach.sqrMagnitude > 1e-8f;
            Vector3 eyeStill = Vector3.zero;
            if (swinging) { Trunk(0f); eyeStill = EyeWorld; }
            Trunk(1f);
            _viewEyeShift = swinging ? EyeWorld - eyeStill : Vector3.zero;

            // arms against the legs: the right arm back while the right foot goes forward (running, further, held a little
            // forward and out; out of breath, dangling forward). A side whose stretchy arm is out (a tool in hand) draws its
            // short arm into the shoulder and holds it still
            float swingAmt = Mathf.Lerp(armSwing, armSwingRun, r);
            for (int s = 0; s < 2; s++)
            {
                float inE = ArmOut(s);
                float sign = s == 1 ? 1f : -1f;
                float free = 1f - inE;
                float swing = swingAmt * sway * sign - stretch * 30f - armRunForward * r - _breath * 18f;
                float outward = sign * (armRunOut * r + heave * 4f);
                arms[s].localRotation = _armRot[s] * Quaternion.Euler(swing * free, 0f, outward * free);
                arms[s].localScale = Vector3.one * Mathf.Lerp(1f, armHiddenScale, inE);
            }

            // tail: lags a turn on a spring, sways against the hips, lifts with speed (and in the air)
            float lagTarget = Mathf.Clamp(-_yawRate * tailLagPerTurn, -tailMaxLag, tailMaxLag);
            float w = 2f * Mathf.PI * tailHz;
            _tailVel += (w * w * (lagTarget - _tailAngle) - 2f * tailDamping * w * _tailVel) * dt;
            _tailAngle += _tailVel * dt;
            // the sway runs down the tail as a wave (each piece a little later than the one before), and the tail whips up
            // and down a little with the bounce. Running, it is held up and straighter behind
            int n = Mathf.Max(tail.Length, 1);
            float lift = -(tailLift * fast + tailLiftRun * r + stretch * 12f) / n;
            float swayAmt = Mathf.Lerp(tailSway, tailSwayRun, r);
            for (int i = 0; i < tail.Length; i++)
            {
                float late = _gaitPhase - (i + 1) * 0.55f;
                float yawEach = (_tailAngle - Mathf.Sin(late) * _gaitAmp * swayAmt) / n;
                float whip = Mathf.Cos(2f * late) * _gaitAmp * 1.5f;
                tail[i].localRotation = _tailRot[i] * Quaternion.Euler(lift + whip + proneE * 4f / n, yawEach, 0f);
            }

            // antennae: swept back by the run, drooping out and back when out of breath (bobbing with each breath)
            float back = antennaRunBack * r + antennaDroop * 0.4f * _breath + heave * 4f;
            float droop = antennaDroop * _breath + heave * 5f;
            for (int i = 0; i < _antennae.Length; i++)
            {
                float side = _antPivot[i].x < 0f ? 1f : -1f;           // the left one tips out to -x
                Quaternion turn = Quaternion.Euler(-back, 0f, droop * side);
                _antennae[i].localRotation = turn * _antRot[i];
                _antennae[i].localPosition = _antPivot[i] + turn * (_antPos[i] - _antPivot[i]);
            }
        }

        /// <summary>The antennae on the head (meshes named Antenna*), and the root each turns about: the bottom of its stalk.</summary>
        void FindAntennae()
        {
            var found = new List<Transform>();
            foreach (Transform c in head)
                if (c.name.StartsWith("Antenna") && c.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh && mf.sharedMesh.isReadable)
                    found.Add(c);
            _antennae = found.ToArray();
            _antPivot = new Vector3[_antennae.Length];
            _antPos = new Vector3[_antennae.Length];
            _antRot = new Quaternion[_antennae.Length];
            for (int i = 0; i < _antennae.Length; i++)
            {
                var t = _antennae[i];
                var v = t.GetComponent<MeshFilter>().sharedMesh.vertices;
                float minY = float.MaxValue;
                foreach (var p in v) minY = Mathf.Min(minY, p.y);
                Vector3 sum = Vector3.zero;
                int k = 0;
                foreach (var p in v) if (p.y < minY + 0.006f) { sum += p; k++; }
                Vector3 root = sum / Mathf.Max(k, 1);
                _antPos[i] = t.localPosition;
                _antRot[i] = t.localRotation;
                _antPivot[i] = t.localPosition + t.localRotation * Vector3.Scale(t.localScale, root);
            }
        }

        // ------------------------------------------------------------------ legs
        void Legs()
        {
            Quaternion bodyRot = transform.rotation;
            Vector3 fwd = transform.forward;
            for (int s = 0; s < 2; s++)
            {
                Vector3 hip = thighs[s].position;
                Vector3 ankle = _foot[s] + _footN[s] * _ankleUp;
                float l1 = _len1[s], l2 = _len2[s];
                Vector3 to = ankle - hip;
                float d = Mathf.Max(to.magnitude, 1e-4f);
                float k = Mathf.Clamp(d / ((l1 + l2) * 0.985f), 1f, Mathf.Lerp(legStretchMax, legStretchRun, _runAmt));
                l1 *= k; l2 *= k;
                Vector3 dir = to / d;
                d = Mathf.Min(d, (l1 + l2) * 0.999f);
                ankle = hip + dir * d;
                float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
                float h = Mathf.Sqrt(Mathf.Max(l1 * l1 - a * a, 0f));
                Vector3 pole = Vector3.ProjectOnPlane(fwd, dir);
                pole = pole.sqrMagnitude > 1e-6f ? pole.normalized : fwd;
                Vector3 knee = hip + dir * a + pole * h;

                Place(thighs[s], hip, knee, bodyRot * _thighRot[s], k);
                Place(shins[s], knee, ankle, bodyRot * _shinRot[s], k);
                // foot: flat on the ground it stands on, pointing the way the body faces (toes up a little mid-swing)
                Vector3 n = _footN[s];
                Vector3 f = Vector3.ProjectOnPlane(fwd, n);
                if (f.sqrMagnitude < 1e-6f) f = fwd;
                Quaternion flat = Quaternion.LookRotation(f.normalized, n);
                float toe = _swing[s] ? -20f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(_t[s])) : 0f;
                feet[s].SetPositionAndRotation(ankle, flat * Quaternion.Euler(toe, 0f, 0f) * _footRot[s]);
            }
        }

        // ------------------------------------------------------------------ tools (SlugBodyBase)
        public override Vector3 FaceCenterWorld => EyeWorld;

        /// <summary>
        /// Where a stretchy arm grows: from the shoulder (the arm bone, a little out along the side), moving to the front
        /// of the chest (armFrontAngle round from straight ahead) as the arm comes out for a tool. It turns with the chest.
        /// </summary>
        public override Vector3 HandRootWorld(float side)
        {
            int s = side < 0f ? 0 : 1;
            Vector3 shoulder = arms[s].position + SideNormal(s) * armRootOut;
            Vector3 front = chest.TransformPoint(_frontRoot[s]) + chest.TransformDirection(_frontN[s]) * armRootOut;
            return Vector3.Lerp(shoulder, front, ArmOut(s));
        }

        /// <summary>Out of the skin where the arm grows: out of the side at the shoulder, a little up, turning to the front.</summary>
        public override Vector3 HandRootNormalWorld(float side)
        {
            int s = side < 0f ? 0 : 1;
            return Vector3.Slerp(SideNormal(s), chest.TransformDirection(_frontN[s]), ArmOut(s)).normalized;
        }

        Vector3 SideNormal(int s)
        {
            var frame = arms[s].parent ? arms[s].parent : transform;
            return frame.TransformDirection(new Vector3(s == 1 ? 1f : -1f, 0.3f, 0f).normalized);
        }

        /// <summary>0..1, eased: how far the stretchy arm on side s is out (a tool in hand).</summary>
        float ArmOut(int s) => _armIn[s] * _armIn[s] * (3f - 2f * _armIn[s]);

        public override void SetGunStance(float shouldered, float sighting, bool longGun)
        {
            _gunShoulder = Mathf.Clamp01(shouldered);
            _gunSight = Mathf.Clamp01(sighting);
            _gunLong = longGun;
        }

        public override void SetSwingTurn(float wound) => _swingTurn = Mathf.Clamp(wound, -1f, 1f);

        public override Vector3 GunHoldShift => new Vector3(0f, 0f, -gunPullIn);

        public override void SetSwingHands(Vector3 left, Vector3 right, float weight)
        {
            _swingHand[0] = left;
            _swingHand[1] = right;
            _swingW = Mathf.Clamp01(weight);
        }

        /// <summary>Along the sights: a little off the right eye (aimEye), so the gun comes up under it, the cheek on the stock.</summary>
        public override bool SightEyeWorld(out Vector3 eye)
        {
            eye = head.TransformPoint(_eyeRHead + aimEye);
            return true;
        }

        /// <summary>Shuts the left eye (0 open .. 1 shut): squashed top to bottom about its middle into a short line.</summary>
        public override void SetWink(float amount)
        {
            if (!_eyeL || Mathf.Abs(amount - _wink) < 0.005f && (amount > 0f || _wink == 0f)) return;
            _wink = Mathf.Clamp01(amount);
            float k = Mathf.Lerp(1f, 0.12f, _wink * _wink * (3f - 2f * _wink));
            Vector3 scale = new Vector3(_eyeLScale.x, _eyeLScale.y * k, _eyeLScale.z);
            _eyeL.localScale = scale;
            _eyeL.localPosition = _eyeLPos + _eyeL.localRotation * (Vector3.Scale(_eyeLScale, _eyeLMid) - Vector3.Scale(scale, _eyeLMid));
        }

        public override Material HandMaterial => _skin ? _skin.sharedMaterial : null;

        // dryness: the skin's and the antennae's own copies of their materials (made on the first change), their colours as built
        readonly Dictionary<Renderer, Color> _wetColor = new Dictionary<Renderer, Color>();
        float _dry, _soak;

        /// <summary>How dry the skin looks (as the crawling slug's: dull, greyed and darker when dry, a little sheen when soaked).</summary>
        public override void SetDryness(float dry, float soaked = 0f)
        {
            dry = Mathf.Clamp01(dry);
            soaked = Mathf.Clamp01(soaked);
            if (!Application.isPlaying || Mathf.Abs(dry - _dry) < 0.004f && Mathf.Abs(soaked - _soak) < 0.004f) return;
            _dry = dry;
            _soak = soaked;
            if (_wetColor.Count == 0)
            {
                if (_skin) _wetColor[_skin] = default;
                foreach (var a in _antennae) if (a && a.TryGetComponent<Renderer>(out var ar)) _wetColor[ar] = default;
                foreach (var r in new List<Renderer>(_wetColor.Keys))
                    _wetColor[r] = r.material.HasProperty("_BaseColor") ? r.material.GetColor("_BaseColor") : Color.white;
            }
            foreach (var kv in _wetColor)
            {
                if (!kv.Key) continue;
                Color c = kv.Value;
                float g = c.grayscale;
                Color dull = Color.Lerp(c, new Color(g, g, g * 0.95f), 0.6f) * 0.78f;
                Color result = Color.Lerp(c * (1f + 0.12f * soaked), dull, dry);
                result.a = c.a;
                kv.Key.material.SetColor("_BaseColor", result);
            }
        }

        public override void UseStretchyArm(float side, GameObject arm)
        {
            var list = _stretchy[side < 0f ? 0 : 1];
            if (arm && !list.Contains(arm)) list.Add(arm);
        }

        bool StretchyShown(int s)
        {
            var list = _stretchy[s];
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i]) { list.RemoveAt(i); continue; }
                if (list[i].activeInHierarchy) return true;
            }
            return false;
        }

        public override object ShapeKey => _rest ? this : null;

        /// <summary>
        /// The skin at t (over <see cref="carryT"/>: hips bone -> chest bone, along the middle line) and angleDeg round the
        /// body (0 = the middle of the back, which faces up and back on the leaning body, 90 = the right side), at rest.
        /// The <paramref name="tHit"/> given back is t: it picks the bone the spot rides on.
        /// </summary>
        public override bool CarrySpot(float t, float angleDeg, out Vector3 p, out Vector3 n, out Vector3 along, out Vector3 right,
                                       out Vector3 belly, out float tHit)
        {
            tHit = t;
            if (!_rest) { p = n = along = right = belly = Vector3.zero; return false; }
            float u = Mathf.Clamp01(Mathf.InverseLerp(carryT.x, carryT.y, t)) * (_carryRestPos.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(u), _carryRestPos.Length - 2);
            Vector3 center = Vector3.Lerp(_carryRestPos[i], _carryRestPos[i + 1], u - i);
            Vector3 line = (_carryRestPos[i + 1] - _carryRestPos[i]).normalized;
            Vector3 back = Vector3.Cross(line, Vector3.right).normalized;     // up and back on the leaning body
            float a = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = (back * Mathf.Cos(a) + Vector3.right * Mathf.Sin(a)).normalized;
            if (!SkinRay(center, dir, out p, out n)) { p = center + dir * 0.1f; n = dir; }
            along = Vector3.ProjectOnPlane(line, n).normalized;
            right = Vector3.Cross(along, n).normalized;
            if (right.x < 0f) right = -right;
            belly = -back;
            return true;
        }

        /// <summary>A body-space point (at rest) carried by the bone that t picks (see <see cref="CarrySpot"/>).</summary>
        public override Vector3 FollowPointWorld(Vector3 pointBody, float t, Vector3 normalBody)
        {
            if (!_rest) return transform.TransformPoint(pointBody);
            int b = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(Mathf.InverseLerp(carryT.x, carryT.y, t)) * (_carryBones.Length - 1)), 0, _carryBones.Length - 1);
            return _carryBones[b].localToWorldMatrix.MultiplyPoint3x4(_carryRestInv[b].MultiplyPoint3x4(pointBody));
        }

        // the skin at rest (body space), for finding carry spots
        Vector3[] _skinV;
        int[] _skinT;
        // answers already found: the skin at rest does not change, and a ray against all of it costs ~7 ms (Nov 4: the tool
        // rack asked again every 0.25 s for every slug, which alone took the island down to a few frames a second)
        readonly Dictionary<(Vector3, Vector3), (bool, Vector3, Vector3)> _skinRays = new Dictionary<(Vector3, Vector3), (bool, Vector3, Vector3)>();

        /// <summary>The first place a ray from inside the body leaves the skin at rest, and the face's normal there.</summary>
        bool SkinRay(Vector3 from, Vector3 dir, out Vector3 hit, out Vector3 normal)
        {
            if (_skinRays.TryGetValue((from, dir), out var known))
            {
                hit = known.Item2;
                normal = known.Item3;
                return known.Item1;
            }
            bool found = SkinRayAll(from, dir, out hit, out normal);
            if (_skinRays.Count > 256) _skinRays.Clear();
            _skinRays[(from, dir)] = (found, hit, normal);
            return found;
        }

        bool SkinRayAll(Vector3 from, Vector3 dir, out Vector3 hit, out Vector3 normal)
        {
            hit = normal = Vector3.zero;
            if (!_skin || !_skin.sharedMesh) return false;
            if (_skinV == null)
            {
                var m = _skin.sharedMesh;
                if (!m.isReadable) return false;
                var local = transform.worldToLocalMatrix * _skin.transform.localToWorldMatrix;
                _skinV = m.vertices;
                for (int i = 0; i < _skinV.Length; i++) _skinV[i] = local.MultiplyPoint3x4(_skinV[i]);
                _skinT = m.triangles;
            }
            float best = float.MaxValue;
            for (int i = 0; i < _skinT.Length; i += 3)
            {
                Vector3 a = _skinV[_skinT[i]], b = _skinV[_skinT[i + 1]], c = _skinV[_skinT[i + 2]];
                // ray against the triangle (Moller-Trumbore)
                Vector3 e1 = b - a, e2 = c - a, h = Vector3.Cross(dir, e2);
                float det = Vector3.Dot(e1, h);
                if (Mathf.Abs(det) < 1e-12f) continue;
                float f = 1f / det;
                Vector3 s = from - a;
                float bu = f * Vector3.Dot(s, h);
                if (bu < 0f || bu > 1f) continue;
                Vector3 q = Vector3.Cross(s, e1);
                float bv = f * Vector3.Dot(dir, q);
                if (bv < 0f || bu + bv > 1f) continue;
                float d = f * Vector3.Dot(e2, q);
                if (d <= 1e-5f || d >= best) continue;
                best = d;
                normal = Vector3.Cross(e1, e2).normalized;
            }
            if (best == float.MaxValue) return false;
            hit = from + dir * best;
            if (Vector3.Dot(normal, dir) < 0f) normal = -normal;
            return true;
        }

        /// <summary>A leg bone from a to b: its +Y along the bone, its rest twist kept, stretched by k along +Y.</summary>
        static void Place(Transform bone, Vector3 a, Vector3 b, Quaternion restWorld, float k)
        {
            Vector3 dir = (b - a).normalized;
            Vector3 restDir = restWorld * Vector3.up;
            bone.SetPositionAndRotation(a, Quaternion.FromToRotation(restDir, dir) * restWorld);
            bone.localScale = new Vector3(1f, k, 1f);
        }
    }
}
