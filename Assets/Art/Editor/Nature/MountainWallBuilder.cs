using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 岩塩の山の段の壁と、よじ登る段の石（docs/ideas/mountain_other_games.md の A・B、Nov 4）。メニュー Slug/Nature/Build Mountain Walls で
    /// 作り直す（Assets/Art/Nature/MountainWalls）。わくさん「壁の岩は全部」：
    /// - Block 大きな面の塊（ケルンの花こう岩のように、平らな大きな面が組み合わさる）
    /// - Strata 地層の板（横縞の板が重なり、板ごとに張り出す。下はえぐれる）
    /// - Column 柱の束（幅 0.55〜1.0 m の縦の柱。柱ごとに張り出しと頭の高さが違う）
    /// 壁の部品は、幅 6 m・高さ 10 m（人なら 36 m）・奥行き 3.6 m で作り、段ごとに伸び縮みさせて置く（IslandSaltTiers）。原点は壁の根元、
    /// +z が山の外。前の面は、地形の壁（根元から上へ 3.6 m 奥へ入る）より 0.4 m ＋でこぼこ 0.1〜0.9 m だけ外にあって、地形の壁を隠す。
    /// 上は平らなふた（奥へ 1.5 m）。当たり判定は形どおり。岩はかじれないのでカクカク。
    /// - StepBlock よじ登る段の石：1 m の箱（上の面は平ら、横はごつごつ）。上の面が原点（y=0）、前の面が z=0、奥へ -z。当たり判定は箱そのもの
    /// 道の色：PathStone（石を積んだ段）、Wood（昔の板の桟道）、WoodOld（日と雨で灰色に風化した板。Nov 4 わくさん「木の上り坂を風化させて」）。
    /// </summary>
    public static class MountainWallBuilder
    {
        public const string Dir = "Assets/Art/Nature/MountainWalls";
        public const float W = 6f, Hn = 10f, Run = 3.6f, Back = 1.5f, Margin = 0.4f;
        public static readonly string[] Styles = { "Block", "Strata", "Column" };
        public const int Variants = 3;

        public static string WallPrefab(int style, int variant) => $"{Dir}/Wall_{Styles[style]}_{variant}.prefab";
        public static string BlockPrefab(int variant) => $"{Dir}/StepBlock_{variant}.prefab";
        public static string MatPath(string name) => $"{Dir}/MountainWall_{name}.mat";

        [MenuItem("Slug/Nature/Build Mountain Walls")]
        public static string Build()
        {
            var mats = PropKit.Mats(Dir, "MountainWall",
                new PropKit.Spec("Block", "#A39C8E", true).Mottle("#88827A", 0.45f, 2.2f).Camo("#ADA799", "#928C80", "#B9B596", 2.6f, 0.5f),
                new PropKit.Spec("Strata", "#ACA28F", true).Mottle("#8E8574", 0.5f, 1.6f).Camo("#B5AC99", "#968D7B", "#C2B9A2", 3.2f, 0.45f),
                new PropKit.Spec("Column", "#958F86", true).Mottle("#7C776F", 0.45f, 2.2f).Camo("#A09A90", "#858078", "#ADA89A", 2.4f, 0.45f),
                new PropKit.Spec("PathStone", "#B3AA97", true).Mottle("#958C7A", 0.5f, 3f).Camo("#BCB3A0", "#A1988A", "#C6BEAA", 3.5f, 0.4f),
                new PropKit.Spec("Wood", "#8C6C4C", true).Mottle("#6F553C", 0.4f, 4f),
                new PropKit.Spec("WoodOld", "#9A9282", true).Mottle("#7C7466", 0.5f, 3.5f).Camo("#A39C8C", "#8A8274", "#B1AB98", 4f, 0.35f));
            for (int i = 0; i < mats.Length; i++) { mats[i].SetFloat("_TopCoat", i < 3 ? 0.7f : i == 3 ? 0.35f : 0f); EditorUtility.SetDirty(mats[i]); }

            var sb = new StringBuilder();
            for (int s = 0; s < Styles.Length; s++)
                for (int v = 0; v < Variants; v++)
                {
                    var mesh = NatureAssets.SaveMesh(WallMesh(s, v, $"Wall_{Styles[s]}_{v}_Mesh"), $"{Dir}/Wall_{Styles[s]}_{v}_Mesh.asset");
                    NatureAssets.SavePrefab(WallPrefab(s, v), mesh, new[] { mats[s] }, go => go.AddComponent<MeshCollider>().sharedMesh = mesh);
                    sb.AppendLine($"Wall_{Styles[s]}_{v} {mesh.triangles.Length / 3}");
                }
            for (int v = 0; v < Variants; v++)
            {
                var kit = new NatureMeshKit(1);
                int seed = 700 + v * 13;
                PropKit.Lump(kit, new Vector3(0f, -0.5f, -0.5f), new Vector3(0.52f, 0.52f, 0.52f), 8, 12, 0, seed, 0.07f, 1.6f, float.NaN, null, 0f, 1f,
                    n => PropKit.Strata(n, seed, 2f, 0.03f) + PropKit.Chips(n, seed + 2, 0.1f), p => { if (p.y > 0f) p.y = 0f; return p; }, 6f);
                var mesh = NatureAssets.SaveMesh(kit.ToMesh($"StepBlock_{v}_Mesh"), $"{Dir}/StepBlock_{v}_Mesh.asset");
                NatureAssets.SavePrefab(BlockPrefab(v), mesh, new[] { mats[0] }, go =>
                {
                    var c = go.AddComponent<BoxCollider>();
                    c.center = new Vector3(0f, -0.5f, -0.5f); c.size = Vector3.one;
                });
                sb.AppendLine($"StepBlock_{v} {kit.TriangleCount}");
            }
            AssetDatabase.SaveAssets();
            return sb.ToString();
        }

        // ------------------------------------------------------------------ the wall parts
        static float Hash(int seed, int i) => NatureAssets.Hash(seed, i);
        static float R(int seed, int i, float a, float b) => Mathf.Lerp(a, b, Hash(seed, i));

        /// <summary>How far the face stands out from the wall line (m) at x (across, -3..3) and y (up, -0.6..10).</summary>
        delegate float Relief(float x, float y);

        static Relief BlockRelief(int seed)
        {
            // big flat-faced lumps: each a flat top with sloped sides (a frustum), tilted a little; the face is the highest of them
            var b = new List<float[]>();
            for (int i = 0; i < 18; i++)
                b.Add(new[] { R(seed, i * 7, -3.6f, 3.6f), R(seed, i * 7 + 1, -0.5f, 10.4f), R(seed, i * 7 + 2, 0.9f, 2.2f), R(seed, i * 7 + 3, 0.8f, 2.4f),
                              R(seed, i * 7 + 4, 0.25f, 0.9f), R(seed, i * 7 + 5, -0.12f, 0.12f), R(seed, i * 7 + 6, -0.1f, 0.1f) });
            return (x, y) =>
            {
                float best = 0f;
                foreach (var k in b)
                {
                    float dx = x - k[0], dy = y - k[1];
                    float m = Mathf.Max(Mathf.Abs(dx) / k[2], Mathf.Abs(dy) / k[3], (Mathf.Abs(dx) + Mathf.Abs(dy)) / (0.8f * (k[2] + k[3])));
                    float v = Mathf.Min(k[4] + k[5] * dx + k[6] * dy, k[4] * 2.4f * (1f - m));
                    best = Mathf.Max(best, v);
                }
                return 0.1f + best;
            };
        }

        static Relief StrataRelief(int seed)
        {
            var bottoms = new List<float>(); var outs = new List<float>();
            float y0 = -0.6f;
            for (int i = 0; y0 < 10.6f; i++) { bottoms.Add(y0); outs.Add(R(seed, i, 0.1f, 0.8f)); y0 += R(seed, 100 + i, 0.5f, 1.4f); }
            bottoms.Add(y0);
            return (x, y) =>
            {
                for (int l = 0; l < outs.Count; l++)
                {
                    float wob = 0.22f * Mathf.Sin(x * 0.7f + seed + l * 1.9f);
                    float lo = bottoms[l] + (l == 0 ? 0f : wob), hi = bottoms[l + 1] + 0.22f * Mathf.Sin(x * 0.7f + seed + (l + 1) * 1.9f);
                    if (y > hi && l < outs.Count - 1) continue;
                    float u = Mathf.Clamp01((y - lo) / Mathf.Max(hi - lo, 0.1f));
                    float o = outs[l] + 0.25f * (Mathf.PerlinNoise(x * 0.3f + l * 5.1f, seed * 0.1f) - 0.5f);
                    float shape = u < 0.15f ? 0.55f + 0.45f * u / 0.15f : u > 0.8f ? 1f - 0.5f * (u - 0.8f) / 0.2f : 1f;
                    return 0.1f + Mathf.Max(0f, o) * shape;
                }
                return 0.1f;
            };
        }

        static Relief ColumnRelief(int seed)
        {
            var edges = new List<float>(); var outs = new List<float>(); var tops = new List<float>(); var joints = new List<float[]>();
            float x0 = -3.6f;
            for (int i = 0; x0 < 3.6f; i++)
            {
                edges.Add(x0); outs.Add(R(seed, i, 0.15f, 0.65f)); tops.Add(Hash(seed, 50 + i) < 0.35f ? R(seed, 60 + i, 6.5f, 9.5f) : 11f);
                joints.Add(new[] { R(seed, 70 + i, 1.5f, 3.5f), R(seed, 80 + i, 4.5f, 6.5f), R(seed, 90 + i, 7.5f, 9.5f) });
                x0 += R(seed, 200 + i, 0.55f, 1.0f);
            }
            edges.Add(x0);
            return (x, y) =>
            {
                int c = 0;
                while (c < outs.Count - 1 && x > edges[c + 1]) c++;
                float w = edges[c + 1] - edges[c], u = (x - edges[c]) / w;
                float o = outs[c] - 0.28f * w * Mathf.Abs(2f * u - 1f);
                if (y > tops[c]) o = 0.05f;                                   // a broken column: its head is lower
                foreach (var j in joints[c]) if (Mathf.Abs(y - j) < 0.12f) o -= 0.06f;
                return 0.1f + Mathf.Max(0f, o);
            };
        }

        static Mesh WallMesh(int style, int variant, string name)
        {
            int seed = 100 * style + 17 * variant + 3;
            Relief relief = style == 0 ? BlockRelief(seed) : style == 1 ? StrataRelief(seed) : ColumnRelief(seed);
            float dx = style == 0 ? 0.4f : style == 1 ? 0.5f : 0.15f, dy = style == 0 ? 0.4f : style == 1 ? 0.2f : 0.6f;
            int nx = Mathf.CeilToInt(W / dx), ny = Mathf.CeilToInt((Hn + 0.6f) / dy);
            var m = new SimpleMesh(1);
            var grid = new int[nx + 1, ny + 1];
            for (int j = 0; j <= ny; j++)
                for (int i = 0; i <= nx; i++)
                {
                    float x = -W / 2f + W * i / nx, y = -0.6f + (Hn + 0.6f) * j / ny;
                    float z = Margin - Run * Mathf.Clamp01(y / Hn) + relief(x, y) + 0.04f * (Mathf.PerlinNoise(x * 1.3f + seed, y * 1.3f) - 0.5f);
                    grid[i, j] = m.V(new Vector3(x, y, z));
                }
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                    m.Quad(0, grid[i, j], grid[i + 1, j], grid[i + 1, j + 1], grid[i, j + 1], Vector3.forward);
            // the flat top, back into the hill
            float zb = -Run - Back;
            for (int i = 0; i < nx; i++)
            {
                var a = m.Pos(grid[i, ny]); var b = m.Pos(grid[i + 1, ny]);
                m.Quad(0, grid[i, ny], grid[i + 1, ny], m.V(new Vector3(b.x, Hn, zb)), m.V(new Vector3(a.x, Hn, zb)), Vector3.up);
            }
            // the two ends
            foreach (int i in new[] { 0, nx })
            {
                var want = i == 0 ? Vector3.left : Vector3.right;
                for (int j = 0; j < ny; j++)
                {
                    var a = m.Pos(grid[i, j]); var b = m.Pos(grid[i, j + 1]);
                    m.Quad(0, grid[i, j], grid[i, j + 1], m.V(new Vector3(b.x, b.y, zb)), m.V(new Vector3(a.x, a.y, zb)), want);
                }
            }
            return m.ToMesh(name);
        }
    }

    /// <summary>
    /// A plain mesh builder that turns every triangle to face the way it is asked to (so the winding never has to be
    /// worked out by hand). Vertex colour black (no wind), uv zero, as the Slug/Toon shader expects of rocks.
    /// </summary>
    public class SimpleMesh
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<int>[] _t;
        public SimpleMesh(int subs) { _t = new List<int>[subs]; for (int i = 0; i < subs; i++) _t[i] = new List<int>(); }
        public int V(Vector3 p) { _v.Add(p); return _v.Count - 1; }
        public Vector3 Pos(int i) => _v[i];
        public int Count => _v.Count;
        public void Tri(int s, int a, int b, int c, Vector3 want)
        {
            var n = Vector3.Cross(_v[b] - _v[a], _v[c] - _v[a]);
            if (n.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(n, want) >= 0f) { _t[s].Add(a); _t[s].Add(b); _t[s].Add(c); }
            else { _t[s].Add(a); _t[s].Add(c); _t[s].Add(b); }
        }
        public void Quad(int s, int a, int b, int c, int d, Vector3 want) { Tri(s, a, b, c, want); Tri(s, a, c, d, want); }
        /// <summary>A triangle from three points (new vertices).</summary>
        public void Tri(int s, Vector3 a, Vector3 b, Vector3 c, Vector3 want) => Tri(s, V(a), V(b), V(c), want);
        /// <summary>A quad from four points (new vertices).</summary>
        public void Quad(int s, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 want) => Quad(s, V(a), V(b), V(c), V(d), want);
        /// <summary>A box between two corners in a frame (o origin, ax/ay/az unit axes), all six sides but the bottom.</summary>
        public void Box(int s, Vector3 o, Vector3 ax, Vector3 ay, Vector3 az, Vector3 min, Vector3 max, bool bottom = false)
        {
            Vector3 P(float x, float y, float z) => o + ax * x + ay * y + az * z;
            Quad(s, P(min.x, max.y, min.z), P(max.x, max.y, min.z), P(max.x, max.y, max.z), P(min.x, max.y, max.z), ay);
            if (bottom) Quad(s, P(min.x, min.y, min.z), P(max.x, min.y, min.z), P(max.x, min.y, max.z), P(min.x, min.y, max.z), -ay);
            Quad(s, P(min.x, min.y, min.z), P(max.x, min.y, min.z), P(max.x, max.y, min.z), P(min.x, max.y, min.z), -az);
            Quad(s, P(min.x, min.y, max.z), P(max.x, min.y, max.z), P(max.x, max.y, max.z), P(min.x, max.y, max.z), az);
            Quad(s, P(min.x, min.y, min.z), P(min.x, max.y, min.z), P(min.x, max.y, max.z), P(min.x, min.y, max.z), -ax);
            Quad(s, P(max.x, min.y, min.z), P(max.x, max.y, min.z), P(max.x, max.y, max.z), P(max.x, min.y, max.z), ax);
        }
        public int TriangleCount { get { int n = 0; foreach (var t in _t) n += t.Count / 3; return n; } }
        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (_v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(_v);
            var uv = new Vector2[_v.Count]; var col = new Color[_v.Count];
            for (int i = 0; i < col.Length; i++) col[i] = new Color(0f, 0f, 0f, 1f);
            m.uv = uv; m.colors = col;
            m.subMeshCount = _t.Length;
            for (int i = 0; i < _t.Length; i++) m.SetTriangles(_t[i], i);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
