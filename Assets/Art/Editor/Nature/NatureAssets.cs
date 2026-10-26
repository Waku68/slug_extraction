using System;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 島の植物のアセットを保存する共通の手順（Oct 25）：メッシュ・マテリアル・プレハブ。作り直してもアセットの GUID は変えない
    /// （場面に置いたプレハブがそのまま新しい形になる）。
    /// </summary>
    public static class NatureAssets
    {
        /// <summary>メッシュを保存する。前のがあれば中身だけ入れ替える。</summary>
        public static Mesh SaveMesh(Mesh mesh, string path)
        {
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) { old.Clear(); EditorUtility.CopySerialized(mesh, old); return old; }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>
        /// 世界の物の質感（WorldLook）のマテリアル。wind は風で揺れる量（m。頂点の色の R が 1 の所が、そよ風でこれだけ動く。0 なら揺れない）。
        /// </summary>
        public static Material Mat(string path, string hex, bool doubleSided, float wind, float rim = 0.15f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            var m = WorldLook.GetOrCreate(path);
            WorldLook.Apply(m, c, rim, 0f, null, 10f, 0f, 60f, doubleSided);
            m.SetFloat("_WindAmount", wind);
            m.enableInstancing = true;
            return m;
        }

        /// <summary>プレハブを保存する（addColliders で当たり判定を足す。null なら当たり判定なし）。</summary>
        public static GameObject SavePrefab(string path, Mesh mesh, Material[] mats, Action<GameObject> addColliders)
        {
            var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            addColliders?.Invoke(go);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>0〜1 の決まった乱数（種と番号から。作り直しても同じ形になる）。</summary>
        public static float Hash(int seed, int i)
        {
            unchecked
            {
                uint h = (uint)seed * 374761393u + (uint)i * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>葉身（NatureMeshKit.Blade）の上の面の点：s は軸に沿って、a は -1 左ふち 〜 0 軸 〜 1 右ふち。葉脈などを葉の上に乗せるのに使う。</summary>
        public static Vector3 BladePoint(Func<float, Vector3> path, Func<float, float> widthAt, float thick, float fold, float s, float a, float lift = 0f)
        {
            Vector3 c = path(s);
            Vector3 tan = (path(Mathf.Min(1f, s + 0.02f)) - path(Mathf.Max(0f, s - 0.02f))).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, tan);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(tan, side).normalized;
            float w = Mathf.Max(0.01f, widthAt(s));
            Vector3 edge = c + side * (w * Mathf.Sign(-a)) - up * (w * fold);
            Vector3 mid = c + up * (thick * 0.6f);
            return Vector3.Lerp(mid, edge, Mathf.Abs(a)) + up * lift;
        }
    }
}
