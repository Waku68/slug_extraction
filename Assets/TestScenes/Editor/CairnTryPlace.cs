using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Puts the Cairn-style shapes (SlugDesign.CairnShapesBuilder: stair rock, undercut rocks, needle, arch, split rock) and
    /// stone cairns into IslandTest_800 where the try's pictures look (docs/ideas/cairn_terrain_ideas.md, J K L), under
    /// one object "CairnTry" (Remove takes it all out). Cairns mark the routes where no path shows: across the black lava
    /// from the crater's cut towards the inlet, and across the salt flat to the canyon's mouth.
    /// </summary>
    public static class CairnTryPlace
    {
        const string Root = "CairnTry";
        const string Shapes = SlugDesign.CairnShapesBuilder.Dir;
        const string Cairns = "Assets/Art/Models/Props/Cairn";

        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Place Shapes")]
        public static string PlaceMenu() => CairnTryShots.WithIsland(Place, true);
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Remove Shapes")]
        public static string RemoveMenu() => CairnTryShots.WithIsland(() => { Remove(); return "removed"; }, true);

        static Terrain _t;

        public static string Place()
        {
            _t = Object.FindAnyObjectByType<Terrain>();
            if (!_t) return "open IslandTest_800 first";
            Remove();
            var root = new GameObject(Root).transform;
            var sb = new StringBuilder();

            // canyon mouth (picture 1): a split rock on the floor, a stair rock at the left wall's foot, a rock roof at the right's
            Vector2 cf = new Vector2(228, 232), ct = new Vector2(300, 252);
            sb.AppendLine(Put(root, "Split_Stone", Along(cf, ct, 22f, -3f), Face(cf, ct, 22f, -3f)));
            sb.AppendLine(AtWall(root, "StepRock_Red", cf, ct, 34f, -1f, true));
            sb.AppendLine(AtWall(root, "Hisashi_Red", cf, ct, 27f, 1f, false));

            // meadow towards the mountain (picture 4): a mushroom rock near, a needle further off to the left
            Vector2 mf = new Vector2(346, 378), mt = new Vector2(368, 316);
            sb.AppendLine(Put(root, "Kinoko_Stone", Along(mf, mt, 18f, 5f), Face(mf, mt, 18f, 5f)));
            sb.AppendLine(Put(root, "Needle_Stone", Along(mf, mt, 60f, -22f), Face(mf, mt, 60f, -22f)));
            sb.AppendLine(Put(root, "StepRock_Stone", Along(mf, mt, 34f, 14f), Face(mf, mt, 34f, 14f) * Quaternion.Euler(0f, 90f, 0f)));

            // forest camp towards the mountain (picture 5): an arch to walk through
            Vector2 ff = new Vector2(500, 300), ft = new Vector2(375, 262);
            sb.AppendLine(Put(root, "Arch_Red", Along(ff, ft, 30f, 6f), Face(ff, ft, 30f, 6f)));

            // cairns where no path shows
            int n = 0;
            n += CairnLine(root, new[] { new Vector2(372, 155), new Vector2(395, 146), new Vector2(430, 150) }, 22f, 1);
            n += CairnLine(root, new[] { new Vector2(250, 318), new Vector2(228, 232) }, 25f, 5);
            sb.AppendLine($"cairns {n}");
            return sb.ToString();
        }

        static void Remove()
        {
            var old = GameObject.Find(Root);
            if (old) Object.DestroyImmediate(old);
        }

        // a point on the line from → to, metres along it and metres to its right (− left), on the ground
        static Vector3 Along(Vector2 from, Vector2 to, float metres, float right)
        {
            Vector2 a = IslandTest800Builder.World(from), b = IslandTest800Builder.World(to);
            Vector2 d = (b - a).normalized, r = new Vector2(d.y, -d.x);
            return OnGround(a + d * metres + r * right);
        }

        // turned so its front (−x for the stair, +z for the rest) looks back at the line's start
        static Quaternion Face(Vector2 from, Vector2 to, float metres, float right)
        {
            Vector3 p = Along(from, to, metres, right), f = OnGround(IslandTest800Builder.World(from));
            Vector3 look = f - p; look.y = 0f;
            return Quaternion.LookRotation(look.normalized);
        }

        static Vector3 OnGround(Vector2 xz) => new Vector3(xz.x, _t.SampleHeight(new Vector3(xz.x, 0f, xz.y)) + _t.transform.position.y, xz.y);

        // walks sideways from the line until the ground is steeper than 50° (a wall), and sets the piece at its foot,
        // its back (−z) to the wall; the stair's steps (−x) run along the wall towards the line's start
        static string AtWall(Transform root, string prefab, Vector2 from, Vector2 to, float metres, float side, bool stair)
        {
            Vector2 a = IslandTest800Builder.World(from), b = IslandTest800Builder.World(to);
            Vector2 d = (b - a).normalized, r = new Vector2(d.y, -d.x) * side;
            Vector2 p = a + d * metres;
            var data = _t.terrainData;
            Vector3 tp = _t.transform.position;
            for (int i = 0; i < 120; i++)
            {
                Vector2 q = p + r * 0.5f;
                float u = (q.x - tp.x) / data.size.x, v = (q.y - tp.z) / data.size.z;
                if (data.GetSteepness(u, v) > 50f) break;
                p = q;
            }
            var pos = OnGround(p - r * 1.2f);
            var rot = Quaternion.LookRotation(new Vector3(-r.x, 0f, -r.y).normalized);    // +z away from the wall
            if (stair) rot *= Quaternion.Euler(0f, Vector2.SignedAngle(d, r) > 0f ? 90f : -90f, 0f);
            return Put(root, prefab, pos, rot);
        }

        static string Put(Transform root, string name, Vector3 pos, Quaternion rot)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Shapes}/{name}.prefab");
            if (!src) return name + ": no prefab (run Slug/Nature/Build Cairn Shapes)";
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, root);
            go.transform.SetPositionAndRotation(pos + Vector3.down * 0.12f, rot);
            return $"{name} at {pos.x:0} {pos.y:0.0} {pos.z:0}";
        }

        static int CairnLine(Transform root, Vector2[] pts, float every, int seed)
        {
            int n = 0;
            float carry = every * 0.5f;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = IslandTest800Builder.World(pts[i]), b = IslandTest800Builder.World(pts[i + 1]);
                float len = (b - a).magnitude;
                for (float s = carry; s < len; s += every)
                {
                    var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Cairns}/Cairn_{(char)('A' + (n + seed) % 3)}.prefab");
                    if (!src) return n;
                    Vector2 d = (b - a).normalized, side = new Vector2(d.y, -d.x) * (((n + seed) % 2 == 0 ? 1f : -1f) * 1.5f);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(src, root);
                    go.transform.SetPositionAndRotation(OnGround(a + d * s + side) + Vector3.down * 0.05f, Quaternion.Euler(0f, (n * 73 + seed * 31) % 360, 0f));
                    n++;
                    carry = s + every - len;
                }
            }
            return n;
        }
    }
}
