using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>Eye drawings of the v5 flat eyes. No longer used by the face (kept so old scenes still load).</summary>
    public enum SlugEyeShape { Dot, TallOval, WideOval, HalfLid, DotWithRim, Line }

    /// <summary>Body + face style. V4Round = the rounded rear-up body with small round 3D eyes, faint cheeks and a tiny mouth (v4 look).</summary>
    public enum SlugBodyStyle { Dignified, V4Round }

    /// <summary>Realistic 3D eyeballs (v6). None = the flat painted face (v7).</summary>
    public enum SlugEyeKind { None, Reptile, Frog, Octopus, Fish, Membrane, LidFolds }

    /// <summary>
    /// Procedural slug whose look is driven by one slider per element, plus a master slider.
    /// Every slider uses the same scale: 1.0 = the dignified "pop" character, 0.0 = close to a real slug.
    /// The master slider (<see cref="pop"/>) overwrites all element sliders when it is moved;
    /// moving an element slider changes only that element.
    ///
    /// Body at 1.0: a short, dignified slug with a gentle S-curve and the head lifted up in front.
    /// Face (v7): painted flat on the skin by the body shader - small round eyes set apart, a lighter muzzle ellipse
    /// with two tiny nostrils, and a straight one-line mouth. It rides the body wave exactly.
    /// Crawl: the whole body breathes in length (stretch / shorten) and the front swells when it shortens, plus a
    /// travelling ripple. Every deformation is limited by the local thickness, so the tail can never turn inside out.
    /// Ears and hands follow the skin point they grow from; the tentacles also sway softly.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public class SlugPop : MonoBehaviour
    {
        [Header("一括（動かすと下の全スライダーを同じ値に揃える）")]
        [Range(0f, 1f)] public float pop = 1f;

        [Header("要素ごと（1.0 = ポップ / 0.0 = 本物寄り）")]
        [Range(0f, 1f), Tooltip("短めで威厳のある体（緩やかなS字・持ち上げた頭）↔細長く低い本物寄り。背中の隆起・体表のしわ・伸び縮みもこれに連動")] public float body = 1f;
        [Range(0f, 1f), Tooltip("縞模様（濃さ）↔まだら模様")] public float pattern = 1f;
        [Range(0f, 1f), Tooltip("苔色↔彩度とコントラストを下げたオリーブ褐色")] public float color = 1f;
        [Range(0f, 1f), Tooltip("目の大きさ（小さな黒い丸目）")] public float eyes = 1f;
        [Tooltip("体と顔のスタイル。V4Round = v4 の 0.5 の個体（丸い立ち上がり体型・小さな丸い目・薄いほっぺ・小さな口）")] public SlugBodyStyle bodyStyle = SlugBodyStyle.Dignified;
        [Tooltip("立体の眼球（None のときは体の表面に描く平面の顔）")] public SlugEyeKind eyeKind = SlugEyeKind.None;
        [Tooltip("（v5 の平面の目。現在は使われない）")] public SlugEyeShape eyeStyle = SlugEyeShape.Dot;
        [Range(0f, 1f), Tooltip("触角の長さ。1.0 = 短い、0.0 = 長い")] public float tentacleLength = 1f;
        [Range(0f, 1f), Tooltip("触角の太さ。1.0 = 太い、0.0 = 細い")] public float tentacleThickness = 1f;
        [Range(0f, 1f), Tooltip("触角の向き。1.0 = 後ろへ流れる、0.5 = ほぼ真上（少し外向き）、0.0 = 前向き")] public float tentacleDirection = 1f;
        [Range(0f, 1f), Tooltip("口（まっすぐな一本線）。0 でなし")] public float expression = 1f;
        [Range(0f, 1f), Tooltip("手の長さ。1.0 = 短い円錐形、0.0 = さらに短い")] public float hands = 1f;
        [Range(0f, 1f), Tooltip("陰影。1.0 = トゥーン2段のくっきりした影、0.0 = なめらかなグラデーション")] public float shading = 1f;
        [Range(0f, 1f), Tooltip("輪郭線。1.0 = 太い、0.5 = 細い、0.0 = なし")] public float outline = 1f;

        [Header("その他")]
        [Range(0f, 1f), Tooltip("0 = 手をしまう、1 = 手を出す")] public float handsExtend = 1f;
        [Range(0.3f, 0.9f), Tooltip("手の付け根の位置（体の長さ方向、0 = 尾、1 = 頭）。頭寄りほど高い（立ち上がった胸のあたり）")] public float handT = 0.6f;
        [Range(0f, 1f), Tooltip("手の付け根を脇腹の真横からどれだけ上へずらすか")] public float handRaise = 0f;
        [Range(0f, 1f), Tooltip("腕を前へ伸ばす量。1 = 少し長く、胸の前へ向く（両手で道具を持つ）")] public float handReach = 0f;
        [Range(0f, 0.9f), Tooltip("坂で上半身を起こすときに曲げる位置（体の長さ方向、0 = 尾、1 = 頭）。高いほど首から上だけが起きる")] public float uprightPivotT = 0.65f;
        [Tooltip("伏せたときに頭を下げる高さ（m）。頭はそのまま真下へ下がり、首が縮む")] public float proneDrop = 0.2f;
        [Tooltip("伏せたときに縮む範囲（体の長さ方向、0 = 尾、1 = 頭）。x より尾側は下がらず、y より頭側はまるごと下がる")]
        public Vector2 proneDropRange = new Vector2(0.35f, 0.8f);
        [Range(0f, 0.35f), Tooltip("伏せたときに手の付け根を頭のほうへずらす量（体の長さ方向）")] public float proneHandShift = 0.2f;
        [Tooltip("頭を回す（背中を覗く）ときにねじれる範囲（体の長さ方向）。x より尾側は回らず、y より頭側はまるごと回る")]
        public Vector2 headTwistRange = new Vector2(0.62f, 0.82f);
        [Tooltip("足（手と同じ円錐形）を、腹が地面に付くあたりの両脇に生やす")] public bool feet = true;
        [Range(0f, 0.6f), Tooltip("足の付け根の位置（体の長さ方向、0 = 尾、1 = 頭）")] public float footT = 0.3f;
        [Range(0.4f, 1f), Tooltip("おなか（地面に付いている後ろ側）の長さの倍率。1 = そのまま、小さいほど胸から尾までが短い")] public float bellyLength = 1f;
        [Range(0f, 1f), Tooltip("おなかの平たさ。0 = 丸い、1 = 平らな足の裏がぺたっと地面に付き、ふちがはっきりする")] public float soleSquare = 0f;
        [Range(0f, 1f), Tooltip("足のふち（おなかと脇腹の境目）のひだ。這う動きに合わせて少し波打ち、ひだの線が入る")] public float footFrill = 0f;
        [Tooltip("向きを変えたときに後ろ側が遅れて曲がる範囲（体の長さ方向、0 = 尾、1 = 頭）。x より尾側は全部、y より頭側は曲がらない")]
        public Vector2 turnRange = new Vector2(0.15f, 0.7f);
        [Tooltip("Slug/Toon（ビルドに含めるため参照を持つ）")] public Shader shader;

        [SerializeField, HideInInspector] float _appliedPop = 1f;
        [SerializeField, HideInInspector] bool _migrated;

        GameObject _visual;
        readonly List<Object> _owned = new List<Object>();
        Mesh _sphere;
        Material _body, _app, _hand, _eye;
        Transform _handL, _handR;
        Vector3 _faceCenter, _faceFwd, _faceUp;
        float _tailT = 0.05f;
        Vector3 _tailPivot;
        float _bodyLength;
        const float FaceT = 0.88f;

        // upright bend of the lifted front (see SetUpright)
        float _uprightDeg;
        Vector3 _uprightPivot;
        Vector2 _uprightRange = new Vector2(0f, 1f);
        // the rear trailing behind a turn (see SetTurn)
        float _turnDeg;
        Vector3 _turnPivot;
        Renderer[] _renderers = new Renderer[0];
        // hand tips (body space, rest pose) and where the hands grow along the body
        Vector3 _handTipL, _handTipR;
        float _handTipT;
        bool _hasHands;
        // hand roots on the skin (body space, rest pose): point, outward normal, and the ring they sit on (for the crawl)
        struct SkinPoint { public Vector3 point, normal, pivot; public float t, radius; }
        SkinPoint _rootL, _rootR, _rootLProne, _rootRProne;
        // lying down (the head sinks) and the head turned round on the neck (see SetProne, SetHeadTwist)
        float _prone, _twistDeg;
        Vector3 _twistPivot;
        // tentacle roots (body space, rest pose): where they leave the skin, their direction and thickness
        readonly Vector3[] _earRoot = new Vector3[2], _earAxis = new Vector3[2];
        float _earRadius, _earLength;
        SlugMeshFactory.BodyShape _shape;

        /// <summary>Middle of the face, in world space. Used to aim close-up cameras.</summary>
        public Vector3 FaceCenterWorld => _visual != null ? _visual.transform.TransformPoint(Bend(_faceCenter, FaceT)) : transform.position;
        public Vector3 FaceForwardWorld => _visual != null ? _visual.transform.TransformDirection(BendDir(_faceFwd, FaceT)) : transform.forward;
        public Vector3 FaceUpWorld => _visual != null ? _visual.transform.TransformDirection(BendDir(_faceUp, FaceT)) : transform.up;
        public bool HasHands => _hasHands && _visual != null;
        /// <summary>Tip of the left (side -1) or right (+1) hand in world space, following the upright bend.</summary>
        public Vector3 HandTipWorld(float side) => _visual != null
            ? _visual.transform.TransformPoint(Bend(side < 0f ? _handTipL : _handTipR, _handTipT)) : transform.position;

        /// <summary>
        /// Where the left (-1) / right (+1) arm grows from the skin, in world space, moving with the skin exactly as the
        /// shader moves it (crawl stretch, ripple, upright bend). Arms drawn from here stay attached to the body.
        /// </summary>
        public Vector3 HandRootWorld(float side)
        {
            if (_visual == null || !_hasHands) return transform.position;
            var r = Root(side);
            return _visual.transform.TransformPoint(Bend(r.point + SkinOffset(r), r.t));
        }

        /// <summary>
        /// Where a body-space point riding the skin at <paramref name="t"/> is in the world right now: moved as the shader
        /// moves the skin there (crawl stretch, front swelling, ripple, upright bend, turn). For hitboxes and the like.
        /// </summary>
        public Vector3 FollowPointWorld(Vector3 pointBody, float t, Vector3 normalBody)
        {
            if (_visual == null || _shape == null) return transform.TransformPoint(pointBody);
            _shape.RingInfo(Mathf.Clamp01(t), out var pivot, out float radius);
            var sp = new SkinPoint { point = pointBody, normal = normalBody, pivot = pivot, t = t, radius = radius };
            return _visual.transform.TransformPoint(Bend(pointBody + SkinOffset(sp), t));
        }

        /// <summary>The outward skin normal at the arm root, in world space.</summary>
        public Vector3 HandRootNormalWorld(float side)
        {
            if (_visual == null || !_hasHands) return transform.right * Mathf.Sign(side);
            var r = Root(side);
            return _visual.transform.TransformDirection(BendDir(r.normal, r.t));
        }

        /// <summary>The arm root, moved up toward the head as the slug lies down.</summary>
        SkinPoint Root(float side)
        {
            var a = side < 0f ? _rootL : _rootR;
            if (_prone <= 0f) return a;
            var b = side < 0f ? _rootLProne : _rootRProne;
            float k = _prone;
            return new SkinPoint
            {
                point = Vector3.Lerp(a.point, b.point, k), normal = Vector3.Slerp(a.normal, b.normal, k),
                pivot = Vector3.Lerp(a.pivot, b.pivot, k), t = Mathf.Lerp(a.t, b.t, k), radius = Mathf.Lerp(a.radius, b.radius, k),
            };
        }

        /// <summary>How far the slug lies down (0 = up, 1 = down).</summary>
        public float Prone => _prone;

        /// <summary>
        /// Lies the slug down by <paramref name="amount"/> (0..1): the head sinks straight down by <see cref="proneDrop"/>
        /// (the neck squashes; the face still looks ahead) and the arm roots move up toward it.
        /// </summary>
        public void SetProne(float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (Mathf.Abs(amount - _prone) < 1e-4f) return;
            _prone = amount;
            SetUpright(_uprightDeg);
        }

        /// <summary>Turns the head round on the neck by this many degrees (+ = to the right), the body staying as it is.</summary>
        public void SetHeadTwist(float degrees)
        {
            if (Mathf.Abs(degrees - _twistDeg) < 1e-3f) return;
            _twistDeg = degrees;
            SetUpright(_uprightDeg);
        }

        /// <summary>
        /// Where a part made with <see cref="GearFollower"/>(t, attach, normal) shows a body-space point right now, in the
        /// visual's space (it moves rigidly with the skin at the attach point). For posing gear by hand.
        /// </summary>
        public Vector3 FollowerLocal(Vector3 pointBody, float t, Vector3 attach, Vector3 normal)
        {
            if (_shape == null) return pointBody;
            _shape.RingInfo(Mathf.Clamp01(t), out var pivot, out float radius);
            var sp = new SkinPoint { point = attach, normal = normal, pivot = pivot, t = t, radius = radius };
            return Bend(pointBody + SkinOffset(sp), t);
        }

        /// <summary>How a <see cref="GearFollower"/> part at <paramref name="t"/> is turned right now (visual space).</summary>
        public Quaternion FollowerRotation(float t) => Quaternion.LookRotation(BendDir(Vector3.forward, t), BendDir(Vector3.up, t));

        /// <summary>The hands' material (colour, toon shading, outline), to dress arms made elsewhere.</summary>
        public Material HandMaterial => _hand;

        /// <summary>CPU copy of BodyOffset in SlugToonInput.hlsl, for a point that rides on the skin.</summary>
        Vector3 SkinOffset(SkinPoint p)
        {
            if (_body == null) return Vector3.zero;
            float time = Shader.GetGlobalFloat("_SlugFreeze") > 0.5f ? Shader.GetGlobalFloat("_SlugTimeValue") : Time.timeSinceLevelLoad;
            float stretch = _body.GetFloat("_Stretch"), speed = _body.GetFloat("_WaveSpeed"), phase0 = _body.GetFloat("_WavePhase");
            float s = 1f + stretch * Mathf.Sin(2f * Mathf.PI * speed * time + phase0);
            float g = 2f * HlslSmooth(0.35f, 0.95f, p.t);
            float k = Mathf.Clamp(1f + (1f / Mathf.Sqrt(s) - 1f) * g, _body.GetFloat("_RadiusMin"), _body.GetFloat("_RadiusMax"));
            Vector3 off = (s - 1f) * (p.pivot - _tailPivot) + (p.point - p.pivot) * (k - 1f);
            float w = 1f - HlslSmooth(_body.GetFloat("_WaveHeadFade"), 1f, p.t);
            float ph = 2f * Mathf.PI * (_body.GetFloat("_WaveFreq") * p.t - speed * time) + phase0;
            float d = Mathf.Sin(ph) * _body.GetFloat("_WaveAmp") * w;
            if (p.radius > 0f) d = Mathf.Clamp(d, -0.4f * p.radius, 0.25f * p.radius);
            return off + p.normal * d;
        }

        static float HlslSmooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Pitches the lifted front of the body forward by this many degrees (bending, not rotating the whole slug),
        /// so it stays upright while the whole slug is tilted nose-up on a slope.
        /// </summary>
        public void SetUpright(float degrees)
        {
            _uprightDeg = degrees;
            float a = degrees * Mathf.Deg2Rad;
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                var m = r.sharedMaterial;
                m.SetFloat("_UprightAngle", a);
                m.SetVector("_UprightPivot", _uprightPivot);
                m.SetVector("_UprightRange", new Vector4(_uprightRange.x, _uprightRange.y, 0f, 0f));
                m.SetFloat("_DropAmount", proneDrop * _prone);
                m.SetVector("_DropRange", new Vector4(proneDropRange.x, Mathf.Max(proneDropRange.y, proneDropRange.x + 1e-3f), 0f, 0f));
                m.SetFloat("_TwistAngle", _twistDeg * Mathf.Deg2Rad);
                m.SetVector("_TwistPivot", _twistPivot);
                m.SetVector("_TwistRange", new Vector4(headTwistRange.x, Mathf.Max(headTwistRange.y, headTwistRange.x + 1e-3f), 0f, 0f));
            }
        }

        /// <summary>
        /// Lets the rear trail behind a turn: the front has turned <paramref name="lagDegrees"/> further (clockwise seen
        /// from above) than the tail. The rear swings back about the vertical axis through <paramref name="pivotWorld"/>
        /// (where the slug turns), fully at the tail and fading out toward the chest, so the body bends instead of the
        /// tail sliding around rigidly.
        /// </summary>
        public void SetTurn(float lagDegrees, Vector3 pivotWorld)
        {
            _turnDeg = lagDegrees;
            if (_visual != null) _turnPivot = _visual.transform.InverseTransformPoint(pivotWorld);
            float a = -lagDegrees * Mathf.Deg2Rad;
            var range = new Vector4(turnRange.x, Mathf.Max(turnRange.y, turnRange.x + 1e-3f), 0f, 0f);
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                var m = r.sharedMaterial;
                m.SetFloat("_TurnAngle", a);
                m.SetVector("_TurnPivot", _turnPivot);
                m.SetVector("_TurnRange", range);
            }
        }

        /// <summary>How far the rear is swung back at t (radians about body y, same as TurnBend in the shader).</summary>
        float TurnAngleAt(float t)
        {
            float x = Mathf.Clamp01((t - turnRange.x) / Mathf.Max(turnRange.y - turnRange.x, 1e-3f));
            return -_turnDeg * Mathf.Deg2Rad * (1f - x * x * (3f - 2f * x));
        }

        Vector3 TurnDir(Vector3 v, float t)
        {
            float a = TurnAngleAt(t);
            if (Mathf.Abs(a) < 1e-5f) return v;
            float s = Mathf.Sin(a), c = Mathf.Cos(a);
            return new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
        }

        /// <summary>Body-space point after all the bends (lying down, the head's turn, upright, then the turn), as the shader moves it.</summary>
        Vector3 Bend(Vector3 p, float t) => TurnDir(Upright(Twist(Drop(p, t), t), t) - _turnPivot, t) + _turnPivot;
        Vector3 BendDir(Vector3 v, float t) => TurnDir(UprightDir(TwistDir(v, t), t), t);

        /// <summary>Same as ProneDrop in the shader.</summary>
        Vector3 Drop(Vector3 p, float t)
        {
            if (_prone <= 0f) return p;
            float x = Mathf.Clamp01((t - proneDropRange.x) / Mathf.Max(proneDropRange.y - proneDropRange.x, 1e-3f));
            p.y -= proneDrop * _prone * x * x * (3f - 2f * x);
            return p;
        }

        /// <summary>Same as HeadTwist in the shader.</summary>
        Vector3 Twist(Vector3 p, float t) => TwistDir(p - _twistPivot, t) + _twistPivot;

        Vector3 TwistDir(Vector3 v, float t)
        {
            if (Mathf.Abs(_twistDeg) < 1e-3f) return v;
            float x = Mathf.Clamp01((t - headTwistRange.x) / Mathf.Max(headTwistRange.y - headTwistRange.x, 1e-3f));
            float a = _twistDeg * Mathf.Deg2Rad * x * x * (3f - 2f * x);
            float s = Mathf.Sin(a), c = Mathf.Cos(a);
            return new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
        }

        float UprightAngleAt(float t)
        {
            float x = Mathf.Clamp01((t - _uprightRange.x) / Mathf.Max(_uprightRange.y - _uprightRange.x, 1e-4f));
            return _uprightDeg * Mathf.Deg2Rad * x * x * (3f - 2f * x);
        }

        /// <summary>Body-space point after the upright bend (same as UprightBend in the shader).</summary>
        Vector3 Upright(Vector3 p, float t) => UprightDir(p - _uprightPivot, t) + _uprightPivot;

        Vector3 UprightDir(Vector3 v, float t)
        {
            float a = UprightAngleAt(t);
            if (Mathf.Abs(a) < 1e-5f) return v;
            float s = Mathf.Sin(a), c = Mathf.Cos(a);
            return new Vector3(v.x, v.y * c - v.z * s, v.y * s + v.z * c);
        }
        /// <summary>The tail end in world space (rest pose). Used to aim a camera at the tail.</summary>
        public Vector3 TailWorld => _visual != null ? _visual.transform.TransformPoint(Bend(_tailPivot, 0f)) : transform.position;
        public float BodyLength => _bodyLength;

        // ------------------------------------------------------------------ lifecycle
        void Reset() { shader = Shader.Find("Slug/Toon"); }
        void OnEnable() { Schedule(); }
        void OnValidate() { Schedule(); }
        void OnDisable() { Cleanup(true); }

        void Schedule()
        {
            // the prefab asset itself cannot hold the generated parts: they would be left as orphans at the origin
            if (!gameObject.scene.IsValid()) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // creating / destroying objects is not allowed inside OnValidate, so defer
                UnityEditor.EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Rebuild(); };
                return;
            }
