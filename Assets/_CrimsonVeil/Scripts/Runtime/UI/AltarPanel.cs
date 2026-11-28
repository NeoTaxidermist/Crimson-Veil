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
                MenuKit.SetLabel(buy[i], cost < 0 ? "Nejvýš" : "Koupit za " + cost);
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
    /// The HUD's tally under the life gauge (v0.110): the crimson carried, the knot a wound is bound with (filling as
    /// blows land), the shards of a life found - and the key to press where it can be used (the altar at a
    /// checkpoint, binding a wound when the knot is full).
    /// </summary>
    public sealed class ProgressHud : MonoBehaviour
    {
        Text crimson, shards, hint;
        Image knot, knotBack;
        PlayerHealth health;

        public static ProgressHud Create(Transform hud)
        {
            var rt = MenuKit.Rect("Progress", hud);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -128f);
            rt.sizeDelta = new Vector2(420f, 40f);
            var p = rt.gameObject.AddComponent<ProgressHud>();
            p.knotBack = Bar(rt, "KnotBack", new Color(0.1f, 0.1f, 0.1f, 0.7f));
            p.knot = Bar(rt, "Knot", MenuKit.Crimson);
            p.knot.type = Image.Type.Filled; p.knot.fillMethod = Image.FillMethod.Horizontal;
            p.crimson = MenuKit.Label(rt, "", 26, new Color(0.9f, 0.12f, 0.12f, 1f), TextAnchor.MiddleLeft, "Crimson");
            p.crimson.rectTransform.anchoredPosition = new Vector2(150f, 0f);
            p.crimson.rectTransform.sizeDelta = new Vector2(140f, 40f);
            p.shards = MenuKit.Label(rt, "", 22, MenuKit.Paper, TextAnchor.MiddleLeft, "Shards");
            p.shards.rectTransform.anchoredPosition = new Vector2(270f, 0f);
            p.shards.rectTransform.sizeDelta = new Vector2(140f, 40f);
            var h = MenuKit.Rect("Hint", hud);
            h.anchorMin = h.anchorMax = new Vector2(0.5f, 0f);
            h.anchoredPosition = new Vector2(0f, 90f);
            h.sizeDelta = new Vector2(800f, 50f);
            p.hint = MenuKit.Label(h, "", 28, MenuKit.Paper);
            MenuKit.Fill(p.hint.rectTransform);
            return p;
        }

        static Image Bar(RectTransform parent, string name, Color c)
        {
            var r = MenuKit.Rect(name, parent);
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(120f, 12f);
            var img = r.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        void Update()
        {
            if (health == null) health = FindFirstObjectByType<PlayerHealth>();
            knot.fillAmount = HeroProgress.Mend;
            crimson.text = "● " + HeroProgress.Crimson;
            int s = HeroProgress.Shards % HeroProgress.ShardsPerHeart;
            shards.text = HeroProgress.Shards > 0 ? "◆ " + s + "/" + HeroProgress.ShardsPerHeart : "";
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
