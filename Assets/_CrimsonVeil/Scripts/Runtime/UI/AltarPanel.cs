using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using UnityEngine;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// A checkpoint's altar (v0.110): the crimson the foes shed buys three upgrades, each up to three levels - a
    /// longer scarf, a stronger binding of wounds, a keener veil. The game stands still while it is open; F or Esc
    /// closes it.
    /// </summary>
    public sealed class AltarPanel : MonoBehaviour
    {
        public static AltarPanel Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.gameObject.activeSelf;

        readonly Button[] buy = new Button[3];
        readonly Text[] info = new Text[3];
        Text purse;
        int openedFrame;

        /// <summary>Opens the altar (made on first use, on the HUD).</summary>
        public static void Show()
        {
            if (Instance == null) Create(ScarfHud.Ensure().transform);
            Instance.Open();
        }

        static void Create(Transform hud)
        {
            var canvas = MenuKit.Canvas("Altar", 90, hud);
            var root = MenuKit.Panel("Panel", canvas.transform, MenuKit.Backdrop);
            var panel = canvas.gameObject.AddComponent<AltarPanel>();
            Instance = panel;
            var column = MenuKit.Column("Content", root, 18f, TextAnchor.UpperCenter);
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(1100f, 700f);
            MenuKit.Label(column, "OLTÁŘ", 44, MenuKit.Paper).rectTransform.sizeDelta = new Vector2(1100f, 60f);
            panel.purse = MenuKit.Label(column, "", 28, MenuKit.Crimson);
            panel.purse.rectTransform.sizeDelta = new Vector2(1100f, 44f);
            for (int i = 0; i < 3; i++)
            {
                var u = (HeroProgress.Upgrade)i;
                var row = MenuKit.Rect("Row " + u, column);
                row.sizeDelta = new Vector2(1100f, 96f);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 24f; h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = false; h.childControlHeight = false;
                var t = MenuKit.Label(row, "", 24, MenuKit.Paper, TextAnchor.MiddleLeft);
                t.rectTransform.sizeDelta = new Vector2(700f, 90f);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                panel.info[i] = t;
                int k = i;
                panel.buy[i] = MenuKit.Button(row, "Koupit", () => panel.Buy((HeroProgress.Upgrade)k), 340f, 64f, 26);
            }
            MenuKit.Label(column, "F nebo Esc zavře oltář.", 20, MenuKit.Dim).rectTransform.sizeDelta = new Vector2(1100f, 40f);
            canvas.gameObject.SetActive(false);
        }

        void Open()
        {
            if (gameObject.activeSelf) return;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            openedFrame = Time.frameCount;
            CrimsonVeil.Core.GameClock.Pause();
            InputBindings.GameplayBlocked = true;
            Refresh();
            MenuKit.Select(buy[0]);
        }

        /// <summary>The frame the altar was last closed (its Esc is not the pause menu's).</summary>
        public static int ClosedFrame { get; private set; } = -10;

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            ClosedFrame = Time.frameCount;
            gameObject.SetActive(false);
            CrimsonVeil.Core.GameClock.Resume();
            InputBindings.GameplayBlocked = false;
        }

        public void Buy(HeroProgress.Upgrade u)
        {
            if (HeroProgress.Buy(u))
            {
                GameSound.Play(Cue.Absorb);
                CombatFx.Shake(0.15f);
            }
            Refresh();
        }

        void Refresh()
        {
            purse.text = "Karmín: " + HeroProgress.Crimson;
            for (int i = 0; i < 3; i++)
            {
                var u = (HeroProgress.Upgrade)i;
                int level = HeroProgress.Level(u), cost = HeroProgress.Cost(u);
                info[i].text = $"{HeroProgress.Name(u)}  {level}/{HeroProgress.MaxLevel}\n<size=20>{HeroProgress.What(u)}</size>";
                info[i].supportRichText = true;
                MenuKit.SetLabel(buy[i], cost >= 0 ? "Koupit za " + cost : HeroProgress.CappedByChapter(u) ? "Víc až v další kapitole" : "Nejvýš");
                buy[i].interactable = cost >= 0 && HeroProgress.Crimson >= cost;
            }
        }

        void Update()
        {
            if (Time.frameCount <= openedFrame) return;   // (the F that opened it is not the one that closes it)
            var interact = InputBindings.Action("Interact");
            if (SettingsPanel.MenuEscPressed() || (interact != null && interact.WasPressedThisFrame())) Close();
        }

        void OnDestroy()
        {
            if (gameObject.activeSelf) { CrimsonVeil.Core.GameClock.Resume(); InputBindings.GameplayBlocked = false; }
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>
    /// The crimson carried, small in the bottom-right corner (v0.113): a drop and its count - and above the scarf at
    /// the bottom the key to press where it can be used (the altar at a checkpoint, binding a wound when the knot is
    /// full). The knot and the shards are drawn by the scarf and the life rings themselves.
    /// </summary>
    public sealed class ProgressHud : MonoBehaviour
    {
        Text crimson, hint;
        PlayerHealth health;
        int shownCrimson = -1;
        float bump;
        RectTransform drop;

        public static ProgressHud Create(Transform hud)
        {
            var rt = MenuKit.Rect("Crimson", hud);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-48f, 40f);
            rt.sizeDelta = new Vector2(200f, 48f);
            var p = rt.gameObject.AddComponent<ProgressHud>();
            p.drop = MenuKit.Rect("Drop", rt);
            p.drop.anchorMin = p.drop.anchorMax = new Vector2(1f, 0.5f);
            p.drop.pivot = new Vector2(1f, 0.5f);
            p.drop.anchoredPosition = new Vector2(-70f, 0f);
            p.drop.sizeDelta = new Vector2(22f, 30f);
            var di = p.drop.gameObject.AddComponent<Image>();
            di.sprite = MenuKit.SoftSprite;
            di.color = new Color(0.8f, 0.04f, 0.06f, 1f);
            di.raycastTarget = false;
            p.crimson = MenuKit.Label(rt, "", 30, MenuKit.Paper, TextAnchor.MiddleRight, "Count");
            p.crimson.fontStyle = FontStyle.Bold;
            var cr = p.crimson.rectTransform;
            cr.anchorMin = cr.anchorMax = new Vector2(1f, 0.5f);
            cr.pivot = new Vector2(1f, 0.5f);
            cr.anchoredPosition = Vector2.zero;
            cr.sizeDelta = new Vector2(64f, 48f);
            var sh = p.crimson.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.8f); sh.effectDistance = new Vector2(2f, -2f);
            var h = MenuKit.Rect("Hint", hud);
            h.anchorMin = h.anchorMax = new Vector2(0.5f, 0f);
            h.anchoredPosition = new Vector2(0f, 150f);
            h.sizeDelta = new Vector2(800f, 50f);
            p.hint = MenuKit.Label(h, "", 26, MenuKit.Paper);
            MenuKit.Fill(p.hint.rectTransform);
            var hs = p.hint.gameObject.AddComponent<Shadow>();
            hs.effectColor = new Color(0f, 0f, 0f, 0.85f); hs.effectDistance = new Vector2(2f, -2f);
            return p;
        }

        void Update()
        {
            if (health == null) health = FindFirstObjectByType<PlayerHealth>();
            // a drop gathered: the count jumps a little
            if (HeroProgress.Crimson != shownCrimson)
            {
                if (shownCrimson >= 0 && HeroProgress.Crimson > shownCrimson) bump = 1f;
                shownCrimson = HeroProgress.Crimson;
                crimson.text = shownCrimson.ToString();
            }
            bump = Mathf.MoveTowards(bump, 0f, Time.unscaledDeltaTime * 4f);
            float k = 1f + 0.25f * bump;
            drop.localScale = new Vector3(k, k, 1f);
            string text = "";
            if (!AltarPanel.IsOpen && !InputBindings.GameplayBlocked)
            {
                if (CrimsonVeil.World.Checkpoint.Nearby != null)
                    text = InputBindings.Shown(InputBindings.Find("Interact"), false) + "   oltář";
                else if (health != null && health.IsAlive && HeroProgress.Mend >= 1f && health.Model.Current < health.Model.Max - 1e-3f)
                    text = InputBindings.Shown(InputBindings.Find("Heal"), false) + " (držet)   obvázat ránu";
            }
            hint.text = text;
        }
    }
}
