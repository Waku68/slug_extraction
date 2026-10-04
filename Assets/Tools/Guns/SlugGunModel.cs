using System.Collections.Generic;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// A gun built from code (<see cref="SlugRifle"/>, <see cref="SlugShotgun"/>, <see cref="SlugSniper"/>,
    /// <see cref="SlugPistol"/>): the shape,
    /// its moving parts (magazine, pump, bolt), and where the two hands go for any state of the handling.
    ///
    /// Local space, the same for every gun: the bore runs along +Z at <see cref="BoreY"/>, up is +Y, and the origin is
    /// the middle of the butt plate, the point that rests on the shoulder. All guns share one look: wood with a faint
    /// generated grain, matte dark steel, rubber, and a toy-like star for the muzzle flash.
    /// </summary>
    public abstract class SlugGunModel : MonoBehaviour
    {
        /// <summary>A fist: where it is and the direction of what it holds (gun space).</summary>
        public struct Hand
        {
            public Vector3 point, axis;
            public Hand(Vector3 point, Vector3 axis) { this.point = point; this.axis = axis; }
        }

        public abstract float BoreY { get; }
        public abstract Vector3 MuzzleLocal { get; }
        /// <summary>Where the right hand holds it when nothing else is going on; the carry is placed by this point.</summary>
        public abstract Vector3 RestGripLocal { get; }
        /// <summary>Where rounds go in: brought into the lower middle of the view in first person during a reload.</summary>
        public abstract Vector3 ReloadFocusLocal { get; }
        /// <summary>The scope's eyepiece, for guns with a scope.</summary>
        public virtual Vector3 ScopeEyeLocal => new Vector3(0f, BoreY + 0.05f, 0.15f);
        /// <summary>
        /// How far forward of the origin the butt plate sits (m) when the stock is cut short. The holder puts the butt
        /// plate on the shoulder, so the whole gun, and the hands on it, come that much closer to the body.
        /// </summary>
        public virtual float ButtZ => 0f;

        /// <summary>Moves the moving parts for this state of the handling and says where the hands go (gun space).</summary>
        public abstract void Pose(GunHandling h, GunSettings s, out Hand right, out Hand left);
        /// <summary>How full it is (0..1), for guns that show it.</summary>
        public virtual void SetAmmo(float fraction) { }

        protected abstract void Build(bool castShadows);

        protected static Material Wood, Steel, Cover, Rubber, Glass, Salt, FlashMat;
        static Mesh _flashMesh;
        static Texture2D _grain;

        Transform _muzzle;
        GameObject _flash;

        public Transform Muzzle => _muzzle;

        /// <summary>Creates a gun of this kind (no colliders). Shadows can be turned off for a first-person copy.</summary>
        public static SlugGunModel Create(GunKind kind, string name, Transform parent, bool castShadows)
        {
            EnsureShared();
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            SlugGunModel gun = kind switch
            {
                GunKind.Shotgun => root.AddComponent<SlugShotgun>(),
                GunKind.Sniper => root.AddComponent<SlugSniper>(),
                GunKind.Pistol => root.AddComponent<SlugPistol>(),
                _ => root.AddComponent<SlugRifle>(),
            };
            gun.Build(castShadows);
            gun._muzzle = new GameObject("Muzzle").transform;
            gun._muzzle.SetParent(root.transform, false);
            gun._muzzle.localPosition = gun.MuzzleLocal;
            gun._flash = Part(gun._muzzle, "Flash", _flashMesh, FlashMat, false).gameObject;
            gun._flash.SetActive(false);
            gun.SetAmmo(1f);
            return gun;
        }

        /// <summary>The muzzle flash: a toy-like star, turned and sized a little differently every shot.</summary>
        public void SetFlash(bool on, float roll, float scale)
        {
            if (_flash.activeSelf != on) _flash.SetActive(on);
            if (!on) return;
            _flash.transform.localRotation = Quaternion.Euler(0f, 0f, roll);
            _flash.transform.localScale = Vector3.one * scale;
        }

        protected static Transform Part(Transform parent, string name, Mesh mesh, Material mat, bool castShadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (!castShadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        /// <summary>An empty child that turns about <paramref name="pivot"/> (for a moving part).</summary>
        protected static Transform Pivot(Transform parent, string name, Vector3 pivot)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pivot;
            return t;
        }

        protected static List<Vector2> Zy(params float[] zy)
        {
            var p = new List<Vector2>(zy.Length / 2);
            for (int i = 0; i + 1 < zy.Length; i += 2) p.Add(new Vector2(zy[i], zy[i + 1]));
            return p;
        }

        /// <summary>The same polygon moved by (dz, dy).</summary>
        protected static List<Vector2> Shift(List<Vector2> p, float dz, float dy)
        {
            var r = new List<Vector2>(p.Count);
            foreach (var q in p) r.Add(new Vector2(q.x + dz, q.y + dy));
            return r;
        }

        /// <summary>A trigger guard (a U under the receiver) and the trigger in it, the guard's top front at (z, y).</summary>
        protected static void TriggerGroup(ProfileMesh steel, float z, float y)
        {
            steel.Extrude(Shift(Zy(0f, 0f, 0f, -0.029f, 0.008f, -0.037f, 0.052f, -0.037f, 0.056f, 0f,
                                   0.05f, 0f, 0.047f, -0.031f, 0.011f, -0.031f, 0.006f, -0.025f, 0.006f, 0f), z, y), 0.005f, 0.001f);
            steel.Extrude(Shift(Zy(0.017f, 0f, 0.024f, 0f, 0.022f, -0.017f, 0.015f, -0.024f, 0.013f, -0.02f, 0.018f, -0.014f), z, y), 0.003f, 0f);
        }

        /// <summary>
        /// A stock's side profile cut short: the part behind <paramref name="join"/> (where the stock meets the receiver
        /// or the grip) is squeezed so its back end moves forward by <paramref name="cut"/>; the rest is unchanged.
        /// </summary>
        protected static List<Vector2> CutStock(List<Vector2> p, float cut, float join)
        {
            var r = new List<Vector2>(p.Count);
            float k = (join - cut) / join;
            foreach (var q in p) r.Add(q.x < join ? new Vector2(cut + q.x * k, q.y) : q);
            return r;
        }

        protected static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>0 → 1 between a and b, then back to 0 between c and d.</summary>
        protected static float Bump(float u, float a, float b, float c, float d)
            => Smooth((u - a) / Mathf.Max(b - a, 1e-4f)) * (1f - Smooth((u - c) / Mathf.Max(d - c, 1e-4f)));

        static void EnsureShared()
        {
            if (_flashMesh != null) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            _grain = GrainTexture();
            Wood = Mat(lit, new Color(0.78f, 0.44f, 0.22f), 0f, 0.3f);
            Wood.SetTexture("_BaseMap", _grain);
            Steel = Mat(lit, new Color(0.17f, 0.175f, 0.19f), 0.1f, 0.28f);
            Cover = Mat(lit, new Color(0.23f, 0.235f, 0.255f), 0.1f, 0.32f);
            Rubber = Mat(lit, new Color(0.11f, 0.1f, 0.1f), 0f, 0.15f);
            Glass = Mat(lit, new Color(0.08f, 0.09f, 0.11f), 0.2f, 0.85f);
            Salt = Mat(lit, new Color(0.97f, 0.96f, 0.93f), 0f, 0.55f);
            FlashMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { hideFlags = HideFlags.DontSave };
            FlashMat.SetColor("_BaseColor", new Color(1f, 0.93f, 0.7f));

            // muzzle flash: a star facing forward and two crossed flames along the bore
            var flash = new ProfileMesh { uvScale = 1f };
            var star = new List<Vector2>();
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f, r = i % 2 == 0 ? 0.042f : 0.014f;
                star.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
            flash.Flat(star, new Vector3(0f, 0f, 0.004f), Vector3.right, Vector3.up, true);
            var flame = Zy(0f, 0f, 0.025f, 0.016f, 0.075f, 0f, 0.025f, -0.016f);
            flash.Flat(flame, Vector3.zero, Vector3.forward, Vector3.up, true);
            flash.Flat(flame, Vector3.zero, Vector3.forward, Vector3.right, true);
            _flashMesh = flash.ToMesh("GunFlash");
        }

        protected static Material Mat(Shader s, Color c, float metallic, float smooth)
        {
            var m = new Material(s) { hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }

        /// <summary>A faint wood grain (generated, not drawn): soft stripes along U that wander a little.</summary>
        static Texture2D GrainTexture()
        {
            const int W = 128, H = 32;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, true) { name = "GunGrain", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                // everything repeats across the tile, so the grain runs on without seams
                float u = x / (float)W * Mathf.PI * 2f;
                float wander = 2.5f * Mathf.Sin(u * 2f) + 1.5f * Mathf.Sin(u * 3f + 1.3f);
                float stripe = 0.5f + 0.5f * Mathf.Sin((y + wander) / H * Mathf.PI * 2f * 5f);
                float fine = 0.5f + 0.5f * Mathf.Sin((y + wander * 0.6f) / H * Mathf.PI * 2f * 13f + Mathf.Sin(u * 5f));
                float v = 0.84f + 0.11f * stripe + 0.05f * fine;
                tex.SetPixel(x, y, new Color(v, v * 0.98f, v * 0.96f));
            }
            tex.Apply(true);
            return tex;
        }
    }
}
