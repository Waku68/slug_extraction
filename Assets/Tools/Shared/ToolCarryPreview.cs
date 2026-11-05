using SlugDesign;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Look only (Nov 4 わくさん：パンパンの袋の上に銃とピッケルが載って、持ち替えが不自然。しっぽで渡す案と体の横にくくる案の
    /// 見た目だけ見たい。手に持っていないときは少し小さくしてもいい): a rifle and a pickaxe tied onto a standing two-legged slug
    /// in one of the ideas, riding its bones. No reaching or drawing yet; the real carrying is still <see cref="SlugToolRack"/>
    /// (and without a bag the tools stay slung on the back).
    ///  - Side: the rifle tied flat along the right flank, muzzle down and back toward the tail; the pickaxe through the
    ///    belt on the left hip, head forward at the belt, handle down and back.
    ///  - Tail: the rifle and the pickaxe tied flat on the top (sky side) of the tail, crossed in an X (Nov 4).
    ///    (The tail would curl round to hand them to the hands.)
    /// Body space: +Z forward, +Y up, +X right, feet at 0.
    /// </summary>
    public class ToolCarryPreview : MonoBehaviour
    {
        public enum Style { Side, Tail }

        public DinoSlugBody slug;
        public Style style = Style.Side;
        [Tooltip("手に持っていないときの大きさ（手に持ったときを 1）")] public float stowedScale = 0.7f;

        [Header("体の横")]
        [Tooltip("銃のまん中（体の空間, m）と、銃口の向き（前後と上下の角度：下へ・後ろへ）")] public Vector3 sideGunAt = new Vector3(0.155f, 0.21f, -0.03f);
        public float sideGunDown = 28f;
        [Tooltip("ピッケルの頭（体の空間, m）と、柄の向き（下へ・後ろへの角度）")] public Vector3 sidePickHead = new Vector3(-0.155f, 0.21f, 0.06f);
        public float sidePickDown = 25f;

        [Header("しっぽ")]
        [Tooltip("ばってんの交わる所：しっぽの付け根から先までの割合（0..1）")] public float tailCrossAt = 0.75f;   // (0.5 sat under the crammed bag)
        [Tooltip("ばってんの開き：しっぽの向きから、それぞれこの角度（度）")] public float tailCrossAngle = 30f;
        [Tooltip("銃のまん中を、しっぽの芯からこれだけ上に（m）。ピッケルはさらにこれだけ上（m）")] public float tailGunLift = 0.05f;
        public float tailPickOver = 0.022f;

        void Start()
        {
            if (!slug) slug = GetComponentInChildren<DinoSlugBody>();
            if (!slug) return;
            var gun = SlugGunModel.Create(GunKind.Rifle, "Preview_Rifle", null, true);
            var pick = SlugPickaxe.Create("Preview_Pickaxe", null, true);
            gun.transform.localScale = pick.transform.localScale = Vector3.one * stowedScale;
            var b = slug.transform;
            float len = gun.MuzzleLocal.z * stowedScale;

            if (style == Style.Side)
            {
                // rifle: flat against the right flank (its left side on the body), muzzle down and back
                Vector3 dir = b.TransformDirection(Quaternion.Euler(-sideGunDown, 0f, 0f) * Vector3.back);
                Vector3 up = Vector3.Cross(dir, b.right);
                if (Vector3.Dot(up, b.up) < 0f) up = -up;
                Vector3 c = b.TransformPoint(sideGunAt);
                gun.transform.SetPositionAndRotation(c - dir * (len * 0.5f) - up * (gun.BoreY * stowedScale * 0.5f), Quaternion.LookRotation(dir, up));
                gun.transform.SetParent(slug.hips, true);
                // pickaxe: head at the belt on the left hip, the handle down and back, the head's points front and back
                Vector3 handle = b.TransformDirection(Quaternion.Euler(-sidePickDown, 0f, 0f) * Vector3.forward);   // bottom -> head
                Vector3 head = b.TransformPoint(sidePickHead);
                Vector3 bottom = head - handle * (SlugPickaxe.HeadLocal.y * stowedScale);
                pick.transform.SetPositionAndRotation(bottom, Quaternion.LookRotation(Vector3.Cross(b.right, handle) * -1f, handle));
                pick.transform.SetParent(slug.hips, true);
            }
            else
            {
                var tail = slug.tail;
                if (tail == null || tail.Length < 2) return;
                Vector3 TailAt(float t, out Vector3 along, out Transform bone)
                {
                    float u = Mathf.Clamp01(t) * (tail.Length - 1);
                    int i = Mathf.Min(Mathf.FloorToInt(u), tail.Length - 2);
                    along = (tail[i + 1].position - tail[i].position).normalized;   // toward the tip
                    bone = tail[Mathf.RoundToInt(u)];
                    return Vector3.Lerp(tail[i].position, tail[i + 1].position, u - i);
                }
                // Nov 4 (わくさん：しっぽの空側に、ばってんで): both lie flat on the top of the tail, crossed in an X over
                // tailCrossAt. The rifle (underneath) runs muzzle toward the tip, swung one way; the pickaxe (on top) head toward
                // the root, swung the other way
                Vector3 p = TailAt(tailCrossAt, out var a, out var bone);
                Vector3 up = Vector3.ProjectOnPlane(b.up, a).normalized;
                Vector3 gunDir = Quaternion.AngleAxis(-tailCrossAngle, up) * a;             // toward the tip, swung right
                Vector3 gunUp = Vector3.Cross(up, gunDir).normalized;                       // lies on its side: its top across the tail
                Vector3 gunMid = p + up * tailGunLift;
                gun.transform.SetPositionAndRotation(gunMid - gunDir * (len * 0.5f) - gunUp * (gun.BoreY * stowedScale * 0.5f),
                                                     Quaternion.LookRotation(gunDir, gunUp));
                gun.transform.SetParent(bone, true);
                Vector3 handle = Quaternion.AngleAxis(tailCrossAngle, up) * -a;             // bottom (tip side) -> head (root side), crossing the rifle
                Vector3 pickMid = p + up * (tailGunLift + tailPickOver);
                Vector3 bottom = pickMid - handle * (SlugPickaxe.HeadLocal.y * stowedScale * 0.5f);
                pick.transform.SetPositionAndRotation(bottom, Quaternion.LookRotation(Vector3.Cross(up, handle).normalized, handle));   // the head's points flat across
                pick.transform.SetParent(bone, true);
            }
        }
    }
}
