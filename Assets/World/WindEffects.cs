using System.Collections.Generic;
using UnityEngine;

namespace SlugWorld
{
    /// <summary>
    /// Shows the island's wind in the air around the slug's eyes (0.47 m; docs/ideas/wind_ideas.md #8 & #12):
    ///   - Drifting motes (舞う物) that change with the ground underfoot: leaves in the forest, sand skimming at eye
    ///     height in the desert, white salt dust on the salt flat and mountain, floating seed fluff in the flower meadow,
    ///     and fine spray by the water and coast.
    ///   - Subtle white wind lines (風の線) that streak past only when the wind or a gust is strong (pop, never noisy).
    /// Added to the scene by <see cref="WeatherPresenter"/>.
    /// </summary>
    public class WindEffects : MonoBehaviour
    {
        [Tooltip("舞う物の量（1秒あたり）")] public float moteRate = 28f;
        [Tooltip("強い風のときにたまに出る白い風の線の最大数")] public int maxWindLines = 7;
        [Tooltip("風の線の明るさ（目立ちすぎない値）")] public float lineIntensity = 0.14f;

        ParticleSystem _motes;
        ParticleSystemRenderer _moteRenderer;
        float _checkTimer;
        GroundKind _ground = GroundKind.Soil;
        bool _meadow, _coast;

        struct WindLine
        {
            public Vector3 start;
            public float length, width, speed, born, life, wavePhase;
        }

        readonly List<WindLine> _lines = new List<WindLine>(12);
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Vector2> _uvs = new List<Vector2>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<int> _tris = new List<int>();
        Mesh _lineMesh;
        Material _lineMat;
        GameObject _lineHolder;
        float _lineSpawnAcc;

        void Start()
        {
            BuildMotes();
            var shader = Shader.Find("Slug/SunShaft");
            if (shader)
            {
                _lineMat = new Material(shader) { hideFlags = HideFlags.DontSave };
                _lineMat.SetColor("_Color", new Color(0.95f, 0.98f, 1f, 1f));
                _lineMat.SetFloat("_Intensity", lineIntensity);
                _lineMesh = new Mesh { name = "WindLines", hideFlags = HideFlags.DontSave };
                _lineMesh.MarkDynamic();
                _lineHolder = new GameObject("_WindLines") { hideFlags = HideFlags.DontSave };
                _lineHolder.transform.SetParent(transform, false);
                _lineHolder.AddComponent<MeshFilter>().sharedMesh = _lineMesh;
                var r = _lineHolder.AddComponent<MeshRenderer>();
                r.sharedMaterial = _lineMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        void OnDestroy()
        {
            if (_lineHolder) Destroy(_lineHolder);
            if (_lineMesh) Destroy(_lineMesh);
            if (_lineMat) Destroy(_lineMat);
        }

        void BuildMotes()
        {
            var go = new GameObject("WindMotes") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _motes = go.AddComponent<ParticleSystem>();
            _motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _motes.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.036f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.02f;

            var shape = _motes.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 2.2f, 14f);

            var vel = _motes.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;

            var noise = _motes.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.65f;
            noise.scrollSpeed = 0.5f;

            var col = _motes.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            _moteRenderer = go.GetComponent<ParticleSystemRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh)
            {
                var m = new Material(sh) { hideFlags = HideFlags.DontSave };
                m.SetFloat("_Surface", 1f);
                m.SetColor("_BaseColor", Color.white);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
                _moteRenderer.sharedMaterial = m;
            }
            _moteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _motes.Play();
        }

