using System.Collections.Generic;
using SlugMove;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800, part 7: the caves (Oct 10). The terrain is one height per point, so a cave is a model: tunnels
    /// (an arched tube with a flat floor) and chambers (a rough half dome), lined in rock, with holes punched in the
    /// terrain only where its surface would cut into them (the mouths, the skylights). Two caves:
    /// - the salt cave behind the falls: a half dome 13 m across under the pool, one way in from the ledge behind
    ///   the curtain, another out onto the mountain's slope to the east. Rock salt inside
    /// - the lava tube under the smooth (pahoehoe) flow: from a vent at the cone's breach, under the three skylights
    ///   (the middle one has a pile of fallen roof under it, so it can be climbed; the other two can only be
    ///   dropped into), then splitting into three mouths in the sea cliff, onto the rock platform the waves have cut
    /// </summary>
    public static partial class IslandTest800Builder
    {
        class Tube { public string name; public List<Vector3> pts = new List<Vector3>(); public float halfW, height; public bool lava, capStart, capEnd; }
        class Dome { public string name; public Vector3 floor; public float r, height; }

        static List<Tube> _tubes = new List<Tube>();
        static List<Dome> _domes = new List<Dome>();
        static readonly List<Vector3> _mouths = new List<Vector3>();          // ways in from outside (framed with rocks)
        static readonly List<Vector3> _openings = new List<Vector3>();        // other places meant to be open (for Check Cave Gaps)
        class SkyFrame { public Vector2 c, along; public float floor; public Tube tube; }
        static readonly List<SkyFrame> _skyFrames = new List<SkyFrame>();
        static float[] _planHm;                                                // the ground the caves were planned on
        const float SkyAlong = 3f, SkyAcross = 1.9f;                           // the skylight's hole: an ellipse along the tube
        const float SaltDomeR = 4.5f, PlatformH = 1.2f, CaveDoorHalfW = 1.4f;

        // ------------------------------------------------------------------ planning (on the finished heights)
        static void PlanCaves(float[] hm)
        {
            _tubes.Clear(); _domes.Clear(); _mouths.Clear(); _openings.Clear(); _skyFrames.Clear();
            _planHm = hm;
            float H(Vector2 w) => HmAt(hm, w.x, w.y);

            // ---- the salt cave behind the falls
            if (_fallOK)
            {
                float floor = LedgeH;
                Vector2 c = _fallC - _fallD * (FallR + AlcoveDeep + 2.5f + SaltDomeR);
                _domes.Add(new Dome { name = "岩塩の洞窟", floor = new Vector3(c.x, floor, c.y), r = SaltDomeR, height = SaltDomeR * 0.78f });
                // from the doorway in the alcove's back wall (BuildFalls leaves it open) to the dome
                var a = new Tube { name = "岩塩の洞窟（滝の裏から）", halfW = CaveDoorHalfW, height = AlcoveHigh - 0.1f };
                Vector2 a0 = FallAt(0f, FallR + AlcoveDeep - 0.3f), a1 = c + _fallD * (SaltDomeR - 1f);
                a.pts.Add(new Vector3(a0.x, floor, a0.y)); a.pts.Add(new Vector3(a1.x, floor, a1.y));
                _tubes.Add(a);
                // the other way out: toward the east, climbing at up to 25°, wherever it reaches the slope soonest
                Vector2 east = _fallS.x > 0f ? _fallS : -_fallS, best = Vector2.zero;
                float bestL = float.MaxValue, climb = Mathf.Tan(25f * Mathf.Deg2Rad);
                for (float ang = -70f; ang <= 70f; ang += 5f)
                {
                    Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, ang) * east);
                    for (float l = SaltDomeR + 1f; l < 40f; l += 0.5f)
                    {
                        var p = c + dir * l;
                        if ((p - _fallC).magnitude < FallR + 4f) break;            // not through the plunge pool
                        if (H(p) <= floor + (l - SaltDomeR) * climb + 0.4f) { if (l < bestL) { bestL = l; best = dir; } break; }
                    }
                }
                if (bestL < float.MaxValue)
                {
                    var b = new Tube { name = "岩塩の洞窟（東の斜面へ）", halfW = 1.3f, height = 2.3f };
                    for (float l = SaltDomeR - 1f; l <= bestL + 0.6f; l += 1f)
                    {
                        var p = c + best * l;
                        b.pts.Add(new Vector3(p.x, floor + Mathf.Max(0f, l - SaltDomeR) * climb, p.y));
                    }
                    _tubes.Add(b);
                    var m = c + best * bestL;
                    _mouths.Add(new Vector3(m.x, floor + (bestL - SaltDomeR) * climb, m.y));
                }
                else Debug.LogWarning("[IslandTest800] the salt cave found no way out to the east");
                _mouths.Add(new Vector3(a0.x, floor, a0.y));
            }

            // ---- the lava tube under the smooth flow: from the collapse at the vent down to the inlet's head (IslandTest800Inlet.cs)
            {
                var path = new List<Vector2>();
                foreach (var s in new[] { new Vector2(392, 147), new Vector2(402, 147), new Vector2(412, 148), new Vector2(428, 150) }) path.Add(W(s));
                Vector2 end = _inletOK ? InletPoint(InletCenterA, 0f) - _inletU * (InletWaterR + InletLedgeW + 0.1f) : W(446, 155);
                path.Add(end);
                var main = new Tube { name = "溶岩洞", halfW = 2.6f, height = 4f, lava = true, capStart = true, capEnd = !_inletOK };
                var pts = Resample(path, 2f);
                float prev = float.MaxValue;
                for (int i = 0; i < pts.Count; i++)
                {
                    // 6 m under the flow, always down toward the sea; the last 24 m come down to the inlet's ledge
                    float f = H(pts[i]) - 6f;
                    if (_inletOK)
                    {
                        float k = 1f - Mathf.Clamp01((pts.Count - 1 - i) * 2f / 24f);
                        f = Mathf.Lerp(f, PlatformH + 0.05f, k * k * (3f - 2f * k));
                    }
                    f = Mathf.Min(f, prev - 0.02f);
                    prev = f;
                    main.pts.Add(new Vector3(pts[i].x, f, pts[i].y));
                }
                _tubes.Add(main);
                // where the roof has fallen in: the skylights over it (the fourth one is in the inlet's roof)
                for (int i = 0; i < 6; i += 2)
                {
                    var w = W(Skylights[i], Skylights[i + 1]);
                    if (!NearTube(main, w.x, w.y, 4f, out float f)) continue;
                    int seg = 0; float bd = float.MaxValue;
                    for (int q = 0; q + 1 < main.pts.Count; q++) { float d = (new Vector2(main.pts[q].x, main.pts[q].z) - w).sqrMagnitude; if (d < bd) { bd = d; seg = q; } }
                    var along = new Vector2(main.pts[seg + 1].x - main.pts[seg].x, main.pts[seg + 1].z - main.pts[seg].z).normalized;
                    _skyFrames.Add(new SkyFrame { c = w, along = along, floor = f, tube = main });
                }
                if (_inletOK)
                {
                    _openings.Add(new Vector3(end.x, PlatformH, end.y));
                    for (float a = InletCenterA - InletWaterR; a < 12f; a += 5f) { var q = InletPoint(a, 0f); _openings.Add(new Vector3(q.x, 0f, q.y)); }
                    var sky = InletSky;
                    _openings.Add(new Vector3(sky.x, 0f, sky.y));

                    // the branch from the head's south side up to a pit at the kipuka's edge, climbing at up to 25 degrees
                    Vector2 p0 = InletPoint(InletCenterA, 0f), d0 = (-_inletU * 0.5f - _inletV * 0.866f).normalized;
                    Vector2 s0 = p0 + d0 * (InletWaterR + InletLedgeW + 0.1f), s1 = s0 + d0 * 3f, kip = W(Kipukas[6], Kipukas[7]);
                    float climb = Mathf.Tan(25f * Mathf.Deg2Rad), bestScore = float.MaxValue, bestL = 0f;
                    Vector2 bestDir = Vector2.zero, toKip = (kip - s1).normalized;
                    for (float ang = -40f; ang <= 40f; ang += 5f)
                    {
                        Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, ang) * toKip);
                        for (float l = 10f; l < 60f; l += 0.5f)
                        {
                            var q = s1 + dir * l;
                            if (InletLocal(q.x, q.y, out _, out float rr) && rr < InletOuterR + 3f) break;     // not back into the inlet
                            if (H(q) <= PlatformH + l * climb + 0.4f) { float score = l + Mathf.Abs(ang) * 0.1f; if (score < bestScore) { bestScore = score; bestL = l; bestDir = dir; } break; }
                        }
                    }
                    if (bestL > 0f)
                    {
                        var br = new Tube { name = "溶岩洞（キプカの落ち穴へ）", halfW = 2f, height = 3.2f, lava = true };
                        br.pts.Add(new Vector3(s0.x, PlatformH + 0.05f, s0.y));
                        br.pts.Add(new Vector3(s1.x, PlatformH + 0.05f, s1.y));
                        for (float l = 1f; l <= bestL + 0.6f; l += 1f) { var q = s1 + bestDir * l; br.pts.Add(new Vector3(q.x, PlatformH + 0.05f + l * climb, q.y)); }
                        _tubes.Add(br);
                        _openings.Add(new Vector3(s0.x, PlatformH, s0.y));
                        var m = s1 + bestDir * bestL;
                        _mouths.Add(new Vector3(m.x, PlatformH + bestL * climb, m.y));
                    }
                    else Debug.LogWarning("[IslandTest800] the branch to the kipuka found no way up");
                }
            }
        }

        // ------------------------------------------------------------------ holes in the terrain where it would cut into a cave
        static bool NearTube(Tube t, float x, float z, float margin, out float floor) => NearTube(t, x, z, margin, out floor, out _);

        static bool NearTube(Tube t, float x, float z, float margin, out float floor, out float dist) => NearTube(t, x, z, margin, out floor, out dist, out _);

        /// <summary>past: the nearest point is beyond one of the tube's ends (nothing of the tube is over it).</summary>
        static bool NearTube(Tube t, float x, float z, float margin, out float floor, out float dist, out bool past)
        {
            floor = 0f;
            past = false;
            float best = float.MaxValue;
            for (int i = 0; i + 1 < t.pts.Count; i++)
            {
                Vector3 a = t.pts[i], b = t.pts[i + 1];
                float ex = b.x - a.x, ez = b.z - a.z, raw = ((x - a.x) * ex + (z - a.z) * ez) / (ex * ex + ez * ez + 1e-6f), tt = Mathf.Clamp01(raw);
                float dx = x - a.x - ex * tt, dz = z - a.z - ez * tt, d = dx * dx + dz * dz;
                if (d < best) { best = d; floor = Mathf.Lerp(a.y, b.y, tt); past = (i == 0 && raw < 0f) || (i + 2 == t.pts.Count && raw > 1f); }
            }
            dist = Mathf.Sqrt(best);
            return dist <= t.halfW + margin;
        }

        static bool InSkylight(SkyFrame fr, float x, float z, float grow)
        {
            Vector2 d = new Vector2(x, z) - fr.c;
            float u = Vector2.Dot(d, fr.along) / (SkyAlong + grow), v = (d.x * fr.along.y - d.y * fr.along.x) / (SkyAcross + grow);
            return u * u + v * v < 1f;
        }

        static void PunchHoles(TerrainData data, float[] hm)
        {
            int n = data.holesResolution;
            var solid = new bool[n, n];
            float cell = Size / n;
            int holes = 0;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    solid[j, i] = true;
                    float x = -Size * 0.5f + (i + 0.5f) * cell, z = -Size * 0.5f + (j + 0.5f) * cell, h = HmAt(hm, x, z);
                    foreach (var fr in _skyFrames)
                        if (h > fr.floor && InSkylight(fr, x, z, 0.4f)) { solid[j, i] = false; break; }     // (the shaft's collar covers the ragged edge)
                    if (solid[j, i])
                    foreach (var t in _tubes)
                        if (NearTube(t, x, z, 0.3f, out float f, out float dd, out bool past) && !past && h > f - 0.6f && h < f + t.height * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Min(dd, t.halfW) / t.halfW, 2f))) + 0.3f) { solid[j, i] = false; break; }
                    if (solid[j, i])
                        foreach (var d in _domes)
                        {
                            float dd = new Vector2(x - d.floor.x, z - d.floor.z).magnitude;
                            if (dd <= d.r + 0.3f && h > d.floor.y - 0.6f && h < d.floor.y + d.height * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Min(dd, d.r) / d.r, 2f))) + 0.3f) { solid[j, i] = false; break; }
                        }
                    if (!solid[j, i]) holes++;
                }
            data.SetHoles(0, 0, solid);
            Debug.Log($"[IslandTest800] terrain holes for the caves: {holes}");
        }

        // ------------------------------------------------------------------ the models
        static Material CaveMat(string name, Color c)
        {
            var m = Mat(name, c);
            m.SetFloat("_Cull", 0f);                 // both sides: where a mouth meets the terrain, the outside of the rock shows
            EditorUtility.SetDirty(m);
            return m;
        }

        static bool InAnyTube(Vector3 p, Tube except, float grow)
        {
            foreach (var t in _tubes)
            {
                if (t == except) continue;
                if (NearTube(t, p.x, p.z, grow - t.halfW * 0.05f, out float f) && p.y > f - 0.3f && p.y < f + t.height + 0.2f) return true;
            }
            return false;
        }

        static Mesh TubeMesh(Tube t)
        {
            var path = new List<Vector2>();
            foreach (var p in t.pts) path.Add(new Vector2(p.x, p.z));
            var flat = Resample(path, 1.2f);
            var verts = new List<Vector3>();
            const int Arch = 12;
            int ring = Arch + 1 + 2;
            float along = 0f;
            for (int i = 0; i < flat.Count; i++)
            {
                if (i > 0) along += (flat[i] - flat[i - 1]).magnitude;
                NearTube(t, flat[i].x, flat[i].y, 99f, out float floor);
                Vector2 tan = (flat[Mathf.Min(flat.Count - 1, i + 1)] - flat[Mathf.Max(0, i - 1)]).normalized, side = new Vector2(-tan.y, tan.x);
                float wob = 1f + 0.12f * Fbm(along * 0.15f, 3f, 2, t.lava ? 131 : 133);
                for (int k = 0; k <= Arch; k++)
                {
                    float a = Mathf.PI * k / Arch, n = 1f + 0.12f * Fbm(along * 0.4f, a * 2f, 2, 135);
                    Vector2 q = flat[i] + side * Mathf.Cos(a) * t.halfW * wob * n;
                    verts.Add(new Vector3(q.x, floor + Mathf.Sin(a) * t.height * n * (t.lava ? 0.95f : 1f), q.y));
                }
                for (int k = 1; k <= 2; k++)
                {
                    Vector2 q = flat[i] + side * Mathf.Lerp(-t.halfW, t.halfW, k / 3f) * wob;
                    verts.Add(new Vector3(q.x, floor + 0.05f * Fbm(along * 0.5f, k, 2, 137), q.y));
                }
            }
            var tris = new List<int>();
            for (int i = 0; i + 1 < flat.Count; i++)
                for (int k = 0; k < ring; k++)
                {
                    int a = i * ring + k, b = i * ring + (k + 1) % ring, c = a + ring, d = b + ring;
                    var mid = (verts[a] + verts[b] + verts[c] + verts[d]) * 0.25f;
                    if (InAnyTube(mid, t, 0f)) continue;                                     // where a branch leaves the main tube
                    if (_planHm != null && AboveGround(verts[a]) && AboveGround(verts[b]) && AboveGround(verts[c]) && AboveGround(verts[d])) continue;   // where it comes out of the ground: open (a pit, a mouth), not a hump
                    if (t.lava && NearSkylight(verts[a]) && NearSkylight(verts[b]) && NearSkylight(verts[c]) && NearSkylight(verts[d])) continue;   // the skylights in the roof
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }
            // closed ends where a tube stops underground (cut where another tube goes on from it)
            for (int e = 0; e < 2; e++)
            {
                if (e == 0 ? !t.capStart : !t.capEnd) continue;
                int i0 = e == 0 ? 0 : (flat.Count - 1) * ring, centre = verts.Count;
                Vector3 cc = Vector3.zero;
                for (int k = 0; k < ring; k++) cc += verts[i0 + k];
                verts.Add(cc / ring);
                for (int k = 0; k < ring; k++)
                {
                    int a = i0 + k, b = i0 + (k + 1) % ring;
                    if (InAnyTube((verts[a] + verts[b] + verts[centre]) / 3f, t, 0f)) continue;
                    tris.AddRange(new[] { centre, a, b });
                }
            }
            var mesh = new Mesh { name = "IT_Cave_" + Mathf.Abs(t.name.GetHashCode()), indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static bool AboveGround(Vector3 p) => p.y > HmAt(_planHm, p.x, p.z) + 0.05f;

        static bool NearSkylight(Vector3 p)
        {
            foreach (var fr in _skyFrames)
                if (p.y > fr.floor + 1.5f && InSkylight(fr, p.x, p.z, 0f)) return true;
            return false;
        }

        /// <summary>The skylight's shaft: from the tube's roof up through the ground (so the sky shows only straight up).</summary>
        static Mesh ShaftMesh(SkyFrame fr, Terrain terrain)
        {
            const int N = 28;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Vector2 side = new Vector2(-fr.along.y, fr.along.x);
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2f / N, lat = Mathf.Sin(a) * SkyAcross;
                var p = fr.c + fr.along * (Mathf.Cos(a) * SkyAlong) + side * lat;
                float bottom = fr.floor + 0.95f * fr.tube.height * Mathf.Sqrt(Mathf.Max(0f, 1f - (lat / fr.tube.halfW) * (lat / fr.tube.halfW))) - 0.9f;   // (deep enough to meet the tube's bumpy roof)
                float top = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y + 0.3f;
                verts.Add(new Vector3(p.x, bottom, p.y)); verts.Add(new Vector3(p.x, top, p.y));
            }
            // a collar on the ground round the top, over the ragged edge of the terrain's hole
            int collar = verts.Count;
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2f / N;
                var p = fr.c + fr.along * (Mathf.Cos(a) * (SkyAlong + 1.1f)) + side * (Mathf.Sin(a) * (SkyAcross + 1.1f));
                verts.Add(new Vector3(p.x, terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y + 0.04f, p.y));
            }
            for (int i = 0; i < N; i++)
            {
                int a = i * 2 + 1, b = ((i + 1) % N) * 2 + 1, c = collar + i, d = collar + (i + 1) % N;
                if (Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]).y >= 0f) tris.AddRange(new[] { a, b, c, b, d, c });
                else tris.AddRange(new[] { a, c, b, b, c, d });
            }
            for (int i = 0; i < N; i++)
            {
                int a = i * 2, b = ((i + 1) % N) * 2;
                var n = Vector3.Cross(verts[b] - verts[a], verts[a + 1] - verts[a]);
                var inward = new Vector3(fr.c.x - verts[a].x, 0f, fr.c.y - verts[a].z);
                if (Vector3.Dot(n, inward) >= 0f) tris.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
                else tris.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
            }
            var mesh = new Mesh { name = "IT_SkyShaft" };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh DomeMesh(Dome d)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            const int Lon = 36, Lat = 9;
            Vector3 P(int i, int j)
            {
                float lon = Mathf.PI * 2f * i / Lon, lat = Mathf.PI * 0.5f * j / Lat;
                float n = j == Lat ? 1f : 1f + 0.14f * Fbm(Mathf.Cos(lon) * 1.5f + 9f, Mathf.Sin(lon) * 1.5f + lat * 2f, 3, 139);
                float rr = d.r * Mathf.Cos(lat) * n;
                return d.floor + new Vector3(Mathf.Cos(lon) * rr, j == 0 ? -0.4f : Mathf.Sin(lat) * d.height * n, Mathf.Sin(lon) * rr);
            }
            for (int j = 0; j <= Lat; j++) for (int i = 0; i < Lon; i++) verts.Add(P(i, j));
            for (int j = 0; j < Lat; j++)
                for (int i = 0; i < Lon; i++)
                {
                    int a = j * Lon + i, b = j * Lon + (i + 1) % Lon, c = a + Lon, e = b + Lon;
                    var mid = (verts[a] + verts[b] + verts[c] + verts[e]) * 0.25f;
                    if (InAnyTube(mid, null, 0.2f)) continue;                                 // the ways in
                    tris.AddRange(new[] { a, b, c, b, e, c });
                }
            // the floor: a little rough
            int centre = verts.Count;
            verts.Add(d.floor);
            for (int r = 1; r <= 3; r++)
                for (int i = 0; i < Lon; i++)
                {
                    float lon = Mathf.PI * 2f * i / Lon, rr = d.r * r / 3f * (r == 3 ? 1.25f : 1f);       // (the outer ring runs under the wall)
                    verts.Add(d.floor + new Vector3(Mathf.Cos(lon) * rr, r == 3 ? -0.05f : 0.12f * Fbm(lon * 2f, r, 2, 141), Mathf.Sin(lon) * rr));
                }
            for (int i = 0; i < Lon; i++) tris.AddRange(new[] { centre, centre + 1 + (i + 1) % Lon, centre + 1 + i });
            for (int r = 1; r < 3; r++)
                for (int i = 0; i < Lon; i++)
                {
                    int a = centre + 1 + (r - 1) * Lon + i, b = centre + 1 + (r - 1) * Lon + (i + 1) % Lon, c = a + Lon, e = b + Lon;
                    tris.AddRange(new[] { a, b, c, b, e, c });
                }
            var mesh = new Mesh { name = "IT_CaveDome", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Cuts the ways into the caves out of the falls' rock (its alcove's back wall).</summary>
        static void CutFallsRock(Transform map)
        {
            var go = GameObject.Find("滝つぼの岩壁");
            if (!go) return;
            var mf = go.GetComponent<MeshFilter>();
            var mesh = mf.sharedMesh;
            var v = mesh.vertices;
            var tri = mesh.triangles;
            var keep = new List<int>();
            for (int i = 0; i < tri.Length; i += 3)
            {
                var mid = (v[tri[i]] + v[tri[i + 1]] + v[tri[i + 2]]) / 3f;
                if (InAnyTube(mid, null, 0.25f)) continue;
                keep.Add(tri[i]); keep.Add(tri[i + 1]); keep.Add(tri[i + 2]);
            }
            mesh.SetTriangles(keep, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            var col = go.GetComponent<MeshCollider>();
            if (col) { col.sharedMesh = null; col.sharedMesh = mesh; }
        }

        /// <summary>The caves at the slug's eye, the shoulder road's bridge, the summit's salt, the platform under the cliffs.</summary>
        [MenuItem("Slug/IslandTest 800 m/Shoot Caves And Loops")]
        public static string ShootCaves()
        {
            var t = OpenTerrain();
            if (!t || _tubes.Count == 0) return "build the scene first";
            float G(Vector2 p) => t.SampleHeight(new Vector3(p.x, 0f, p.y)) + t.transform.position.y;
            var sb = new System.Text.StringBuilder();
            void Shot(string file, Vector3 eye, Vector3 look, float fov = 70f) => sb.AppendLine(ShotUtil.Capture($"{ShotDir}/{file}.png", eye, look, fov, 1200, 600, 3000f));
            Vector3 up = Vector3.up * 0.47f;
            if (_domes.Count > 0)
            {
                var d = _domes[0];
                var a = _tubes.Find(x => x.name.Contains("滝の裏"));
                Shot("50_salt_cave_inside", d.floor + _fallDir3() * (d.r - 1.5f) + up, d.floor - _fallDir3() * d.r + Vector3.up * 1.5f);
                if (a != null) Shot("51_salt_cave_from_ledge", a.pts[0] + up, a.pts[1] + Vector3.up * 1f);
                var b = _tubes.Find(x => x.name.Contains("東の斜面"));
                if (b != null)
                {
                    Vector3 end = b.pts[b.pts.Count - 1], dir = (end - b.pts[0]).normalized;
                    var eye = end + new Vector3(dir.x, 0f, dir.z) * 7f;
                    eye.y = G(new Vector2(eye.x, eye.z)) + 0.47f;
                    Shot("52_salt_cave_east_mouth", eye, b.pts[0] + Vector3.up * 1.2f);
                }
            }
            var lava = _tubes.Find(x => x.name == "溶岩洞");
            if (lava != null)
            {
                Vector3 v0 = lava.pts[0], v1 = lava.pts[Mathf.Min(6, lava.pts.Count - 1)];
                var back = v0 - (v1 - v0).normalized * 8f;
                back.y = G(new Vector2(back.x, back.z)) + 0.47f;
                Shot("53_lava_vent", back, v0 + Vector3.up * 1f);
                var sky = W(Skylights[4], Skylights[5]);   // the skylight with the pile under it
                NearTube(lava, sky.x, sky.y, 9f, out float f);
                int k = Mathf.Max(0, lava.pts.FindIndex(p => new Vector2(p.x - sky.x, p.z - sky.y).magnitude < 3f) - 6);
                Shot("54_lava_tube_skylight", lava.pts[k] + up, new Vector3(sky.x, f + 3f, sky.y));
            }
            ShootInlet((file, eye, look, fov) => Shot(file, eye, look, fov), t);
            var kip = _tubes.Find(x => x.name.Contains("キプカ"));
            if (kip != null)
            {
                Vector3 m = kip.pts[kip.pts.Count - 1], back = kip.pts[Mathf.Max(0, kip.pts.Count - 12)], dir = m - back;
                dir.y = 0f; dir.Normalize();
                var eye = m + dir * 6f;
                eye.y = G(new Vector2(eye.x, eye.z)) + 0.47f;
                Shot("60_kipuka_pit", eye, back + Vector3.up * 1f);
            }
            foreach (var (a, b) in _ringBridges)
            {
                var mid = (a + b) * 0.5f;
                var along = (b - a); along.y = 0f; along.Normalize();
                var eye = a - along * 10f; eye.y = G(new Vector2(eye.x, eye.z)) + 0.47f;
                Shot("56_shoulder_bridge", eye, b + Vector3.up * 1f);
                break;
            }
            var top = W(MountainSvg);
            Shot("57_summit_salt", new Vector3(top.x - 6f, G(top + new Vector2(-6f, 0f)) + 0.47f, top.y), new Vector3(top.x, G(top) + 0.5f, top.y));
            return sb.ToString();
        }

        static Vector3 _fallDir3() => new Vector3(_fallD.x, 0f, _fallD.y);

        static void BuildCaves(Transform map, Terrain terrain)
        {
            var root = new GameObject("洞窟").transform;
            root.SetParent(map, false);
            var saltMat = CaveMat("IT_SaltCave", new Color(0.8f, 0.76f, 0.76f));
            var lavaMat = CaveMat("IT_LavaTube", new Color(0.2f, 0.19f, 0.19f));
            EnsureRocks();
            var rnd = new System.Random(820);
            foreach (var t in _tubes)
            {
                var mesh = TubeMesh(t);
                mesh.name = "IT_Tube_" + _tubes.IndexOf(t);
                MeshObject(root, t.name, SaveMesh(mesh), t.lava ? lavaMat : saltMat, true);
            }
            for (int i = 0; i < _skyFrames.Count; i++)
            {
                var mesh = ShaftMesh(_skyFrames[i], terrain);
                mesh.name = "IT_SkyShaft_" + i;
                MeshObject(root, "天窓の縦穴", SaveMesh(mesh), lavaMat, true);
            }
            BuildInlet(root, terrain, rnd);
            foreach (var d in _domes)
            {
                MeshObject(root, d.name, SaveMesh(DomeMesh(d)), saltMat, true);
                // rock salt inside, and a dim light so the gray model can be seen
                for (int i = 0; i < 2; i++)
                {
                    float a = 1.2f + i * 2.3f;
                    ExtractTestBuilder.SaltRock(root, d.floor + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (d.r * 0.6f), 1.2f);
                }
                var lg = new GameObject("洞窟の明かり（仮）");
                lg.transform.SetParent(root, false);
                lg.transform.position = d.floor + Vector3.up * d.height * 0.6f;
                var light = lg.AddComponent<Light>();
                light.type = LightType.Point; light.range = d.r * 2.2f; light.intensity = 1.2f; light.color = new Color(1f, 0.85f, 0.85f);
            }
            // piles of fallen roof under the vent's collapse and the middle skylight: the ways up out of the tube there
            // (the other two skylights can only be dropped into)
            foreach (int s in new[] { 0, 2 })
            {
                var w = W(Skylights[s * 2], Skylights[s * 2 + 1]);
                var lava = _tubes.Find(x => x.name == "溶岩洞");
                if (lava != null && NearTube(lava, w.x, w.y, 5f, out float f))
                {
                    float top = terrain.SampleHeight(new Vector3(w.x, 0f, w.y)) + terrain.transform.position.y;
                    var pile = Rock(root, s == 0 ? "火口の切れ目の崩れた入口" : "天窓の下の崩れた岩", Mat("IT_LavaRock", new Color(0.2f, 0.2f, 0.21f)), new Vector3(w.x, f, w.y), new Vector3(7f, (top - f) * 1.7f, 6f), rnd, 0.3f);
                    pile.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    if (s == 0) _mouths.Add(new Vector3(w.x, top, w.y));
                }
            }
            // rocks round each mouth, to frame it
            foreach (var m in _mouths)
                for (int i = 0; i < 4; i++)
                {
                    var off = Random.insideUnitCircle * 4f;
                    var p = new Vector3(m.x + off.x, 0f, m.z + off.y);
                    p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
                    if (Mathf.Abs(p.y - m.y) > 6f) continue;
                    Rock(root, "口の岩", Mat("IT_Stone", new Color(0.62f, 0.5f, 0.4f)), p, new Vector3(1.6f, 1.2f, 1.5f) * (0.7f + (float)rnd.NextDouble() * 0.6f), rnd);
                }
            var kipTube = _tubes.Find(x => x.name.Contains("キプカ"));
            if (kipTube != null) { var e = kipTube.pts[kipTube.pts.Count - 1]; MoveTestBuilder.Label("キプカの落ち穴", e + Vector3.up * 4f, 2.4f); }
            if (_domes.Count > 0) MoveTestBuilder.Label("岩塩の洞窟", _domes[0].floor + Vector3.up * 4f, 2f);
        }
    }
}
