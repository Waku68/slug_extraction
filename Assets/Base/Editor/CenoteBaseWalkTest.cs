using System.Collections.Generic;
using System.Text;
using SlugMove;
using UnityEditor;
using UnityEngine;

namespace SlugBase
{
    /// <summary>
    /// Crawls the slug of the open CenoteBase scene along the main routes (hall, ramp, gallery and the upper rooms,
    /// the deck, the sea mouth and pier, the ground-floor rooms, in and out of the pool) by stepping
    /// <see cref="SlugMoveController.Simulate"/> in the editor, and logs where it gets stuck.
    /// Menu: Slug/CenoteBase Walk Test. Waypoints are in the Blender frame (x, y = north, z = up) like the generator.
    /// </summary>
    public static class CenoteBaseWalkTest
    {
        const float Dt = 1f / 60f;
        const float ReachDist = 0.45f;
        const float StuckTime = 2.5f;

        // the same shapes as cenote_base_gen.py
        const float EllipseX = 1.18f, EllipseY = 0.88f;
        static readonly float[,] HallProfile = { { 2.0f, 4.4f }, { 3.0f, 4.8f }, { 4.6f, 5.0f }, { 6.0f, 4.8f }, { 7.0f, 4.0f } };

        static float HallRadius(float z)
        {
            if (z <= HallProfile[0, 0]) return HallProfile[0, 1];
            for (int i = 1; i < HallProfile.GetLength(0); i++)
                if (z <= HallProfile[i, 0])
                {
                    float t = (z - HallProfile[i - 1, 0]) / (HallProfile[i, 0] - HallProfile[i - 1, 0]);
                    return Mathf.Lerp(HallProfile[i - 1, 1], HallProfile[i, 1], t);
                }
            return HallProfile[HallProfile.GetLength(0) - 1, 1];
        }

        /// <summary>A point at a clock position (12 = north), radius in ellipse units, height z, as a Unity position.</summary>
        static Vector3 Clock(float clock, float r, float z)
        {
            float th = (3f - clock) * 30f * Mathf.Deg2Rad;
            return B(Mathf.Cos(th) * r * EllipseX, Mathf.Sin(th) * r * EllipseY, z);
        }

        static Vector3 B(float x, float y, float z) => new Vector3(x, z, y);

        static Dictionary<string, List<Vector3>> Routes()
        {
            var r = new Dictionary<string, List<Vector3>>();
            r["hall_and_pool"] = new List<Vector3> { B(-0.2f, -1.6f, 2f), B(-3.4f, -0.8f, 2f), B(-3.6f, 2.0f, 2f), B(-0.2f, 2.8f, 2f), B(1.8f, 1.0f, 1.5f), B(1.8f, 0f, 1.45f), B(1.8f, -2.2f, 2f), B(0.6f, -2.0f, 2f) };
            var ramp = new List<Vector3> { Clock(7.4f, 3.6f, 2f), Clock(7.8f, HallRadius(2f) - 0.54f, 2f) };
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f, z = 2f + 2.6f * t;
                ramp.Add(Clock(8f + 3f * t, HallRadius(z) - 0.54f, z));
            }
            ramp.Add(Clock(11.4f, 4.25f, 4.6f));
            r["ramp_up"] = ramp;
            var gal = new List<Vector3>();
            foreach (var c in new[] { 11.5f, 12.0f }) gal.Add(Clock(c, 4.25f, 4.6f));
            gal.Add(Clock(12.0f, 5.9f, 4.6f)); gal.Add(Clock(12.0f, 4.25f, 4.6f));
            foreach (var room in new[] { 1.5f, 3.0f, 4.3f })
            {
                gal.Add(Clock(room - 0.5f, 4.25f, 4.6f)); gal.Add(Clock(room, 4.25f, 4.6f));
                gal.Add(Clock(room, 5.9f, 4.6f)); gal.Add(Clock(room, 4.25f, 4.6f));
            }
            gal.Add(Clock(5.0f, 4.25f, 4.6f)); gal.Add(Clock(5.5f, 4.25f, 4.6f)); gal.Add(Clock(5.8f, 4.4f, 4.6f));
            gal.Add(B(1.15f, -4.6f, 4.6f)); gal.Add(B(1.3f, -6.6f, 4.6f)); gal.Add(B(0.2f, -7.6f, 4.6f));
            r["gallery_rooms_deck"] = gal;
            r["sea_mouth_pier"] = new List<Vector3> { B(-1.0f, -2.6f, 2f), B(-2.5f, -3.8f, 2f), B(-2.5f, -5.5f, 2f), B(-2.5f, -8.5f, 2f), B(-2.5f, -11.6f, 2f) };
            // home from the bird's basket on the deck: out of the basket, along the gallery to the dive board at 3:30, into the pool, to the map table
            r["deck_basket_home"] = new List<Vector3> { B(4.4f, -8.5f, 4.62f), B(2.9f, -8.6f, 4.6f), B(2.0f, -6.7f, 4.6f), B(1.2f, -6.2f, 4.6f), B(1.15f, -4.4f, 4.6f), Clock(5.6f, 4.25f, 4.6f), Clock(4.6f, 4.25f, 4.6f), Clock(3.5f, 4.25f, 4.6f), Clock(3.5f, 2.7f, 4.6f), B(0.2f, -1.2f, 2f), B(-1.6f, -0.9f, 2f) };
            r["ground_rooms"] = new List<Vector3> { B(0.8f, -2.2f, 2f), Clock(5.4f, 4.0f, 2f), Clock(5.4f, 5.6f, 2f), Clock(5.4f, 4.0f, 2f), Clock(4.0f, 4.0f, 2f), Clock(4.0f, 5.6f, 2f), Clock(4.0f, 4.0f, 2f) };
            return r;
        }

