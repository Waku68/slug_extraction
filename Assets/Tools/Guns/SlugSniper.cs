using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// A bolt-action sniper rifle in the hunting style: a one-piece wooden stock that runs from the butt to under the
    /// barrel, with a pistol grip; a round steel receiver, a long tapered barrel and a scope on two rings. It fires a
    /// chunk of rock salt.
    /// The right hand holds the pistol grip and works the bolt after every shot (up, back, forward, down); to reload it
    /// opens the bolt and presses chunks into the action one at a time. The left hand rests under the fore-end.
    /// </summary>
    public class SlugSniper : SlugGunModel
    {
        const float Bore = 0.05f, Length = 0.805f, ScopeY = 0.095f;
        /// <summary>How much the stock is cut short (m).</summary>
        const float StockCut = 0.07f;
        protected override float BodyScale => 0.75f;
        protected override float WidthScale => 1.05f;      // short, but as thick as ever: a chunky little gun
        protected override float HeightScale => 0.85f;
        protected override float BarrelSplit => 0.6f;        // in front of the fore-end
        protected override float BarrelExtra => 0.14f;
        static readonly Vector3 GripLocal = new Vector3(0.004f, -0.03f, 0.224f);
        static readonly Vector3 GripAxis = new Vector3(0f, -1f, -0.35f).normalized;
        static readonly Vector3 LeftGripLocal = new Vector3(-0.004f, -0.014f, 0.5f);
        static readonly Vector3 BoltPivot = new Vector3(0f, Bore, 0f);
        /// <summary>The bolt knob, in bolt space (the bolt turns about the bore and slides along it).</summary>
        static readonly Vector3 KnobBolt = new Vector3(0.04f, -0.026f, 0.28f);
        static readonly Vector3 ActionTop = new Vector3(0f, 0.09f, 0.3f), ActionIn = new Vector3(0f, 0.066f, 0.3f);
        const float BoltLift = 65f;

        protected override float RawBoreY => Bore;
        protected override Vector3 RawMuzzle => new Vector3(0f, Bore, Length);
        protected override Vector3 RawRestGrip => GripLocal;
        protected override Vector3 RawReloadFocus => new Vector3(0f, 0.06f, 0.29f);
        protected override Vector3 RawScopeEye => new Vector3(0f, ScopeY, 0.128f);
        protected override float RawButtZ => StockCut;

        static Mesh _woodMesh, _steelMesh, _rubberMesh, _boltMesh, _glassMesh, _chunkMesh, _bottleMesh, _brassMesh;
        static Material _rockSalt;

        Transform _bolt, _handChunk;

        protected override void Build(bool castShadows)
        {
            EnsureMeshes();
            Part(transform, "Wood", _woodMesh, Wood, castShadows);
            Part(transform, "Steel", _steelMesh, Steel, castShadows);
            Part(transform, "Rubber", _rubberMesh, Rubber, castShadows);
            Part(transform, "Lenses", _glassMesh, Glass, false);
            Part(transform, "Bottle", _bottleMesh, Bottle, castShadows);
            Part(transform, "Brass", _brassMesh, Brass, castShadows);
            _bolt = Moving(Pivot(transform, "Bolt", BoltPivot));
            Part(_bolt, "BoltSteel", _boltMesh, Cover, castShadows);
            _handChunk = Moving(Part(transform, "HandChunk", _chunkMesh, _rockSalt, castShadows));
            _handChunk.gameObject.SetActive(false);
        }

        Vector3 Knob(float angle, float slide) => BoltPivot + new Vector3(0f, 0f, -slide) + Quaternion.Euler(0f, 0f, angle) * KnobBolt;

        /// <summary>
        /// After a shot the right hand leaves the grip for the bolt knob: lift, pull back, push forward, press down, back to
        /// the grip. Reloading: open the bolt, then fetch a chunk from the side and press it down into the action, again
        /// and again; close the bolt to finish.
        /// </summary>
        protected override void RawPose(GunHandling h, GunSettings s, out Hand right, out Hand left)
        {
            float travel = s.actionTravel, angle = 0f, slide = 0f;
            Vector3 hand = GripLocal, axis = GripAxis;
            bool holding = false;
            Vector3 knobAxis = Vector3.forward;
            float u = h.StageProgress;
            if (h.Cycling)
            {
                float c = h.Cycle;
                angle = BoltLift * Bump(c, 0.15f, 0.3f, 0.68f, 0.8f);
                slide = travel * Bump(c, 0.3f, 0.5f, 0.5f, 0.68f);
                float on = Bump(c, 0f, 0.15f, 0.8f, 1f);
                hand = Vector3.Lerp(GripLocal, Knob(angle, slide), on);
                axis = Vector3.Slerp(GripAxis, knobAxis, on);
            }
            else switch (h.ReloadStage)
            {
                case GunHandling.Stage.Open:
                {
                    angle = BoltLift * Smooth((u - 0.25f) / 0.25f);
                    slide = travel * Smooth((u - 0.5f) / 0.25f);
                    Vector3 knob = Knob(angle, slide), pouch = knob + s.magazineAway;
                    hand = u < 0.75f ? Vector3.Lerp(GripLocal, knob, Smooth(u / 0.25f)) : Vector3.Lerp(knob, pouch, Smooth((u - 0.75f) / 0.25f));
                    axis = Vector3.Slerp(GripAxis, knobAxis, Smooth(u / 0.25f));
                    break;
                }
                case GunHandling.Stage.Insert:
                {
                    angle = BoltLift; slide = travel;
                    Vector3 pouch = Knob(angle, slide) + s.magazineAway;
                    const float at = GunHandling.InsertAt;
                    if (u < 0.4f) hand = Vector3.Lerp(pouch, ActionTop, Smooth(u / 0.4f));
                    else if (u < at) hand = Vector3.Lerp(ActionTop, ActionIn, Smooth((u - 0.4f) / (at - 0.4f)));
                    else hand = Vector3.Lerp(ActionIn, pouch, Smooth((u - at) / (1f - at)));
                    axis = Vector3.down;
                    holding = u < at;
                    break;
                }
                case GunHandling.Stage.Close:
                {
                    angle = BoltLift * (1f - Smooth((u - 0.55f) / 0.2f));
                    slide = travel * (1f - Smooth((u - 0.25f) / 0.3f));
                    Vector3 knob = Knob(angle, slide), pouch = Knob(BoltLift, travel) + s.magazineAway;
                    hand = u < 0.25f ? Vector3.Lerp(pouch, knob, Smooth(u / 0.25f)) : Vector3.Lerp(knob, GripLocal, Smooth((u - 0.75f) / 0.25f));
                    axis = Vector3.Slerp(knobAxis, GripAxis, Smooth((u - 0.75f) / 0.25f));
                    break;
                }
            }
            Place(_bolt, BoltPivot + new Vector3(0f, 0f, -slide));
            _bolt.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (_handChunk.gameObject.activeSelf != holding) _handChunk.gameObject.SetActive(holding);
            // the chunk peeks out under the fist as it is pressed down into the action
            if (holding) Place(_handChunk, hand + new Vector3(0f, -0.016f, 0f));
            right = new Hand(hand, axis);
            left = new Hand(LeftGripLocal, Vector3.forward);
        }

        static void EnsureMeshes()
        {
            if (_woodMesh != null) return;
            const float BoreY = Bore;
            _rockSalt = Mat(Shader.Find("Universal Render Pipeline/Lit"), new Color(0.96f, 0.82f, 0.85f), 0f, 0.5f);

            // ---- wood: one piece from the butt to under the barrel, with a comb, a pistol grip and a long fore-end
            _woodMesh = new ProfileMesh()
                .Extrude(CutStock(Zy(0f, 0.036f, 0.16f, 0.048f, 0.2f, 0.036f, 0.52f, 0.036f, 0.585f, 0.031f, 0.59f, 0.016f, 0.56f, 0.002f,
                            0.33f, -0.006f, 0.29f, -0.012f, 0.255f, -0.016f, 0.242f, -0.052f, 0.204f, -0.054f, 0.198f, -0.018f,
                            0.15f, -0.024f, 0.07f, -0.036f, -0.004f, -0.054f), StockCut, 0.19f), 0.02f, 0.005f)
                .ToMesh("SniperWood");

            // ---- the CO2 bottle under the fore-end, along the barrel (an air rifle's reservoir), a brass gauge on its
            //      front cap
            const float BottleY = -0.032f, BottleR = 0.019f;
            _bottleMesh = new ProfileMesh()
                .Tube(new Vector3(0f, BottleY, 0.3f), new Vector3(0f, BottleY, 0.312f), BottleR * 0.6f, 12, BottleR)
                .Tube(new Vector3(0f, BottleY, 0.312f), new Vector3(0f, BottleY, 0.55f), BottleR, 12)
                .ToMesh("SniperBottle");
            _brassMesh = new ProfileMesh()
                .Tube(new Vector3(0f, BottleY, 0.55f), new Vector3(0f, BottleY, 0.566f), BottleR * 0.9f, 12)
                .Tube(new Vector3(0f, BottleY, 0.566f), new Vector3(0f, BottleY, 0.571f), BottleR * 0.62f, 12)
                .ToMesh("SniperGauge");

            // ---- steel: the round receiver, the tapered barrel and its crown, trigger and guard, the scope on two rings
            var steel = new ProfileMesh();
            steel.Tube(new Vector3(0f, BoreY, 0.2f), new Vector3(0f, BoreY, 0.37f), 0.0155f, 12);
            steel.Tube(new Vector3(0f, BoreY, 0.37f), new Vector3(0f, BoreY, 0.79f), 0.011f, 10, 0.0082f);
            steel.Tube(new Vector3(0f, BoreY, 0.785f), new Vector3(0f, BoreY, Length), 0.0098f, 10);
            TriggerGroup(steel, 0.279f, -0.006f);
            steel.Tube(new Vector3(0f, ScopeY, 0.17f), new Vector3(0f, ScopeY, 0.41f), 0.0125f, 12);           // scope tube
            steel.Tube(new Vector3(0f, ScopeY, 0.128f), new Vector3(0f, ScopeY, 0.175f), 0.0165f, 12, 0.0125f); // eyepiece
            steel.Tube(new Vector3(0f, ScopeY, 0.4f), new Vector3(0f, ScopeY, 0.47f), 0.0125f, 12, 0.0205f);    // objective bell
            steel.Tube(new Vector3(0f, ScopeY + 0.011f, 0.29f), new Vector3(0f, ScopeY + 0.024f, 0.29f), 0.0075f, 8);   // turrets
            steel.Tube(new Vector3(0.011f, ScopeY, 0.29f), new Vector3(0.024f, ScopeY, 0.29f), 0.0075f, 8);
            foreach (float z in new[] { 0.215f, 0.35f })                                                       // rings
                steel.Extrude(Zy(z, BoreY + 0.01f, z + 0.016f, BoreY + 0.01f, z + 0.016f, ScopeY + 0.004f, z, ScopeY + 0.004f), 0.009f, 0.002f);
            _steelMesh = steel.ToMesh("SniperSteel");
            _rubberMesh = new ProfileMesh()
                .Extrude(Shift(Zy(-0.012f, 0.038f, -0.016f, -0.056f, -0.004f, -0.056f, 0f, 0.038f), StockCut, 0f), 0.021f, 0.003f)
                .ToMesh("SniperButtPad");
            var glass = new ProfileMesh();
            glass.Tube(new Vector3(0f, ScopeY, 0.468f), new Vector3(0f, ScopeY, 0.4715f), 0.0185f, 12);
            glass.Tube(new Vector3(0f, ScopeY, 0.1265f), new Vector3(0f, ScopeY, 0.13f), 0.014f, 12);
            _glassMesh = glass.ToMesh("SniperLenses");

            // ---- the bolt (bolt space: about the bore): the shroud behind the receiver and the handle with its knob
            var bolt = new ProfileMesh();
            bolt.Tube(new Vector3(0f, 0f, 0.165f), new Vector3(0f, 0f, 0.205f), 0.0118f, 10);
            bolt.Tube(new Vector3(0.01f, -0.004f, 0.29f), new Vector3(0.034f, -0.021f, 0.282f), 0.0042f, 8);
            bolt.Tube(new Vector3(0.031f, -0.019f, 0.283f), new Vector3(0.045f, -0.029f, 0.278f), 0.0088f, 10);
            _boltMesh = bolt.ToMesh("SniperBolt");

            // ---- a chunk of rock salt for the hand to load: a small faceted lump
            _chunkMesh = new ProfileMesh()
                .Extrude(Zy(-0.009f, -0.006f, 0.0f, -0.011f, 0.01f, -0.005f, 0.008f, 0.008f, -0.004f, 0.01f), 0.0075f, 0.003f)
                .ToMesh("RockSaltChunk");
        }
    }
}
