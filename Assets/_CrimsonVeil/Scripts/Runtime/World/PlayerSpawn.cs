using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>Where the hero starts. On play, the hero is moved here (and it becomes the first respawn point).</summary>
    public sealed class PlayerSpawn : MonoBehaviour
    {
        void Start()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            Vector3 pos = PlayFromHereOverride.TryConsume(out var over) ? PlayFromHereOverride.SnapToGround(over) : transform.position;
            player.Teleport(pos);
            player.SetSpawnPoint(pos);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));
            Gizmos.DrawLine(transform.position + Vector3.up * 1.8f, transform.position + new Vector3(0.6f, 1.8f, 0f));
        }
    }

    /// <summary>Editor "play from here" hand-off (set before entering play mode).</summary>
    public static class PlayFromHereOverride
    {
        const string Key = "CrimsonVeil.PlayFromHere";

        public static void Set(Vector3 position)
        {
#if UNITY_EDITOR
            UnityEditor.SessionState.SetVector3(Key, position);
            UnityEditor.SessionState.SetBool(Key + ".set", true);
#endif
        }

        public static bool TryConsume(out Vector3 position)
        {
            position = default;
#if UNITY_EDITOR
            if (!UnityEditor.SessionState.GetBool(Key + ".set", false)) return false;
            UnityEditor.SessionState.SetBool(Key + ".set", false);
            position = UnityEditor.SessionState.GetVector3(Key, Vector3.zero);
            return true;
#else
            return false;
#endif
        }

        /// <summary>Drops a point onto the first solid ground below it (or just above it if inside a block).</summary>
        public static Vector3 SnapToGround(Vector3 p)
        {
            var from = new Vector3(p.x, p.y + 30f, 0f);
            if (Physics.Raycast(from, Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore))
                return new Vector3(p.x, hit.point.y + 0.05f, 0f);
            return new Vector3(p.x, p.y, 0f);
        }

        // Levels without a PlayerSpawn: still honour "play from here".
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplyWithoutSpawn()
        {
            if (Object.FindFirstObjectByType<PlayerSpawn>() != null) return;
            if (!TryConsume(out var p)) return;
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            var pos = SnapToGround(p);
            player.Teleport(pos);
            player.SetSpawnPoint(pos);
        }
    }
}
