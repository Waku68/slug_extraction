using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// The canvas backpack (the slugs' bag), sewn round one sack that swells with what is in it:
    ///  - empty, the cloth slumps: thin, pooled at the bottom, a little shorter, in folds across it;
    ///  - what goes in settles to the bottom: the lower part fills out round and taut while the cloth above still hangs in
    ///    folds; crammed, the whole sack is round and pulls in at its two side seams;
    ///  - a lid over the top and down the front, hinged across the front where it ends (it falls open outward, away from
    ///    the back and from the eyes looking in), held by two straps with buckles just below it. The straps run on down
    ///    the front to the bottom and stretch over the bulge; a pocket with its own flap sits between them; dark piping
    ///    runs along the side seams. The fuller, the higher the lid rides.
    /// Lid, pocket and straps grow less than the sack (as the square root), so a crammed bag bulges round them. No texture:
    /// the parts are told apart by their shape and shade.
    /// Bag space: x across, y up the bag (to its mouth), z out from the back; the back panel lies at z = -half.z, the
    /// bottom at y = -half.y. The sack is parametrised like a globe lying on its side: lat runs across (the poles are the
    /// sides), lon round the section (0 the top, 90° the front, 180° the bottom, 270° the back).
    /// </summary>
    internal static class CanvasBag
    {
        const float StrapHalf = 0.011f, StrapThick = 0.003f, LidThick = 0.007f, PipeRadius = 0.0035f;

        /// <summary>Where bag space sits: its middle and its axes.</summary>
        public struct Frame
        {
            public Vector3 c, x, y, z;
            public Frame(Vector3 c, Vector3 x, Vector3 y, Vector3 z) { this.c = c; this.x = x; this.y = y; this.z = z; }
            public Vector3 P(Vector3 l) => c + x * l.x + y * l.y + z * l.z;
            public Vector3 D(Vector3 l) => x * l.x + y * l.y + z * l.z;
        }

        /// <summary>The bag's meshes, and where its lid hinges and its mouth is (in the frame's space).</summary>
        public class Parts
        {
            public readonly SlugEquipment.GearMesh body = new SlugEquipment.GearMesh(), trim = new SlugEquipment.GearMesh(),
                pocket = new SlugEquipment.GearMesh(), straps = new SlugEquipment.GearMesh(), buckles = new SlugEquipment.GearMesh(),
                lid = new SlugEquipment.GearMesh(), lidStraps = new SlugEquipment.GearMesh(), mouth = new SlugEquipment.GearMesh();
            public Vector3 hinge, mouthCenter;
        }

        /// <summary>
        /// The bag with its envelope's half size <paramref name="half"/>, sewn from cloth panels the size of the empty bag
        /// (<paramref name="cloth"/>, half sizes), filled by <paramref name="fill"/> (0 = empty, 1 = crammed).
        /// </summary>
        public static Parts Make(Frame f, Vector3 cloth, Vector3 half, float fill)
        {
            var s = new Sack(half, cloth, fill);
            var o = new Parts();
            Body(o.body, s, f);

            float gx = Mathf.Sqrt(half.x / cloth.x), gy = Mathf.Sqrt(half.y / cloth.y);
            float top = s.At(0f, 0f).y, bottom = s.At(0f, Mathf.PI).y;

            // the lid: from the back of the top, over it and down the front, lifted the fuller the bag
            float lidX = cloth.x * 0.98f * gx, drape = cloth.y * 0.62f * gy;
            float lonBack = -0.32f * Mathf.PI, lonEnd = s.LonAtY(top - drape, 0f, 0.8f * Mathf.PI);
            float lidLat = s.LatAtX(lidX, 0f), lidGap = 0.004f + 0.01f * fill;
            Patch(o.lid, s, f, -lidLat, lidLat, lonBack, lonEnd, 16, 22, lidGap, LidThick, 0.005f);
            o.hinge = f.P(s.At(0f, lonEnd) + s.Normal(0f, lonEnd) * lidGap);
            o.mouthCenter = f.P(s.At(0f, 0f));

            // two straps: over the lid to its edge (they go with it), buckled just below, then on down the front to the bottom
            float strapX = 0.66f * lidX, yEnd = s.At(0f, lonEnd).y;
            float lonBuckle = s.LonAtY(yEnd - 0.017f, lonEnd, Mathf.PI);
            float lonStrapEnd = s.LonAtY(bottom + 0.02f, lonEnd, 0.98f * Mathf.PI);
            foreach (float side in new[] { -1f, 1f })
            {
                float a = side * s.LatAtX(strapX - StrapHalf, lonEnd), b = side * s.LatAtX(strapX + StrapHalf, lonEnd);
                Patch(o.lidStraps, s, f, Mathf.Min(a, b), Mathf.Max(a, b), 0.08f * Mathf.PI, lonEnd, 1, 14, lidGap + LidThick, StrapThick);
                Patch(o.straps, s, f, Mathf.Min(a, b), Mathf.Max(a, b), lonEnd, lonStrapEnd, 1, 24, 0.002f, StrapThick);
                Buckle(o.buckles, s, f, (a + b) * 0.5f, lonBuckle);
            }

            // the pocket between the straps, below the buckles, with a flap over its top
            float pocketX = 0.44f * lidX, yTop = s.At(0f, lonBuckle).y - 0.03f, yBot = bottom + 0.2f * (top - bottom);
            if (yTop - yBot > 0.03f)
            {
                float lon0 = s.LonAtY(yTop, lonBuckle, Mathf.PI), lon1 = s.LonAtY(yBot, lon0, Mathf.PI);
                float pLat = s.LatAtX(pocketX, (lon0 + lon1) * 0.5f), fLat = s.LatAtX(pocketX + 0.006f, lon0);
                float bulge = 0.008f + 0.008f * fill;
                Patch(o.pocket, s, f, -pLat, pLat, lon0, lon1, 8, 10, 0.002f, 0.005f, 0f, bulge);
                Patch(o.pocket, s, f, -fLat, fLat, lon0 - 0.02f, Mathf.Lerp(lon0, lon1, 0.36f), 8, 5, 0.008f + bulge * 0.6f, 0.004f, 0.003f);
            }

            // piping round the front panel: down the side seams, across under the lid and round the bottom edge
            {
                var pts = new List<Vector3>();
                const int n = 96;
                for (int j = 0; j < n; j++)
                {
                    float a = j / (float)n * Mathf.PI * 2f;
                    float lat = s.SeamLat * Rounded(Mathf.Cos(a)), lon = Mathf.PI * (0.5f + 0.27f * Rounded(Mathf.Sin(a)));
                    pts.Add(s.At(lat, lon) + s.Normal(lat, lon) * (PipeRadius * 0.4f));
                }
                Tube(o.trim, f, pts, PipeRadius, 6);
            }

            // the dark mouth under the lid (shown while it is open)
            float mLat = s.LatAtX(0.72f * lidX, 0f);
            Patch(o.mouth, s, f, -mLat, mLat, -0.2f * Mathf.PI, 0.2f * Mathf.PI, 8, 8, 0.0015f, 0.001f);
            return o;
        }

        // ------------------------------------------------------------------ the sack
        /// <summary>The sack's surface: a soft box rounded by how full it is, filled from the bottom, folded where slack.</summary>
        sealed class Sack
        {
            readonly Vector3 _half, _cloth;
            readonly float _fill, _r, _rBack, _level, _slackDepth;
            public readonly float SeamLat;

            public Sack(Vector3 half, Vector3 cloth, float fill)
            {
                _half = half; _cloth = cloth; _fill = fill;
                _r = Mathf.Lerp(0.42f, 1f, fill);                       // boxy when slack, round when crammed
                _rBack = _r * 0.55f;                                     // the back panel stays flatter against the back
                _level = Mathf.Lerp(-1.6f, 1.5f, fill);                  // how high what is in it comes (-1 bottom .. 1 top)
                _slackDepth = Mathf.Min(1f, cloth.z * 0.6f / Mathf.Max(half.z, 1e-4f));   // slack cloth lies thin
                SeamLat = Mathf.Asin(Mathf.Pow(0.8f, 1f / _r));          // where the sides are 80% of the way out
            }

            static float P(float v, float e) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), e);
            static float S(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

            public Vector3 At(float lat, float lon)
            {
                float ring = Mathf.Cos(lat), zr = ring * Mathf.Sin(lon);
                float sx = P(Mathf.Sin(lat), _r), sy = P(ring * Mathf.Cos(lon), _r), sz = P(zr, zr < 0f ? _rBack : _r);
                // the side seams pull in when it is crammed, so the panels between them bulge
                float d = (Mathf.Abs(lat) - SeamLat) / 0.07f;
                float seam = 1f - 0.05f * _fill * Mathf.Exp(-d * d);
                sy *= seam;
                sz = (sz + 1f) * seam - 1f;
                // full (round, as deep as the envelope) below the level of what is in it, slack (thin, pinched to the mouth) above;
                // an empty bag's cloth pools at the bottom and the top slumps down
                float full = S((_level - sy) / 1.3f + 0.5f), slack = 1f - full;
                float depth = Mathf.Min(1f, Mathf.Lerp(_slackDepth, 1f, full) + (1f - _fill) * 0.45f * S((0.1f - sy) / 1.1f));
                float width = Mathf.Lerp(0.92f, 1f, full) * (1f - 0.12f * slack * S((sy - 0.45f) / 0.55f));
                float y = sy - (1f - _fill) * 0.13f * (sy + 1f);
                // folds across the slack cloth, sagging between the side seams (not on the back panel)
                float fold = Mathf.Sin(sy * Mathf.PI * 3f + 2.4f * sx * sx + 0.8f * Mathf.Sin(3.1f * sx + 1.3f));
                float amp = _cloth.y * 0.08f * slack * (1f - 0.5f * _fill) * S((sz + 0.6f) / 0.8f);
                // (the folds ripple the sides' outline too)
                float side = amp * fold * 0.7f * Mathf.Sign(sx) * Mathf.Pow(Mathf.Abs(sx), 4f);
                return new Vector3(sx * _half.x * width + side, y * _half.y, -_half.z + (sz + 1f) * _half.z * depth + amp * fold);
            }

            public Vector3 Normal(float lat, float lon)
            {
                const float e = 0.004f;
                // (as slopes, not small steps: Vector3.normalized gives zero for anything shorter than 1e-5)
                Vector3 n = Vector3.Cross((At(lat, lon + e) - At(lat, lon - e)) / e, (At(lat + e, lon) - At(lat - e, lon)) / e);
                return n.sqrMagnitude > 1e-8f ? n.normalized : new Vector3(Mathf.Sign(lat), 0f, 0f);
            }

            /// <summary>The lat where the surface is <paramref name="x"/> out from the middle (along this lon).</summary>
            public float LatAtX(float x, float lon)
            {
                float lo = 0f, hi = 0.42f * Mathf.PI;
                if (At(hi, lon).x <= x) return hi;
                for (int i = 0; i < 24; i++)
                {
                    float m = (lo + hi) * 0.5f;
                    if (At(m, lon).x < x) lo = m; else hi = m;
                }
                return (lo + hi) * 0.5f;
            }

            /// <summary>The lon between <paramref name="from"/> and <paramref name="to"/> where the middle of the front comes down to <paramref name="y"/>.</summary>
            public float LonAtY(float y, float from, float to)
            {
                const int n = 200;
                float prevLon = from, prev = At(0f, from).y;
                for (int i = 1; i <= n; i++)
                {
                    float lon = Mathf.Lerp(from, to, i / (float)n), cur = At(0f, lon).y;
                    if (cur <= y) return Mathf.Lerp(prevLon, lon, (prev - y) / Mathf.Max(prev - cur, 1e-6f));
                    prevLon = lon; prev = cur;
                }
                return to;
            }
        }

        // ------------------------------------------------------------------ pieces
        static void Body(SlugEquipment.GearMesh mb, Sack s, Frame f, int nLat = 28, int nLon = 64)
        {
            var idx = new int[nLat + 1, nLon];
            for (int i = 0; i <= nLat; i++)
            for (int j = 0; j < nLon; j++)
                idx[i, j] = mb.Vert(f.P(s.At(Lat(i, nLat), j / (float)nLon * Mathf.PI * 2f)));
            for (int i = 0; i < nLat; i++)
            for (int j = 0; j < nLon; j++)
            {
                float lat = Lat(i + 0.5f, nLat), lon = (j + 0.5f) / nLon * Mathf.PI * 2f;
                Vector3 under = f.P(s.At(lat, lon) - s.Normal(lat, lon) * 0.05f);
                mb.Quad(idx[i, j], idx[i + 1, j], idx[i + 1, (j + 1) % nLon], idx[i, (j + 1) % nLon], under, true);
            }
        }

        static float Lat(float i, int n) => Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, i / n);

        /// <summary>A circle's cos or sin pushed out toward a square with round corners.</summary>
        static float Rounded(float v) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), 0.3f);

        /// <summary>
        /// A piece of cloth sewn on the sack over a lat × lon patch: <paramref name="gap"/> off it, <paramref name="thick"/>
        /// thick (thinner at its rim, which lifts by <paramref name="flare"/>), bulging by <paramref name="dome"/> in the middle.
        /// </summary>
        static void Patch(SlugEquipment.GearMesh mb, Sack s, Frame f, float lat0, float lat1, float lon0, float lon1, int nA, int nB,
                          float gap, float thick, float flare = 0f, float dome = 0f)
        {
            var outer = new int[nA + 1, nB + 1];
            var inner = new int[nA + 1, nB + 1];
            var under = new Vector3[nA + 1, nB + 1];
            for (int i = 0; i <= nA; i++)
            for (int j = 0; j <= nB; j++)
            {
                float u = i / (float)nA, v = j / (float)nB;
                float lat = Mathf.Lerp(lat0, lat1, u), lon = Mathf.Lerp(lon0, lon1, v);
                Vector3 p = s.At(lat, lon), n = s.Normal(lat, lon);
                float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1f - u) * nA, Mathf.Min(v, 1f - v) * nB));
                float du = u * 2f - 1f, dv = v * 2f - 1f;
                float tk = thick * Mathf.Lerp(0.6f, 1f, edge) + dome * Mathf.Clamp01(1f - du * du - dv * dv);
                float g = gap + flare * (1f - edge);
                outer[i, j] = mb.Vert(f.P(p + n * (g + tk)));
                inner[i, j] = mb.Vert(f.P(p + n * g));
                under[i, j] = f.P(p - n * 0.05f);
            }
            for (int i = 0; i < nA; i++)
            for (int j = 0; j < nB; j++)
            {
                Vector3 from = (under[i, j] + under[i + 1, j + 1]) * 0.5f;
                mb.Quad(outer[i, j], outer[i + 1, j], outer[i + 1, j + 1], outer[i, j + 1], from, true);
                mb.Quad(inner[i, j], inner[i + 1, j], inner[i + 1, j + 1], inner[i, j + 1], from, false);
            }
            // the rim all round, crisp; it faces away from the middle of the piece
            Vector3 mid = Vector3.zero;
            foreach (int v in outer) mid += mb.Pos(v);
            mid /= outer.Length;
            void Edge(int a0, int a1, int b0, int b1)
                => mb.Quad(mb.Copy(outer[a0, a1]), mb.Copy(outer[b0, b1]), mb.Copy(inner[b0, b1]), mb.Copy(inner[a0, a1]), mid, true);
            for (int i = 0; i < nA; i++) { Edge(i, 0, i + 1, 0); Edge(i, nB, i + 1, nB); }
            for (int j = 0; j < nB; j++) { Edge(0, j, 0, j + 1); Edge(nA, j, nA, j + 1); }
        }

        /// <summary>A flat metal buckle on the strap at (lat, lon), with its bar across.</summary>
        static void Buckle(SlugEquipment.GearMesh mb, Sack s, Frame f, float lat, float lon)
        {
            Vector3 n = s.Normal(lat, lon);
            Vector3 down = (s.At(lat, lon + 0.01f) - s.At(lat, lon - 0.01f)).normalized;
            Vector3 across = Vector3.Cross(down, n).normalized;
            Vector3 p = s.At(lat, lon) + n * (0.002f + StrapThick + 0.003f);
            mb.RoundedBox(f.P(p), f.D(across), f.D(down), f.D(n), new Vector3(StrapHalf + 0.004f, 0.012f, 0.0028f), 0.3f, 6, 12);
            mb.RoundedBox(f.P(p + n * 0.0025f), f.D(across), f.D(down), f.D(n), new Vector3(StrapHalf + 0.006f, 0.0025f, 0.0022f), 0.4f, 4, 10);
        }

        /// <summary>A round tube through closed loop of points (bag space), for the piping on the seams.</summary>
        static void Tube(SlugEquipment.GearMesh mb, Frame f, List<Vector3> pts, float r, int sides)
        {
            int n = pts.Count;
            var idx = new int[n, sides];
            for (int i = 0; i < n; i++)
            {
                Vector3 t = (pts[(i + 1) % n] - pts[(i - 1 + n) % n]).normalized;
                Vector3 u = Vector3.ProjectOnPlane(Vector3.right, t).normalized, w = Vector3.Cross(t, u);
                for (int j = 0; j < sides; j++)
                {
                    float a = j / (float)sides * Mathf.PI * 2f;
                    idx[i, j] = mb.Vert(f.P(pts[i] + (u * Mathf.Cos(a) + w * Mathf.Sin(a)) * r));
                }
            }
            for (int i = 0; i < n; i++)
            for (int j = 0; j < sides; j++)
            {
                int i2 = (i + 1) % n, j2 = (j + 1) % sides;
                mb.Quad(idx[i, j], idx[i2, j], idx[i2, j2], idx[i, j2], f.P((pts[i] + pts[i2]) * 0.5f), true);
            }
        }
    }
}
