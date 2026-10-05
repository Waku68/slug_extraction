using System.Collections.Generic;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Which clips play for what (a key per happening: pickaxe_hit_salt, bag_stow, gun_fire_rifle…). A key can have a
    /// version for one item (bag_stow_RockSalt) that is used before the plain one. Clips are swapped here without code:
    /// the placeholders in Assets/Audio/Placeholder now, わく's own sounds later.
    /// </summary>
    [CreateAssetMenu(menuName = "Slug/Sound Bank", fileName = "SoundBank")]
    public class SoundBank : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("何のときの音か（例：pickaxe_hit_salt、bag_stow_RockSalt）")] public string key = "";
            [Tooltip("鳴らす音。複数あれば毎回ランダムに選ぶ")] public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1f), Tooltip("音量")] public float volume = 0.8f;
            [Range(0f, 0.3f), Tooltip("音の高さの揺らぎ（毎回少し変える）")] public float pitchJitter = 0.06f;
        }

        [Range(0f, 1f), Tooltip("全部の効果音の音量")] public float masterVolume = 0.8f;
        public List<Entry> entries = new List<Entry>();

        public Entry Find(string key)
        {
            foreach (var e in entries) if (e.key == key && e.clips.Length > 0) return e;
            return null;
        }
    }
}