#endif
            Rebuild();
        }

        /// <summary>Sets the master slider and every element slider to the same value.</summary>
        public void SetAll(float value)
        {
            value = Mathf.Clamp01(value);
            pop = _appliedPop = value;
            body = pattern = color = eyes = value;
            tentacleLength = tentacleThickness = tentacleDirection = value;
            expression = hands = shading = outline = value;
            _migrated = true;
        }

        /// <summary>The master slider was moved (or the data predates the element sliders): copy it into every element.</summary>
        void ResolveMaster()
        {
            if (!_migrated || !Mathf.Approximately(pop, _appliedPop)) SetAll(pop);
        }

        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        void Cleanup(bool materialsToo)
        {
            if (_visual != null) Kill(_visual);
            _visual = null;
            foreach (var o in _owned) Kill(o);
            _owned.Clear();
            if (materialsToo)
            {
                Kill(_sphere); _sphere = null;
                foreach (var m in new[] { _body, _app, _hand, _eye }) Kill(m);
                _body = _app = _hand = _eye = null;
            }
        }

        // ------------------------------------------------------------------ helpers
        static float Mix(float atZero, float atOne, float x) => Mathf.Lerp(atZero, atOne, x);
        static float Ease(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        static Color Darken(Color c, float v)
        {
            Color.RGBToHSV(c, out float h, out float s, out float val);
            return Color.HSVToRGB(h, s * 0.9f, val * v);
        }

        T Own<T>(T o) where T : Object { o.hideFlags = HideFlags.DontSave; _owned.Add(o); return o; }

        Material NewMaterial()
        {
            var sh = shader != null ? shader : Shader.Find("Slug/Toon");
            return new Material(sh) { hideFlags = HideFlags.DontSave };
        }

        GameObject Part(string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(_visual.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (mat != null) mat.SetVector("_PartOffset", pos);     // the upright bend needs body-space positions
            return go;
        }

        /// <summary>
        /// A copy of an appendage material that moves rigidly with the crawl at the given skin point:
        /// the same displacement the body shader gives that point (stretch, front swelling, ripple), limited by the local thickness.
        /// </summary>
        Material Follower(SlugMeshFactory.BodyShape shape, Material template, float t, Vector3 attachPoint, Vector3 normalBody)
        {
            shape.RingInfo(Mathf.Clamp01(t), out var pivot, out var radius);
            var m = Own(new Material(template));
            m.SetFloat("_FollowMode", 1f);
            m.SetFloat("_FollowT", t);
            m.SetVector("_FollowNormal", normalBody);
            m.SetVector("_FollowPivot", new Vector4(pivot.x, pivot.y, pivot.z, 0f));
            var rad = attachPoint - pivot;
            m.SetVector("_FollowRadial", new Vector4(rad.x, rad.y, rad.z, radius));
            return m;
        }

        /// <summary>Tangent frame (outward axis, up axis) on a surface point.</summary>
        static void DecalFrame(Vector3 n, Vector3 upHint, out Vector3 r, out Vector3 u)
        {
            u = (upHint - n * Vector3.Dot(upHint, n)).normalized;
            r = Vector3.Cross(u, n).normalized;
            if (r.x < 0f) r = -r;                 // +x = the outer side of the right eye
        }

        static Vector3 MirrorX(Vector3 v) => new Vector3(-v.x, v.y, v.z);

        /// <summary>Rotates a mesh about its origin (vertices and normals). The result keeps the body-space orientation.</summary>
        static Mesh Rotated(Mesh src, Quaternion q, string name)
        {
            var v = src.vertices; var n = src.normals;
            for (int i = 0; i < v.Length; i++) { v[i] = q * v[i]; n[i] = q * n[i]; }
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.vertices = v; m.normals = n; m.uv = src.uv; m.triangles = src.triangles;
            m.RecalculateBounds();
            return m;
        }

        // ------------------------------------------------------------------ the build
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            ResolveMaster();
            Cleanup(false);
            _hasHands = false;
            // a script reload forgets _visual, so an older hidden Visual can be left behind: remove it too
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                if (c.name == "Visual" && (c.hideFlags & HideFlags.DontSaveInEditor) != 0) Kill(c);
            }
            if (_body == null) { _body = NewMaterial(); _app = NewMaterial(); _hand = NewMaterial(); _eye = NewMaterial(); }
            if (_sphere == null) { _sphere = SlugMeshFactory.Sphere("Sphere", 1f, 26, 20); _sphere.hideFlags = HideFlags.DontSave; }

            _visual = new GameObject("Visual") { hideFlags = HideFlags.DontSave };
            _visual.transform.SetParent(transform, false);

            // ---------------- body: short dignified slug (1.0) <-> long low real slug (0.0)
            float[] ts = { 0f, 0.15f, 0.35f, 0.55f, 0.7f, 0.85f, 1f };
            // 1.0: the torso keeps its thickness to a small pointed tail; smooth back line
            float[] rx1 = { 0.13f, 0.15f, 0.155f, 0.16f, 0.16f, 0.155f, 0.15f };
            float[] ry1 = { 0.115f, 0.135f, 0.14f, 0.145f, 0.145f, 0.14f, 0.135f };
            // 0.0: slender and low; hump (mantle) behind the head; narrow head and tail
            float[] rx0 = { 0.035f, 0.085f, 0.105f, 0.12f, 0.115f, 0.095f, 0.07f };
            float[] ry0 = { 0.028f, 0.062f, 0.078f, 0.105f, 0.112f, 0.08f, 0.058f };
            var keys = new ProfileKey[ts.Length];
            for (int i = 0; i < ts.Length; i++)
                keys[i] = new ProfileKey(ts[i], Mathf.Lerp(rx0[i], rx1[i], body), Mathf.Lerp(ry0[i], ry1[i], body));

            var spec = new BodySpec
            {
                length = Mix(1.7f, 1.15f, body),           // shorter than v6 (1.6)
                rings = 44,
                radial = Mathf.RoundToInt(Mix(16f, 12f, body)),
                bottomFlat = Mix(0.35f, 0.5f, body),
                baseFlare = 0f,
                capTail = Mix(0.42f, 0.17f, body),         // a small, thin tail point
                tailPow = Mix(1f, 1.9f, body),
                capHead = Mix(0.16f, 0.14f, body),
                tailPointy = Mix(0.78f, 1f, body),
                endBias = 0.45f,
                bendDeg = Mix(30f, 98f, body),             // the head is lifted a little more than v6 (84 deg)
                bendStart = Mix(0.55f, 0.42f, body),
                sDeg = Mix(0f, 9f, body),                  // gentle S-curve in side view
                sCycles = 1f,
                keel = Mix(0.35f, 0f, body),
                keelHead = 0f,
                keys = keys
            };
            if (bodyStyle == SlugBodyStyle.V4Round)
            {
                // the v4 body: round, rear-up, 1.1 long
                spec = new BodySpec
                {
                    // (more rings and sides than v4 had: the flat sole needs them for its rounded edge, the foot fringe to ripple)
                    length = 1.1f, rings = 40, radial = 16, bottomFlat = 0.6f, capTail = 0.2f, capHead = 0.2f, tailPointy = 0.3f, endBias = 0.5f,
                    bendDeg = 88f, bendStart = 0.28f,
                    keys = new[] { new ProfileKey(0f, 0.09f, 0.08f), new ProfileKey(0.2f, 0.15f, 0.13f), new ProfileKey(0.5f, 0.17f, 0.16f), new ProfileKey(0.8f, 0.17f, 0.16f), new ProfileKey(1f, 0.16f, 0.16f) }
                };
            }
            spec.rearScale = bellyLength;      // a shorter belly: only the part on the ground, the lifted front is unchanged
            spec.soleSquare = soleSquare;
            spec.baseFlare = Mathf.Max(spec.baseFlare, 0.14f * footFrill);   // the fringe spreads out a little, like a skirt
            var shape = SlugMeshFactory.Body("Body", spec);
            Own(shape.mesh);
            _shape = shape;
            _bodyLength = spec.length;
            shape.RingInfo(0f, out _tailPivot, out _);
            // the upright bend pivots at the chest: the neck and head stand up over it on a slope
            float pivotT = Mathf.Max(uprightPivotT, spec.bendStart);
            shape.RingInfo(pivotT, out _uprightPivot, out _);
            _uprightRange = new Vector2(pivotT - 0.08f, Mathf.Min(pivotT + 0.2f, 1f));
            // the head turns on the neck about the vertical axis through the middle of the head
            _twistPivot = shape.FrameAt(0.88f).center;

            // ---------------- face: painted flat on the skin
            float headT = 0.88f;
            Ring fr = shape.FrameAt(headT);
            float tilt = shape.Phi(headT) * 0.8f;               // the face looks forward as the head is lifted
            Vector3 faceF = fr.forward * Mathf.Cos(tilt) - fr.up * Mathf.Sin(tilt);
            Vector3 faceU = fr.up * Mathf.Cos(tilt) + fr.forward * Mathf.Sin(tilt);
            Vector3 faceR = fr.right;
            Vector3 origin = fr.center + faceU * (fr.ry * 0.05f);
            System.Func<float, float, Vector3> dirOf = (yaw, pitch) =>
            {
                float y = yaw * Mathf.Deg2Rad, p = pitch * Mathf.Deg2Rad;
                return (faceF * Mathf.Cos(p) * Mathf.Cos(y) + faceR * Mathf.Cos(p) * Mathf.Sin(y) + faceU * Mathf.Sin(p)).normalized;
            };

            var face = new FaceFrame { hf = fr.rx };
            if (shape.Raycast(origin, dirOf(0f, 4f), out var pc, out var nc))
            {
                DecalFrame(nc, faceU, out var r, out var u);
                face.ok = true; face.c = new Vector3(0f, pc.y, pc.z); face.r = new Vector3(1f, 0f, 0f); face.u = u; face.n = nc;
                _faceCenter = face.c; _faceFwd = nc; _faceUp = u;
            }

            bool v4 = bodyStyle == SlugBodyStyle.V4Round;
            if (v4) face.ok = false;                       // the v4 face is made of small 3D parts, not painted
            ConfigureMaterials(spec, shape, face);
            Part("Body", shape.mesh, _body, Vector3.zero, Quaternion.identity, Vector3.one);
            if (v4) BuildV4Face(shape);

            // ---------------- 3D eyeballs (v6 option)
            if (eyeKind != SlugEyeKind.None && shape.RaycastT(origin, dirOf(Mix(48f, 40f, body), Mix(14f, 8f, body)), out var pe0, out var ne0, out var te0))
                BuildEyeballs(shape, pe0, ne0, faceU, te0);

            // ---------------- tentacles (ears): thin, short, nearly straight up, a gentle S and a soft sway
            {
                float t = 0.93f;
                float length = Mix(0.36f, 0.13f, tentacleLength);
                float baseR = Mix(0.014f, 0.045f, tentacleThickness), tipR = Mix(0.011f, 0.03f, tentacleThickness), bulb = Mix(0.02f, 0f, tentacleThickness);
                int radial = Mathf.RoundToInt(Mix(8f, 9f, tentacleThickness));
                Ring ef = shape.FrameAt(t);
                foreach (float side in new[] { -1f, 1f })
                {
                    // direction: 0 = leaning forward, 0.5 = straight UP in the world (slightly outward), 1 = swept back.
                    Vector3 a0 = BodyDir(ef, new Vector3(0.35f, 0.9f, 0.55f), side), a1 = new Vector3(0.14f * side, 1f, 0.02f), a2 = BodyDir(ef, new Vector3(0.5f, 1f, -0.2f), side);
                    Vector3 e0 = BodyDir(ef, new Vector3(0.5f, 0.55f, 1f), side), e1 = new Vector3(0.2f * side, 1f, -0.02f), e2 = BodyDir(ef, new Vector3(0.7f, 1f, -0.5f), side);
                    Vector3 a = Blend3(a0.normalized, a1.normalized, a2.normalized, tentacleDirection);
                    Vector3 b = Blend3(e0.normalized, e1.normalized, e2.normalized, tentacleDirection);
                    // spread the two roots apart when the tentacles stand up, so they do not grow from one point
                    Vector3 start = ef.center + ef.right * side * (ef.rx * 0.45f * (1f - tentacleDirection));
                    Vector3 hitN = Vector3.up;
                    float hitT = t;
                    if (!shape.RaycastT(start, a.normalized, out var pe, out hitN, out hitT)) { pe = start; hitT = t; }
                    Vector3 rootPos = pe - a.normalized * (baseR * 0.9f);
                    // same structure as the arms: a tapering cone (thick root, thin tip), slightly curved outward, no S-wiggle
                    var cone = SlugMeshFactory.ConeHand("Tentacle", length, baseR, baseR * 0.15f, 0.12f, radial);
                    Vector3 axis = ((a + b) * 0.5f).normalized;
                    var mesh = Own(Rotated(cone, Quaternion.LookRotation(axis, -ef.right * side), "Ear"));
                    Kill(cone);
                    var mat = Follower(shape, _app, hitT, pe, hitN);
                    mat.SetFloat("_SwayAmp", 0.012f);                                    // a faint sway that goes with the crawl
                    mat.SetVector("_SwayDir", new Vector4(side * 0.7f, 0f, 0.7f, 0f));
                    Part(side < 0 ? "Ear_L" : "Ear_R", mesh, mat, rootPos, Quaternion.identity, Vector3.one);
                    int ei = side < 0 ? 0 : 1;
                    _earRoot[ei] = rootPos; _earAxis[ei] = axis;
                    _earRadius = baseR; _earLength = length;
                }
            }

            // ---------------- hands: short tapering cone (thick root, thin tip), slightly curved, no fingers
            //                 feet: the same cone, low on the sides where the belly meets the ground
            {
                float handLen = Mix(0.03f, 0.08f, hands);
                float baseR = Mix(0.012f, 0.022f, hands);
                var cone = SlugMeshFactory.ConeHand("Hand", handLen, baseR, baseR * 0.22f, 0.35f, 8);
                // reach: longer arms that grow from the front of the flank and point forward, to hold a tool in front of the chest
                var armCone = handReach > 0f
                    ? SlugMeshFactory.ConeHand("Hand", handLen * (1f + 0.7f * handReach), baseR, baseR * 0.22f, 0.35f * (1f - 0.6f * handReach), 8)
                    : cone;
                Ring hf = shape.FrameAt(handT);
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 rayDir = (hf.right * side + Vector3.up * handRaise + Vector3.forward * (0.6f * handReach)).normalized;
                    Vector3 outDir = Vector3.Lerp(new Vector3(0.55f * side, -0.4f, 0.75f).normalized,      // forward, out and a little down
                                                  new Vector3(0.05f * side, -0.15f, 1f).normalized, handReach).normalized;
                    if (!shape.RaycastT(hf.center, rayDir, out var ph, out var nh, out var hT)) continue;
                    // the mesh is rotated into body-space orientation, so the crawl displacement applies to it directly
                    var handMesh = Own(Rotated(armCone, Quaternion.LookRotation(outDir, Vector3.up), "Hand"));
                    var hand = Part(side < 0 ? "Hand_L" : "Hand_R", handMesh, Follower(shape, _hand, hT, ph, nh),
                                    ph - nh * (baseR * 0.6f), Quaternion.identity, Vector3.one);
                    if (side < 0) _handL = hand.transform; else _handR = hand.transform;
                    // the tip is the vertex farthest from the root
                    Vector3 tip = Vector3.zero;
                    foreach (var v in handMesh.vertices) if (v.sqrMagnitude > tip.sqrMagnitude) tip = v;
                    if (side < 0) _handTipL = hand.transform.localPosition + tip; else _handTipR = hand.transform.localPosition + tip;
                    _handTipT = hT;
                    _hasHands = true;
                    shape.RingInfo(Mathf.Clamp01(hT), out var ringPivot, out var ringRadius);
                    var root = new SkinPoint { point = ph, normal = nh, pivot = ringPivot, t = hT, radius = ringRadius };
                    if (side < 0) _rootL = root; else _rootR = root;
                    // lying down, the arms grow from further up toward the head
                    var prone = root;
                    Ring hp = shape.FrameAt(Mathf.Min(handT + proneHandShift, 0.95f));
                    if (shape.RaycastT(hp.center, rayDir, out var pp, out var np, out var pT))
                    {
                        shape.RingInfo(Mathf.Clamp01(pT), out var pPivot, out var pRadius);
                        prone = new SkinPoint { point = pp, normal = np, pivot = pPivot, t = pT, radius = pRadius };
                    }
                    if (side < 0) _rootLProne = prone; else _rootRProne = prone;
                }
                if (feet)
                {
                    Ring ff = shape.FrameAt(footT);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Vector3 rayDir = (ff.right * side + Vector3.down * 0.35f).normalized;  // the lower flank, just above the sole
                        Vector3 outDir = new Vector3(0.7f * side, 0f, 0.5f).normalized;        // out and forward, level with the ground
                        if (!shape.RaycastT(ff.center, rayDir, out var pf, out var nf, out var fT)) continue;
                        var footMesh = Own(Rotated(cone, Quaternion.LookRotation(outDir, Vector3.up), "Foot"));
                        Part(side < 0 ? "Foot_L" : "Foot_R", footMesh, Follower(shape, _hand, fT, pf, nf),
                             pf - nf * (baseR * 0.6f), Quaternion.identity, Vector3.one);
                    }
                }
                if (armCone != cone) Kill(armCone);
                Kill(cone);
            }

            // centre the pivot on the footprint
            var b0 = shape.mesh.bounds;
            _visual.transform.localPosition = new Vector3(-b0.center.x, 0f, -b0.center.z);
            ApplyHands();
            _renderers = _visual.GetComponentsInChildren<Renderer>(true);
            SetUpright(_uprightDeg);
            _turnDeg = 0f;
            SetTurn(0f, transform.position);
            Rebuilt?.Invoke();
        }

        // ------------------------------------------------------------------ v4 face: small round eyes, faint cheeks, tiny mouth
        Material FlatTemplate(Color c, Color emission)
        {
            var m = Own(new Material(_app));
            m.SetColor("_BaseColor", c);
            m.SetColor("_ShadeColor", new Color(0.7f, 0.7f, 0.85f));
            m.SetFloat("_OutlineWidth", 0f); m.SetFloat("_RimAmount", 0f); m.SetFloat("_AmbientStrength", 0.5f);
            m.SetFloat("_PatternType", 0f); m.SetFloat("_BellyAmount", 0f); m.SetFloat("_MottleAmount", 0f);
            m.SetColor("_EmissionColor", emission);
            return m;
        }

        void BuildV4Face(SlugMeshFactory.BodyShape shape)
        {
            float headT = 0.86f, tilt = 62f * Mathf.Deg2Rad;
            Ring fr = shape.FrameAt(headT);
            Vector3 faceF = fr.forward * Mathf.Cos(tilt) - fr.up * Mathf.Sin(tilt);
            Vector3 faceU = fr.up * Mathf.Cos(tilt) + fr.forward * Mathf.Sin(tilt);
            Vector3 origin = fr.center + faceU * (fr.ry * 0.05f);
            System.Func<float, float, Vector3> dirOf = (yaw, pitch) =>
            {
                float y = yaw * Mathf.Deg2Rad, p = pitch * Mathf.Deg2Rad;
                return (faceF * Mathf.Cos(p) * Mathf.Cos(y) + fr.right * Mathf.Cos(p) * Mathf.Sin(y) + faceU * Mathf.Sin(p)).normalized;
            };
            Color bodyC = _body.GetColor("_BaseColor");
            var eyeT = FlatTemplate(new Color(0.07f, 0.06f, 0.07f), Color.black);
            var hiT = FlatTemplate(Color.white, new Color(1f, 0.97f, 0.9f) * 0.8f);
            var cheekT = FlatTemplate(Color.Lerp(bodyC, Hex("#C08A70"), 0.8f), Color.black);
            var mouthT = FlatTemplate(Darken(bodyC, 0.3f), Color.black);

            Vector3 size = Vector3.Lerp(new Vector3(0.019f, 0.019f, 0.016f), new Vector3(0.058f, 0.072f, 0.038f), Mathf.Pow(eyes, 1.6f));
            float glint = Mathf.Pow(eyes, 0.7f);
            float amount = Mathf.SmoothStep(0f, 1f, expression);

            System.Func<Vector3, Vector3, Vector3, Vector3> unused = (a, b, c) => a;
            foreach (float side in new[] { -1f, 1f })
            {
                string tag = side < 0 ? "L" : "R";
                if (shape.RaycastT(origin, dirOf(side * 34f, 6f), out var p, out var n, out var t))
                {
                    Vector3 Y = (faceU - n * Vector3.Dot(faceU, n)).normalized, X = Vector3.Cross(Y, n).normalized;
                    Vector3 c = p - n * (size.z * 0.4f);
                    Part("Eye_" + tag, Own(SlugMeshFactory.BakeEllipsoid(_sphere, "Eye", c, X, Y, n, size.x, size.y, size.z)), Follower(shape, eyeT, t, p, n), Vector3.zero, Quaternion.identity, Vector3.one);
                    float hr = 0.30f * glint * (size.x + size.y) * 0.5f;
                    if (hr > 0.0005f)
                    {
                        Vector3 hc = c + X * (0.30f * size.x) + Y * (0.34f * size.y) + n * (0.89f * size.z);
                        Part("Eye_" + tag + "_Highlight", Own(SlugMeshFactory.BakeEllipsoid(_sphere, "Highlight", hc, X, Y, n, hr, hr, hr * 0.5f)), Follower(shape, hiT, t, p, n), Vector3.zero, Quaternion.identity, Vector3.one);
                    }
                }
                float cs = 0f;   // no cheeks
                if (cs > 0.002f && shape.RaycastT(origin, dirOf(side * 58f, -14f), out var pc, out var nc, out var tc))
                {
                    Vector3 Y = (faceU - nc * Vector3.Dot(faceU, nc)).normalized, X = Vector3.Cross(Y, nc).normalized;
                    Part("Cheek_" + tag, Own(SlugMeshFactory.BakeEllipsoid(_sphere, "Cheek", pc - nc * 0.004f, X, Y, nc, cs * 1.25f, cs * 0.8f, cs * 0.3f)), Follower(shape, cheekT, tc, pc, nc), Vector3.zero, Quaternion.identity, Vector3.one);
                }
            }
            if (amount > 0.02f && shape.RaycastT(origin, dirOf(0f, -24f), out var pm, out var nm, out var tm))
            {
                Vector3 Y = (faceU - nm * Vector3.Dot(faceU, nm)).normalized, X = Vector3.Cross(Y, nm).normalized;
                var smile = Own(SlugMeshFactory.Smile("Mouth", 0.045f, 0.016f, 0.007f, 6));
                Part("Mouth", Own(SlugMeshFactory.BakeEllipsoid(smile, "MouthB", pm - nm * 0.002f, X, Y, nm, amount, amount, amount)), Follower(shape, mouthT, tm, pm, nm), Vector3.zero, Quaternion.identity, Vector3.one);
            }
        }

        // ------------------------------------------------------------------ eyeballs (v6 option)
        void BuildEyeballs(SlugMeshFactory.BodyShape shape, Vector3 p, Vector3 n, Vector3 faceU, float attachT)
        {
            DecalFrame(n, faceU, out var X, out var Y);
            Vector3 Z = n;

            float R = Mathf.Lerp(0.019f, 0.058f, Mathf.Pow(eyes, 1.6f));
            float rk = 1f, depth = 0.55f, zs = 0.8f;
            switch (eyeKind)
            {
                case SlugEyeKind.Frog: rk = 1.15f; depth = 0.35f; zs = 1.0f; break;
                case SlugEyeKind.Fish: rk = 1.2f; depth = 0.12f; zs = 1.0f; break;
                case SlugEyeKind.LidFolds: rk = 1.0f; depth = 0.5f; zs = 0.85f; break;
            }
            float Re = R * rk;
            Vector3 C = p - n * (Re * depth);

            ConfigureEyeTemplate(Re);
            foreach (float side in new[] { 1f, -1f })
            {
                Vector3 c = side > 0 ? C : MirrorX(C), x = side > 0 ? X : MirrorX(X), y = side > 0 ? Y : MirrorX(Y), z = side > 0 ? Z : MirrorX(Z);
                Vector3 pp = side > 0 ? p : MirrorX(p);
                var eyeMat = Follower(shape, _eye, attachT, pp, z);
                eyeMat.SetVector("_EyeFC", new Vector4(c.x, c.y, c.z, 0f));
                eyeMat.SetVector("_EyeFX", new Vector4(x.x, x.y, x.z, 0f));
                eyeMat.SetVector("_EyeFY", new Vector4(y.x, y.y, y.z, 0f));
                eyeMat.SetVector("_EyeFZ", new Vector4(z.x, z.y, z.z, 0f));
                var ball = Own(SlugMeshFactory.BakeEllipsoid(_sphere, "Eyeball", c, x, y, z, Re, Re, Re * zs));
                string tag = side > 0 ? "R" : "L";
                var ballGo = Part("Eye_" + tag, ball, eyeMat, Vector3.zero, Quaternion.identity, Vector3.one);
                ballGo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                if (eyeKind == SlugEyeKind.LidFolds)
                {
                    var skin = Follower(shape, _app, attachT, pp, z);
                    skin.SetFloat("_OutlineWidth", 0f);
                    skin.SetColor("_BaseColor", skin.GetColor("_BaseColor") * 0.94f);
                    var lids = new (float cut, float scale, bool lower, float turn)[]
                    {
                        (-0.05f, 1.12f, false, 5f), (0.20f, 1.23f, false, -9f), (0.42f, 1.35f, false, 11f), (0.42f, 1.14f, true, -4f),
                    };
                    int li = 0;
                    foreach (var l in lids)
                    {
                        var unit = Own(SlugMeshFactory.Lid("LidUnit", l.cut, 14, 8));
                        Vector3 yy = l.lower ? -y : y;
                        var spin = Quaternion.AngleAxis(l.turn, z);
                        var m = Own(SlugMeshFactory.BakeEllipsoid(unit, "Lid", c, spin * x, spin * yy, z, Re * l.scale, Re * l.scale, Re * l.scale * 0.9f));
                        var lidGo = Part($"Lid_{tag}{li++}", m, skin, Vector3.zero, Quaternion.identity, Vector3.one);
                        lidGo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }
        }

        void ConfigureEyeTemplate(float radius)
        {
            var m = _eye;
            m.CopyPropertiesFromMaterial(_body);
            m.SetFloat("_EyeKind", (float)(int)eyeKind);
            m.SetFloat("_EyeRad", radius);
            m.SetVector("_FaceC", Vector4.zero);       // no flat face on the eyeball
            m.SetFloat("_OutlineWidth", 0f);
            m.SetFloat("_PatternType", 0f);
            m.SetFloat("_WrinkleAmount", 0f);
            m.SetFloat("_MottleAmount", 0f);
            m.SetFloat("_BellyAmount", 0f);
            m.SetColor("_ShadeColor", new Color(0.62f, 0.62f, 0.68f));
            m.SetFloat("_AmbientStrength", 0.55f);
            switch (eyeKind)
            {
                case SlugEyeKind.Reptile: m.SetColor("_EyeIris1", Hex("#D9B43A")); m.SetColor("_EyeIris2", Hex("#5E7A2E")); break;
                case SlugEyeKind.Frog: m.SetColor("_EyeIris1", Hex("#E2B836")); m.SetColor("_EyeIris2", Hex("#8A5E1C")); break;
                case SlugEyeKind.Octopus: m.SetColor("_EyeIris1", Hex("#CC8B33")); m.SetColor("_EyeIris2", Hex("#6A3E18")); break;
                case SlugEyeKind.Fish: m.SetColor("_EyeIris1", Hex("#BFE6E2")); m.SetColor("_EyeIris2", Hex("#6A5A44")); m.SetColor("_EyeSclera", Hex("#C9D0CC")); break;
                case SlugEyeKind.Membrane: m.SetColor("_EyeIris1", Hex("#D8B440")); m.SetColor("_EyeIris2", Hex("#7C7A2C")); break;
                case SlugEyeKind.LidFolds: m.SetColor("_EyeIris1", Hex("#2A1C16")); m.SetColor("_EyeIris2", Hex("#3A2A20")); break;
            }
        }

        static Vector3 BodyDir(Ring f, Vector3 v, float side) => f.right * (v.x * side) + f.up * v.y + f.forward * v.z;

        /// <summary>Piecewise blend: x = 0 -> a, 0.5 -> b, 1 -> c.</summary>
        static Vector3 Blend3(Vector3 a, Vector3 b, Vector3 c, float x)
            => x < 0.5f ? Vector3.Lerp(a, b, x * 2f) : Vector3.Lerp(b, c, (x - 0.5f) * 2f);

        public void ApplyHands()
        {
            float s = Mathf.SmoothStep(0f, 1f, handsExtend);
            Vector3 v = Vector3.one * Mathf.Max(s, 0.0001f);
            foreach (var h in new[] { _handL, _handR })
            {
                if (h == null) continue;
                h.localScale = v;
                h.gameObject.SetActive(s > 0.001f);
            }
        }

        // ------------------------------------------------------------------ worn gear (helmet, armour)
        /// <summary>Called at the end of every rebuild: the visual (and any gear on it) was made anew, so gear is put back on.</summary>
        public event System.Action Rebuilt;

        /// <summary>Where gear is parented (body space). Null until the slug is built.</summary>
        public Transform VisualRoot => _visual != null ? _visual.transform : null;
        /// <summary>The body's shape in body space (frames along the spine, raycasts against the skin), for fitting gear.</summary>
        public SlugMeshFactory.BodyShape Shape => _visual != null ? _shape : null;

        /// <summary>Where the left (-1) / right (+1) tentacle leaves the skin, its direction, root radius and length (body space).</summary>
        public void EarRoot(float side, out Vector3 point, out Vector3 axis, out float radius, out float length)
        {
            int i = side < 0f ? 0 : 1;
            point = _earRoot[i]; axis = _earAxis[i]; radius = _earRadius; length = _earLength;
        }

        /// <summary>
        /// A material for worn gear: the body's crawl, bends and shading, with its own colour and outline, and none of the
        /// skin's own markings (no face, belly, rings, wrinkles or mottling). The caller owns it.
        /// </summary>
        public Material GearMaterial(Color baseColor)
        {
            var m = new Material(_body) { name = "Gear", hideFlags = HideFlags.DontSave };
            m.SetFloat("_EyeKind", 0f);
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_ShadeColor", new Color(0.66f, 0.66f, 0.72f));
            m.SetColor("_OutlineColor", Darken(baseColor, 0.4f));
            m.SetColor("_RimColor", Color.Lerp(baseColor, Color.white, 0.35f));
            m.SetFloat("_BellyAmount", 0f);
            m.SetFloat("_PatternType", 0f);
            m.SetFloat("_WrinkleAmount", 0f);
            m.SetFloat("_MottleAmount", 0f);
            m.SetFloat("_BumpAmp", 0f);
            m.SetVector("_FaceC", Vector4.zero);
            m.SetVector("_FaceMouth", Vector4.zero);
            m.SetFloat("_FollowMode", 0f);
            m.SetFloat("_SwayAmp", 0f);
            m.SetFloat("_SkirtAmp", 0f);
            return m;
        }

        /// <summary>A copy of a gear material that moves rigidly with the skin at one point (a helmet on the head).</summary>
        public Material GearFollower(Material template, float t, Vector3 pointBody, Vector3 normalBody)
            => _shape != null ? Follower(_shape, template, t, pointBody, normalBody) : template;

        /// <summary>
        /// Puts a piece of gear on the body (mesh in body space at <paramref name="pos"/>). It is part of the visual: it bends
        /// with the slug, and goes away when the slug is rebuilt (gear listens to <see cref="Rebuilt"/> to come back).
        /// </summary>
        public GameObject AddGear(string name, Mesh mesh, Material mat, Vector3 pos)
        {
            if (_visual == null) return null;
            var go = Part(name, mesh, mat, pos, Quaternion.identity, Vector3.one);
            _renderers = _visual.GetComponentsInChildren<Renderer>(true);
            SetUpright(_uprightDeg);
            var range = new Vector4(turnRange.x, Mathf.Max(turnRange.y, turnRange.x + 1e-3f), 0f, 0f);
            mat.SetFloat("_TurnAngle", -_turnDeg * Mathf.Deg2Rad);
            mat.SetVector("_TurnPivot", _turnPivot);
            mat.SetVector("_TurnRange", range);
            return go;
        }

        // ------------------------------------------------------------------ materials
        struct FaceFrame
        {
            public bool ok;
            public float hf;                // half width of the head at the face
            public Vector3 c, r, u, n;
        }

        static Vector4 V4(Vector3 v, float w = 0f) => new Vector4(v.x, v.y, v.z, w);

        void ConfigureMaterials(BodySpec spec, SlugMeshFactory.BodyShape shape, FaceFrame face)
        {
            // colour slider: moss at 1, dull olive-brown at 0 (lower saturation and contrast)
            Color bodyC = Color.Lerp(Hex("#5D5B47"), Hex("#6E7A4C"), color);
            Color belly = Color.Lerp(Hex("#8F8B75"), Hex("#A4AA82"), color);
            Color shade = Color.Lerp(Hex("#7A7A70"), Hex("#5E6A5C"), color);

            // pattern slider: bold green rings at 1 -> blotchy mottling at 0
            Color stripe = Hex("#4C5A36");
            stripe.a = 0.65f * Ease(0.2f, 0.9f, pattern);
            Color mottle = Hex("#34312A");
            float mottleAmount = Mathf.Pow(1f - pattern, 0.9f) * 0.85f;

            // outline slider: thick -> thin -> none (always a darker version of the body colour)
            float width = outline >= 0.5f ? Mathf.Lerp(0.0045f, 0.009f, (outline - 0.5f) * 2f) : Mathf.Lerp(0f, 0.0045f, outline * 2f);
            width = width < 0.0004f ? 0f : width;

            float L = spec.length;
            float C = 2f * Mathf.PI * 0.5f * (shape.Profile(0.5f, true) + shape.Profile(0.5f, false));
            float r = 1f - body;

            var m = _body;
            m.SetFloat("_EyeKind", 0f);
            m.SetColor("_BaseColor", bodyC);
            m.SetColor("_ShadeColor", shade);
            m.SetColor("_BellyColor", belly);
            m.SetFloat("_BellyAmount", 0.5f);
            m.SetFloat("_PatternType", 2f);
            m.SetColor("_PatternColor", stripe);
            m.SetVector("_PatternScale", new Vector4(8f, 9f, 0f, 0f));
            m.SetFloat("_PatternSize", 0.38f);
            // shading slider
            m.SetFloat("_Bands", 2f);
            m.SetFloat("_BandSoftness", Mathf.Lerp(0.3f, 0.03f, Ease(0.4f, 1f, shading)));
            m.SetFloat("_ToonSmooth", Ease(0.05f, 0.85f, 1f - shading));
            m.SetFloat("_AmbientStrength", Mix(0.5f, 0.35f, shading));
            m.SetColor("_RimColor", Color.Lerp(bodyC, Color.white, 0.45f));
            m.SetFloat("_RimAmount", Mix(0.04f, 0.2f, shading));
            m.SetColor("_OutlineColor", Darken(bodyC, 0.42f));
            m.SetFloat("_OutlineWidth", width);
            // body slider: crawl and skin
            m.SetFloat("_BodyLength", L);
            m.SetFloat("_WaveAmp", Mix(0.02f, 0.014f, body));         // ripple (limited by the local thickness)
            m.SetFloat("_WaveFreq", Mix(3.0f, 2.2f, body));
            m.SetFloat("_WaveSpeed", Mix(0.5f, 0.8f, body));
            m.SetFloat("_WaveHeadFade", Mix(0.7f, 0.9f, body));
            m.SetFloat("_Stretch", Mix(0.03f, 0.035f, body));         // whole-body stretch / shorten, small: worn gear rides on it
            m.SetVector("_TailPivot", V4(_tailPivot));
            m.SetFloat("_RadiusMin", 0.4f);                           // thickness limits: never inside out,
            m.SetFloat("_RadiusMax", 1.3f);                           // never fatter than the root
            m.SetFloat("_WrinkleAmount", Mathf.Pow(r, 1.1f));
            m.SetVector("_WrinkleScale", new Vector4(10f, 10f * L / C, 0f, 0f));
            m.SetFloat("_MottleAmount", mottleAmount);
            m.SetColor("_MottleColor", mottle);
            m.SetFloat("_MottleScale", Mix(9f, 6f, body));
            m.SetFloat("_FollowMode", 0f);
            m.SetFloat("_SwayAmp", 0f);
            // the foot fringe: on the edge of the sole, along the part lying on the ground
            m.SetFloat("_SkirtAmp", 0.02f * footFrill);
            m.SetFloat("_SkirtFreq", 12f);
            m.SetFloat("_SkirtU", Mathf.Lerp(0.2f, 0.125f, soleSquare));
            m.SetFloat("_SkirtWidth", 0.07f);
            m.SetFloat("_SkirtLines", 0.5f * footFrill);
            m.SetFloat("_SkirtEnd", Mathf.Clamp01(spec.bendStart + 0.22f));

            // ---- the face: all sizes are fractions of the head width so it scales with the body
            float hf = face.hf > 0f ? face.hf : 0.15f;
            float eyeR = hf * Mathf.Lerp(0.07f, 0.16f, eyes);                 // small round eyes
            m.SetVector("_FaceC", face.ok ? V4(face.c, 1f) : Vector4.zero);
            m.SetVector("_FaceR", V4(face.r));
            m.SetVector("_FaceU", V4(face.u));
            m.SetVector("_FaceN", V4(face.n));
            m.SetVector("_FaceEye", new Vector4(hf * 0.45f, hf * 0.30f, eyeR, 0f));              // set a little apart
            m.SetVector("_FaceMuz", new Vector4(-hf * 0.32f, hf * 0.66f, hf * 0.36f, hf * 0.04f)); // lighter ellipse, lower half
            m.SetVector("_FaceNos", new Vector4(hf * 0.13f, -hf * 0.18f, hf * 0.035f, hf * 0.05f)); // two tiny nostrils
            m.SetVector("_FaceMouth", new Vector4(-hf * 0.47f, hf * 0.30f, hf * 0.012f, Mathf.SmoothStep(0f, 1f, expression))); // straight line
            Color muzzle = Color.Lerp(belly, Color.white, 0.4f);                // brighter than the body
            m.SetColor("_FaceMuzzleColor", new Color(muzzle.r, muzzle.g, muzzle.b, 0.96f));
            m.SetColor("_FaceInkColor", new Color(0.05f, 0.045f, 0.05f));

            // tentacles follow the colour slider, hands follow the hands slider
            Appendage(_app, bodyC, belly, color);
            Appendage(_hand, bodyC, belly, hands);
        }

        void Appendage(Material a, Color bodyC, Color belly, float t)
        {
            Color c = Color.Lerp(bodyC, Color.Lerp(bodyC, belly, 0.45f), t);
            a.CopyPropertiesFromMaterial(_body);
            a.SetFloat("_EyeKind", 0f);
            a.SetColor("_BaseColor", c);
            a.SetColor("_OutlineColor", Darken(c, 0.42f));
            a.SetFloat("_BellyAmount", 0f);
            a.SetFloat("_PatternType", 0f);
            a.SetFloat("_WrinkleAmount", 0f);
            a.SetFloat("_MottleAmount", _body.GetFloat("_MottleAmount") * 0.6f);
            a.SetFloat("_MottleScale", 20f);
            a.SetVector("_FaceC", Vector4.zero);     // the face is painted on the body only
            a.SetFloat("_SkirtAmp", 0f);
            a.SetVector("_FaceMouth", Vector4.zero);
        }
    }
}
