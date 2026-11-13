using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Draws the veil as one continuous scarf that splits into several tendrils ("many ends"), like the key art.
    /// Every strand follows the simulated centre line; tendrils branch off the main ribbon (they start *on* it,
    /// so the swirl stays connected) and every end winds into a smooth spiral curl (an Euler spiral whose
    /// curvature grows towards the tip). When the veil hardens into a weapon the tendrils fold back into the
    /// blade and the curls unroll. Colour, shading and smoky edges come from the CrimsonVeil/Veil shader.
    /// </summary>
    [DefaultExecutionOrder(60)]
    [RequireComponent(typeof(VeilController))]
    public sealed class VeilRenderer : MonoBehaviour
    {
        [SerializeField] Material material;
        [Tooltip("Catmull-Rom samples per simulation segment.")]
        [SerializeField, Range(1, 6)] int smoothing = 3;
        [SerializeField, Range(0f, 1f)] float flutterTwist = 0.35f;
        [Tooltip("Points in each end curl.")]
        [SerializeField, Range(6, 40)] int curlPoints = 22;
        [Tooltip("Global multiplier of the curl size.")]
        [SerializeField] float curlScale = 1.7f;
        [Tooltip("How far (m) the tendrils fan away from the main ribbon at their ends.")]
        [SerializeField] float tendrilSpread = 0.5f;

        public struct Strand
        {
            /// <summary>Where on the scarf the strand starts and ends (0 = neck, 1 = tip).</summary>
            public float start, end;
            public float width, side, spread, phase, alpha, seed;
            /// <summary>End curl: length (m), number of turns, winding direction.</summary>
            public float curlLength, curlTurns, curlSign;
            public Color tint;
        }

        public static readonly Strand[] Strands =
        {
            // Back ribbon: darker, drifts behind the main one.
            new Strand { start = 0f, end = 0.96f, width = 0.7f, side = 0.45f, spread = 0.9f, phase = 1.7f, alpha = 0.6f, seed = 0.31f,
                curlLength = 0.5f, curlTurns = 1.1f, curlSign = -1f, tint = new Color(0.68f, 0.68f, 0.68f) },
            // Main ribbon: ends in one big loop, like the key art.
            new Strand { start = 0f, end = 1f, width = 1f, side = 0f, spread = 0.25f, phase = 0f, alpha = 1f, seed = 0f,
                curlLength = 0.95f, curlTurns = 1.05f, curlSign = 1f, tint = Color.white },
            // Broad tendrils splitting off the main ribbon, each ending in its own curl.
            new Strand { start = 0.32f, end = 0.8f, width = 0.7f, side = -0.95f, spread = 1.1f, phase = 3.9f, alpha = 0.92f, seed = 0.67f,
                curlLength = 0.6f, curlTurns = 1.4f, curlSign = -1f, tint = new Color(1.15f, 1.05f, 1.05f) },
            new Strand { start = 0.45f, end = 0.92f, width = 0.6f, side = 1f, spread = 1.2f, phase = 0.8f, alpha = 0.88f, seed = 0.12f,
                curlLength = 0.65f, curlTurns = 1.6f, curlSign = 1f, tint = Color.white },
            new Strand { start = 0.22f, end = 0.6f, width = 0.5f, side = 1.2f, spread = 1f, phase = 2.4f, alpha = 0.75f, seed = 0.48f,
                curlLength = 0.5f, curlTurns = 1.4f, curlSign = -1f, tint = new Color(0.78f, 0.76f, 0.76f) },
            new Strand { start = 0.55f, end = 0.82f, width = 0.45f, side = -0.6f, spread = 1.3f, phase = 5.1f, alpha = 0.8f, seed = 0.83f,
                curlLength = 0.45f, curlTurns = 1.9f, curlSign = 1f, tint = new Color(0.75f, 0.72f, 0.72f) },
            // Fine silk threads riding along the scarf and fanning out at its tail.
            new Strand { start = 0.15f, end = 1f, width = 0.09f, side = 0.3f, spread = 1.6f, phase = 4.4f, alpha = 0.9f, seed = 0.21f,
                curlLength = 0.55f, curlTurns = 2.2f, curlSign = -1f, tint = new Color(1.3f, 1.15f, 1.15f) },
            new Strand { start = 0.38f, end = 0.95f, width = 0.07f, side = -0.35f, spread = 1.8f, phase = 1.1f, alpha = 0.85f, seed = 0.92f,
                curlLength = 0.5f, curlTurns = 2.4f, curlSign = 1f, tint = new Color(1.3f, 1.15f, 1.15f) },
            new Strand { start = 0.6f, end = 1f, width = 0.08f, side = 1.6f, spread = 1.4f, phase = 3.2f, alpha = 0.8f, seed = 0.57f,
                curlLength = 0.45f, curlTurns = 1.8f, curlSign = 1f, tint = new Color(1.2f, 1.1f, 1.1f) },
            new Strand { start = 0.5f, end = 0.97f, width = 0.06f, side = -1.5f, spread = 1.5f, phase = 0.3f, alpha = 0.75f, seed = 0.39f,
                curlLength = 0.5f, curlTurns = 2f, curlSign = -1f, tint = new Color(1.2f, 1.1f, 1.1f) },
        };

        VeilController veil;
        GameObject meshObject;
        Mesh mesh;
        Vector3[] centre, normalAt;
        float[] hardAt, widthAt, alphaAt;
        Vector3[] vertices;
        Color[] colors;
        Vector2[] uv2;
        Vector3[] curl;
        int samples, perStrand;
        Vector3 lastAnchor;
        float speedSmoothed;

        public int SampleCount => samples;
        public int PointsPerStrand => perStrand;

        void Awake() => veil = GetComponent<VeilController>();

        void Start()
        {
            int n = veil.Simulation.Count;
            samples = (n - 1) * smoothing + 1;
            perStrand = samples + curlPoints;
            centre = new Vector3[samples];
            normalAt = new Vector3[samples];
            hardAt = new float[samples];
            widthAt = new float[samples];
            alphaAt = new float[samples];
            curl = new Vector3[curlPoints + 1];

            int per = perStrand * 2;
            vertices = new Vector3[per * Strands.Length];
            colors = new Color[vertices.Length];
            uv2 = new Vector2[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var tris = new int[(perStrand - 1) * 6 * Strands.Length];
            int k = 0;
            for (int s = 0; s < Strands.Length; s++)
            {
                int baseV = s * per;
                for (int i = 0; i < perStrand; i++)
                {
                    // Keep the curl mostly out of the shader's tail erosion: it ends at 0.9.
                    float t = 0.9f * i / (perStrand - 1);
                    uvs[baseV + i * 2] = new Vector2(t, 0f);
                    uvs[baseV + i * 2 + 1] = new Vector2(t, 1f);
                    if (i == perStrand - 1) continue;
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

        /// <summary>
        /// Smooth end curl (Euler spiral): starts at <paramref name="start"/> heading along <paramref name="heading"/>
        /// (radians, XY plane) and turns by <paramref name="turns"/> full revolutions, the curvature growing
        /// with the distance, so the stroke winds tighter towards its tip. Writes count + 1 points.
        /// </summary>
        public static void BuildCurl(Vector3 start, float heading, float sign, float length, float turns, int count, Vector3[] into)
        {
            into[0] = start;
            float total = turns * Mathf.PI * 2f * sign;
            float ds = length / count;
            Vector3 p = start;
            for (int i = 1; i <= count; i++)
            {
                float u = (i - 0.5f) / count;
                float a = heading + total * u * u;
                p += new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * ds;
                into[i] = p;
            }
        }

        /// <summary>Sideways offset of a tendril from the main ribbon: zero where it branches, so it is attached.</summary>
        public static float BranchOffset(float u, float side, float width) => side * width * Mathf.SmoothStep(0f, 1f, u);

        void LateUpdate()
        {
            var sim = veil.Simulation;
            if (sim == null || mesh == null) return;

            var p = sim.Positions;
            int n = sim.Count;
            float formHardness = Mathf.Max(0.01f, VeilFormLibrary.Hardness(veil.ShapeForm));
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);

            // Speed of the hero: curls stretch open a little at a run and wind up again at rest.
            float speed = (p[0] - lastAnchor).magnitude / dt;
            lastAnchor = p[0];
            if (speed > 40f) speed = 0f; // teleport
            speedSmoothed = Mathf.Lerp(speedSmoothed, speed, 1f - Mathf.Exp(-3f * dt));
            float unroll = Mathf.Lerp(1f, 0.62f, Mathf.Clamp01(speedSmoothed / 8f));

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
            float facing = veil.Facing;

            // Per-sample frame, width and alpha of the scarf body.
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / (samples - 1);
                Vector3 tan = centre[Mathf.Min(i + 1, samples - 1)] - centre[Mathf.Max(i - 1, 0)];
                tan.z = 0f;
                if (tan.sqrMagnitude < 1e-8f) tan = Vector3.down;
                tan.Normalize();
                normalAt[i] = new Vector3(-tan.y, tan.x, 0f);
                float hard = Mathf.SmoothStep(0f, 1f, hardAt[i]);
                hardAt[i] = hard;

                // A scarf: snug at the neck, a band that slowly tapers; it ripples and twists as it flies.
                float flowProfile = Mathf.Lerp(0.75f, 1f, Mathf.SmoothStep(0f, 1f, t * 5f)) * Mathf.Lerp(1f, 0.55f, Mathf.Pow(t, 1.6f))
                                    * (1f + 0.18f * Mathf.Sin(t * 9f - time * 6f));
                float twist = Mathf.Abs(Mathf.Cos(time * 2.7f + t * 6f));
                flowProfile *= Mathf.Lerp(1f, twist * 0.65f + 0.35f, flutterTwist * t);

                float weaponProfile = t < hf ? 0.4f : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.86f) / 0.14f) * 0.85f;
                weaponProfile *= 1f + 0.08f * Mathf.Sin(t * 14f - time * 5f);
                widthAt[i] = Mathf.Lerp(flowW * flowProfile, formW * weaponProfile, hard) * Mathf.Lerp(0.45f, 1f, vit);

                float fray = 1f - Mathf.SmoothStep(0f, 1f, (t - Mathf.Lerp(0.3f, 1.05f, vit)) / 0.25f);
                alphaAt[i] = Mathf.Lerp(fray, 1f, hard) * Mathf.Lerp(0.6f, 1f, vit) * Mathf.SmoothStep(0f, 1f, t * 12f + 0.3f);
            }

            int per = perStrand * 2;
            for (int s = 0; s < Strands.Length; s++)
            {
                var st = Strands[s];
                bool main = s == 1;
                bool branch = st.start > 0f;
                int baseV = s * per;
                Vector3 lastC = centre[0], lastTan = Vector3.right;
                float lastW = 0f, lastA = 0f, lastHard = 0f;

                // 1) Body: follows the scarf from `start` to `end`.
                for (int i = 0; i < samples; i++)
                {
                    float u = (float)i / (samples - 1);
                    float f = Mathf.Lerp(st.start, st.end, u);
                    float fi = f * (samples - 1);
                    int i0 = Mathf.Min(Mathf.FloorToInt(fi), samples - 2);
                    float fr = fi - i0;
                    Vector3 c = Vector3.Lerp(centre[i0], centre[i0 + 1], fr);
                    Vector3 nrm = Vector3.Lerp(normalAt[i0], normalAt[i0 + 1], fr).normalized;
                    float w = Mathf.Lerp(widthAt[i0], widthAt[i0 + 1], fr);
                    float a = Mathf.Lerp(alphaAt[i0], alphaAt[i0 + 1], fr);
                    float hard = Mathf.Lerp(hardAt[i0], hardAt[i0 + 1], fr);
                    float loose = 1f - hard;

                    // Strands drift apart towards the tail and fold back into one when hardened.
                    float wave = Mathf.Sin(f * Mathf.PI * 2.4f - time * 2.1f + st.phase);
                    float off;
                    if (branch)
                        off = (BranchOffset(u, st.side, tendrilSpread * Mathf.Lerp(0.5f, 1f, vit)) + wave * 0.14f * st.spread * u) * loose;
                    else
                        off = (st.side + wave * 0.35f * st.spread * f) * w * loose * Mathf.SmoothStep(0f, 1f, f * 3f);
                    c += nrm * off + new Vector3(0f, 0f, (s - 1) * 0.012f);

                    // Tendrils emerge from the ribbon: thin where they split, then full.
                    float sw = w * Mathf.Lerp(st.width, main ? 1f : 0.2f, hard);
                    if (branch) sw *= Mathf.Lerp(0.25f, 1f, Mathf.SmoothStep(0f, 0.35f, u));
                    float alpha = a * Mathf.Lerp(st.alpha, main ? 1f : 0.1f, hard);
                    if (branch) alpha *= Mathf.SmoothStep(0f, 1f, u * 5f);

                    WriteVertex(baseV + i * 2, c, nrm, sw, alpha, st, hard, vit);
                    if (i > 0) lastTan = c - lastC;
                    lastC = c; lastW = sw; lastA = alpha; lastHard = hard;
                }

                // 2) End curl: the stroke keeps going and winds into a spiral. A hardened tip doesn't curl.
                float looseEnd = 1f - lastHard;
                float breathe = 1f + 0.18f * Mathf.Sin(time * (1.3f + st.seed) + st.phase);
                float turns = st.curlTurns * breathe * unroll * looseEnd;
                float length = st.curlLength * curlScale * Mathf.Lerp(0.5f, 1f, vit) * Mathf.Lerp(0.15f, 1f, looseEnd);
                lastTan.z = 0f;
                float heading = lastTan.sqrMagnitude > 1e-10f ? Mathf.Atan2(lastTan.y, lastTan.x) : -Mathf.PI * 0.5f;
                BuildCurl(lastC, heading, st.curlSign * facing, length, turns, curlPoints, curl);
                for (int k = 1; k <= curlPoints; k++)
                {
                    float u = (float)k / curlPoints;
                    Vector3 d = curl[Mathf.Min(k + 1, curlPoints)] - curl[k - 1];
                    d.z = 0f;
                    Vector3 nrm = d.sqrMagnitude > 1e-10f ? new Vector3(-d.y, d.x, 0f).normalized : Vector3.up;
                    // Stays broad through most of the loop, thinning only at the very tip.
                    float sw = lastW * Mathf.Lerp(1f, 0.12f, Mathf.Pow(u, 1.6f));
                    float alpha = lastA * Mathf.Lerp(1f, 0.55f, u);
                    WriteVertex(baseV + (samples - 1 + k) * 2, curl[k], nrm, sw, alpha, st, lastHard, vit);
                }
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetUVs(1, uv2);
            mesh.RecalculateBounds();
        }

        void WriteVertex(int v, Vector3 c, Vector3 nrm, float width, float alpha, Strand st, float hard, float vit)
        {
            vertices[v] = c + nrm * (width * 0.5f);
            vertices[v + 1] = c - nrm * (width * 0.5f);
            var col = st.tint;
            col.a = alpha;
            col.r *= Mathf.Lerp(0.6f, 1f, vit);
            colors[v] = colors[v + 1] = col;
            uv2[v] = uv2[v + 1] = new Vector2(hard, st.seed);
        }
    }
}
