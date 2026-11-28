using System.Collections.Generic;
using CrimsonVeil.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The title screen (the MainMenu scene), in the game's own look (v0.112, the user: "brutálně vylepši design
    /// menu... obdélníčky, kde bude vždycky náhled kapitoly"): the ruins under the crimson tree drift slowly behind,
    /// sunk in ink at the edges, crimson leaves fall; the title is painted over a stroke of the scarf; the choices
    /// stand on the left, the chosen one on a crimson stroke of a brush. Hrát opens the chapters - a card each, its
    /// picture, its name; Nastavení the settings; Konec leaves. Space, Enter or the gamepad's A confirm, Esc goes back.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        /// <summary>The first chapter's scene, and the menu's own (both in the build's scene list).</summary>
        public const string GameScene = "Prototype_Ruins", MenuScene = "MainMenu", TestRoomScene = "TestRoom";

        /// <summary>A chapter on the chapter screen.</summary>
        public struct Chapter
        {
            public string scene, number, title, line;
        }

        /// <summary>The chapters, in order: the test room first (every weapon, a place to try things), then the story's.</summary>
        public static readonly Chapter[] Chapters =
        {
            new Chapter { scene = TestRoomScene, number = "ZKUŠEBNA", title = "Zkušební místnost", line = "Všechny zbraně, hrany, tunel a kruhy pro bič" },
            new Chapter { scene = GameScene, number = "KAPITOLA 1", title = "Ruiny pod rudým stromem", line = "Hradby, propast a mnich se štítem" },
        };

        public Button Play { get; private set; }
        public Button Settings { get; private set; }
        public Button Exit { get; private set; }
        public SettingsPanel SettingsPanel { get; private set; }
        /// <summary>The chapter cards (in the order of <see cref="Chapters"/>).</summary>
        public readonly List<Button> ChapterCards = new List<Button>();
        public bool ChaptersOpen => chapters != null && chapters.gameObject.activeSelf;

        RectTransform front, chapters, backdrop, frontBand;
        readonly List<RectTransform> petals = new List<RectTransform>();
        readonly List<Vector3> petalMotion = new List<Vector3>();
        float time;

        void Start()
        {
            CrimsonVeil.Core.GameClock.Reset();
            InputBindings.GameplayBlocked = false;
            _ = InputBindings.Asset;
            Build();
        }

        void Build()
        {
            var canvas = MenuKit.Canvas("Menu", 100, transform);
            var root = MenuKit.Panel("Backdrop", canvas.transform, new Color(0.04f, 0.04f, 0.05f, 1f));

            // the ruins behind, darkened, drifting slowly; the edges sunk in ink
            var pic = Resources.Load<Texture2D>("Chapters/TitleBackdrop");
            if (pic != null)
            {
                backdrop = MenuKit.Rect("Ruins", root);
                MenuKit.Fill(backdrop);
                backdrop.offsetMin = new Vector2(-60f, -40f); backdrop.offsetMax = new Vector2(60f, 40f);
                var raw = backdrop.gameObject.AddComponent<RawImage>();
                raw.texture = pic;
                raw.color = new Color(0.62f, 0.6f, 0.6f, 1f);
                raw.raycastTarget = false;
            }
            var vig = MenuKit.Fill(MenuKit.Rect("Vignette", root)).gameObject.AddComponent<RawImage>();
            vig.texture = MenuKit.Vignette;
            vig.raycastTarget = false;
            // a darker band on the left, under the choices
            var band = MenuKit.Rect("Band", root);
            band.anchorMin = new Vector2(0f, 0f); band.anchorMax = new Vector2(0.6f, 1f);
            band.offsetMin = band.offsetMax = Vector2.zero;
            var bandImg = band.gameObject.AddComponent<RawImage>();
            bandImg.texture = MenuKit.LeftShade;
            bandImg.raycastTarget = false;
            frontBand = band;
            MakePetals(root);

            front = MenuKit.Fill(MenuKit.Rect("Front", root));
            Title(front, new Vector2(150f, -170f));

            var column = MenuKit.Column("Choices", front, 14f, TextAnchor.UpperLeft);
            column.anchorMin = column.anchorMax = new Vector2(0f, 0.5f);
            column.pivot = new Vector2(0f, 0.5f);
            column.anchoredPosition = new Vector2(130f, -90f);
            column.sizeDelta = new Vector2(520f, 300f);
            Play = Choice(column, "Hrát", OpenChapters);
            Settings = Choice(column, "Nastavení", OpenSettings);
            Exit = Choice(column, "Konec", MenuKit.Quit);

            Hint(front, "Mezerník / Enter · potvrdit      Esc · zpět      W S · výběr");

            BuildChapters(root);
            SettingsPanel = SettingsPanel.Create(canvas.transform, CloseSettings);
            MenuKit.Select(Play);
        }

        /// <summary>"CRIMSON VEIL", painted over a crimson stroke of the scarf.</summary>
        static void Title(RectTransform parent, Vector2 at)
        {
            var stroke = MenuKit.Rect("Stroke", parent);
            stroke.anchorMin = stroke.anchorMax = new Vector2(0f, 1f);
            stroke.pivot = new Vector2(0f, 0.5f);
            stroke.anchoredPosition = at + new Vector2(-40f, -62f);
            stroke.sizeDelta = new Vector2(760f, 70f);
            stroke.localRotation = Quaternion.Euler(0f, 0f, 1.5f);
            var si = stroke.gameObject.AddComponent<Image>();
            si.sprite = MenuKit.BrushSprite;
            si.color = MenuKit.Crimson;
            si.raycastTarget = false;
            var title = MenuKit.Label(parent, "C R I M S O N    V E I L", 84, MenuKit.Paper, TextAnchor.MiddleLeft, "Title");
            title.fontStyle = FontStyle.Bold;
            var t = title.rectTransform;
            t.anchorMin = t.anchorMax = new Vector2(0f, 1f);
            t.pivot = new Vector2(0f, 0.5f);
            t.anchoredPosition = at;
            t.sizeDelta = new Vector2(1200f, 110f);
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(4f, -4f);
        }

        /// <summary>A choice on the left: its word, on a brush stroke that turns crimson when chosen.</summary>
        static Button Choice(RectTransform column, string label, System.Action onClick)
        {
            var b = MenuKit.Button(column, label, onClick, 440f, 74f, 40);
            var text = b.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.rectTransform.offsetMin = new Vector2(46f, 0f);
            var c = b.colors;
            c.normalColor = new Color(0f, 0f, 0f, 0f);   // (at rest only the word: the stroke comes with the choice)
            b.colors = c;
            return b;
        }

        static void Hint(RectTransform parent, string text)
        {
            var hint = MenuKit.Label(parent, text, 22, MenuKit.Dim);
            var h = hint.rectTransform;
            h.anchorMin = h.anchorMax = new Vector2(0.5f, 0f);
            h.anchoredPosition = new Vector2(0f, 44f);
        }

        /// <summary>The chapter screen: a card each - its picture, a crimson rule, its number, name and a line on it.</summary>
        void BuildChapters(RectTransform root)
        {
            chapters = MenuKit.Fill(MenuKit.Rect("Chapters", root));
            var heading = MenuKit.Label(chapters, "VYBER KAPITOLU", 54, MenuKit.Paper);
            heading.fontStyle = FontStyle.Bold;
            var hr = heading.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 1f);
            hr.anchoredPosition = new Vector2(0f, -150f);
            var rule = MenuKit.Rect("Rule", chapters);
            rule.anchorMin = rule.anchorMax = new Vector2(0.5f, 1f);
            rule.anchoredPosition = new Vector2(0f, -205f);
            rule.sizeDelta = new Vector2(460f, 26f);
            var ri = rule.gameObject.AddComponent<Image>();
            ri.sprite = MenuKit.BrushSprite; ri.color = MenuKit.Crimson; ri.raycastTarget = false;

            var row = MenuKit.Rect("Cards", chapters);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = new Vector2(0f, -10f);
            row.sizeDelta = new Vector2(1500f, 480f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 70f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false; h.childControlHeight = false;
            foreach (var ch in Chapters) ChapterCards.Add(Card(row, ch));
            Hint(chapters, "Mezerník / Enter · hrát      Esc · zpět      A D · výběr");
            chapters.gameObject.SetActive(false);
        }

        Button Card(RectTransform row, Chapter ch)
        {
            const float w = 620f, hgt = 440f;
            var rt = MenuKit.Rect("Card " + ch.scene, row);
            rt.sizeDelta = new Vector2(w, hgt);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.preferredHeight = hgt;
            // the frame: ink at rest, crimson chosen
            var frame = rt.gameObject.AddComponent<Image>();
            frame.color = Color.white;
            var b = rt.gameObject.AddComponent<Button>();
            var c = b.colors;
            c.normalColor = new Color(0.1f, 0.1f, 0.11f, 1f);
            c.highlightedColor = MenuKit.Crimson; c.selectedColor = MenuKit.Crimson;
            c.pressedColor = new Color(0.45f, 0.03f, 0.05f, 1f);
            c.fadeDuration = 0.08f;
            b.colors = c;
            b.targetGraphic = frame;
            string scene = ch.scene;
            b.onClick.AddListener(() => StartChapter(scene));
            var nav = new Navigation { mode = Navigation.Mode.Horizontal };
            b.navigation = nav;

            // the picture, inset in the frame
            var picRt = MenuKit.Rect("Picture", rt);
            picRt.anchorMin = new Vector2(0f, 0f); picRt.anchorMax = new Vector2(1f, 1f);
            picRt.offsetMin = new Vector2(8f, 8f); picRt.offsetMax = new Vector2(-8f, -8f);
            var raw = picRt.gameObject.AddComponent<RawImage>();
            raw.texture = Resources.Load<Texture2D>("Chapters/" + ch.scene);
            raw.color = raw.texture != null ? Color.white : new Color(0.15f, 0.15f, 0.16f, 1f);
            raw.raycastTarget = false;
            // ink rising from the bottom, under the words
            var shade = MenuKit.Rect("Shade", picRt);
            shade.anchorMin = new Vector2(0f, 0f); shade.anchorMax = new Vector2(1f, 0.48f);
            shade.offsetMin = shade.offsetMax = Vector2.zero;
            var si = shade.gameObject.AddComponent<Image>();
            si.color = new Color(0.02f, 0.02f, 0.025f, 0.82f); si.raycastTarget = false;
            var number = MenuKit.Label(picRt, ch.number, 22, new Color(0.9f, 0.18f, 0.18f, 1f), TextAnchor.MiddleLeft, "Number");
            Place(number.rectTransform, new Vector2(28f, 150f), new Vector2(560f, 30f));
            var title = MenuKit.Label(picRt, ch.title, 38, MenuKit.Paper, TextAnchor.MiddleLeft, "Name");
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, new Vector2(28f, 106f), new Vector2(560f, 48f));
            var line = MenuKit.Label(picRt, ch.line, 22, MenuKit.Dim, TextAnchor.MiddleLeft, "Line");
            Place(line.rectTransform, new Vector2(28f, 62f), new Vector2(560f, 30f));
            rt.gameObject.AddComponent<CardLift>();
            return b;
        }

        static void Place(RectTransform r, Vector2 bottomLeft, Vector2 size)
        {
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.pivot = Vector2.zero;
            r.anchoredPosition = bottomLeft;
            r.sizeDelta = size;
        }

        /// <summary>Crimson leaves drifting down over the ruins, as from the tree.</summary>
        void MakePetals(RectTransform root)
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 26; i++)
            {
                var p = MenuKit.Rect("Leaf", root);
                p.anchorMin = p.anchorMax = new Vector2(0f, 0f);
                float s = 8f + 14f * (float)rng.NextDouble();
                p.sizeDelta = new Vector2(s * 1.6f, s);
                p.anchoredPosition = new Vector2((float)rng.NextDouble() * 1920f, (float)rng.NextDouble() * 1080f);
                var img = p.gameObject.AddComponent<Image>();
                img.sprite = MenuKit.SoftSprite;
                img.color = new Color(0.75f, 0.04f, 0.06f, 0.35f + 0.5f * (float)rng.NextDouble());
                img.raycastTarget = false;
                petals.Add(p);
                petalMotion.Add(new Vector3(-20f - 40f * (float)rng.NextDouble(), -40f - 70f * (float)rng.NextDouble(), (float)rng.NextDouble() * 6.28f));
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            if (backdrop != null)
            {
                // the ruins drift: a slow sway and breath
                backdrop.anchoredPosition = new Vector2(Mathf.Sin(time * 0.05f) * 40f, Mathf.Sin(time * 0.07f) * 14f);
                backdrop.localScale = Vector3.one * (1.03f + 0.02f * Mathf.Sin(time * 0.04f));
            }
            for (int i = 0; i < petals.Count; i++)
            {
                var p = petals[i]; var m = petalMotion[i];
                var pos = p.anchoredPosition + new Vector2(m.x + Mathf.Sin(time * 1.3f + m.z) * 30f, m.y) * dt;
                if (pos.y < -30f) pos = new Vector2(pos.x + 300f, 1110f);
                if (pos.x < -30f) pos.x += 1980f;
                p.anchoredPosition = pos;
                p.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 2f + m.z) * 40f);
            }
            if (ChaptersOpen && SettingsPanel.MenuEscPressed()) CloseChapters();
        }

        /// <summary>Hrát: the chapters.</summary>
        public void OpenChapters()
        {
            front.gameObject.SetActive(false);
            frontBand.gameObject.SetActive(false);   // (the cards stand in the middle: no shade on the left)
            chapters.gameObject.SetActive(true);
            // (the story's latest chapter chosen first; the test room beside it)
            MenuKit.Select(ChapterCards[ChapterCards.Count - 1]);
        }

        public void CloseChapters()
        {
            chapters.gameObject.SetActive(false);
            frontBand.gameObject.SetActive(true);
            front.gameObject.SetActive(true);
            MenuKit.Select(Play);
        }

        /// <summary>A new game in the first chapter (kept for the tests and the old Hrát).</summary>
        public void StartGame() => StartChapter(GameScene);

        /// <summary>A new game in <paramref name="scene"/>: nothing carried over from an earlier run (v0.111).</summary>
        public void StartChapter(string scene)
        {
            CrimsonVeil.Core.GameClock.Reset();
            InputBindings.GameplayBlocked = false;
            CrimsonVeil.Combat.HeroProgress.Reset();
            SceneManager.LoadScene(scene);
        }

        public void OpenSettings()
        {
            front.gameObject.SetActive(false);
            SettingsPanel.Open();
        }

        void CloseSettings()
        {
            front.gameObject.SetActive(true);
            MenuKit.Select(Settings);
        }
    }

    /// <summary>A chosen card lifts a little towards the eye (it grows), and sinks back when left.</summary>
    public sealed class CardLift : MonoBehaviour
    {
        void Update()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            bool chosen = es != null && es.currentSelectedGameObject == gameObject;
            float want = chosen ? 1.05f : 0.97f;
            float s = Mathf.MoveTowards(transform.localScale.x, want, Time.unscaledDeltaTime * 0.8f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
