using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Draws the veil as several layered, camera-facing cloth strands following the simulation.
    /// The centre line is smoothed with Catmull-Rom; secondary strands fan out towards the tail like
    /// a layered cloak and fold back into one blade when the veil hardens into a weapon.
    /// Colour, shading and smoky edges come from the CrimsonVeil/Veil shader.
    /// </summary>
    [DefaultExecutionOrder(60)]
    [RequireComponent(typeof(VeilController))]
    public sealed class VeilRenderer : MonoBehaviour
    {
        [SerializeField] Material material;
        [Tooltip("Catmull-Rom samples per simulation segment.")]
        [SerializeField, Range(1, 6)] int smoothing = 3;
        [SerializeField, Range(0f, 1f)] float flutterTwist = 0.5f;

        struct Strand
        {
            public float width, offset, phase, alpha, seed, spread;
            public Color tint;
        }

        static readonly Strand[] Strands =
        {
            // Back layer: wide, darker, fans out the most.
            new Strand { width = 0.85f, offset = 0.35f, phase = 1.7f, alpha = 0.55f, seed = 0.31f, spread = 0.9f, tint = new Color(0.7f, 0.7f, 0.7f) },
            // Main cloth.
            new Strand { width = 1f, offset = 0f, phase = 0f, alpha = 1f, seed = 0.0f, spread = 0.25f, tint = Color.white },
            // Thin bright ribbon on top.
            new Strand { width = 0.35f, offset = -0.3f, phase = 3.9f, alpha = 0.8f, seed = 0.67f, spread = 1.1f, tint = new Color(1.25f, 1.1f, 1.1f) },
        };

        VeilController veil;
        GameObject meshObject;
        Mesh mesh;
        Vector3[] centre;
        float[] hardAt;
        Vector3[] vertices;
        Color[] colors;
        Vector2[] uv2;
        int samples;

        public int SampleCount => samples;

        void Awake() => veil = GetComponent<VeilController>();

        void Start()
        {
            int n = veil.Simulation.Count;
            samples = (n - 1) * smoothing + 1;
            centre = new Vector3[samples];
            hardAt = new float[samples];
            int per = samples * 2;
            vertices = new Vector3[per * Strands.Length];
            colors = new Color[vertices.Length];
            uv2 = new Vector2[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var tris = new int[(samples - 1) * 6 * Strands.Length];
            int k = 0;
            for (int s = 0; s < Strands.Length; s++)
            {
                int baseV = s * per;
                for (int i = 0; i < samples; i++)
                {
                    float t = (float)i / (samples - 1);
                    uvs[baseV + i * 2] = new Vector2(t, 0f);
                    uvs[baseV + i * 2 + 1] = new Vector2(t, 1f);
                    if (i == samples - 1) continue;
                    int v = baseV + i * 2;
                    tris[k++] = v; tris[k++] = v + 2; tris[k++] = v + 1;
                    tris[k++] = v + 1; tris[k++] = v + 2; tris[k++] = v + 3;
                }
            }

            mesh = new Mesh { name = "VeilCloth" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;

            meshObject = new GameObject("VeilCloth");
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

        /// <summary>Catmull-Rom point between p1 and p2.</summary>
        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        void LateUpdate()
        {
            var sim = veil.Simulation;
            if (sim == null || mesh == null) return;

            var p = sim.Positions;
            int n = sim.Count;
            float formHardness = Mathf.Max(0.01f, VeilFormLibrary.Hardness(veil.ShapeForm));

            // Smooth centre line + per-sample hardness.
            for (int seg = 0; seg < n - 1; seg++)
            {
                Vector3 p0 = p[Mathf.Max(seg - 1, 0)], p1 = p[seg], p2 = p[seg + 1], p3 = p[Mathf.Min(seg + 2, n - 1)];
                float h1 = sim.UseNodeHardness ? sim.NodeHardness[seg] : sim.Hardness;
                float h2 = sim.UseNodeHardness ? sim.NodeHardness[seg + 1] : sim.Hardness;
                for (int k = 0; k < smoothing; k++)
                {
                    float u = (float)k / smoothing;
                    int idx = seg * smoothing + k;
                    centre[idx] = CatmullRom(p0, p1, p2, p3, u);
                    hardAt[idx] = Mathf.Clamp01(Mathf.Lerp(h1, h2, u) / formHardness);
                }
            }
            centre[samples - 1] = p[n - 1];
            hardAt[samples - 1] = Mathf.Clamp01((sim.UseNodeHardness ? sim.NodeHardness[n - 1] : sim.Hardness) / formHardness);

            float flowW = VeilFormLibrary.Width(VeilForm.Flow);
            float formW = VeilFormLibrary.Width(veil.ShapeForm);
            float hf = veil.HandFraction;
            float time = Time.time;
            float vit = Mathf.Clamp01(veil.Vitality);
            int per = samples * 2;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / (samples - 1);
                Vector3 tan = centre[Mathf.Min(i + 1, samples - 1)] - centre[Mathf.Max(i - 1, 0)];
                tan.z = 0f;
                if (tan.sqrMagnitude < 1e-8f) tan = Vector3.down;
                tan.Normalize();
                var normal = new Vector3(-tan.y, tan.x, 0f);
                float hard = Mathf.SmoothStep(0f, 1f, hardAt[i]);

                // Cloth: widest in the middle, narrowing to a wisp; ripples and twists as it flies.
                float flowProfile = (0.55f + 0.8f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f))) * (1f - 0.8f * t * t * t)
                                    * (1f + 0.22f * Mathf.Sin(t * 9f - time * 6f));
                float twist = Mathf.Abs(Mathf.Cos(time * 2.7f + t * 6f));
                flowProfile *= Mathf.Lerp(1f, twist * 0.65f + 0.35f, flutterTwist * t);

                float weaponProfile = t < hf ? 0.4f : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.86f) / 0.14f) * 0.85f;
                weaponProfile *= 1f + 0.08f * Mathf.Sin(t * 14f - time * 5f);
                float w = Mathf.Lerp(flowW * flowProfile, formW * weaponProfile, hard) * Mathf.Lerp(0.45f, 1f, vit);

                float fray = 1f - Mathf.SmoothStep(0f, 1f, (t - Mathf.Lerp(0.3f, 1.05f, vit)) / 0.25f);
                float baseAlpha = Mathf.Lerp(fray, 1f, hard) * Mathf.Lerp(0.6f, 1f, vit) * Mathf.SmoothStep(0f, 1f, t * 12f + 0.3f);

                for (int s = 0; s < Strands.Length; s++)
                {
                    var st = Strands[s];
                    float loose = 1f - hard;
                    // Strands fan out towards the tail and fold back into one when hardened.
                    float wave = Mathf.Sin(t * Mathf.PI * 2.4f - time * 2.1f + st.phase);
                    float off = (st.offset + wave * 0.35f * st.spread * t) * w * loose * Mathf.SmoothStep(0f, 1f, t * 3f);
                    float sw = w * Mathf.Lerp(st.width, s == 1 ? 1f : 0.2f, hard);
                    Vector3 c = centre[i] + normal * off + new Vector3(0f, 0f, (s - 1) * 0.02f);

                    int v = s * per + i * 2;
                    vertices[v] = c + normal * (sw * 0.5f);
                    vertices[v + 1] = c - normal * (sw * 0.5f);
                    var col = st.tint;
                    col.a = baseAlpha * Mathf.Lerp(st.alpha, s == 1 ? 1f : 0.15f, hard);
                    col.r *= Mathf.Lerp(0.6f, 1f, vit);
                    colors[v] = colors[v + 1] = col;
                    uv2[v] = uv2[v + 1] = new Vector2(hard, st.seed);
                }
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetUVs(1, uv2);
            mesh.RecalculateBounds();
        }
    }
}
