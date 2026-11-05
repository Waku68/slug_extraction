using UnityEngine;

namespace SlugWorld
{
    /// <summary>What the ground under the slug is, for how fast it dries (spec 4章: "光って濡れて見える地面は乾かない、ツヤのない
    /// 乾いた地面は乾く、真っ白な塩はとても乾く").</summary>
    public enum GroundKind { Soil, Sand, Salt, Wet }

    /// <summary>
    /// Reads the kind of ground at a point. Moisture zones (water, salt) come first; then the terrain's layers, by name
    /// (spec 4章 Oct 10: the kind lives on the terrain's layers, nothing new is painted). Off the terrain (in a cave, on a
    /// rock) it is soil. The names: salt → salt; water, wet, mud, moss → wet; desert, sand, lava, canyon, red → sand;
    /// anything else (forest, low ground, rock, path, gravel, the ring) → soil.
    /// </summary>
    public static class GroundKinds
    {
        /// <summary>How far under the terrain's surface a point still counts as on it (below that it is in a cave).</summary>
        const float BelowSurface = 1.5f;

        public static GroundKind At(Vector3 p)
        {
            if (MoistureZone.Any(MoistureZone.Kind.Water, p)) return GroundKind.Wet;
            if (MoistureZone.Any(MoistureZone.Kind.Salt, p)) return GroundKind.Salt;
            foreach (var t in Terrain.activeTerrains)
                if (OnTerrain(t, p, out var kind)) return kind;
            return GroundKind.Soil;
        }

        static bool OnTerrain(Terrain t, Vector3 p, out GroundKind kind)
        {
            kind = GroundKind.Soil;
            if (!Alphas(t, p, out var layers, out var a)) return Over(t, p);
            var weight = new float[4];
            for (int l = 0; l < layers.Length && l < a.GetLength(2); l++)
                weight[(int)KindOf(layers[l])] += a[0, 0, l];
            int best = 0;
            for (int k = 1; k < weight.Length; k++) if (weight[k] > weight[best]) best = k;
            kind = (GroundKind)best;
            return true;
        }

        /// <summary>The name (lower case) of the terrain layer most painted at a point, or null off the terrain or in a
        /// cave under it (footsteps tell grass from rock by it).</summary>
        public static string LayerNameAt(Vector3 p)
        {
            foreach (var t in Terrain.activeTerrains)
            {
                if (!Alphas(t, p, out var layers, out var a)) continue;
                int best = 0;
                for (int l = 1; l < layers.Length && l < a.GetLength(2); l++) if (a[0, 0, l] > a[0, 0, best]) best = l;
                return layers[best] ? layers[best].name.ToLowerInvariant() : "";
            }
            return null;
        }

        /// <summary>Over the terrain's square (then under it, in a cave, or on it).</summary>
        static bool Over(Terrain t, Vector3 p)
        {
            var data = t ? t.terrainData : null;
            if (!data) return false;
            Vector3 local = p - t.transform.position;
            return local.x >= 0f && local.z >= 0f && local.x <= data.size.x && local.z <= data.size.z;
        }

        /// <summary>The painted layers at a point on the terrain's surface (false off it, in a cave under it, or unpainted).</summary>
        static bool Alphas(Terrain t, Vector3 p, out TerrainLayer[] layers, out float[,,] a)
        {
            layers = null; a = null;
            if (!Over(t, p)) return false;
            var data = t.terrainData;
            if (p.y < t.SampleHeight(p) + t.transform.position.y - BelowSurface) return false;   // under it: a cave
            layers = data.terrainLayers;
            if (layers == null || layers.Length == 0) return false;
            Vector3 local = p - t.transform.position;
            float u = local.x / data.size.x, v = local.z / data.size.z;
            int res = data.alphamapResolution;
            int ax = Mathf.Clamp(Mathf.RoundToInt(u * (res - 1)), 0, res - 1), az = Mathf.Clamp(Mathf.RoundToInt(v * (res - 1)), 0, res - 1);
            a = data.GetAlphamaps(ax, az, 1, 1);
            return true;
        }

        /// <summary>The kind a terrain layer stands for, by its name.</summary>
        public static GroundKind KindOf(TerrainLayer layer)
        {
            string n = layer ? layer.name.ToLowerInvariant() : "";
            if (n.Contains("salt")) return GroundKind.Salt;
            if (n.Contains("water") || n.Contains("wet") || n.Contains("mud") || n.Contains("moss")) return GroundKind.Wet;
            if (n.Contains("desert") || n.Contains("sand") || n.Contains("lava") || n.Contains("canyon") || n.Contains("red")) return GroundKind.Sand;
            return GroundKind.Soil;
        }

        /// <summary>The kind's name, for the development display.</summary>
        public static string Name(GroundKind k) => k == GroundKind.Sand ? "砂・黒い溶岩" : k == GroundKind.Salt ? "塩" : k == GroundKind.Wet ? "濡れた地面" : "土・草";
    }
}
