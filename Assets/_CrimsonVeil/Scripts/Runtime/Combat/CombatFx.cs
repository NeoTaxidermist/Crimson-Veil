using CrimsonVeil.CameraSystem;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Game-feel helpers: hit-stop, camera trauma and ink bursts. Safe to call without a scene
    /// instance (e.g. in tests) — effects are then skipped.
    /// </summary>
    public sealed class CombatFx : MonoBehaviour
    {
        public static readonly Color Crimson = new Color(0.9f, 0.03f, 0.05f, 0.9f);
        public static readonly Color Ink = new Color(0.03f, 0.03f, 0.03f, 0.9f);

        [SerializeField] Material burstMaterial;
        [SerializeField, Range(0f, 1f)] float hitStopScale = 0.03f;

        public static CombatFx Instance { get; private set; }

        float hitStopUntil;
        bool stopping;

        void Awake() => Instance = this;

        void OnDisable()
        {
            if (stopping) Time.timeScale = 1f;
            stopping = false;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (stopping && Time.unscaledTime >= hitStopUntil)
            {
                stopping = false;
                Time.timeScale = 1f;
            }
        }

        /// <summary>Freeze the world for a few frames so hits land with weight.</summary>
        public static void HitStop(float seconds)
        {
            var fx = Instance;
            if (fx == null || seconds <= 0f) return;
            fx.hitStopUntil = Mathf.Max(fx.hitStopUntil, Time.unscaledTime + seconds);
            fx.stopping = true;
            Time.timeScale = fx.hitStopScale;
        }

        public static void Shake(float trauma) => CameraFollow25D.AddTrauma(trauma);

        public static void Burst(Vector3 position, Vector2 direction, Color color, int count = 18, float speed = 6f)
        {
            var fx = Instance;
            if (fx == null) return;

            var go = new GameObject("InkBurst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor = color;
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.useUnscaledTime = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;
            Vector2 d = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector2.up;
            go.transform.rotation = Quaternion.LookRotation(new Vector3(d.x, d.y, 0f), Vector3.back);

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 2.5f;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = fx.burstMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }
    }
}
