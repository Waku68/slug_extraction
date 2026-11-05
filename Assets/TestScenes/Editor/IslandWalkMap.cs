using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800: where a slug can walk, over the whole island (Nov 5, わくさん「島全体で通路をマッピングしてほしい。足りないところとか見たい」).
    /// Looks straight down every 0.94 m (one terrain height sample) and takes the top surface: the hand-shaped terrain AND
    /// the placed parts (ways, bridges, walkways, rocks). A spot is walkable when it is no steeper than 50°; two spots next to
    /// each other join when the step between them is one the slug can take (its slope, plus a 0.45 m climb). Falling is free
    /// (no fall damage), so a slug can always drop off a cliff to whatever walkable ground it slides down to.
    /// Walkways and bridges (WalkwayPath, Map/通路) are laid in from their deck lines, since they are narrower than two cells.
    /// Out (Screenshots/IslandWalk/): a picture from above and a list.
    ///  - green: the main walkable ground (the biggest piece); yellow: where it is 6 m wide or less (narrow ways, chokepoints)
    ///  - light green: other pieces you can drop into and get back out of by another way
    ///  - orange: you can drop in, but there is no way back up (a trap)
    ///  - red: you can't get there at all (a plateau or ledge with no way up)
    ///  - grey: too steep (rock), blue: sea, brown tint: a walkway or bridge
    /// Numbers on the picture are the list's numbers. Only looks at the top surface: caves and the ground under bridges
    /// are not seen. Heightmap-and-ray checks are a first pass; walk a slug-sized CharacterController to prove a way.
    /// </summary>
    public static class IslandWalkMap
    {
        public const string OutDir = "Screenshots/IslandWalk";
        const float MaxSlope = 50f;     // the slug walks up to this
        const float Climb = 0.45f;      // ... and climbs a ledge this high
        const float MinArea = 30f;      // smaller pieces (a rock top, a tree stump) are not listed (m²)
        const float SeaLevel = 0.25f;
        const float NarrowWidth = 6f;   // a way this wide or less is a narrow way (a person: 21 m)

        [MenuItem("Slug/IslandTest 800 m/Walk Map")]
        public static string Run() => Run("walk_map");

        public static string Run(string tag)
        {
            var t = Object.FindAnyObjectByType<Terrain>();
            if (!t) return "open IslandTest_800 first";
            var d = t.terrainData;
            int n = d.heightmapResolution;
            float cell = d.size.x / (n - 1);
            Vector3 o = t.transform.position;
            Physics.SyncTransforms();

            var h = new float[n * n];
            var tan = new float[n * n];
            var kind = new byte[n * n];      // 0 sea, 1 too steep, 2 walkable
            float cosMax = Mathf.Cos(MaxSlope * Mathf.Deg2Rad);
            float top = o.y + d.size.y + 80f;
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    int i = z * n + x;
                    var p = new Vector3(o.x + x * cell, top, o.z + z * cell);
                    if (!Physics.Raycast(p, Vector3.down, out var hit, d.size.y + 200f, ~0, QueryTriggerInteraction.Ignore)
                        || hit.point.y < SeaLevel || hit.collider.name == "Sea")
                    { h[i] = 0f; kind[i] = 0; continue; }
                    h[i] = hit.point.y;
                    float ny = Mathf.Clamp(hit.normal.y, 0.01f, 1f);
                    tan[i] = Mathf.Sqrt(1f - ny * ny) / ny;
                    kind[i] = (byte)(ny >= cosMax ? 2 : 1);
                }

            // ---- walkways and bridges: marked here (drawn brown); joined below as one link through every piece they touch
            //      (they are narrower than two cells and can lie one over another, so the rays alone can't follow them) ----
            var deck = new bool[n * n];
            var walkways = Object.FindObjectsByType<SlugWorld.WalkwayPath>(FindObjectsInactive.Exclude);
            foreach (var w in walkways)
            {
                var L = w.deckLine;
                if (L == null || L.Length < 2) continue;
                for (int k = 0; k < L.Length; k++)
                {
                    var f = L[Mathf.Min(L.Length - 1, k + 1)] - L[Mathf.Max(0, k - 1)];
                    var side = Vector3.Cross(Vector3.up, new Vector3(f.x, 0f, f.z)).normalized;
                    for (float o2 = -0.5f * w.width; o2 <= 0.5f * w.width + 1e-3f; o2 += 0.3f)
                    {
                        var q = L[k] + side * o2;
                        int qx = Mathf.RoundToInt((q.x - o.x) / cell), qz = Mathf.RoundToInt((q.z - o.z) / cell);
                        if (qx < 0 || qz < 0 || qx >= n || qz >= n) continue;
                        int qi = qz * n + qx;
                        deck[qi] = true;
                    }
                }
            }

            // ---- pieces of walkable ground that join without falling ----
            int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 }, dz = { 0, 0, 1, -1, 1, -1, 1, -1 };
            float tanMax = Mathf.Tan(MaxSlope * Mathf.Deg2Rad);
            var comp = Enumerable.Repeat(-1, n * n).ToArray();
            var size = new List<int>();
            var queue = new int[n * n];
            for (int s = 0; s < n * n; s++)
            {
                if (kind[s] != 2 || comp[s] >= 0) continue;
                int c = size.Count, qh = 0, qt = 0, count = 0;
                comp[s] = c; queue[qt++] = s;
                while (qh < qt)
                {
                    int a = queue[qh++]; count++;
                    int ax = a % n, az = a / n;
                    for (int k = 0; k < 8; k++)
                    {
                        int bx = ax + dx[k], bz = az + dz[k];
                        if (bx < 0 || bz < 0 || bx >= n || bz >= n) continue;
                        int b = bz * n + bx;
                        if (kind[b] != 2 || comp[b] >= 0) continue;
                        if (!Joins(h, tan, a, b, k < 4 ? cell : cell * 1.414f, tanMax)) continue;
                        comp[b] = c; queue[qt++] = b;
                    }
                }
                size.Add(count);
            }
            int nc = size.Count;

            // a walkway joins every piece whose top it lies on (the deck itself, or the ground at its ends and benches)
            var root = Enumerable.Range(0, nc).ToArray();
            int Find(int x) { while (root[x] != x) { root[x] = root[root[x]]; x = root[x]; } return x; }
            foreach (var w in walkways)
            {
                if (w.deckLine == null) continue;
                int first = -1;
                foreach (var p in w.deckLine)
                    foreach (var off in new[] { Vector3.zero, Vector3.right * 0.5f, Vector3.left * 0.5f, Vector3.forward * 0.5f, Vector3.back * 0.5f })
                    {
                        var q = p + off;
                        int qx = Mathf.RoundToInt((q.x - o.x) / cell), qz = Mathf.RoundToInt((q.z - o.z) / cell);
                        if (qx < 0 || qz < 0 || qx >= n || qz >= n) continue;
                        int qi = qz * n + qx;
                        if (comp[qi] < 0 || Mathf.Abs(h[qi] - p.y) > 0.5f) continue;
                        int c = Find(comp[qi]);
                        if (first < 0) first = c; else if (c != first) { root[c] = first; size[first] += size[c]; size[c] = 0; }
                    }
            }
            for (int i = 0; i < n * n; i++) if (comp[i] >= 0) comp[i] = Find(comp[i]);

            // ---- falling: off a ledge, or down a steep face to where it slides out ----
            var landing = Enumerable.Repeat(-2, n * n).ToArray();   // -2 not yet, -1 nowhere (sea or a pit)
            var drops = new HashSet<long>();
            var path = new List<int>();
            for (int a = 0; a < n * n; a++)
            {
                if (kind[a] != 2) continue;
                int ax = a % n, az = a / n;
                for (int k = 0; k < 8; k++)
                {
                    int bx = ax + dx[k], bz = az + dz[k];
                    if (bx < 0 || bz < 0 || bx >= n || bz >= n) continue;
                    int b = bz * n + bx;
                    if (h[b] > h[a] - 0.3f || kind[b] == 0) continue;
                    int to = kind[b] == 2 ? b : Land(b, n, h, kind, landing, path, dx, dz);
                    if (to >= 0 && comp[to] != comp[a]) drops.Add((long)comp[a] * nc + comp[to]);
                }
            }
            var fwd = new List<int>[nc]; var back = new List<int>[nc];
            for (int c = 0; c < nc; c++) { fwd[c] = new List<int>(); back[c] = new List<int>(); }
            foreach (var e in drops) { int a = (int)(e / nc), b = (int)(e % nc); fwd[a].Add(b); back[b].Add(a); }

            int main = 0;
            for (int c = 1; c < nc; c++) if (size[c] > size[main]) main = c;
            var reach = Spread(main, fwd, nc);     // can get there from the main ground
            var home = Spread(main, back, nc);     // can get back to the main ground from there
            float area1 = cell * cell;

            // classes: 0 main, 1 there and back, 2 trap, 3 can't get there, 4 too small to list
            var cls = new byte[nc];
            for (int c = 0; c < nc; c++)
                cls[c] = (byte)(c == main ? 0 : size[c] * area1 < MinArea ? 4 : reach[c] && home[c] ? 1 : reach[c] ? 2 : 3);

            // ---- list the problems, biggest first, with the nearest main ground ----
            var issues = Enumerable.Range(0, nc).Where(c => cls[c] == 2 || cls[c] == 3).OrderByDescending(c => size[c]).ToList();
            var sumX = new double[nc]; var sumZ = new double[nc]; var lo = new float[nc]; var hi = new float[nc];
            for (int c = 0; c < nc; c++) { lo[c] = 999f; hi[c] = -999f; }
            for (int i = 0; i < n * n; i++)
            {
                int c = comp[i]; if (c < 0) continue;
                sumX[c] += i % n; sumZ[c] += i / n; lo[c] = Mathf.Min(lo[c], h[i]); hi[c] = Mathf.Max(hi[c], h[i]);
            }

            var sb = new StringBuilder();
            float total = 0f; foreach (var s in size) total += s * area1;
            sb.AppendLine($"# Walk map ({tag})  grid {cell:F2} m, walk ≤ {MaxSlope}°, climb {Climb} m, falling free");
            sb.AppendLine($"walkable {total / 10000f:F1} ha in {nc} pieces; main {size[main] * area1 / 10000f:F1} ha");
            sb.AppendLine($"there-and-back {Enumerable.Range(0, nc).Where(c => cls[c] == 1).Sum(c => size[c]) * area1:F0} m², trap {Enumerable.Range(0, nc).Where(c => cls[c] == 2).Sum(c => size[c]) * area1:F0} m², can't reach {Enumerable.Range(0, nc).Where(c => cls[c] == 3).Sum(c => size[c]) * area1:F0} m²");
            sb.AppendLine();
            sb.AppendLine("no | kind | area m² | centre (x, z) | height | nearest main ground: gap m, its height (main − this)");
            var labelAt = new List<(int num, int x, int z)>();
            int num = 0;
            foreach (var c in issues.Take(40))
            {
                num++;
                int cx = (int)(sumX[c] / size[c]), cz = (int)(sumZ[c] / size[c]);
                // the cell of this piece nearest its centre
                int best = -1; float bd = float.MaxValue;
                for (int z = Mathf.Max(0, cz - 120); z < Mathf.Min(n, cz + 120); z++)
                    for (int x = Mathf.Max(0, cx - 120); x < Mathf.Min(n, cx + 120); x++)
                    {
                        int i = z * n + x; if (comp[i] != c) continue;
                        float dd = (x - cx) * (x - cx) + (z - cz) * (z - cz);
                        if (dd < bd) { bd = dd; best = i; }
                    }
                if (best < 0) best = cz * n + cx;
                var (gap, mh, fromH) = NearestMain(c, main, comp, h, n, cell, queue);
                string near = gap < 0 ? "none within 80 m" : $"{gap:F0} m, {mh:F1} m ({mh - fromH:+0.0;-0.0})";
                var w = new Vector3(o.x + (best % n) * cell, 0, o.z + (best / n) * cell);
                sb.AppendLine($"{num} | {(cls[c] == 2 ? "trap (drop in, no way up)" : "can't reach")} | {size[c] * area1:F0} | ({w.x:F0}, {w.z:F0}) | {lo[c]:F0}–{hi[c]:F0} m | {near}");
                labelAt.Add((num, best % n, best / n));
            }

            // ---- walkways: which pieces each one runs through (a change of piece along a deck is a break) ----
            sb.AppendLine();
            sb.AppendLine("walkway | pieces along it (no = list number, main, ok = there and back, - = small)");
            foreach (var w in Object.FindObjectsByType<SlugWorld.WalkwayPath>(FindObjectsInactive.Exclude))
            {
                if (w.deckLine == null) continue;
                var seq = new StringBuilder(); int lastC = -2;
                foreach (var p in w.deckLine)
                {
                    int qx = Mathf.RoundToInt((p.x - o.x) / cell), qz = Mathf.RoundToInt((p.z - o.z) / cell);
                    if (qx < 0 || qz < 0 || qx >= n || qz >= n) continue;
                    int c = comp[qz * n + qx];
                    if (c == lastC) continue;
                    lastC = c;
                    int li = c < 0 ? -1 : issues.IndexOf(c);
                    string tagC = c < 0 ? "?" : cls[c] == 0 ? "main" : cls[c] == 1 ? "ok" : li >= 0 ? $"no{li + 1}" : "-";
                    seq.Append($"{tagC}@({p.x:F0},{p.y:F0},{p.z:F0}) ");
                }
                sb.AppendLine($"{w.name} | {seq}");
            }

            // ---- named places ----
            sb.AppendLine();
            sb.AppendLine("place | piece");
            var places = new List<Transform>();
            var mapRoot = GameObject.Find("Map");
            if (mapRoot)
            {
                foreach (Transform ch in mapRoot.transform) if (ch.name.StartsWith("Label_")) places.Add(ch);
                var look = mapRoot.transform.Find("見る場所");
                if (look) foreach (Transform ch in look) places.Add(ch);
            }
            foreach (var pl in places)
            {
                int px = Mathf.RoundToInt((pl.position.x - o.x) / cell), pz = Mathf.RoundToInt((pl.position.z - o.z) / cell);
                int c = NearestComp(px, pz, n, comp, 4);
                string what = c < 0 ? "no walkable ground within 4 m" : cls[c] == 0 ? "main" : cls[c] == 1 ? "there and back" : cls[c] == 2 ? "TRAP" : cls[c] == 3 ? "CAN'T REACH" : "small piece";
                int li = issues.IndexOf(c);
                sb.AppendLine($"{pl.name.Replace("Label_", "")} | {what}{(li >= 0 && li < 40 ? $" (no {li + 1})" : "")}");
            }

            // ---- narrow ways: the walkable width through a spot is the shortest of its four runs (across, along, both
            //      diagonals) over ground of the same piece; ≤ NarrowWidth is a narrow way (the island's real passages) ----
            var run = new float[n * n];
            for (int i = 0; i < n * n; i++) run[i] = float.MaxValue;
            int[][] dirs = { new[] { 1, 0 }, new[] { 0, 1 }, new[] { 1, 1 }, new[] { 1, -1 } };
            var lineCells = new List<int>();
            foreach (var dv in dirs)
            {
                float stepLen = cell * Mathf.Sqrt(dv[0] * dv[0] + dv[1] * dv[1]);
                var seen = new bool[n * n];
                for (int st = 0; st < n * n; st++)
                {
                    if (seen[st] || comp[st] < 0) continue;
                    // walk back to the start of this run
                    int sx = st % n, sz = st / n, c0 = comp[st];
                    while (true)
                    {
                        int px = sx - dv[0], pz = sz - dv[1];
                        if (px < 0 || pz < 0 || px >= n || pz >= n || comp[pz * n + px] != c0) break;
                        sx = px; sz = pz;
                    }
                    lineCells.Clear();
                    for (int x = sx, z = sz; x >= 0 && z >= 0 && x < n && z < n && comp[z * n + x] == c0; x += dv[0], z += dv[1]) { lineCells.Add(z * n + x); seen[z * n + x] = true; }
                    float len = lineCells.Count * stepLen;
                    foreach (var ci in lineCells) run[ci] = Mathf.Min(run[ci], len);
                }
            }
            int narrowCells = 0;
            for (int i = 0; i < n * n; i++) if (comp[i] == main && run[i] <= NarrowWidth) narrowCells++;
            sb.AppendLine();
            sb.AppendLine($"narrow ways (main ground {NarrowWidth} m wide or less, yellow): {narrowCells * area1:F0} m²");

            // ---- the picture ----
            var px2 = new Color[n * n];
            var light = new Vector3(-0.5f, 0.75f, 0.45f).normalized;
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    int i = z * n + x;
                    if (kind[i] == 0) { px2[i] = new Color(0.27f, 0.42f, 0.6f); continue; }
                    float hx = h[z * n + Mathf.Min(n - 1, x + 1)] - h[z * n + Mathf.Max(0, x - 1)];
                    float hz = h[Mathf.Min(n - 1, z + 1) * n + x] - h[Mathf.Max(0, z - 1) * n + x];
                    var nrm = new Vector3(-hx, 2f * cell, -hz).normalized;
                    float shade = 0.62f + 0.45f * Mathf.Clamp01(Vector3.Dot(nrm, light));
                    Color col;
                    if (kind[i] == 1) col = new Color(0.55f, 0.53f, 0.52f);
                    else
                        switch (cls[comp[i]])
                        {
                            case 0: col = run[i] <= NarrowWidth ? new Color(0.95f, 0.85f, 0.25f) : new Color(0.47f, 0.72f, 0.38f); break;
                            case 1: col = new Color(0.66f, 0.85f, 0.5f); break;
                            case 2: col = new Color(1f, 0.6f, 0.15f); break;
                            case 3: col = new Color(0.9f, 0.15f, 0.15f); break;
                            default: col = new Color(0.62f, 0.62f, 0.5f); break;
                        }
                    if (h[i] > 1f && Mathf.Repeat(h[i], 10f) < 0.4f) shade *= 0.85f;   // a line every 10 m of height
                    if (deck[i]) col = Color.Lerp(col, new Color(0.55f, 0.33f, 0.15f), 0.6f);   // walkways and bridges, brown
                    px2[i] = col * shade;
                }
            foreach (var pl in places)
            {
                int px = Mathf.RoundToInt((pl.position.x - o.x) / cell), pz = Mathf.RoundToInt((pl.position.z - o.z) / cell);
                Disc(px2, n, px, pz, 4, new Color(0.1f, 0.1f, 0.35f));
            }
            foreach (var (nm, lx, lz) in labelAt) Number(px2, n, lx, lz, nm);

            var tex = new Texture2D(n, n, TextureFormat.RGB24, false);
            tex.SetPixels(px2); tex.Apply();
            Directory.CreateDirectory(OutDir);
            File.WriteAllBytes($"{OutDir}/{tag}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            File.WriteAllText($"{OutDir}/{tag}.txt", sb.ToString());
            return sb.ToString();
        }

        static bool Joins(float[] h, float[] tan, int a, int b, float dist, float tanMax)
        {
            float allowed = dist * Mathf.Min(tanMax, Mathf.Max(tan[a], tan[b]) + 0.1f) + Climb;
            return Mathf.Abs(h[a] - h[b]) <= allowed;
        }

        // slide down a steep face, always to the lowest neighbour, until the ground can be walked on
        static int Land(int s, int n, float[] h, byte[] kind, int[] landing, List<int> path, int[] dx, int[] dz)
        {
            path.Clear();
            int cur = s, result = -1;
            for (int guard = 0; guard < 6000; guard++)
            {
                if (landing[cur] != -2) { result = landing[cur]; break; }
                if (kind[cur] == 2) { result = cur; break; }
                if (kind[cur] == 0) { result = -1; break; }
                path.Add(cur);
                int cx = cur % n, cz = cur / n, low = -1; float lh = h[cur];
                for (int k = 0; k < 8; k++)
                {
                    int bx = cx + dx[k], bz = cz + dz[k];
                    if (bx < 0 || bz < 0 || bx >= n || bz >= n) continue;
                    int b = bz * n + bx;
                    float bh = kind[b] == 0 ? -1f : h[b];
                    if (bh < lh) { lh = bh; low = b; }
                }
                if (low < 0) { result = -1; break; }   // a pit of steep ground
                cur = low;
            }
            foreach (var p in path) landing[p] = result;
            return result;
        }

        static bool[] Spread(int start, List<int>[] edges, int nc)
        {
            var seen = new bool[nc];
            var st = new Stack<int>(); st.Push(start); seen[start] = true;
            while (st.Count > 0) foreach (var b in edges[st.Pop()]) if (!seen[b]) { seen[b] = true; st.Push(b); }
            return seen;
        }

        // grow out from the piece over any ground until the main ground (up to 80 m)
        static (float gap, float mainH, float fromH) NearestMain(int c, int main, int[] comp, float[] h, int n, float cell, int[] queue)
        {
            int lim = Mathf.CeilToInt(80f / cell);
            int minX = n, minZ = n, maxX = 0, maxZ = 0;
            for (int i = 0; i < n * n; i++)
                if (comp[i] == c) { int x = i % n, z = i / n; minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z); }
            int x0 = Mathf.Max(0, minX - lim), x1 = Mathf.Min(n - 1, maxX + lim), z0 = Mathf.Max(0, minZ - lim), z1 = Mathf.Min(n - 1, maxZ + lim);
            int w = x1 - x0 + 1, hh = z1 - z0 + 1;
            var dist = new int[w * hh]; var from = new int[w * hh];
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;
            int qh = 0, qt = 0;
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                    if (comp[z * n + x] == c) { int li = (z - z0) * w + (x - x0); dist[li] = 0; from[li] = z * n + x; queue[qt++] = li; }
            while (qh < qt)
            {
                int li = queue[qh++]; int lx = li % w, lz = li / w;
                int gi = (lz + z0) * n + (lx + x0);
                if (comp[gi] == main) return (dist[li] * cell, h[gi], h[from[li]]);
                if (dist[li] >= lim) continue;
                for (int k = 0; k < 4; k++)
                {
                    int bx = lx + (k == 0 ? 1 : k == 1 ? -1 : 0), bz = lz + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (bx < 0 || bz < 0 || bx >= w || bz >= hh) continue;
                    int bi = bz * w + bx; if (dist[bi] >= 0) continue;
                    dist[bi] = dist[li] + 1; from[bi] = from[li]; queue[qt++] = bi;
                }
            }
            return (-1f, 0f, 0f);
        }

        static int NearestComp(int px, int pz, int n, int[] comp, int r)
        {
            int best = -1; int bd = int.MaxValue;
            for (int z = pz - r; z <= pz + r; z++)
                for (int x = px - r; x <= px + r; x++)
                {
                    if (x < 0 || z < 0 || x >= n || z >= n) continue;
                    int c = comp[z * n + x]; if (c < 0) continue;
                    int dd = (x - px) * (x - px) + (z - pz) * (z - pz);
                    if (dd < bd) { bd = dd; best = c; }
                }
            return best;
        }

        static void Disc(Color[] px, int n, int cx, int cz, int r, Color col)
        {
            for (int z = cz - r; z <= cz + r; z++)
                for (int x = cx - r; x <= cx + r; x++)
                    if (x >= 0 && z >= 0 && x < n && z < n && (x - cx) * (x - cx) + (z - cz) * (z - cz) <= r * r) px[z * n + x] = col;
        }

        // 3×5 digits, drawn 3 px a dot, white box behind
        static readonly string[] Digits = { "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001", "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111" };
        static void Number(Color[] px, int n, int cx, int cz, int value)
        {
            string s = value.ToString();
            const int sc = 3;
            int wpx = s.Length * 4 * sc + sc, hpx = 7 * sc;
            int x0 = cx - wpx / 2, z0 = cz - hpx / 2;
            for (int z = 0; z < hpx; z++)
                for (int x = 0; x < wpx; x++)
                {
                    int X = x0 + x, Z = z0 + z;
                    if (X >= 0 && Z >= 0 && X < n && Z < n) px[Z * n + X] = Color.white;
                }
            for (int k = 0; k < s.Length; k++)
            {
                string g = Digits[s[k] - '0'];
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        if (g[row * 3 + col] != '1') continue;
                        for (int yy = 0; yy < sc; yy++)
                            for (int xx = 0; xx < sc; xx++)
                            {
                                int X = x0 + sc + (k * 4 + col) * sc + xx, Z = z0 + sc + (4 - row) * sc + yy;
                                if (X >= 0 && Z >= 0 && X < n && Z < n) px[Z * n + X] = Color.black;
                            }
                    }
            }
        }
    }
}
