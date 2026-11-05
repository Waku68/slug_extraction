using System.Collections.Generic;
using UnityEngine;

namespace SlugWorld
{
    /// <summary>
    /// Shows the island's moving clouds (300..450 m across) in the sky and the grey columns of rain hanging under
    /// them (spec 4章, docs/ideas/wind_ideas.md #3 & #6): built as wide, monumental Cairn / Mathieu Bablet style
    /// bande-dessinee cumulonimbus masses (sleek horizontal anvil base, stepped lateral wing terraces, sweeping
    /// cauliflower summit domes, 4 crisp gouache tonal bands, and thin comic ink lines on silhouettes and lobe junctions).
    /// Added to the scene by <see cref="WeatherPresenter"/>.
    /// </summary>
    public class RainCloudVisuals : MonoBehaviour
    {
        [Tooltip("雨雲の雲底の高さ (m)")] public float cloudHeight = 118f;
        [Tooltip("雨雲の背の高さの倍率（半径に対する比）")] public float cloudHeightRatio = 0.76f;
        [Tooltip("遠くから見える雨の柱の濃さ")] public float columnOpacity = 0.42f;

        readonly List<RainCloud> _clouds = new List<RainCloud>(4);
        readonly List<Transform> _cloudBodies = new List<Transform>(3);
        readonly List<Material> _cloudMats = new List<Material>(3);
        readonly List<Mesh> _cloudMeshes = new List<Mesh>(3);
        Mesh _columnMesh;
        Material _columnMat;
        GameObject _columnHolder;

        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Vector2> _uvs = new List<Vector2>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<int> _tris = new List<int>();

        void Start() => EnsureBuilt();

