using SlugMove;
using System.Collections.Generic;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800, part 6: the rock-salt mountain shaped for the ways round the island (動線, Oct 10). Before, it was
    /// a cone you could crawl straight up from anywhere, so every way between two places ran over the top. Now:
    /// - a cliff band rings its upper part (62 m to 88 m, about 75°): the summit dome above can only be reached up
    ///   four ridges (north from the saddle and the cone, east from the forest, south from the pool, west from the
    ///   mesa), each a ramp of about 33° that breaks through the band. Four ways up, each a chokepoint
    /// - under the band a shoulder about 25 m wide runs round the mountain at 62 m: a way from one region to the
    ///   next without the summit (the road on it goes round each ridge's nose). It is broken in the south-west,
    ///   where the falls' gorge and the canyon's head cut into the mountain
    /// - below the shoulder the slopes are as before, with their benches
    /// </summary>
    public static partial class IslandTest800Builder
    {
        const float ShoulderH = 62f, CliffTopH = 88f, CliffW = 7f;     // 肩の高さ、崖の上の高さ、崖の幅（m）
        static readonly (string name, Vector2 to)[] Spurs =
        {
            ("北の尾根", new Vector2(359, 199)), ("東の尾根", new Vector2(462, 272)),
            ("南の尾根", new Vector2(368, 316)), ("西の尾根", new Vector2(336, 238)),
        };
        const float SpurWidthDeg = 10f;

        static float SpurAngle(int k) => Mathf.Atan2(Spurs[k].to.y - MountainSvg.y, Spurs[k].to.x - MountainSvg.x);

        static float MountainFootAt(float ang)
        {
            float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
            return MountainFoot * (1f + 0.18f * Mathf.Max(0f, ca) + 0.1f * Mathf.Max(0f, sa) - 0.12f * Mathf.Max(0f, -sa)) * 0.95f;
        }

        static float Wobble(float ang) => 1f + 0.25f * (Vn(Mathf.Cos(ang) * 1.8f + 21f, Mathf.Sin(ang) * 1.8f + 37f) - 0.5f) * Warp;

        static float SaltCone(float d, float foot) { float t = Mathf.Clamp01((d - 12f) / (foot - 12f)); return MountainTop * Mathf.Pow(1f - t, 1.25f); }
        static float ConeDist(float hgt, float foot) => 12f + (1f - Mathf.Pow(hgt / MountainTop, 0.8f)) * (foot - 12f);

        /// <summary>How much a direction from the summit is one of the ridges (0-1).</summary>
        static float SpurAt(float ang)
        {
            float s = 0f;
            for (int k = 0; k < Spurs.Length; k++)
            {
                float da = Mathf.DeltaAngle(ang * Mathf.Rad2Deg, SpurAngle(k) * Mathf.Rad2Deg) / SpurWidthDeg;
                s = Mathf.Max(s, Mathf.Exp(-da * da));
            }
            return s;
        }

        /// <summary>The mountain's height (m) at a point of the drawing, before it is merged with the ground round it.</summary>
        static float MountainHeight(float sx, float sy, float mx, float my, out float spur)
        {
            float dx = sx - MountainSvg.x, dy = sy - MountainSvg.y, ang = Mathf.Atan2(dy, dx), ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
            float d = Mathf.Sqrt(dx * dx + dy * dy) * K * Wobble(ang);
            float foot = MountainFootAt(ang);
            // the band is not a drum: its top wanders in and out and up and down, and the cliff is wider in places
            float topH = CliffTopH + 5f * Fbm(ca * 1.6f + 70f, sa * 1.6f + 70f, 2, 121);
            float dTop = ConeDist(topH, foot) + 5f * Fbm(ca * 2.4f + 80f, sa * 2.4f + 80f, 2, 123), dSh = ConeDist(ShoulderH, foot);
            float cliffW = CliffW * (0.7f + 0.8f * Vn(ca * 3f + 90f, sa * 3f + 90f));
            // the faces: the dome, the cliff band, the shoulder, the slopes below
            float face;
            if (d <= dTop) face = SaltCone(d, foot) + (topH - SaltCone(dTop, foot)) * Smooth(dTop - 12f, dTop, d);
            else if (d <= dTop + cliffW) face = Mathf.Lerp(topH, ShoulderH + 1.5f, Smooth(0f, 1f, (d - dTop) / cliffW));
            else if (d <= dSh) face = ShoulderH + 1.5f - 1.5f * (d - dTop - cliffW) / Mathf.Max(1f, dSh - dTop - cliffW);
            else face = SaltCone(d, foot);
            // the ridges: a straight ramp from below the shoulder up to the dome, through the band
            float r0 = dSh + 10f, r1 = dTop - 8f, ramp = face;
            if (d < r0 && d > r1) ramp = Mathf.Lerp(SaltCone(r0, foot), SaltCone(r1, foot), (r0 - d) / (r0 - r1));
            spur = SpurAt(ang) * Smooth(dTop - 14f, dTop - 4f, d) * (1f - Smooth(r0 - 2f, r0 + 6f, d));
            float hm = Mathf.Lerp(face, Mathf.Max(face, ramp), spur);
            float up = hm / MountainTop;
            hm += MountainTop * 0.05f * Fbm(ca * 2.2f + d * 0.005f + 40f, sa * 2.2f + d * 0.005f + 40f, 3, 63) * 4f * up * (1f - up) * Warp * (1f - Smooth(dTop - 2f, dSh, d) * (1f - Smooth(dSh, dSh + 8f, d)));
            hm += 3f * Fbm(mx * 0.03f, my * 0.03f, 3, 65) * up * Warp * (d > dTop ? 0.5f : 1f);
            // the benches, below the shoulder only
            if (hm > 2f && d > dSh)
            {
                float nn = Fbm(mx * 0.02f, my * 0.02f, 2, 61) * BenchStep * 0.8f;
                float k = (hm + nn) / BenchStep, f = k - Mathf.Floor(k), f2 = Mathf.Max(0f, (f - BenchFlat) / (1f - BenchFlat));
                float ter = BenchStep * (Mathf.Floor(k) + 0.35f * Smooth(0f, 1f, f2) + 0.65f * f2) - nn;
                hm = Lerp(hm, ter, Smooth(-0.05f, 0.12f, Vn(mx * 0.012f + 3f, my * 0.012f + 8f) - 0.42f) * Smooth(dSh, dSh + 10f, d) * (1f - spur));
            }
            return hm;
        }

        /// <summary>A point of the drawing on the shoulder, in a direction from the summit (radians), at an offset outward (m).</summary>
        static Vector2 ShoulderSvg(float ang, float outward)
        {
            float d = (ConeDist(ShoulderH, MountainFootAt(ang)) + outward) / Wobble(ang);
            return MountainSvg + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * d / K;
        }

        /// <summary>The ways the mountain adds: the ridge trails to the summit (the road round the shoulder is CarveRing).</summary>
        static List<Way> MountainWays()
        {
            var list = new List<Way>();
            for (int k = 0; k < Spurs.Length; k++)
            {
                var foot = ShoulderSvg(SpurAngle(k), 6f);
                list.Add(new Way { name = $"尾根道（{Spurs[k].name}）", road = false, svg = new[] { foot.x, foot.y, MountainSvg.x, MountainSvg.y } });
            }
            return list;
        }

        static readonly List<(Vector3 a, Vector3 b)> _ringBridges = new List<(Vector3, Vector3)>();

        /// <summary>The road right round the shoulder: over the ridges (at most about 22°) and across the canyon's head
        /// on a natural stone bridge (where the ground drops more than 4 m under it).</summary>
        static void CarveRing(float[] hm, float[] mask)
        {
            _ringBridges.Clear();
            var pts = new List<Vector2>();
            for (int i = 0; i <= 360; i++)
            {
                float ang = i * Mathf.Deg2Rad;
                var s = ShoulderSvg(ang, -9f);
                pts.Add(W(s));
            }
            pts = Resample(pts, 2f);
            int n = pts.Count;
            var g = new float[n];
            var gap = new bool[n];
            for (int i = 0; i < n; i++) { g[i] = HmAt(hm, pts[i].x, pts[i].y); gap[i] = g[i] < ShoulderH - 14f; }        // (a dip like the pool's bench the road just goes down into)
            // across a gap the road keeps the height of its two ends
            for (int i = 0; i < n; i++)
            {
                if (!gap[i]) continue;
                int a = i; while (a > 0 && gap[a]) a--;
                int b = i; while (b < n - 1 && gap[b]) b++;
                g[i] = Mathf.Lerp(g[a], g[b], (i - a) / (float)Mathf.Max(1, b - a));
            }
            // evened out, then never steeper than about 22°
            var y = new float[n];
            for (int i = 0; i < n; i++) { float sum = 0f; int c = 0; for (int k = -3; k <= 3; k++) { sum += g[(i + k + n) % n]; c++; } y[i] = sum / c; }
            float grade = Mathf.Tan(22f * Mathf.Deg2Rad) * 0.92f;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 1; i < n; i++) { float d = (pts[i] - pts[i - 1]).magnitude * grade; y[i] = Mathf.Clamp(y[i], y[i - 1] - d, y[i - 1] + d); }
                for (int i = n - 2; i >= 0; i--) { float d = (pts[i] - pts[i + 1]).magnitude * grade; y[i] = Mathf.Clamp(y[i], y[i + 1] - d, y[i + 1] + d); }
            }
            var all = new List<Vector3>();
            var piece = new List<Vector3>();
            int start = -1;
            for (int i = 0; i < n; i++)
            {
                var p = new Vector3(pts[i].x, y[i], pts[i].y);
                all.Add(p);
                if (gap[i])
                {
                    if (start < 0) start = Mathf.Max(0, i - 1);
                    if (piece.Count >= 2) Carve(hm, mask, piece, RoadHalfWidth, 5.5f, 0f);
                    piece.Clear();
                    continue;
                }
                if (start >= 0) { _ringBridges.Add((all[start], p)); start = -1; }
                piece.Add(p);
            }
            if (piece.Count >= 2) Carve(hm, mask, piece, RoadHalfWidth, 5.5f, 0f);
            _ways.Add(("肩の道（山をひと回り）", true, all));
        }

        /// <summary>The natural stone bridges the shoulder road crosses gaps on.</summary>
        static void BuildRingBridges(Transform parent)
        {
            var stone = Mat("IT_SaltRock", new Color(0.84f, 0.83f, 0.79f));
            foreach (var (a, b) in _ringBridges)
            {
                Vector3 mid = (a + b) * 0.5f, along = b - a;
                float len = new Vector2(along.x, along.z).magnitude + 6f;
                var deck = WeatherSceneSetup.Prim(parent, "肩の石橋（仮）", PrimitiveType.Cube, stone, mid - Vector3.up * 1.4f, new Vector3(4.5f, 2.8f, len), 0f);
                deck.transform.rotation = Quaternion.LookRotation(along.normalized);
                MoveTestBuilder.Label("肩の石橋", mid + Vector3.up * 4f, 3f);
            }
        }

        /// <summary>The end of a road on the shoulder at the foot of a ridge (svg).</summary>
        static Vector2 RidgeFoot(int k) => ShoulderSvg(SpurAngle(k), -9f);
    }
}
