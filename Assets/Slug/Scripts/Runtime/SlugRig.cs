using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// Runtime handle of a slug candidate. There are no bones: the crawl is a vertex-shader wave,
    /// and the finger-less hands are simply scaled out of / back into the body.
    /// </summary>
    [ExecuteAlways]
    public class SlugRig : MonoBehaviour
    {
        public Transform handL, handR;
        [Range(0f, 1f)] public float handsExtend = 1f;
        [Tooltip("Seconds to fully extend / retract when SetHandsOut is used in play mode.")]
        public float handsSpeed = 6f;

        float _target = -1f;

        public void SetHandsOut(bool out_) { _target = out_ ? 1f : 0f; }

        void OnEnable() { Apply(); }
        void OnValidate() { Apply(); }

        void Update()
        {
            if (_target >= 0f && Application.isPlaying)
            {
                handsExtend = Mathf.MoveTowards(handsExtend, _target, handsSpeed * Time.deltaTime);
                Apply();
            }
        }

        public void Apply()
        {
            float s = Mathf.SmoothStep(0f, 1f, handsExtend);
            // keep a tiny scale so the transform stays invertible (the hand is buried inside the body)
            Vector3 v = Vector3.one * Mathf.Max(s, 0.0001f);
            if (handL) handL.localScale = v;
            if (handR) handR.localScale = v;
            if (handL) handL.gameObject.SetActive(s > 0.001f);
            if (handR) handR.gameObject.SetActive(s > 0.001f);
        }
    }
}
