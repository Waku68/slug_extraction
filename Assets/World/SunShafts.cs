using System.Collections.Generic;
using UnityEngine;

namespace SlugWorld
{
    /// <summary>
    /// Shafts of sunlight standing in the air on a sunny day (Oct 26 わくさん: the sun dries the slug and drying kills, so
    /// show where it falls). Columns of light lean from the sunlit ground up toward the sun, around the eyes: most of them
    /// at the edges of shade (through gaps in the leaves, past rocks), some out in the open, brighter and more of them on
    /// salt. In the shade there are none, so the line where they stop is the line of the shade, readable from far off.
    /// Only on a sunny day out of dawn and dusk; everyone sees these. Since Nov 3 this is the only sign of the sun (the
    /// light on the player's own view, SunGlare, got in the way of seeing).
    /// Each shaft fades in, stands a while and fades out, so they shift as the sun and the eyes move. One mesh, rebuilt each
    /// frame (a few dozen quads turned to face the camera). Added to the weather by <see cref="WeatherPresenter"/>.
    /// </summary>
    public class SunShafts : MonoBehaviour
    {
        [Tooltip("一度に立つ光の筋の数（いちばん多いとき）")] public int count = 50;
        [Tooltip("目からの距離（m）：近い・遠い")] public Vector2 range = new Vector2(3f, 45f);
        [Tooltip("1秒に新しく探す数")] public float triesPerSecond = 70f;
        [Tooltip("影のふちでない、開けた日なたにも立てる割合")] public float openChance = 0.15f;
        [Tooltip("筋の高さ（m、太陽に向かう長さ）")] public Vector2 height = new Vector2(5f, 14f);
        [Tooltip("筋の太さ（m）")] public Vector2 width = new Vector2(0.25f, 1.0f);
        [Tooltip("1本が立っている時間（秒）")] public Vector2 life = new Vector2(5f, 11f);
        [Tooltip("出る・消える時間（秒）")] public float fade = 1.6f;
        [Tooltip("強さ：土や草の日なた・塩の上")] public float soilStrength = 0.55f, saltStrength = 1f;
        [Tooltip("全体の明るさ")] public float intensity = 0.2f;
        public Color color = new Color(1f, 0.93f, 0.7f, 1f);

        struct Shaft
        {
            public Vector3 ground;
            public float height, width, strength, born, life, seed;
        }

        readonly List<Shaft> _shafts = new List<Shaft>();
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Vector2> _uvs = new List<Vector2>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<int> _tris = new List<int>();
        Mesh _mesh;
        Material _material;
        GameObject _holder;
        float _tries, _on;
        int _mask;

