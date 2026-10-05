using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Fills the <see cref="SoundBank"/> with the placeholder sounds (Assets/Audio/Placeholder, made by
    /// SoundGen/placeholder_sfx.py) and gives the player's slug in the open scene a <see cref="SlugSounds"/>.
    /// Clips already set by hand in the bank (わく's own sounds) are kept; only empty keys get placeholders.
    /// </summary>
    public static class PlaceholderSounds
    {
        const string Folder = "Assets/Audio/Placeholder";
        const string BankPath = "Assets/Tuning/SoundBank.asset";

        /// <summary>わく's own sounds used in place of a placeholder (key → clip path).</summary>
        static readonly Dictionary<string, string> Own = new Dictionary<string, string>
        {
            { "bag_stow_RockSalt", "Assets/Audio/SE/Bag/bag_push_salt.wav" },
        };

        [MenuItem("Slug/Set Up Placeholder Sounds (open scene)")]
        public static void SetUp()
        {
            var bank = Bank();
            int n = 0;
            foreach (var b in Object.FindObjectsByType<SlugBag>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!b.IsPlayer) continue;
                var s = b.GetComponent<SlugSounds>() ?? b.gameObject.AddComponent<SlugSounds>();
                s.bank = bank;
                EditorUtility.SetDirty(s);
                n++;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Placeholder sounds: {bank.entries.Count} keys, {n} player(s)");
        }

        /// <summary>The bank, created if missing, with every placeholder file grouped by its key (name without _01).</summary>
        public static SoundBank Bank()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (!bank)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }
            var groups = new SortedDictionary<string, List<AudioClip>>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Import(path);
                string name = Path.GetFileNameWithoutExtension(path);
                int us = name.LastIndexOf('_');
                string key = us > 0 && int.TryParse(name.Substring(us + 1), out _) ? name.Substring(0, us) : name;
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<AudioClip>();
                list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
            foreach (var kv in groups) SetIfEmpty(bank, kv.Key, kv.Value.ToArray());
            foreach (var kv in Own)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(kv.Value);
                if (clip) SetIfEmpty(bank, kv.Key, new[] { clip });
            }
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }

        static void SetIfEmpty(SoundBank bank, string key, AudioClip[] clips)
        {
            var e = bank.entries.Find(x => x.key == key);
            if (e == null) bank.entries.Add(e = new SoundBank.Entry { key = key });
            if (e.clips == null || e.clips.Length == 0) e.clips = clips;
        }

        /// <summary>Short sounds that play often: decompressed on load, mono (sound_list.md).</summary>
        static void Import(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (!imp) return;
            var s = imp.defaultSampleSettings;
            if (imp.forceToMono && s.loadType == AudioClipLoadType.DecompressOnLoad) return;
            imp.forceToMono = true;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
        }
    }
}
