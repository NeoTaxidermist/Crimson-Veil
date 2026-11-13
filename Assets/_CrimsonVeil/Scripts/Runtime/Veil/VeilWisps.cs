using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Rare fine smoke threads leaving the veil's tip (the main swirl is the connected tendrils of
    /// VeilRenderer). Each thread is advected by the same vortex field as the cloth and remembers its
    /// path, drawn as a thin tapering ribbon.
    /// </summary>
    [DefaultExecutionOrder(70)]
    [RequireComponent(typeof(VeilController))]
    public sealed class VeilWisps : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField] int maxWisps = 60;
        [SerializeField] int trailPoints = 24;
        [SerializeField] float sampleInterval = 0.025f;
        [SerializeField] float baseRate = 3f;
        [SerializeField] float speedRate = 0.6f;
        [SerializeField] float weaponRate = 5f;
        [SerializeField] float swingRate = 16f;
        [SerializeField] float lifetime = 1.9f;
        [SerializeField] Vector2 widthRange = new Vector2(0.05f, 0.18f);
        [SerializeField] float followSharpness = 1.6f;
        [Tooltip("Base turn rate of a curl (rad/s); grows as the curl ages, so strokes wind into spirals.")]
        [SerializeField] float curl = 3.6f;
        [SerializeField] float drag = 0.7f;
        [SerializeField] float maxSpeed = 3.5f;
        float swingAccumulator;

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
            float vit = Mathf.Lerp(0.3f, 1f, veil.Vitality);
            bool weapon = veil.Form != VeilForm.Flow && veil.Hardness > 0.2f;

            // Loose cloth: curls peel off the tail. Weapon: the blade keeps smoking along its length.
            emitAccumulator += (weapon ? weaponRate : baseRate + speedRate * speed) * vit * dt;
            while (emitAccumulator >= 1f)
            {
                emitAccumulator -= 1f;
                if (weapon) Emit(sim, Mathf.Max(veil.HandFraction, 0.2f), 1f, 0.6f);
                else Emit(sim, 0.85f, 1f, 0.25f); // only a faint thread now and then leaves the very tip
            }
            // A swing leaves a smoky arc behind the tip.
            if (veil.IsAttacking)
            {
                swingAccumulator += swingRate * dt;
                while (swingAccumulator >= 1f) { swingAccumulator -= 1f; Emit(sim, 0.8f, 1f, 0.3f); }
            }

            float follow = 1f - Mathf.Exp(-followSharpness * dt);
            float decay = Mathf.Exp(-drag * dt);
            int active = 0;
            for (int i = 0; i < wisps.Length; i++)
            {
                ref var w = ref wisps[i];
                if (w.life <= 0f) continue;
                w.age += dt;
                if (w.age >= w.life) { w.life = 0f; continue; }
                active++;

                Vector2 air = sim.SampleAirVelocity(w.head);
                // Every stroke curls into a tightening spiral: its turn rate grows as it ages and slows.
                float omega = w.spin * curl * (0.6f + 1.6f * w.age / w.life);
                Vector3 v = Vector3.Lerp(w.velocity, new Vector3(air.x, air.y, 0f), follow) * decay;
                float c = Mathf.Cos(omega * dt), s = Mathf.Sin(omega * dt);
                w.velocity = new Vector3(v.x * c - v.y * s, v.x * s + v.y * c, 0f);
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

        /// <summary>Puff of curls along part of the veil (e.g. when it changes form).</summary>
        public void Burst(int count, float fromFraction, float toFraction, float speed)
        {
            var sim = veil != null ? veil.Simulation : null;
            if (sim == null) return;
            for (int i = 0; i < count; i++) Emit(sim, fromFraction, toFraction, speed);
        }

        void OnEnable()
        {
            if (veil == null) veil = GetComponent<VeilController>();
            veil.FormChanged += OnFormChanged;
        }

        void OnDisable()
        {
            if (veil != null) veil.FormChanged -= OnFormChanged;
        }

        // The cloth unfurls into (or dissolves out of) a weapon in a swirl of smoke.
        void OnFormChanged(VeilForm form) => Burst(form == VeilForm.Flow ? 10 : 8, 0.4f, 1f, 1.4f);

        void Emit(VeilSimulation sim, float fromFraction, float toFraction, float speed)
        {
            int n = sim.Count;
            int node = Mathf.Clamp(Mathf.RoundToInt(Random.Range(fromFraction, toFraction) * (n - 1)), 0, n - 1);
            ref var w = ref wisps[cursor];
            int baseIdx = cursor * trailPoints;
            cursor = (cursor + 1) % wisps.Length;

            Vector3 p = sim.Positions[node];
            Vector3 tan = sim.Positions[Mathf.Min(node + 1, n - 1)] - sim.Positions[Mathf.Max(node - 1, 0)];
            Vector3 normal = new Vector3(-tan.y, tan.x, 0f).normalized * (Random.value < 0.5f ? -1f : 1f);
            w.head = p + normal * Random.Range(0f, 0.08f);
            w.head.z = p.z + Random.Range(-0.05f, 0.05f);
            w.velocity = normal * speed * Random.Range(0.5f, 1.2f);
            w.age = 0f;
            // The swirl itself is drawn by VeilRenderer as connected tendrils; detached smoke is only rare,
            // fine filaments (thick loose curls read as flying petals, not as part of the veil).
            bool filament = Random.value < 0.85f;
            w.life = lifetime * Random.Range(0.6f, 1.2f) * (filament ? 1.35f : 1f);
            w.width = filament ? Random.Range(0.015f, 0.04f) : Random.Range(widthRange.x, widthRange.y);
            w.seed = Random.value;
            w.spin = (Random.value < 0.5f ? -1f : 1f) * Random.Range(0.6f, 1.4f) * (filament ? 1.5f : 1f);
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
