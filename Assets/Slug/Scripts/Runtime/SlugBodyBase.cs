using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// What the tools ask of a slug's body, whichever body it is: the crawling one (<see cref="SlugPop"/>) or the two-legged
    /// one (<see cref="DinoSlugBody"/>, Nov 2). The holders, the rack, the back draw and the carry keep one of these in their
    /// <c>slug</c> field (a base class rather than an interface, so Unity saves the reference).
    ///   Arms: a held tool's stretchy arms grow from the arm roots (the skin at the side for the crawling slug, the
    ///     shoulders for the two-legged one); the body hides its own short hands while those arms are out.
    ///   Carried: the tools not in hand ride on spots of the body (<see cref="CarrySpot"/>), found at rest and followed as
    ///     the body moves (<see cref="FollowPointWorld"/>).
    /// </summary>
    public abstract class SlugBodyBase : MonoBehaviour
    {
        /// <summary>Middle of the face, in world space. Used to aim close-up cameras and third-person tools.</summary>
        public abstract Vector3 FaceCenterWorld { get; }

        /// <summary>Where the left (-1) / right (+1) stretchy arm grows from, in world space, this frame.</summary>
        public abstract Vector3 HandRootWorld(float side);

        /// <summary>Which way the left (-1) / right (+1) arm root faces out of the body, in world space.</summary>
        public abstract Vector3 HandRootNormalWorld(float side);

        /// <summary>The hands' material (colour, toon shading, outline), to dress arms made elsewhere.</summary>
        public abstract Material HandMaterial { get; }

        /// <summary>
        /// A stretchy arm made for a tool, growing from the left (-1) / right (+1) arm root. The body hides its own short
        /// hand on that side while the arm is shown (active).
        /// </summary>
        public abstract void UseStretchyArm(float side, GameObject arm);

        /// <summary>Shuts the left eye (0 open .. 1 shut): one eye closed to look along a gun.</summary>
        public virtual void SetWink(float amount) { }

        /// <summary>
        /// How dry the skin looks: 0 = as built, 1 = parched (dull, greyed, darker); <paramref name="soaked"/> (0..1) a
        /// little extra sheen when wetter than full. Only the skin, not the gear.
        /// </summary>
        public virtual void SetDryness(float dry, float soaked = 0f) { }

        /// <summary>
        /// A gun held up (every frame while one is in hand): <paramref name="shouldered"/> 0..1 how far it is up at the
        /// shoulder, <paramref name="sighting"/> 0..1 how far the eye is along the sights; <paramref name="longGun"/> false
        /// for the pistol. The body turns its upper half to it as it can (0, 0 = no gun up).
        /// </summary>
        public virtual void SetGunStance(float shouldered, float sighting, bool longGun) { }

        /// <summary>
        /// The pickaxe's swing as the upper body follows it (every frame while it is in hand): +1 wound up behind the
        /// right shoulder, 0 square to the blow (or no swing), down to -1 carried on through past it.
        /// </summary>
        public virtual void SetSwingTurn(float wound) { }

        /// <summary>
        /// Where the hands are on the swinging pickaxe (world, this frame; the body reads them the next; while the blow
        /// comes down, where they will land), and how much
        /// the swing counts (0 = no swing): the body may lean and shift toward them so the arms need not stretch.
        /// </summary>
        public virtual void SetSwingHands(Vector3 left, Vector3 right, float weight) { }

        /// <summary>
        /// Where the eye looking along a long gun's sights is, this frame (third person; the first-person view has its
        /// own). False: the body has no such place and the gun stays at the shoulder.
        /// </summary>
        public virtual bool SightEyeWorld(out Vector3 eye) { eye = Vector3.zero; return false; }

        /// <summary>
        /// How far this body brings a gun held in the hands (carried across the chest, or the pistol held out) closer
        /// than the gun's settings put it: a shift in body space (+Z forward), added to where the hands go.
        /// </summary>
        public virtual Vector3 GunHoldShift => Vector3.zero;

        /// <summary>Where gear is parented. Its scale is the body's.</summary>
        public virtual Transform VisualRoot => transform;

        /// <summary>Changes when the body is rebuilt (the carry spots are found again); null before it is built.</summary>
        public abstract object ShapeKey { get; }

        /// <summary>
        /// A spot on the skin to carry a tool on, at rest, in body space: <paramref name="t"/> along the body (0 = tail tip,
        /// 1 = head, as <see cref="SlugMove.LoadoutSettings"/> gives it) and <paramref name="angleDeg"/> round it (0 = the
        /// middle of the back, 90 = the right side). Gives the outward normal, the way along the body toward the head, the
        /// body's right, the belly side there, and the <paramref name="tHit"/> to follow the spot with.
        /// </summary>
        public abstract bool CarrySpot(float t, float angleDeg, out Vector3 p, out Vector3 n, out Vector3 along, out Vector3 right,
                                       out Vector3 belly, out float tHit);

        /// <summary>Where a body-space point riding the skin at <paramref name="t"/> (from <see cref="CarrySpot"/>) is in the world now.</summary>
        public abstract Vector3 FollowPointWorld(Vector3 pointBody, float t, Vector3 normalBody);
    }
}