        void LateUpdate()
        {
            var wp = WeatherPresenter.Current;
            var cam = Camera.main;
            if (!wp || !cam) return;

            Vector3 eye = cam.transform.position;
            var wind = wp.Wind;
            float gustHere = IslandWind.GustWaveAt(wp.ShownHours, eye, wind);
            float windSpeed = wind.Speed * (1f + 0.35f * gustHere);
            Vector3 windVec = new Vector3(wind.direction.x, 0f, wind.direction.y) * windSpeed;

            _checkTimer -= Time.deltaTime;
            if (_checkTimer <= 0f)
            {
                _checkTimer = 0.3f;
                _ground = GroundKinds.At(eye);
                _coast = eye.y < 2.4f;
                _meadow = _ground == GroundKind.Soil && eye.z < -105f && Mathf.Abs(eye.x) < 160f;
            }

            UpdateMotes(eye, wind, windVec, wp.Time.daylightAmount, wp.Weather.rain);
            UpdateWindLines(eye, wind, gustHere, wp.Time.daylightAmount);
        }

        void UpdateMotes(Vector3 eye, IslandWindState wind, Vector3 windVec, float day, float rain)
        {
            if (!_motes) return;
            // Place the emitter box slightly upwind of the camera so motes drift across the view
            Vector3 upwind = -new Vector3(wind.direction.x, 0f, wind.direction.y) * 3.5f;
            float boxY = _ground == GroundKind.Sand ? 0.15f : 0.7f;
            _motes.transform.position = eye + upwind + Vector3.up * boxY;

            var main = _motes.main;
            var shape = _motes.shape;
            var vel = _motes.velocityOverLifetime;
            var emission = _motes.emission;

            float lightMul = Mathf.Lerp(0.35f, 1f, day);
            float rateMul = Mathf.Lerp(0.35f, 1.8f, wind.strength + wind.gust * 0.5f) * (1f - 0.65f * rain);

            if (_ground == GroundKind.Sand)
            {
                // Desert: sand skimming low right at the slug's 0.47 m eye height
                main.startColor = new Color(0.86f, 0.75f, 0.56f, 0.55f) * lightMul;
                main.startSize = new ParticleSystem.MinMaxCurve(0.010f, 0.022f);
                shape.scale = new Vector3(13f, 0.65f, 13f);
                vel.x = windVec.x * 1.15f;
                vel.y = -0.05f;
                vel.z = windVec.z * 1.15f;
                _moteRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                _moteRenderer.lengthScale = 2.4f;
                rateMul *= 1.6f;
            }
            else if (_ground == GroundKind.Salt)
            {
                // Salt flat / salt mountain: sparkling white salt powder
                main.startColor = new Color(0.95f, 0.96f, 0.98f, 0.6f) * lightMul;
                main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.025f);
                shape.scale = new Vector3(13f, 1.4f, 13f);
                vel.x = windVec.x * 0.9f;
                vel.y = 0.02f;
                vel.z = windVec.z * 0.9f;
                _moteRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                rateMul *= 1.25f;
            }
            else if (_ground == GroundKind.Wet || _coast)
            {
                // Water / coast: fine sea spray
                main.startColor = new Color(0.84f, 0.92f, 0.98f, 0.45f) * lightMul;
                main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.026f);
                shape.scale = new Vector3(13f, 1.5f, 13f);
                vel.x = windVec.x * 0.85f;
                vel.y = 0.06f;
                vel.z = windVec.z * 0.85f;
                _moteRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
            else if (_meadow)
            {
                // Flower meadow: floating white seed fluff (綿毛)
                main.startColor = new Color(0.98f, 0.97f, 0.90f, 0.7f) * lightMul;
                main.startSize = new ParticleSystem.MinMaxCurve(0.022f, 0.042f);
                shape.scale = new Vector3(14f, 2.2f, 14f);
                vel.x = windVec.x * 0.55f;
                vel.y = 0.05f;
                vel.z = windVec.z * 0.55f;
                _moteRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
            else
            {
                // Forest / soil: small olive-green leaves tumbling in the breeze
                main.startColor = new Color(0.46f, 0.58f, 0.30f, 0.78f) * lightMul;
                main.startSize = new ParticleSystem.MinMaxCurve(0.024f, 0.046f);
                shape.scale = new Vector3(14f, 2.6f, 14f);
                vel.x = windVec.x * 0.7f;
                vel.y = -0.18f;
                vel.z = windVec.z * 0.7f;
                _moteRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            emission.rateOverTime = moteRate * rateMul;
        }

        void UpdateWindLines(Vector3 eye, IslandWindState wind, float gustHere, float day)
        {
            if (!_lineMesh) return;
            float now = Time.time;
            _lines.RemoveAll(l => now > l.born + l.life);

            // Spawn wind lines only when wind + gust is noticeably strong (あつ森 / 風のタクト控えめ)
            float gale = Mathf.InverseLerp(0.42f, 0.88f, wind.strength + gustHere * 0.55f);
            if (gale > 0.02f && _lines.Count < maxWindLines)
            {
                _lineSpawnAcc += gale * 4.5f * Time.deltaTime;
                while (_lineSpawnAcc >= 1f && _lines.Count < maxWindLines)
                {
                    _lineSpawnAcc -= 1f;
                    Vector2 r2 = Random.insideUnitCircle * 9f;
                    Vector3 upwind = -new Vector3(wind.direction.x, 0f, wind.direction.y) * Random.Range(3f, 9f);
                    _lines.Add(new WindLine
                    {
                        start = eye + upwind + new Vector3(r2.x, Random.Range(-0.2f, 2.2f), r2.y),
                        length = Random.Range(2.4f, 4.5f),
                        width = Random.Range(0.025f, 0.048f),
                        speed = Mathf.Lerp(6f, 12f, gale),
                        born = now,
                        life = Random.Range(0.8f, 1.35f),
                        wavePhase = Random.Range(0f, 6.28f),
                    });
                }
            }
            else _lineSpawnAcc = 0f;

            _verts.Clear(); _uvs.Clear(); _colors.Clear(); _tris.Clear();
            Vector3 wDir = new Vector3(wind.direction.x, 0f, wind.direction.y).normalized;
            const int segs = 8;

            foreach (var l in _lines)
            {
                float age = now - l.born;
                float uLife = age / l.life;
                float fade = Mathf.Sin(uLife * Mathf.PI);
                if (fade <= 0.01f) continue;

                Vector3 head = l.start + wDir * (age * l.speed);
                int baseIdx = _verts.Count;
                for (int i = 0; i <= segs; i++)
                {
                    float s = i / (float)segs;
                    Vector3 p = head - wDir * (s * l.length);
                    p.y += Mathf.Sin(s * 5f + age * 8f + l.wavePhase) * 0.06f;
                    Vector3 side = Vector3.Cross(wDir, eye - p);
                    if (side.sqrMagnitude < 1e-6f) side = Vector3.up;
                    side = side.normalized * (l.width * 0.5f * Mathf.Sin(s * Mathf.PI));

                    _verts.Add(p - side);
                    _verts.Add(p + side);
                    _uvs.Add(new Vector2(0f, s));
                    _uvs.Add(new Vector2(1f, s));
                    var c = new Color(1f, l.wavePhase * 0.15f, 1f, fade * Mathf.Lerp(0.4f, 1f, day));
                    _colors.Add(c);
                    _colors.Add(c);
                }
                for (int i = 0; i < segs; i++)
                {
                    int b = baseIdx + i * 2;
                    _tris.Add(b); _tris.Add(b + 2); _tris.Add(b + 1);
                    _tris.Add(b + 1); _tris.Add(b + 2); _tris.Add(b + 3);
                }
            }

            _lineMesh.Clear();
            if (_verts.Count > 0)
            {
                _lineMesh.SetVertices(_verts);
                _lineMesh.SetUVs(0, _uvs);
                _lineMesh.SetColors(_colors);
                _lineMesh.SetTriangles(_tris, 0);
                _lineMesh.bounds = new Bounds(eye, Vector3.one * 30f);
            }
            _lineMat.SetFloat("_Intensity", lineIntensity);
        }
    }
}
