using System.Collections.Generic;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// An assault rifle in the spirit of the AK, on CO2: the stock is the gas bottle itself (a painted steel bottle
    /// with a brass valve where it screws into the receiver, and a rubber pad on its end); wooden pistol grip and
    /// handguard on a dark steel receiver and barrel, and a curved magazine with a little window on each side that shows
    /// how much salt is left (it empties from the bottom up, as the follower pushes the salt up). Drawn compact with a
    /// long barrel. Slugs load it with refined salt, poured in from the top like a paintball marker's loader: a small
    /// filler with a hinged lid sits on the dust cover and feeds the magazine below (it stays on the gun).
    /// The right hand holds the magazine; to reload it grabs a handful of salt from the belt (the gloves keep the salt
    /// off the skin), flips the lid open, and lets a thin stream run from the fist into the filler (as long as it takes,
    /// or until the trigger is pulled), then goes back to the magazine. The left hand cups the handguard just in front of the magazine throughout. (Guns set to <see cref="ReloadStyle.Magazine"/>
    /// still pull the magazine out and bring it back full.)
    /// </summary>
    public class SlugRifle : SlugGunModel
    {
        public const float Bore = 0.046f, Length = 0.625f;
        /// <summary>How much shorter the stock is than the AK's (m).</summary>
        const float StockCut = 0.065f;
        protected override float BodyScale => 0.75f;
        protected override float WidthScale => 1.05f;      // short, but as thick as ever: a chunky little gun
        protected override float HeightScale => 0.85f;
        protected override float BarrelSplit => 0.532f;      // just in front of the handguard
        protected override float BarrelExtra => 0.02f;
        /// <summary>The top of the magazine, where it turns when pulled out.</summary>
        public static readonly Vector3 MagPivotLocal = new Vector3(0f, 0.016f, 0.322f);
        /// <summary>The left hand: cupped under the back end of the handguard, just in front of the magazine.</summary>
        public static readonly Vector3 LeftGripLocal = new Vector3(-0.004f, 0.006f, 0.41f);

        const float MagRadius = 0.26f, MagSweep = 30f, MagHalf = 0.0135f;
        const float WindowFrom = 0.12f, WindowTo = 0.86f;

        /// <summary>The right hand: on the right side of the magazine, part way down (magazine space).</summary>
        public static Vector3 MagGripLocal => MagazinePoint(0.42f, 0.004f, 0.017f);
        /// <summary>The direction the magazine runs at the right hand (magazine space).</summary>
        public static Vector3 MagGripAxis => MagazineAxis(0.42f);

        protected override float RawBoreY => Bore;
        protected override Vector3 RawMuzzle => new Vector3(0f, Bore, Length);
        protected override Vector3 RawRestGrip => MagPivotLocal + MagGripLocal;
        protected override Vector3 RawReloadFocus => FillerTop;
        /// <summary>The height of the line over the rear notch and the tip of the front post: well above the dust cover,
        /// so looking along it the cover drops out of the way and the notch stands against the world beyond.</summary>
        const float SightLine = 0.098f;
        public override bool HasSights => true;
        // on the sight line over the bottle, behind the receiver, as a cheek on the stock puts it (from further back the
        // dust cover looks narrow instead of a floor under the eye)
        protected override Vector3 RawSightEye => new Vector3(0f, SightLine, 0.07f);

        /// <summary>The filler stands on the sight line right in front of the eye: the own view leaves it out while sighting.</summary>
        public override void SetSighting(float t)
        {
            bool show = t < 0.6f;
            if (_filler.gameObject.activeSelf == show) return;
            _filler.gameObject.SetActive(show);
            _fillerHole.gameObject.SetActive(show);
            _lid.gameObject.SetActive(show);
        }

        /// <summary>The middle of the filler's mouth, and the lid's hinge at its back edge.</summary>
        static readonly Vector3 FillerTop = new Vector3(0f, 0.112f, 0.255f), LidHinge = new Vector3(0f, 0.112f, 0.234f);
        /// <summary>Where the right hand holds the handful of salt while pouring: above the filler, a little behind it.</summary>
        static readonly Vector3 PourHand = new Vector3(0.004f, 0.172f, 0.222f);
        /// <summary>Where the salt runs out of the fist (fist space).</summary>
        static readonly Vector3 SpoutLocal = new Vector3(0f, 0f, 0.04f);
        protected override float RawButtZ => StockCut;

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

        static Mesh _woodMesh, _steelMesh, _coverMesh, _rubberMesh, _magMesh, _windowMesh, _bottleMesh, _brassMesh;
        static Mesh _fillerMesh, _fillerHoleMesh, _lidMesh, _pouchMesh, _streamMesh, _sightTipMesh;

        Transform _mag, _lid, _pouch, _stream, _filler, _fillerHole;
        Mesh _fillMesh;
        float _fill = -1f;

        protected override void Build(bool castShadows)
        {
            EnsureMeshes();
            Part(transform, "Wood", _woodMesh, Wood, castShadows);
            Part(transform, "Steel", _steelMesh, Steel, castShadows);
            Part(transform, "Cover", _coverMesh, Cover, castShadows);
            Part(transform, "Rubber", _rubberMesh, Rubber, castShadows);
            Part(transform, "Bottle", _bottleMesh, Bottle, castShadows);
            Part(transform, "Brass", _brassMesh, Brass, castShadows);
            Part(transform, "SightTip", _sightTipMesh, Salt, castShadows);
            _mag = Moving(Pivot(transform, "Magazine", MagPivotLocal));
            Part(_mag, "MagBody", _magMesh, Steel, castShadows);
            Part(_mag, "MagWindow", _windowMesh, Glass, false);
            _fillMesh = new Mesh { name = "MagSalt", hideFlags = HideFlags.DontSave };
            _fillMesh.MarkDynamic();
            Part(_mag, "MagSalt", _fillMesh, Salt, false);
            _filler = Part(transform, "Filler", _fillerMesh, Brass, castShadows);
            _fillerHole = Part(transform, "FillerHole", _fillerHoleMesh, Rubber, false);
            _lid = Moving(Pivot(transform, "Lid", LidHinge));
            Part(_lid, "LidSteel", _lidMesh, Steel, castShadows);
            _pouch = Moving(Pivot(transform, "Pouch", PourHand));
            Part(_pouch, "Handful", _pouchMesh, Salt, castShadows);
            _pouch.gameObject.SetActive(false);
            _stream = Moving(Pivot(transform, "Stream", FillerTop));
            Part(_stream, "SaltStream", _streamMesh, Salt, false);
            _stream.gameObject.SetActive(false);
        }

        /// <summary>
        /// Reloading by pouring: the right hand goes from the magazine down to the belt and grabs a handful of salt (the
        /// lid flips open meanwhile), comes up over the filler, tips the fist and lets the salt run in (a thin stream, the
        /// hand shaking a little), then goes back past the belt to the magazine as the lid shuts.
        /// A magazine-style reload rocks the magazine out, carries it away and brings it back full.
        /// </summary>
        protected override void RawPose(GunHandling h, GunSettings s, out Hand right, out Hand left)
        {
            if (s.reloadStyle == ReloadStyle.Pour) { PourPose(h, s, out right, out left); return; }
            HideReloadParts();
            Vector3 magPos = MagPivotLocal + new Vector3(0f, -0.95f, 0.3f) * (s.magazineDrop * h.MagazineOut)
                             + s.magazineAway * h.MagazineAway;
            Quaternion magRot = Quaternion.Euler(-12f * h.MagazineOut + 20f * h.MagazineAway, 0f, 35f * h.MagazineAway);
            Place(_mag, magPos);
            _mag.localRotation = magRot;
            right = new Hand(magPos + magRot * MagGripLocal, magRot * MagGripAxis);
            left = new Hand(LeftGripLocal, Vector3.forward);
        }

        void HideReloadParts()
        {
            _lid.localRotation = Quaternion.identity;
            Place(_lid, LidHinge);
            if (_pouch.gameObject.activeSelf) _pouch.gameObject.SetActive(false);
            if (_stream.gameObject.activeSelf) _stream.gameObject.SetActive(false);
        }

        void PourPose(GunHandling h, GunSettings s, out Hand right, out Hand left)
        {
            Place(_mag, MagPivotLocal);
            _mag.localRotation = Quaternion.identity;
            left = new Hand(LeftGripLocal, Vector3.forward);
            Vector3 grip = MagPivotLocal + MagGripLocal, gripAxis = MagGripAxis;
            Vector3 belt = MagPivotLocal + s.magazineAway;
            // the pouch tipped toward the filler: its spout points down at the mouth
            Quaternion tipped = Quaternion.LookRotation((FillerTop + new Vector3(0f, 0.035f, 0f) - PourHand).normalized, Vector3.forward);
            Quaternion carried = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            float u = h.StageProgress, lid = 0f, tip = 0f;
            Vector3 hand = grip, axis = gripAxis;
            bool pouch = false, pouring = false;
            switch (h.ReloadStage)
            {
                case GunHandling.Stage.Open:
                    lid = Smooth(u / 0.5f);
                    hand = u < 0.45f ? Vector3.Lerp(grip, belt, Smooth(u / 0.45f)) : Vector3.Lerp(belt, PourHand, Smooth((u - 0.45f) / 0.55f));
                    axis = Vector3.Slerp(gripAxis, Vector3.up, Smooth(u / 0.45f));
                    pouch = u >= 0.4f;
                    tip = Smooth((u - 0.7f) / 0.3f);
                    break;
                case GunHandling.Stage.Pour:
                {
                    lid = 1f; tip = 1f; pouch = true; pouring = true;
                    float a = u * Mathf.PI * 2f;
                    hand = PourHand + new Vector3(0.002f * Mathf.Sin(a * 2f), 0.003f * Mathf.Sin(a), 0f);      // a little shake
                    axis = Vector3.up;
                    break;
                }
                case GunHandling.Stage.Close:
                    lid = 1f - Smooth((u - 0.2f) / 0.5f);
                    hand = u < 0.5f ? Vector3.Lerp(PourHand, belt, Smooth(u / 0.5f)) : Vector3.Lerp(belt, grip, Smooth((u - 0.5f) / 0.5f));
                    axis = Vector3.Slerp(Vector3.up, gripAxis, Smooth((u - 0.5f) / 0.5f));
                    pouch = u < 0.55f;
                    tip = 1f - Smooth(u / 0.3f);
                    break;
            }
            Place(_lid, LidHinge);
            _lid.localRotation = Quaternion.Euler(-110f * lid, 0f, 0f);
            if (_pouch.gameObject.activeSelf != pouch) _pouch.gameObject.SetActive(pouch);
            Quaternion pouchRot = Quaternion.Slerp(carried, tipped, tip);
            if (pouch) { Place(_pouch, hand); _pouch.localRotation = pouchRot; }
            if (_stream.gameObject.activeSelf != pouring) _stream.gameObject.SetActive(pouring);
            if (pouring)
            {
                // a thin stream from the spout down into the filler (laid out as drawn, so it always joins the two)
                Vector3 top = Warp(hand + pouchRot * SpoutLocal), bottom = Warp(FillerTop);
                Vector3 d = top - bottom;
                Place(_stream, FillerTop);
                _stream.localRotation = Quaternion.FromToRotation(Vector3.up, d);
                _stream.localScale = new Vector3(1f, d.magnitude, 1f);
            }
            right = new Hand(hand, axis);
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

            // ---- the stock is the CO2 bottle: a round-ended painted bottle from the butt pad to a brass valve that
            //      screws into the back of the receiver
            //      (a fat, short bottle: its domed end is the butt, with a rubber ring round it)
            const float BottleY = 0.01f, BottleR = 0.034f;
            _bottleMesh = new ProfileMesh()
                .Tube(new Vector3(0f, BottleY, StockCut + 0.004f), new Vector3(0f, BottleY, StockCut + 0.012f), BottleR * 0.6f, 16, BottleR * 0.88f)
                .Tube(new Vector3(0f, BottleY, StockCut + 0.012f), new Vector3(0f, BottleY, StockCut + 0.024f), BottleR * 0.88f, 16, BottleR)
                .Tube(new Vector3(0f, BottleY, StockCut + 0.024f), new Vector3(0f, BottleY, 0.122f), BottleR, 16)
                .Tube(new Vector3(0f, BottleY, 0.122f), new Vector3(0f, BottleY, 0.142f), BottleR, 16, BottleR * 0.4f)
                .ToMesh("RifleBottle");
            _brassMesh = new ProfileMesh()
                .Tube(new Vector3(0f, BottleY, 0.14f), new Vector3(0f, BottleY, 0.156f), 0.0115f, 10)
                .Tube(new Vector3(0f, BottleY - 0.012f, 0.148f), new Vector3(0f, BottleY - 0.022f, 0.148f), 0.0055f, 8)   // the gauge port
                .ToMesh("RifleValve");

            // ---- the filler on the dust cover: a brass collar and a short flared mouth, dark inside, with a steel lid
            //      hinged at its back; the handful of salt it is filled from (fist space: the salt runs out along +Z),
            //      and a thin stream of salt (a unit length up +Y, stretched to fit)
            _fillerMesh = new ProfileMesh()
                .Tube(new Vector3(0f, 0.079f, FillerTop.z), new Vector3(0f, 0.091f, FillerTop.z), 0.012f, 12)
                .Tube(new Vector3(0f, 0.091f, FillerTop.z), new Vector3(0f, FillerTop.y, FillerTop.z), 0.012f, 12, 0.0195f)
                .ToMesh("RifleFiller");
            _fillerHoleMesh = new ProfileMesh()
                .Tube(new Vector3(0f, FillerTop.y - 0.002f, FillerTop.z), new Vector3(0f, FillerTop.y + 0.0004f, FillerTop.z), 0.016f, 12)
                .ToMesh("RifleFillerHole");
            _lidMesh = new ProfileMesh()
                .Tube(new Vector3(0f, 0.0004f, FillerTop.z - LidHinge.z), new Vector3(0f, 0.0044f, FillerTop.z - LidHinge.z), 0.0215f, 14)
                .Tube(new Vector3(0f, 0.0044f, FillerTop.z - LidHinge.z + 0.012f), new Vector3(0f, 0.0084f, FillerTop.z - LidHinge.z + 0.012f), 0.004f, 8)   // a little tab
                .ToMesh("RifleLid");
            // a handful of salt: a white lump in the (gloved) fist, bulging out where the salt runs from
            _pouchMesh = new ProfileMesh()
                .Extrude(Zy(-0.018f, -0.012f, 0.004f, -0.02f, 0.03f, -0.014f, 0.04f, 0f, 0.03f, 0.014f, 0.004f, 0.02f, -0.018f, 0.012f), 0.019f, 0.008f)
                .ToMesh("SaltHandful");
            _streamMesh = new ProfileMesh().Tube(Vector3.zero, Vector3.up, 0.0028f, 6).ToMesh("SaltStream");

            // ---- wood: the pistol grip, the handguard below the barrel and the cover above it
            var wood = new ProfileMesh();
            wood.Extrude(Zy(0.206f, 0.014f, 0.244f, 0.014f, 0.226f, -0.066f, 0.19f, -0.066f), 0.016f, 0.004f);
            wood.Extrude(Zy(0.385f, 0.06f, 0.385f, 0.02f, 0.4f, 0.012f, 0.515f, 0.016f, 0.53f, 0.028f, 0.53f, 0.058f), 0.024f, 0.005f);
            wood.Extrude(Zy(0.398f, 0.058f, 0.505f, 0.058f, 0.505f, 0.07f, 0.497f, 0.08f, 0.41f, 0.08f, 0.398f, 0.072f), 0.016f, 0.004f);
            _woodMesh = wood.ToMesh("RifleWood");

            // ---- steel: receiver, sights, barrel and its parts, trigger and guard, the selector and charging handle
            var steel = new ProfileMesh();
            steel.Extrude(Zy(0.152f, 0.012f, 0.362f, 0.012f, 0.362f, 0.066f, 0.17f, 0.066f, 0.152f, 0.056f), 0.019f, 0.003f);
            steel.Extrude(Zy(0.36f, 0.056f, 0.398f, 0.056f, 0.398f, 0.072f, 0.36f, 0.079f), 0.014f, 0.002f);           // rear sight block
            steel.Extrude(Zy(0.364f, 0.079f, 0.39f, 0.074f, 0.39f, SightLine - 0.008f, 0.366f, SightLine - 0.005f), 0.0115f, 0.001f);   // rear sight leaf
            for (int side = -1; side <= 1; side += 2)                                                                     // the ears either side of its notch (wider than the post looks)
                steel.Extrude(Zy(0.364f, 0.079f, 0.373f, 0.079f, 0.373f, SightLine + 0.0005f, 0.364f, SightLine + 0.0005f), 0.0029f, 0.0008f, side * 0.0087f);
            steel.Tube(new Vector3(0f, BoreY, 0.36f), new Vector3(0f, BoreY, 0.59f), 0.0085f);                           // barrel
            steel.Tube(new Vector3(0f, 0.068f, 0.5f), new Vector3(0f, 0.068f, 0.528f), 0.0075f);                        // gas tube (ends at the handguard, so it is not run out with the barrel)
            steel.Extrude(Zy(0.535f, 0.036f, 0.56f, 0.036f, 0.56f, 0.078f, 0.535f, 0.078f), 0.012f, 0.002f);           // gas block
            steel.Tube(new Vector3(0f, BoreY, 0.565f), new Vector3(0f, BoreY, 0.59f), 0.0125f);                          // front sight base
            steel.Extrude(Zy(0.568f, 0.05f, 0.588f, 0.05f, 0.585f, SightLine, 0.571f, SightLine), 0.0075f, 0.0015f);   // front sight post
            // its tip painted salt white, easy to find in the notch
            _sightTipMesh = new ProfileMesh()
                .Extrude(Zy(0.5705f, SightLine - 0.006f, 0.5855f, SightLine - 0.006f, 0.5855f, SightLine + 0.0005f, 0.5705f, SightLine + 0.0005f), 0.0078f, 0.0015f)
                .ToMesh("RifleSightTip");
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
            // a rubber ring round the bottle's end, where it rests against the body
            _rubberMesh = new ProfileMesh()
                .Tube(new Vector3(0f, 0.01f, StockCut), new Vector3(0f, 0.01f, StockCut + 0.014f), 0.03f, 16, 0.0335f)
                .ToMesh("RifleButtRing");

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
