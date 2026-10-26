using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Slime flung off by the shrink and stretch (<see cref="SlugMoveController"/> adds it): a few drops behind when it
    /// stretches off the ground, a ring of drops and a wet mark that fades where it lands. A placeholder look
    /// (settings.lungeSplash turns it off).
    /// </summary>
    public class SlugSplash : MonoBehaviour
    {
        static readonly Color Slime = new Color(0.82f, 0.88f, 0.78f, 0.75f);
        static readonly Color Wet = new Color(0.12f, 0.14f, 0.12f, 0.4f);     // the mark: the ground darkened by the slime
        static Material _dropMat, _markMat;

        SlugMoveController _move;
        CharacterController _cc;
        ParticleSystem _drops, _marks;

        void Awake()
        {
            _move = GetComponent<SlugMoveController>();
            _cc = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            if (!_move) return;
            _move.Lunged += OnLunged;
            _move.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (!_move) return;
            _move.Lunged -= OnLunged;
            _move.Landed -= OnLanded;
        }

        bool On => _move && _move.settings && _move.settings.lungeSplash;

        Vector3 Feet => _cc ? new Vector3(transform.position.x, _cc.bounds.min.y, transform.position.z) : transform.position;

        /// <summary>Thrown back and up from under the tail, more the longer it was charged.</summary>
        void OnLunged(float charge)
        {
            if (!On || !_move.OnGround) return;
            Build();
            var p = new ParticleSystem.EmitParams
            {
                startColor = Slime,
            };
            int n = Mathf.RoundToInt(Mathf.Lerp(5f, 12f, charge));
            Vector3 back = -transform.forward;
            for (int i = 0; i < n; i++)
            {
                Vector3 side = transform.right * Random.Range(-0.6f, 0.6f);
                p.position = Feet + back * Random.Range(0.2f, 0.45f) + side * 0.25f + Vector3.up * 0.03f;
                p.velocity = (back * Random.Range(0.4f, 1.2f) + side + Vector3.up * Random.Range(1f, 2.2f)) * Mathf.Lerp(0.7f, 1f, charge);
                p.startSize = Random.Range(0.03f, 0.06f);
                p.startLifetime = Random.Range(0.35f, 0.6f);
                _drops.Emit(p, 1);
            }
        }

        /// <summary>A ring of drops round the body, and a wet mark left on flat enough ground.</summary>
        void OnLanded()
        {
            if (!On) return;
            Build();
            var p = new ParticleSystem.EmitParams { startColor = Slime };
            Vector3 at = Feet;
            for (int i = 0; i < 14; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                // from the edge of the body (longer along it), outward
                Vector3 edge = transform.rotation * new Vector3(Mathf.Cos(a) * 0.32f, 0f, Mathf.Sin(a) * 0.58f);
                p.position = at + edge + Vector3.up * 0.04f;
                p.velocity = edge.normalized * Random.Range(0.7f, 1.6f) + Vector3.up * Random.Range(0.8f, 1.6f);
                p.startSize = Random.Range(0.03f, 0.055f);
                p.startLifetime = Random.Range(0.3f, 0.5f);
                _drops.Emit(p, 1);
            }
            if (_move.SlopeAngle > 20f) return;     // the mark lies flat: only where the ground does too
            var m = new ParticleSystem.EmitParams
            {
                position = at + Vector3.up * 0.012f,
                velocity = Vector3.zero,
                startColor = Wet,
                startSize = Random.Range(0.8f, 1f),
                rotation = transform.eulerAngles.y + Random.Range(-15f, 15f),
                startLifetime = 5f,
            };
            _marks.Emit(m, 1);
        }

        // ------------------------------------------------------------------ building
        void Build()
        {
            if (_drops) return;
            _drops = Make("SplashDrops", ParticleSystemRenderMode.Billboard, 1.6f, DropMat(), 200);
            _marks = Make("SplashMarks", ParticleSystemRenderMode.HorizontalBillboard, 0f, MarkMat(), 40);
            var col = _marks.colorOverLifetime;      // fades out, slowly at first
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = _drops.sizeOverLifetime;     // drops shrink as they fall
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
        }

        ParticleSystem Make(string name, ParticleSystemRenderMode mode, float gravity, Material mat, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.layer = 2;      // Ignore Raycast: never in the way of a shot or the ground probe
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = max;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = mode;
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        static Material DropMat() => _dropMat ? _dropMat : _dropMat = MakeMat(RoundTexture(32, 0.6f));
        static Material MarkMat() => _markMat ? _markMat : _markMat = MakeMat(RoundTexture(64, 0.7f));

        /// <summary>A soft white disc (hard edge 1 = a sharp drop, smaller = a blurred smear).</summary>
        static Texture2D RoundTexture(int n, float hard)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // a little lumpy at the edge, like a splat
                    float edge = 1f - 0.12f * Mathf.Sin(Mathf.Atan2(dy, dx) * 5f) * (1f - hard);
                    float a = Mathf.Clamp01((edge - d) / Mathf.Max(1f - hard, 0.05f));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Material MakeMat(Texture2D tex)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (!shader) return null;
            var m = new Material(shader) { hideFlags = HideFlags.DontSave };
            m.SetFloat("_Surface", 1f);
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            return m;
        }
    }
}
