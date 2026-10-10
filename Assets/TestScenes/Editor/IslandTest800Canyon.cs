using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800, part 3: the dry canyon, shaped like a real one cut in layered sandstone (Oct 10). Its walls are
    /// the same layers on both sides, from the floor up: a talus apron of fallen rock, a lower cliff, a ledge, a slope
    /// of soft rock, a narrow ledge and the hard caprock cliff at the rim. The floor is sand with a dry wash wandering
    /// down it. The canyon meanders, swells into alcoves and narrows toward its dead end (a pour-off with a plunge pool
    /// under it); three side canyons branch into the mesa and end high in the walls; an alluvial fan spreads from the
    /// mouth onto the desert. The walls are painted in red and pale bands (the strata).
    /// </summary>
    public static partial class IslandTest800Builder
    {
        // the wall's layers from the floor up (m, for a wall 45.5 m high; a lower wall is the same shape, smaller)
        static readonly float[] WallRun = { 7f, 4f, 3f, 12f, 2f, 5f };          // talus, lower cliff, ledge, soft slope, ledge, caprock (cliffs about 72°: steeper shows as a comb on the 0.94 m ground)
        static readonly float[] WallRise = { 4f, 12f, 0.5f, 10f, 0.3f, 18.7f };
        const float WallHeight = 45.5f;

        static readonly float[][] SideCanyonSvg =
        {
            new float[] { 300,253, 302,243, 305,232 },    // north, into the north half of the mesa
            new float[] { 266,244, 264,256, 261,268 },    // south
            new float[] { 318,256, 322,246, 327,238 },    // north-east, by the dead end
        };
        static Vector2[][] _sides;
        static float[][] _sideAt;
        static float[] _sideFloor;     // the main canyon's floor where each side canyon leaves it

        static void PrepareCanyon()
        {
            _sides = new Vector2[SideCanyonSvg.Length][];
            _sideAt = new float[SideCanyonSvg.Length][];
            _sideFloor = new float[SideCanyonSvg.Length];
            for (int i = 0; i < _sides.Length; i++)
            {
                _sides[i] = Pts(SideCanyonSvg[i]);
                _sideAt[i] = Along(_sides[i]);
                Line(_sides[i][0].x, _sides[i][0].y, _canyon, _canyonAt, out float fr);
                _sideFloor[i] = Low + (_canyonFloorEnd - Low) * (1f - fr);
            }
        }

        /// <summary>How high the wall stands at a distance from the floor's edge (m), for a wall of the given size.</summary>
        static float WallAt(float x, float scale)
        {
            float rise = 0f;
            for (int i = 0; i < WallRun.Length; i++)
            {
                float run = WallRun[i] * scale, r = WallRise[i] * scale;
                if (x < run) { float t = x / run; return rise + r * (i == 0 ? Mathf.Pow(t, 1.5f) : t); }   // the talus is concave
                x -= run; rise += r;
            }
            return 10000f;    // past the rim: the canyon takes nothing
        }

        const float WallDepth = 33f;    // how far the whole wall reaches across (the sum of WallRun)
        /// <summary>How far out from the rim a wall of this height reaches (m).</summary>
        static float WallFoot(float height) => WallDepth * height / WallHeight;
        /// <summary>A wall of the given height (m), at a distance from its foot (m).</summary>
        static float WallFromFoot(float x, float height) => x <= 0f ? 0f : Mathf.Min(WallAt(x, Mathf.Max(0.05f, height / WallHeight)), height);

        /// <summary>How much the mesa's cliffs have broken down into a slope here (0 to 1): a few sure places and some by chance.</summary>
        static float MesaBreak(float sx, float sy, float mx, float my)
        {
            float b = Smooth(0.66f, 0.76f, Vn(mx * 0.012f + 5f, my * 0.012f + 9f));
            for (int i = 0; i < MesaBreaks.Length; i += 2)
            {
                float d = Mathf.Sqrt((sx - MesaBreaks[i]) * (sx - MesaBreaks[i]) + (sy - MesaBreaks[i + 1]) * (sy - MesaBreaks[i + 1])) * K;
                b = Mathf.Max(b, 1f - Smooth(14f, 26f, d));
            }
            return b;
        }

        /// <summary>The ground with the canyon cut in (m); floor tells how much of the point is the canyon's sandy floor.</summary>
        static float Canyon(float ux, float uy, float mx, float my, float h, float mesa01, out float floor01)
        {
            floor01 = 0f;
            // the canyon's own meander, on top of the island's wobble
            float cx = ux + Fbm(mx * 0.025f, my * 0.025f, 2, 91) * 7f / K, cy = uy + Fbm(mx * 0.025f, my * 0.025f, 2, 92) * 7f / K;
            float dc = Line(cx, cy, _canyon, _canyonAt, out float fr);
            if (dc > 60f && fr > 0.02f && fr < 0.98f) return FanAt(ux, uy, h);
            float floor = Low + (_canyonFloorEnd - Low) * (1f - fr);
            float layers = 0.85f + 0.3f * Vn(mx * 0.02f + 11f, my * 0.02f + 3f);              // the layers thicken and thin
            float half = CanyonFloorWidth * 0.5f * (0.35f + 0.5f * fr) + 3.5f * Fbm(mx * 0.035f, my * 0.035f, 2, 71);          // narrow at the dead end, alcoves
            float x = dc - half + 2.5f * Fbm(mx * 0.06f, my * 0.06f, 2, 93);                   // the rim and the ledges wander
            float hc;
            if (x <= 0f)
            {
                float wash = 1f - Smooth(0.6f, 2.2f, Mathf.Abs(dc - half * 0.4f * Fbm(mx * 0.02f, my * 0.02f, 2, 95)));
                hc = floor + 0.35f * Fbm(mx * 0.08f, my * 0.08f, 2, 94) - 0.6f * wash;
                floor01 = 1f;
            }
            else
            {
                float wall = Mesa - floor, brk = Smooth(0.72f, 0.8f, Vn(mx * 0.02f + 21f, my * 0.02f + 4f));   // now and then a wall has fallen into a slope
                float cliff = WallAt(x, Mathf.Max(0.2f, wall / WallHeight) * layers), slope = x * 0.65f;
                hc = cliff > wall && slope > wall ? 10000f : floor + Lerp(Mathf.Min(cliff, wall), Mathf.Min(slope, wall), brk);
            }

            // the side canyons: narrow, their floors climbing to end high in the walls
            for (int i = 0; i < _sides.Length; i++)
            {
                float d = Line(cx, cy, _sides[i], _sideAt[i], out float sf);
                if (d > 40f) continue;
                float sfloor = Mathf.Lerp(_sideFloor[i], Mesa - 7f, Mathf.Pow(sf, 1.3f));
                float sx = d - (2f + 1.5f * (1f - sf)) + 1.5f * Fbm(mx * 0.07f, my * 0.07f, 2, 96 + i);
                float hs = sx <= 0f ? sfloor : sfloor + WallAt(sx, Mathf.Max(0.15f, (Mesa - sfloor) / WallHeight) * layers);
                if (hs < hc) { hc = hs; floor01 = sx <= 0f ? 1f : 0f; }
            }

            // the plunge pool under the pour-off at the dead end
            float dp = Mathf.Sqrt((cx - _canyon[0].x) * (cx - _canyon[0].x) + (cy - _canyon[0].y) * (cy - _canyon[0].y)) * K;
            hc -= 1.2f * (1f - Smooth(2f, 5f, dp));

            if (hc < h)
            {
                float inMesa = Smooth(0.05f, 0.35f, mesa01);      // the canyon is cut in the mesa only, never in the mountain beside it
                floor01 *= inMesa;
                return Lerp(h, hc, inMesa);
            }
            floor01 = 0f;
            return FanAt(ux, uy, h);
        }

        /// <summary>The alluvial fan the old river spread out of the canyon's mouth onto the desert.</summary>
        static float FanAt(float ux, float uy, float h)
        {
            Vector2 mouth = _canyon[_canyon.Length - 1];
            float dx = (ux - mouth.x) * K, dy = (uy - mouth.y) * K, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 50f || h > Low + 4f) return h;
            float west = d < 1f ? 1f : Smooth(-0.3f, 0.5f, -dx / d);
            return h + 2f * (1f - Smooth(4f, 50f, d)) * west;
        }
    }
}
