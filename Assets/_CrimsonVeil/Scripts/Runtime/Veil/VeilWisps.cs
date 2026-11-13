using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Smoke curls peeling off the veil's tail. Each wisp is advected by the same vortex field as the
    /// cloth and remembers its path, drawn as a thin tapering ribbon — the Kármán street becomes
    /// visible as red curling strokes behind the hero.
    /// </summary>
    [DefaultExecutionOrder(70)]
    [RequireComponent(typeof(VeilController))]
    public sealed class VeilWisps : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField] int maxWisps = 48;
        [SerializeField] int trailPoints = 14;
        [SerializeField] float sampleInterval = 0.04f;
        [SerializeField] float baseRate = 6f;
        [SerializeField] float speedRate = 3.5f;
        [SerializeField] float lifetime = 1.6f;
        [SerializeField] Vector2 widthRange = new Vector2(0.05f, 0.14f);
        [SerializeField] float followSharpness = 3.5f;
        [SerializeField] float curl = 1.4f;
        [SerializeField] float maxSpeed = 4f;

        struct Wisp
        {
            public Vector3 head;
            public Vector3 velocity;
            public float age, life, width, seed, sampleTimer, spin;
            public int count;
        }

        VeilController veil;
        Wisp[] wisps;
        Vector3[] trails; // [wisp * trailPoints + k], k = 0 newest
        int cursor;
        float emitAccumulator;
        Vector3 lastRoot;
        GameObject meshObject;
        Mesh mesh;
        Vector3[] vertices;
        Color[] colors;
        Vector2[] uv2;

        public int ActiveCount { get; private set; }

        void Awake()
        {
            veil = GetComponent<VeilController>();
            wisps = new Wisp[maxWisps];
            trails = new Vector3[maxWisps * trailPoints];
        }

        void Start()
        {
            int per = trailPoints * 2;
            vertices = new Vector3[maxWisps * per];
            colors = new Color[vertices.Length];
            uv2 = new Vector2[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var tris = new int[maxWisps * (trailPoints - 1) * 6];
            int k = 0;
            for (int w = 0; w < maxWisps; w++)
            for (int i = 0; i < trailPoints; i++)
            {
                int v = w * per + i * 2;
                float t = (float)i / (trailPoints - 1);
                uvs[v] = new Vector2(t, 0f);
                uvs[v + 1] = new Vector2(t, 1f);
                if (i == trailPoints - 1) continue;
                tris[k++] = v; tris[k++] = v + 2; tris[k++] = v + 1;
                tris[k++] = v + 1; tris[k++] = v + 2; tris[k++] = v + 3;
            }
            mesh = new Mesh { name = "VeilSmoke" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;

            meshObject = new GameObject("VeilSmoke");
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

        void Update()
        {
            var sim = veil.Simulation;
            if (sim == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float speed = sim.AnchorVelocity.magnitude;
            Vector3 root = sim.Positions[0];
            bool jumped = (root - lastRoot).sqrMagnitude > 9f;
            lastRoot = root;
            if (jumped)
            {
                // Teleport / respawn: old trails would streak across the screen.
                for (int i = 0; i < wisps.Length; i++) wisps[i].life = 0f;
                return;
            }
            float softness = 1f - Mathf.Clamp01(veil.Hardness) * 0.8f;
            emitAccumulator += (baseRate + speedRate * speed) * softness * Mathf.Lerp(0.3f, 1f, veil.Vitality) * dt;
            while (emitAccumulator >= 1f)
            {
                emitAccumulator -= 1f;
                Emit(sim);
            }

            float follow = 1f - Mathf.Exp(-followSharpness * dt);
            int active = 0;
            for (int i = 0; i < wisps.Length; i++)
            {
                ref var w = ref wisps[i];
                if (w.life <= 0f) continue;
                w.age += dt;
                if (w.age >= w.life) { w.life = 0f; continue; }
                active++;

                Vector2 air = sim.SampleAirVelocity(w.head);
                // A gentle curl of its own so every stroke spirals a little.
                var spinV = new Vector2(-w.velocity.y, w.velocity.x) * (w.spin * curl * dt);
                w.velocity = Vector3.Lerp(w.velocity, new Vector3(air.x, air.y, 0f), follow) + new Vector3(spinV.x, spinV.y, 0f);
                w.velocity = Vector3.ClampMagnitude(w.velocity, maxSpeed); // smoke drifts, it never darts
                w.head += w.velocity * dt;

                w.sampleTimer -= dt;
                int baseIdx = i * trailPoints;
                if (w.sampleTimer <= 0f)
                {
                    w.sampleTimer = sampleInterval;
                    for (int k = trailPoints - 1; k > 0; k--) trails[baseIdx + k] = trails[baseIdx + k - 1];
                    w.count = Mathf.Min(trailPoints, w.count + 1);
                }
                trails[baseIdx] = w.head;
            }
            ActiveCount = active;
        }

        void Emit(VeilSimulation sim)
        {
            int n = sim.Count;
            int node = Random.Range(Mathf.FloorToInt(n * 0.55f), n);
            ref var w = ref wisps[cursor];
            int baseIdx = cursor * trailPoints;
            cursor = (cursor + 1) % wisps.Length;

            w.head = sim.Positions[node] + (Vector3)(Random.insideUnitCircle * 0.06f);
            w.velocity = Vector3.zero;
            w.age = 0f;
            w.life = lifetime * Random.Range(0.6f, 1.15f);
            w.width = Random.Range(widthRange.x, widthRange.y);
            w.seed = Random.value;
            w.spin = Random.value < 0.5f ? -1f : 1f;
            w.sampleTimer = sampleInterval;
            w.count = 1;
            for (int k = 0; k < trailPoints; k++) trails[baseIdx + k] = w.head;
        }

        void LateUpdate()
        {
            if (mesh == null) return;
            int per = trailPoints * 2;
            for (int i = 0; i < wisps.Length; i++)
            {
                var w = wisps[i];
                int baseV = i * per, baseIdx = i * trailPoints;
                if (w.life <= 0f)
                {
                    for (int k = 0; k < per; k++) vertices[baseV + k] = Vector3.zero;
                    continue;
                }
                float lifeFade = Mathf.Sin(Mathf.Clamp01(w.age / w.life) * Mathf.PI);
                for (int k = 0; k < trailPoints; k++)
                {
                    int kk = Mathf.Min(k, w.count - 1);
                    Vector3 p = trails[baseIdx + kk];
                    Vector3 prev = trails[baseIdx + Mathf.Max(kk - 1, 0)];
                    Vector3 next = trails[baseIdx + Mathf.Min(kk + 1, w.count - 1)];
                    Vector3 tan = prev - next;
                    tan.z = 0f;
                    if (tan.sqrMagnitude < 1e-8f) tan = Vector3.right;
                    tan.Normalize();
                    var normal = new Vector3(-tan.y, tan.x, 0f);
                    float t = (float)k / (trailPoints - 1);
                    float width = w.width * Mathf.Sin(Mathf.Clamp01(t * 1.15f) * Mathf.PI) * (0.6f + 0.6f * lifeFade);
                    int v = baseV + k * 2;
                    vertices[v] = p + normal * (width * 0.5f);
                    vertices[v + 1] = p - normal * (width * 0.5f);
                    var c = new Color(1f, 1f, 1f, lifeFade * 0.85f);
                    colors[v] = colors[v + 1] = c;
                    uv2[v] = uv2[v + 1] = new Vector2(0f, w.seed);
                }
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetUVs(1, uv2);
            mesh.RecalculateBounds();
        }
    }
}
