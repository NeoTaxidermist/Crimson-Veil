using System.Collections.Generic;
using CrimsonVeil.World;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// Re-roots ground plants the moment they are moved in the editor (Scene view drag, Inspector, layer
    /// change, scripts) — independent of whether the Scene view is repainting, which is when
    /// <see cref="GroundAnchor"/>'s own edit-mode Update would run.
    /// </summary>
    [InitializeOnLoad]
    static class GroundAnchorWatcher
    {
        static readonly List<GroundAnchor> anchors = new List<GroundAnchor>();
        static bool dirty = true;

        static GroundAnchorWatcher()
        {
            EditorApplication.hierarchyChanged += () => dirty = true;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (dirty)
            {
                anchors.Clear();
                anchors.AddRange(Object.FindObjectsByType<GroundAnchor>(FindObjectsSortMode.None));
                dirty = false;
            }
            foreach (var a in anchors)
            {
                if (a == null) { dirty = true; continue; }
                if (!a.transform.hasChanged) continue;
                a.Snap();
                a.transform.hasChanged = false;
            }
        }
    }
}
