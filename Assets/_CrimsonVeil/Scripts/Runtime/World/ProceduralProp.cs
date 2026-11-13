using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>Base for props whose mesh is generated from a few parameters (rebuilt in edit mode too).</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public abstract class ProceduralProp : MonoBehaviour
    {
        [SerializeField] protected int seed = 1;
        [SerializeField] bool addCollider;

        Mesh mesh;

        protected abstract Mesh Build();

        void OnEnable() => Rebuild();

        void OnValidate()
        {
            if (isActiveAndEnabled) Rebuild();
        }

        public void Rebuild()
        {
            if (mesh != null) DestroyMesh(mesh);
            mesh = Build();
            mesh.hideFlags = HideFlags.DontSave;
            GetComponent<MeshFilter>().sharedMesh = mesh;
            if (addCollider)
            {
                var col = GetComponent<MeshCollider>();
                if (col == null) col = gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = mesh;
            }
        }

        void OnDisable()
        {
            if (mesh != null) DestroyMesh(mesh);
            mesh = null;
        }

        static void DestroyMesh(Mesh m)
        {
            if (Application.isPlaying) Destroy(m);
            else DestroyImmediate(m);
        }

        public void Configure(int newSeed, bool collider = false)
        {
            seed = newSeed;
            addCollider = collider;
        }
    }
}
