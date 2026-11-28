using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// Sets the hero's respawn point when he passes by. Wordless: a faint crimson flicker.
    /// <para>Since v0.110 it is also where he rests: every time he comes to it his life and his scarf are made full
    /// (life no longer grows back by itself), and standing at it, <c>F</c> opens its altar, where the crimson the foes
    /// shed buys upgrades (<see cref="CrimsonVeil.UI.AltarPanel"/>). (The user: there will be fewer checkpoints - each
    /// one counts more.)</para>
    /// </summary>
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] float radius = 1.2f;

        public bool Reached { get; private set; }
        /// <summary>The hero stands at it now.</summary>
        public bool HeroHere { get; private set; }
        /// <summary>The checkpoint the hero stands at, if any (for the HUD's hint and the altar).</summary>
        public static Checkpoint Nearby { get; private set; }

        PlayerController player;
        PlayerHealth health;

        void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player != null) health = player.GetComponent<PlayerHealth>();
        }

        void OnDisable() { if (Nearby == this) Nearby = null; }

        void Update()
        {
            if (player == null) return;
            Vector3 d = player.transform.position - transform.position;
            bool here = Mathf.Abs(d.x) < radius && Mathf.Abs(d.y) < 3f && (health == null || health.IsAlive);
            if (here && !HeroHere)
            {
                // arrived: the respawn point, and he is made whole
                if (!Reached) Combat.CombatFx.Burst(transform.position + Vector3.up * 0.5f, Vector2.up, Combat.CombatFx.Crimson, 10, 2f);
                Reached = true;
                player.SetSpawnPoint(transform.position);
                if (health != null)
                {
                    bool wounded = health.Model.Current < health.Model.Max - 1e-3f;
                    health.Refill();
                    if (wounded) Combat.CombatFx.Burst(player.transform.position + Vector3.up * 1f, Vector2.up, Combat.CombatFx.Crimson, 24, 3f);
                }
            }
            HeroHere = here;
            if (here) Nearby = this;
            else if (Nearby == this) Nearby = null;
            // (not in the frame the altar was closed: the same F would open it again)
            if (here && player.InputSource != null && player.InputSource.InteractPressed && CrimsonVeil.UI.AltarPanel.ClosedFrame != Time.frameCount)
                CrimsonVeil.UI.AltarPanel.Show();
        }
    }
}
