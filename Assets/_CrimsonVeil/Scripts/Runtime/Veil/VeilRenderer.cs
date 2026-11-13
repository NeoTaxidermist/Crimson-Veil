using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>Builds a camera-facing ribbon mesh (XY plane) from the veil simulation every frame.</summary>
    [DefaultExecutionOrder(60)]
    [RequireComponent(typeof(VeilController))]
    public sealed class VeilRenderer : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField] Color rootColor = new Color(0.9f, 0.02f, 0.04f, 1f);
        [SerializeField] Color tipColor = new Color(0.45f, 0f, 0.02f, 0f);
        [SerializeField] Color hardColor = new Color(1.4f, 0.03f, 0.05f, 1f);
        [SerializeField, Range(0f, 1f)] float flutterTwist = 0.6f;

        VeilController veil;
        GameObject meshObject;
        Mesh mesh;
        Vector3[] vertices;
        Color[] colors;

        void Awake()
        {
            veil = GetComponent<VeilController>();
        }

        void Start()
        {
            int n = veil.Simulation.Count;
            vertices = new Vector3[n * 2];
            colors = new Color[n * 2];
            var uvs = new Vector2[n * 2];
            var tris = new int[(n - 1) * 6];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                uvs[i * 2] = new Vector2(t, 0f);
                uvs[i * 2 + 1] = new Vector2(t, 1f);
                if (i == n - 1) continue;
                int v = i * 2, k = i * 6;
                tris[k] = v; tris[k + 1] = v + 2; tris[k + 2] = v + 1;
                tris[k + 3] = v + 1; tris[k + 4] = v + 2; tris[k + 5] = v + 3;
            }

            mesh = new Mesh { name = "VeilRibbon" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;

            meshObject = new GameObject("VeilRibbon");
            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = meshObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        void OnDestroy()
        {
            if (meshObject != null) Destroy(meshObject);
            if (mesh != null) Destroy(mesh);
        }

        void LateUpdate()
        {
            var sim = veil.Simulation;
            if (sim == null || mesh == null) return;

            var p = sim.Positions;
            int n = sim.Count;
            float hard = sim.Hardness;
            float flowW = VeilFormLibrary.Width(VeilForm.Flow);
            float formW = VeilFormLibrary.Width(veil.ShapeForm);
            float hf = veil.HandFraction;
            float time = Time.time;

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                Vector3 tan = p[Mathf.Min(i + 1, n - 1)] - p[Mathf.Max(i - 1, 0)];
                tan.z = 0f;
                if (tan.sqrMagnitude < 1e-8f) tan = Vector3.down;
                tan.Normalize();
                var normal = new Vector3(-tan.y, tan.x, 0f);

                float flowProfile = Mathf.Lerp(1f, 0.12f, Mathf.Pow(t, 0.7f)) * (1f + 0.25f * Mathf.Sin(t * 9f - time * 6f));
                float twist = Mathf.Abs(Mathf.Cos(time * 3.1f + t * 7f));
                flowProfile *= Mathf.Lerp(1f, twist * 0.7f + 0.3f, flutterTwist * t);

                float weaponProfile = t < hf ? 0.45f : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.88f) / 0.12f) * 0.85f;
                float w = Mathf.Lerp(flowW * flowProfile, formW * weaponProfile, hard);

                vertices[i * 2] = p[i] + normal * (w * 0.5f);
                vertices[i * 2 + 1] = p[i] - normal * (w * 0.5f);

                Color soft = Color.Lerp(rootColor, tipColor, t * t);
                Color c = Color.Lerp(soft, hardColor, hard);
                colors[i * 2] = c;
                colors[i * 2 + 1] = c;
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }
    }
}
