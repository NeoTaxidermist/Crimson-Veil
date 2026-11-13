using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// The fallen kite's cloth. The hero takes it by touching it; from then on he can glide for
    /// <see cref="Seconds"/> seconds in total (hold jump while falling).
    /// </summary>
    public sealed class KiteCloth : MonoBehaviour
    {
        public float Seconds = 8f;
        bool taken;

        void Update()
        {
            if (taken) return;
            var hero = Object.FindFirstObjectByType<PlayerController>();
            if (hero == null || hero.Motor == null) return;
            Vector3 d = hero.transform.position + Vector3.up * 0.9f - transform.position;
            d.z = 0f;
            if (d.sqrMagnitude > 1.1f * 1.1f) return;
            taken = true;
            hero.Motor.GlideTime += Seconds;
            CombatFx.Burst(transform.position, Vector2.up, CombatFx.Crimson, 24, 3f);
            gameObject.AddComponent<CrimsonVeil.World.ShrinkAndDestroy>().Begin(0.6f);
        }
    }
}