        void Start()
        {
            var shader = Shader.Find("Slug/SunShaft");
            if (!shader) { enabled = false; return; }
            _material = new Material(shader) { hideFlags = HideFlags.DontSave };
            _mesh = new Mesh { name = "SunShafts", hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
            _holder = new GameObject("_SunShafts") { hideFlags = HideFlags.DontSave };
            _holder.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var r = _holder.AddComponent<MeshRenderer>();
            r.sharedMaterial = _material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _mask = SlugMove.SlugHitboxes.WorldMask;
        }

        void OnDestroy()
        {
            if (_holder) Destroy(_holder);
            if (_mesh) Destroy(_mesh);
            if (_material) Destroy(_material);
        }

        void LateUpdate()
        {
            var weather = WeatherPresenter.Current;
            var cam = Camera.main;
            if (!weather || !cam) return;
            var t = weather.Time;
            bool sunny = weather.Weather.sky == Sky.Sunny && t.daylightAmount > 0.5f;
            float target = sunny ? 1f - t.twilight : 0f;
            _on = Mathf.MoveTowards(_on, target, Time.deltaTime / 2f);
            Vector3 toSun = -t.sunDirection.normalized;
            float now = Time.time;

            // old shafts go; new ones are looked for while the sun is out
            _shafts.RemoveAll(s => now > s.born + s.life);
            if (_on > 0.01f && toSun.y > 0.05f)
            {
                _tries += triesPerSecond * Time.deltaTime;
                for (; _tries >= 1f; _tries -= 1f)
                    if (_shafts.Count < count && TryPlace(cam.transform, toSun, out var s)) _shafts.Add(s);
            }
            else _tries = 0f;

            Build(cam, toSun, now);
            _material.SetColor("_Color", color);
            _material.SetFloat("_Intensity", intensity * _on);
        }

        bool TryPlace(Transform eye, Vector3 toSun, out Shaft shaft)
        {
            shaft = default;
            // mostly ahead of the eyes (that is where they are seen), at any distance in the range
            Vector3 fwd = Vector3.ProjectOnPlane(eye.forward, Vector3.up);
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            float ang = Random.Range(-75f, 75f) * (Random.value < 0.8f ? 1f : 2.3f);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd.normalized;
            float dist = Mathf.Lerp(range.x, range.y, Mathf.Pow(Random.value, 0.7f));
            if (!Ground(eye.position + dir * dist, eye.position.y, out var g)) return false;
            if ( WeatherPresenter.Current && WeatherPresenter.Current.WeatherAt(WeatherPresenter.Current.ShownHours, g).sky != Sky.Sunny) return false;
            if (!Sunlit(g, toSun)) return false;

            // the edge of the shade: a neighbour in shade
            bool edge = false;
            for (int i = 0; i < 4 && !edge; i++)
            {
                Vector3 side = Quaternion.AngleAxis(i * 90f + Random.Range(0f, 90f), Vector3.up) * Vector3.forward * Random.Range(0.8f, 2f);
                if (Ground(g + side, g.y, out var n) && !Sunlit(n, toSun)) edge = true;
            }
            if (!edge && Random.value > openChance) return false;

            var kind = GroundKinds.At(g);
            if (kind == GroundKind.Wet) return false;                  // wet ground does not dry: no light on it
            bool salt = kind == GroundKind.Salt;
            if (!salt && !edge && Random.value > 0.6f) return false;
            shaft = new Shaft
            {
                ground = g,
                height = Random.Range(height.x, height.y),
                width = Mathf.Lerp(width.x, width.y, Mathf.Pow(Random.value, 1.5f)),
                strength = (salt ? saltStrength : soilStrength) * Random.Range(0.7f, 1f),
                born = Time.time,
                life = Random.Range(life.x, life.y),
                seed = Random.value,
            };
            return true;
        }

        /// <summary>The ground under a point: the terrain where there is one (not the tree tops), else the lowest surface.</summary>
        bool Ground(Vector3 p, float nearY, out Vector3 ground)
        {
            ground = p;
            foreach (var terrain in Terrain.activeTerrains)
            {
                var o = terrain.GetPosition();
                var size = terrain.terrainData.size;
                if (p.x < o.x || p.z < o.z || p.x > o.x + size.x || p.z > o.z + size.z) continue;
                ground = new Vector3(p.x, o.y + terrain.SampleHeight(p), p.z);
                return true;
            }
            var hits = Physics.RaycastAll(new Vector3(p.x, nearY + 60f, p.z), Vector3.down, 120f, _mask, QueryTriggerInteraction.Ignore);
            float lowest = float.MaxValue;
            foreach (var h in hits)
                if (h.normal.y > 0.5f && h.point.y < lowest) { lowest = h.point.y; ground = h.point; }
            return lowest < float.MaxValue;
        }

        bool Sunlit(Vector3 ground, Vector3 toSun) =>
            !Physics.Raycast(ground + Vector3.up * 0.3f, toSun, 300f, _mask, QueryTriggerInteraction.Ignore);

        void Build(Camera cam, Vector3 toSun, float now)
        {
            _verts.Clear(); _uvs.Clear(); _colors.Clear(); _tris.Clear();
            Vector3 eye = cam.transform.position;
            foreach (var s in _shafts)
            {
                float age = now - s.born;
                float a = Mathf.Clamp01(age / fade) * Mathf.Clamp01((s.life - age) / fade);
                Vector3 top = s.ground + toSun * s.height;
                Vector3 mid = (s.ground + top) * 0.5f;
                float d = Vector3.Distance(eye, mid);
                // not in the eyes' face, and thinning out far away
                a *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.5f, 4f, d)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(range.y + 10f, range.y - 10f, d));
                if (a <= 0.002f) continue;
                Vector3 side = Vector3.Cross(toSun, eye - mid);
                if (side.sqrMagnitude < 1e-6f) continue;
                side = side.normalized * (s.width * 0.5f);
                int b = _verts.Count;
                _verts.Add(s.ground - side); _verts.Add(s.ground + side);
                _verts.Add(top - side * 1.4f); _verts.Add(top + side * 1.4f);
                _uvs.Add(new Vector2(0f, 0f)); _uvs.Add(new Vector2(1f, 0f));
                _uvs.Add(new Vector2(0f, 1f)); _uvs.Add(new Vector2(1f, 1f));
                var c = new Color(1f, s.seed, 1f, a * s.strength);
                _colors.Add(c); _colors.Add(c); _colors.Add(c); _colors.Add(c);
                _tris.Add(b); _tris.Add(b + 2); _tris.Add(b + 1);
                _tris.Add(b + 1); _tris.Add(b + 2); _tris.Add(b + 3);
            }
            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_tris, 0);
            _mesh.bounds = new Bounds(eye, Vector3.one * (range.y * 2f + 40f));
        }
    }
}
