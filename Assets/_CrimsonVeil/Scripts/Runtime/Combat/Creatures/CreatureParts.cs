using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Builds creature silhouettes from primitives (ink bodies, crimson details) — usable from the editor
    /// (Level Kit) and at runtime. Parts never carry colliders; creatures add their own hitboxes.
    /// </summary>
    public static class CreatureParts
    {
        public static Transform Group(Transform parent, string name, Vector3 localPos, Vector3 euler = default)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localEulerAngles = euler;
            return t;
        }

        public static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Material material, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localScale = scale;
            t.localEulerAngles = euler;
            var r = go.GetComponent<MeshRenderer>();
            if (material != null) r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return t;
        }

        /// <summary>A crimson thread / rope drawn as a line.</summary>
        public static LineRenderer Line(Transform parent, string name, Material material, float width, int points)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = points;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) line.sharedMaterial = material;
            line.startColor = line.endColor = new Color(0.85f, 0.03f, 0.05f, 1f);
            return line;
        }

        /// <summary>Finds a descendant by name (built visuals are looked up at runtime this way).</summary>
        public static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = Find(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
