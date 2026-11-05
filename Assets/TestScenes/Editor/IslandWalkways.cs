using System.Collections.Generic;
using System.Text;
using SlugDesign;
using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Board walkways along the cliffs, plank bridges and ladders on the rock, laid through points わくさん places by hand.
    /// Nov 5 わくさん「cube 設置してるとこは橋とか置きたい」, then「cube おいてるところで一回途切れさせて」「桟橋と登り階段が
    /// 不自然」「この写真の橋みたいな見た目を意識して」(a wooden bridge over a sea gorge: thick square posts bound with rope at
    /// two heights, a sagging rope rail, planks of uneven width across two beams, diagonal braces from under the deck into
    /// the cliff) and「岩に掛けたはしご」.
    /// Each one is a WalkwayPath (Assets/World/WalkwayPath.cs) under Map/通路; its children are the points in order.
    /// Move the points, then Slug/IslandTest 800 m/Walkways/Rebuild, then Walkways/Walk Test. The terrain is never changed.
    ///  - Wall: a walkway along a cliff. Posts on the outer side every PostGap, two sagging ropes between them, a cross
    ///    beam under each post running into the rock and a diagonal brace from the outer beam down into the rock.
    ///    Where the line between points cuts into the rock, the deck moves out from the wall (up to 4 m); where the
    ///    ground is still in the way (a bench), the deck is lifted onto it. A point named 切れ目… breaks the walkway: the
    ///    deck stops BreakGap short of it on both sides and meets the ground there (you walk on the ground across).
    ///  - Bridge: the same deck between its two ends, sagging a little, posts and ropes on both sides, taller end posts,
    ///    and braces from under its first and last few metres into the cliffs.
    ///  - Ladder: at each point a small platform braced into the rock (its floor at the point), with a rope rail on its
    ///    two sides, and a ladder leaned up to its outer edge: the first from the ground, the rest from the platform below.
    ///    The ladder's lean (65–82°) is picked so its foot stands on gentle ground and the climb clears the rock. Each ladder
    ///    has a SlugLadder (Assets/Movement) the slug climbs.
    /// The walking surface is a separate smooth collider (deck 0.3 m thick) with low rail walls (0.5 m) so a slug doesn't
    /// slide off; rails stop OpenEnd short of each end so you can walk on and off.
    /// Wood is faceted and no textures (pop). Materials: Assets/TestScenes/Generated/Walkways/WW_*.mat.
    /// </summary>
    public static class IslandWalkways
    {
        const string Dir = "Assets/TestScenes/Generated/Walkways";
        const float Plank = 0.05f;     // plank thickness
        const float BeamH = 0.12f;     // the two beams under the planks
        const float RailH = 0.5f;      // rail height (the slug's eye is 0.47 m)
        const float PostGap = 1.6f;
        const float Sample = 0.5f;     // the deck is laid in 0.5 m pieces
        const float MaxPush = 4f;      // how far the deck may move out from the wall
        const float OpenEnd = 0.8f;
        const float BreakGap = 1.3f;   // the deck stops this far short of a break
        const float LadderW = 0.5f;    // between the ladder's rails (the slug is 0.36 m wide)
        const float RungGap = 0.28f;
        const float FeetOut = 0.28f;   // the feet climb this far out from the rungs

        // sub-meshes
        const int PlankA = 0, PlankB = 1, BeamM = 2, RopeM = 3;

        static Terrain _t;

        [MenuItem("Slug/IslandTest 800 m/Walkways/Rebuild")]
        public static string RebuildAll()
        {
            var sb = new StringBuilder();
            foreach (var w in Object.FindObjectsByType<WalkwayPath>(FindObjectsInactive.Exclude)) sb.AppendLine(Build(w));
            AssetDatabase.SaveAssets();
            return sb.ToString();
        }

        [MenuItem("Slug/IslandTest 800 m/Walkways/Walk Test")]
        public static string WalkTestAll()
        {
            var sb = new StringBuilder();
            foreach (var w in Object.FindObjectsByType<WalkwayPath>(FindObjectsInactive.Exclude))
                sb.AppendLine($"{w.name}: {(w.kind == WalkwayPath.Kind.Ladder ? LadderTest(w) : WalkTest(w))}");
            return sb.ToString();
        }

        static Material[] Materials()
        {
            System.IO.Directory.CreateDirectory(Dir);
            return new[]
            {
                NatureAssets.Mat($"{Dir}/WW_Plank.mat", "#B8844F", false, 0f),
                NatureAssets.Mat($"{Dir}/WW_PlankDark.mat", "#9C6B41", false, 0f),
                NatureAssets.Mat($"{Dir}/WW_Beam.mat", "#73492B", false, 0f),
                NatureAssets.Mat($"{Dir}/WW_Rope.mat", "#D4B079", false, 0f),
            };
        }

        // ------------------------------------------------------------------ build
        public static string Build(WalkwayPath w)
        {
            _t = Object.FindAnyObjectByType<Terrain>();
            if (!_t) return "no terrain";
            var old = w.transform.Find(WalkwayPath.BuiltName);
            if (old) Object.DestroyImmediate(old.gameObject);
            var pts = new List<(Vector3 p, bool brk)>();
            foreach (Transform c in w.transform) if (c.name != WalkwayPath.BuiltName) pts.Add((WalkwayPath.DeckPoint(c), WalkwayPath.IsBreak(c)));
            if (pts.Count < (w.kind == WalkwayPath.Kind.Ladder ? 1 : 2)) return $"{w.name}: needs more points";

            var m = new SimpleMesh(4);
            var col = new SimpleMesh(1);
            var go = new GameObject(WalkwayPath.BuiltName);
            go.transform.SetParent(w.transform, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var ls = w.transform.lossyScale;
            go.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
            int seed = w.name.GetHashCode() & 0xffff;
            var report = new StringBuilder();
            var line = new List<Vector3>();
            var sections = new List<int>();

            if (w.kind == WalkwayPath.Kind.Ladder)
                BuildLadders(w, pts.ConvertAll(x => x.p), m, col, go, line, sections, report, seed);
            else
            {
                // pieces between breaks; a break ends one piece and starts the next
                var pieces = new List<(List<Vector3> pts, bool trimStart, bool trimEnd)>();
                var cur = new List<Vector3>(); bool trimStart = false;
                for (int i = 0; i < pts.Count; i++)
                {
                    cur.Add(pts[i].p);
                    if (pts[i].brk && i > 0 && i < pts.Count - 1)
                    {
                        pieces.Add((cur, trimStart, true));
                        cur = new List<Vector3> { pts[i].p }; trimStart = true;
                    }
                }
                pieces.Add((cur, trimStart, false));
                int k = 0;
                foreach (var pc in pieces)
                {
                    if (pc.pts.Count < 2) continue;
                    var s = LayDeck(w, pc.pts, pc.trimStart, pc.trimEnd, report, out var outv, out var fwd);
                    if (s.Count < 3) { report.Append($" piece {k + 1} too short after the break;"); continue; }
                    Look(w, m, s, outv, fwd, seed + 101 * k);
                    Surface(w, col, s, outv);
                    sections.Add(line.Count);
                    line.AddRange(s);
                    k++;
                }
                if (k > 1) report.Insert(0, $" {k} pieces (broken at the 切れ目);");
            }

            var mesh = NatureAssets.SaveMesh(m.ToMesh($"{w.name}_Mesh"), $"{Dir}/{Safe(w.name)}_Mesh.asset");
            var cm = NatureAssets.SaveMesh(col.ToMesh($"{w.name}_Collider"), $"{Dir}/{Safe(w.name)}_Collider.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = Materials();
            go.AddComponent<MeshCollider>().sharedMesh = cm;
            go.isStatic = true;
            w.deckLine = line.ToArray();
            w.sections = sections.ToArray();
            EditorUtility.SetDirty(w);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(w.gameObject.scene);
            float len = 0f; for (int i = 1; i < line.Count; i++) if (System.Array.IndexOf(w.sections, i) < 0) len += Vector3.Distance(line[i - 1], line[i]);
            return $"{w.name} ({w.kind}): {(w.kind == WalkwayPath.Kind.Ladder ? "" : $"{len:F0} m, ")}{m.TriangleCount} tris;{report}";
        }

        static string Safe(string s) => s.Replace("/", "_");
        static float Ground(Vector3 p) => _t.SampleHeight(p) + _t.transform.position.y;
        static float Steep(Vector3 p) => _t.terrainData.GetSteepness((p.x - _t.transform.position.x) / _t.terrainData.size.x, (p.z - _t.transform.position.z) / _t.terrainData.size.z);

        // the rock's outward facing (horizontal), averaged over a disc
        static Vector3 WallOut(Vector3 p, float radius)
        {
            var o = Vector3.zero;
            for (float x = -radius; x <= radius; x += radius / 3f)
                for (float z = -radius; z <= radius; z += radius / 3f)
                {
                    if (x * x + z * z > radius * radius) continue;
                    var q = p + new Vector3(x, 0f, z);
                    var nq = _t.terrainData.GetInterpolatedNormal((q.x - _t.transform.position.x) / _t.terrainData.size.x, (q.z - _t.transform.position.z) / _t.terrainData.size.z);
                    o += new Vector3(nq.x, 0f, nq.z);
                }
            return o.sqrMagnitude < 1e-6f ? Vector3.forward : o.normalized;
        }

        // a wall comes up into the deck here (centre, outer edge, or the inner quarter); ground only a little above it
        // (a bench) is left for the lift
        static bool Into(Vector3 p, Vector3 o, float half)
            => Ground(p) > p.y + 0.5f || Ground(p + o * half) > p.y + 0.5f || Ground(p - o * half * 0.5f) > p.y + 0.5f;

        // ------------------------------------------------------------------ the deck's line (walkway, bridge)
        static List<Vector3> LayDeck(WalkwayPath w, List<Vector3> pts, bool trimStart, bool trimEnd, StringBuilder report, out Vector3[] outv, out Vector3[] fwd)
        {
            bool wall = w.kind == WalkwayPath.Kind.Wall;
            float half = 0.5f * w.width;
            var s = new List<Vector3> { pts[0] };
            float tanMax = Mathf.Tan(w.maxGrade * Mathf.Deg2Rad);
            for (int i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1]; var b = pts[i];
                var d = b - a; float hz = new Vector2(d.x, d.z).magnitude;
                if (wall && Mathf.Abs(d.y) > hz * tanMax) report.Append($" points {i}–{i + 1} steeper than {w.maxGrade:F0}°;");
                int k = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / Sample));
                for (int j = 1; j <= k; j++) s.Add(Vector3.Lerp(a, b, j / (float)k));
            }
            // stop short of a break
            if (trimEnd) { var e = s[s.Count - 1]; while (s.Count > 2 && Flat(s[s.Count - 1] - e) < BreakGap) s.RemoveAt(s.Count - 1); }
            if (trimStart) { var e = s[0]; while (s.Count > 2 && Flat(s[0] - e) < BreakGap) s.RemoveAt(0); }
            if (!wall)
            {
                float len = Vector3.Distance(s[0], s[s.Count - 1]), sag = 0.012f * len;
                for (int i = 0; i < s.Count; i++) { float u = i / (s.Count - 1f); s[i] += Vector3.down * (sag * 4f * u * (1f - u)); }
            }
            int n = s.Count;
            outv = new Vector3[n]; fwd = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var f = s[Mathf.Min(n - 1, i + 1)] - s[Mathf.Max(0, i - 1)];
                var fh = new Vector3(f.x, 0f, f.z).normalized;
                var perp = Vector3.Cross(Vector3.up, fh);
                outv[i] = !wall || Ground(s[i] + perp * 1.5f) <= Ground(s[i] - perp * 1.5f) ? perp : -perp;
            }
            SmoothSides(outv);
            if (wall)
            {
                var pokes = 0;
                var push = new float[n];
                for (int i = 0; i < n; i++) { float d = 0f; while (d < MaxPush && Into(s[i] + outv[i] * d, outv[i], half)) d += 0.1f; push[i] = d; }
                var wide = new float[n];
                for (int i = 0; i < n; i++) { float mx = 0f; for (int k = -3; k <= 3; k++) mx = Mathf.Max(mx, push[Mathf.Clamp(i + k, 0, n - 1)]); wide[i] = mx; }
                for (int i = 0; i < n; i++)
                {
                    float a = 0f; for (int k = -3; k <= 3; k++) a += wide[Mathf.Clamp(i + k, 0, n - 1)];
                    float pin = Mathf.Clamp01(Mathf.Min(i, n - 1 - i) * Sample / 2f);   // the two ends stay on their points
                    s[i] += outv[i] * (a / 7f * pin);
                }
                // still in the ground (a bench, a ridge): lift the deck onto it; the ends meet the ground
                float tanG = Mathf.Tan(w.maxGrade * Mathf.Deg2Rad) * Sample;
                for (int i = 0; i < n; i++)
                {
                    bool end = i < 2 || i > n - 3;
                    float need = end ? Ground(s[i]) + 0.04f : Mathf.Max(Ground(s[i]), Mathf.Max(Ground(s[i] + outv[i] * half), Ground(s[i] - outv[i] * half * 0.5f))) + 0.08f;
                    if (need > s[i].y) { if (!end) pokes++; s[i] = new Vector3(s[i].x, need, s[i].z); }
                    else if (end && ((i == 0 && trimStart) || (i == n - 1 && trimEnd))) s[i] = new Vector3(s[i].x, need, s[i].z);   // down onto the ground at a break
                }
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int i = 1; i < n; i++) if (s[i].y < s[i - 1].y - tanG) s[i] = new Vector3(s[i].x, s[i - 1].y - tanG, s[i].z);
                    for (int i = n - 2; i >= 0; i--) if (s[i].y < s[i + 1].y - tanG) s[i] = new Vector3(s[i].x, s[i + 1].y - tanG, s[i].z);
                }
                if (pokes > 0) report.Append($" lifted onto the ground at {pokes} of {n} spots;");
            }
            for (int i = 0; i < n; i++)
            {
                var f = s[Mathf.Min(n - 1, i + 1)] - s[Mathf.Max(0, i - 1)];
                fwd[i] = f.normalized;
                outv[i] = (outv[i] - Vector3.Dot(outv[i], fwd[i]) * fwd[i]).normalized;
            }
            return s;
        }

        static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;

        static void SmoothSides(Vector3[] o)
        {
            int n = o.Length;
            var c = (Vector3[])o.Clone();
            for (int i = 0; i < n; i++)
            {
                var a = Vector3.zero;
                for (int k = -6; k <= 6; k++) a += c[Mathf.Clamp(i + k, 0, n - 1)];
                if (Vector3.Dot(a, c[i]) < 0f) o[i] = -c[i];
            }
        }

        // ------------------------------------------------------------------ the look (the photo's bridge)
        static float H(int seed, int i)
        {
            uint x = (uint)(seed * 73856093) ^ (uint)(i * 19349663);
            x ^= x >> 13; x *= 0x5bd1e995; x ^= x >> 15;
            return (x & 0xffffff) / (float)0x1000000;
        }

        static float[] Dist(List<Vector3> s)
        {
            var d = new float[s.Count];
            for (int i = 1; i < s.Count; i++) d[i] = d[i - 1] + Vector3.Distance(s[i - 1], s[i]);
            return d;
        }

        static Vector3 At(List<Vector3> s, float[] dist, float d, out int i)
        {
            i = 0;
            while (i < s.Count - 2 && dist[i + 1] < d) i++;
            float u = Mathf.InverseLerp(dist[i], dist[i + 1], d);
            return Vector3.Lerp(s[i], s[i + 1], u);
        }

        static Vector3 Up(Vector3 f, Vector3 r) { var u = Vector3.Cross(f, r); return u.y < 0f ? -u : u; }

        static IEnumerable<float> Every(float gap, float from, float to)
        {
            int k = Mathf.Max(1, Mathf.RoundToInt((to - from) / gap));
            for (int i = 0; i <= k; i++) yield return Mathf.Lerp(from, to, i / (float)k);
        }

        static void Look(WalkwayPath w, SimpleMesh m, List<Vector3> s, Vector3[] o, Vector3[] f, int seed)
        {
            bool wall = w.kind == WalkwayPath.Kind.Wall;
            float half = 0.5f * w.width;
            var dist = Dist(s);
            float total = dist[dist.Length - 1];

            // planks across, uneven widths, ends not quite in line, a few a little twisted, two shades
            int idx = 0;
            for (float d = 0.03f; d < total - 0.05f; idx++)
            {
                float pw = 0.13f + 0.08f * H(seed, idx * 9 + 1), gap = 0.012f + 0.025f * H(seed, idx * 9 + 2);
                float e = Mathf.Min(d + pw, total);
                var p = At(s, dist, 0.5f * (d + e), out int i);
                var r = o[i]; var fw = f[i]; var up = Up(fw, r);
                float yaw = 0.05f * (H(seed, idx * 9 + 3) - 0.5f);
                var rr = (r + fw * yaw).normalized; var ff = Vector3.Cross(up, rr); if (Vector3.Dot(ff, fw) < 0f) ff = -ff;
                float inEnd = half + 0.06f + 0.06f * (H(seed, idx * 9 + 4) - 0.5f);
                float outEnd = half + 0.06f + 0.06f * (H(seed, idx * 9 + 5) - 0.5f);
                float yj = 0.01f * (H(seed, idx * 9 + 6) - 0.5f);
                m.Box(H(seed, idx * 9 + 7) < 0.35f ? PlankB : PlankA, p + up * yj - ff * (0.5f * (e - d)), rr, up, ff,
                      new Vector3(-inEnd, -Plank, 0f), new Vector3(outEnd, 0f, e - d), true);
                d = e + gap;
            }

            // two beams under the planks, edge to edge
            float bx = half - 0.1f;
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i + 1 < s.Count; i++)
                {
                    var a = s[i] + o[i] * side * bx + Vector3.down * Plank;
                    var b = s[i + 1] + o[i + 1] * side * bx + Vector3.down * Plank;
                    var fw = (b - a); float len = fw.magnitude; if (len < 1e-4f) continue; fw /= len;
                    var up = Up(fw, o[i]);
                    m.Box(BeamM, a, o[i], up, fw, new Vector3(-0.045f, -BeamH, 0f), new Vector3(0.045f, 0f, len + 0.01f), true);
                }

            // posts, ropes, cross beams and braces
            float from = Mathf.Min(OpenEnd, total * 0.3f), to = total - Mathf.Min(OpenEnd, total * 0.3f);
            var sides = wall ? new[] { 1f } : new[] { -1f, 1f };
            var stations = new List<float>(Every(PostGap, from, to));
            foreach (float side in sides)
            {
                Vector3? lastTop = null, lastLow = null;
                for (int k = 0; k < stations.Count; k++)
                {
                    float d = stations[k];
                    var p = At(s, dist, d, out int i);
                    var r = o[i] * side;
                    bool end = k == 0 || k == stations.Count - 1;
                    float th = end ? 0.06f : 0.045f, top = RailH + (end ? 0.2f : 0.06f);
                    var lean = r * 0.025f * (H(seed + 5, k * 2 + (side > 0 ? 1 : 0)) - 0.5f) + f[i] * 0.02f * (H(seed + 6, k) - 0.5f);
                    var foot = p + r * (half + 0.02f) + Vector3.down * (Plank + BeamH + 0.08f);
                    var tip = p + r * (half + 0.02f) + Vector3.up * top + lean;
                    Post(m, foot, tip, th, f[i]);
                    // rope bound round the post where the ropes are tied, and low down
                    Lash(m, Vector3.Lerp(foot, tip, Mathf.InverseLerp(foot.y, tip.y, p.y + RailH - 0.03f)), th + 0.014f, f[i]);
                    Lash(m, Vector3.Lerp(foot, tip, Mathf.InverseLerp(foot.y, tip.y, p.y + 0.06f)), th + 0.014f, f[i]);
                    if (end) Lash(m, Vector3.Lerp(foot, tip, Mathf.InverseLerp(foot.y, tip.y, p.y + RailH * 0.5f)), th + 0.014f, f[i]);
                    var ropeTop = p + r * (half + 0.02f + th) + Vector3.up * (RailH - 0.03f) + lean * 0.9f;
                    var ropeLow = p + r * (half + 0.02f + th) + Vector3.up * (RailH * 0.48f) + lean * 0.5f;
                    if (lastTop.HasValue) { Rope(m, lastTop.Value, ropeTop, 0.06f); Rope(m, lastLow.Value, ropeLow, 0.045f); }
                    lastTop = ropeTop; lastLow = ropeLow;
                }
            }
            foreach (float d in stations)
            {
                var p = At(s, dist, d, out int i);
                var r = o[i]; var fw = f[i]; var up = Up(fw, r);
                var c = p + Vector3.down * (Plank + BeamH);
                // the cross beam: on a wall walkway it runs on into the rock
                float inner = half + 0.12f;
                if (wall)
                {
                    var hit = IntoWall(c - r * half, -r, 0.8f);     // only when the rock is right there (else it sticks out into the air)
                    if (hit.HasValue) inner = Mathf.Max(inner, Vector3.Dot(c - hit.Value, r) + 0.3f);
                }
                m.Box(BeamM, c, r, up, fw, new Vector3(-inner, -0.08f, -0.04f), new Vector3(half + 0.12f, 0f, 0.04f), true);
            }
            if (wall)
            {
                foreach (float d in stations)
                {
                    var p = At(s, dist, d, out int i);
                    var r = o[i];
                    var outer = p + r * (half - 0.1f) + Vector3.down * (Plank + BeamH + 0.05f);
                    var lo = IntoWall(p + Vector3.down * 1.3f, -r, 5f) ?? IntoWall(p + Vector3.down * 0.8f, -r, 5f);
                    if (lo.HasValue) Stick(m, BeamM, outer, lo.Value - r * 0.25f + Vector3.down * 0.1f, 0.04f);
                    else
                    {
                        float g = Ground(outer);
                        if (outer.y - g > 0.3f && outer.y - g < 8f) Stick(m, BeamM, outer, new Vector3(outer.x, g - 0.3f, outer.z), 0.05f);
                    }
                }
            }
            else
            {
                // the bridge's ends: braces from under the deck down into the cliff it rests on
                foreach (var (d, toward) in new[] { (0.6f, -1f), (1.6f, -1f), (total - 0.6f, 1f), (total - 1.6f, 1f) })
                {
                    if (d < 0f || d > total) continue;
                    var p = At(s, dist, d, out int i);
                    var dirH = new Vector3(f[i].x, 0f, f[i].z).normalized * toward;
                    foreach (float side in new[] { -1f, 1f })
                    {
                        var top = p + o[i] * side * (half - 0.1f) + Vector3.down * (Plank + BeamH + 0.03f);
                        var lo = IntoWall(top + Vector3.down * 1.2f - dirH * 0.4f, dirH, 4f);
                        if (lo.HasValue) Stick(m, BeamM, top, lo.Value + dirH * 0.25f, 0.045f);
                    }
                }
            }
        }

        // a square post from foot to tip, faced along the walk
        static void Post(SimpleMesh m, Vector3 foot, Vector3 tip, float half, Vector3 along)
        {
            var axis = (tip - foot); float len = axis.magnitude; axis /= len;
            var x = Vector3.Cross(axis, new Vector3(along.x, 0f, along.z).normalized); if (x.sqrMagnitude < 1e-4f) x = Vector3.right; x.Normalize();
            var z = Vector3.Cross(x, axis);
            m.Box(BeamM, foot, x, axis, z, new Vector3(-half, 0f, -half), new Vector3(half, len, half), true);
            // a little cap, the top cut flat and a bit narrower
            m.Box(BeamM, tip, x, axis, z, new Vector3(-half * 0.8f, 0f, -half * 0.8f), new Vector3(half * 0.8f, 0.02f, half * 0.8f), false);
        }

        // rope wound round a post: a short band, a little fatter than the post
        static void Lash(SimpleMesh m, Vector3 c, float half, Vector3 along)
        {
            var z = new Vector3(along.x, 0f, along.z).normalized; var x = Vector3.Cross(Vector3.up, z);
            m.Box(RopeM, c + Vector3.down * 0.02f, x, Vector3.up, z, new Vector3(-half, 0f, -half), new Vector3(half, 0.04f, half), true);
        }

        // walk in from p along dir at the same height until the ground is above it
        static Vector3? IntoWall(Vector3 p, Vector3 dir, float max)
        {
            for (float d = 0f; d <= max; d += 0.1f)
            {
                var q = p + dir * d;
                if (Ground(q) >= q.y) return q;
            }
            return null;
        }

        // a stick between two points (round-ish, six sides)
        static void Stick(SimpleMesh m, int sub, Vector3 a, Vector3 b, float r, int sides = 4)
        {
            var ax = b - a; float len = ax.magnitude;
            if (len < 0.01f) return;
            var z = ax / len;
            var x = Vector3.Cross(Mathf.Abs(z.y) > 0.9f ? Vector3.forward : Vector3.up, z).normalized;
            var y = Vector3.Cross(z, x);
            if (sides == 4) { m.Box(sub, a, x, y, z, new Vector3(-r, -r, 0f), new Vector3(r, r, len), true); return; }
            for (int k = 0; k < sides; k++)
            {
                float a0 = k * Mathf.PI * 2f / sides, a1 = (k + 1) * Mathf.PI * 2f / sides;
                Vector3 c0 = (x * Mathf.Cos(a0) + y * Mathf.Sin(a0)) * r, c1 = (x * Mathf.Cos(a1) + y * Mathf.Sin(a1)) * r;
                m.Quad(sub, a + c0, a + c1, b + c1, b + c0, c0 + c1);
            }
        }

        static void Rope(SimpleMesh m, Vector3 a, Vector3 b, float sag)
        {
            const int pieces = 6;
            Vector3 last = a;
            for (int k = 1; k <= pieces; k++)
            {
                float u = k / (float)pieces;
                var p = Vector3.Lerp(a, b, u) + Vector3.down * (sag * 4f * u * (1f - u));
                Stick(m, RopeM, last, p, 0.016f, 6);
                last = p;
            }
        }

        // ------------------------------------------------------------------ the walking surface
        static void Surface(WalkwayPath w, SimpleMesh c, List<Vector3> s, Vector3[] o)
        {
            float half = 0.5f * w.width;
            var dn = Vector3.down * 0.3f;
            int n = s.Count;
            for (int i = 0; i + 1 < n; i++)
            {
                Vector3 a0 = s[i] - o[i] * half, a1 = s[i] + o[i] * half, b0 = s[i + 1] - o[i + 1] * half, b1 = s[i + 1] + o[i + 1] * half;
                c.Quad(0, a0, a1, b1, b0, Vector3.up);
                c.Quad(0, a0 + dn, a1 + dn, b1 + dn, b0 + dn, Vector3.down);
                c.Quad(0, a0, b0, b0 + dn, a0 + dn, -o[i]);
                c.Quad(0, a1, b1, b1 + dn, a1 + dn, o[i]);
            }
            var f0 = (s[1] - s[0]).normalized; var f1 = (s[n - 1] - s[n - 2]).normalized;
            c.Quad(0, s[0] - o[0] * half, s[0] + o[0] * half, s[0] + o[0] * half + dn, s[0] - o[0] * half + dn, -f0);
            c.Quad(0, s[n - 1] - o[n - 1] * half, s[n - 1] + o[n - 1] * half, s[n - 1] + o[n - 1] * half + dn, s[n - 1] - o[n - 1] * half + dn, f1);
            // low rail walls
            var dist = Dist(s);
            float total = dist[n - 1];
            float from = Mathf.Min(OpenEnd, total * 0.3f), to = total - Mathf.Min(OpenEnd, total * 0.3f);
            var up = Vector3.up * RailH;
            foreach (float side in w.kind == WalkwayPath.Kind.Wall ? new[] { 1f } : new[] { -1f, 1f })
                for (int i = 0; i + 1 < n; i++)
                {
                    if (dist[i] < from || dist[i + 1] > to) continue;
                    var a = s[i] + o[i] * side * half; var b = s[i + 1] + o[i + 1] * side * half;
                    c.Quad(0, a, b, b + up, a + up, o[i] * side);
                    c.Quad(0, a, b, b + up, a + up, -o[i] * side);
                }
        }

        // ------------------------------------------------------------------ ladders and their platforms
        const float PlatDepth = 1.3f, PlatWidth = 1.5f;

        static void BuildLadders(WalkwayPath w, List<Vector3> pts, SimpleMesh m, SimpleMesh col, GameObject go, List<Vector3> line, List<int> sections, StringBuilder report, int seed)
        {
            int count = pts.Count;
            // platforms: floor at the point, set out from the rock until it is clear of it
            var cen = new List<Vector3>(); var outs = new List<Vector3>();
            var outer = new float[count]; var halfW = new float[count]; var lat = new float[count];
            for (int k = 0; k < count; k++)
            {
                var n = WallOut(pts[k], 3f);
                var c = pts[k] + n * (PlatDepth * 0.5f - 0.3f);
                for (float d = 0f; d < 3f; d += 0.1f)
                {
                    var q = c + n * d;
                    if (Ground(q) < q.y - 0.1f && Ground(q + n * PlatDepth * 0.5f) < q.y - 0.1f) { c = q; break; }
                }
                cen.Add(c); outs.Add(n);
                outer[k] = PlatDepth * 0.5f; halfW[k] = PlatWidth * 0.5f;
                lat[k] = count == 1 ? 0f : (k % 2 == 0 ? -0.35f : 0.35f);   // ladders one above another stand side by side
            }
            // ladders from the top down: each one's foot stands on the platform below, which grows out to take it
            var feet = new Vector3[count]; var angles = new float[count]; var ok = new bool[count];
            for (int k = count - 1; k >= 0; k--)
            {
                var n = outs[k]; var t = Vector3.Cross(Vector3.up, n);
                var edge = cen[k] + n * outer[k] + t * lat[k];
                ok[k] = PickLadder(edge, n, k > 0 ? cen[k - 1].y : (float?)null, out feet[k], out angles[k], out string why);
                if (!ok[k]) { report.Append($" ladder {k + 1}: no good foot ({why});"); continue; }
                if (k > 0)
                {
                    var nb = outs[k - 1]; var tb = Vector3.Cross(Vector3.up, nb);
                    var rel = feet[k] - cen[k - 1];
                    float dn = Vector3.Dot(rel, nb), dt = Vector3.Dot(rel, tb);
                    if (dn > outer[k - 1] - 0.3f) outer[k - 1] = Mathf.Min(dn + 0.4f, 3.2f);
                    halfW[k - 1] = Mathf.Max(halfW[k - 1], Mathf.Abs(dt) + LadderW * 0.5f + 0.25f);
                    if (dn < -PlatDepth * 0.5f + 0.2f) report.Append($" ladder {k + 1}: its foot is behind the platform below;");
                }
            }
            sections.Add(0);
            for (int k = 0; k < count; k++)
            {
                var n = outs[k]; var t = Vector3.Cross(Vector3.up, n);
                Platform(m, col, cen[k], n, t, outer[k], halfW[k], seed + 37 * k);
                if (!ok[k]) { line.Add(cen[k]); continue; }
                var edge = cen[k] + n * outer[k] + t * lat[k];
                LadderLook(m, feet[k], edge, n, t, seed + 53 * k);
                // the slug's climb: a trigger in front of the rungs
                var feetFoot = feet[k] + n * FeetOut; feetFoot.y = feet[k].y + 0.02f;
                var feetTop = edge + n * FeetOut + Vector3.up * 0.05f;
                var lad = new GameObject($"はしご_{k + 1}");
                lad.transform.SetParent(go.transform, false);
                var up = (feetTop - feetFoot).normalized; var zAx = Vector3.Cross(t, up).normalized;
                lad.transform.SetPositionAndRotation(0.5f * (feetFoot + feetTop), Quaternion.LookRotation(zAx, up));
                var box = lad.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(LadderW + 0.5f, Vector3.Distance(feetFoot, feetTop) + 0.9f, 1.1f);
                var sl = lad.AddComponent<SlugLadder>();
                sl.foot = feetFoot; sl.top = feetTop; sl.into = -n;
                line.Add(feetFoot); line.Add(cen[k] + n * (outer[k] * 0.5f - PlatDepth * 0.25f));
                report.Append($" ladder {k + 1}: {feetTop.y - feetFoot.y:F1} m at {angles[k]:F0}°;");
            }
        }

        // the ladder's foot: lean between 65° and 82° (74° best), on gentle ground (or at baseY, a platform below), the
        // climb clear of the rock
        static bool PickLadder(Vector3 edge, Vector3 n, float? baseY, out Vector3 foot, out float angle, out string why)
        {
            foot = edge; angle = 0f; why = "";
            float best = float.MaxValue;
            for (float a = 65f; a <= 82f; a += 1f)
            {
                float cot = 1f / Mathf.Tan(a * Mathf.Deg2Rad);
                float by = baseY ?? Ground(edge + n * 2f);
                if (!baseY.HasValue)
                    for (int it = 0; it < 4; it++) { var f = edge + n * ((edge.y - by) * cot); by = Ground(f); }
                var ft = edge + n * ((edge.y - by) * cot); ft.y = by;
                float score = Mathf.Abs(a - 74f);
                if (!baseY.HasValue)
                {
                    float st = Steep(ft);
                    if (st > 38f) { why = $"ground under the foot {st:F0}°"; continue; }
                    score += st * 2f;
                }
                // the climb clears the rock
                bool clear = true;
                var ff = ft + n * FeetOut; var top = edge + n * FeetOut;
                for (float u = 0.08f; u < 1f; u += 0.04f)
                {
                    var q = Vector3.Lerp(ff, top, u);
                    if (Ground(q) > q.y - 0.05f) { clear = false; break; }
                    if (Ground(q + Vector3.up * 0.3f - n * 0.18f) > q.y + 0.3f) { clear = false; break; }
                }
                if (!clear) { why = "the climb runs into the rock"; continue; }
                if (score < best) { best = score; foot = ft; angle = a; }
            }
            return best < float.MaxValue;
        }

        // a platform: its back PlatDepth/2 behind c (into the rock), its front `front` out from c, `hw` either side
        static void Platform(SimpleMesh m, SimpleMesh col, Vector3 c, Vector3 n, Vector3 t, float front, float hw, int seed)
        {
            float hd = PlatDepth * 0.5f;
            // (the planks and posts are laid about a centre halfway between back and front)
            c += n * (0.5f * (front - hd)); hd = 0.5f * (front + hd);
            // planks along the rock, uneven
            int idx = 0;
            for (float d = -hd; d < hd - 0.05f; idx++)
            {
                float pw = 0.13f + 0.08f * H(seed, idx * 5 + 1), gap = 0.012f + 0.025f * H(seed, idx * 5 + 2);
                float e = Mathf.Min(d + pw, hd);
                float a = hw + 0.06f + 0.05f * (H(seed, idx * 5 + 3) - 0.5f), b = hw + 0.06f + 0.05f * (H(seed, idx * 5 + 4) - 0.5f);
                m.Box(H(seed, idx * 5 + 5) < 0.35f ? PlankB : PlankA, c + n * d, t, Vector3.up, n, new Vector3(-a, -Plank, 0f), new Vector3(b, 0f, e - d), true);
                d = e + gap;
            }
            // two beams from the rock out, running on into the rock, braced from below
            foreach (float side in new[] { -1f, 1f })
            {
                var o = c + t * side * (hw - 0.12f) + Vector3.down * Plank;
                var inner = IntoWall(o + Vector3.down * 0.06f, -n, 3f);
                float back = inner.HasValue ? Vector3.Dot(o - inner.Value, n) + 0.35f : hd + 0.2f;
                m.Box(BeamM, o, t, Vector3.up, n, new Vector3(-0.05f, -BeamH, -back), new Vector3(0.05f, 0f, hd + 0.05f), true);
                var outer = o + n * (hd - 0.05f) + Vector3.down * BeamH;
                var lo = IntoWall(c + t * side * (hw - 0.12f) + Vector3.down * 1.3f, -n, 5f);
                if (lo.HasValue) Stick(m, BeamM, outer, lo.Value - n * 0.25f, 0.045f);
            }
            // posts at the four corners, ropes along the two sides (the outer edge stays open for the ladder)
            foreach (float side in new[] { -1f, 1f })
            {
                var pIn = c + t * side * (hw + 0.03f) - n * (hd - 0.1f);
                var pOut = c + t * side * (hw + 0.03f) + n * (hd - 0.06f);
                foreach (var p in new[] { pIn, pOut })
                {
                    Post(m, p + Vector3.down * (Plank + BeamH + 0.05f), p + Vector3.up * (RailH + 0.12f), 0.05f, n);
                    Lash(m, p + Vector3.up * (RailH - 0.03f), 0.064f, n);
                    Lash(m, p + Vector3.up * 0.06f, 0.064f, n);
                }
                Rope(m, pIn + Vector3.up * (RailH - 0.03f) + t * side * 0.05f, pOut + Vector3.up * (RailH - 0.03f) + t * side * 0.05f, 0.04f);
                Rope(m, pIn + Vector3.up * (RailH * 0.48f) + t * side * 0.05f, pOut + Vector3.up * (RailH * 0.48f) + t * side * 0.05f, 0.03f);
                // the rail's wall
                var a0 = c + t * side * hw - n * hd; var a1 = c + t * side * hw + n * hd; var up = Vector3.up * RailH;
                col.Quad(0, a0, a1, a1 + up, a0 + up, t * side);
                col.Quad(0, a0, a1, a1 + up, a0 + up, -t * side);
            }
            col.Box(0, c, t, Vector3.up, n, new Vector3(-hw, -0.3f, -hd - 0.3f), new Vector3(hw, 0f, hd), true);
        }

        static void LadderLook(SimpleMesh m, Vector3 foot, Vector3 edge, Vector3 n, Vector3 t, int seed)
        {
            var top = edge + (edge - foot).normalized * 0.55f;           // the rails stand above the platform, as on the photo
            var bottom = foot - (edge - foot).normalized * 0.1f;         // a little into the ground
            float hw = 0.5f * LadderW;
            foreach (float side in new[] { -1f, 1f })
            {
                var lean = t * 0.015f * (H(seed, side > 0 ? 1 : 2) - 0.5f);
                Stick(m, BeamM, bottom + t * side * hw, top + t * side * hw + lean, 0.032f, 6);
            }
            var axis = (edge - foot); float len = axis.magnitude; axis /= len;
            int k = 0;
            for (float d = RungGap * 0.8f; d < len + 0.3f; d += RungGap, k++)
            {
                var p = foot + axis * d + n * 0.01f;
                float wob = 0.012f * (H(seed, 10 + k) - 0.5f);
                Stick(m, H(seed, 40 + k) < 0.35f ? PlankB : PlankA, p - t * (hw + 0.05f) + Vector3.up * wob, p + t * (hw + 0.05f) - Vector3.up * wob, 0.02f, 6);
                foreach (float side in new[] { -1f, 1f })
                {
                    var q = p + t * side * hw;
                    m.Box(RopeM, q - axis * 0.035f, t, axis, Vector3.Cross(t, axis), new Vector3(-0.042f, 0f, -0.042f), new Vector3(0.042f, 0.07f, 0.042f), true);
                }
            }
            // tied to the platform's edge
            foreach (float side in new[] { -1f, 1f })
            {
                var q = edge + t * side * hw + Vector3.down * 0.05f;
                m.Box(RopeM, q, t, Vector3.up, n, new Vector3(-0.05f, -0.03f, -0.07f), new Vector3(0.05f, 0.05f, 0.03f), true);
            }
        }

        // ------------------------------------------------------------------ walk test
        /// <summary>A slug-sized CharacterController (r 0.18, h 0.56, 50°, step 0.15) walks each piece of the deck there and back.</summary>
        public static string WalkTest(WalkwayPath w)
        {
            if (w.deckLine == null || w.deckLine.Length < 2) return "rebuild first";
            var secs = w.sections != null && w.sections.Length > 0 ? w.sections : new[] { 0 };
            var go = new GameObject("WalkTester") { hideFlags = HideFlags.HideAndDontSave };
            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.18f; cc.height = 0.56f; cc.center = Vector3.up * 0.28f; cc.slopeLimit = 50f; cc.stepOffset = 0.15f; cc.skinWidth = 0.02f;
            Physics.SyncTransforms();
            var sb = new StringBuilder();
            for (int k = 0; k < secs.Length; k++)
            {
                int a = secs[k], b = k + 1 < secs.Length ? secs[k + 1] : w.deckLine.Length;
                var line = new List<Vector3>();
                for (int i = a; i < b; i++) line.Add(w.deckLine[i]);
                string there = Walk(cc, line);
                line.Reverse();
                string back = Walk(cc, line);
                sb.Append($"{(secs.Length > 1 ? $"piece {k + 1}: " : "")}there {there ?? "OK"}, back {back ?? "OK"}; ");
            }
            Object.DestroyImmediate(go);
            return sb.ToString();
        }

        static string Walk(CharacterController cc, List<Vector3> line)
        {
            cc.enabled = false;
            cc.transform.position = line[0] + Vector3.up * 0.4f;
            cc.enabled = true;
            Physics.SyncTransforms();
            for (int i = 1; i < line.Count; i++)
            {
                var target = line[i];
                float floor = Mathf.Min(target.y, line[i - 1].y) - 1.5f;
                float best = float.MaxValue; int since = 0;
                for (int step = 0; step < 6000; step++)
                {
                    var p = cc.transform.position;
                    var d = target - p; d.y = 0f;
                    float dist = d.magnitude;
                    if (dist < 0.35f) break;
                    if (dist < best - 0.01f) { best = dist; since = 0; }
                    else if (++since > 80) return $"stuck before point {i} at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0})";
                    cc.Move(d / dist * 0.04f + Vector3.down * 0.06f);
                    if (cc.transform.position.y < floor) return $"fell before point {i} at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0})";
                }
            }
            return null;
        }

        /// <summary>
        /// The real slug movement (SlugMoveController, the player's settings) climbs each ladder from its foot onto its
        /// platform, then climbs back down from the platform to the foot.
        /// </summary>
        public static string LadderTest(WalkwayPath w)
        {
            var built = w.transform.Find(WalkwayPath.BuiltName);
            if (!built) return "rebuild first";
            var player = Object.FindAnyObjectByType<SlugMoveController>();
            if (!player || !player.settings) return "no slug with settings in the scene";
            var go = new GameObject("LadderTester") { hideFlags = HideFlags.HideAndDontSave };
            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.18f; cc.height = 0.56f; cc.center = Vector3.up * 0.28f; cc.skinWidth = 0.02f;
            var mc = go.AddComponent<SlugMoveController>();
            mc.settings = player.settings; mc.driven = true;
            typeof(SlugMoveController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(mc, null);
            var sb = new StringBuilder();
            foreach (var l in built.GetComponentsInChildren<SlugLadder>())
            {
                // up: from the foot, facing the rock
                Place(go, cc, l.foot + Vector3.up * 0.05f, l.into);
                bool climbed = false; float t = 0f;
                for (; t < 40f; t += 0.02f)
                {
                    mc.Simulate(0.02f, Toward(go, l.into), false);
                    if (mc.Climbing) climbed = true;
                    if (climbed && !mc.Climbing) break;
                }
                var p = go.transform.position;
                bool onTop = climbed && !mc.Climbing && p.y > l.top.y - 0.2f && Vector3.Dot(p - l.top, l.into) > 0.1f;
                sb.Append($"{l.name} up {(onTop ? $"OK ({t:F1} s)" : $"FAILED at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}){(climbed ? "" : ", never got on")}")}");
                // down: from the platform, walking off over the ladder's top
                Place(go, cc, l.top + l.into * 0.45f + Vector3.up * 0.02f, -l.into);
                climbed = false; t = 0f;
                for (; t < 40f; t += 0.02f)
                {
                    mc.Simulate(0.02f, Toward(go, -l.into), false);
                    if (mc.Climbing) climbed = true;
                    if (climbed && !mc.Climbing) break;
                }
                p = go.transform.position;
                bool down = climbed && !mc.Climbing && p.y < l.foot.y + 0.4f;
                sb.Append($", down {(down ? $"OK ({t:F1} s)" : $"FAILED at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}){(climbed ? "" : ", never got on")}")}; ");
            }
            Object.DestroyImmediate(go);
            return sb.ToString();
        }

        // the move keys that head this way in the world (the eyes look where the body faces: no camera here)
        static Vector2 Toward(GameObject go, Vector3 world)
        {
            var local = Quaternion.Inverse(Quaternion.Euler(0f, go.transform.eulerAngles.y, 0f)) * new Vector3(world.x, 0f, world.z).normalized;
            return new Vector2(local.x, local.z);
        }

        static void Place(GameObject go, CharacterController cc, Vector3 p, Vector3 face)
        {
            cc.enabled = false;
            go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(face.x, 0f, face.z)));
            cc.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
