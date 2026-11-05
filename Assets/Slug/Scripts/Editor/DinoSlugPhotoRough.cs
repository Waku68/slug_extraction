using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SlugDesign
{
    /// <summary>
    /// 足のあるナメクジ（dino slug）の灰色ラフ。Nov 1、わくさんの写真（緑のナメクジが地図を持って歩く絵）を
    /// そのまま写す。DinoSlugRough（今のナメクジの部品を写した版）とは別。
    ///   寸法は写真から測った：写真の 1 px = K m（目の高さ 0.47 m ＝ 写真の足元から目まで 295 px）。
    ///   部品の位置は写真のピクセル（P(x, y, 横)）のまま書いてあるので、写真と見比べて直せる。
    ///   体：丸い頭（あごの下は内へ切れこみ、のどはほぼまっすぐ下へ）＋ 前へ傾いた胴（背中は頭の後ろから尾の上まで
    ///       まっすぐなだらか）＋ 横へまっすぐ伸びて先がとがる長い尾（地面から少し浮く）。横幅は最初の 0.75（Nov 2 に下のとおり変えた）。
    ///       足・腕・触角は左右対称（写真の手前＝右の形を左にも映す）。足は短い太い2本で関節あり（ひざ・足首・前へ出た足先）。
    ///       腕は短く細め（Nov 2 から体の横に下ろす）。目は小さな黒い長丸を顔の横寄りに2つ、口は見えない。触角2本の先に玉（写真の 3/4 の大きさ）。
    ///   持ち物（かばん・寝袋・肩ひも・地図）は作らない（Nov 1 わくさん）。かばんや地図で隠れている背中とおなかの前は、
    ///   体が自然に見えるようにつないだ。
    ///   Nov 2：正面の写真（2本足で立って正面を向く絵）に合わせた。横幅と横の位置は正面の写真、高さと前後の形は横の写真が正
    ///   （わくさん）。正面の写真は 1 px = KF m（足元 py 846 から目 py 460 まで 386 px ＝ 0.47 m、真ん中 px 836）。
    ///     頭：横幅 0.75 → 1.0（横から見た奥行きと同じ、まんまる。幅 0.247 m ＝ 人なら 0.89 m）
    ///     胴と尾：横幅 0.75 → 0.85（おなかの幅 0.27 m ＝ 人なら 0.97 m）。首は細くするのをやめ、少し太く（×0.88 → ×1.15）、
    ///       頭から洋なし形の胴へそのまま続く
    ///     足：真ん中から 0.052 → 0.100 m（写真 82 px）。足先の横幅 0.056 → 0.070 m、もも 0.034 → 0.038 m
    ///     腕：前へ突き出す（地図を持つ形）→ 体の横に下ろす。肩 0.33 m・手 0.22 m の高さ、手は真ん中から 0.14 m、
    ///       少し前（z 0.09〜0.13）。つなぎのなじみ 0.02 → 0.01（腕のきわに筋が出て、こぶでなく腕に見える）
    ///     目：真ん中から 0.046 → 0.073 m（写真 60 px）、大きさ 20×28 → 22×35 mm（正面から見た大きさ）
    ///     触角：根元を真ん中から 0.026 → 0.056 m、外へ 22° 開いて V 字（玉は真ん中から約 0.11 m）。長さと玉は今のまま（3/4）
    ///     尾はまっすぐ後ろのまま（わくさん。曲げるのは後で動きとして）。
    ///   Nov 2 2回目（わくさん：肩は肩当てに沿ってなめらか、ひざで曲がる、顔は濃淡で輪郭を取る）：
    ///     肩：腕の付け根を 0.33 → 0.31 m に下げて内へ（真ん中から 0.122 → 0.114 m）、太さ R20 → R18。上のほうは広くなじませ
    ///       （0.035）、下は筋を残す（0.008）。首から腕へなで肩の斜面が続く。頭と胴のつなぎも横は広く（横から見て正面だけあごの角）
    ///     足：もも（腰 (914, 738) → ひざ (934, 764)、太さ R23）→ ひざ R15 → すね（→ 足首 (920, 786)）。ひざが前へ 2 cm ほど出て
    ///       曲がる。ももは自分の丸み（なじみ 0.05 → 0.03）、ひざにくびれ（0.006）。正面からはももがふくらみ、ひざで締まる
    ///     あご：濃淡で取った線に合わせた（Screenshots/Character/dino_face_shading.png）。横はあごの下の線を (1072, 563)→(1037, 578)
    ///       から (1080, 551)→(1040, 565) へ約 1.5 cm 上げた。正面はあごの線が真ん中で低く横で上がる笑い口の形（ChinRise）
    ///       正面の写真で目と目のあいだの下に、ほんのり明るい鼻先のふくらみが見える気がするが、弱いので入れていない
    ///   Nov 2 3回目（わくさん：正面の肩と胴のずんぐりが直っていない、横の輪郭は尾の先から足の付け根・後頭部までなめらか）：
    ///     胴の横幅を高さごとに正面の写真へ合わせた（BodyWidthKeys、首の ×1.15 はやめた）。0.38 m 0.110・0.35 m 0.124・
    ///       0.32 m 0.139・0.18 m 0.134（真ん中からの m）で、どれも写真と 4 mm 以内。頭と胴のつなぎは横で 0.06 → 0.03
    ///     背中を横へ丸く落とした（BackRound 0.187 → 0.10）。目の高さから見ても肩が平らな台に見えない（なで肩）
    ///     腕：付け根を真ん中から 0.127 m・0.29 m の高さへ下げ、手は 0.136 m。ほぼまっすぐ下がる。太さ R18 → R16
    ///     横の輪郭：上の線（TopKeys：尾の先 → 尾の付け根 (700, 677)・(750, 655) → かばんの下でだんだん急に → 後頭部
    ///       (925, 522)）と下の線（BottomKeys：尾の先 → 足の後ろ (880, 765)）の2本のなめらかな線にした。尾はこの2本の
    ///       あいだの丸い輪切りを並べ、背中もこの上の線で切る（Nov 1 のまっすぐな背中の線と尾の太さの表は使わない）。
    ///       背中の真ん中は Nov 1 より 2〜4 cm 低い（尾の付け根から後頭部へ、ゆるい弧）
    ///     もも R23 → R26（正面からのふくらみを写真に）
    ///   Nov 2 4回目（わくさん：いま動かせるナメクジの模様を貼る）：ゲームのナメクジ（Slug_Pop、模様 0.5・丸い体型・
    ///     足の裏 0.75）の体と触角の材質を写して貼った（DinoPhoto_Skin.mat・DinoPhoto_SkinAntenna.mat）。動き（這う波・
    ///     伸び縮み・足のひだ・曲げ）と顔の絵は切ってある。模様の座標：まわり方向は背中 0 → おなか 0.5（左右同じ）、
    ///     長さ方向は尾の先 0 → 頭 1（SpineKeys の線に沿って）。絵は Screenshots/Character/dino_skin_*.png
    ///   Nov 2 5回目（わくさん：背中がまだなだらかでない）：尾・胴・頭のつなぎが重なって、かばんの下（写真 px 780〜860）の
    ///     背中が上の線より 2〜3 cm（人なら 7〜11 cm）盛り上がり、こぶになっていた。つないだあとの背中を、尾の付け根（650〜720 で
    ///     効き始め）から首の手前（870〜910 で切れる）まで、もう一度同じ上の線で丸く切る（なじみ 0.02）。背中の真ん中は上の線
    ///     どおり、傾きは尾の付け根から後頭部へだんだん急になるだけ。首と頭は切らない（正面の幅はそのまま）
    ///   Nov 2 6回目（わくさん：触角の根元の間くらいの長さで、にっこりした口）：目と同じ黒の細い線（太さ 5.6 mm、はしは 6 割）
    ///     を顔に貼った。はばは触角の根元と同じ 0.112 m（人なら 0.40 m）、真ん中の高さ 0.42 m（目の下 0.443 m とあごの角
    ///     0.40 m のあいだ）、両はしは 1.2 cm 上がる（MouthY・MouthRise・MouthThick）
    ///   正面の写真と重ねた絵：Screenshots/Character/dino_front_compare.png（写真｜ラフ｜重ね）。頭のてっぺんは写真より 3 cm 低い
    ///   （横の写真の高さのまま）。
    ///   形は距離の場（SDF）を足し合わせて Surface Nets で面にする（つなぎ目がなめらか）。
    /// 座標：+Z が前、+Y が上、地面 y = 0、原点は両足のあいだ。メニュー Slug/Character/Build Dino Photo Rough。
    /// </summary>
    public static partial class DinoSlugPhotoRough
    {
        const string ScenePath = "Assets/Scenes/Preview/DinoSlugPhotoRough.unity";
        const string MeshDir = "Assets/Slug/Generated/DinoPhoto";
        const string ShotDir = "Screenshots/Character";

        // photo pixels -> metres. ground under the feet is py 800, between the feet px 885
        const float K = 0.47f / 295f, X0 = 885f, Y0 = 800f;
        static Vector3 P(float px, float py, float side = 0f) => new Vector3(side, (Y0 - py) * K, (px - X0) * K);
        static float R(float pixels) => pixels * K;

        // Nov 2: the front photo (standing, facing the camera). Side-to-side sizes and positions come from it, heights and
        // front-back shapes stay from the side photo (わくさん：正面の写真を正). Front photo: axis px 836, ground py 846,
        // 1 px = KF m (eye height 0.47 m = 386 px from the ground to the eye centre).
        const float KF = 0.47f / 386f;
        static float FW(float pixels) => pixels * KF;   // a side-to-side distance from the axis in the front photo

        // ------------------------------------------------------------------ shapes
        static float Body(Vector3 p)
        {
            float d = Core(p);
            // legs and arms are left-right mirrors (Nov 1 わくさん：左右対称に), built for the right side at |x|
            Vector3 m = new Vector3(Mathf.Abs(p.x), p.y, p.z);
            d = SMin(SMin(d, Thigh(m), 0.03f), Leg(m), 0.006f);   // the thigh is its own round mass; a crease at the knee
            // the top of the arm melts into the shoulder slope (no step under the strap); lower down a crease, so it reads as an arm
            d = SMin(d, Arm(m), Mathf.Lerp(0.008f, 0.02f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Hand.y, Shoulder.y, p.y))));
            return d;
        }

        /// <summary>Head, body and tail, without the legs and arms (metres, roughly).</summary>
        static float Core(Vector3 p)
        {
            // Nov 1 3rd pass (わくさん)：あご・えらを写真に、足に関節、背中は頭から尾へなだらか、胴をしゅっと。
            // Nov 2 (front photo): the head is round from the front (HeadWide), the body and tail a little wider (BodyWide)
            Vector3 q = new Vector3(p.x / BodyWide, p.y, p.z);

            // head: ellipse fitted to 28 outline points (within 3 px): 83 px front-back, 74 px tall, nose end tipped up 9°;
            // then made HeadScale smaller around the chin (HeadPivot), so the face front stays on the photo's line (Nov 1)
            Vector3 hq = HeadPivot + (new Vector3(p.x / HeadWide, p.y, p.z) - HeadPivot) / HeadScale;
            float head = Ellipsoid(Quaternion.Euler(9f, 0f, 0f) * (hq - HeadC), Vector3.zero, new Vector3(R(82), R(73.5f), R(83)));
            // chin underside: flat, cut along the line (1072, 563) -> (1037, 578), just under the photo outline (an ellipse bulges below it).
            // Nov 2: raised to (1080, 551) -> (1040, 565), the edge of the lit face in both photos' shading (about 1.5 cm higher;
            // the front photo's chin middle is py 530, the old cut sat at ~542)
            // Nov 2 (front photo's shading): from the front the chin is a soft smile-shaped line, lowest in the middle (py 530)
            // and rising to the sides (py 500 at 76 px out), so the cut curves up toward the sides by ChinRise * x^2
            Vector3 hc = hq - Vector3.up * (ChinRise * hq.x * hq.x);
            head = -SMin(-head, -Vector3.Dot(hc - P(1080, 551), ChinDown), 0.012f) * HeadScale * Mathf.Min(HeadWide, 1f) / BodyWide;   // in body units

            // body, side-to-side width set height by height to the front photo's outline (Nov 2, BodyWidth): a pear, narrow
            // sloping shoulders under the head, widest low down. Nov 1 had the neck narrowed (x0.88) so the jaw edge showed from
            // the front; the front photo has the head run into the body with hardly any neck
            Vector3 b = new Vector3(q.x / BodyWidth(q.y), q.y, q.z);
            // body: ellipse fitted to the chest front (1030@600) and the belly front sloping back going down
            // (1010@667 -> 950@745) down to the bottom (py 765), leaning forward 38° toward the head ...
            float belly = Ellipsoid(Quaternion.Euler(-37.7f, 0f, 0f) * (b - P(911, 642)), Vector3.zero, new Vector3(R(112), R(139), R(99)));
            // ... with the back trimmed to the smooth top line (TopKeys), round across (BackRound), so the back runs from the
            // back of the head down into the tail with no corner (Nov 2; Nov 1 trimmed it to a straight line)
            float bpx = b.z / K + X0, top = (Y0 - Line(TopY, bpx)) * K;
            float slope = (Line(TopY, bpx - 2f) - Line(TopY, bpx + 2f)) / 4f;   // dy/dz
            float cut = (b.y - top + b.x * b.x / (2f * BackRound)) / Mathf.Sqrt(1f + slope * slope);
            belly = -SMin(-belly, -cut, 0.05f);
            // throat: under the mouth corner the chin turns in sharply and the throat goes almost straight down
            // ((1080, 552) -> (1060, 560) -> (1044, 570) -> (1036, 580) -> (1030, 595))
            belly = SMin(belly, Sphere(b, P(1000, 590), R(33)), 0.015f);   // throat front (1030-1035 at py 578-590)
            // head into body: a small blend at the front (the jaw edge stays) growing to a wide one at the back
            // (the back of the head runs into the back slope with no seam). Nov 2: wide on the sides too, so from the front the
            // head's sides run straight into the neck and the sloping shoulders (only the chin in the middle shows an edge)
            float front = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(P(950, 0).z, P(1010, 0).z, q.z));
            float sideOn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.04f, 0.10f, Mathf.Abs(p.x)));
            float d = SMin(belly, head, Mathf.Lerp(Mathf.Lerp(0.06f, 0.03f, sideOn), 0.01f, front * (1f - sideOn)));

            // tail: round pieces between the top and bottom lines, every 5 px; ends in a point
            int near = Mathf.Clamp(Mathf.RoundToInt((q.z / K + X0 - LineX0) / LineStep), 0, TailPts.Length - 2);
            float t = float.MaxValue;
            for (int i = Mathf.Max(0, near - 8); i <= Mathf.Min(TailPts.Length - 2, near + 8); i++)
                t = Mathf.Min(t, Cone(q, TailPts[i], TailPts[i + 1], TailR[i], TailR[i + 1]));
            d = SMin(d, t, 0.07f);   // wide blend: the tail's underside runs into the belly with no kink
            // Nov 2 5th pass (わくさん：背中がまだなだらかでない): where the tail, the trimmed body and the back of the head all reach
            // the top line, the blends above heaped up a hump 2-3 cm over it (px 780-860, under the bag). Trim the blended back
            // once more to the same round top line, from the tail root to behind the neck (not the tail, where the line is the
            // tail's own top, nor the neck and head, whose widths are fitted to the front photo)
            float backOnly = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(650f, 720f, bpx)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(870f, 910f, bpx)));
            if (backOnly > 0f) d = Mathf.Lerp(d, -SMin(-d, -cut, 0.02f), backOnly);
            return d * BodyWide;   // back to (roughly) metres
        }

        // Hip, knee, ankle of the right leg (side photo px), the thigh's own round mass, the knee + shin + foot
        static readonly Vector3 HipJ = P(914, 738, FW(82)), KneeJ = P(934, 764, FW(82)), AnkleJ = P(920, 786, FW(82));
        static float Thigh(Vector3 m)
        {
            // legs (round, not squashed), with joints as the photo's right (near) leg: thigh -> knee -> shin -> foot with a
            // flat sole, toe out to 945. Nov 2 (front photo): feet wide apart under the outer edge of the body (centres 82 px
            // out, 57 px wide); from the front a round thigh bulges out of the bottom of the body (to 114 px out at py 775),
            // the knee pinches in (py 800) and a short shin goes down to the foot. The knee is bent: the thigh runs forward
            // and down from the hip (914, 738) to the knee (934, 764), the shin back and down to the ankle (920, 786)
            return Cone(m, P(914, 738, LegSide), P(932, 762, LegSide), R(26), R(19));
        }

        static float Leg(Vector3 m)
        {
            float lx = LegSide;
            float leg = SMin(Sphere(m, P(934, 764, lx), R(15)), Cone(m, P(933, 766, lx), P(920, 786, lx), R(16), R(13.5f)), 0.006f);   // knee, shin
            leg = SMin(leg, Ellipsoid(m, P(928, 792, lx), new Vector3(FW(28.5f), R(8), R(19))), 0.01f);      // foot
            return -SMin(-leg, m.y - 0.0008f, 0.004f);                                                        // flat sole on the ground
        }

        static float Arm(Vector3 m)
        {
            // arms (Nov 2: lowered, as the front photo): out of the sloping shoulder (front py ~590, under the bag strap)
            // straight down along the side, a little in front of the middle, to a round hand at the belly (front (952, 665),
            // 116 px out, radius ~21 px). Front-back is not in either photo: set so the arm hugs the side.
            return SMin(Cone(m, Shoulder, Hand, R(16), R(14)), Sphere(m, Hand + Vector3.down * 0.006f, R(15)), 0.01f);
        }

        // Nov 1: whole body 0.75 wide (2/3 of the first pass, then a little wider). Nov 2 (front photo): the head is as
        // wide as it is deep (247 mm across, 203 px), the body and tail a little wider (belly 0.27 m across at 0.18 m up)
        const float HeadWide = 1.0f, BodyWide = 0.85f;
        static readonly float LegSide = FW(82);
        static readonly Vector3 Shoulder = new Vector3(FW(104), (846 - 608) * KF, 0.09f), Hand = new Vector3(FW(112), (846 - 661) * KF, 0.13f);
        const float MouthY = 0.42f, MouthRise = 0.012f, MouthThick = 0.0028f;   // mouth middle height, corners this much higher, line radius (m)
        const int MouthPts = 15;
        const float ChinRise = 4f;   // chin cut rises 3.6 cm at 9 cm out (front photo: py 530 in the middle, 500 at 76 px out)
        static readonly Vector3 HeadC = P(1006, 515);
        const float HeadScale = 0.93f;                          // Nov 1: head a little smaller
        static readonly Vector3 HeadPivot = P(1060, 560);       // ... around the chin, so the face front stays near 1088
        static Vector3 HP(float px, float py, float side = 0f) => HeadPivot + (P(px, py, side) - HeadPivot) * HeadScale;   // a photo point on the head, after the shrink
        static readonly Vector3 ChinDown = Vector3.Cross((P(1040, 565) - P(1080, 551)).normalized, Vector3.right);   // points down, out of the chin
        // Nov 2 (わくさん：横の輪郭は尾の先から後頭部まで、尾の先から足の付け根までなめらか): the side outline is two smooth
        // lines through the photo's edges (photo x, y in px). Top: the tail's top dips a little behind the tip, turns up at the
        // tail root (700, 677) -> (750, 655) and keeps bending up under the bag (hidden there, so drawn as one smooth curve,
        // steepening evenly) to the back of the head. Bottom: the tail's underside down to the back of the legs.
        // (Nov 1 had a straight back line from behind the head to (729, 662) and the tail's own keys; they met at a corner.)
        static readonly Vector3[] TopKeys =
        {
            new Vector3(512, 674), new Vector3(550, 681), new Vector3(600, 688), new Vector3(640, 689), new Vector3(700, 677),
            new Vector3(750, 655), new Vector3(800, 628), new Vector3(850, 595), new Vector3(890, 560), new Vector3(925, 522),
        };
        static readonly Vector3[] BottomKeys =
        {
            new Vector3(512, 676), new Vector3(550, 697), new Vector3(600, 717), new Vector3(650, 732), new Vector3(700, 745),
            new Vector3(750, 756), new Vector3(800, 760), new Vector3(850, 763), new Vector3(880, 765),
        };
        const float LineX0 = 512f, LineStep = 5f;   // the lines are sampled every 5 px from the tail tip
        static float[] TopY, BottomY;
        const float BackRound = 0.10f;    // the back is round across: it drops x^2 / (2 * BackRound) to the sides (Nov 1 trim ~0.187; rounder, so the shoulders slope away from above)

        // tail: round pieces along the middle of the two lines, as thick as the gap between them, from the tip to 850
        const float TailEnd = 850f;
        static Vector3[] TailPts;
        static float[] TailR;

        static DinoSlugPhotoRough()
        {
            TopY = SampleLine(TopKeys);
            BottomY = SampleLine(BottomKeys);
            int n = Mathf.RoundToInt((TailEnd - LineX0) / LineStep) + 1;
            TailPts = new Vector3[n];
            TailR = new float[n];
            for (int k = 0; k < n; k++)
            {
                float x = LineX0 + k * LineStep, top = Line(TopY, x), bottom = Line(BottomY, x);
                TailPts[k] = P(x, (top + bottom) * 0.5f);
                TailR[k] = R(Mathf.Max((bottom - top) * 0.5f, 0.5f));
            }
        }

        // body width by height (x BodyWide), fitted to the front photo's outline (half-widths in m: 0.110 at 0.38 m up,
        // 0.124 at 0.35, 0.139 at 0.32 with the arm, 0.134 at 0.18 below the hands); between the keys it eases
        static readonly Vector2[] BodyWidthKeys =
        {
            new Vector2(0.12f, 1.0f), new Vector2(0.20f, 0.94f), new Vector2(0.27f, 0.96f), new Vector2(0.32f, 0.97f),
            new Vector2(0.35f, 0.95f), new Vector2(0.38f, 1.01f), new Vector2(0.42f, 1.05f),
        };
        static float BodyWidth(float y)
        {
            if (y <= BodyWidthKeys[0].x) return BodyWidthKeys[0].y;
            for (int i = 0; i < BodyWidthKeys.Length - 1; i++)
                if (y < BodyWidthKeys[i + 1].x)
                    return Mathf.Lerp(BodyWidthKeys[i].y, BodyWidthKeys[i + 1].y, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BodyWidthKeys[i].x, BodyWidthKeys[i + 1].x, y)));
            return BodyWidthKeys[BodyWidthKeys.Length - 1].y;
        }

        /// <summary>Photo y every LineStep px along a Catmull-Rom line through the keys (x, y).</summary>
        static float[] SampleLine(Vector3[] keys)
        {
            int n = Mathf.RoundToInt((keys[keys.Length - 1].x - LineX0) / LineStep) + 1;
            var ys = new float[n];
            for (int k = 0; k < n; k++)
            {
                float x = LineX0 + k * LineStep;
                int i = 0;
                while (i < keys.Length - 2 && keys[i + 1].x < x) i++;
                float f = (x - keys[i].x) / (keys[i + 1].x - keys[i].x);
                ys[k] = CatmullRom(keys[Mathf.Max(i - 1, 0)], keys[i], keys[i + 1], keys[Mathf.Min(i + 2, keys.Length - 1)], f).y;
            }
            return ys;
        }

        /// <summary>A sampled line's photo y at photo x (held flat past its ends).</summary>
        static float Line(float[] ys, float px)
        {
            float u = Mathf.Clamp((px - LineX0) / LineStep, 0f, ys.Length - 1.001f);
            int i = (int)u;
            return Mathf.Lerp(ys[i], ys[i + 1], u - i);
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        // ------------------------------------------------------------------ build
        [MenuItem("Slug/Character/Build Dino Photo Rough")]
        public static void Build()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty && !string.IsNullOrEmpty(active.path)) EditorSceneManager.SaveScene(active);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Directory.CreateDirectory(MeshDir);

            var gray = NewMat("Body", new Color(0.66f, 0.66f, 0.64f));
            var dark = NewMat("Eye", new Color(0.06f, 0.06f, 0.06f));
            var gear = NewMat("Compare", new Color(0.45f, 0.44f, 0.42f));
            var groundMat = NewMat("Ground", new Color(0.40f, 0.46f, 0.50f));

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            var root = new GameObject("DinoSlug_Photo");
            var bodyMesh = Mesher.Build(Body, new Vector3(-0.26f, -0.01f, -0.66f), new Vector3(0.26f, 0.62f, 0.42f), 0.006f);
            SkinUV(bodyMesh);
            var bodyGo = Part(root, "Body", bodyMesh, gray);
            var antennas = FaceParts(root, dark, gray);

            // the comparison slug: capsule 1.1 m long, 0.5 m tall (README 3-2)
            var cmp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cmp.name = "CompareSlug_1.1m";
            UnityEngine.Object.DestroyImmediate(cmp.GetComponent<Collider>());
            cmp.transform.SetPositionAndRotation(new Vector3(-0.75f, 0.25f, -0.05f), Quaternion.Euler(90f, 0f, 0f));
            cmp.transform.localScale = new Vector3(0.5f, 0.55f, 0.5f);
            cmp.GetComponent<MeshRenderer>().sharedMaterial = gear;

            var sun = GameObject.Find("Directional Light");
            if (sun != null) sun.transform.rotation = Quaternion.Euler(35f, -60f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Directory.CreateDirectory(ShotDir);

            // photo match: orthographic from the right side, 1 px = K m, framed like the photo (1672 x 941)
            cmp.SetActive(false);
            ground.SetActive(false);
            Shot("dino_photo_match.png", P(836, 470.5f) + Vector3.right * 3f, Vector3.left, 941 * 0.5f * K, 1672, 941);
            // front photo match (Nov 2): orthographic from the front, 1 px = KF m, axis at px 836, ground at py 846
            Shot("dino_front_match.png", new Vector3(0f, (846 - 470.5f) * KF, 3f), Vector3.back, 941 * 0.5f * KF, 1672, 941);
            ground.SetActive(true);
            // Nov 2: from the slug eye height (0.47 m), 1.8 m away: front, front three-quarter, side
            Vector3 look = new Vector3(0f, 0.33f, 0f);
            foreach (var (file, dir) in new[] { ("dino_eye_front.png", new Vector3(0f, 0f, 1f)), ("dino_eye_34.png", new Vector3(0.7f, 0f, 0.7f)), ("dino_eye_side.png", new Vector3(1f, 0f, 0f)) })
            {
                Vector3 at = new Vector3(0f, 0.47f, 0f) + dir.normalized * 1.8f;
                Shot(file, at, (look - at).normalized, 0f, 1200, 900);
            }
            // A: from the slug eye height (0.47 m), front-right three-quarter
            Vector3 c = new Vector3(0f, 0.30f, -0.05f);
            Shot("dino_photo_A.png", new Vector3(1.3f, 0.47f, 1.5f), (c - new Vector3(1.3f, 0.47f, 1.5f)).normalized, 0f, 1200, 900);
            // B: side, next to the comparison capsule
            cmp.SetActive(true);
            Shot("dino_photo_B.png", new Vector3(3f, 0.35f, -0.1f), Vector3.left, 0.55f, 1200, 900);
            // C: from above at an angle
            Shot("dino_photo_C.png", new Vector3(1.2f, 2.2f, -1.0f), (new Vector3(-0.3f, 0.2f, -0.05f) - new Vector3(1.2f, 2.2f, -1.0f)).normalized, 0f, 1200, 900);
            // front (left-right symmetry, the jaw edge) and back three-quarter (the back ridge, the tail)
            cmp.SetActive(false);
            Shot("dino_photo_front.png", new Vector3(0f, 0.32f, 3f), Vector3.back, 0.42f, 900, 900);
            Shot("dino_photo_back34.png", new Vector3(-1.3f, 0.6f, -1.4f), (new Vector3(0f, 0.3f, 0f) - new Vector3(-1.3f, 0.6f, -1.4f)).normalized, 0f, 900, 900);

            // Nov 2 (わくさん：いま動かせるナメクジの模様を貼る): the playable slug's skin on the body and antennae, and the
            // same three eye-height shots with it. The scene is left with the skin on.
            var (skin, app) = SlugSkin();
            bodyGo.GetComponent<MeshRenderer>().sharedMaterial = skin;
            foreach (var a in antennas) a.GetComponent<MeshRenderer>().sharedMaterial = app;
            foreach (var (file, dir) in new[] { ("dino_skin_front.png", new Vector3(0f, 0f, 1f)), ("dino_skin_34.png", new Vector3(0.7f, 0f, 0.7f)), ("dino_skin_side.png", new Vector3(1f, 0f, 0f)), ("dino_skin_back34.png", new Vector3(-0.7f, 0f, -0.7f)) })
            {
                Vector3 at = new Vector3(0f, 0.47f, 0f) + dir.normalized * 1.8f;
                Shot(file, at, (look - at).normalized, 0f, 1200, 900);
            }
            cmp.SetActive(true);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[DinoSlugPhotoRough] built " + ScenePath);
        }

        /// <summary>Eyes, antennae and mouth on the body (made by the body's field); returns the antennae.</summary>
        static List<GameObject> FaceParts(GameObject root, Material dark, Material gray)
        {
            var antennas = new List<GameObject>();

            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "_L" : "_R";

                // eye: small black upright oval, at the side photo's height (507) and the front photo's spread (60 px out),
                // found by coming in from the front onto the face. Size from the front photo (18 x 29 px), seen face on
                Vector3 at = Surface(new Vector3(side * FW(60), HP(1077, 507).y, 0.4f), Vector3.back), n = Normal(Body, at);
                var rot = Quaternion.LookRotation(n, Vector3.up);
                Vector3 eyeC = at - n * 0.003f;
                Part(root, "Eye" + s, Mesher.Build(q => Ellipsoid(Quaternion.Inverse(rot) * (q - eyeC), Vector3.zero, new Vector3(FW(9), FW(14.5f), 0.006f)),
                                                   eyeC - Vector3.one * 0.03f, eyeC + Vector3.one * 0.03f, 0.0015f), dark);

                // antenna: straight stalk from the top of the head to a ball, left-right mirrors, length and ball 3/4 of
                // the side photo's (Nov 1). Nov 2 (front photo): roots 46 px out on the top of the head, opened into a V,
                // 22° out from upright (balls 87 px out); the little forward lean from the side photo stays
                const float antScale = 0.75f;
                Vector3 b0 = Surface(new Vector3(side * FW(46), 0.75f, HP(1029, 450).z), Vector3.down);
                b0 -= Normal(Body, b0) * 0.008f;
                Vector3 up = P(1039, 360) - P(1029, 450);
                Vector3 tip = b0 + (up + Vector3.right * (side * Mathf.Tan(22f * Mathf.Deg2Rad) * up.y)).normalized * up.magnitude * antScale;
                var pts = new Vector3[6];
                for (int i = 0; i < 6; i++) pts[i] = Vector3.Lerp(b0, tip, i / 5f);
                Func<Vector3, float> ant = q =>
                {
                    float d = Sphere(q, tip, R(20) * antScale);   // ball: photo radius 20 px
                    for (int i = 0; i < 5; i++) d = Mathf.Min(d, Cone(q, pts[i], pts[i + 1], Mathf.Lerp(R(8), R(6.5f), i / 5f) * antScale, Mathf.Lerp(R(8), R(6.5f), (i + 1) / 5f) * antScale));
                    return d;
                };
                antennas.Add(Part(root, "Antenna" + s, Mesher.Build(ant, Vector3.Min(b0, tip) - Vector3.one * 0.04f, Vector3.Max(b0, tip) + Vector3.one * 0.04f, 0.003f), gray));
            }

            // mouth (Nov 2 わくさん：触角の根元の間くらいの長さで、にっこりした口): a thin black line in the eyes' black, laid on
            // the face halfway between the eyes' bottom (0.443 m) and where the face turns under at the chin (0.40 m); corners
            // as far out as the antenna roots (46 px) and MouthRise higher than the middle, a smile. Thinner at the corners
            var mouth = new Vector3[MouthPts];
            for (int i = 0; i < MouthPts; i++)
            {
                float x = Mathf.Lerp(-FW(46), FW(46), i / (MouthPts - 1f)), u = x / FW(46);
                Vector3 at = Surface(new Vector3(x, MouthY + MouthRise * u * u, 0.4f), Vector3.back);
                mouth[i] = at - Normal(Body, at) * 0.001f;
            }
            Func<Vector3, float> lip = q =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < MouthPts - 1; i++)
                {
                    float r0 = Mathf.Lerp(MouthThick, MouthThick * 0.6f, Mathf.Abs(i / (MouthPts - 1f) * 2f - 1f));
                    float r1 = Mathf.Lerp(MouthThick, MouthThick * 0.6f, Mathf.Abs((i + 1) / (MouthPts - 1f) * 2f - 1f));
                    d = Mathf.Min(d, Cone(q, mouth[i], mouth[i + 1], r0, r1));
                }
                return d;
            };
            Vector3 lo = mouth[0], hi = mouth[0];
            foreach (var m in mouth) { lo = Vector3.Min(lo, m); hi = Vector3.Max(hi, m); }
            Part(root, "Mouth", Mesher.Build(lip, lo - Vector3.one * 0.008f, hi + Vector3.one * 0.008f, 0.001f), dark);
            return antennas;
        }

        // ------------------------------------------------------------------ the playable slug's skin (Nov 2)
        // the playable slug as it is in the game and test scenes: Slug_Pop with pattern 0.5 (faint green rings + some
        // mottling), round body style, flat sole 0.75; everything else at the prefab's values
        const string SlugPrefab = "Assets/Slug/Prefabs/Slug_Pop.prefab";
        // the slug shader moves the body (crawl wave, stretch, sole fringe, bends); a statue keeps all of it at zero
        static readonly string[] StillKeys =
        {
            "_WaveAmp", "_Stretch", "_SkirtAmp", "_BumpAmp", "_JiggleAmp", "_RuffleAmp", "_UprightAngle", "_DropAmount",
            "_TwistAngle", "_TurnAngle", "_FollowMode", "_SwayAmp",
        };

        static (Material body, Material app) SlugSkin()
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SlugPrefab));
            var pop = go.GetComponent<SlugPop>();
            pop.pattern = 0.5f;
            pop.bodyStyle = SlugBodyStyle.V4Round;
            pop.soleSquare = 0.75f;
            pop.Rebuild();
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var body = StillCopy((Material)typeof(SlugPop).GetField("_body", F).GetValue(pop), "Skin");
            var app = StillCopy((Material)typeof(SlugPop).GetField("_app", F).GetValue(pop), "SkinAntenna");
            UnityEngine.Object.DestroyImmediate(go);
            return (body, app);
        }

        static Material StillCopy(Material src, string name)
        {
            var m = new Material(src) { name = "DinoPhoto_" + name, hideFlags = HideFlags.None };
            foreach (var k in StillKeys) m.SetFloat(k, 0f);
            m.SetVector("_PathAnchor", Vector4.zero);
            m.SetVector("_FaceC", Vector4.zero);   // no painted face (the rough has its own eyes)
            string path = MeshDir + "/" + m.name + ".mat";
            var old = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (old == null) { AssetDatabase.CreateAsset(m, path); return m; }
            // kept under the same file, so what uses it (the rough's scene) keeps it
            old.shader = m.shader;
            old.CopyPropertiesFromMaterial(m);
            EditorUtility.SetDirty(old);
            return old;
        }

        // uv as on the playable slug's body: x round the body (0 = middle of the back, 0.5 = middle of the belly; both
        // sides go 0 -> 0.5 the same way, so there is no seam down the back), y along it (0 = tail tip, 1 = head). Measured from a spine through the middle of the tail, body and head
        // (side photo px); the belly side is the side under the spine, so on the upright front it is the chest and belly
        static readonly Vector2[] SpineKeys =
        {
            new Vector2(512, 675), new Vector2(600, 702), new Vector2(700, 711), new Vector2(800, 694), new Vector2(870, 665),
            new Vector2(920, 620), new Vector2(960, 580), new Vector2(1000, 530), new Vector2(1030, 470),
        };

        static void SkinUV(Mesh mesh)
        {
            int n = SpineKeys.Length;
            var sp = new Vector3[n];
            var len = new float[n];
            for (int i = 0; i < n; i++)
            {
                sp[i] = P(SpineKeys[i].x, SpineKeys[i].y);
                if (i > 0) len[i] = len[i - 1] + (sp[i] - sp[i - 1]).magnitude;
            }
            var verts = mesh.vertices;
            var uv = new Vector2[verts.Length];
            for (int v = 0; v < verts.Length; v++)
            {
                Vector3 q = new Vector3(0f, verts[v].y, verts[v].z);
                float best = float.MaxValue, t = 0f;
                Vector3 c = sp[0], tan = Vector3.forward;
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 ab = sp[i + 1] - sp[i];
                    float f = Mathf.Clamp01(Vector3.Dot(q - sp[i], ab) / ab.sqrMagnitude);
                    Vector3 on = sp[i] + ab * f;
                    float dd = (q - on).sqrMagnitude;
                    if (dd < best) { best = dd; c = on; tan = ab.normalized; t = (len[i] + ab.magnitude * f) / len[n - 1]; }
                }
                Vector3 up = new Vector3(0f, tan.z, -tan.y);   // away from the belly side
                Vector3 off = verts[v] - c;
                uv[v] = new Vector2(Mathf.Abs(Mathf.Atan2(off.x, Vector3.Dot(off, up))) / (2f * Mathf.PI), t);
            }
            mesh.uv = uv;
        }

        // ------------------------------------------------------------------ SDF bits
        static float Sphere(Vector3 p, Vector3 c, float r) => (p - c).magnitude - r;

        /// <summary>Capsule whose radius goes from ra at a to rb at b.</summary>
        static float Cone(Vector3 p, Vector3 a, Vector3 b, float ra, float rb)
        {
            Vector3 ba = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ba) / Vector3.Dot(ba, ba));
            return (p - (a + ba * t)).magnitude - Mathf.Lerp(ra, rb, t);
        }

        static float Ellipsoid(Vector3 p, Vector3 c, Vector3 r)
        {
            Vector3 q = p - c;
            float k0 = new Vector3(q.x / r.x, q.y / r.y, q.z / r.z).magnitude;
            float k1 = new Vector3(q.x / (r.x * r.x), q.y / (r.y * r.y), q.z / (r.z * r.z)).magnitude;
            return k1 < 1e-6f ? -Mathf.Min(r.x, Mathf.Min(r.y, r.z)) : k0 * (k0 - 1f) / k1;
        }

        static float RoundBox(Vector3 p, Vector3 c, Vector3 half, float r)
        {
            Vector3 q = p - c;
            q = new Vector3(Mathf.Abs(q.x), Mathf.Abs(q.y), Mathf.Abs(q.z)) - half + Vector3.one * r;
            return Vector3.Max(q, Vector3.zero).magnitude + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f) - r;
        }

        /// <summary>Cylinder along X, rounded rim.</summary>
        static float CylinderX(Vector3 q, float r, float halfLen, float round)
        {
            var d = new Vector2(new Vector2(q.y, q.z).magnitude - r + round, Mathf.Abs(q.x) - halfLen + round);
            return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + Vector2.Max(d, Vector2.zero).magnitude - round;
        }

        static float SMin(float a, float b, float k)
        {
            float h = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
            return Mathf.Min(a, b) - h * h * k * 0.25f;
        }

        static Vector3 Normal(Func<Vector3, float> f, Vector3 p)
        {
            const float e = 0.0008f;
            return new Vector3(f(p + Vector3.right * e) - f(p - Vector3.right * e),
                               f(p + Vector3.up * e) - f(p - Vector3.up * e),
                               f(p + Vector3.forward * e) - f(p - Vector3.forward * e)).normalized;
        }

        /// <summary>Walk from p along dir (1 mm steps) until it crosses the body surface.</summary>
        static Vector3 Surface(Vector3 p, Vector3 dir)
        {
            dir.Normalize();
            bool inside0 = Body(p) < 0f;
            for (int i = 0; i < 2000; i++)
            {
                Vector3 q = p + dir * 0.001f;
                if ((Body(q) < 0f) != inside0) return q;
                p = q;
            }
            return p;
        }

        // ------------------------------------------------------------------ helpers
        // where Part saves its meshes (the rig builder saves its own copies elsewhere)
        static string PartDir = MeshDir, PartPrefix = "DinoPhoto_";

        static GameObject Part(GameObject root, string name, Mesh mesh, Material mat)
        {
            mesh.name = PartPrefix + name;
            string path = PartDir + "/" + mesh.name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Material NewMat(string name, Color c)
        {
            string path = MeshDir + "/DinoPhoto_" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>orthoSize &gt; 0 = orthographic.</summary>
        static void Shot(string file, Vector3 camPos, Vector3 forward, float orthoSize, int W, int H)
        {
            var camGo = new GameObject("ShotCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.13f, 0.17f);
            cam.nearClipPlane = 0.02f;
            cam.orthographic = orthoSize > 0f;
            cam.orthographicSize = orthoSize;
            cam.fieldOfView = 30f;
            cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(forward, Vector3.up));
            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = (float)W / H;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(camGo);
        }

        // ------------------------------------------------------------------ Surface Nets
        /// <summary>Turns a distance field into a smooth mesh: one vertex per cell the surface crosses, snapped onto the surface.</summary>
        static class Mesher
        {
            public static Mesh Build(Func<Vector3, float> f, Vector3 lo, Vector3 hi, float h)
            {
                int nx = Mathf.CeilToInt((hi.x - lo.x) / h) + 1, ny = Mathf.CeilToInt((hi.y - lo.y) / h) + 1, nz = Mathf.CeilToInt((hi.z - lo.z) / h) + 1;
                var val = new float[nx * ny * nz];
                Func<int, int, int, int> id = (i, j, k) => (k * ny + j) * nx + i;
                for (int k = 0; k < nz; k++)
                    for (int j = 0; j < ny; j++)
                        for (int i = 0; i < nx; i++)
                            val[id(i, j, k)] = f(lo + new Vector3(i, j, k) * h);

                var cellVert = new Dictionary<int, int>();
                var verts = new List<Vector3>();
                var corner = new Vector3[8];
                var cv = new float[8];
                for (int k = 0; k < nz - 1; k++)
                    for (int j = 0; j < ny - 1; j++)
                        for (int i = 0; i < nx - 1; i++)
                        {
                            int neg = 0;
                            for (int c = 0; c < 8; c++)
                            {
                                int di = c & 1, dj = (c >> 1) & 1, dk = (c >> 2) & 1;
                                cv[c] = val[id(i + di, j + dj, k + dk)];
                                corner[c] = new Vector3(di, dj, dk);
                                if (cv[c] < 0f) neg++;
                            }
                            if (neg == 0 || neg == 8) continue;
                            Vector3 sum = Vector3.zero;
                            int cnt = 0;
                            for (int a = 0; a < 8; a++)
                                for (int b = a + 1; b < 8; b++)
                                {
                                    int x = a ^ b;
                                    if (x != 1 && x != 2 && x != 4) continue;   // cube edges only
                                    if ((cv[a] < 0f) == (cv[b] < 0f)) continue;
                                    float t = cv[a] / (cv[a] - cv[b]);
                                    sum += Vector3.Lerp(corner[a], corner[b], t);
                                    cnt++;
                                }
                            Vector3 v = lo + (new Vector3(i, j, k) + sum / cnt) * h;
                            for (int it = 0; it < 2; it++) v -= Normal(f, v) * f(v);   // snap onto the surface
                            cellVert[id(i, j, k)] = verts.Count;
                            verts.Add(v);
                        }

                var tris = new List<int>();
                for (int k = 1; k < nz - 1; k++)
                    for (int j = 1; j < ny - 1; j++)
                        for (int i = 1; i < nx - 1; i++)
                        {
                            float v0 = val[id(i, j, k)];
                            // edges along +x, +y, +z from this grid point; the four cells around each edge make a quad
                            TryQuad(v0, val[id(i + 1, j, k)], id(i, j, k), id(i, j - 1, k), id(i, j - 1, k - 1), id(i, j, k - 1), cellVert, verts, tris, f, lo + new Vector3(i + 0.5f, j, k) * h);
                            TryQuad(v0, val[id(i, j + 1, k)], id(i, j, k), id(i, j, k - 1), id(i - 1, j, k - 1), id(i - 1, j, k), cellVert, verts, tris, f, lo + new Vector3(i, j + 0.5f, k) * h);
                            TryQuad(v0, val[id(i, j, k + 1)], id(i, j, k), id(i - 1, j, k), id(i - 1, j - 1, k), id(i, j - 1, k), cellVert, verts, tris, f, lo + new Vector3(i, j, k + 0.5f) * h);
                        }

                var normals = new Vector3[verts.Count];
                for (int i = 0; i < verts.Count; i++) normals[i] = Normal(f, verts[i]);
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(verts);
                mesh.SetNormals(normals);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateBounds();
                return mesh;
            }

            static void TryQuad(float a, float b, int c0, int c1, int c2, int c3, Dictionary<int, int> cellVert, List<Vector3> verts, List<int> tris, Func<Vector3, float> f, Vector3 mid)
            {
                if ((a < 0f) == (b < 0f)) return;
                if (!cellVert.TryGetValue(c0, out int i0) || !cellVert.TryGetValue(c1, out int i1) ||
                    !cellVert.TryGetValue(c2, out int i2) || !cellVert.TryGetValue(c3, out int i3)) return;
                Vector3 n = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
                if (Vector3.Dot(n, Normal(f, mid)) < 0f) { int t = i1; i1 = i3; i3 = t; }
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i0); tris.Add(i2); tris.Add(i3);
            }
        }
    }
}
