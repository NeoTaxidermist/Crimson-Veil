using System.Collections.Generic;
using System.Linq;
using CrimsonVeil.Player;
using CrimsonVeil.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// Crimson Veil ▸ Level Editor. Pick a piece from the palette, click in the Scene view to place it
    /// (snapped to the grid, on the chosen depth layer). Shift+click keeps placing, Esc / right click stops.
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        static readonly Color Crimson = new Color(0.85f, 0.08f, 0.1f);

        LevelKitCatalog.Entry placing;
        LevelLayer layer = LevelLayer.Near;
        float grid = 0.5f;
        bool snap = true;
        bool showJumpReach = true;
        Vector2 scroll;
        Vector3 hoverPoint;
        bool hoverValid;
        string status = "";
        readonly Dictionary<LevelKitCatalog.Category, bool> foldouts = new Dictionary<LevelKitCatalog.Category, bool>();

        [MenuItem("Crimson Veil/Level Editor %#l")]
        public static void Open()
        {
            var w = GetWindow<LevelEditorWindow>("Level Editor");
            w.minSize = new Vector2(300f, 400f);
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            layer = (LevelLayer)EditorPrefs.GetInt("CV.LE.layer", (int)LevelLayer.Near);
            grid = EditorPrefs.GetFloat("CV.LE.grid", 0.5f);
            snap = EditorPrefs.GetBool("CV.LE.snap", true);
            showJumpReach = EditorPrefs.GetBool("CV.LE.reach", true);
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorPrefs.SetInt("CV.LE.layer", (int)layer);
            EditorPrefs.SetFloat("CV.LE.grid", grid);
            EditorPrefs.SetBool("CV.LE.snap", snap);
            EditorPrefs.SetBool("CV.LE.reach", showJumpReach);
        }

        // ================================================================== window UI

        void OnGUI()
        {
            var title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
            GUILayout.Space(4);
            GUILayout.Label("Crimson Veil – Level Editor", title);
            EditorGUILayout.HelpBox(placing == null
                ? "Vyber dílek a klikni do Scene view. Shift+klik = pokládat dál, Esc / pravé tlačítko = konec."
                : $"Pokládáš: {placing.label}. Klikni do Scene view (Shift = víc kusů, Esc = konec).", placing == null ? MessageType.Info : MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                snap = GUILayout.Toggle(snap, "Mřížka", "Button", GUILayout.Width(70));
                using (new EditorGUI.DisabledScope(!snap))
                    grid = EditorGUILayout.FloatField(grid, GUILayout.Width(50));
                showJumpReach = GUILayout.Toggle(showJumpReach, "Dosah skoku", "Button");
            }
            layer = (LevelLayer)EditorGUILayout.EnumPopup(new GUIContent("Vrstva dekorací", "Hloubka, do které se pokládají dekorace. Dál = světlejší, větší, víc v mlze."), layer);

            GUILayout.Space(6);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (LevelKitCatalog.Category cat in System.Enum.GetValues(typeof(LevelKitCatalog.Category)))
            {
                if (!foldouts.ContainsKey(cat)) foldouts[cat] = true;
                foldouts[cat] = EditorGUILayout.BeginFoldoutHeaderGroup(foldouts[cat], CategoryName(cat));
                if (foldouts[cat]) DrawPalette(cat);
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            GUILayout.Space(8);
            GUILayout.Label("Nástroje", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("▶ Hrát odsud", "Spustí hru s hrdinou v místě, kam se dívá Scene view."), GUILayout.Height(30))) PlayFromHere();
                if (GUILayout.Button(new GUIContent("✔ Zkontrolovat", "Najde typické chyby v úrovni."), GUILayout.Height(30))) Validate();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Nová úroveň", "Vytvoří prázdnou scénu se základem, hrdinou a první plošinou."))) NewLevel();
                if (GUILayout.Button(new GUIContent("Přestavět dílky", "Znovu vygeneruje prefaby z kódu (po změně vzhledu)."))) LevelKitCatalog.RebuildPrefabs();
            }
            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<DepthLayer>() != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Vybrané do vrstvy:", GUILayout.Width(115));
                    if (GUILayout.Button(layer.ToString()))
                        foreach (var go in Selection.gameObjects)
                        {
                            Undo.RecordObject(go.transform, "Change layer");
                            LevelKitFactory.ApplyLayer(go, layer);
                        }
                }
            }
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        void DrawPalette(LevelKitCatalog.Category cat)
        {
            var entries = LevelKitCatalog.Entries.Where(e => e.category == cat).ToList();
            int cols = Mathf.Max(1, Mathf.FloorToInt((position.width - 20f) / 140f));
            for (int i = 0; i < entries.Count; i += cols)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int k = i; k < Mathf.Min(i + cols, entries.Count); k++)
                    {
                        var e = entries[k];
                        bool active = placing == e;
                        var prev = GUI.backgroundColor;
                        if (active) GUI.backgroundColor = Crimson;
                        var label = new GUIContent(e.label + (e.layered ? "  ◐" : ""), e.hint + (e.layered ? "\n◐ = pokládá se do zvolené vrstvy." : ""));
                        if (GUILayout.Button(label, GUILayout.Height(34), GUILayout.MinWidth(120)))
                        {
                            placing = active ? null : e;
                            if (placing != null && placing.category == LevelKitCatalog.Category.Scena)
                            {
                                PlaceAt(Vector3.zero);
                                placing = null;
                            }
                            SceneView.RepaintAll();
                        }
                        GUI.backgroundColor = prev;
                    }
                }
            }
        }

        static string CategoryName(LevelKitCatalog.Category c)
        {
            switch (c)
            {
                case LevelKitCatalog.Category.Teren: return "Terén";
                case LevelKitCatalog.Category.Hra: return "Hratelnost";
                case LevelKitCatalog.Category.Dekorace: return "Dekorace";
                default: return "Scéna";
            }
        }

        // ================================================================== scene view

        void OnSceneGUI(SceneView view)
        {
            if (showJumpReach) DrawJumpReach();
            if (placing == null) return;

            var e = Event.current;
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            float z = placing.layered ? DepthLayer.Z(layer) : 0f;
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            var plane = new Plane(Vector3.back, new Vector3(0f, 0f, z));
            hoverValid = plane.Raycast(ray, out float dist);
            if (hoverValid)
            {
                hoverPoint = ray.GetPoint(dist);
                if (snap && grid > 0.01f)
                {
                    hoverPoint.x = Mathf.Round(hoverPoint.x / grid) * grid;
                    hoverPoint.y = Mathf.Round(hoverPoint.y / grid) * grid;
                }
                DrawPreview(placing, hoverPoint);
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) view.Repaint();

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape || e.type == EventType.MouseDown && e.button == 1)
            {
                placing = null;
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && hoverValid)
            {
                PlaceAt(hoverPoint);
                if (!e.shift) placing = null;
                e.Use();
                Repaint();
            }
        }

        void DrawPreview(LevelKitCatalog.Entry entry, Vector3 p)
        {
            Handles.color = Crimson;
            var s = entry.previewSize * (entry.layered ? LevelKitFactory.LayerScale(layer) : 1f);
            bool hangsDown = entry.category == LevelKitCatalog.Category.Teren;
            Vector3 c = hangsDown ? p + Vector3.down * s.y * 0.5f : p + Vector3.up * s.y * 0.5f;
            Handles.DrawWireCube(c, new Vector3(s.x, s.y, 0.1f));
            Handles.DrawSolidDisc(p, Vector3.back, 0.12f);
            Handles.Label(p + Vector3.up * 0.4f, $"{entry.label}  ({p.x:0.#}, {p.y:0.#})", EditorStyles.whiteBoldLabel);
        }

        void PlaceAt(Vector3 p)
        {
            var parent = GroupFor(placing);
            var go = LevelKitCatalog.Place(placing, p, layer, parent);
            Undo.RegisterCreatedObjectUndo(go, "Place " + placing.label);
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            status = $"Položeno: {placing.label} na ({p.x:0.##}, {p.y:0.##})";
        }

        static Transform GroupFor(LevelKitCatalog.Entry e)
        {
            if (e.category == LevelKitCatalog.Category.Scena) return null;
            string root = "Level";
            var rootGo = GameObject.Find(root) ?? new GameObject(root);
            string sub = CategoryName(e.category);
            var t = rootGo.transform.Find(sub);
            if (t == null)
            {
                t = new GameObject(sub).transform;
                t.SetParent(rootGo.transform, false);
                Undo.RegisterCreatedObjectUndo(t.gameObject, "Create group");
            }
            return t;
        }

        // ================================================================== jump reach gizmo

        static void DrawJumpReach()
        {
            var block = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<TerrainBlock>() : null;
            if (block == null) return;
            var s = new MotorSettings();
            foreach (int dir in new[] { -1, 1 })
            {
                var edge = new Vector3(dir > 0 ? block.RightX : block.LeftX, block.TopY, 0f);
                DrawArc(edge, dir, s.maxRunSpeed, s, new Color(1f, 0.2f, 0.2f, 0.9f));
                DrawArc(edge, dir, s.dashSpeed * s.dashEndSpeedFactor + 2f, s, new Color(1f, 0.6f, 0.6f, 0.5f));
            }
        }

        /// <summary>Approximate apex-held jump arc (with fall gravity) — what the hero can reach from this edge.</summary>
        static void DrawArc(Vector3 from, int dir, float vx, MotorSettings s, Color color)
        {
            Handles.color = color;
            var pts = new List<Vector3>();
            float vy = s.JumpVelocity, x = 0f, y = 0f;
            const float dt = 1f / 60f;
            for (int i = 0; i < 240 && y > -8f; i++)
            {
                pts.Add(from + new Vector3(dir * x, y, 0f));
                float mult = vy < 0f ? s.fallGravityMultiplier : (Mathf.Abs(vy) < s.apexThreshold ? s.apexGravityMultiplier : 1f);
                vy = Mathf.Max(vy - s.Gravity * mult * dt, -s.maxFallSpeed);
                x += vx * dt; y += vy * dt;
            }
            Handles.DrawAAPolyLine(3f, pts.ToArray());
        }

        // ================================================================== tools

        static void PlayFromHere()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null) return;
            Vector3 p = view.pivot;
            PlayFromHereOverride.Set(new Vector3(p.x, p.y, 0f));
            EditorApplication.isPlaying = true;
        }

        void Validate()
        {
            var problems = new List<string>();
            int players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            if (players == 0) problems.Add("Chybí hrdina (Hratelnost ▸ Hrdina).");
            if (players > 1) problems.Add($"Hrdinů je {players}, má být jeden.");
            if (FindObjectsByType<PlayerSpawn>(FindObjectsSortMode.None).Length == 0) problems.Add("Chybí Start hráče – hrdina začne tam, kde stojí.");
            if (Camera.main == null) problems.Add("Chybí kamera (Scéna ▸ Základ úrovně).");
            if (FindObjectsByType<CrimsonVeil.Combat.CombatFx>(FindObjectsSortMode.None).Length == 0) problems.Add("Chybí efekty boje (Základ úrovně).");
            foreach (var t in FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None))
                if (Mathf.Abs(t.transform.position.z) > 0.01f) problems.Add($"{t.name}: terén není v rovině hry (z ≠ 0).");
            foreach (var e in FindObjectsByType<CrimsonVeil.Combat.Enemy>(FindObjectsSortMode.None))
                if (!Physics.Raycast(e.transform.position + Vector3.up, Vector3.down, 3f)) problems.Add($"{e.name} na x={e.transform.position.x:0.#} nestojí na zemi.");
            foreach (var a in FindObjectsByType<GroundAnchor>(FindObjectsSortMode.None))
                if (!a.Snap()) problems.Add($"{a.name} na x={a.transform.position.x:0.#} visí nad jámou (nemá pod sebou terén).");
            status = problems.Count == 0 ? "Úroveň vypadá v pořádku. ✔" : "Nalezeno:\n• " + string.Join("\n• ", problems);
        }

        static void NewLevel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LevelKitFactory.ApplyRenderSettings();
            PrefabUtility.InstantiatePrefab(LevelKitCatalog.Prefab(LevelKitCatalog.Find("LevelEssentials")));
            LevelKitCatalog.Place(LevelKitCatalog.Find("Cliff"), new Vector3(0f, 0f, 0f), LevelLayer.Playfield, GroupFor(LevelKitCatalog.Find("Cliff")));
            LevelKitCatalog.Place(LevelKitCatalog.Find("PlayerSpawn"), new Vector3(-3f, 0.05f, 0f), LevelLayer.Playfield, GroupFor(LevelKitCatalog.Find("PlayerSpawn")));
            LevelKitCatalog.Place(LevelKitCatalog.Find("Player"), new Vector3(-3f, 0.05f, 0f), LevelLayer.Playfield, null);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
