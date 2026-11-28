using CrimsonVeil.Combat;
using CrimsonVeil.Veil;
using UnityEngine;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The HUD (v0.113, the user's choice): life in the top-left corner as crimson leaves on a twig
    /// (<see cref="LifeLeaves"/>); the scarf's strength at the bottom in the middle as the scarf itself, a fluttering
    /// ribbon of flame with the weapons' costs marked on it and the mend knot at its start (<see cref="EnergyScarf"/>);
    /// the crimson carried in the bottom-right corner (<see cref="ProgressHud"/>). (Until v0.112 a scarf in the corner
    /// was the life, a thread under it the strength.) Created automatically when the hero comes to life.
    /// </summary>
    public sealed class ScarfHud : MonoBehaviour
    {
        public LifeLeaves Life { get; private set; }
        public EnergyScarf Energy { get; private set; }

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

            // (each on a canvas of its own: the scarf's flutter, redrawn, re-batches only itself)
            hud.Life = Widget<LifeLeaves>(go.transform, "Life", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(520f, 80f));
            hud.Energy = Widget<EnergyScarf>(go.transform, "Energy", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(620f, 64f));
            Minimap.Create(go.transform);   // the map, on TAB
            PlaytestLog.Create(go);         // the playtest's times and deaths, written to Playtests/
            HurtVignette.Create(go.transform); // struck: the edges of the picture run red
            PauseMenu.Create(go);           // Esc: the game stands still, the settings, back to the title
            ProgressHud.Create(go.transform); // the crimson carried, the mend knot, the shards (v0.110)
            return hud;
        }

        static T Widget<T>(Transform hud, string name, Vector2 anchor, Vector2 pivot, Vector2 at, Vector2 size) where T : MaskableGraphic
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(hud, false);
            go.AddComponent<Canvas>();
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = at;
            rt.sizeDelta = size;
            var g = go.AddComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        void Awake()
        {
            if (Life == null) Life = GetComponentInChildren<LifeLeaves>();
            if (Energy == null) Energy = GetComponentInChildren<EnergyScarf>();
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
            if (Life == null || Energy == null || health.Model == null) return;
            float dt = Time.unscaledDeltaTime;
            var m = health.Model;
            if (shownHealth < 0f) shownHealth = trailing = m.Current;
            // Life is shown at once (no doubt in a fight); what was just lost glows on as embers, then burns away.
            shownHealth = m.Current;
            trailHold -= dt;
            if (trailing < shownHealth) trailing = shownHealth;
            else if (trailHold <= 0f) trailing = Mathf.MoveTowards(trailing, shownHealth, dt * 2.5f);
            flash = Mathf.MoveTowards(flash, 0f, dt * 3f);

            Life.maxHealth = m.Max;
            Life.health = shownHealth;
            Life.trailingHealth = trailing;
            Life.hurtFlash = flash;
            Life.shards = HeroProgress.Shards % HeroProgress.ShardsPerHeart;
            Life.mend = HeroProgress.Mend;
            Life.mendAmount = HeroProgress.HealAmount;
            Life.mending = health.Mending;
            Energy.charge = veil != null ? veil.Charge.Value : 0f;
            Energy.capacity = veil != null ? veil.Charge.Capacity : 1f;
            Energy.mend = HeroProgress.Mend;
            Energy.hurtFlash = flash;
        }
    }
}
