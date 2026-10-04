using System.Collections.Generic;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// A small gun for the hip: a dark steel slide on a lighter frame, wooden grip panels and a magazine in the grip,
    /// loaded with refined salt; a small CO2 cartridge rides in the grip behind the magazine. It has no stock: held out in front of the chest in both hands, the right fist round the
    /// grip and the left one cupped under it. The slide jumps back on every shot and stays back when the salt runs out
    /// (until a full magazine is seated). Reloading, the left hand pulls the magazine down out of the grip, takes it
    /// away, and pushes a full one back in.
    ///
    /// Local space as for every <see cref="SlugGunModel"/>: the bore along +Z at <see cref="BoreY"/>; the origin is the
    /// back of the frame at the top of the grip (where the "butt" of a stocked gun would be; the holder places it in
    /// front of the chest instead of on the shoulder).
    /// </summary>
    public class SlugPistol : SlugGunModel
    {
        const float Bore = 0.03f, Length = 0.196f;
        /// <summary>The top of the magazine well, where the magazine leaves the grip.</summary>
        static readonly Vector3 MagPivotLocal = new Vector3(0f, 0.002f, 0.009f);
        /// <summary>Down the grip (and the magazine), raked back a little.</summary>
        static readonly Vector3 GripAxis = new Vector3(0f, -0.993f, -0.115f);
        static readonly Vector3 RightGripLocal = new Vector3(0.004f, -0.036f, 0.006f);
        static readonly Vector3 LeftGripLocal = new Vector3(-0.012f, -0.058f, 0.014f);
        const float MagLength = 0.088f, SlideTravel = 0.022f;

        protected override float RawBoreY => Bore;
        protected override Vector3 RawMuzzle => new Vector3(0f, Bore, Length);
        protected override Vector3 RawRestGrip => RightGripLocal;
        protected override Vector3 RawReloadFocus => MagPivotLocal + GripAxis * MagLength;

        static Mesh _slideMesh, _frameMesh, _gripMesh, _steelMesh, _magMesh, _magSaltMesh, _serrationMesh, _portMesh;

        Transform _slide, _mag;
        bool _wasReloading, _lockedForReload;

        protected override void Build(bool castShadows)
        {
            EnsureMeshes();
            Part(transform, "Frame", _frameMesh, Cover, castShadows);
            Part(transform, "Grip", _gripMesh, Wood, castShadows);
            Part(transform, "Steel", _steelMesh, Steel, castShadows);
            _slide = Moving(Pivot(transform, "Slide", Vector3.zero));
            Part(_slide, "SlideBody", _slideMesh, Steel, castShadows);
            Part(_slide, "Serrations", _serrationMesh, Rubber, castShadows);
            Part(_slide, "Port", _portMesh, Cover, castShadows);
            _mag = Moving(Pivot(transform, "Magazine", MagPivotLocal));
            Part(_mag, "MagBody", _magMesh, Steel, castShadows);
            Part(_mag, "MagSalt", _magSaltMesh, Salt, false);
        }

        /// <summary>
        /// The slide kicks back on every shot (and stays back on an empty magazine); the magazine is pulled down the grip,
        /// carried off by the left hand, and pushed back in full.
        /// </summary>
        protected override void RawPose(GunHandling h, GunSettings s, out Hand right, out Hand left)
        {
            // an empty gun keeps its slide back until a full magazine is seated
            if (h.Reloading && !_wasReloading) _lockedForReload = h.Ammo == 0;
            _wasReloading = h.Reloading;
            bool seated = h.ReloadStage != GunHandling.Stage.Magazine || h.StageProgress >= 0.78f;
            bool locked = (!h.Reloading && h.Ammo == 0) || (h.Reloading && _lockedForReload && !seated);
            float blow = Bump(h.SinceShot, 0f, 0.022f, 0.035f, 0.09f);
            Place(_slide, new Vector3(0f, 0f, -SlideTravel * Mathf.Max(blow, locked ? 1f : 0f)));

            float outAmount = h.MagazineOut, away = h.MagazineAway;
            Vector3 magPos = MagPivotLocal + GripAxis * (s.magazineDrop * outAmount + 0.01f * away) + s.magazineAway * away;
            Quaternion magRot = Quaternion.Euler(15f * away, 0f, -30f * away);
            Place(_mag, magPos);
            _mag.localRotation = magRot;

            right = new Hand(RightGripLocal, GripAxis);
            // the left hand leaves its place under the right fist for the magazine's floor plate, and comes back
            float onMag = h.ReloadStage == GunHandling.Stage.Magazine ? Bump(h.StageProgress, 0.08f, 0.2f, 0.8f, 0.92f) : 0f;
            Vector3 plate = magPos + magRot * (GripAxis * (MagLength + 0.012f) + new Vector3(-0.004f, 0f, 0.004f));
            left = new Hand(Vector3.Lerp(LeftGripLocal, plate, onMag), Vector3.Slerp(GripAxis, magRot * Vector3.forward, onMag));
        }

        // ------------------------------------------------------------------ the shape
        static void EnsureMeshes()
        {
            if (_slideMesh != null) return;

            // ---- the slide (moves back on a shot): a long block with a chamfered top front, the sights on top
            var slide = new ProfileMesh();
            slide.Extrude(Zy(-0.012f, 0.014f, 0.19f, 0.014f, 0.19f, 0.039f, 0.182f, 0.045f, -0.005f, 0.045f, -0.012f, 0.038f), 0.0125f, 0.003f);
            slide.Extrude(Zy(0.174f, 0.044f, 0.184f, 0.044f, 0.183f, 0.052f, 0.176f, 0.052f), 0.0025f, 0.0008f);           // front sight
            slide.Extrude(Zy(-0.006f, 0.044f, 0.01f, 0.044f, 0.01f, 0.052f, -0.006f, 0.052f), 0.0028f, 0.0006f, 0.0065f);  // rear sight, two posts
            slide.Extrude(Zy(-0.006f, 0.044f, 0.01f, 0.044f, 0.01f, 0.052f, -0.006f, 0.052f), 0.0028f, 0.0006f, -0.0065f);
            slide.Tube(new Vector3(0f, Bore, 0.186f), new Vector3(0f, Bore, Length), 0.0062f, 10);                           // barrel crown
            _slideMesh = slide.ToMesh("PistolSlide");

            // grip grooves at the back of the slide, and the ejection port on the right
            var grooves = new ProfileMesh();
            for (int i = 0; i < 4; i++)
            {
                float z = -0.006f + i * 0.0065f;
                foreach (float side in new[] { 1f, -1f })
                    grooves.Extrude(Zy(z, 0.019f, z + 0.0028f, 0.019f, z + 0.0028f, 0.04f, z, 0.04f), 0.0005f, 0f, side * 0.0127f);
            }
            _serrationMesh = grooves.ToMesh("PistolSerrations");
            _portMesh = new ProfileMesh().Extrude(Zy(0.07f, 0.029f, 0.112f, 0.029f, 0.112f, 0.042f, 0.07f, 0.042f), 0.0006f, 0f, 0.0127f).ToMesh("PistolPort");

            // ---- the frame under the slide, a shade lighter, with a dust cover to the front
            _frameMesh = new ProfileMesh()
                .Extrude(Zy(-0.008f, 0.002f, 0.168f, 0.002f, 0.172f, 0.008f, 0.172f, 0.0145f, -0.008f, 0.0145f), 0.011f, 0.0025f)
                .ToMesh("PistolFrame");

            // ---- wooden grip panels, raked back
            _gripMesh = new ProfileMesh()
                .Extrude(Zy(-0.009f, 0.004f, 0.034f, 0.004f, 0.032f, -0.012f, 0.025f, -0.08f, -0.017f, -0.08f, -0.023f, -0.014f), 0.0145f, 0.004f)
                .ToMesh("PistolGrip");

            // ---- small steel parts: the trigger and its guard, the hammer, the slide stop
            var steel = new ProfileMesh();
            TriggerGroup(steel, 0.034f, 0.003f);
            steel.Extrude(Zy(-0.011f, 0.03f, -0.004f, 0.031f, -0.006f, 0.047f, -0.016f, 0.051f, -0.019f, 0.045f), 0.0035f, 0.001f);   // hammer
            steel.Extrude(Zy(0.03f, 0.012f, 0.07f, 0.012f, 0.07f, 0.017f, 0.03f, 0.017f), 0.0012f, 0f, -0.0118f);                      // slide stop
            _steelMesh = steel.ToMesh("PistolSteel");

            // ---- the magazine (its own space: the top of the well at the origin), mostly hidden in the grip; a wider
            //      floor plate shows under it, and the salt at its top shows when it is out
            Vector3 down = GripAxis, front = Vector3.Cross(Vector3.right, down).normalized * -1f;
            var mag = new ProfileMesh();
            mag.Extrude(Band(down, front, 0.006f, MagLength, -0.011f, 0.011f), 0.008f, 0.0015f);
            mag.Extrude(Band(down, front, MagLength, MagLength + 0.007f, -0.015f, 0.014f), 0.0115f, 0.002f);
            _magMesh = mag.ToMesh("PistolMagazine");
            _magSaltMesh = new ProfileMesh().Extrude(Band(down, front, -0.001f, 0.007f, -0.009f, 0.009f), 0.006f, 0.0015f).ToMesh("PistolMagSalt");
        }

        /// <summary>A strip along <paramref name="down"/> from a to b, between two offsets toward <paramref name="front"/>, as (z, y) points.</summary>
        static List<Vector2> Band(Vector3 down, Vector3 front, float a, float b, float back, float fore)
        {
            var p = new List<Vector2>();
            foreach (var (along, across) in new[] { (a, back), (a, fore), (b, fore), (b, back) })
            {
                Vector3 q = down * along + front * across;
                p.Add(new Vector2(q.z, q.y));
            }
            return p;
        }
    }
}
