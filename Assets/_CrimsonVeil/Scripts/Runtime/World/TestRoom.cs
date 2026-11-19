using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// The test room left of the start (plain grey blocks, no enemies, no grass): a place to look at the hero's
    /// movement and animation. Walking in, the hero knows every weapon.
    /// </summary>
    public sealed class TestRoom : MonoBehaviour
    {
        [Tooltip("The room's span along x (world).")]
        public float minX = -61f, maxX = -14.5f;

        /// <summary>Every weapon of the scarf.</summary>
        public static readonly VeilForm[] AllWeapons = { VeilForm.Sword, VeilForm.Whip, VeilForm.Spear, VeilForm.Shield, VeilForm.Bow };

        PlayerController player;
        VeilController veil;

        public bool Contains(float x) => x >= minX && x <= maxX;

        void Update()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
                veil = player.GetComponent<VeilController>();
            }
            if (veil != null && Contains(player.transform.position.x)) Arm(veil);
        }

        /// <summary>Teaches the veil every weapon it doesn't know yet.</summary>
        public static void Arm(VeilController veil)
        {
            foreach (var f in AllWeapons)
                if (!veil.IsUnlocked(f)) veil.Unlock(f);
        }
    }
}
