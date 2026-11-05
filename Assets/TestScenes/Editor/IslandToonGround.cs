using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The island's ground in the toon look (docs/ideas/cairn_terrain_ideas.md, B and F). Bakes one picture of the island
    /// seen from above from the terrain's layers (the layer colours mixed by their weights) and draws the terrain with
    /// Slug/ToonTerrain, which turns ground steeper than 50° into bare rock with strata. The picture also gets the plant
    /// bands of the terraces: a flat place whose edge drops away steeply grows a band of plants along that edge, so the
    /// steps read from afar (Cairn: trees only on the ledges). On the pale salt and rock the band is a red salt plant
    /// (like glasswort), on green ground a deep green, on red and sand an olive scrub.
    /// Off puts the old Lit ground back. Only the open scene changes; IslandTest800Builder's Build Scene puts the Lit back.
    /// </summary>
    public static class IslandToonGround
    {
        const string TexPath = MoveTestBuilder.GenDir + "/IslandToonGround.png";
        const string MatPath = MoveTestBuilder.GenDir + "/IslandToonGround.mat";
        const string LitPath = MoveTestBuilder.GenDir + "/RT_TerrainMat.mat";

        const float FlatBelow = 14f;     // a terrace is flatter than this (degrees)
        const float EdgeReach = 3.5f;    // the band lies within this of the edge (m)
        const float EdgeDrop = 1.2f;     // ... where the ground falls at least this much (m)
        const float EdgeSteep = 32f;     // ... down a slope at least this steep (degrees)

        const float RockDarken = 0.6f;   // a pale cliff face is this much of its ground colour
        const float RockCool = 0.35f;    // ... and this much towards the shade colour

        static readonly Color SaltPlant = new Color(0.63f, 0.27f, 0.23f);
        static readonly Color GreenPlant = new Color(0.24f, 0.36f, 0.19f);
        static readonly Color ScrubPlant = new Color(0.47f, 0.46f, 0.28f);

        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Toon Ground On")]
        public static string On()
        {
            var t = Object.FindAnyObjectByType<Terrain>();
            if (!t) return "open IslandTest_800 first";
            var tex = Bake(t.terrainData);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (!mat) { mat = new Material(Shader.Find("Slug/ToonTerrain")); AssetDatabase.CreateAsset(mat, MatPath); }
            mat.shader = Shader.Find("Slug/ToonTerrain");
            mat.SetTexture("_IslandColor", tex);
            mat.SetFloat("_SteepAngle", 34f);
            mat.SetFloat("_StrataAmount", 0.16f);
            mat.SetFloat("_StrataPeriod", 3.6f);
            mat.SetFloat("_LedgeLine", 0.42f);
            mat.SetFloat("_CrackAmount", 0.55f);
            mat.SetFloat("_PatchAmount", 0.42f);
            mat.SetFloat("_SpeckAmount", 0.32f);
            mat.SetFloat("_RockDarken", RockDarken);   // Nov 5: walkable tops vs cliffs read apart on the pale salt
            mat.SetFloat("_RockCool", RockCool);
            EditorUtility.SetDirty(mat);
            t.materialTemplate = mat;
            t.drawInstanced = false;
            var map = t.GetComponent<IslandGroundMap>() ?? t.gameObject.AddComponent<IslandGroundMap>();
            map.colours = tex;
            map.Apply();
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
            return $"toon ground on ({tex.width}×{tex.height})";
        }

        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Toon Ground Off")]
        public static string Off()
        {
            var t = Object.FindAnyObjectByType<Terrain>();
            if (!t) return "open IslandTest_800 first";
            t.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>(LitPath);
            var map = t.GetComponent<IslandGroundMap>();
            if (map) Object.DestroyImmediate(map);
            EditorUtility.SetDirty(t);
            return "Lit ground back";
        }

        // the gray island's stand-in things (Lit) in the same toon as the rest: rocks faceted and wearing the ground of
        // their place on top (G), plants smooth with the cool blue-green shade (README §8)
        static readonly string[] Rocks = { "IT_Stone", "IT_LavaRock", "IT_SaltRock", "IT_RedRock", "IT_BlackSalt", "IT_FallRock", "IT_SaltCave", "IT_LavaTube" };
        static readonly string[] Plants = { "W_Bark", "W_Leaves", "IT_Grass", "IT_Shrub", "IT_Driftwood" };

        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Toon Things On")]
        public static string ThingsOn() => SetThings(true);
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Toon Things Off")]
        public static string ThingsOff() => SetThings(false);

        static string SetThings(bool on)
        {
            int n = 0;
            foreach (var name in Rocks) n += SetThing(name, on, true);
            foreach (var name in Plants) n += SetThing(name, on, false);
            AssetDatabase.SaveAssets();
            return $"{n} materials {(on ? "toon" : "Lit")}";
        }

        static int SetThing(string name, bool on, bool rock)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{MoveTestBuilder.GenDir}/{name}.mat");
            if (!m) return 0;
            var col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.gray;
            if (!on) { m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetColor("_BaseColor", col); EditorUtility.SetDirty(m); return 1; }
            m.shader = Shader.Find("Slug/Toon");
            SlugDesign.WorldLook.Apply(m, col, rock ? 0.1f : 0.15f, rock ? 0.35f : 0.15f, col * 0.88f, rock ? 3f : 2f);
            m.SetFloat("_Facet", rock ? 1f : 0f);
            m.SetFloat("_TopCoat", rock ? 0.75f : 0f);
            if (!rock) { m.SetColor("_ShadeColor", new Color(0.5f, 0.62f, 0.74f)); m.SetColor("_OutlineColor", col * 0.4f); }
            EditorUtility.SetDirty(m);
            return 1;
        }

        static Texture2D Bake(TerrainData data)
        {
            int n = data.alphamapResolution, layers = data.alphamapLayers;
            var alpha = data.GetAlphamaps(0, 0, n, n);
            var colors = new Color[layers];
            for (int i = 0; i < layers; i++)
            {
                var d = data.terrainLayers[i].diffuseTexture;
                colors[i] = d ? ReadPixel(d) : Color.gray;
            }

            var px = new Color[n * n];
            float metre = data.size.x / (n - 1f);
            int reach = Mathf.Max(1, Mathf.RoundToInt(EdgeReach / metre));
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    Color c = Color.black;
                    for (int i = 0; i < layers; i++) c += colors[i] * alpha[z, x, i];
                    c.a = 1f;
                    float u = x / (n - 1f), v = z / (n - 1f);
                    // (only along some of the edges, so the bands don't ring every terrace)
                    if (Mathf.PerlinNoise(u * 86f + 3.3f, v * 86f + 7.1f) > 0.48f && data.GetSteepness(u, v) < FlatBelow && OnEdge(data, u, v, reach, n))
                        c = Color.Lerp(c, BandColour(c), 0.85f);
                    px[z * n + x] = c;
                }

            var tex = new Texture2D(n, n, TextureFormat.RGB24, true);
            tex.SetPixels(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(TexPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexPath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(TexPath);
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.sRGBTexture = true;
            imp.isReadable = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
        }

        // the ground falls away steeply within reach (the edge of a terrace)
        static bool OnEdge(TerrainData data, float u, float v, int reach, int n)
        {
            float h = data.GetInterpolatedHeight(u, v);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                float du = Mathf.Cos(a) * reach / (n - 1f), dv = Mathf.Sin(a) * reach / (n - 1f);
                float uu = u + du, vv = v + dv;
                if (uu < 0f || uu > 1f || vv < 0f || vv > 1f) continue;
                if (h - data.GetInterpolatedHeight(uu, vv) > EdgeDrop && data.GetSteepness(uu, vv) > EdgeSteep) return true;
            }
            return false;
        }

        static Color BandColour(Color ground)
        {
            Color.RGBToHSV(ground, out float hue, out float sat, out float val);
            if (sat < 0.18f) return SaltPlant;                                        // salt, pale rock, gravel
            if (ground.g > ground.r && ground.g >= ground.b) return GreenPlant;       // forest, low ground, meadow
            return ScrubPlant;                                                        // red, sand, canyon
        }

        static Color ReadPixel(Texture2D t)
        {
            var path = AssetDatabase.GetAssetPath(t);
            if (t.isReadable || string.IsNullOrEmpty(path) || path.EndsWith(".asset")) return t.GetPixel(0, 0);
            var rt = RenderTexture.GetTemporary(4, 4);
            Graphics.Blit(t, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var r = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            r.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            var c = r.GetPixel(1, 1);
            Object.DestroyImmediate(r);
            return c;
        }
    }
}
