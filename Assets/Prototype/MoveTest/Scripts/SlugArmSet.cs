using System.Collections.Generic;
using SlugDesign;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// The two stretchy arms of a held tool, twice over: a pair on the body (on <see cref="SlugToolHolder.BodyToolLayer"/>,
    /// seen by every camera except the player's own in first person) and a first-person pair under the player's camera.
    /// The first-person arms are drawn shrunk toward the eye, so their outline is thinned by the same factor.
    /// The arms replace the slug's short cone hands: they grow from the same spots on the body.
    /// </summary>
    public class SlugArmSet
    {
        public SlugArmMesh BodyL { get; private set; }
        public SlugArmMesh BodyR { get; private set; }
        public SlugArmMesh ViewL { get; private set; }
        public SlugArmMesh ViewR { get; private set; }

        Material _mat, _viewMat;

        public static SlugArmSet Create(string prefix, SlugPop slug, Transform viewParent, float viewOutlineScale)
        {
            var set = new SlugArmSet();
            slug.handsExtend = 0f;
            slug.ApplyHands();
            set._mat = new Material(slug.HandMaterial) { name = "SlugArm", hideFlags = HideFlags.DontSave };
            foreach (var p in new[] { "_Stretch", "_WaveAmp", "_SwayAmp", "_UprightAngle", "_TurnAngle" }) set._mat.SetFloat(p, 0f);
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var sphere = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            set.BodyL = SlugArmMesh.Create(prefix + "Arm_L", null, set._mat, sphere);
            set.BodyR = SlugArmMesh.Create(prefix + "Arm_R", null, set._mat, sphere);
            set._viewMat = new Material(set._mat) { name = "SlugViewArm", hideFlags = HideFlags.DontSave };
            set._viewMat.SetFloat("_OutlineWidth", set._mat.GetFloat("_OutlineWidth") * viewOutlineScale);
            set.ViewL = SlugArmMesh.Create(prefix + "ViewArm_L", viewParent, set._viewMat, sphere);
            set.ViewR = SlugArmMesh.Create(prefix + "ViewArm_R", viewParent, set._viewMat, sphere);
            foreach (var a in new[] { set.ViewL, set.ViewR })
            {
                a.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                a.FistRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            foreach (var a in new[] { set.BodyL, set.BodyR })
                foreach (var c in a.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = SlugToolHolder.BodyToolLayer;
            return set;
        }

        public IEnumerable<Renderer> ViewRenderers
        {
            get
            {
                foreach (var a in new[] { ViewL, ViewR })
                    if (a) foreach (var r in a.GetComponentsInChildren<Renderer>(true)) yield return r;
            }
        }

        public IEnumerable<Renderer> BodyRenderers
        {
            get
            {
                foreach (var a in new[] { BodyL, BodyR })
                    if (a) foreach (var r in a.GetComponentsInChildren<Renderer>(true)) yield return r;
            }
        }

        public void ShowBody(bool on)
        {
            foreach (var a in new[] { BodyL, BodyR }) if (a && a.gameObject.activeSelf != on) a.gameObject.SetActive(on);
        }

        public void ShowView(bool on)
        {
            foreach (var a in new[] { ViewL, ViewR }) if (a && a.gameObject.activeSelf != on) a.gameObject.SetActive(on);
        }

        /// <summary>Destroys the body arms and the materials (the first-person arms go with the camera).</summary>
        public void Destroy()
        {
            foreach (var a in new[] { BodyL, BodyR }) if (a) Object.Destroy(a.gameObject);
            if (_mat) Object.Destroy(_mat);
            if (_viewMat) Object.Destroy(_viewMat);
        }

        /// <summary>
        /// An arm from its root to a grip: it leaves the body mostly toward the hand (a little along the skin normal), so
        /// a long reach stays a gentle curve close to the body instead of looping out to the side, and its fist wraps
        /// <paramref name="handle"/> from the side. <paramref name="fistScale"/> also scales how far the root sinks into the skin.
        /// </summary>
        public static void Build(SlugArmMesh arm, Vector3 root, Vector3 rootNormal, Vector3 grip, Vector3 handle,
                                 float rootRadius, float handRadius, Vector3 fistSize, float fistScale)
        {
            Vector3 reach = grip - root;
            Vector3 leave = (rootNormal.normalized * 0.3f + reach.normalized * 0.7f).normalized;
            Vector3 arrive = Vector3.ProjectOnPlane(reach, handle);
            if (arrive.sqrMagnitude < 1e-4f * reach.sqrMagnitude) arrive = reach;
            arm.Build(root - rootNormal.normalized * 0.01f * fistScale, leave, grip, arrive.normalized, handle,
                      rootRadius, handRadius, fistSize);
        }
    }
}
