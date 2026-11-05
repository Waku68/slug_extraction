using UnityEngine;

namespace SlugWorld
{
    /// <summary>
    /// Tells every shader what the island's ground looks like from above (the toon ground's colour picture), so rocks can
    /// wear the ground of their place on the faces that look up (Slug/Toon _TopCoat; docs/ideas/cairn_terrain_ideas.md, G).
    /// Sits on the terrain; the picture is baked by IslandToonGround (Slug/IslandTest 800 m/Cairn Try).
    /// </summary>
    [ExecuteAlways]
    public class IslandGroundMap : MonoBehaviour
    {
        [Tooltip("島を上から見た地面の色（IslandToonGround が焼く）")] public Texture2D colours;

        static readonly int GroundId = Shader.PropertyToID("_SlugGround");
        static readonly int RectId = Shader.PropertyToID("_SlugGroundRect");

        void OnEnable() => Apply();
        void OnValidate() => Apply();

        public void Apply()
        {
            var t = GetComponent<Terrain>();
            if (!colours || !t || !t.terrainData) { Shader.SetGlobalVector(RectId, Vector4.zero); return; }
            Vector3 p = t.transform.position, s = t.terrainData.size;
            Shader.SetGlobalTexture(GroundId, colours);
            Shader.SetGlobalVector(RectId, new Vector4(p.x, p.z, 1f / s.x, 1f));
        }

        void OnDisable() => Shader.SetGlobalVector(RectId, Vector4.zero);
    }
}
