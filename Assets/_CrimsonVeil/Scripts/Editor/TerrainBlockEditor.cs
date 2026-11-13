using CrimsonVeil.World;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.EditorTools
{
    /// <summary>Scene-view handles to resize terrain blocks (left/right edge, bottom), snapped to the grid.</summary>
    [CustomEditor(typeof(TerrainBlock))]
    [CanEditMultipleObjects]
    public sealed class TerrainBlockEditor : Editor
    {
        const float Grid = 0.5f;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Pozice = střed pochozí horní hrany. Táhni červené úchyty ve Scene view (šířka, výška).", MessageType.None);
            DrawDefaultInspector();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Nový tvar (seed)"))
                    foreach (TerrainBlock t in targets)
                    {
                        Undo.RecordObject(t, "Reseed");
                        t.seed = Random.Range(1, 9999);
                        t.Rebuild();
                    }
                if (GUILayout.Button("Přestavět"))
                    foreach (TerrainBlock t in targets) t.Rebuild();
            }
        }

        void OnSceneGUI()
        {
            var t = (TerrainBlock)target;
            var tr = t.transform;
            Handles.color = new Color(0.9f, 0.1f, 0.15f);
            float size = HandleUtility.GetHandleSize(tr.position) * 0.12f;

            // Right edge
            EditorGUI.BeginChangeCheck();
            var right = tr.position + new Vector3(t.width * 0.5f, -Mathf.Min(0.5f, t.height * 0.5f), 0f);
            var newRight = Handles.Slider(right, Vector3.right, size * 1.5f, Handles.ConeHandleCap, Grid);
            if (EditorGUI.EndChangeCheck()) Resize(t, Snap(newRight.x) - Snap(tr.position.x - t.width * 0.5f), +1);

            // Left edge
            EditorGUI.BeginChangeCheck();
            var left = tr.position + new Vector3(-t.width * 0.5f, -Mathf.Min(0.5f, t.height * 0.5f), 0f);
            var newLeft = Handles.Slider(left, Vector3.left, size * 1.5f, Handles.ConeHandleCap, Grid);
            if (EditorGUI.EndChangeCheck()) Resize(t, Snap(tr.position.x + t.width * 0.5f) - Snap(newLeft.x), -1);

            // Bottom
            EditorGUI.BeginChangeCheck();
            var bottom = tr.position + Vector3.down * t.height;
            var newBottom = Handles.Slider(bottom, Vector3.down, size * 1.5f, Handles.ConeHandleCap, Grid);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Resize terrain");
                t.height = Mathf.Max(0.5f, Snap(tr.position.y - newBottom.y));
                t.Rebuild();
            }

            Handles.Label(tr.position + Vector3.up * 0.4f, $"{t.width:0.#} × {t.height:0.#} m", EditorStyles.whiteBoldLabel);
        }

        static void Resize(TerrainBlock t, float newWidth, int side)
        {
            newWidth = Mathf.Max(0.5f, newWidth);
            Undo.RecordObject(t, "Resize terrain");
            Undo.RecordObject(t.transform, "Resize terrain");
            // Keep the opposite edge fixed.
            float fixedEdge = side > 0 ? t.transform.position.x - t.width * 0.5f : t.transform.position.x + t.width * 0.5f;
            t.width = newWidth;
            var p = t.transform.position;
            p.x = side > 0 ? fixedEdge + newWidth * 0.5f : fixedEdge - newWidth * 0.5f;
            t.transform.position = p;
            t.Rebuild();
        }

        static float Snap(float v) => Mathf.Round(v / Grid) * Grid;
    }
}
