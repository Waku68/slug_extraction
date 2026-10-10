using System.Collections.Generic;
using SlugMove;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SlugExtract
{
    /// <summary>
    /// IslandTest_800, part 10: the inlet where the lava tube meets the sea (Oct 10, わく's drawing; how-to in
    /// .claude/skills/modeling/shapes/coast.md). The pahoehoe flow ends in the east sea cliff; the sea has got into the end
    /// of the lava tube and opened it into a narrow inlet without a roof:
    /// - water 18 m wide and 22 m deep from the cliff's foot, 2.5 m deep. Round it, a horseshoe ledge 4.5 m wide at the
    ///   wave-cut platform's height (1.2 m), joining the platform outside to the north and the south (the way round the
    ///   coast has to go round it). Walls of about 72° up to the lava (all terrain, cut after the erosion)
    /// - at the head, the last few metres keep their roof (the lava tube's end: a model), with a skylight right over the
    ///   water: from above, a drop into salt water (a short cut that hurts)
    /// - two tunnels up from the head: the lava tube to the cone's breach, and a branch south to a pit at the kipuka's edge
    /// - black salt grows on the ledge's walls where the spray dries
    /// </summary>
    public static partial class IslandTest800Builder
    {
        const float InletDeep = 22f;            // from the cliff's foot (the platform's inner edge) to the head of the water
        const float InletWaterR = 9f;           // half the water's width
        const float InletLedgeW = 4.5f;         // the ledge round it
        const float InletWallW = 3.5f;          // the wall up to the lava (about 72° for 11 m)
        const float InletWaterDepth = -2.5f;
        const float InletRoofFront = -12f;      // the roof covers the head up to here (a, m from the cliff's foot; the head is at -22)
        const float InletSkyA = -16.5f, InletSkyR = 3.4f;

        static bool _inletOK;
        static Vector2 _inletC, _inletU, _inletV;      // the cliff's foot on the inlet's axis; seaward; to its left (north)

        static float InletCenterA => -InletDeep + InletWaterR;      // the centre of the head's half circle
        static float InletOuterR => InletWaterR + InletLedgeW + InletWallW;
        static Vector2 InletPoint(float a, float b) => _inletC + _inletU * a + _inletV * b;
        static Vector2 InletSky => InletPoint(InletSkyA, 0f);

        /// <summary>Finds the cliff's foot where the pahoehoe flow meets the sea (needs Prepare's island outline).</summary>
        static void PrepareInlet()
        {
            _inletOK = false;
            Vector2 from = W(FlowSvg[1][6], FlowSvg[1][7]), to = W(FlowSvg[1][8], FlowSvg[1][9]);
            _inletU = (to - from).normalized;
            _inletV = new Vector2(-_inletU.y, _inletU.x);
            for (float l = 0f; l < 120f; l += 0.5f)
            {
                var p = from + _inletU * l;
                var s = Svg(p.x, p.y);
                float mx = s.x * K, my = s.y * K;
                float inM = Sd(s.x, s.y, _island, _islandBox) + Fbm(mx * 0.025f, my * 0.025f, 3, 7) * 14f * Warp;
                if (inM < 6f) { _inletC = p; _inletOK = true; return; }      // (the platform is the last 6 m)
            }
            Debug.LogWarning("[IslandTest800] the inlet found no coast");
        }

        /// <summary>The inlet's local coordinates: a along the axis (0 at the cliff's foot, + to the sea), r from the water's middle line (with a ragged edge).</summary>
        static bool InletLocal(float x, float z, out float a, out float r)
        {
            a = r = 0f;
            if (!_inletOK) return false;
            Vector2 p = new Vector2(x, z) - _inletC;
            a = Vector2.Dot(p, _inletU);
            float b = Vector2.Dot(p, _inletV);
            if (a < InletCenterA - InletOuterR - 3f || a > 40f || Mathf.Abs(b) > InletOuterR + 3f) return false;
            r = new Vector2(a - Mathf.Clamp(a, InletCenterA, 40f), b).magnitude + 0.9f * Fbm(x * 0.07f, z * 0.07f, 2, 171);
            return true;
        }

        /// <summary>Cuts the inlet into the finished ground (after the erosion).</summary>
        static float InletCarve(float x, float z, float h)
        {
            if (!InletLocal(x, z, out float a, out float r) || r >= InletOuterR) return h;
            float ledge = PlatformH + 0.2f * Fbm(x * 0.1f, z * 0.1f, 2, 151);
            float carved;
            if (r < InletWaterR)
            {
                // the ledge's edge drops into the water; under the skylight, at the head, it shelves gently so a slug can get out
                float headness = a < InletCenterA ? Smooth(0.7f, 0.97f, (InletCenterA - a) / Mathf.Max(0.01f, r)) : 0f;
                float edge = Mathf.Lerp(1.4f, 5.5f, headness);
                carved = Mathf.Lerp(InletWaterDepth, ledge, Smooth(InletWaterR - edge, InletWaterR, r));
            }
            else if (r < InletWaterR + InletLedgeW) carved = ledge;
            else carved = Mathf.Lerp(ledge, h, (r - InletWaterR - InletLedgeW) / InletWallW);
            return Mathf.Min(h, carved);
        }

        // ------------------------------------------------------------------ the roof at the head, with the skylight
        /// <summary>
        /// A shell over the head: its underside a low vault (about 7 m over the water, coming down into the walls), its top
        /// the uncut ground (so the lava runs on over it), a broken lip toward the sea, and the skylight a shaft through it.
        /// Built on rings round the skylight, so the shaft's edge is clean.
        /// </summary>
        static Mesh InletRoofMesh()
        {
            const int Segs = 72;
            const float Step = 0.6f, MaxRho = 24f;
            int rings = Mathf.CeilToInt((MaxRho - InletSkyR) / Step) + 1;
            var sky = InletSky;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var ceil = new int[rings, Segs];
            var top = new int[rings, Segs];
            var pos = new Vector2[rings, Segs];
            float Front(float b) => InletRoofFront + 1.2f * Fbm(b * 0.18f, 3f, 2, 173);
            float Top(Vector2 p) => HeightNoInlet(p.x, p.y) + 0.08f;
            float Ceil(Vector2 p, float r, float t)
            {
                float baseY = Mathf.Min(7.2f, t - 2.4f);
                float y = r < InletWaterR + InletLedgeW
                    ? baseY + 0.8f * (1f - (r / (InletWaterR + InletLedgeW)) * (r / (InletWaterR + InletLedgeW)))
                    : Mathf.Lerp(baseY, t - 1.2f, (r - InletWaterR - InletLedgeW) / (InletWallW + 0.3f));
                return y + 0.3f * Fbm(p.x * 0.3f, p.y * 0.3f, 2, 175);
            }
            for (int k = 0; k < rings; k++)
                for (int m = 0; m < Segs; m++)
                {
                    float rho = InletSkyR + k * Step, th = m * Mathf.PI * 2f / Segs;
                    var p = sky + (_inletU * Mathf.Cos(th) + _inletV * Mathf.Sin(th)) * rho;
                    // past the lip, the rings are pulled back onto it (so the roof ends on a clean line; the cells there fold flat)
                    float pa = Vector2.Dot(p - _inletC, _inletU), fa = Front(Vector2.Dot(p - _inletC, _inletV));
                    if (pa > fa) p -= _inletU * (pa - fa);
                    pos[k, m] = p;
                    InletLocal(p.x, p.y, out _, out float r);
                    float t = Top(p);
                    ceil[k, m] = verts.Count; verts.Add(new Vector3(p.x, Ceil(p, r, t), p.y));
                    top[k, m] = verts.Count; verts.Add(new Vector3(p.x, t, p.y));
                }
            // 0 in, 2 out into the walls (buried)
            int Cell(int k, int m)
            {
                if (k >= rings - 1) return 2;
                var c = (pos[k, m] + pos[k + 1, m] + pos[k, (m + 1) % Segs] + pos[k + 1, (m + 1) % Segs]) * 0.25f;
                InletLocal(c.x, c.y, out _, out float r);
                return r > InletOuterR + 0.3f ? 2 : 0;
            }
            void Tri(int i0, int i1, int i2, Vector3 want)
            {
                var n = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
                if (Vector3.Dot(n, want) >= 0f) tris.AddRange(new[] { i0, i1, i2 }); else tris.AddRange(new[] { i0, i2, i1 });
            }
            void Quad(int a0, int a1, int b0, int b1, Vector3 want) { Tri(a0, a1, b0, want); Tri(a1, b1, b0, want); }
            int Dup(int i) { verts.Add(verts[i]); return verts.Count - 1; }
            for (int k = 0; k < rings - 1; k++)
                for (int m = 0; m < Segs; m++)
                {
                    if (Cell(k, m) != 0) continue;
                    int m1 = (m + 1) % Segs;
                    Quad(ceil[k, m], ceil[k, m1], ceil[k + 1, m], ceil[k + 1, m1], Vector3.down);
                    Quad(top[k, m], top[k, m1], top[k + 1, m], top[k + 1, m1], Vector3.up);
                    // the skylight's shaft
                    if (k == 0)
                    {
                        var mid = (pos[0, m] + pos[0, m1]) * 0.5f;
                        var inward = new Vector3(sky.x - mid.x, 0f, sky.y - mid.y);
                        Quad(Dup(ceil[0, m]), Dup(ceil[0, m1]), Dup(top[0, m]), Dup(top[0, m1]), inward);
                    }
                }
            // the lip, where the roof ends toward the sea: a strip from the vault up to the ground along the lip's line
            {
                Vector3 outward = new Vector3(_inletU.x, 0f, _inletU.y);
                int prevC = -1, prevT = -1;
                for (float b = -InletOuterR - 1f; b <= InletOuterR + 1f; b += 0.4f)
                {
                    var p = InletPoint(Front(b), b);
                    InletLocal(p.x, p.y, out _, out float r);
                    if (r > InletOuterR + 0.3f) { prevC = -1; continue; }
                    float t = Top(p);
                    int ci = verts.Count; verts.Add(new Vector3(p.x, Ceil(p, r, t) - 0.05f, p.y));
                    int ti = verts.Count; verts.Add(new Vector3(p.x, t + 0.05f, p.y));
                    if (prevC >= 0) Quad(prevC, ci, prevT, ti, outward);
                    prevC = ci; prevT = ti;
                }
            }
            var mesh = new Mesh { name = "IT_InletRoof", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static void BuildInlet(Transform root, Terrain terrain, System.Random rnd)
        {
            if (!_inletOK) return;
            var lavaMat = CaveMat("IT_LavaTube", new Color(0.2f, 0.19f, 0.19f));
            MeshObject(root, "入江の奥の天井", SaveMesh(InletRoofMesh()), lavaMat, true);
            float G(Vector2 p) => terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;

            // black salt where the spray dries: at the foot of the walls along both ledges
            var black = Mat("IT_BlackSalt", new Color(0.17f, 0.17f, 0.18f));
            int n = 0;
            for (float a = InletCenterA - 6f; a < -2f; a += 4.5f)
                foreach (float side in new[] { 1f, -1f })
                {
                    if ((n++ & 1) == 1 && a > -6f) continue;
                    float b = side * (InletWaterR + InletLedgeW - 0.7f) + (float)(rnd.NextDouble() - 0.5);
                    var p = InletPoint(a + (float)rnd.NextDouble() * 2f, b);
                    var rock = ExtractTestBuilder.SaltRock(root, new Vector3(p.x, G(p), p.y), 1.1f);
                    rock.gameObject.name = "黒い塩（しぶき）";
                    // (stand-in: the rock salt's mushroom painted black, until black salt has its own kind and shape)
                    foreach (var mr in rock.GetComponentsInChildren<Renderer>(true))
                    {
                        var mats = mr.sharedMaterials;
                        for (int q = 0; q < mats.Length; q++) mats[q] = black;
                        mr.sharedMaterials = mats;
                    }
                }
            // rocks fallen from the walls onto the ledge (not across it: the way round must stay open)
            var stone = Mat("IT_LavaRock", new Color(0.2f, 0.2f, 0.21f));
            for (int i = 0; i < 7; i++)
            {
                float ang = (float)rnd.NextDouble() * Mathf.PI * 1.6f - Mathf.PI * 0.8f;
                Vector2 dir = -_inletU * Mathf.Cos(ang) + _inletV * Mathf.Sin(ang);
                var p = InletPoint(InletCenterA, 0f) + dir * (InletWaterR + InletLedgeW - 0.3f);
                if (Vector2.Dot(p - _inletC, _inletU) > -1f) continue;
                Rock(root, "落ちた岩", stone, new Vector3(p.x, G(p), p.y), new Vector3(1.4f, 1f, 1.2f) * (0.6f + (float)rnd.NextDouble() * 0.6f), rnd);
            }
            var c = InletPoint(-6f, 0f);
            MoveTestBuilder.Label("入江", new Vector3(c.x, 14f, c.y), 4f);
            var s = InletSky;
            MoveTestBuilder.Label("天窓（下は塩水）", new Vector3(s.x, G(s) + 4f, s.y), 2.4f);
        }

        /// <summary>The inlet's shots for Shoot Caves: from the mouth, under the roof, down the skylight, from the air.</summary>
        static void ShootInlet(System.Action<string, Vector3, Vector3, float> shot, Terrain t)
        {
            if (!_inletOK) return;
            float G(Vector2 p) => t.SampleHeight(new Vector3(p.x, 0f, p.y)) + t.transform.position.y;
            Vector3 At(float a, float b, float up) { var p = InletPoint(a, b); return new Vector3(p.x, G(p) + up, p.y); }
            Vector3 head = At(InletCenterA - InletWaterR, 0f, 0f);
            head.y = 1.5f;
            shot("55_inlet_from_mouth", At(-1.5f, -(InletWaterR + InletLedgeW * 0.5f), 0.47f), head + Vector3.up * 2f, 70f);
            var sky = InletSky;
            // from the head's ledge at the water's edge, up the skylight
            var under = At(InletCenterA - InletWaterR - 0.6f, 1.5f, 0.47f);
            shot("58_inlet_under_skylight", under, new Vector3(sky.x, 13f, sky.y), 80f);
            // leaning over the skylight's edge: down the shaft to the water
            Vector2 rim = sky - _inletU * (InletSkyR - 0.3f);          // (on the roof: the ground before the cut)
            shot("59_inlet_skylight_from_above", new Vector3(rim.x, HeightNoInlet(rim.x, rim.y) + 0.55f, rim.y), new Vector3(sky.x + _inletU.x * 1.5f, 0f, sky.y + _inletU.y * 1.5f), 70f);
            var mid = InletPoint(-8f, 0f);
            shot("air_inlet", new Vector3(mid.x, 0f, mid.y) + new Vector3(_inletU.x, 0f, _inletU.y) * 45f + new Vector3(_inletV.x, 0f, _inletV.y) * 20f + Vector3.up * 40f, new Vector3(mid.x, 2f, mid.y), 60f);
        }
    }
}
