using SlugDesign;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// The way a tool comes off the back into the hands, and back on again (the same path played backwards).
    /// Over p 0..1 (0 = riding on the back, 1 = held): the right hand goes up over the right shoulder and down to the tool
    /// where it rides (up to <see cref="LoadoutSettings.reachPart"/>); then it slides the tool out of the straps along its
    /// length, and swings it up over the shoulder and round to the front, where the left hand takes it. Neither the hand
    /// nor the tool goes into what is worn on the back, however swollen the bag (<see cref="SlugToolRack.OutOfBack"/>).
    /// </summary>
    public static class BackDraw
    {
        /// <summary>Of the pull (after the hand has reached the tool), the part spent sliding it out of the straps.</summary>
        const float SlidePart = 0.3f;
        /// <summary>How hard the bag is jolted when a tool is pulled free of it or pushed home (degrees per second).</summary>
        const float Jolt = 70f;

        /// <summary>0..1 (eased): how far the right hand has gone from in front of the chest to the tool on the back.</summary>
        public static float Reach(float p, float reachPart) => Ease(p / Mathf.Max(reachPart, 1e-3f));
        /// <summary>0..1 (eased): how far the tool has come from the back to the hands.</summary>
        public static float Pull(float p, float reachPart) => Ease((p - reachPart) / Mathf.Max(1f - reachPart, 1e-3f));
        /// <summary>
        /// 0..1: how much the right arm is drawn round over the shoulder (through <see cref="Over"/>) rather than straight
        /// to the hand: fully while the hand is on the back, letting go as the hand comes past the shoulder.
        /// </summary>
        public static float ArmVia(float p, float reachPart)
        {
            float u = Mathf.Clamp01((p - reachPart) / Mathf.Max(1f - reachPart, 1e-3f));
            float past = SlidePart + 0.5f * (1f - SlidePart);      // where the hand is over the shoulder
            return Reach(p, reachPart) * (1f - Ease((u - (past - 0.2f)) / 0.2f));
        }

        /// <summary>The p at which the tool comes free of the straps.</summary>
        static float Free(float reachPart) => reachPart + SlidePart * (1f - reachPart);

        /// <summary>The point over the right shoulder that the hand and the tool pass (world), outside the bag.</summary>
        public static Vector3 Over(SlugBodyBase slug, Quaternion body, LoadoutSettings s, SlugToolRack rack)
        {
            Vector3 over = slug.HandRootWorld(1f) + body * s.overShoulder;
            return rack ? over + rack.OutOfBack(over, 0.04f) : over;
        }

        /// <summary>
        /// The tool on its way from where it rides (<paramref name="seat"/>) to where it is held (<paramref name="held"/>),
        /// turning in the hand at <paramref name="gripLocal"/>. <paramref name="slideLocal"/> is the way (and how far) it
        /// slides out of the straps, in its own space. Pushed out of the bag by its deepest point of <paramref name="samples"/>.
        /// </summary>
        public static ToolPose Tool(ToolPose seat, ToolPose held, Vector3 gripLocal, Vector3 slideLocal, Vector3 over,
                                    float p, float reachPart, SlugToolRack rack, Vector3[] samples, float margin)
        {
            float u = Mathf.Clamp01((p - reachPart) / Mathf.Max(1f - reachPart, 1e-3f));
            Vector3 g0 = seat.Point(gripLocal), g1 = g0 + seat.rotation * slideLocal;
            Vector3 grip;
            Quaternion rot;
            if (u < SlidePart)
            {
                grip = Vector3.Lerp(g0, g1, Ease(u / SlidePart));
                rot = seat.rotation;
            }
            else
            {
                float v = Ease((u - SlidePart) / (1f - SlidePart));
                grip = Through(g1, over, held.Point(gripLocal), v);
                rot = Quaternion.Slerp(seat.rotation, held.rotation, v);
            }
            var pose = new ToolPose { position = grip - rot * gripLocal, rotation = rot };
            if (!rack || samples == null || u >= 1f) return pose;
            // the deepest point decides how far the whole tool is pushed out (fading in from the seat, which already
            // sits outside the bag, so the tool does not jump as the hand takes it)
            Vector3 push = Vector3.zero;
            foreach (var local in samples)
            {
                Vector3 d = rack.OutOfBack(pose.Point(local), margin);
                if (d.sqrMagnitude > push.sqrMagnitude) push = d;
            }
            pose.position += push * Ease(u / SlidePart);
            return pose;
        }

        /// <summary>The right hand on its way from in front of the chest to the tool on the back, over the shoulder.</summary>
        public static Vector3 Hand(Vector3 rest, Vector3 over, Vector3 grip, float reach, SlugToolRack rack)
        {
            Vector3 hand = Through(rest, over, grip, reach);
            return rack ? hand + rack.OutOfBack(hand, 0.02f) : hand;
        }

        /// <summary>
        /// First person (camera space): the tool comes in from off the top right. It first goes forward over the view,
        /// then down into the hands, as if brought over the shoulder.
        /// </summary>
        public static Vector3 FpArc(Vector3 from, Vector3 to, float t)
        {
            // (a shallow bow, so it comes into view well before it is in the hands)
            Vector3 c = new Vector3(Mathf.Lerp(from.x, to.x, 0.4f), Mathf.Lerp(from.y, to.y, 0.3f), Mathf.Lerp(from.z, to.z, 0.7f));
            float s = 1f - t;
            return s * s * from + 2f * s * t * c + t * t * to;
        }

        /// <summary>The bag on the back gives a little jolt when the tool comes free of it (drawn) or is pushed home (put away).</summary>
        public static void Jostle(SlugToolRack rack, float before, float now, float reachPart)
        {
            if (!rack || !rack.Gear) return;
            float free = Free(reachPart);
            if (before < free && now >= free) rack.Gear.Jostle(Jolt);
            else if (before > reachPart && now <= reachPart) rack.Gear.Jostle(-Jolt * 0.7f);
        }

        /// <summary>A curve from a to c that passes through b halfway.</summary>
        static Vector3 Through(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            Vector3 ctrl = 2f * b - 0.5f * (a + c);
            float s = 1f - t;
            return s * s * a + 2f * s * t * ctrl + t * t * c;
        }

        static float Ease(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }
    }
}
