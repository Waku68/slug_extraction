using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugBase
{
    /// <summary>
    /// Development helpers for the cenote base scene: F6 steps every room through S1 → S2 → S3, and a slug that
    /// falls into the sea (below <see cref="fallY"/>) is put back at the spawn.
    /// </summary>
    public class CenoteBaseDev : MonoBehaviour
    {
        public Transform player;
        public Vector3 spawn;
        public float spawnYaw;
        [Tooltip("これより低く落ちたら出発点へ戻す（m。海面は 0）")] public float fallY = 0.8f;

        CenoteRoom[] _rooms;

        void Start() => _rooms = FindObjectsByType<CenoteRoom>();

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f6Key.wasPressedThisFrame && _rooms.Length > 0)
            {
                int next = _rooms[0].stage % 3 + 1;
                foreach (var room in _rooms) room.SetStage(next);
                Debug.Log($"[CenoteBase] all rooms: S{next}");
            }

            if (player && player.position.y < fallY)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                player.SetPositionAndRotation(spawn, Quaternion.Euler(0f, spawnYaw, 0f));
                if (cc) cc.enabled = true;
            }
        }
    }
}
