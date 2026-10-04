using UnityEngine;

namespace SlugExtract
{
    public enum ItemKind { RockSalt, Crop }

    /// <summary>The kinds of bag (what is carried on the back), plus the folding eco bag and a snail's shell.</summary>
    public enum BagKind { Cloth, Leather, ShellBox, Net, Eco, SnailShell, None }

    [System.Serializable]
    public class ItemDef
    {
        public string displayName = "";
        [Tooltip("1 個の重さ。重いほど遅くなる")] public float weight = 1f;
        [Tooltip("1 個のかさ。かさが多いほどバッグが膨らみ、背中の当たり判定が大きくなる")] public float volume = 1f;
        [Tooltip("1 個の価値（塩に換算）")] public int value = 10;
        [Tooltip("1 個の大きさ（m）。採ると 1 個ずつ出てきて、手でつかんで運ぶ")] public float size = 0.1f;
        [Tooltip("手に持ったときに見える大きさ（m）。持っている感じが出るように、実物より大きく見せてよい")] public float heldSize = 0.1f;
        [Tooltip("体の中の隠し場所にしまえるか（塩はしまえない：ナメクジが溶ける）")] public bool canHide = true;
        [Tooltip("叩いて採るときの欠片の色")] public Color chipColor = Color.white;
    }

    /// <summary>One kind of bag: how much it takes, how long it holds out, how it takes a round in the back.</summary>
    [System.Serializable]
    public class BagDef
    {
        public string displayName = "";
        [Tooltip("耐久。背中に弾が当たると減り、0 で壊れて中身が全部落ちる")] public float durability = 60f;
        [Range(0f, 1f), Tooltip("背中に当たった弾が体に通る割合（1 = 貫通する、0 = 止める）")] public float bodyDamage = 1f;
        [Tooltip("入るかさの上限（0 = 上限なし）")] public float capacity = 0f;
        [Tooltip("膨らみやすさ（1 = ふつう、小さいほど硬くて膨らみにくい）")] public float swell = 1f;
        [Tooltip("空のバッグの重さ")] public float weight = 0.3f;
        [Tooltip("布の色")] public Color color = new Color(0.52f, 0.4f, 0.26f);
        [Tooltip("ふたとポケットの色")] public Color flapColor = new Color(0.36f, 0.26f, 0.17f);
    }

    /// <summary>Rules of the solo "harvest and escape" prototype, in one asset. Read every frame, so it can be tuned in play mode.</summary>
    [CreateAssetMenu(menuName = "Slug/Extract Settings", fileName = "ExtractSettings")]
    public class ExtractSettings : ScriptableObject
    {
        [Header("試合")]
        [Tooltip("雨が止むまでの時間（秒）。過ぎると乾いて倒れ、持ち物を全部失う")] public float timeLimit = 240f;
        [Tooltip("脱出口にとどまって脱出するまでの時間（秒）")] public float extractTime = 3f;
        [Tooltip("脱出口の半径（m）")] public float extractRadius = 1.5f;

        [Header("重さと速さ（上限はない）")]
        [Tooltip("閾値：この重さを超えると、這うのが明らかに遅くなる")] public float weightThreshold = 10f;
        [Range(0f, 0.5f), Tooltip("閾値までに遅くなる割合（閾値ちょうどで、この分だけ遅い）")] public float lightSlowdown = 0.15f;
        [Range(0.1f, 1f), Tooltip("閾値を超えた瞬間の速さの倍率（ここで明らかに落ちる）")] public float overSpeed = 0.3f;
        [Range(0.1f, 1f), Tooltip("いちばん遅いときの速さの倍率（overSpeed と同じなら、超えたらずっと同じ遅さ）")] public float heaviestSpeed = 0.3f;
        [Tooltip("閾値をどれだけ超えると、いちばん遅くなるか（重さ）")] public float heavyRange = 10f;

        [Header("バッグの膨らみ")]
        [Tooltip("かさ 1 あたりの膨らみ（大きさの倍率の増え方）")] public float swellPerVolume = 0.05f;
        [Tooltip("見た目の膨らみの限度（倍率）。上限はないが、絵が破綻しないように")] public float swellVisualMax = 2.6f;

