using System.Collections.Generic;
using System.IO;
using System.Text;
using SlugDesign;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The rock-salt mountain as a cluster of salt towers (Cairn look, "攻め", Nov 3; docs/ideas/cairn_terrain_ideas.md §8).
    /// Instead of one cone with benches, seven towers of different heights stand on a high floor of gullies (40-46 m):
    /// their walls are too steep to climb, so the ways up are
    /// - ledge trails (岩棚の細道) winding round each tower's flank, 2.2 m wide (a human's 8 m), never steeper than 17°
    /// - stair-rock shortcuts (段の岩の近道): steps carved in the wall, 0.36 m each (the slug climbs 0.45), 31°
    /// - old slug plank bridges (昔のナメクジの橋) from one tower's flat top to a ledge on the next, with rope rails
    /// The summit tower (113 m) is reached only over the bridges. The south flank with the pool, the falls and the salt
    /// cave is left as it was. The terrain inside the mountain is lowered to the gully floor (never raised); the things
    /// on it move down with it, things inside a tower are hidden, and the mountain's own ways (the shoulder road, the
    /// ridge trails, the shoulder's stone bridge) are hidden. Remove puts all of it back (heights and things are kept in
    /// Assets/TestScenes/Generated/SaltTowers_Backup.txt). Menu Slug/IslandTest 800 m/Cairn Try/Salt Towers ...
    /// </summary>
    public static class IslandSaltTowers
    {
        const string Root = "SaltTowers";
        const string Dir = "Assets/Art/Nature/SaltTowers";
        const string Backup = MoveTestBuilderGen.Dir + "/SaltTowers_Backup.txt";

        const float ZoneIn = 52f, ZoneOut = 66f;            // fully lowered inside, blended out to (m from the summit): the shoulder
        static readonly Vector2 KeepAt = new Vector2(-4f, -110f);   // pool, falls, salt cave (world x, z)
        const float KeepIn = 30f, KeepOut = 44f;

        const float LedgeW = 2.2f, LedgeMaxGrade = 0.3f, StepRise = 0.36f, StepRun = 0.6f;

        class Tower
        {
            public string name; public Vector2 rel; public float top, r0; public int seed;
            public float floor; public List<Vector3> ledge = new List<Vector3>();
            public Vector2 C => Center + rel;
        }

        // rel: metres east (x) and north (z) of the summit; top: height of the flat top (m); r0: radius at the foot (m)
        static Tower[] Towers() => new[]
        {
            new Tower { name = "T0 山頂の柱", rel = new Vector2(0f, 0f), top = 113f, r0 = 11f, seed = 1 },
            new Tower { name = "T1 北西の柱", rel = new Vector2(-27f, 21f), top = 97f, r0 = 8f, seed = 2 },
            new Tower { name = "T2 北東の柱", rel = new Vector2(26f, 23f), top = 103f, r0 = 9f, seed = 3 },
            new Tower { name = "T3 南東の柱", rel = new Vector2(29f, -21f), top = 91f, r0 = 8f, seed = 4 },
            new Tower { name = "T4 南西の柱", rel = new Vector2(-25f, -24f), top = 85f, r0 = 7f, seed = 5 },
            new Tower { name = "T5 東の段の柱", rel = new Vector2(43f, 3f), top = 79f, r0 = 6f, seed = 6 },
            new Tower { name = "T6 北の柱", rel = new Vector2(3f, 38f), top = 87f, r0 = 7f, seed = 7 },
        };

        // low crags on the skirt (6-20 m, a human's 20-70 m) that break the wide slopes round the towers: a ring of
        // candidates, kept clear of the towers, the pool and falls, the canyon's head (west) and the saddle way (north)
        static List<Tower> Crags(Tower[] towers)
        {
            var list = new List<Tower>();
            if (!SkirtCrags) return list;
            for (int i = 0; i < 90; i++)
            {
                float a = i * 0.95f + 0.3f * Mathf.Sin(i * 3.7f);
                float r = 62f + 92f * Mathf.Repeat(i * 0.618f, 1f);
                var rel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                var at = Center + rel;
                bool clear = (at - KeepAt).magnitude > KeepOut + 6f
                             && !(rel.x < -60f && rel.y > -15f && rel.y < 65f)
                             && (rel - new Vector2(-28f, 111f)).magnitude > 28f && (rel - new Vector2(-14f, 85f)).magnitude > 18f;
                foreach (var tw in towers) if ((rel - tw.rel).magnitude < tw.r0 * 1.5f + 10f) clear = false;
                foreach (var c in list) if ((rel - c.rel).magnitude < 20f) clear = false;
                if (!clear) continue;
                float h = 6f + 14f * Mathf.Repeat(i * 0.377f + 0.2f, 1f);
                list.Add(new Tower { name = $"小塔 {list.Count + 1}", rel = rel, top = h, r0 = 4f + h * 0.28f, seed = 20 + i });
            }
            return list;
        }

        // Nov 3 わくさん: change only above the old shoulder (62 m), with smaller pillars, so the skirt stays as it was
        const bool SkirtCrags = false;

        static Vector2 Center => IslandTest800Builder.World(new Vector2(375f, 262f));
        static Terrain _t;

        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Salt Towers Build")]
        public static string BuildMenu() => CairnTryShots.WithIsland(Build, true);
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Salt Towers Remove")]
        public static string RemoveMenu() => CairnTryShots.WithIsland(Remove, true);

        public static string Build()
        {
            _t = Object.FindAnyObjectByType<Terrain>();
            if (!_t) return "open IslandTest_800 first";
            if (File.Exists(Backup)) Remove();
            var sb = new StringBuilder();
            var towers = Towers();
            var crags = Crags(towers);
            var all = new List<Tower>(towers); all.AddRange(crags);
            SaveBackup(towers);
            sb.AppendLine(LowerGround());
            sb.AppendLine(MoveThings(all.ToArray()));
            foreach (var tw in all) tw.floor = Ground(tw.C);
            foreach (var cr in crags) cr.top = cr.floor + cr.top;     // (a crag's top was given above its foot)

            Directory.CreateDirectory(Dir);
            var mats = PropKit.Mats(Dir, "SaltTowers",
                new PropKit.Spec("Salt", "#E7E0D6", true).Mottle("#D3CABC", 0.4f, 0.12f).Camo("#EDE7DE", "#DCD3C6", "#E9DCD2", 0.09f, 0.35f),
                new PropKit.Spec("Ledge", "#D6C6B0", true).Mottle("#C4B39C", 0.4f, 0.5f),
                new PropKit.Spec("Plank", "#7A5638", true).Mottle("#664630", 0.4f, 3f),
                new PropKit.Spec("Rope", "#B59A6A", false));
            mats[0].SetFloat("_TopCoat", 0f);
            mats[1].SetFloat("_TopCoat", 0f);
            foreach (var m in mats) EditorUtility.SetDirty(m);

            var root = new GameObject(Root).transform;
            int steps = 0, bridges = 0;
            // ledges first (bridges and stairs land on them)
            // each bridge lands where the next pillar's ledge passes at the height of the pillar it comes from
            float T(int i) => towers[i].top - 0.2f;
            Ledge(towers[0], new[] { (Face(towers[0], towers[1]), T(1) - 0.8f), (Face(towers[0], towers[2]), T(2)), (Face(towers[0], towers[2]) + 2.2f, T(0)) }, false);
            Ledge(towers[1], new[] { (Face(towers[1], towers[0]) + 2.6f, -1f), (Face(towers[1], towers[4]), T(4)), (Face(towers[1], towers[4]) + 2.4f, T(1)) }, true);
            Ledge(towers[2], new[] { (Face(towers[2], towers[0]) + 3.2f, -1f), (Face(towers[2], towers[6]), T(6)), (Face(towers[2], towers[3]), T(3)), (Face(towers[2], towers[3]) + 2.4f, T(2)) }, true);
            Ledge(towers[3], new[] { (Face(towers[3], towers[0]) + 3f, -1f), (Face(towers[3], towers[5]), T(5)) }, true);
            Ledge(towers[4], new[] { (Face(towers[4], towers[0]) + 2.2f, -1f), (Face(towers[4], towers[1]) + 0.3f, T(4)) }, true);
            Ledge(towers[6], new[] { (Face(towers[6], towers[0]) + 2.5f, -1f), (Face(towers[6], towers[2]) + 0.3f, T(6)) }, true);

            foreach (var tw in towers) PutTower(root, tw, mats);
            var cragRoot = new GameObject("岩の小塔（裾）").transform;
            cragRoot.SetParent(root, false);
            foreach (var cr in crags) PutTower(cragRoot, cr, mats);
            // stair shortcuts: T1 from the floor to its ledge 12 m up, T3 from its ledge's end up to the top, T5 all the way
            steps += Stair(root, mats, towers[1], LedgeAt(towers[1], towers[1].floor + 12f), towers[1].floor, +1f);
            var t3end = towers[3].ledge[towers[3].ledge.Count - 1];
            steps += Stair(root, mats, towers[3], t3end, towers[3].top, -1f);
            steps += Stair(root, mats, towers[5], new Vector3(Face(towers[5], towers[0]) - 0.6f, towers[5].top, 0f), towers[5].floor, +1f);
            // bridges from a top to the next tower's ledge (or top)
            bridges += Bridge(root, mats, towers[4], towers[1]);
            bridges += Bridge(root, mats, towers[1], towers[0]);
            bridges += Bridge(root, mats, towers[2], towers[0]);
            bridges += Bridge(root, mats, towers[3], towers[2]);
            bridges += Bridge(root, mats, towers[5], towers[3]);
            bridges += Bridge(root, mats, towers[6], towers[2]);
            AssetDatabase.SaveAssets();
            sb.AppendLine($"towers {towers.Length}, ledges, steps {steps}, bridges {bridges}");
            foreach (var tw in towers) sb.AppendLine($"{tw.name}: floor {tw.floor:0} m, top {tw.top:0} m, ledge {tw.ledge.Count} pts");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ the tower's wall
        // its radius at a height and an angle: tapering, lumpy, with strata (a ledge-like step every 4.5 m) and
        // vertical flutes, so the wall reads as rock and none of it can be climbed
        static float Radius(Tower tw, float y, float a)
        {
            float t = Mathf.Clamp01((y - (tw.floor - 8f)) / (tw.top - tw.floor + 8f));
            // a wide foot that narrows (a skirt of fallen blocks low down), an oval that turns with height,
            // big lobes (buttresses) that wander, uneven strata, a few deep clefts
            float r = tw.r0 * (1.1f - 0.5f * t + 0.12f * Mathf.Pow(1f - t, 4f));
            float turn = tw.seed * 0.9f + y * 0.012f;
            float oval = 0.2f * Mathf.Cos(2f * (a - turn));
            float lobes = 0.12f * Mathf.Sin(3f * a + tw.seed * 1.7f + y * 0.03f) + 0.07f * Mathf.Sin(5f * a - tw.seed + y * 0.07f);
            float period = 3.5f + 2.5f * Mathf.PerlinNoise(a * 0.8f + tw.seed, y * 0.02f);
            float f = Mathf.Repeat(y / period + 0.35f * Mathf.Sin(a * 2f + tw.seed), 1f);
            float k = Mathf.Clamp(tw.r0 / 16f, 0.45f, 1.5f);
            float strata = k * (0.8f * Mathf.SmoothStep(0f, 1f, f * 4f) - 0.8f) * (0.6f + 0.6f * Mathf.PerlinNoise(a * 1.3f, y * 0.05f + tw.seed));
            float cleft = -2.2f * k * Mathf.Pow(Mathf.Abs(Mathf.Sin(a * 2.5f + tw.seed * 2.1f + y * 0.004f)), 40f);
            return r * (1f + oval + lobes) + strata + cleft;
        }

        // the tower leans a little (its middle wanders off the foot as it rises)
        static Vector2 Axis(Tower tw, float y)
        {
            float h = Mathf.Max(0f, y - tw.floor);
            float a = tw.seed * 2.4f;
            return tw.C + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (h * 0.06f) + new Vector2(Mathf.Sin(h * 0.05f + tw.seed), Mathf.Cos(h * 0.04f)) * 1.5f;
        }

        static void PutTower(Transform root, Tower tw, Material[] mats)
        {
            var kit = new NatureMeshKit(2);
            float y0 = tw.floor - 8f, y1 = tw.top;
            int rows = Mathf.CeilToInt((y1 - y0) / 1.1f), sides = 56;
            int first = kit.VertexCount;
            PropKit.Surface(kit, (t, a) =>
            {
                float y = Mathf.Lerp(y0, y1, t);
                float r = Radius(tw, y, a);
                if (t > 0.985f) r *= 0.97f;                     // a small bevel at the top edge
                var ax = Axis(tw, y);
                return new Vector3(ax.x + Mathf.Cos(a) * r, y, ax.y + Mathf.Sin(a) * r);
            }, rows, sides, 0);
            // the flat top, a little domed (both windings, so it shows from above whichever way the ring runs)
            int ring = first + rows * sides;
            var top = Axis(tw, y1);
            int mid = kit.Vert(new Vector3(top.x, y1 + 0.6f, top.y), new Vector2(0.5f, 1f));
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                kit.Tri(0, ring + i, ring + j, mid);
                kit.Tri(0, ring + j, ring + i, mid);
            }
            AddLedgeMesh(kit, tw, 1);
            var mesh = NatureAssets.SaveMesh(kit.ToMesh($"SaltTower_{tw.seed}"), $"{Dir}/SaltTower_{tw.seed}.asset");
            var go = new GameObject(tw.name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { mats[0], mats[1] };
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
        }

        // ------------------------------------------------------------------ ledge trails
        static float Face(Tower a, Tower b) => Mathf.Atan2(b.C.y - a.C.y, b.C.x - a.C.x);

        // a trail winding up the tower through waypoints (angle, height); height < 0 = the floor where it starts.
        // Each stretch winds round far enough to stay under the grade (extra turns if needed). Stored as (angle, y, 0)
        static void Ledge(Tower tw, (float a, float y)[] pts, bool fromFloor)
        {
            tw.ledge.Clear();
            float a = pts[0].a, y = pts[0].y < 0f ? GroundAt(tw, pts[0].a) + 0.05f : pts[0].y;
            tw.ledge.Add(new Vector3(a, y, 0f));
            for (int i = 1; i < pts.Length; i++)
            {
                float target = pts[i].a, ty = pts[i].y;
                float da = Mathf.Repeat(target - a, Mathf.PI * 2f);
                float rMid = Radius(tw, (y + ty) * 0.5f, a) + LedgeW * 0.5f;
                while (da * rMid * LedgeMaxGrade < Mathf.Abs(ty - y)) da += Mathf.PI * 2f;
                int n = Mathf.CeilToInt(da * rMid / 1.2f);
                for (int k = 1; k <= n; k++)
                {
                    float s = k / (float)n;
                    tw.ledge.Add(new Vector3(a + da * s, Mathf.Lerp(y, ty, s), 0f));
                }
                a += da; y = ty;
            }
        }

        static Vector3 LedgeAt(Tower tw, float y)
        {
            for (int i = 1; i < tw.ledge.Count; i++)
                if (tw.ledge[i].y >= y) return tw.ledge[i];
            return tw.ledge[tw.ledge.Count - 1];
        }

        static Vector3 OnWall(Tower tw, float a, float y, float outFromWall)
        {
            float r = Radius(tw, y, a) + outFromWall;
            var ax = Axis(tw, y);
            return new Vector3(ax.x + Mathf.Cos(a) * r, y, ax.y + Mathf.Sin(a) * r);
        }

        // the ledge as a slab: flat top (inner edge sunk into the wall), a short outer face, and an underside back to the wall
        static void AddLedgeMesh(NatureMeshKit kit, Tower tw, int sub)
        {
            if (tw.ledge.Count < 2) return;
            int prev = -1;
            for (int i = 0; i < tw.ledge.Count; i++)
            {
                float a = tw.ledge[i].x, y = tw.ledge[i].y;
                float r = Radius(tw, y, a), w = LedgeW * (1f + 0.15f * Mathf.Sin(i * 0.7f + tw.seed));
                var ax = Axis(tw, y);
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), c = new Vector3(ax.x, 0f, ax.y);
                int s = kit.Vert(c + dir * (r - 1.2f) + Vector3.up * y, new Vector2(0f, i));
                kit.Vert(c + dir * (r + w) + Vector3.up * y, new Vector2(0.4f, i));
                kit.Vert(c + dir * (r + w - 0.2f) + Vector3.up * (y - 0.7f), new Vector2(0.6f, i));
                kit.Vert(c + dir * (r - 1.2f) + Vector3.up * (y - 2.2f), new Vector2(1f, i));
                if (prev >= 0)
                    for (int e = 0; e < 3; e++)
                        kit.Quad(sub, prev + e, prev + e + 1, s + e + 1, s + e);
                prev = s;
            }
        }

        // ------------------------------------------------------------------ stair-rock shortcuts (carved steps)
        // from a point on the wall (angle, height) the steps run round the wall in direction dir, stepping down to toY
        static int Stair(Transform root, Material[] mats, Tower tw, Vector3 from, float toY, float dir)
        {
            var kit = new NatureMeshKit(1);
            float a = from.x, y = from.y;
            int n = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(y - toY) / StepRise));
            float down = Mathf.Sign(toY - y);
            for (int i = 0; i < n; i++)
            {
                float top = y + down * StepRise * i;
                float r = Radius(tw, top, a);
                float da = StepRun / Mathf.Max(r, 1f);
                var ax = Axis(tw, top);
                Vector3 c = new Vector3(ax.x, 0f, ax.y);
                Vector3 d0 = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), d1 = new Vector3(Mathf.Cos(a + dir * da * 1.15f), 0f, Mathf.Sin(a + dir * da * 1.15f));
                float bottom = top - 1.4f;
                var p = new[]
                {
                    c + d0 * (r - 1f) + Vector3.up * top, c + d0 * (r + 1.7f) + Vector3.up * top, c + d1 * (r + 1.7f) + Vector3.up * top, c + d1 * (r - 1f) + Vector3.up * top,
                    c + d0 * (r - 1f) + Vector3.up * bottom, c + d0 * (r + 1.5f) + Vector3.up * bottom, c + d1 * (r + 1.5f) + Vector3.up * bottom, c + d1 * (r - 1f) + Vector3.up * bottom,
                };
                Box(kit, p, 0);
                a += dir * da;
            }
            var mesh = NatureAssets.SaveMesh(kit.ToMesh($"SaltStair_{tw.seed}_{(int)from.y}"), $"{Dir}/SaltStair_{tw.seed}_{(int)from.y}.asset");
            var go = new GameObject($"段の岩の近道（{tw.name}）");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mats[1];
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return n;
        }

        // 8 corners: top 0-3, bottom 4-7, same order round
        static void Box(NatureMeshKit kit, Vector3[] p, int sub)
        {
            int[][] faces = { new[] { 0, 1, 2, 3 }, new[] { 4, 7, 6, 5 }, new[] { 0, 4, 5, 1 }, new[] { 1, 5, 6, 2 }, new[] { 2, 6, 7, 3 }, new[] { 3, 7, 4, 0 } };
            foreach (var f in faces)
            {
                int s = kit.Vert(p[f[0]], Vector2.zero); kit.Vert(p[f[1]], Vector2.right); kit.Vert(p[f[2]], Vector2.one); kit.Vert(p[f[3]], Vector2.up);
                kit.Quad(sub, s, s + 1, s + 2, s + 3);
            }
        }

        // ------------------------------------------------------------------ old slug plank bridges
        // from a's flat top (its edge facing b) to b's ledge where it passes at a's top height (or b's top)
        static int Bridge(Transform root, Material[] mats, Tower a, Tower b)
        {
            float fa = Face(a, b), fb = Face(b, a);
            Vector3 from = OnWall(a, fa, a.top, -1.5f);
            Vector3 to;
            if (b.ledge.Count > 1)
            {
                // the ledge point nearest to a's top height, facing a
                Vector3 best = b.ledge[0]; float bestD = float.MaxValue;
                foreach (var l in b.ledge)
                {
                    float d = Mathf.Abs(l.y - a.top) + 6f * Mathf.Abs(Mathf.DeltaAngle(l.x * Mathf.Rad2Deg, fb * Mathf.Rad2Deg)) / 180f;
                    if (d < bestD) { bestD = d; best = l; }
                }
                to = OnWall(b, best.x, best.y, LedgeW * 0.6f);
            }
            else to = OnWall(b, fb, b.top, -1.5f);

            var kit = new NatureMeshKit(2);
            Vector3 span = to - from, flat = new Vector3(span.x, 0f, span.z);
            Vector3 side = Vector3.Cross(Vector3.up, flat.normalized) * 0.7f;
            float len = flat.magnitude, sag = Mathf.Min(1.4f, len * 0.035f);
            int planks = Mathf.CeilToInt(len / 0.3f);
            System.Func<float, Vector3> deck = s => Vector3.Lerp(from, to, s) + Vector3.down * (sag * 4f * s * (1f - s));
            for (int i = 0; i < planks; i++)
            {
                float s0 = i / (float)planks, s1 = (i + 0.82f) / planks;
                Vector3 p0 = deck(s0), p1 = deck(s1);
                float wob = 0.06f * Mathf.Sin(i * 2.3f);
                var p = new[]
                {
                    p0 - side + Vector3.up * wob, p0 + side, p1 + side, p1 - side + Vector3.up * wob,
                    p0 - side + Vector3.down * 0.14f, p0 + side + Vector3.down * 0.14f, p1 + side + Vector3.down * 0.14f, p1 - side + Vector3.down * 0.14f,
                };
                Box(kit, p, 0);
            }
            // posts at the ends, rope rails sagging between them
            foreach (var end in new[] { 0f, 1f })
                foreach (var sd in new[] { -1f, 1f })
                {
                    Vector3 b0 = deck(end) + side * sd * 1.15f;
                    PropKit.Stick(kit, b0 + Vector3.down * 0.4f, b0 + Vector3.up * 1.1f, 0.12f, 0.1f, 6, 0);
                }
            foreach (var sd in new[] { -1f, 1f })
            {
                Vector3 prevP = deck(0f) + side * sd * 1.15f + Vector3.up * 0.9f;
                for (int k = 1; k <= 16; k++)
                {
                    float s = k / 16f;
                    Vector3 q = deck(s) + side * sd * 1.15f + Vector3.up * (0.9f - 0.3f * 4f * s * (1f - s));
                    PropKit.Stick(kit, prevP, q, 0.04f, 0.04f, 5, 1);
                    prevP = q;
                }
            }
            string id = $"{a.seed}{b.seed}";
            var mesh = NatureAssets.SaveMesh(kit.ToMesh($"SaltBridge_{id}"), $"{Dir}/SaltBridge_{id}.asset");
            var go = new GameObject($"昔の橋（{a.name} → {b.name}）");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { mats[2], mats[3] };
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return 1;
        }

        // ------------------------------------------------------------------ the ground
        static float Ground(Vector2 xz) => _t.SampleHeight(new Vector3(xz.x, 0f, xz.y)) + _t.transform.position.y;
        static float GroundAt(Tower tw, float a) => Ground(tw.C + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (Radius(tw, tw.floor, a) + LedgeW * 0.5f));

        // the floor between the pillars: the old shoulder's height (62 m) rising to 66 in the middle, with shallow hollows
        static float Floor(float x, float z)
        {
            float r = (new Vector2(x, z) - Center).magnitude;
            float g = 1f - Mathf.Abs(2f * Mathf.PerlinNoise(x * 0.018f + 3.1f, z * 0.018f + 7.7f) - 1f);
            return 62f + 4f * (1f - Mathf.Clamp01(r / 55f)) - 3f * Mathf.Pow(g, 4f) + 1f * (Mathf.PerlinNoise(x * 0.05f, z * 0.05f) - 0.5f);
        }

        static float Weight(float x, float z)
        {
            float r = (new Vector2(x, z) - Center).magnitude;
            float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ZoneIn, ZoneOut, r));
            float k = (new Vector2(x, z) - KeepAt).magnitude;
            return w * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(KeepIn, KeepOut, k));
        }

        static string LowerGround()
        {
            var data = _t.terrainData;
            int res = data.heightmapResolution;
            var h = data.GetHeights(0, 0, res, res);
            Vector3 tp = _t.transform.position, size = data.size;
            int changed = 0;
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                {
                    float x = tp.x + size.x * i / (res - 1f), z = tp.z + size.z * j / (res - 1f);
                    float w = Weight(x, z);
                    if (w <= 0f) continue;
                    float old = tp.y + h[j, i] * size.y;
                    float nw = Mathf.Lerp(old, Mathf.Min(old, Floor(x, z)), w);
                    if (nw < old - 0.01f) { h[j, i] = (nw - tp.y) / size.y; changed++; }
                }
            data.SetHeights(0, 0, h);
            _t.Flush();
            return $"ground lowered at {changed} points";
        }

        // ------------------------------------------------------------------ the things on the mountain
        static IEnumerable<Transform> Things()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name.StartsWith("ボット") && r.name != "ボット用の岩塩") { yield return r.transform; continue; }
                if (r.name == "ボット用の岩塩") { foreach (Transform c in r.transform) yield return c; continue; }
                if (r.name != "Map") continue;
                foreach (Transform g in r.transform)
                {
                    if (g.name.StartsWith("Label_")) yield return g;
                    else if (g.name == "隠れる物") { foreach (Transform kind in g) foreach (Transform c in kind) yield return c; }
                    else if (g.name == "Island") { foreach (Transform c in g) if (c.name.StartsWith("RockSalt")) yield return c; }
                }
            }
        }

        static readonly string[] MountainWays = { "尾根道（北の尾根）", "尾根道（東の尾根）", "尾根道（南の尾根）", "尾根道（西の尾根）" };

        static IEnumerable<Transform> Ways()
        {
            var map = GameObject.Find("Map");
            if (!map) yield break;
            foreach (Transform g in map.transform)
                if (g.name == "道" || g.name == "Island")
                    foreach (Transform c in g)
                        if (System.Array.IndexOf(MountainWays, c.name) >= 0) yield return c;
        }

        static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name + "#" + t.GetSiblingIndex() : t.name;

        static void SaveBackup(Tower[] towers)
        {
            var data = _t.terrainData;
            int res = data.heightmapResolution;
            var h = data.GetHeights(0, 0, res, res);
            var sb = new StringBuilder();
            sb.AppendLine(res.ToString());
            for (int j = 0; j < res; j++)
            {
                var row = new string[res];
                for (int i = 0; i < res; i++) row[i] = h[j, i].ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                sb.AppendLine(string.Join(",", row));
            }
            foreach (var t in Things()) sb.AppendLine($"T|{Path(t)}|{V(t.position)}|{(t.gameObject.activeSelf ? 1 : 0)}");
            foreach (var t in Ways()) sb.AppendLine($"T|{Path(t)}|{V(t.position)}|{(t.gameObject.activeSelf ? 1 : 0)}");
            File.WriteAllText(Backup, sb.ToString());
            _backupHeights = null;
        }

        static string V(Vector3 v) => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R}", v.x, v.y, v.z);

        static string MoveThings(Tower[] towers)
        {
            int moved = 0, hidden = 0;
            var data = _t.terrainData;
            foreach (var t in Things())
            {
                var p = t.position;
                foreach (var tw in towers)
                    if ((new Vector2(p.x, p.z) - tw.C).magnitude < tw.r0 * 1.45f + 2f) { t.gameObject.SetActive(false); hidden++; goto next; }
                float before = BackupGround(p);
                float after = Ground(new Vector2(p.x, p.z));
                if (Mathf.Abs(after - before) > 0.02f) { t.position = p + Vector3.up * (after - before); moved++; }
                next:;
            }
            foreach (var t in Ways()) { t.gameObject.SetActive(false); hidden++; }
            return $"things moved down {moved}, hidden {hidden}";
        }

        static float[,] _backupHeights;
        static float BackupGround(Vector3 p)
        {
            if (_backupHeights == null) LoadBackupHeights();
            var data = _t.terrainData;
            Vector3 tp = _t.transform.position, size = data.size;
            int res = data.heightmapResolution;
            float fx = Mathf.Clamp01((p.x - tp.x) / size.x) * (res - 1), fz = Mathf.Clamp01((p.z - tp.z) / size.z) * (res - 1);
            int i = Mathf.Clamp((int)fx, 0, res - 2), j = Mathf.Clamp((int)fz, 0, res - 2);
            float u = fx - i, v = fz - j;
            float h = Mathf.Lerp(Mathf.Lerp(_backupHeights[j, i], _backupHeights[j, i + 1], u), Mathf.Lerp(_backupHeights[j + 1, i], _backupHeights[j + 1, i + 1], u), v);
            return tp.y + h * size.y;
        }

        static void LoadBackupHeights()
        {
            var lines = File.ReadAllLines(Backup);
            int res = int.Parse(lines[0]);
            _backupHeights = new float[res, res];
            for (int j = 0; j < res; j++)
            {
                var parts = lines[1 + j].Split(',');
                for (int i = 0; i < res; i++) _backupHeights[j, i] = float.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public static string Remove()
        {
            _t = Object.FindAnyObjectByType<Terrain>();
            var old = GameObject.Find(Root);
            if (old) Object.DestroyImmediate(old);
            if (!_t || !File.Exists(Backup)) return "nothing to put back";
            LoadBackupHeights();
            _t.terrainData.SetHeights(0, 0, _backupHeights);
            _t.Flush();
            var byPath = new Dictionary<string, Transform>();
            foreach (var t in Things()) byPath[Path(t)] = t;
            foreach (var t in Ways()) byPath[Path(t)] = t;
            int n = 0;
            foreach (var line in File.ReadAllLines(Backup))
            {
                if (!line.StartsWith("T|")) continue;
                var parts = line.Split('|');
                if (!byPath.TryGetValue(parts[1], out var t)) continue;
                var v = parts[2].Split(',');
                t.position = new Vector3(float.Parse(v[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(v[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(v[2], System.Globalization.CultureInfo.InvariantCulture));
                t.gameObject.SetActive(parts[3] == "1");
                n++;
            }
            _backupHeights = null;
            File.Delete(Backup);
            return $"mountain put back ({n} things)";
        }
    }

    static class MoveTestBuilderGen { public const string Dir = "Assets/TestScenes/Generated"; }
}
