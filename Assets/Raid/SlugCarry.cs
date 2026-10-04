using SlugDesign;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The thing in the slug's hands (<see cref="SlugBag.Holding"/>): a chunk of rock salt or a turnip held in both
    /// gloved fists in front of the chest. Taking it from the ground, the arms stretch down to where it lay and bring it
    /// up; putting it in the bag, they lift it over the right shoulder and down into the mouth of the bag on the back
    /// (it shrinks out of sight as it goes in); taking something out is the same the other way.
    /// Drawn twice over like the tools: the body's thing and arms (seen by every camera except the player's own in first
    /// person), and a first-person copy held low in the middle of the view, drawn shrunk toward the eye.
    /// Runs after the camera rig so the first-person copy follows this frame's camera.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SlugCarry : MonoBehaviour
    {
        public SlugBag bag;
        public SlugMoveController controller;
        public SlugCameraRig cameraRig;
        public SlugPop slug;
        [Tooltip("抱える位置：両腕の付け根の真ん中からのずれ（体の向き基準, m）。物の大きさの半分だけさらに前へ")] public Vector3 holdOffset = new Vector3(0f, -0.03f, 0.05f);
        [Tooltip("一人称で抱える位置（カメラ基準, m, 縮める前）。画面の下の真ん中")] public Vector3 fpHold = new Vector3(0f, -0.17f, 0.36f);
        [Tooltip("一人称で、物と腕を目に向かって縮めて描く倍率")] public float fpScale = 0.45f;
        [Tooltip("腕の太さ（付け根・手首の半径, m）")] public float armRadiusRoot = 0.022f, armRadiusHand = 0.015f;
        [Tooltip("握りこぶしの大きさ（m）")] public Vector3 fistSize = new Vector3(0.05f, 0.042f, 0.05f);

        GameObject _body, _view;
        ItemKind _kind;
        bool _has;
        SlugArmSet _arms;
        Renderer[] _bodyRenderers = new Renderer[0], _viewRenderers = new Renderer[0];

        void OnEnable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        void OnDisable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;

        void Start()
        {
            _arms = SlugArmSet.Create("Carry", slug, cameraRig.cam.transform, fpScale);
            _arms.ShowBody(false);
            _arms.ShowView(false);
        }

        void OnDestroy()
        {
            if (_body) Destroy(_body);
            _arms?.Destroy();
        }

        void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (_arms == null) return;
            bool own = camera == cameraRig.cam;
            foreach (var r in _viewRenderers) if (r) r.enabled = own;
            var mode = own && !cameraRig.ShowsBody ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var r in _bodyRenderers) if (r) r.shadowCastingMode = mode;
        }

        void Show(bool on)
        {
            if (_body && _body.activeSelf != on) _body.SetActive(on);
            if (!on && _view && _view.activeSelf) _view.SetActive(false);
            _arms.ShowBody(on);
            if (!on) _arms.ShowView(false);
        }

        /// <summary>The thing's look, made again when another kind of thing is taken.</summary>
        void Ensure(ItemKind kind)
        {
            if (_has && _kind == kind) return;
            if (_body) Destroy(_body);
            if (_view) Destroy(_view);
            _body = ItemPickup.Visual(bag.settings, kind, null, true);
            _body.name = "Carried_" + kind;
            foreach (var c in _body.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = SlugToolHolder.BodyToolLayer;
            _view = ItemPickup.Visual(bag.settings, kind, cameraRig.cam.transform, false);
            _view.name = "ViewCarried_" + kind;
            _bodyRenderers = _body.GetComponentsInChildren<Renderer>(true);
            var views = new System.Collections.Generic.List<Renderer>(_view.GetComponentsInChildren<Renderer>(true));
            views.AddRange(_arms.ViewRenderers);
            _viewRenderers = views.ToArray();
            var bodies = new System.Collections.Generic.List<Renderer>(_bodyRenderers);
            bodies.AddRange(_arms.BodyRenderers);
            _bodyRenderers = bodies.ToArray();
            _kind = kind;
            _has = true;
        }

        static float Ease(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t) => Vector3.Lerp(Vector3.Lerp(a, b, t), Vector3.Lerp(b, c, t), t);

        void LateUpdate()
        {
            if (_arms == null) return;
            if (!bag.Holding) { Show(false); return; }
            Ensure(bag.HeldKind);
            Show(true);

            float size = bag.settings.Def(bag.HeldKind).size;
            Quaternion body = controller.transform.rotation;
            Vector3 mid = (slug.HandRootWorld(-1f) + slug.HandRootWorld(1f)) * 0.5f;
            Vector3 hold = mid + body * (holdOffset + Vector3.forward * (size * 0.5f));
            Vector3 over = mid + body * new Vector3(0.12f, 0.24f, -0.04f);        // up over the right shoulder
            float u = Ease(bag.CarryProgress), scale = 1f, away = 0f;
            Vector3 pos = hold;
            switch (bag.Carry)
            {
                case SlugBag.CarryPhase.Grab:
                    pos = Vector3.Lerp(bag.GrabFrom, hold, u);
                    away = 1f - u;
                    break;
                case SlugBag.CarryPhase.Stow:
                    pos = Bezier(hold, over, bag.BagMouthWorld(), u);
                    scale = 1f - 0.6f * Ease((bag.CarryProgress - 0.7f) / 0.3f);      // into the bag
                    away = u;
                    break;
                case SlugBag.CarryPhase.TakeOut:
                    pos = Bezier(bag.BagMouthWorld(), over, hold, u);
                    scale = 0.4f + 0.6f * Ease(bag.CarryProgress / 0.3f);             // out of the bag
                    away = 1f - u;
                    break;
                default:
                    pos += Vector3.up * (0.006f * Mathf.Sin(Time.time * 3f));         // held: a slight bob
                    break;
            }
            Quaternion rot = body * Quaternion.Euler(-10f, 25f, 0f);
            bag.heldWorld = pos;
            _body.transform.SetPositionAndRotation(pos, rot);
            _body.transform.localScale = Vector3.one * (size * ScaleOf(bag.HeldKind) * scale);

            // both fists on its sides
            Vector3 side = body * Vector3.right * (size * 0.45f * scale);
            Vector3 up = body * Vector3.up;
            SlugArmSet.Build(_arms.BodyR, slug.HandRootWorld(1f), slug.HandRootNormalWorld(1f), pos + side, up,
                             armRadiusRoot, armRadiusHand, fistSize, 1f);
            SlugArmSet.Build(_arms.BodyL, slug.HandRootWorld(-1f), slug.HandRootNormalWorld(-1f), pos - side, up,
                             armRadiusRoot, armRadiusHand, fistSize, 1f);

            // ---- first person: held low in the middle of the view; while it travels it goes off toward where it
            //      really is (down to the ground, back over the shoulder), drawn shrunk toward the eye
            bool firstPerson = !cameraRig.ShowsBody;
            if (_view.activeSelf != firstPerson) _view.SetActive(firstPerson);
            _arms.ShowView(firstPerson);
            if (!firstPerson) return;
            var cam = cameraRig.cam.transform;
            Quaternion toCam = Quaternion.Inverse(cam.rotation);
            Vector3 eye = cam.position;
            float sc = fpScale;
            Vector3 MapWorld(Vector3 w) => toCam * (w - eye) * sc;
            Vector3 heldV = fpHold * sc;
            Vector3 posV = Vector3.Lerp(heldV, MapWorld(pos), away);
            Quaternion rotV = Quaternion.Euler(-15f, 20f, 0f);
            _view.transform.localPosition = posV;
            _view.transform.localRotation = rotV;
            _view.transform.localScale = Vector3.one * (size * ScaleOf(bag.HeldKind) * scale * sc);
            Vector3 sideV = Vector3.right * (size * 0.45f * scale * sc);
            float thick = sc * 0.8f;
            SlugArmSet.Build(_arms.ViewR, MapWorld(slug.HandRootWorld(1f)), toCam * slug.HandRootNormalWorld(1f), posV + sideV, Vector3.up,
                             armRadiusRoot * thick, armRadiusHand * thick, fistSize * sc, sc);
            SlugArmSet.Build(_arms.ViewL, MapWorld(slug.HandRootWorld(-1f)), toCam * slug.HandRootNormalWorld(-1f), posV - sideV, Vector3.up,
                             armRadiusRoot * thick, armRadiusHand * thick, fistSize * sc, sc);
        }

        /// <summary>How <see cref="ItemPickup.Visual"/> scales its mesh up to 1 m (its root's scale divided by the size).</summary>
        static float ScaleOf(ItemKind kind) => kind == ItemKind.RockSalt ? 1f / 0.06f : 1f / 0.065f;
    }
}
