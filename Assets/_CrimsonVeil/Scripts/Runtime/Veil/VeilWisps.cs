using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Ink-in-water wisps shed from the veil and advected by the same vortex field,
    /// which makes the Kármán street visible as red curls behind the hero.
    /// </summary>
    [DefaultExecutionOrder(70)]
    [RequireComponent(typeof(VeilController))]
    public sealed class VeilWisps : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField] int maxWisps = 360;
        [SerializeField] float baseRate = 35f;
        [SerializeField] float speedRate = 12f;
        [SerializeField] float lifetime = 1.4f;
        [SerializeField] Vector2 sizeRange = new Vector2(0.04f, 0.12f);
        [SerializeField] float streak = 0.06f;
        [SerializeField] float followSharpness = 8f;
        [SerializeField] Color color = new Color(0.85f, 0.02f, 0.04f, 0.55f);

        struct Wisp
        {
            public Vector3 position;
            public Vector3 velocity;
            public float age;
            public float life;
            public float size;
        }

        VeilController veil;
        Wisp[] wisps;
        int cursor;
        float emitAccumulator;
        GameObject meshObject;
        Mesh mesh;
        Vector3[] vertices;
        Color[] colors;

        void Awake()
        {
            veil = GetComponent<VeilController>();
            wisps = new Wisp[maxWisps];
        }

        void Start()
        {
            vertices = new Vector3[maxWisps * 4];
            colors = new Color[maxWisps * 4];
            var uvs = new Vector2[maxWisps * 4];
            var tris = new int[maxWisps * 6];
            for (int i = 0; i < maxWisps; i++)
            {
                int v = i * 4, k = i * 6;
                uvs[v] = new Vector2(0, 0); uvs[v + 1] = new Vector2(1, 0);
                uvs[v + 2] = new Vector2(1, 1); uvs[v + 3] = new Vector2(0, 1);
                tris[k] = v; tris[k + 1] = v + 2; tris[k + 2] = v + 1;
                tris[k + 3] = v; tris[k + 4] = v + 3; tris[k + 5] = v + 2;
            }
            mesh = new Mesh { name = "VeilWisps" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;

            meshObject = new GameObject("VeilWisps");
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
            float softness = 1f - sim.Hardness * 0.7f;
            emitAccumulator += (baseRate + speedRate * speed) * softness * dt;
            while (emitAccumulator >= 1f)
            {
                emitAccumulator -= 1f;
                Emit(sim);
            }

            float follow = 1f - Mathf.Exp(-followSharpness * dt);
            for (int i = 0; i < wisps.Length; i++)
            {
                if (wisps[i].life <= 0f) continue;
                ref var w = ref wisps[i];
                w.age += dt;
                if (w.age >= w.life) { w.life = 0f; continue; }
                Vector2 air = sim.SampleAirVelocity(w.position);
                w.velocity = Vector3.Lerp(w.velocity, new Vector3(air.x, air.y, 0f), follow);
                w.position += w.velocity * dt;
            }
        }

        void Emit(VeilSimulation sim)
        {
            int n = sim.Count;
            int node = Random.Range(Mathf.FloorToInt(n * 0.3f), n);
            ref var w = ref wisps[cursor];
            cursor = (cursor + 1) % wisps.Length;
            w.position = sim.Positions[node] + (Vector3)(Random.insideUnitCircle * 0.05f);
            w.velocity = Vector3.zero;
            w.age = 0f;
            w.life = lifetime * Random.Range(0.6f, 1.2f);
            w.size = Random.Range(sizeRange.x, sizeRange.y);
        }

        void LateUpdate()
        {
            if (mesh == null) return;
            for (int i = 0; i < wisps.Length; i++)
            {
                int v = i * 4;
                var w = wisps[i];
                if (w.life <= 0f)
                {
                    vertices[v] = vertices[v + 1] = vertices[v + 2] = vertices[v + 3] = Vector3.zero;
                    continue;
                }
                Vector3 dir = w.velocity;
                dir.z = 0f;
                float spd = dir.magnitude;
                dir = spd > 1e-4f ? dir / spd : Vector3.right;
                var side = new Vector3(-dir.y, dir.x, 0f) * (w.size * 0.5f);
                var along = dir * ((w.size + spd * streak) * 0.5f);
                vertices[v] = w.position - along - side;
                vertices[v + 1] = w.position + along - side;
                vertices[v + 2] = w.position + along + side;
                vertices[v + 3] = w.position - along + side;

                float fade = Mathf.Sin(Mathf.Clamp01(w.age / w.life) * Mathf.PI);
                var c = color;
                c.a *= fade;
                colors[v] = colors[v + 1] = colors[v + 2] = colors[v + 3] = c;
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }
    }
}
