using UnityEngine;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The hero struck: the edges of the picture run crimson for a moment and ebb away (in real time, through the
    /// hit-stop). On the HUD, made by <see cref="ScarfHud"/>.
    /// </summary>
    public sealed class HurtVignette : MonoBehaviour
    {
        /// <summary>How long (s) the red takes to ebb away from a full pulse.</summary>
        public const float Seconds = 0.55f;
        /// <summary>The deepest the edges go (alpha) at a full pulse.</summary>
        public const float Deepest = 0.42f;

        static HurtVignette instance;
        RawImage image;
        float level;

        /// <summary>How strongly the edges are red now (0..1).</summary>
        public static float Level => instance != null ? instance.level : 0f;

        public static HurtVignette Create(Transform hud)
        {
            var rt = MenuKit.Fill(MenuKit.Rect("HurtVignette", hud));
            rt.SetAsFirstSibling();   // (under the gauge and the map)
            var v = rt.gameObject.AddComponent<HurtVignette>();
            v.image = rt.gameObject.AddComponent<RawImage>();
            v.image.texture = Ring(128);
            v.image.color = new Color(0.75f, 0.02f, 0.03f, 0f);
            v.image.raycastTarget = false;
            instance = v;
            return v;
        }

        /// <summary>Runs the edges red: <paramref name="strength"/> 1 for a wound, less for a blow the scarf took.</summary>
        public static void Pulse(float strength)
        {
            if (instance == null) return;
            instance.level = Mathf.Max(instance.level, Mathf.Clamp01(strength));
        }

        void Update()
        {
            level = Mathf.MoveTowards(level, 0f, Time.unscaledDeltaTime / Seconds);
            var c = image.color;
            c.a = Deepest * level * level * (3f - 2f * level);
            image.color = c;
            // (not drawn at all when clear: a screen-sized quad drawn at nothing is still the whole picture blended over)
            image.enabled = c.a > 0.001f;
        }

        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>A soft ring: clear in the middle, opaque at the edges and corners.</summary>
        public static Texture2D Ring(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "HurtRing" };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(u * u * 0.8f + v * v);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 1.3f, d));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }
    }
}
