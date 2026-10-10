using System.Collections.Generic;
using SlugMove;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800, part 9: tools to work on the island cheaply (Oct 10; the rules in .claude/skills/modeling/shapes/README.md).
    /// - Rebuild Caves Only: the cave models and the terrain's holes again, on the terrain as it is (seconds, not the whole
    ///   build). Shapes cut into the terrain itself (an inlet, a pit's hollow) still need Build Scene
    /// - Check Cave Gaps: rays from inside every cave; counts where the sky would show (through the terrain's underside, or
    ///   out of a hole that is not a mouth or a skylight) and says where
    /// - Sheet/...: runs a Shoot menu with every shot drawn small into one numbered image (one picture to look at)
    /// All of them first bring back what the build keeps in memory (the rivers, the falls, the caves) if a recompile lost it,
    /// so the Shoot and Measure menus also work again after a script change without a rebuild.
    /// </summary>
    public static partial class IslandTest800Builder
    {
        static float[] TerrainHm(Terrain t)
        {
            var h = t.terrainData.GetHeights(0, 0, Res, Res);
            var hm = new float[Res * Res];
            for (int iz = 0; iz < Res; iz++)
                for (int ix = 0; ix < Res; ix++)
                    hm[iz * Res + ix] = h[iz, ix] * MaxHeight + Bottom;
            return hm;
        }

        /// <summary>Brings back the build's in-memory state (lost when scripts recompile).</summary>
        static void EnsureState(Terrain t, bool replanCaves = false)
        {
            if (!_prepared) Prepare();
            if (_rivers == null) { Erode(); PrepareRivers(); }        // (the falls' place comes from the south river)
            if (replanCaves || _tubes.Count == 0) PlanCaves(TerrainHm(t));
        }

        [MenuItem("Slug/IslandTest 800 m/Rebuild Caves Only")]
        public static string RebuildCaves()
        {
            var t = OpenTerrain();
            if (!t) return "open IslandTest_800 first";
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var map = t.transform.parent;
            MoveTestBuilder._map = map;
            EnsureState(t, true);
            PunchHoles(t.terrainData, TerrainHm(t));
            var old = new List<GameObject>();
            foreach (Transform c in map)
                if (c.name == "洞窟" || c.name.StartsWith("Label_溶岩洞の口") || c.name == "Label_岩塩の洞窟" || c.name == "Label_入江" || c.name.StartsWith("Label_天窓（下は塩水）") || c.name == "Label_キプカの落ち穴") old.Add(c.gameObject);
            foreach (var g in old) Object.DestroyImmediate(g);
            Random.InitState(820);
            BuildCaves(map, t);
            t.Flush();
            EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
            EditorSceneManager.SaveScene(t.gameObject.scene);
            AssetDatabase.SaveAssets();
            return $"caves rebuilt: {_tubes.Count} tunnels, {_domes.Count} rooms ({watch.ElapsedMilliseconds} ms)";
        }

        // ------------------------------------------------------------------ gaps
        static bool InsideCave(Vector3 p)
        {
            if (InAnyTube(p, null, 0f)) return true;
            foreach (var d in _domes)
                if (new Vector2(p.x - d.floor.x, p.z - d.floor.z).magnitude <= d.r && p.y > d.floor.y - 0.3f && p.y < d.floor.y + d.height) return true;
            return false;
        }

        static string NearestCave(Vector3 p)
        {
            string best = "?";
            float bd = float.MaxValue;
            foreach (var tb in _tubes) { NearTube(tb, p.x, p.z, 0f, out _, out float d); if (d < bd) { bd = d; best = tb.name; } }
            foreach (var d in _domes) { float dd = new Vector2(p.x - d.floor.x, p.z - d.floor.z).magnitude - d.r; if (dd < bd) { bd = dd; best = d.name; } }
            return best;
        }

        [MenuItem("Slug/IslandTest 800 m/Check Cave Gaps")]
        public static string CheckCaveGaps()
        {
            var t = OpenTerrain();
            if (!t) return "open IslandTest_800 first";
            EnsureState(t);
            Physics.SyncTransforms();
            bool backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;            // the cave's inside faces, and the terrain from below
            try
            {
                // the places meant to be open: the mouths and the skylights
                var open = new List<Vector2>();
                foreach (var m in _mouths) open.Add(new Vector2(m.x, m.z));
                foreach (var m in _openings) open.Add(new Vector2(m.x, m.z));
                for (int i = 0; i < Skylights.Length; i += 2) open.Add(W(Skylights[i], Skylights[i + 1]));
                bool NearOpening(Vector3 p) { if (InletLocal(p.x, p.z, out _, out float ir) && ir < InletOuterR) return true; foreach (var o in open) if (new Vector2(p.x - o.x, p.z - o.y).magnitude < 7f) return true; return false; }   // (the inlet is open)

                // self-test: from under solid ground, a ray up must meet the terrain's underside
                var probe = W(new Vector2(375f, 300f));
                bool selfTest = Physics.Raycast(new Vector3(probe.x, Bottom + 1f, probe.y), Vector3.up, out var ph, 400f) && ph.collider is TerrainCollider;

                var dirs = new List<Vector3> { Vector3.up };
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI / 4f;
                    dirs.Add(new Vector3(Mathf.Cos(a), 1f, Mathf.Sin(a)).normalized);                  // 45° up
                    dirs.Add(new Vector3(Mathf.Cos(a + 0.39f), 0.36f, Mathf.Sin(a + 0.39f)).normalized); // 20° up
                }
                var points = new List<Vector3>();
                foreach (var tb in _tubes)
                {
                    var path = new List<Vector2>();
                    foreach (var p in tb.pts) path.Add(new Vector2(p.x, p.z));
                    var flat = Resample(path, 1.5f);
                    for (int i = 1; i + 1 < flat.Count; i++)                         // (not on the end rings: a ray starting on a cap passes through it)
                    {
                        NearTube(tb, flat[i].x, flat[i].y, 99f, out float floor);
                        Vector2 tan = (flat[Mathf.Min(flat.Count - 1, i + 1)] - flat[Mathf.Max(0, i - 1)]).normalized, side = new Vector2(-tan.y, tan.x);
                        foreach (float s in new[] { -0.45f, 0f, 0.45f })               // (well inside: the walls wobble by 12%)
                            foreach (float up in new[] { 0.4f, tb.height * 0.45f })
                            {
                                var q = flat[i] + side * s * tb.halfW;
                                points.Add(new Vector3(q.x, floor + up, q.y));
                            }
                    }
                }
                foreach (var d in _domes)
                    for (int r = 0; r < 3; r++)
                        for (int k = 0; k < 12; k++)
                        {
                            float a = k * Mathf.PI / 6f, rr = d.r * r * 0.3f;
                            foreach (float up in new[] { 0.4f, d.height * 0.6f })
                                points.Add(d.floor + new Vector3(Mathf.Cos(a) * rr, up, Mathf.Sin(a) * rr));
                        }

                // under the inlet's roof (the open inlet itself is meant to see the sky)
                if (_inletOK)
                    for (float a = InletCenterA - InletWaterR + 1f; a < InletRoofFront - 1f; a += 2.5f)
                        for (float b = -InletWaterR - InletLedgeW + 1f; b < InletWaterR + InletLedgeW - 1f; b += 2.5f)
                        {
                            var q = InletPoint(a, b);
                            InletLocal(q.x, q.y, out _, out float r);
                            if (r > InletWaterR + InletLedgeW - 0.5f) continue;
                            float g = Mathf.Max(0f, t.SampleHeight(new Vector3(q.x, 0f, q.y)) + t.transform.position.y);
                            points.Add(new Vector3(q.x, g + 0.5f, q.y)); points.Add(new Vector3(q.x, g + 3.5f, q.y));
                        }
                int rays = 0, under = 0, holes = 0;
                var spots = new Dictionary<Vector3Int, (int n, Vector3 at, bool under)>();
                void Mark(Vector3 at, bool isUnder)
                {
                    var key = new Vector3Int(Mathf.FloorToInt(at.x / 4f), Mathf.FloorToInt(at.y / 4f), Mathf.FloorToInt(at.z / 4f));
                    spots[key] = spots.TryGetValue(key, out var v) ? (v.n + 1, v.at, v.under) : (1, at, isUnder);
                }
                foreach (var p in points)
                    foreach (var dir in dirs)
                    {
                        rays++;
                        if (Physics.Raycast(p, dir, out var hit, 120f))
                        {
                            // the terrain met from below: the ray left the cave where no rock is, under solid ground
                            if (hit.collider is TerrainCollider && Vector3.Dot(hit.normal, dir) > 0f) { under++; Mark(p, true); }
                            continue;
                        }
                        // out to the sky: fine if it left the cave through a mouth or a skylight
                        Vector3 exit = p;
                        for (float l = 0f; l < 120f && InsideCave(exit); l += 0.5f) exit = p + dir * l;
                        if (!NearOpening(exit)) { holes++; Mark(exit, false); }
                    }

                var list = new List<(int n, Vector3 at, bool under)>(spots.Values);
                list.Sort((a, b) => b.n.CompareTo(a.n));
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"cave gaps: {rays:N0} rays from {points.Count:N0} points (self-test {(selfTest ? "ok" : "FAILED: the terrain was not hit from below, under-ground gaps are not counted")})");
                sb.AppendLine($"- under the ground (the sky shows through the terrain's underside): {under} rays");
                sb.AppendLine($"- out of a hole that is not a mouth or a skylight: {holes} rays");
                sb.AppendLine($"- {list.Count} places (4 m cells). worst:");
                for (int i = 0; i < Mathf.Min(6, list.Count); i++)
                    sb.AppendLine($"  {list[i].n} rays, {(list[i].under ? "under ground" : "hole")}, {NearestCave(list[i].at)} at ({list[i].at.x:F0}, {list[i].at.y:F1}, {list[i].at.z:F0})");
                return sb.ToString().TrimEnd();
            }
            finally { Physics.queriesHitBackfaces = backfaces; }
        }

        // ------------------------------------------------------------------ one image per Shoot menu
        static string Sheet(string name, int maxShots, System.Func<string> shoot)
        {
            var t = OpenTerrain();
            if (!t) return "open IslandTest_800 first";
            EnsureState(t);
            ShotUtil.BeginSheet($"{ShotDir}/sheet_{name}.png", 3, maxShots);
            string written;
            try { shoot(); }
            finally { written = ShotUtil.EndSheet(); }       // (never leave the sheet open: later captures would vanish into it)
            return written;
        }

        [MenuItem("Slug/IslandTest 800 m/Sheet/Caves And Loops")] public static string SheetCaves() => Sheet("caves", 12, ShootCaves);
        [MenuItem("Slug/IslandTest 800 m/Sheet/Falls")] public static string SheetFalls() => Sheet("falls", 6, ShootFalls);
        [MenuItem("Slug/IslandTest 800 m/Sheet/Rivers")] public static string SheetRivers() => Sheet("rivers", 9, ShootRivers);
        [MenuItem("Slug/IslandTest 800 m/Sheet/Ways And Cover")] public static string SheetWays() => Sheet("ways", 12, ShootWays);
        [MenuItem("Slug/IslandTest 800 m/Sheet/Views")] public static string SheetViews() => Sheet("views", 21, ShootViews);
    }
}
