using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// A ring the hero can lash the whip around: with the whip formed, an attack in reach latches onto it,
    /// the hero swings beneath it (left/right pumps, up/down climbs the whip) and jumps off with the swing's
    /// momentum — higher or further than a plain jump. The transform is the point the whip wraps around.
    /// A crimson glint shows when it is the one the whip would catch.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GrapplePoint : MonoBehaviour
    {
        [Tooltip("How far (m) from the hero's hands the whip reaches this ring.")]
        [Min(1f)] public float reach = 7.5f;

        static readonly List<GrapplePoint> all = new List<GrapplePoint>();
        public static IReadOnlyList<GrapplePoint> All => all;

        /// <summary>Lit while this is the ring the whip would catch.</summary>
        public bool Highlighted { get; private set; }
        Transform glint;
        Vector3 glintScale = Vector3.one;
        float pulse;

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
            glint = transform.Find("Glint");
            if (glint != null) glintScale = glint.localScale;
            SetHighlighted(false);
        }

        void OnDisable() => all.Remove(this);

        public void SetHighlighted(bool on)
        {
            Highlighted = on;
            if (glint != null) glint.gameObject.SetActive(on);
        }

        void Update()
        {
            if (!Application.isPlaying || glint == null || !Highlighted) return;
            pulse += Time.deltaTime;
            glint.localScale = glintScale * (1f + 0.18f * Mathf.Sin(pulse * 7f));
        }

        public Vector2 Point => transform.position;

        /// <summary>
        /// The ring the whip catches from hands at <paramref name="grip"/>: in reach, above the hands, not
        /// behind a wall; rings ahead of the hero win over those behind, nearer over further.
        /// </summary>
        public static GrapplePoint FindBest(Vector2 grip, int facing, LayerMask blockers)
        {
            GrapplePoint best = null;
            float bestScore = float.MaxValue;
            foreach (var g in all)
            {
                if (g == null || !g.isActiveAndEnabled) continue;
                if (!CanReach(grip, g.Point, g.reach)) continue;
                Vector2 d = g.Point - grip;
                if (blockers != 0 && Physics.Linecast(new Vector3(grip.x, grip.y, 0f), new Vector3(g.Point.x, g.Point.y, 0f), blockers, QueryTriggerInteraction.Ignore))
                    continue;
                float score = d.magnitude + (d.x * facing < -0.5f ? 4f : 0f);
                if (score < bestScore) { bestScore = score; best = g; }
            }
            return best;
        }

        /// <summary>In reach and high enough above the hands to hang from.</summary>
        public static bool CanReach(Vector2 grip, Vector2 ring, float reach)
        {
            Vector2 d = ring - grip;
            return d.y > 0.6f && d.sqrMagnitude <= reach * reach;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.1f, 0.15f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }

        void OnDrawGizmosSelected()
        {
            // Reach: the hero's hands must be inside this circle (and below the ring) to latch on.
            Gizmos.color = new Color(0.9f, 0.1f, 0.15f, 0.35f);
            const int seg = 48;
            Vector3 c = transform.position, prev = c + new Vector3(reach, 0f, 0f);
            for (int i = 1; i <= seg; i++)
            {
                float a = Mathf.PI * 2f * i / seg;
                var p = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * reach;
                if (Mathf.Sin(a) < 0f || Mathf.Sin(a - Mathf.PI * 2f / seg) < 0f) Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
