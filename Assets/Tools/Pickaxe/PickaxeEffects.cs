using UnityEngine;

namespace SlugMove
{
    public static class PickaxeEffects
    {
        /// <summary>Small cubes that pop out of a hit point and vanish. They are on Ignore Raycast, so they never block anything.</summary>
        public static void Chips(Vector3 point, Vector3 normal, Color color, int count, float speed = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = "Chip";
                c.layer = 2;
                c.transform.position = point + normal * 0.03f;
                c.transform.rotation = Random.rotation;
                c.transform.localScale = Vector3.one * Random.Range(0.02f, 0.045f);
                var r = c.GetComponent<Renderer>();
                r.material.SetColor("_BaseColor", color);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var rb = c.AddComponent<Rigidbody>();
                rb.mass = 0.02f;
                rb.linearVelocity = (normal + Random.insideUnitSphere * 0.8f + Vector3.up * 0.8f).normalized * Random.Range(1.2f, 2.4f) * speed;
                rb.angularVelocity = Random.insideUnitSphere * 10f;
                Object.Destroy(c, 0.9f);
            }
        }
    }
}
