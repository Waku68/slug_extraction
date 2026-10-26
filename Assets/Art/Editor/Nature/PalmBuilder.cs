using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 立っているヤシ（Oct 20、わくさんの「C ずんぐり」を見本に、もっとずんぐり・特徴は濃く・シンプルに）。
    /// メニュー Slug/Nature/Build Palm で、なめらか版とカクカク版のメッシュ・マテリアル・プレハブを作り直す。
    ///
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - 幹：高さ 4.0 m（人なら約 14 m）、根元の半径 0.55 m・てっぺん 0.32 m の太いとっくり形。少し弓なり（0.5 m）。
    ///   節は 7 段の大きなドーナツ（ふくらみ ×1.12、くびれ ×0.84）で、くびれの帯だけ暗い色。根元は大きく広がる。
    /// - 葉：7 枚、長さ 3.0 m・片側の幅 0.55 m・厚み 6 cm。上へ出てから先が大きく垂れる。
    ///   ふちは丸い切れ込みが片側 5 つ（羽のような葉の特徴は残し、ぎざぎざは丸く大きく）。V 字に少し折れる。
    /// - 実：本物の大きさ（直径 約 28 cm、Oct 20 わくさん）を 6 個、葉の付け根の下に房で。
    /// 色：幹 #B0A28A、節のくびれ #7A6C5A、葉 #788A50、実 #6A5A3A、葉の付け根 #8A8A55。
    /// </summary>
    public static class PalmBuilder
    {
        const string Dir = "Assets/Art/Nature/Palm";
        const int TRUNK = 0, RING = 1, LEAF = 2, NUT = 3, CROWN = 4;

        // 形の数字（ここを変えて作り直す）
        const float TrunkH = 4.0f, RBase = 0.55f, RTop = 0.32f, Lean = 0.5f, Flare = 0.9f;
        const int Nodes = 7;
        const float Bulge = 1.12f, Pinch = 0.84f;
        const int Fronds = 7;
        const float FrondLen = 3.0f, FrondWidth = 0.55f, FrondThick = 0.06f, Droop = 0.75f, Fold = 0.35f;
        const int Lobes = 5;
        const int Nuts = 6;
        const float NutR = 0.14f;

        [MenuItem("Slug/Nature/Build Palm")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var mats = Materials();
            var smooth = Make("Palm_Smooth", 18, 4, 26, 5, false, mats);
            var facet = Make("Palm_Facet", 8, 2, 10, 3, true, mats);
            AssetDatabase.SaveAssets();
            Debug.Log($"Palm built: {smooth} / {facet}");
        }

        static string Make(string name, int sides, int rowsPerNode, int bladeSegs, int nutRings, bool facet, Material[][] mats)
        {
            var kit = new NatureMeshKit(5);
            var rnd = new System.Random(3);

            Vector3 Center(float t) => new Vector3(Lean * Mathf.Pow(Mathf.Max(0f, t), 1.7f), TrunkH * t, 0f);
            float Radius(float t) => Mathf.Lerp(RBase, RTop, Mathf.Pow(Mathf.Max(0f, t), 0.8f)) * (1f + Flare * Mathf.Exp(-Mathf.Max(0f, t) * TrunkH / 0.45f));

            // 幹の行：(t, 倍率, 帯のマテリアル)。節ごとに くびれ → ふくらみ → くびれ、くびれの細い帯は暗い色
            var rows = new List<Vector3> { new Vector3(-0.5f / TrunkH, 1f, TRUNK) };
            for (int k = 0; k < Nodes; k++)
            {
                float t0 = k / (float)Nodes, t1 = (k + 1) / (float)Nodes, len = t1 - t0;
                rows.Add(new Vector3(t0 + len * 0.12f, Pinch, k == 0 ? TRUNK : RING));
                for (int i = 1; i <= rowsPerNode; i++)
                {
                    float f = 0.12f + 0.88f * i / (rowsPerNode + 1);
                    float b = Mathf.Sin(Mathf.PI * (f - 0.12f) / 0.88f);
                    rows.Add(new Vector3(t0 + len * f, Mathf.Lerp(Pinch, Bulge, Mathf.Pow(b, 0.6f)), TRUNK));
                }
                rows.Add(new Vector3(t1, Pinch, TRUNK));
            }
            kit.BeginTube();
            kit.Tube(Center, Radius, rows, sides, TRUNK, -1, 0.4f);

            // 葉の付け根（ふくらんだ鞘）
            Vector3 top = Center(1f);
            var crownRows = new List<Vector3> { new Vector3(0f, 1f, CROWN), new Vector3(0.4f, 1.0f, CROWN), new Vector3(0.75f, 0.75f, CROWN), new Vector3(1f, 0.25f, CROWN) };
            kit.BeginTube();
            kit.Tube(t => top + Vector3.up * (t * 0.6f), t => RTop * Pinch * (1f + 0.6f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 1.4f))), crownRows, sides, -1, CROWN);
            Vector3 frondBase = top + Vector3.up * 0.42f;

            // 葉
            float y0 = (float)rnd.NextDouble() * Mathf.PI * 2f;
            for (int f = 0; f < Fronds; f++)
            {
                float yaw = y0 + Mathf.PI * 2f * f / Fronds + ((float)rnd.NextDouble() - 0.5f) * 0.25f;
                float pitch = Mathf.Deg2Rad * Mathf.Lerp(18f, 42f, (float)rnd.NextDouble());
                float len = FrondLen * Mathf.Lerp(0.9f, 1.08f, (float)rnd.NextDouble());
                float droop = Droop * Mathf.Lerp(0.85f, 1.15f, (float)rnd.NextDouble());
                var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                var d0 = h * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
                Vector3 Path(float s) => frondBase + d0 * len * s + Vector3.down * (droop * len * s * s);
                float Width(float s)
                {
                    float body = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, s * 1.02f))), 0.55f) * (1f - 0.3f * s);
                    float lobe = 0.62f + 0.38f * Mathf.Abs(Mathf.Sin(Mathf.PI * Lobes * s));   // 丸い切れ込み
                    return FrondWidth * body * (s < 0.08f ? 0.4f + 7.5f * s : lobe);
                }
                kit.Blade(Path, Width, bladeSegs, FrondThick, Fold, LEAF);
            }

            // 実（本物の大きさ）
            for (int i = 0; i < Nuts; i++)
            {
                float a = y0 + 0.5f + (i % 3 - 1) * 0.45f + (i / 3) * Mathf.PI;
                float rr = RTop * 1.15f + NutR + 0.04f * (i % 2);
                var c = top + new Vector3(Mathf.Cos(a) * rr, 0.05f - 0.16f * (i % 2), Mathf.Sin(a) * rr);
                kit.Ball(c, new Vector3(NutR, NutR * 1.12f, NutR), nutRings, NUT);
            }

            var mesh = kit.ToMesh(name);
            string meshPath = $"{Dir}/{name}_Mesh.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (old != null) { old.Clear(); EditorUtility.CopySerialized(mesh, old); mesh = old; }
            else AssetDatabase.CreateAsset(mesh, meshPath);

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats[facet ? 1 : 0];
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(Lean * 0.4f, TrunkH * 0.5f, 0f); col.height = TrunkH; col.radius = RBase * 0.9f;
            PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return $"{name} {kit.TriangleCount} tris";
        }

        /// <summary>[0] なめらか（_Facet 0）、[1] カクカク（_Facet 1）。</summary>
        static Material[][] Materials()
        {
            var cols = new[] { "#B0A28A", "#7A6C5A", "#788A50", "#6A5A3A", "#8A8A55" };
            var names = new[] { "Trunk", "TrunkRing", "Leaf", "Nut", "Crown" };
            var res = new Material[2][];
            for (int v = 0; v < 2; v++)
            {
                res[v] = new Material[5];
                for (int i = 0; i < 5; i++)
                {
                    ColorUtility.TryParseHtmlString(cols[i], out var c);
                    var m = WorldLook.GetOrCreate($"{Dir}/Palm_{names[i]}{(v == 0 ? "" : "_Facet")}.mat");
                    WorldLook.Apply(m, c, 0.15f, 0f, null, 10f, 0f, 60f, i == LEAF);
                    m.SetFloat("_Facet", v == 0 ? 0f : 1f);
                    res[v][i] = m;
                }
            }
            return res;
        }
    }
}
