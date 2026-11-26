using System.Collections.Generic;
using CrimsonVeil.Player;
using CrimsonVeil.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// The map of the level, opened and closed with TAB (the gamepad's Select / View button). It draws the level's
    /// ground (every <see cref="TerrainBlock"/>) in pale ink on a dark sheet, the hero as a crimson mark, the
    /// checkpoints and the whip rings. For now the whole level shows (<see cref="RevealAll"/>); the map already keeps
    /// what the hero has explored, so turned off, everything else lies in fog until he has been there.
    /// Created with the HUD, so every level has it without wiring.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        [Tooltip("Show the whole level. Off: only what the hero has explored, the rest in fog.")]
        public bool revealAll = true;
        [Tooltip("How far around the hero the map is uncovered as he goes (m).")]
        public float revealRadius = 9f;

        public static readonly Color Sheet = new Color(0.04f, 0.04f, 0.05f, 0.96f);
        /// <summary>The open air of the level on the map (the picture is opaque: the game doesn't show through it).</summary>
        public static readonly Color Air = new Color(0.12f, 0.12f, 0.13f, 1f);
        public static readonly Color Ground = new Color(0.82f, 0.8f, 0.76f, 1f);
        public static readonly Color GroundEdge = new Color(1f, 0.98f, 0.94f, 1f);
        public static readonly Color Fog = new Color(0.02f, 0.02f, 0.025f, 0.97f);
        public static readonly Color Crimson = new Color(0.86f, 0.06f, 0.08f, 1f);

        public bool IsOpen { get; private set; }
        public MinimapModel Model { get; private set; }
        /// <summary>The hero's marker on the map (for tests: where he is drawn).</summary>
        public RectTransform HeroMarker => hero;

        RectTransform panel, picture, hero;
        Text titleText;
        PlaytestLog playtest;
        RawImage image;
        Texture2D tex;
        Transform player;
        readonly List<(RectTransform mark, Vector2 world)> marks = new List<(RectTransform, Vector2)>();

        /// <summary>Builds the map under the HUD's canvas (closed).</summary>
        public static Minimap Create(Transform canvas)
        {
            var existing = canvas.GetComponentInChildren<Minimap>(true);
            if (existing != null) return existing;
            var go = new GameObject("Minimap", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var m = go.AddComponent<Minimap>();
            m.BuildUi();
            return m;
        }

        void BuildUi()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            panel = NewRect("Panel", transform);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = Sheet;
            bg.raycastTarget = false;

            var title = NewRect("Title", panel);
            title.anchorMin = new Vector2(0f, 1f); title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.sizeDelta = new Vector2(0f, 48f);
            title.anchoredPosition = new Vector2(0f, -8f);
            var text = titleText = title.gameObject.AddComponent<Text>();
            text.text = "MAPA  ·  TAB";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.85f, 0.82f, 0.78f, 0.9f);
            text.raycastTarget = false;

            picture = NewRect("Picture", panel);
            picture.anchorMin = picture.anchorMax = new Vector2(0.5f, 0.5f);
            image = picture.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;

            hero = Marker("Hero", 22f, Crimson, true);
            panel.gameObject.SetActive(false);
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        RectTransform Marker(string name, float size, Color color, bool diamond)
        {
            var r = NewRect(name, picture);
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.sizeDelta = new Vector2(size, size);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (diamond) r.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return r;
        }

        /// <summary>Reads the level: its ground, checkpoints and rings.</summary>
        public void Scan()
        {
            var rects = new List<Rect>();
            foreach (var b in FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None))
            {
                if (!b.isActiveAndEnabled || !b.solid) continue;
                var tb = b.TopBounds;
                rects.Add(Rect.MinMaxRect(tb.min.x, tb.min.y, tb.max.x, tb.max.y));
            }
            Model = new MinimapModel(rects) { RevealAll = revealAll, RevealRadius = revealRadius };
            foreach (var (m, _) in marks) if (m != null) Destroy(m.gameObject);
            marks.Clear();
            foreach (var c in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
                marks.Add((Marker("Checkpoint", 12f, new Color(0.95f, 0.92f, 0.85f, 1f), true), c.transform.position));
            foreach (var g in FindObjectsByType<GrapplePoint>(FindObjectsSortMode.None))
                marks.Add((Marker("Ring", 9f, new Color(0.7f, 0.68f, 0.64f, 1f), false), g.transform.position));
            hero.SetAsLastSibling();
            Layout();
            Redraw();
        }

        void Layout()
        {
            // as wide as the screen allows (the level is long and low), the sheet a margin round it
            var canvas = GetComponentInParent<Canvas>();
            var size = canvas != null ? ((RectTransform)canvas.transform).rect.size : new Vector2(1920f, 1080f);
            float aspect = Model.Bounds.width / Mathf.Max(0.01f, Model.Bounds.height);
            float w = size.x * 0.9f, h = w / aspect;
            if (h > size.y * 0.7f) { h = size.y * 0.7f; w = h * aspect; }
            picture.sizeDelta = new Vector2(w, h);
            panel.sizeDelta = new Vector2(w + 48f, h + 96f);
            picture.anchoredPosition = new Vector2(0f, -18f);
        }

        /// <summary>Paints the picture: ground pale with a bright top edge, the sheet dark, fog over the unexplored.</summary>
        public void Redraw()
        {
            if (Model == null) return;
            if (tex == null || tex.width != Model.Width || tex.height != Model.Height)
            {
                if (tex != null) Destroy(tex);
                tex = new Texture2D(Model.Width, Model.Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                image.texture = tex;
            }
            var px = new Color32[Model.Width * Model.Height];
            Color32 sheet = Air, ground = Ground, edge = GroundEdge, fog = Fog;
            for (int y = 0; y < Model.Height; y++)
                for (int x = 0; x < Model.Width; x++)
                {
                    Color32 c;
                    if (!Model.IsVisible(x, y)) c = fog;
                    else if (Model.IsSolid(x, y)) c = Model.IsSolid(x, y + 1) && Model.IsSolid(x, y + 2) ? ground : edge;
                    else c = sheet;
                    px[y * Model.Width + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            Model.Dirty = false;
        }

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (open && Model == null) Scan();
            panel.gameObject.SetActive(open);
            if (open) { Layout(); Redraw(); Place(); }
        }

        void Update()
        {
            // (the map's key is one of the controls: Tab or Select, rebindable in the settings)
            if (!InputBindings.GameplayBlocked && InputBindings.Action("Map").WasPressedThisFrame()) Toggle();

            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    player = pc.transform;
                    Model = null;           // a (new) level with its hero: read it again
                }
            }
            if (Model == null && player != null) Scan();
            if (Model == null || player == null) return;
            Model.RevealAll = revealAll;
            Model.RevealRadius = revealRadius;
            Model.Reveal(player.position);            // remembered even while the whole map shows
            if (IsOpen)
            {
                if (Model.Dirty) Redraw();
                Place();
            }
        }

        /// <summary>The map's title: with the playtest's time and deaths when it is measured.</summary>
        public string Title()
        {
            if (playtest == null) playtest = GetComponentInParent<PlaytestLog>();
            var m = playtest != null ? playtest.Model : null;
            if (m == null) return "MAPA  ·  TAB";
            string time = PlaytestModel.Clock(m.Finished ? m.FinishedAt : m.Elapsed);
            string where = m.Current >= 0 ? m.Records[m.Current].name : "";
            return "MAPA  ·  " + time + "  ·  smrti " + m.Deaths + (where.Length > 0 ? "  ·  " + where : "") + "  ·  TAB";
        }

        void Place()
        {
            if (titleText != null) titleText.text = Title();
            if (Model == null) return;
            var size = picture.sizeDelta;
            if (player != null)
            {
                var uv = Model.WorldToUv((Vector2)player.position + Vector2.up * 0.9f);
                hero.anchoredPosition = new Vector2(uv.x * size.x, uv.y * size.y);
                hero.gameObject.SetActive(true);
            }
            foreach (var (m, w) in marks)
            {
                if (m == null) continue;
                var c = Model.WorldToCell(w);
                bool shown = Model.IsVisible(c.x, c.y);
                m.gameObject.SetActive(shown);
                var uv = Model.WorldToUv(w + Vector2.up * 0.6f);
                m.anchoredPosition = new Vector2(uv.x * size.x, uv.y * size.y);
            }
        }

        void OnDestroy()
        {
            if (tex != null) Destroy(tex);
        }
    }
}
