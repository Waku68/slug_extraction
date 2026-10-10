using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The first step of the two-person test (docs/discussions/who_what_scope_discussion.md): a bot slug on the rock salt
    /// mountain of IslandTest_800, mining rock salt on the shoulder and shooting at the player when it notices them
    /// (<see cref="SlugBot"/>). Also puts rock salt mushrooms round the shoulder (the mountain had them only on the summit).
    /// </summary>
    public static partial class IslandTest800Builder
    {
        const string BotName = "ボット";
        const string BotSaltName = "ボット用の岩塩";

        [MenuItem("Slug/IslandTest 800 m/Add Bot On The Mountain")]
        public static string AddBot()
        {
            var terrain = Terrain.activeTerrain;
            if (!terrain) return "no terrain (open IslandTest_800)";
            var settings = ExtractTestBuilder.PrepareAssets();
            var condition = AssetDatabase.LoadAssetAtPath<ConditionSettings>("Assets/Tuning/ConditionSettings.asset");

            // the player: salt hurts it too
            SlugBag playerBag = null;
            foreach (var b in Object.FindObjectsByType<SlugBag>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.IsPlayer) playerBag = b;
            if (!playerBag) return "no player";
            var player = playerBag.controller.gameObject;
            if (!player.GetComponent<SlugHealth>()) player.AddComponent<SlugHealth>().settings = condition;
            var dmg = player.GetComponent<SlugDamage>() ?? player.AddComponent<SlugDamage>();
            dmg.vanishOnFall = false;
            EditorUtility.SetDirty(dmg);

            foreach (var name in new[] { BotName, BotSaltName })
            {
                var old = GameObject.Find(name);
                if (!old) continue;
                var oldRig = old.GetComponent<SlugCameraRig>();
                if (oldRig && oldRig.cam && !oldRig.cam.transform.IsChildOf(old.transform)) Object.DestroyImmediate(oldRig.cam.gameObject);
                Object.DestroyImmediate(old);
            }

            // the mountain: its highest point
            var td = terrain.terrainData;
            Vector3 origin = terrain.transform.position, size = td.size;
            Vector3 top = origin;
            float topH = -1f;
            for (int i = 0; i <= 64; i++)
                for (int j = 0; j <= 64; j++)
                {
                    var p = origin + new Vector3(size.x * i / 64f, 0f, size.z * j / 64f);
                    float h = terrain.SampleHeight(p);
                    if (h > topH) { topH = h; top = p; }
                }

            // rock salt round the shoulder (about 57–67 m up, on flat enough ground), ten spots
            var saltRoot = new GameObject(BotSaltName).transform;
            Vector3 spawn = Vector3.zero;
            int placed = 0;
            for (int k = 0; k < 10; k++)
            {
                float a = (k * 36f + 10f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (float r = 20f; r < 200f; r += 1.5f)
                {
                    var p = top + dir * r;
                    float h = terrain.SampleHeight(p) + origin.y;
                    if (h > 67f) continue;
                    if (h < 57f) break;
                    var n = td.GetInterpolatedNormal((p.x - origin.x) / size.x, (p.z - origin.z) / size.z);
                    if (n.y < 0.9f) continue;
                    var at = new Vector3(p.x, h, p.z);
                    ExtractTestBuilder.SaltRock(saltRoot, at, 1.2f);
                    ExtractTestBuilder.SaltRock(saltRoot, at + Quaternion.Euler(0f, k * 50f, 0f) * new Vector3(1.6f, 0f, 0.8f), 1f);
                    if (placed == 0) spawn = at + dir * 2.5f;
                    placed++;
                    break;
                }
            }
            if (placed == 0) return "no shoulder ground found";
            spawn.y = terrain.SampleHeight(spawn) + origin.y + 0.05f;

            // the bot: the player's body, driven by SlugBot
            var bot = MoveTestBuilder.BuildPlayer(MoveTestBuilder.LoadSettings(), spawn);
            bot.name = BotName;
            Object.DestroyImmediate(bot.GetComponent<SlugMoveHud>());
            var picker = bot.GetComponent<LoadoutPicker>();
            if (picker) Object.DestroyImmediate(picker);
            var cam = bot.GetComponent<SlugCameraRig>().cam;
            Object.DestroyImmediate(cam.GetComponent<AudioListener>());
            cam.tag = "Untagged";
            cam.enabled = false;
            cam.gameObject.name = "ボットの目";
            cam.transform.SetParent(bot.transform, true);

            var loadout = bot.GetComponent<SlugLoadout>();
            loadout.backGun = GunKind.Rifle;
            loadout.hip = HipTool.Pistol;
            loadout.startWith = SlugLoadout.PickaxeSlot;
            var bag = bot.AddComponent<SlugBag>();
            bag.settings = settings;
            bag.kind = BagKind.Leather;
            bag.startItems = new[] { ItemKind.RockSalt, ItemKind.RockSalt };
            ExtractTestBuilder.FitBag(bot, bag);
            bag.gear.helmet = true;
            bag.gear.armor = SlugDesign.SlugEquipment.Armor.ShellPlate;
            var health = bot.AddComponent<SlugHealth>();
            health.settings = condition;
            bot.AddComponent<SlugDamage>().vanishOnFall = true;

            var brain = bot.AddComponent<SlugBot>();
            brain.controller = bot.GetComponent<SlugMoveController>();
            brain.cameraRig = bot.GetComponent<SlugCameraRig>();
            brain.pickaxe = bot.GetComponent<SlugToolHolder>();
            brain.loadout = loadout;
            brain.bag = bag;
            brain.health = health;
            brain.slug = bag.slug;
            brain.homeCenter = new Vector3(top.x, topH + origin.y, top.z);
            brain.homeRadius = 90f;
            bag.slug.Rebuild();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Selection.activeGameObject = bot;
            return $"bot at {spawn:F1}, {placed} salt spots round the shoulder (top {topH + origin.y:F0} m)";
        }
    }
}