        [Header("耐久・エコバッグ・隠し場所")]
        [Tooltip("背中に弾が 1 粒当たったときに減る耐久（弾の速さ 140 m/s あたり）")] public float hitDamage = 8f;
        [Tooltip("エコバッグを広げる時間（秒）。その間は無防備")] public float ecoOpenTime = 3f;
        [Range(0.1f, 1f), Tooltip("背中を覗いている間の速さの倍率")] public float peekSpeed = 0.4f;
        [Tooltip("背中を覗くときに体をねじる角度（度）。頑張って後ろを見て、ぎりぎり見えるくらい")] public float peekTwist = 90f;
        [Tooltip("落ちている物をつかめる距離（m）")] public float pickupReach = 0.8f;
        [Tooltip("つかむ・バッグに入れる・バッグから出す時間（秒）")] public float grabTime = 0.3f, stowTime = 0.5f, takeOutTime = 0.5f;

        [Header("バッグの種類")]
        public BagDef cloth = new BagDef { displayName = "布の袋", durability = 60f, bodyDamage = 1f, capacity = 0f, swell = 1f, weight = 0.3f };
        public BagDef leather = new BagDef { displayName = "革の袋", durability = 120f, bodyDamage = 0.5f, capacity = 0f, swell = 0.8f, weight = 0.8f,
            color = new Color(0.4f, 0.25f, 0.15f), flapColor = new Color(0.27f, 0.16f, 0.1f) };
        public BagDef shellBox = new BagDef { displayName = "殻の箱", durability = 220f, bodyDamage = 0f, capacity = 10f, swell = 0.3f, weight = 2f,
            color = new Color(0.62f, 0.57f, 0.46f), flapColor = new Color(0.43f, 0.4f, 0.32f) };
        public BagDef net = new BagDef { displayName = "網バッグ", durability = 40f, bodyDamage = 1f, capacity = 8f, swell = 1.1f, weight = 0.1f,
            color = new Color(0.55f, 0.6f, 0.45f), flapColor = new Color(0.4f, 0.45f, 0.32f) };
        public BagDef eco = new BagDef { displayName = "エコバッグ", durability = 15f, bodyDamage = 1f, capacity = 1.8f, swell = 1.2f, weight = 0.05f,
            color = new Color(0.78f, 0.8f, 0.72f), flapColor = new Color(0.62f, 0.66f, 0.56f) };
        public BagDef snailShell = new BagDef { displayName = "カタツムリの殻", durability = 200f, bodyDamage = 0.3f, capacity = 6f, swell = 0f, weight = 0f };

        [Header("採取物")]
        public ItemDef rockSalt = new ItemDef { displayName = "岩塩", weight = 1.5f, volume = 0.6f, value = 25, size = 0.2f, heldSize = 0.3f, canHide = false, chipColor = new Color(0.95f, 0.88f, 0.9f) };
        public ItemDef crop = new ItemDef { displayName = "雨カブ", weight = 0.5f, volume = 1.2f, value = 8, size = 0.15f, heldSize = 0.15f, canHide = true, chipColor = new Color(0.55f, 0.75f, 0.3f) };

        public ItemDef Def(ItemKind k) => k == ItemKind.RockSalt ? rockSalt : crop;

        public BagDef Bag(BagKind k) => k switch
        {
            BagKind.Leather => leather,
            BagKind.ShellBox => shellBox,
            BagKind.Net => net,
            BagKind.Eco => eco,
            BagKind.SnailShell => snailShell,
            _ => cloth,
        };

        /// <summary>
        /// How fast the slug crawls carrying <paramref name="weight"/>: a little slower up to the threshold, then a clear
        /// drop, and slower still as it goes on past it (there is no limit to what goes in).
        /// </summary>
        public float SpeedFor(float weight)
        {
            if (weight <= weightThreshold) return 1f - lightSlowdown * Mathf.Clamp01(weight / Mathf.Max(weightThreshold, 1e-3f));
            float over = (weight - weightThreshold) / Mathf.Max(heavyRange, 1e-3f);
            return Mathf.Lerp(overSpeed, heaviestSpeed, Mathf.Clamp01(over));
        }
    }
}
