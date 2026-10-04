using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// A pump shotgun: a straight wooden stock and a grooved wooden pump on a short steel receiver, a barrel with a
    /// tube magazine under it, and a loading port underneath. It fires a handful of coarse salt from shells wrapped in
    /// a leaf, with a dab of salt showing at the top.
    /// The right hand holds the wrist of the stock and feeds shells into the port one at a time; the left hand rides the
    /// pump, racking it back and forth after every shot (out flies the spent shell).
    /// </summary>
    public class SlugShotgun : SlugGunModel
    {
        const float Bore = 0.046f, Length = 0.6f;
        static readonly Vector3 LeftGripLocal = new Vector3(-0.004f, -0.013f, 0.44f);
        static readonly Vector3 WristLocal = new Vector3(0.004f, 0.02f, 0.178f);
        static readonly Vector3 WristAxis = new Vector3(0f, 0.25f, 1f).normalized;
        static readonly Vector3 PortLocal = new Vector3(0f, -0.018f, 0.266f), PushedLocal = new Vector3(0f, -0.002f, 0.286f);
        /// <summary>The ejection port on the right side, where spent shells fly out.</summary>
        public static readonly Vector3 EjectLocal = new Vector3(0.024f, 0.042f, 0.272f);

        public override float BoreY => Bore;
        public override Vector3 MuzzleLocal => new Vector3(0f, Bore, Length);
        public override Vector3 RestGripLocal => WristLocal;
        public override Vector3 ReloadFocusLocal => new Vector3(0f, 0f, 0.275f);

        static Mesh _woodMesh, _steelMesh, _coverMesh, _rubberMesh, _pumpMesh, _grooveMesh;
        static Mesh _shellBody, _shellBase, _shellTop;
        static Material _leaf, _darkWood;

        Transform _pump, _handShell;

        protected override void Build(bool castShadows)
        {
            EnsureMeshes();
            Part(transform, "Wood", _woodMesh, Wood, castShadows);
            Part(transform, "Steel", _steelMesh, Steel, castShadows);
            Part(transform, "Cover", _coverMesh, Cover, castShadows);
            Part(transform, "Rubber", _rubberMesh, Rubber, castShadows);
            _pump = Pivot(transform, "Pump", Vector3.zero);
            Part(_pump, "PumpWood", _pumpMesh, Wood, castShadows);
            Part(_pump, "PumpGrooves", _grooveMesh, _darkWood, castShadows);
            _handShell = Shell(transform, castShadows, false).transform;
            _handShell.name = "HandShell";
            _handShell.gameObject.SetActive(false);
        }

        /// <summary>
        /// The pump racks back and forth after a shot. Reloading, the right hand goes from the wrist down to the side for
        /// a shell, brings it up under the port, pushes it in, and goes back for the next.
        /// </summary>
        public override void Pose(GunHandling h, GunSettings s, out Hand right, out Hand left)
        {
            float pump = h.Cycling ? Bump(h.Cycle, 0f, 0.4f, 0.5f, 0.85f) : 0f;
            Vector3 back = new Vector3(0f, 0f, -s.actionTravel * pump);
            _pump.localPosition = back;
            left = new Hand(LeftGripLocal + back, Vector3.forward);

            Vector3 pouch = WristLocal + s.magazineAway;
            Vector3 hand = WristLocal, axis = WristAxis;
            bool holding = false;
            float u = h.StageProgress;
            switch (h.ReloadStage)
            {
                case GunHandling.Stage.Open:
                    hand = Vector3.Lerp(WristLocal, pouch, Smooth(u));
                    axis = Vector3.Slerp(WristAxis, Vector3.forward, Smooth(u));
                    break;
                case GunHandling.Stage.Insert:
                    const float at = GunHandling.InsertAt;
                    if (u < 0.45f) hand = Vector3.Lerp(pouch, PortLocal, Smooth(u / 0.45f));
                    else if (u < at) hand = Vector3.Lerp(PortLocal, PushedLocal, Smooth((u - 0.45f) / (at - 0.45f)));
                    else hand = Vector3.Lerp(PushedLocal, pouch, Smooth((u - at) / (1f - at)));
                    axis = Vector3.forward;
                    holding = u < at;
                    break;
                case GunHandling.Stage.Close:
                    hand = Vector3.Lerp(pouch, WristLocal, Smooth(u));
                    axis = Vector3.Slerp(Vector3.forward, WristAxis, Smooth(u));
                    break;
            }
            if (_handShell.gameObject.activeSelf != holding) _handShell.gameObject.SetActive(holding);
            // the shell pokes out of the front of the fist, pointing forward into the port
            if (holding) _handShell.localPosition = hand + new Vector3(0f, 0.012f, -0.004f);
            right = new Hand(hand, axis);
        }

        /// <summary>A coarse-salt shell along +Z from its base: a leaf wrap, a steel base and a dab of salt on top.</summary>
        public static GameObject Shell(Transform parent, bool castShadows, bool physics)
        {
            EnsureMeshes();
            var go = new GameObject("Shell");
            go.transform.SetParent(parent, false);
            Part(go.transform, "Wrap", _shellBody, _leaf, castShadows);
            Part(go.transform, "Base", _shellBase, Cover, castShadows);
            Part(go.transform, "Salt", _shellTop, Salt, castShadows);
            if (physics)
            {
                go.layer = 2;                                       // Ignore Raycast: never blocks a shot or the aim
                foreach (Transform c in go.transform) c.gameObject.layer = 2;
                var col = go.AddComponent<CapsuleCollider>();
                col.direction = 2;
                col.radius = 0.01f;
                col.height = 0.036f;
                col.center = new Vector3(0f, 0f, 0.018f);
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.01f;
            }
            return go;
        }

        static void EnsureMeshes()
        {
            if (_woodMesh != null) return;
            const float BoreY = Bore;
            _leaf = Mat(Shader.Find("Universal Render Pipeline/Lit"), new Color(0.36f, 0.56f, 0.25f), 0f, 0.25f);
            _darkWood = new Material(Wood) { name = "DarkWood", hideFlags = HideFlags.DontSave };
            _darkWood.SetColor("_BaseColor", new Color(0.46f, 0.25f, 0.12f));

            // ---- wood: a straight stock with a slim wrist
            _woodMesh = new ProfileMesh()
                .Extrude(Zy(0f, 0.034f, -0.004f, -0.05f, 0.07f, -0.034f, 0.15f, -0.012f, 0.2f, 0f, 0.205f, 0.012f, 0.205f, 0.054f, 0.15f, 0.05f), 0.021f, 0.005f)
                .ToMesh("ShotgunWood");

            // ---- steel: receiver, barrel with its bead, the tube magazine and its clamp, trigger and guard
            var steel = new ProfileMesh();
            steel.Extrude(Zy(0.2f, 0f, 0.33f, 0f, 0.33f, 0.066f, 0.215f, 0.066f, 0.2f, 0.058f), 0.019f, 0.003f);
            steel.Tube(new Vector3(0f, BoreY, 0.33f), new Vector3(0f, BoreY, Length), 0.0115f);
            steel.Tube(new Vector3(0f, BoreY, 0.588f), new Vector3(0f, BoreY, Length), 0.0128f);
            steel.Tube(new Vector3(0f, BoreY + 0.01f, 0.59f), new Vector3(0f, BoreY + 0.017f, 0.59f), 0.003f, 6);
            steel.Tube(new Vector3(0f, 0.017f, 0.33f), new Vector3(0f, 0.017f, 0.575f), 0.0095f);
            steel.Tube(new Vector3(0f, 0.017f, 0.57f), new Vector3(0f, 0.017f, 0.586f), 0.0108f);
            steel.Extrude(Zy(0.548f, 0.006f, 0.562f, 0.006f, 0.562f, 0.058f, 0.548f, 0.058f), 0.0125f, 0.002f);
            TriggerGroup(steel, 0.214f, 0f);
            _steelMesh = steel.ToMesh("ShotgunSteel");
            _coverMesh = new ProfileMesh()
                .Extrude(Zy(0.212f, 0.062f, 0.33f, 0.062f, 0.33f, 0.071f, 0.222f, 0.071f), 0.014f, 0.003f)
                .ToMesh("ShotgunTop");

            // ---- dark: the butt pad, the loading port underneath and the ejection port on the right
            var rubber = new ProfileMesh();
            rubber.Extrude(Zy(-0.01f, 0.036f, -0.014f, -0.052f, -0.003f, -0.052f, 0.001f, 0.036f), 0.022f, 0.003f);
            rubber.Extrude(Zy(0.24f, -0.003f, 0.31f, -0.003f, 0.31f, 0.003f, 0.24f, 0.003f), 0.012f, 0f);
            rubber.Extrude(Zy(0.245f, 0.028f, 0.3f, 0.028f, 0.3f, 0.055f, 0.245f, 0.055f), 0.001f, 0f, 0.0195f);
            _rubberMesh = rubber.ToMesh("ShotgunDark");

            // ---- the pump: a chunky wooden fore-end around the tube, with grooves (darker wood) for the fingers it lacks
            _pumpMesh = new ProfileMesh()
                .Extrude(Zy(0.36f, 0.002f, 0.366f, -0.006f, 0.494f, -0.006f, 0.5f, 0.002f, 0.5f, 0.034f, 0.494f, 0.04f, 0.366f, 0.04f, 0.36f, 0.034f), 0.022f, 0.006f)
                .ToMesh("ShotgunPump");
            var grooves = new ProfileMesh();
            // thin bands the same shape as the pump, a hair proud of it, in darker wood, so they read as grooves
            for (int i = 0; i < 4; i++)
            {
                float z = 0.393f + i * 0.024f;
                grooves.Extrude(Zy(z, -0.0062f, z + 0.004f, -0.0062f, z + 0.004f, 0.0402f, z, 0.0402f), 0.0222f, 0.0062f);
            }
            _grooveMesh = grooves.ToMesh("ShotgunGrooves");

            // ---- a shell (along +Z from its base)
            _shellBody = new ProfileMesh().Tube(new Vector3(0f, 0f, 0.006f), new Vector3(0f, 0f, 0.034f), 0.0095f, 10).ToMesh("ShellWrap");
            _shellBase = new ProfileMesh().Tube(Vector3.zero, new Vector3(0f, 0f, 0.007f), 0.0102f, 10).ToMesh("ShellBase");
            _shellTop = new ProfileMesh().Tube(new Vector3(0f, 0f, 0.033f), new Vector3(0f, 0f, 0.037f), 0.0082f, 10).ToMesh("ShellSalt");
        }
    }
}