        void RemoveOrphanChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var ch = transform.GetChild(i);
                if (ch.name.StartsWith("_RainCloud_") || ch.name == "_RainColumns")
                {
                    if (!_cloudBodies.Contains(ch) && (_columnHolder == null || ch.gameObject != _columnHolder))
                        SafeDestroy(ch.gameObject);
                }
            }
        }

        void EnsureBuilt()
        {
            RemoveOrphanChildren();
            if (_cloudBodies.Count > 0 && _cloudBodies[0] != null) return;
            _cloudBodies.Clear();
            _cloudMats.Clear();
            _cloudMeshes.Clear();

            var cloudShader = Shader.Find("Slug/RainCloud")
                           ?? Shader.Find("Slug/Toon")
                           ?? Shader.Find("Universal Render Pipeline/Lit");

            for (int i = 0; i < 3; i++)
            {
                var mesh = BuildCloudCanopyMesh(i);
                _cloudMeshes.Add(mesh);

                var go = new GameObject($"_RainCloud_{i}") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                var m = new Material(cloudShader) { name = $"RainCloudMat_{i}", hideFlags = HideFlags.DontSave };
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _cloudBodies.Add(go.transform);
                _cloudMats.Add(m);
            }

            var colShader = Shader.Find("Slug/RainColumn");
            if (colShader && !_columnHolder)
            {
                _columnMat = new Material(colShader) { hideFlags = HideFlags.DontSave };
                _columnMesh = new Mesh { name = "RainColumns", hideFlags = HideFlags.DontSave };
                _columnMesh.MarkDynamic();
                _columnHolder = new GameObject("_RainColumns") { hideFlags = HideFlags.DontSave };
                _columnHolder.transform.SetParent(transform, false);
                _columnHolder.AddComponent<MeshFilter>().sharedMesh = _columnMesh;
                var cr = _columnHolder.AddComponent<MeshRenderer>();
                cr.sharedMaterial = _columnMat;
                cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                cr.receiveShadows = false;
            }
        }

        public void RefreshNow()
        {
            EnsureBuilt();
            LateUpdate();
        }

        static void SafeDestroy(Object o)
        {
            if (!o) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        void OnDestroy() => Cleanup();

        public void Cleanup()
        {
            foreach (var t in _cloudBodies) if (t) SafeDestroy(t.gameObject);
            foreach (var m in _cloudMats) if (m) SafeDestroy(m);
            foreach (var mesh in _cloudMeshes) if (mesh) SafeDestroy(mesh);
            _cloudBodies.Clear();
            _cloudMats.Clear();
            _cloudMeshes.Clear();
            if (_columnHolder) SafeDestroy(_columnHolder);
            if (_columnMesh) SafeDestroy(_columnMesh);
            if (_columnMat) SafeDestroy(_columnMat);
            _columnHolder = null; _columnMesh = null; _columnMat = null;
            RemoveOrphanChildren();
        }

        void LateUpdate()
        {
            var wp = WeatherPresenter.Current;
            if (!wp || !wp.settings) return;
            IslandWeather.CloudsAt(wp.settings, wp.ShownHours, _clouds, wp.FixedSky);
            var t = wp.Time;
            var wind = wp.Wind;
            float day = t.daylightAmount;
            float dusk = t.twilight;
            Vector3 toSun = -t.sunDirection;
            if (toSun.y < 0.14f) toSun = new Vector3(toSun.x, 0.30f, toSun.z).normalized;

            bool isTest = wp.IsSmallTestField;
            float posScale = isTest ? 1f / 2.2f : 1f;
            float radScale = isTest ? 0.48f : 1f;
            Vector3 obs = wp.ObserverPosition;

            _verts.Clear(); _uvs.Clear(); _colors.Clear(); _tris.Clear();

            for (int i = 0; i < _cloudBodies.Count; i++)
            {
                if (i >= _clouds.Count)
                {
                    if (_cloudBodies[i].gameObject.activeSelf) _cloudBodies[i].gameObject.SetActive(false);
                    continue;
                }
                var c = _clouds[i];
                var body = _cloudBodies[i];
                if (!body.gameObject.activeSelf) body.gameObject.SetActive(true);

                Vector2 xz = c.center * posScale;
                float r = c.radius * radScale;
                float h = isTest ? 68f : cloudHeight;
                float vScale = r * cloudHeightRatio * Mathf.Lerp(0.88f, 1.12f, c.rain);

                Vector3 cloudPos = new Vector3(xz.x, h, xz.y);
                body.position = cloudPos;

                // Orient the cloud's wide anvil profile toward the observer when distant, and tilt the base plane
                // slightly toward the sightline so the flat underside reads as a sleek horizontal anvil edge (bottom 12%)
                // while 88% of the cloud shows its towering sunlit cauliflower face!
                Vector3 toObs = obs - cloudPos;
                float horizDist = new Vector2(toObs.x, toObs.z).magnitude;
                float windYaw = Mathf.Atan2(-wind.direction.y, -wind.direction.x) * Mathf.Rad2Deg + i * 35f;
                float faceYaw = horizDist > 5f ? Mathf.Atan2(toObs.x, toObs.z) * Mathf.Rad2Deg : windYaw;
                float farBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(55f, 175f, horizDist));
                float yaw = Mathf.LerpAngle(windYaw, faceYaw, farBlend);

                float sightPitch = Mathf.Atan2(cloudPos.y - obs.y, Mathf.Max(horizDist, 1f)) * Mathf.Rad2Deg;
                float tiltPitch = Mathf.Clamp(sightPitch * 0.72f, 0f, 19f) * farBlend;
                body.rotation = Quaternion.Euler(tiltPitch, yaw, 0f);
                body.localScale = new Vector3(r * 1.05f, vScale, r * 0.78f);

                // Cairn / Mathieu Bablet 4-band gouache palette:
                float storm = Mathf.Clamp01(c.rain * 0.85f);

                Color litSunny  = new Color(0.99f, 0.97f, 0.92f);
                Color litStorm  = new Color(0.91f, 0.94f, 0.98f);
                Color litDusk   = new Color(1.00f, 0.72f, 0.50f);
                Color litNight  = new Color(0.30f, 0.38f, 0.55f);

                Color midLSunny = new Color(0.87f, 0.91f, 0.96f);
                Color midLStorm = new Color(0.72f, 0.79f, 0.89f);
                Color midLDusk  = new Color(0.93f, 0.56f, 0.58f);
                Color midLNight = new Color(0.21f, 0.27f, 0.42f);

                Color midSSunny = new Color(0.60f, 0.71f, 0.86f);
                Color midSStorm = new Color(0.48f, 0.57f, 0.71f);
                Color midSDusk  = new Color(0.58f, 0.45f, 0.68f);
                Color midSNight = new Color(0.14f, 0.20f, 0.33f);

                Color baseSunny = new Color(0.43f, 0.54f, 0.71f);
                Color baseStorm = new Color(0.29f, 0.37f, 0.51f);
                Color baseDusk  = new Color(0.38f, 0.32f, 0.52f);
                Color baseNight = new Color(0.09f, 0.13f, 0.24f);

                Color inkSunny  = new Color(0.24f, 0.33f, 0.48f);
                Color inkStorm  = new Color(0.17f, 0.24f, 0.37f);
                Color inkDusk   = new Color(0.26f, 0.19f, 0.36f);
                Color inkNight  = new Color(0.05f, 0.08f, 0.16f);

                float warmDusk = Mathf.Clamp01(dusk * 1.35f) * (1f - storm * 0.45f);
                Color litCol  = Color.Lerp(litNight,  Color.Lerp(Color.Lerp(litSunny,  litStorm,  storm), litDusk,  warmDusk), day);
                Color midLCol = Color.Lerp(midLNight, Color.Lerp(Color.Lerp(midLSunny, midLStorm, storm), midLDusk, warmDusk), day);
                Color midSCol = Color.Lerp(midSNight, Color.Lerp(Color.Lerp(midSSunny, midSStorm, storm), midSDusk, warmDusk), day);
                Color baseCol = Color.Lerp(baseNight, Color.Lerp(Color.Lerp(baseSunny, baseStorm, storm), baseDusk, warmDusk), day);
                Color inkCol  = Color.Lerp(inkNight,  Color.Lerp(Color.Lerp(inkSunny,  inkStorm,  storm), inkDusk,  warmDusk), day);
                Color rimCol  = Color.Lerp(litCol, new Color(1f, 0.97f, 0.88f), 0.55f * day);

                var m = _cloudMats[i];
                m.SetColor("_LitColor", litCol);
                m.SetColor("_MidLightColor", midLCol);
                m.SetColor("_MidShadeColor", midSCol);
                m.SetColor("_BaseShadeColor", baseCol);
                m.SetColor("_RimColor", rimCol);
                m.SetColor("_InkColor", inkCol);
                m.SetColor("_SkyHorizonColor", wp.SkyColor);
                m.SetVector("_SunDir", new Vector4(toSun.x, toSun.y, toSun.z, 0f));
                m.SetFloat("_OutlineWidth", 0.0025f);
                m.SetFloat("_InkStrength", 0.55f);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", litCol);
                if (m.HasProperty("_ShadeColor")) m.SetColor("_ShadeColor", baseCol);

                if (c.rain > 0.04f && _columnMesh)
                {
                    AddRainCurtain(new Vector3(xz.x, h, xz.y), r * 0.65f, h, c.rain, wind, i);
                }
            }

            if (_columnMesh)
            {
                _columnMesh.Clear();
                if (_verts.Count > 0)
                {
                    _columnMesh.SetVertices(_verts);
                    _columnMesh.SetUVs(0, _uvs);
                    _columnMesh.SetColors(_colors);
                    _columnMesh.SetTriangles(_tris, 0);
                    _columnMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 3000f);
                }
                Color rainCol = Color.Lerp(new Color(0.20f, 0.26f, 0.38f), new Color(0.62f, 0.72f, 0.84f), day);
                rainCol = Color.Lerp(rainCol, new Color(0.82f, 0.62f, 0.58f), dusk * 0.4f);
                rainCol.a = columnOpacity;
                _columnMat.SetColor("_Color", rainCol);
            }
        }

        void AddRainCurtain(Vector3 topCenter, float radius, float height, float rain, IslandWindState wind, int seed)
        {
            Vector3 slant = new Vector3(wind.direction.x, 0f, wind.direction.y) * (wind.Speed * 3.4f);
            Vector3 botCenter = new Vector3(topCenter.x, -2f, topCenter.z) + slant;

            const int ringSegs = 18;
            int baseIdx = _verts.Count;
            for (int i = 0; i <= ringSegs; i++)
            {
                float u = i / (float)ringSegs;
                float a = u * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                _verts.Add(botCenter + dir * (radius * 0.88f));
                _verts.Add(topCenter + dir * (radius * 0.78f));
                _uvs.Add(new Vector2(u * 2f, 0f));
                _uvs.Add(new Vector2(u * 2f, 1f));
                var c = new Color(1f, seed * 0.37f, 1f, rain * 0.65f);
                _colors.Add(c); _colors.Add(c);
            }
            for (int i = 0; i < ringSegs; i++)
            {
                int b = baseIdx + i * 2;
                _tris.Add(b); _tris.Add(b + 1); _tris.Add(b + 2);
                _tris.Add(b + 2); _tris.Add(b + 1); _tris.Add(b + 3);
            }

            for (int k = 0; k < 3; k++)
            {
                float ang = (k * 60f + seed * 25f) * Mathf.Deg2Rad;
                Vector3 side = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (radius * 0.82f);
                int b = _verts.Count;
                _verts.Add(botCenter - side); _verts.Add(botCenter + side);
                _verts.Add(topCenter - side * 0.9f); _verts.Add(topCenter + side * 0.9f);
                _uvs.Add(new Vector2(0f, 0f)); _uvs.Add(new Vector2(1f, 0f));
                _uvs.Add(new Vector2(0f, 1f)); _uvs.Add(new Vector2(1f, 1f));
                var c = new Color(1f, (seed + k) * 0.29f, 1f, rain * 0.85f);
                _colors.Add(c); _colors.Add(c); _colors.Add(c); _colors.Add(c);
                _tris.Add(b); _tris.Add(b + 2); _tris.Add(b + 1);
                _tris.Add(b + 1); _tris.Add(b + 2); _tris.Add(b + 3);
            }
        }

        struct CloudPuff
        {
            public Vector3 center;
            public Vector3 radii;
            public int seed;
        }

        static float Hash11(int n)
        {
            n = (n << 13) ^ n;
            return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
        }

        static float RandRange(int seed, int step, float min, float max)
        {
            float t = Hash11(seed * 73856093 ^ step * 19349663) * 0.5f + 0.5f;
            return Mathf.Lerp(min, max, t);
        }

        static float Hash3(int x, int y, int z)
        {
            int n = x * 374761393 + y * 668265263 + z * 2147483647;
            n = (n ^ (n >> 13)) * 1274126177;
            return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
        }

        static float ValueNoise3D(Vector3 p)
        {
            int ix = Mathf.FloorToInt(p.x), iy = Mathf.FloorToInt(p.y), iz = Mathf.FloorToInt(p.z);
            float fx = p.x - ix, fy = p.y - iy, fz = p.z - iz;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            fz = fz * fz * (3f - 2f * fz);

            float c000 = Hash3(ix, iy, iz),         c100 = Hash3(ix + 1, iy, iz);
            float c010 = Hash3(ix, iy + 1, iz),     c110 = Hash3(ix + 1, iy + 1, iz);
            float c001 = Hash3(ix, iy, iz + 1),     c101 = Hash3(ix + 1, iy, iz + 1);
            float c011 = Hash3(ix, iy + 1, iz + 1), c111 = Hash3(ix + 1, iy + 1, iz + 1);

            float x00 = Mathf.Lerp(c000, c100, fx), x10 = Mathf.Lerp(c010, c110, fx);
            float x01 = Mathf.Lerp(c001, c101, fx), x11 = Mathf.Lerp(c011, c111, fx);
            float y0 = Mathf.Lerp(x00, x10, fy),    y1 = Mathf.Lerp(x01, x11, fy);
            return Mathf.Lerp(y0, y1, fz);
        }

        static float CauliflowerScallop(Vector3 n, int seed, out float knuckleRidge)
        {
            Vector3 offset = new Vector3((seed & 31) * 1.73f, ((seed >> 5) & 31) * 2.19f, ((seed >> 10) & 31) * 1.41f);
            float b1 = Mathf.Abs(ValueNoise3D(n * 2.1f + offset) * 2f - 1f);
            float b2 = Mathf.Abs(ValueNoise3D(n * 4.3f + offset * 1.7f + new Vector3(7.1f, 3.3f, 5.9f)) * 2f - 1f);

            float topWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.10f, 0.40f, n.y));
            knuckleRidge = Mathf.Clamp01(b1 * 0.68f + b2 * 0.32f);
            return (b1 * 0.17f + b2 * 0.065f) * topWeight;
        }

        static Vector3 EvaluatePuffPoint(in CloudPuff puff, Vector3 unitDir, out float knuckleRidge, out float baseFlatten)
        {
            float scallop = CauliflowerScallop(unitDir, puff.seed, out knuckleRidge);
            Vector3 r = puff.radii * (1f + scallop);
            Vector3 p = puff.center + Vector3.Scale(unitDir, r);

            // Crisp horizontal Lifted Condensation Level (y = 0.0): every basal lobe is cleanly sliced flat at y = 0
            baseFlatten = 0f;
            if (p.y < 0.12f)
            {
                baseFlatten = Mathf.Clamp01((0.12f - p.y) / 0.15f);
                if (p.y < 0f)
                    p.y *= 0.02f;
            }
            return p;
        }

        static float NormalizedDistToPuff(Vector3 p, in CloudPuff puff)
        {
            Vector3 d = p - puff.center;
            Vector3 scaled = new Vector3(d.x / Mathf.Max(puff.radii.x, 1e-3f),
                                         d.y / Mathf.Max(puff.radii.y, 1e-3f),
                                         d.z / Mathf.Max(puff.radii.z, 1e-3f));
            float len = scaled.magnitude;
            if (len < 1e-4f) return 0f;
            Vector3 unitDir = scaled / len;
            float scallop = CauliflowerScallop(unitDir, puff.seed, out _);
            return len / (1f + scallop);
        }

        /// <summary>
        /// Builds a monumental Cairn / Mathieu Bablet cumulonimbus mesh from 16 bold, sweeping domes
        /// (Big-Medium-Small hierarchy, wide terraced anvil wings, towering central head, no tiny fish-scale clutter).
        /// </summary>
        static Mesh BuildCloudCanopyMesh(int variant)
        {
            int s = 810 + variant * 233;
            var puffs = new List<CloudPuff>(18);
            float flip = (variant == 1) ? -1f : 1f;

            // 1) ANVIL BASE & WIDE WING TERRACES (5 sweeping horizontal masses resting on y = 0)
            var baseLobes = new (Vector3 pos, Vector3 rad)[]
            {
                (new Vector3( 0.00f * flip, 0.12f,  0.04f), new Vector3(0.56f, 0.24f, 0.34f)), // central anvil slab
                (new Vector3( 0.54f * flip, 0.10f,  0.02f), new Vector3(0.46f, 0.21f, 0.30f)), // right wing terrace
                (new Vector3( 0.92f * flip, 0.07f, -0.02f), new Vector3(0.32f, 0.15f, 0.22f)), // far right anvil shelf
                (new Vector3(-0.50f * flip, 0.10f,  0.02f), new Vector3(0.44f, 0.20f, 0.28f)), // left wing terrace
                (new Vector3(-0.86f * flip, 0.06f, -0.02f), new Vector3(0.28f, 0.14f, 0.20f)), // far left anvil shelf
            };
            for (int i = 0; i < baseLobes.Length; i++)
            {
                Vector3 jit = new Vector3(RandRange(s, 10 + i, -0.04f, 0.04f), 0f, RandRange(s, 30 + i, -0.03f, 0.03f));
                puffs.Add(new CloudPuff { center = baseLobes[i].pos + jit, radii = baseLobes[i].rad, seed = s + 10 + i * 7 });
            }

            // 2) MONUMENTAL MIDDLE CUMULUS DOMES (6 huge, sweeping cauliflower volumes forming the proud body)
            var bodyLobes = new (Vector3 pos, Vector3 rad)[]
            {
                (new Vector3( 0.10f * flip, 0.42f,  0.04f), new Vector3(0.46f, 0.40f, 0.34f)), // dominant central mass
                (new Vector3(-0.30f * flip, 0.32f,  0.06f), new Vector3(0.36f, 0.32f, 0.28f)), // left shoulder dome
                (new Vector3( 0.44f * flip, 0.30f,  0.05f), new Vector3(0.35f, 0.30f, 0.27f)), // right shoulder dome
                (new Vector3( 0.12f * flip, 0.34f, -0.14f), new Vector3(0.38f, 0.34f, 0.28f)), // rear volume backing
                (new Vector3(-0.62f * flip, 0.20f,  0.02f), new Vector3(0.26f, 0.22f, 0.22f)), // outer left step
                (new Vector3( 0.72f * flip, 0.20f,  0.00f), new Vector3(0.25f, 0.21f, 0.21f)), // outer right step
            };
            for (int i = 0; i < bodyLobes.Length; i++)
            {
                Vector3 jit = new Vector3(RandRange(s, 110 + i, -0.03f, 0.03f),
                                          RandRange(s, 130 + i, -0.02f, 0.03f),
                                          RandRange(s, 150 + i, -0.02f, 0.02f));
                puffs.Add(new CloudPuff { center = bodyLobes[i].pos + jit, radii = bodyLobes[i].rad, seed = s + 100 + i * 11 });
            }

            // 3) SUMMIT CROWN & RIM ACCENTS (5 crisp upper turrets crowning the top up to y ~ 1.02)
            var crownLobes = new (Vector3 pos, Vector3 rad)[]
            {
                (new Vector3( 0.14f * flip, 0.68f,  0.00f), new Vector3(0.32f, 0.30f, 0.26f)), // grand summit crown
                (new Vector3(-0.14f * flip, 0.56f,  0.02f), new Vector3(0.25f, 0.24f, 0.22f)), // upper-left turret
                (new Vector3( 0.36f * flip, 0.50f,  0.02f), new Vector3(0.23f, 0.22f, 0.20f)), // upper-right turret
                (new Vector3(-0.42f * flip, 0.42f,  0.03f), new Vector3(0.19f, 0.18f, 0.17f)), // left crest accent
                (new Vector3( 0.56f * flip, 0.38f,  0.02f), new Vector3(0.18f, 0.17f, 0.16f)), // right crest accent
            };
            for (int i = 0; i < crownLobes.Length; i++)
            {
                Vector3 jit = new Vector3(RandRange(s, 210 + i, -0.03f, 0.03f),
                                          RandRange(s, 230 + i, -0.02f, 0.02f),
                                          RandRange(s, 250 + i, -0.02f, 0.02f));
                puffs.Add(new CloudPuff { center = crownLobes[i].pos + jit, radii = crownLobes[i].rad, seed = s + 200 + i * 17 });
            }

            var verts = new List<Vector3>(6144);
            var norms = new List<Vector3>(6144);
            var uvs = new List<Vector2>(6144);
            var uv2s = new List<Vector3>(6144);
            var cols = new List<Color>(6144);
            var tris = new List<int>(18432);

            const int rings = 14, segs = 22;
            const float minY = 0.0f, maxY = 0.96f;
            Vector3 macroCenter = new Vector3(0.04f * flip, 0.22f, 0.0f);

            for (int k = 0; k < puffs.Count; k++)
            {
                var puff = puffs[k];
                int start = verts.Count;

                for (int y = 0; y <= rings; y++)
                {
                    float v = y / (float)rings;
                    float phi = Mathf.PI * v;
                    float sinPhi = Mathf.Sin(phi), cosPhi = Mathf.Cos(phi);

                    for (int x = 0; x <= segs; x++)
                    {
                        float u = x / (float)segs;
                        float th = Mathf.PI * 2f * u;
                        Vector3 unitDir = new Vector3(sinPhi * Mathf.Cos(th), cosPhi, sinPhi * Mathf.Sin(th));

                        Vector3 p = EvaluatePuffPoint(puff, unitDir, out float knuckleRidge, out float baseFlatten);

                        const float eps = 0.04f;
                        Vector3 tangentTheta = new Vector3(-Mathf.Sin(th), 0f, Mathf.Cos(th));
                        Vector3 tangentPhi = new Vector3(cosPhi * Mathf.Cos(th), -sinPhi, cosPhi * Mathf.Sin(th));
                        Vector3 d1 = (unitDir + tangentTheta * eps).normalized;
                        Vector3 d2 = (unitDir + tangentPhi * eps).normalized;
                        Vector3 p1 = EvaluatePuffPoint(puff, d1, out _, out _);
                        Vector3 p2 = EvaluatePuffPoint(puff, d2, out _, out _);
                        Vector3 localN = Vector3.Cross(p2 - p, p1 - p);
                        if (localN.sqrMagnitude < 1e-8f) localN = unitDir;
                        else localN.Normalize();
                        if (Vector3.Dot(localN, unitDir) < 0f) localN = -localN;

                        Vector3 macroN = new Vector3((p.x - macroCenter.x) * 0.55f,
                                                     Mathf.Max(-0.20f, (p.y - macroCenter.y) * 1.15f + 0.20f),
                                                     (p.z - macroCenter.z) * 0.85f).normalized;
                        if (baseFlatten > 0.35f)
                            macroN = Vector3.Slerp(macroN, new Vector3(0f, -0.92f, 0f), baseFlatten * 0.88f).normalized;

                        float minOtherDist = 10f;
                        for (int j = 0; j < puffs.Count; j++)
                        {
                            if (j == k) continue;
                            float nd = NormalizedDistToPuff(p, puffs[j]);
                            if (nd < minOtherDist) minOtherDist = nd;
                        }
                        float signedSeam = minOtherDist - 1.0f;
                        float height01 = Mathf.Clamp01((p.y - minY) / (maxY - minY));
                        float puffLocalY = Mathf.Clamp01(unitDir.y * 0.5f + 0.5f);

                        verts.Add(p);
                        norms.Add(localN);
                        uvs.Add(new Vector2(knuckleRidge, signedSeam));
                        uv2s.Add(macroN);
                        cols.Add(new Color(puffLocalY, height01, baseFlatten, 1f));
                    }
                }

                for (int y = 0; y < rings; y++)
                {
                    for (int x = 0; x < segs; x++)
                    {
                        int a = start + y * (segs + 1) + x;
                        int b = a + segs + 1;
                        tris.Add(a); tris.Add(a + 1); tris.Add(b + 1);
                        tris.Add(a); tris.Add(b + 1); tris.Add(b);
                    }
                }
            }

            var mesh = new Mesh { name = $"RainCloudCanopy_{variant}", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(2, uv2s);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
