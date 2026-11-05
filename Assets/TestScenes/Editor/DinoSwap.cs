using System.Collections.Generic;
using SlugDesign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Puts the two-legged body (<see cref="DinoSlugBody"/>) in place of every crawling slug (<see cref="SlugPop"/>) in the
    /// open scene (Nov 3 わくさん：前のナメクジのキャラを全部のシーンで取り替えたい), keeping everything else as it is:
    ///   - a slug driven by a controller (the player, a bot): its Body is replaced by the two-legged one, placed with its
    ///     face over the front of the capsule;
    ///   - a slug standing on its own (a practice slug): the body goes under it and the crawling slug's parts are taken off.
    /// The gear is moved over with its look (the helmet, the armour, the bag, the vest and their colours) to the two-legged
    /// gear (<see cref="DinoEquipment"/>), and every reference in the scene to the old body, its transform or its gear is
    /// pointed at the new ones (the camera, the tools, the bag, the hitboxes, the moisture, the bot...).
    /// The crawling slug itself is kept, playable, in the lookbook (<see cref="SlugLookbook"/>).
    /// Menu Slug/Character/Swap To Two-Legged Body (open scene).
    /// </summary>
    public static class DinoSwap
    {
        [MenuItem("Slug/Character/Swap To Two-Legged Body (open scene)")]
        public static void SwapMenu() => Debug.Log("[DinoSwap] " + Swap());

        public static string Swap()
        {
            var pops = Object.FindObjectsByType<SlugPop>(FindObjectsInactive.Include);
            if (pops.Length == 0) return "no crawling slug in " + EditorSceneManager.GetActiveScene().path;
            var log = new List<string>();
            foreach (var pop in pops) log.Add(SwapOne(pop));
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            return string.Join("; ", log);
        }

        static string SwapOne(SlugPop pop)
        {
            var oldGo = pop.gameObject;
            string where = oldGo.transform.parent ? oldGo.transform.parent.name : oldGo.name;
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(oldGo);
            if (root) PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var oldGear = pop.GetComponent<SlugEquipment>();

            // the new body: in place of the old one under the controller, or under the standing slug
            var cc = oldGo.GetComponentInParent<CharacterController>();
            bool standing = !cc || cc.gameObject == oldGo;
            Transform parent = standing ? oldGo.transform : oldGo.transform.parent;
            var dino = DinoMoveTestBuilder.AddBody(parent, standing ? null : cc);
            if (!standing) dino.transform.SetSiblingIndex(oldGo.transform.GetSiblingIndex());

            SlugGearBase gear = null;
            if (oldGear)
            {
                var g = dino.gameObject.AddComponent<DinoEquipment>();
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(oldGear), g);   // the look (the shared fields)
                g.slug = dino;
                gear = g;
            }

            // every reference to the old body, its transform, its game object or its gear now points at the new ones
            int n = 0;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (!mb || mb == pop || mb == oldGear || mb is DinoSlugBody || mb is DinoEquipment) continue;
                var so = new SerializedObject(mb);
                var it = so.GetIterator();
                bool changed = false;
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference || !it.objectReferenceValue) continue;
                    var v = it.objectReferenceValue;
                    Object to;
                    if (v == pop) to = dino;
                    else if (v == oldGear) to = gear;
                    else if (!standing && v == oldGo.transform) to = dino.transform;
                    else if (!standing && v == oldGo) to = dino.gameObject;
                    else continue;
                    // (a field that can only hold the crawling slug or its gear is cleared)
                    string type = it.type;
                    if (type.Contains("SlugPop") || type.Contains("SlugEquipment")) to = null;
                    it.objectReferenceValue = to;
                    changed = true;
                    n++;
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
            // the camera of the slug that was swapped looks from the new body's eyes
            foreach (var rig in Object.FindObjectsByType<SlugCameraRig>(FindObjectsInactive.Include))
                if (rig.controller && rig.controller.body == dino.transform) { rig.slug = null; rig.dino = dino; EditorUtility.SetDirty(rig); }

            // the old one goes
            if (standing)
            {
                if (oldGear) Object.DestroyImmediate(oldGear);
                Object.DestroyImmediate(pop);
            }
            else Object.DestroyImmediate(oldGo);
            if (gear) gear.Build();
            return $"{where}: {n} refs" + (gear ? $" (gear: helmet {gear.helmet}, {gear.armor}, bag {gear.bag})" : "");
        }
    }
}
