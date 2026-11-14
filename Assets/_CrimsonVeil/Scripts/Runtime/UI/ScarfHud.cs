using CrimsonVeil.Combat;
using CrimsonVeil.Veil;
using UnityEngine;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The HUD: the life gauge in the top-left corner, drawn as the hero's scarf (see <see cref="ScarfHealthBar"/>).
    /// Created automatically when the hero comes to life, so every level has it without wiring.
    /// </summary>
    public sealed class ScarfHud : MonoBehaviour
    {
        public ScarfHealthBar Bar { get; private set; }

        PlayerHealth health;
        VeilController veil;
        float shownHealth = -1f, trailing, flash, trailHold;

        /// <summary>Makes sure the scene has a HUD; returns it.</summary>
        public static ScarfHud Ensure()
        {
            var existing = FindFirstObjectByType<ScarfHud>();
            if (existing != null) return existing;

            var go = new GameObject("HUD");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var hud = go.AddComponent<ScarfHud>();

            var barGo = new GameObject("HealthScarf", typeof(RectTransform));
            barGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)barGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(34f, -26f);
            rt.sizeDelta = new Vector2(470f, 96f);
            hud.Bar = barGo.AddComponent<ScarfHealthBar>();
            hud.Bar.raycastTarget = false;
            return hud;
        }

        void Awake()
        {
            if (Bar == null) Bar = GetComponentInChildren<ScarfHealthBar>();
        }

        void Update()
        {
            if (health == null)
            {
                health = FindFirstObjectByType<PlayerHealth>();
                if (health == null || health.Model == null) return;
                veil = health.GetComponent<VeilController>();
                health.Model.Damaged += _ => { flash = 1f; trailHold = 0.45f; };
            }
            if (Bar == null || health.Model == null) return;
            float dt = Time.unscaledDeltaTime;
            var m = health.Model;
            if (shownHealth < 0f) shownHealth = trailing = m.Current;
            // Life is shown at once (no doubt in a fight); what was just lost glows on as embers, then burns away.
            shownHealth = m.Current;
            trailHold -= dt;
            if (trailing < shownHealth) trailing = shownHealth;
            else if (trailHold <= 0f) trailing = Mathf.MoveTowards(trailing, shownHealth, dt * 2.5f);
            flash = Mathf.MoveTowards(flash, 0f, dt * 3f);

            Bar.maxHealth = m.Max;
            Bar.health = shownHealth;
            Bar.trailingHealth = trailing;
            Bar.hurtFlash = flash;
            Bar.charge = veil != null ? veil.Charge.Value : 0f;
        }
    }
}