        [MenuItem("Slug/CenoteBase Walk Test")]
        public static void Run()
        {
            var move = Object.FindAnyObjectByType<SlugMoveController>();
            if (!move) { Debug.LogWarning("[WalkTest] no slug in the open scene"); return; }
            var tr = move.transform;
            var cc = move.GetComponent<CharacterController>();
            var rig = move.cameraRig;
            Vector3 home = tr.position; Quaternion homeRot = tr.rotation;
            bool wasDriven = move.driven;
            move.driven = true; move.cameraRig = null;
            Physics.SyncTransforms();
            var log = new StringBuilder("[WalkTest]\n");
            try
            {
                foreach (var kv in Routes())
                {
                    var pts = kv.Value;
                    Teleport(tr, cc, pts[0] + Vector3.up * 0.05f);
                    for (int i = 0; i < 5; i++) move.Simulate(Dt, new Vector2(0f, -1f), false);   // lets go of any climb left from the last route
                    Teleport(tr, cc, pts[0] + Vector3.up * 0.05f);
                    for (int i = 0; i < 30; i++) move.Simulate(Dt, Vector2.zero, false);
                    int reached = 0;
                    string fail = null;
                    float maxDrop = 0f;
                    for (int w = 1; w < pts.Count && fail == null; w++)
                    {
                        float since = 0f, best = float.MaxValue, total = 0f;
                        while (true)
                        {
                            Vector3 to = pts[w] - tr.position;
                            Vector3 flat = new Vector3(to.x, 0f, to.z);
                            if (flat.magnitude < ReachDist && Mathf.Abs(to.y) < 0.6f) { reached++; break; }
                            tr.rotation = Quaternion.LookRotation(flat.normalized);
                            float yBefore = tr.position.y;
                            move.Simulate(Dt, new Vector2(0f, 1f), false);
                            maxDrop = Mathf.Max(maxDrop, yBefore - tr.position.y);
                            total += Dt; since += Dt;
                            float d = flat.magnitude;
                            if (d < best - 0.05f) { best = d; since = 0f; }
                            if (since > StuckTime || total > 40f)
                            {
                                fail = $"stuck going to #{w} {Fmt(pts[w])} at {Fmt(tr.position)} (left {d:0.00} m, dy {to.y:0.00})";
                                break;
                            }
                            if (tr.position.y < 0.8f) { fail = $"fell into the sea going to #{w} at {Fmt(tr.position)}"; break; }
                        }
                    }
                    log.Append($"{kv.Key}: {reached}/{pts.Count - 1} waypoints" + (fail != null ? " — " + fail : " — OK") + "\n");
                }
                log.Append(LiftTest(move, tr, cc));
            }
            finally
            {
                move.driven = wasDriven; move.cameraRig = rig;
                Teleport(tr, cc, home); tr.rotation = homeRot;
            }
            Debug.Log(log.ToString());
        }

        /// <summary>Crawls onto the low board of the lift, lets it carry the slug up, then crawls off onto the gallery.</summary>
        static string LiftTest(SlugMoveController move, Transform tr, CharacterController cc)
        {
            var lift = Object.FindAnyObjectByType<CenoteLift>();
            if (!lift) return "lift: none\n";
            var low = lift.boards[1 - lift.upper];
            Vector3 start = low.position + (low.position - new Vector3(0f, low.position.y, 0f)).normalized * -1.2f;
            start.y = 2.05f;
            Teleport(tr, cc, start);
            for (int i = 0; i < 5; i++) move.Simulate(Dt, new Vector2(0f, -1f), false);
            float t = 0f;
            while (t < 4f)
            {
                Vector3 to = low.position - tr.position; to.y = 0f;
                if (to.magnitude < 0.15f) break;
                tr.rotation = Quaternion.LookRotation(to.normalized);
                move.Simulate(Dt, new Vector2(0f, 1f), false); Physics.SyncTransforms(); lift.Step(Dt); t += Dt;
            }
            float ride = 0f;
            while (ride < 8f) { move.Simulate(Dt, Vector2.zero, false); Physics.SyncTransforms(); lift.Step(Dt); ride += Dt; if (!lift.Moving && tr.position.y > 4.4f) break; }
            float topY = tr.position.y;
            // off the board, outward onto the gallery
            Vector3 outward = new Vector3(tr.position.x, 0f, tr.position.z).normalized;
            for (int i = 0; i < 120; i++) { tr.rotation = Quaternion.LookRotation(outward); move.Simulate(Dt, new Vector2(0f, 1f), false); Physics.SyncTransforms(); lift.Step(Dt); }
            return $"lift: rode up to y {topY:0.00} in {ride:0.0} s, then off at {Fmt(tr.position)} (y {tr.position.y:0.00})" + (tr.position.y > 4.4f ? " — OK\n" : " — FAILED\n");
        }

        static void Teleport(Transform tr, CharacterController cc, Vector3 pos)
        {
            cc.enabled = false; tr.position = pos; cc.enabled = true;
            Physics.SyncTransforms();
        }

        /// <summary>A Unity position written in the Blender frame (x, y, z up), to compare with the generator.</summary>
        static string Fmt(Vector3 u) => $"B({u.x:0.00}, {u.z:0.00}, {u.y:0.00})";
    }
}
