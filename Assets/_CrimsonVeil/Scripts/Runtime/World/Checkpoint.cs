using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>Sets the hero's respawn point when they pass by. Wordless: a faint crimson flicker.</summary>
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] float radius = 1.2f;

        public bool Reached { get; private set; }

        PlayerController player;

        void Start() => player = FindFirstObjectByType<PlayerController>();

        void Update()
        {
            if (Reached || player == null) return;
            Vector3 d = player.transform.position - transform.position;
            if (Mathf.Abs(d.x) < radius && Mathf.Abs(d.y) < 3f)
            {
                Reached = true;
                player.SetSpawnPoint(transform.position);
                Combat.CombatFx.Burst(transform.position + Vector3.up * 0.5f, Vector2.up, Combat.CombatFx.Crimson, 10, 2f);
            }
        }
    }
}
