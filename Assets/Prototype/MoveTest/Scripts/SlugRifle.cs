using System.Collections.Generic;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// An assault rifle in the spirit of the AK: a wooden stock, pistol grip and handguard on a dark steel receiver and
    /// barrel, and a curved magazine with a little window on each side that shows how much salt is left (it empties
    /// from the bottom up, as the follower pushes the salt up). Slugs load it with refined salt.
    /// The right hand holds the magazine (and pulls it out to reload); the left hand cups the handguard by the muzzle.
    /// The magazine is a child that turns about its top (<see cref="MagPivotLocal"/>).
    /// </summary>
    public class SlugRifle : SlugGunModel
    {
        public const float Bore = 0.046f, Length = 0.625f;
        /// <summary>The top of the magazine, where it turns when pulled out.</summary>
        public static readonly Vector3 MagPivotLocal = new Vector3(0f, 0.016f, 0.322f);
        /// <summary>The left hand: cupped under the front end of the handguard, close to the muzzle.</summary>
        public static readonly Vector3 LeftGripLocal = new Vector3(-0.004f, 0.004f, 0.495f);

        const float MagRadius = 0.26f, MagSweep = 30f, MagHalf = 0.0135f;
        const float WindowFrom = 0.12f, WindowTo = 0.86f;

        /// <summary>The right hand: on the right side of the magazine, part way down (magazine space).</summary>
        public static Vector3 MagGripLocal => MagazinePoint(0.42f, 0.004f, 0.017f);
        /// <summary>The direction the magazine runs at the right hand (magazine space).</summary>
        public static Vector3 MagGripAxis => MagazineAxis(0.42f);

        public override float BoreY => Bore;
        public override Vector3 MuzzleLocal => new Vector3(0f, Bore, Length);
        public override Vector3 RestGripLocal => MagPivotLocal + MagGripLocal;
        public override Vector3 ReloadFocusLocal => MagPivotLocal;

        /// <summary>
        /// A point of the magazine in its own space: <paramref name="t"/> 0..1 from the top down its curve,
        /// <paramref name="across"/> toward the front (+) or back (-) of it, <paramref name="x"/> to the side.
        /// It starts straight down and curves forward.
        /// </summary>
        public static Vector3 MagazinePoint(float t, float across, float x)
        {
            float a = t * MagSweep * Mathf.Deg2Rad;
            return new Vector3(x, -MagRadius * Mathf.Sin(a) + across * Mathf.Sin(a), MagRadius * (1f - Mathf.Cos(a)) + across * Mathf.Cos(a));
        }

        public static Vector3 MagazineAxis(float t)
        {
            float a = t * MagSweep * Mathf.Deg2Rad;
            return new Vector3(0f, -Mathf.Cos(a), Mathf.Sin(a));
        }

        static Mesh _woodMesh, _steelMesh, _coverMesh, _rubberMesh, _magMesh, _windowMesh;

        Transform _mag;
        Mesh _fillMesh;
        float _fill = -1f;

        protected override void Build(bool castShadows)
        {
            EnsureMeshes();
            Part(transform, "Wood", _woodMesh, Wood, castShadows);
            Part(transform, "Steel", _steelMesh, Steel, castShadows);
            Part(transform, "Cover", _coverMesh, Cover, castShadows);
            Part(transform, "Rubber", _rubberMesh, Rubber, castShadows);
            _mag = Pivot(transform, "Magazine", MagPivotLocal);
            Part(_mag, "MagBody", _magMesh, Steel, castShadows);
            Part(_mag, "MagWindow", _windowMesh, Glass, false);
            _fillMesh = new Mesh { name = "MagSalt", hideFlags = HideFlags.DontSave };
            _fillMesh.MarkDynamic();
            Part(_mag, "MagSalt", _fillMesh, Salt, false);
        }

        /// <summary>The magazine: rocked out along its curve, then carried away by the right hand and brought back full.</summary>
        public override void Pose(GunHandling h, GunSettings s, out Hand right, out Hand left)
        {
            Vector3 magPos = MagPivotLocal + new Vector3(0f, -0.95f, 0.3f) * (s.magazineDrop * h.MagazineOut)
                             + s.magazineAway * h.MagazineAway;
            Quaternion magRot = Quaternion.Euler(-12f * h.MagazineOut + 20f * h.MagazineAway, 0f, 35f * h.MagazineAway);
            _mag.localPosition = magPos;
            _mag.localRotation = magRot;
            right = new Hand(magPos + magRot * MagGripLocal, magRot * MagGripAxis);
            left = new Hand(LeftGripLocal, Vector3.forward);
        }

        /// <summary>How full the magazine is (0..1), shown in its side windows.</summary>
        public override void SetAmmo(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            if (Mathf.Abs(fraction - _fill) < 1e-4f) return;
            _fill = fraction;
            _fillMesh.Clear();
            if (fraction <= 0f) return;
            // the salt sits at the top (the follower pushes it up), so it empties from the bottom
            float to = Mathf.Lerp(WindowFrom, WindowTo, fraction);
            var built = new ProfileMesh().Extrude(Band(WindowFrom, to, -0.009f, 0.007f), MagHalf + 0.0013f, 0f).ToMesh("tmp");
            _fillMesh.SetVertices(built.vertices);
            _fillMesh.SetNormals(built.normals);
            _fillMesh.SetTriangles(built.triangles, 0);
            _fillMesh.RecalculateBounds();
            DestroyImmediate(built);
        }

        void OnDestroy()
        {
            if (_fillMesh) Destroy(_fillMesh);
        }

        // ------------------------------------------------------------------ the shape
        /// <summary>A band along the magazine's curve (magazine space, as (z, y)), from t0 to t1, between two offsets across it.</summary>
        static List<Vector2> Band(float t0, float t1, float back, float front, int steps = 10)
        {
            var p = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                var q = MagazinePoint(Mathf.Lerp(t0, t1, i / (float)steps), front, 0f);
                p.Add(new Vector2(q.z, q.y));
            }
            for (int i = steps; i >= 0; i--)
            {
                var q = MagazinePoint(Mathf.Lerp(t0, t1, i / (float)steps), back, 0f);
                p.Add(new Vector2(q.z, q.y));
            }
            return p;
        }

        static void EnsureMeshes()
        {
            if (_woodMesh != null) return;
            const float BoreY = Bore;

            // ---- wood: the stock, the pistol grip, the handguard below the barrel and the cover above it
            var wood = new ProfileMesh();
            wood.Extrude(Zy(0f, 0.035f, -0.004f, -0.044f, 0.045f, -0.031f, 0.1f, -0.008f, 0.158f, 0.018f, 0.158f, 0.057f), 0.02f, 0.004f);
            wood.Extrude(Zy(0.206f, 0.014f, 0.244f, 0.014f, 0.226f, -0.066f, 0.19f, -0.066f), 0.016f, 0.004f);
            wood.Extrude(Zy(0.385f, 0.06f, 0.385f, 0.02f, 0.4f, 0.012f, 0.515f, 0.016f, 0.53f, 0.028f, 0.53f, 0.058f), 0.024f, 0.005f);
            wood.Extrude(Zy(0.398f, 0.058f, 0.505f, 0.058f, 0.505f, 0.07f, 0.497f, 0.08f, 0.41f, 0.08f, 0.398f, 0.072f), 0.016f, 0.004f);
            _woodMesh = wood.ToMesh("RifleWood");

            // ---- steel: receiver, sights, barrel and its parts, trigger and guard, the selector and charging handle
            var steel = new ProfileMesh();
            steel.Extrude(Zy(0.152f, 0.012f, 0.362f, 0.012f, 0.362f, 0.066f, 0.17f, 0.066f, 0.152f, 0.056f), 0.019f, 0.003f);
            steel.Extrude(Zy(0.36f, 0.056f, 0.398f, 0.056f, 0.398f, 0.072f, 0.36f, 0.079f), 0.014f, 0.002f);           // rear sight block
            steel.Extrude(Zy(0.364f, 0.079f, 0.39f, 0.074f, 0.39f, 0.078f, 0.366f, 0.083f), 0.009f, 0.001f);           // rear sight leaf
            steel.Tube(new Vector3(0f, BoreY, 0.36f), new Vector3(0f, BoreY, 0.59f), 0.0085f);                           // barrel
            steel.Tube(new Vector3(0f, 0.068f, 0.5f), new Vector3(0f, 0.068f, 0.545f), 0.0075f);                        // gas tube
            steel.Extrude(Zy(0.535f, 0.036f, 0.56f, 0.036f, 0.56f, 0.078f, 0.535f, 0.078f), 0.012f, 0.002f);           // gas block
            steel.Tube(new Vector3(0f, BoreY, 0.565f), new Vector3(0f, BoreY, 0.59f), 0.0125f);                          // front sight base
            steel.Extrude(Zy(0.568f, 0.05f, 0.588f, 0.05f, 0.585f, 0.086f, 0.571f, 0.086f), 0.007f, 0.0015f);           // front sight post
            steel.Tube(new Vector3(0f, BoreY, 0.588f), new Vector3(0f, BoreY, Length), 0.012f, 10, 0.0105f);             // muzzle brake
            TriggerGroup(steel, 0.244f, 0.013f);
            steel.Extrude(Zy(0.25f, 0.044f, 0.335f, 0.034f, 0.338f, 0.041f, 0.252f, 0.051f), 0.0016f, 0f, 0.0205f);   // selector lever
            steel.Tube(new Vector3(0.018f, 0.056f, 0.338f), new Vector3(0.038f, 0.056f, 0.338f), 0.004f, 8);          // charging handle
            steel.Tube(new Vector3(0.036f, 0.056f, 0.338f), new Vector3(0.046f, 0.056f, 0.338f), 0.0075f, 8);
            _steelMesh = steel.ToMesh("RifleSteel");

            // ---- the dust cover on top of the receiver, a shade lighter
            _coverMesh = new ProfileMesh()
                .Extrude(Zy(0.165f, 0.064f, 0.356f, 0.064f, 0.356f, 0.074f, 0.348f, 0.081f, 0.19f, 0.081f, 0.165f, 0.07f), 0.017f, 0.004f)
                .ToMesh("RifleCover");
            _rubberMesh = new ProfileMesh()
                .Extrude(Zy(-0.008f, 0.037f, -0.012f, -0.046f, -0.002f, -0.046f, 0.002f, 0.037f), 0.021f, 0.003f)
                .ToMesh("RifleButtPlate");

            // ---- the magazine (its own space: the top middle at the origin) with a floor plate and the side windows
            var mag = new ProfileMesh();
            mag.Extrude(MagazineOutline(), MagHalf, 0.003f);
            var b0 = MagazinePoint(1f, -0.025f, 0f); var b1 = MagazinePoint(1f, 0.025f, 0f);
            var ax = MagazineAxis(1f);
            mag.Extrude(new List<Vector2>
            {
                new Vector2(b0.z, b0.y), new Vector2(b1.z, b1.y),
                new Vector2(b1.z + ax.z * 0.008f, b1.y + ax.y * 0.008f), new Vector2(b0.z + ax.z * 0.008f, b0.y + ax.y * 0.008f),
            }, MagHalf + 0.0015f, 0.002f);
            _magMesh = mag.ToMesh("RifleMagazine");
            _windowMesh = new ProfileMesh().Extrude(Band(WindowFrom - 0.02f, WindowTo + 0.02f, -0.011f, 0.009f), MagHalf + 0.0007f, 0f).ToMesh("RifleMagWindow");
        }

        /// <summary>The magazine's side outline: slightly deeper at the top, curving forward as it goes down.</summary>
        static List<Vector2> MagazineOutline()
        {
            const int steps = 12;
            var p = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                var q = MagazinePoint(t, Mathf.Lerp(0.0225f, 0.02f, t), 0f);
                p.Add(new Vector2(q.z, q.y));
            }
            for (int i = steps; i >= 0; i--)
            {
                float t = i / (float)steps;
                var q = MagazinePoint(t, -Mathf.Lerp(0.0225f, 0.02f, t), 0f);
                p.Add(new Vector2(q.z, q.y));
            }
            return p;
        }
    }
}
