using UnityEngine;

namespace SlugExtract
{
    public enum ItemKind { RockSalt, Crop }

    [System.Serializable]
    public class ItemDef
    {
        public string displayName = "";
        [Tooltip("1 個の重さ")] public float weight = 1f;
        [Tooltip("1 個の価値（塩に換算）")] public int value = 10;
        [Tooltip("叩いて採るときの欠片の色")] public Color chipColor = Color.white;
    }

    /// <summary>Rules of the solo "harvest and escape" prototype, in one asset. Read every frame, so it can be tuned in play mode.</summary>
    [CreateAssetMenu(menuName = "Slug/Extract Settings", fileName = "ExtractSettings")]
    public class ExtractSettings : ScriptableObject
    {
        [Header("試合")]
        [Tooltip("雨が止むまでの時間（秒）。過ぎると乾いて倒れ、持ち物を全部失う")] public float timeLimit = 240f;
        [Tooltip("脱出口にとどまって脱出するまでの時間（秒）")] public float extractTime = 3f;
        [Tooltip("脱出口の半径（m）")] public float extractRadius = 1.5f;

        [Header("バッグ")]
        [Tooltip("持てる重さの上限")] public float bagCapacity = 20f;
        [Range(0.1f, 1f), Tooltip("バッグが満杯のときの速度の倍率（重さに比例して下がる）")] public float speedAtFull = 0.55f;

        [Header("採取物")]
        public ItemDef rockSalt = new ItemDef { displayName = "岩塩", weight = 1.5f, value = 25, chipColor = new Color(0.95f, 0.88f, 0.9f) };
        public ItemDef crop = new ItemDef { displayName = "雨カブ", weight = 0.5f, value = 8, chipColor = new Color(0.55f, 0.75f, 0.3f) };

        public ItemDef Def(ItemKind k) => k == ItemKind.RockSalt ? rockSalt : crop;
    }
}
