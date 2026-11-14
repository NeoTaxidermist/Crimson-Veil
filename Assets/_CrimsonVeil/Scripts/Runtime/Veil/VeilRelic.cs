using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// A relic on a stone plinth: a crimson glyph of a weapon floating above it. Touching it teaches the veil
    /// that weapon (it can be cast from then on). Relics already behind the hero when play starts (playing
    /// from the middle of a level, a checkpoint) count as found.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class VeilRelic : MonoBehaviour
    {
        public VeilForm form = VeilForm.Spear;

        static readonly List<VeilRelic> all = new List<VeilRelic>();
        public static IReadOnlyList<VeilRelic> All => all;

        public bool Collected { get; private set; }
        Transform glyph;
        Vector3 glyphRest;
        float t;

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            glyph = transform.Find("Glyph");
            if (glyph != null) glyphRest = glyph.localPosition;
        }

        void OnDisable() => all.Remove(this);

        void Update()
        {
            if (glyph == null || Collected) return;
            t += Time.deltaTime;
            glyph.localPosition = glyphRest + Vector3.up * (Mathf.Sin(t * 1.6f) * 0.12f);
            glyph.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.9f) * 35f, Mathf.Sin(t * 1.3f) * 6f);
        }

        void OnTriggerEnter(Collider other)
        {
            var veil = other.GetComponentInParent<VeilController>();
            if (veil != null) Collect(veil);
        }

        /// <summary>Teaches <paramref name="veil"/> the relic's weapon; the glyph dissolves into crimson sparks.</summary>
        public void Collect(VeilController veil, bool quiet = false)
        {
            if (Collected) return;
            Collected = true;
            veil.Unlock(form);
            if (glyph != null)
            {
                if (!quiet) CrimsonVeil.Combat.CombatFx.Burst(glyph.position, Vector2.up, CrimsonVeil.Combat.CombatFx.Crimson, 40, 5f);
                glyph.gameObject.SetActive(false);
            }
            var glint = transform.Find("Glint");
            if (glint != null) glint.gameObject.SetActive(false);
        }
    }
}
