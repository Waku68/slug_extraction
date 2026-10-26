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
    /// generated grain, matte dark steel, rubber, a painted CO2 bottle with brass fittings (they shoot salt on carbon
    /// dioxide, like a paintball marker, so the rain never stops them), and a puff of gas at the muzzle.
    ///
    /// Each gun is authored at full size ("raw" space) and drawn compact (<see cref="Warp"/>): everything up to
    /// <see cref="BarrelSplit"/> (stock, grip, receiver, the hands' places) is shortened by <see cref="BodyScale"/>
    /// (its thickness and height by <see cref="WidthScale"/> and <see cref="HeightScale"/>, so it stays chunky), and the
    /// barrel is run out <see cref="BarrelExtra"/> further. Small in the hands, so the arms stay short, and
    /// still long enough to read as a gun. The holder only ever sees the drawn space.
    /// </summary>
    public abstract class SlugGunModel : MonoBehaviour
    {
        /// <summary>A fist: where it is and the direction of what it holds (gun space).</summary>
        public struct Hand
        {
            public Vector3 point, axis;
            public Hand(Vector3 point, Vector3 axis) { this.point = point; this.axis = axis; }
        }

        // ---- the raw shape, as authored (full size)
        protected abstract float RawBoreY { get; }
        protected abstract Vector3 RawMuzzle { get; }
        protected abstract Vector3 RawRestGrip { get; }
        protected abstract Vector3 RawReloadFocus { get; }
        protected virtual Vector3 RawScopeEye => new Vector3(0f, RawBoreY + 0.05f, 0.15f);
        protected virtual float RawButtZ => 0f;
        /// <summary>Moves the moving parts (with <see cref="Place"/>) and says where the hands go, in raw space.</summary>
        protected abstract void RawPose(GunHandling h, GunSettings s, out Hand right, out Hand left);

        // ---- how it is drawn: compact, with a long barrel
        /// <summary>How much the stock, grip and receiver are scaled down (1 = as authored).</summary>
        protected virtual float BodyScale => 1f;
        /// <summary>The thickness (side to side) as drawn (1 = as authored).</summary>
        protected virtual float WidthScale => BodyScale;
        /// <summary>The height as drawn (1 = as authored).</summary>
        protected virtual float HeightScale => BodyScale;
        Vector3 Scale3 => new Vector3(WidthScale, HeightScale, BodyScale);
        /// <summary>Where the barrel is cut to be run out further (raw z): everything in front of it moves forward.</summary>
        protected virtual float BarrelSplit => float.PositiveInfinity;
        /// <summary>How much longer the barrel is drawn (m).</summary>
        protected virtual float BarrelExtra => 0f;

        /// <summary>The scale of the stock, grip and receiver (1 = as authored).</summary>
        public float Scale => BodyScale;
        /// <summary>A raw point as drawn: scaled down, and pushed forward when it is past the barrel's cut.</summary>
        public Vector3 Warp(Vector3 raw) => Vector3.Scale(raw, Scale3) + (raw.z > BarrelSplit ? new Vector3(0f, 0f, BarrelExtra) : Vector3.zero);

        public float BoreY => RawBoreY * HeightScale;
        public Vector3 MuzzleLocal => Warp(RawMuzzle);
        /// <summary>Where the right hand holds it when nothing else is going on; the carry is placed by this point.</summary>
        public Vector3 RestGripLocal => Warp(RawRestGrip);
        /// <summary>Where rounds go in: brought into the lower middle of the view in first person during a reload.</summary>
        public Vector3 ReloadFocusLocal => Warp(RawReloadFocus);
        /// <summary>The scope's eyepiece, for guns with a scope.</summary>
        public Vector3 ScopeEyeLocal => Warp(RawScopeEye);
        /// <summary>The gun can be looked along its iron sights (the aim button brings <see cref="SightEyeLocal"/> to the eye).</summary>
        public virtual bool HasSights => false;
        /// <summary>Where the eye goes to look along the iron sights (raw): on the line over the rear notch and the front post.</summary>
        protected virtual Vector3 RawSightEye => Vector3.zero;
        public Vector3 SightEyeLocal => Warp(RawSightEye);
        /// <summary>The own first-person copy only: 0..1 how far it is brought up to look along the sights (parts in the way can step aside).</summary>
        public virtual void SetSighting(float t) { }
        /// <summary>
        /// How far forward of the origin the butt plate sits (m) when the stock is cut short. The holder puts the butt
        /// plate on the shoulder, so the whole gun, and the hands on it, come that much closer to the body.
        /// </summary>
        public float ButtZ => RawButtZ * BodyScale;

        /// <summary>Moves the moving parts for this state of the handling and says where the hands go (gun space, as drawn).</summary>
        public void Pose(GunHandling h, GunSettings s, out Hand right, out Hand left)
        {
            RawPose(h, s, out right, out left);
            right.point = Warp(right.point);
            left.point = Warp(left.point);
        }

        /// <summary>Puts a moving part at a raw position (it is drawn at its warped place, scaled like the body).</summary>
        protected void Place(Transform part, Vector3 raw) => part.localPosition = Warp(raw);

        /// <summary>Marks a child as a moving part (a magazine, a pump, a bolt, a round in the hand).</summary>
        protected Transform Moving(Transform part)
        {
            _moving.Add(part);
            return part;
        }
        /// <summary>How full it is (0..1), for guns that show it.</summary>
        public virtual void SetAmmo(float fraction) { }

        protected abstract void Build(bool castShadows);

        protected static Material Wood, Steel, Cover, Rubber, Glass, Salt, Bottle, Brass, FlashMat;
        static Mesh _flashMesh;
        static Texture2D _grain;
        static readonly HashSet<Mesh> Warped = new HashSet<Mesh>();

        readonly List<Transform> _moving = new List<Transform>();
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
            // drawn compact: the fixed parts' meshes are warped once (they are shared), the moving parts are scaled as a whole
            foreach (Transform c in root.transform)
            {
                if (gun._moving.Contains(c)) { c.localScale = gun.Scale3; continue; }
                foreach (var mf in c.GetComponentsInChildren<MeshFilter>(true)) gun.WarpMesh(mf.sharedMesh);
            }
            gun._muzzle = new GameObject("Muzzle").transform;
            gun._muzzle.SetParent(root.transform, false);
            gun._muzzle.localPosition = gun.MuzzleLocal;
            gun._flash = Part(gun._muzzle, "Flash", _flashMesh, FlashMat, false).gameObject;
            gun._flash.SetActive(false);
            gun.SetAmmo(1f);
            return gun;
        }

        void WarpMesh(Mesh m)
        {
            if (m == null || Warped.Contains(m) || (Scale3 == Vector3.one && BarrelExtra == 0f)) return;
            Warped.Add(m);
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = Warp(v[i]);
            m.vertices = v;
            m.RecalculateNormals();      // stretched unevenly: the faces turn (every flat face has its own vertices)
            m.RecalculateBounds();
        }

        /// <summary>The gas puff at the muzzle: a soft white burst, turned and sized a little differently every shot.</summary>
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
            Bottle = Mat(lit, new Color(0.3f, 0.41f, 0.39f), 0.3f, 0.5f);       // a painted CO2 bottle, muted teal
            Brass = Mat(lit, new Color(0.74f, 0.57f, 0.3f), 0.6f, 0.5f);
            FlashMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { hideFlags = HideFlags.DontSave };
            FlashMat.SetColor("_BaseColor", new Color(0.93f, 0.96f, 1f));

            // the gas puff: a soft, lumpy round burst facing forward and two crossed plumes along the bore
            var flash = new ProfileMesh { uvScale = 1f };
            var puff = new List<Vector2>();
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f, r = i % 2 == 0 ? 0.034f : 0.025f;
                puff.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
            flash.Flat(puff, new Vector3(0f, 0f, 0.01f), Vector3.right, Vector3.up, true);
            var plume = Zy(0f, 0f, 0.02f, 0.02f, 0.05f, 0.026f, 0.07f, 0.012f, 0.072f, -0.012f, 0.05f, -0.026f, 0.02f, -0.02f);
            flash.Flat(plume, Vector3.zero, Vector3.forward, Vector3.up, true);
            flash.Flat(plume, Vector3.zero, Vector3.forward, Vector3.right, true);
            _flashMesh = flash.ToMesh("GunPuff");
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
