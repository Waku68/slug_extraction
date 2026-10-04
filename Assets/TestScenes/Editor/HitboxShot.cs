using UnityEngine;

namespace SlugMove
{
    /// <summary>Debug (play mode): renders a view twice, as it is and with every hitbox drawn solid on top, side by side.</summary>
    public static class HitboxShot
    {
        public static string Capture(string path, Vector3 position, Vector3 lookAt, float fov = 30f, int width = 700, int height = 500)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            ShotUtil.Capture(System.IO.Path.Combine(dir, name + "_body.png"), position, lookAt, fov, width, height);
            var shown = new System.Collections.Generic.List<GameObject>();
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            foreach (var h in Object.FindObjectsByType<SlugHitbox>())
            {
                var col = h.GetComponent<Collider>();
                GameObject g;
                if (col is CapsuleCollider c)
                {
                    g = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    g.transform.SetPositionAndRotation(h.transform.position, h.transform.rotation);
                    g.transform.localScale = new Vector3(c.radius * 2f, c.height * 0.5f, c.radius * 2f);
                }
                else
                {
                    var b = (BoxCollider)col;
                    g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    g.transform.SetPositionAndRotation(h.transform.position, h.transform.rotation);
                    g.transform.localScale = b.size;
                }
                Object.DestroyImmediate(g.GetComponent<Collider>());
                var m = new Material(unlit);
                m.SetColor("_BaseColor", h.Armoured ? new Color(0.3f, 0.55f, 1f) : h.part == "触角" ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.85f, 0.2f));
                g.GetComponent<Renderer>().sharedMaterial = m;
                shown.Add(g);
            }
            ShotUtil.Capture(System.IO.Path.Combine(dir, name + "_boxes.png"), position, lookAt, fov, width, height);
            foreach (var g in shown) Object.DestroyImmediate(g);
            return shown.Count + " hitboxes";
        }
    }
}
