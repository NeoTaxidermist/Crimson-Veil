using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A swarm of ink crows that wheels around one point, scatters when struck and closes in again.
    /// It shadows the hero from above and every few seconds dives straight through him.
    /// Every crow is its own target: the whip's long lash downs several at once, a sword one at a time.
    /// </summary>
    public sealed class CrowSwarm : MonoBehaviour
    {
        [SerializeField] float sightRange = 13f;
        [SerializeField] float hoverHeight = 2.6f;
        [SerializeField] float diveInterval = 3.6f;
        [SerializeField] float diveTime = 1.1f;
        [SerializeField] float peckDamage = 0.25f;

        public int Alive => crows.Count;
        public bool IsDiving => diveClock >= 0f;
        public System.Action Destroyed;

        readonly List<Crow> crows = new List<Crow>();
        CombatTarget target;
        Vector3 centre, home;
        float diveTimer, diveClock = -1f, scatter, contactCooldown;
        Vector3 diveFrom, diveTo;

        void Awake()
        {
            home = centre = transform.position;
            foreach (var c in GetComponentsInChildren<Crow>())
            {
                c.Swarm = this;
                c.Phase = Random.value * Mathf.PI * 2f;
                c.Radius = Random.Range(0.7f, 1.4f);
                c.Speed = Random.Range(2.2f, 3.4f) * (Random.value < 0.5f ? -1f : 1f);
                crows.Add(c);
            }
            diveTimer = diveInterval;
        }

        void Start() => target = CombatTarget.Find();

        public void OnCrowDown(Crow c)
        {
            crows.Remove(c);
            scatter = 1f; // the rest scatter for a moment
            if (crows.Count == 0)
            {
                Destroyed?.Invoke();
                Destroy(gameObject, 3f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || crows.Count == 0) return;
            if (!target.IsValid) target = CombatTarget.Find();
            bool hunting = target.IsValid && target.IsAlive && Vector3.Distance(target.Position, centre) < sightRange;
            Vector3 chest = hunting ? target.Position + Vector3.up * 0.9f : home;
            scatter = Mathf.MoveTowards(scatter, 0f, dt * 0.8f);
            contactCooldown -= dt;

            // The swarm centre: shadow the hero from above, then dive through him and climb out the far side.
            if (diveClock >= 0f)
            {
                diveClock += dt;
                float u = Mathf.Clamp01(diveClock / diveTime);
                centre = CrowSwarmLogic.DivePath(diveFrom, diveTo, u);
                if (u >= 1f) diveClock = -1f;
            }
            else
            {
                Vector3 want = hunting ? chest + new Vector3(Mathf.Sin(Time.time * 0.6f) * 1.5f, hoverHeight, 0f) : home;
                centre = Vector3.Lerp(centre, want, 1f - Mathf.Exp(-1.6f * dt));
                if (hunting)
                {
                    diveTimer -= dt;
                    if (diveTimer <= 0f)
                    {
                        diveTimer = diveInterval;
                        diveClock = 0f;
                        diveFrom = centre;
                        diveTo = chest + new Vector3(Mathf.Sign(chest.x - centre.x + 1e-3f) * 4f, 2.5f, 0f);
                    }
                }
            }

            // Each crow wheels around the centre on its own orbit; scattering throws them wide.
            float spread = 1f + scatter * 2.2f;
            foreach (var c in crows)
            {
                c.Phase += c.Speed * dt;
                Vector3 orbit = new Vector3(Mathf.Cos(c.Phase) * c.Radius * 1.4f, Mathf.Sin(c.Phase * 1.3f) * c.Radius * 0.7f, Mathf.Sin(c.Phase) * 0.3f) * spread;
                Vector3 want = centre + orbit;
                Vector3 pos = c.transform.position;
                Vector3 next = Vector3.Lerp(pos, want, 1f - Mathf.Exp(-4f * dt));
                c.Velocity = (next - pos) / dt;
                c.transform.position = next;

                if (hunting && contactCooldown <= 0f && (chest - next).sqrMagnitude < 0.45f * 0.45f)
                {
                    contactCooldown = 0.5f;
                    target.Damageable.ReceiveHit(new DamageInfo
                    {
                        amount = peckDamage,
                        knockback = new Vector2(Mathf.Sign(c.Velocity.x + 1e-3f) * 3f, 2f),
                        point = next,
                        kind = DamageKind.Contact,
                        source = c.gameObject,
                    });
                }
            }
        }

        public static void BuildVisual(Transform root, Material ink, Material accent, int count = 8)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Crow" + i);
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(Mathf.Cos(i * 0.8f) * 1.1f, Mathf.Sin(i * 1.1f) * 0.6f, 0f);
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.28f;
                go.AddComponent<Crow>();
                Crow.BuildVisual(go.transform, ink, accent);
            }
        }
    }

    /// <summary>Crow swarm rules, engine independent.</summary>
    public static class CrowSwarmLogic
    {
        /// <summary>Dive: swoop down through the target (lowest at the middle) and climb out the other side.</summary>
        public static Vector3 DivePath(Vector3 from, Vector3 to, float u)
        {
            u = Mathf.Clamp01(u);
            Vector3 straight = Vector3.Lerp(from, to, u);
            float lowest = Mathf.Min(from.y, to.y) - 2.6f;
            float dip = Mathf.Sin(u * Mathf.PI);
            straight.y = Mathf.Lerp(straight.y, lowest, dip);
            return straight;
        }
    }
}
