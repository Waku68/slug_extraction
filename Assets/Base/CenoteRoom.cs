using UnityEngine;

namespace SlugBase
{
    /// <summary>
    /// One room of the cenote base (CenoteFurniture_&lt;Room&gt;). Its children &lt;Room&gt;_S1, _S2, _S3 hold what each
    /// stage adds (S2 and S3 add to the stage before), so stage n shows S1..Sn. &lt;Room&gt;_Carved is always on.
    /// </summary>
    public class CenoteRoom : MonoBehaviour
    {
        [Range(1, 3), Tooltip("部屋の段階。S1 から始まり、S2・S3 で1つずつ足される")] public int stage = 1;

        void Awake() => Apply();
        void OnValidate() => Apply();

        public void Apply()
        {
            for (int n = 1; n <= 3; n++)
            {
                var part = FindStage(n);
                if (part && part.gameObject.activeSelf != (n <= stage)) part.gameObject.SetActive(n <= stage);
            }
        }

        public void SetStage(int value)
        {
            stage = Mathf.Clamp(value, 1, 3);
            Apply();
        }

        Transform FindStage(int n)
        {
            string suffix = "_S" + n;
            foreach (Transform child in transform)
                if (child.name.EndsWith(suffix)) return child;
            return null;
        }
    }
}
