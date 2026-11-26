using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A body struck flashes for a moment: a foe pale, the hero crimson. The ink silhouettes' <c>_HitFlash</c> is set on
    /// all their renderers and fades out in real time (the hit-stop slows the game's own clock nearly to a halt).
    /// </summary>
    public sealed class HitFlash : MonoBehaviour
    {
        /// <summary>A foe's flash: bone-pale (the grade leaves anything not red grey, so it reads as a white flash).</summary>
        public static readonly Color Pale = new Color(0.95f, 0.93f, 0.9f, 1f);
        /// <summary>The hero's flash when he is struck.</summary>
        public static readonly Color Wound = new Color(0.85f, 0.05f, 0.06f, 1f);

        public const float FoeSeconds = 0.13f, HeroSeconds = 0.22f;

        static readonly int FlashId = Shader.PropertyToID("_HitFlash");
        static readonly int ColorId = Shader.PropertyToID("_FlashColor");

        Renderer[] renderers;
        MaterialPropertyBlock block;
        float until, length;
        Color color;

        /// <summary>The flash's strength <paramref name="t"/> into it (0..1 of its length): full at once, then away.</summary>
        public static float Strength(float t) => t < 0f || t >= 1f ? 0f : (1f - t) * (1f - t);

        /// <summary>Flashes <paramref name="body"/> (and what it carries) in <paramref name="c"/> for <paramref name="seconds"/>.</summary>
        public static HitFlash Flash(GameObject body, Color c, float seconds)
        {
            if (body == null) return null;
            var f = body.GetComponent<HitFlash>();
            if (f == null) f = body.AddComponent<HitFlash>();
            f.color = c;
            f.length = Mathf.Max(0.01f, seconds);
            f.until = Time.unscaledTime + f.length;
            f.enabled = true;
            f.Apply(1f);
            return f;
        }

        /// <summary>How strongly it flashes now (0 when not).</summary>
        public float Now => enabled ? Strength(1f - (until - Time.unscaledTime) / length) : 0f;

        void Apply(float strength)
        {
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                r.GetPropertyBlock(block);
                block.SetFloat(FlashId, strength);
                block.SetColor(ColorId, color);
                r.SetPropertyBlock(block);
            }
        }

        void LateUpdate()
        {
            float left = until - Time.unscaledTime;
            if (left <= 0f)
            {
                Apply(0f);
                enabled = false;
                return;
            }
            Apply(Strength(1f - left / length));
        }
    }
}
