using SlugDesign;
using SlugExtract;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// The MoveTest map with the two-legged slug (Nov 2): walking and the tools (1 pickaxe, 2 back gun, 3 pistol) and,
    /// since Nov 3, its hitboxes. The same movement, camera and tools as the crawling slug, with <see cref="DinoSlugBody"/>
    /// on the body. V for third person. Since Nov 3 every game scene's player is built here (<see cref="BuildPlayer"/>).
    /// Menu Slug/Character/Build Dino Move Test → Assets/Scenes/Tests/DinoMoveTest.unity (builds the rig first).
    /// </summary>
    public static class DinoMoveTestBuilder
    {
        const string ScenePath = "Assets/Scenes/Tests/DinoMoveTest.unity";
        // the face sits this far in front of the capsule's middle (the crawling slug's is 0.122 m): the capsule stands
        // over the chest, the tail trails behind it
        const float FaceForward = 0.2f;

        [MenuItem("Slug/Character/Build Dino Move Test")]
        public static void Build()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty && !string.IsNullOrEmpty(active.path)) EditorSceneManager.SaveScene(active);
            DinoSlugPhotoRough.BuildRig();

            var settings = MoveTestBuilder.LoadSettings();
            var scene = MoveTestBuilder.NewScene(new Vector2(50f, 40f));
            MoveTestBuilder.BuildMap();
            var player = BuildPlayer(settings, new Vector3(0f, 0f, -6f));
            WearFullBag(player);
            EditorSceneManager.SaveScene(scene, ScenePath);
            MoveTestBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[DinoMoveTest] scene saved: " + ScenePath);
        }

        /// <summary>
        /// Nov 4 (わくさん：バッグとヘルメットを着せて、パンパンのときの動きを確かめたい): the player wears the helmet, the
        /// shell-plate armour, the vest and the cloth bag crammed full (capacity 30: 15 rain turnips + 15 rock salt, so it is
        /// heavy too). Q (hold) looks into it, G takes the top thing out (empties it a little at a time), E puts it back.
        /// Beside the start, two slugs standing in the same gear, one with an empty bag and one crammed, to compare from outside.
        /// </summary>
        static void WearFullBag(GameObject player)
        {
            var extract = AssetDatabase.LoadAssetAtPath<ExtractSettings>(MoveTestBuilder.TuningDir + "/ExtractSettings.asset");
            var full = new ItemKind[30];
            for (int i = 0; i < full.Length; i++) full[i] = i % 2 == 0 ? ItemKind.Crop : ItemKind.RockSalt;

            var body = player.GetComponentInChildren<SlugBodyBase>();
            var gear = ExtractTestBuilder.AddGear(body);
            gear.helmet = true;
            gear.armor = SlugGearBase.Armor.ShellPlate;
            var bag = player.AddComponent<SlugBag>();
            bag.settings = extract;
            bag.controller = player.GetComponent<SlugMoveController>();
            bag.kind = BagKind.Cloth;
            bag.startItems = full;
            ExtractTestBuilder.FitBag(player, bag);

            foreach (var (name, x, items) in new[] { ("空のバッグ", -1.2f, new ItemKind[0]), ("パンパンのバッグ", 1.2f, full) })
            {
                var at = new Vector3(x, 0f, -3.5f);
                var dummy = BuildStanding(name, at, Quaternion.Euler(0f, x < 0f ? 135f : 225f, 0f), true, SlugGearBase.Armor.ShellPlate, true);
                var dummyBag = dummy.AddComponent<SlugBag>();
                dummyBag.settings = extract;
                dummyBag.startItems = items;
                ExtractTestBuilder.FitBag(dummy, dummyBag);
                MoveTestBuilder.Label(name, at + Vector3.up * 0.95f, 1.2f);
            }

            // Nov 4 (わくさん：袋の上の銃とピッケルの持ち替えが不自然。体の横にくくる案と、しっぽで渡す案の見た目だけ見たい):
            // two more crammed slugs with the rifle and pickaxe tied on (look only, in play; the right side toward the start)
            foreach (var (name, x, style) in new[] { ("体の横にくくる", -2.8f, ToolCarryPreview.Style.Side), ("しっぽにくくる", 2.8f, ToolCarryPreview.Style.Tail) })
            {
                var at = new Vector3(x, 0f, -3.5f);
                var dummy = BuildStanding(name, at, Quaternion.Euler(0f, 90f, 0f), true, SlugGearBase.Armor.ShellPlate, true);
                var dummyBag = dummy.AddComponent<SlugBag>();
                dummyBag.settings = extract;
                dummyBag.startItems = full;
                ExtractTestBuilder.FitBag(dummy, dummyBag);
                var preview = dummy.AddComponent<ToolCarryPreview>();
                preview.style = style;
                MoveTestBuilder.Label(name, at + Vector3.up * 0.95f, 1.2f);
            }
        }

        /// <summary>The two-legged player (body, camera, tools, hitboxes) at <paramref name="position"/>: every scene's player since Nov 3.</summary>
        internal static GameObject BuildPlayer(SlugMoveSettings settings, Vector3 position)
        {
            var player = new GameObject("Player");
            player.transform.position = position;
            var cc = player.AddComponent<CharacterController>();
            cc.radius = 0.18f;
            cc.height = 0.56f;
            cc.center = new Vector3(0f, 0.28f, 0f);
            cc.skinWidth = 0.03f;
            cc.minMoveDistance = 0f;
            cc.slopeLimit = settings.slopeLimit;
            cc.stepOffset = settings.stepHeight;

            var dino = AddBody(player.transform, cc);
            var bodyGo = dino.gameObject;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = settings.fieldOfView;
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(player.transform.position + new Vector3(0f, 0.47f, FaceForward), Quaternion.identity);

            var move = player.AddComponent<SlugMoveController>();
            var rig = player.AddComponent<SlugCameraRig>();
            var hud = player.AddComponent<SlugMoveHud>();
            var tool = player.AddComponent<SlugToolHolder>();
            move.settings = settings; move.cameraRig = rig; move.body = bodyGo.transform;
            rig.settings = settings; rig.controller = move; rig.dino = dino; rig.cam = cam;
            hud.controller = move; hud.cameraRig = rig;
            // the same tools as MoveTest (6章の3): the stretchy arms grow from the shoulders
            tool.pickaxe = MoveTestBuilder.LoadPickaxeSettings(); tool.controller = move; tool.cameraRig = rig; tool.slug = dino;
            var kinds = new[] { GunKind.Rifle, GunKind.Shotgun, GunKind.Sniper, GunKind.Pistol };
            var guns = new SlugGunHolder[kinds.Length];
            for (int i = 0; i < kinds.Length; i++)
            {
                var gun = guns[i] = player.AddComponent<SlugGunHolder>();
                gun.gun = MoveTestBuilder.LoadGunSettings(kinds[i]); gun.controller = move; gun.cameraRig = rig; gun.slug = dino;
                gun.enabled = false;
            }
            var loadout = player.AddComponent<SlugLoadout>();
            loadout.pickaxe = tool; loadout.guns = guns;
            MoveTestBuilder.FitLoadout(player);
            // what a shot can hit, part by part (the movement capsule is not hit)
            player.AddComponent<SlugHitboxes>().slug = dino;
            return player;
        }

        /// <summary>
        /// The two-legged body under <paramref name="parent"/> (named Body): standing on the ground with its face
        /// <see cref="FaceForward"/> in front of the capsule's middle when <paramref name="cc"/> is given, else at the parent's feet.
        /// </summary>
        public static DinoSlugBody AddBody(Transform parent, CharacterController cc)
        {
            var bodyGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DinoSlugPhotoRough.RigPrefab));
            bodyGo.name = "Body";
            bodyGo.transform.SetParent(parent, false);
            var dino = bodyGo.GetComponent<DinoSlugBody>();
            if (cc)
            {
                float faceZ = bodyGo.transform.InverseTransformPoint(dino.eyeAnchor.position).z;
                bodyGo.transform.localPosition = new Vector3(0f, -cc.skinWidth, FaceForward - faceZ);
            }
            return dino;
        }

        /// <summary>
        /// A two-legged slug standing still at <paramref name="position"/> (no controller): a target to shoot at, in the
        /// gear given, with its hitboxes on the root and the body under it.
        /// </summary>
        public static GameObject BuildStanding(string name, Vector3 position, Quaternion rotation, bool helmet, SlugGearBase.Armor armor, bool bag)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(position, rotation);
            var dino = AddBody(root.transform, null);
            var gear = dino.gameObject.AddComponent<DinoEquipment>();
            gear.slug = dino;
            gear.helmet = helmet;
            gear.armor = armor;
            gear.bag = bag;
            root.AddComponent<SlugHitboxes>().slug = dino;
            return root;
        }
    }
}
