using SlugDesign;
using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Puts an enemy to practise on in the open scene: a slug with a leather bag full of things, health, and salt that
    /// hurts it. Shot down, it drops its bag where it stood and is gone; walk over and swap bags (C held next to it).
    /// The player gets <see cref="SlugDamage"/> too (it does not vanish: the raid ends).
    /// </summary>
    public static class PracticeEnemy
    {
        const string Tuning = "Assets/Tuning/";

        [MenuItem("Slug/Add Practice Enemy (open scene)")]
        public static void Add()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ExtractSettings>(Tuning + "ExtractSettings.asset");
            var condition = AssetDatabase.LoadAssetAtPath<ConditionSettings>(Tuning + "ConditionSettings.asset");
            Transform player = null;
            foreach (var b in Object.FindObjectsByType<SlugBag>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!b.IsPlayer) continue;
                player = b.controller ? b.controller.transform : b.transform;
                var dmg = b.GetComponent<SlugDamage>() ?? b.gameObject.AddComponent<SlugDamage>();
                dmg.vanishOnFall = false;
                EditorUtility.SetDirty(dmg);
            }
            Vector3 at = player ? player.position + player.forward * 5f + player.right * 1.5f : Vector3.zero;
            if (Physics.Raycast(at + Vector3.up * 5f, Vector3.down, out var hit, 20f)) at.y = hit.point.y;

            var facing = player ? Quaternion.LookRotation(Vector3.ProjectOnPlane(player.position - at, Vector3.up), Vector3.up) : Quaternion.identity;
            var enemy = DinoMoveTestBuilder.BuildStanding("敵役のナメクジ", at, facing, false, SlugGearBase.Armor.None, true);
            var bag = enemy.AddComponent<SlugBag>();
            bag.settings = settings;
            bag.kind = BagKind.Leather;
            bag.startItems = new[] { ItemKind.RockSalt, ItemKind.Crop, ItemKind.RockSalt, ItemKind.UgoTake, ItemKind.RockSalt, ItemKind.UgoTake };
            ExtractTestBuilder.FitBag(enemy, bag);
            enemy.AddComponent<SlugHealth>().settings = condition;
            enemy.AddComponent<SlugDamage>().vanishOnFall = true;
            Selection.activeGameObject = enemy;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Practice enemy added at " + at);
        }
    }
}
