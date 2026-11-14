using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// The main level-building block: a platform, ledge, wall or cliff.
    /// The transform position is the <b>centre of the walkable top edge</b>; the block hangs down by
    /// <see cref="height"/>. Collision is an exact box (clean gameplay), while the visual silhouette,
    /// grass on top and hanging roots are generated and always stay inside the block's edges.
    /// Resize it with the scene-view handles; everything regenerates.
    /// </summary>
    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class TerrainBlock : MonoBehaviour
    {
        [Header("Shape")]
        [Min(0.5f)] public float width = 6f;
        [Min(0.5f)] public float height = 2f;
        [Min(0.5f)] public float depth = 4f;
        public TerrainStyle style = TerrainStyle.Rock;
        [Range(0f, 1f)] public float roughness = 0.7f;
        public int seed = 1;

        [Header("Dressing")]
        public bool grassOnTop = true;
        [Range(0f, 30f)] public float grassDensity = 12f;
        [Range(0.1f, 2f)] public float grassHeight = 0.6f;
        public bool hangingRoots = true;
        public bool solid = true;

        [Header("Materials")]
        public Material bodyMaterial;
        public Material grassMaterial;

        const string GeneratedName = "__Generated";

        public Bounds TopBounds => new Bounds(transform.position + Vector3.down * height * 0.5f, new Vector3(width, height, depth));
        public float TopY => transform.position.y;
        public float LeftX => transform.position.x - width * 0.5f;
        public float RightX => transform.position.x + width * 0.5f;

        void OnEnable() => Rebuild();

        void OnValidate()
        {
#if UNITY_EDITOR
            // Defer: creating objects inside OnValidate is not allowed.
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
#endif
        }

        public void Rebuild()
        {
            if (this == null) return;
            // Prefab assets on disk are never dressed — only instances in a scene or the prefab stage.
            if (!gameObject.scene.IsValid()) return;
            var box = GetComponent<BoxCollider>();
            if (solid)
            {
                if (box == null) box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, -height * 0.5f, 0f);
                box.size = new Vector3(width, height, depth);
                box.isTrigger = false;
            }
            else if (box != null) box.enabled = false;

            var gen = transform.Find(GeneratedName);
            if (gen == null)
            {
                gen = new GameObject(GeneratedName).transform;
                gen.SetParent(transform, false);
            }
            gen.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            for (int i = gen.childCount - 1; i >= 0; i--) DestroySafe(gen.GetChild(i).gameObject);

            Part(gen, "Body", ProceduralMeshes.Terrain(width, height, depth, style, roughness, seed), bodyMaterial, Vector3.zero);

            if (grassOnTop && grassDensity > 0f && width > 0.8f)
            {
                // Grass grows right up to the front lip and over the whole top, so no bare top face shows
                // between the rows; it thins out towards the (chipped) corners instead of stopping short.
                float w = width - 0.2f; // never overhang the edges
                int blades = Mathf.Max(4, Mathf.RoundToInt(w * grassDensity));
                var h = new Vector2(grassHeight * 0.3f, grassHeight);
                float taper = Mathf.Min(0.9f, w * 0.25f);
                Part(gen, "GrassFront", ProceduralMeshes.Grass(seed * 31 + 1, Mathf.RoundToInt(blades * 1.3f), w, h, 0.05f, taper, 0.12f), grassMaterial, new Vector3(0f, 0f, -depth * 0.5f + 0.12f));
                Part(gen, "GrassMid", ProceduralMeshes.Grass(seed * 31 + 3, blades, w, h * 1.2f, 0.05f, taper, 0.05f), grassMaterial, new Vector3(0f, 0f, -depth * 0.15f));
                Part(gen, "GrassBack", ProceduralMeshes.Grass(seed * 31 + 2, Mathf.RoundToInt(blades * 0.8f), w, h * 1.4f, 0.05f, taper, 0.05f), grassMaterial, new Vector3(0f, 0f, depth * 0.5f - 0.4f));
            }

            if (hangingRoots && height > 0.6f)
            {
                int strands = Mathf.Max(3, Mathf.RoundToInt(width * 2.5f));
                Part(gen, "Roots", ProceduralMeshes.HangingRoots(seed * 17 + 3, strands, width * 0.9f, new Vector2(0.15f, Mathf.Min(1.2f, height * 0.5f))),
                    grassMaterial, new Vector3(0f, -0.12f, -depth * 0.5f - 0.02f));
            }
        }

        static void Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            mesh.hideFlags = HideFlags.DontSave;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void DestroySafe(GameObject go)
        {
            var mf = go.GetComponent<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (Application.isPlaying) { Destroy(go); if (mesh != null) Destroy(mesh); }
            else { DestroyImmediate(go); if (mesh != null) DestroyImmediate(mesh); }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(TopBounds.center, TopBounds.size);
        }
    }
}
