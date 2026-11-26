using CrimsonVeil.CameraSystem;
using CrimsonVeil.Veil;
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

        /// <summary>How long (real seconds) the world will still stand still (0 when it is not).</summary>
        public static float HitStopLeft => Instance != null && Instance.stopping ? Mathf.Max(0f, Instance.hitStopUntil - Time.unscaledTime) : 0f;
        bool stopping;

        void Awake() => Instance = this;

        void OnDisable()
        {
            if (stopping) CrimsonVeil.Core.GameClock.EndHitStop();
            stopping = false;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (stopping && Time.unscaledTime >= hitStopUntil)
            {
                stopping = false;
                CrimsonVeil.Core.GameClock.EndHitStop();
            }
        }

        /// <summary>Freeze the world for a few frames so hits land with weight.</summary>
        /// <summary>The direction a burst flies: <paramref name="direction"/> in the picture, leaned by
        /// <paramref name="depth"/> (up to 45°) through depth.</summary>
        public static Vector3 BurstDirection(Vector2 direction, float depth)
        {
            Vector2 d = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector2.up;
            return new Vector3(d.x, d.y, Mathf.Clamp(depth, -1f, 1f)).normalized;
        }

        public static void HitStop(float seconds)
        {
            var fx = Instance;
            if (fx == null || seconds <= 0f) return;
            fx.hitStopUntil = Mathf.Max(fx.hitStopUntil, Time.unscaledTime + seconds);
            fx.stopping = true;
            CrimsonVeil.Core.GameClock.SetHitStop(fx.hitStopScale);   // (paused, the clock stands all the same)
        }

        public static void Shake(float trauma) => CameraFollow25D.AddTrauma(trauma);

        /// <summary>How long (s) the world stands still when a blow lands, by weapon; a finisher holds it longer.</summary>
        public static float HitStopFor(VeilForm form, bool finisher)
        {
            // (it was 0.055 s - three frames at 60 fps, not seen at all: the blow passed through as through smoke)
            float s = form == VeilForm.Spear || form == VeilForm.Shield ? 0.11f : form == VeilForm.Whip ? 0.07f : 0.085f;
            return finisher ? s * 1.7f : s;
        }

        public const float SlashSeconds = 0.14f;

        /// <summary>
        /// The cut itself: a bright crimson stroke through the point struck, along the blade's way, gone in a moment
        /// (in real time: it is seen through the hit-stop).
        /// </summary>
        public static void Slash(Vector3 point, Vector2 along, float length, float width)
        {
            var fx = Instance;
            if (fx == null) return;
            Vector2 d = along.sqrMagnitude > 1e-4f ? along.normalized : Vector2.right;
            var go = new GameObject("Slash");
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 3;
            Vector3 half = new Vector3(d.x, d.y, 0f) * (length * 0.5f);
            Vector3 front = new Vector3(0f, 0f, -0.3f);   // (in front of the body it cuts)
            lr.SetPosition(0, point - half + front);
            lr.SetPosition(1, point + front);
            lr.SetPosition(2, point + half + front);
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, width), new Keyframe(1f, 0f));
            lr.sharedMaterial = fx.burstMaterial;
            lr.startColor = lr.endColor = new Color(1f, 0.12f, 0.1f, 1f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.numCapVertices = 2;
            go.AddComponent<SlashMark>().Begin(lr, SlashSeconds);
        }

        /// <summary>The stroke thins and fades away.</summary>
        sealed class SlashMark : MonoBehaviour
        {
            LineRenderer lr;
            float start, length;
            public void Begin(LineRenderer l, float seconds) { lr = l; length = seconds; start = Time.unscaledTime; }
            void Update()
            {
                float t = (Time.unscaledTime - start) / length;
                if (t >= 1f) { Destroy(gameObject); return; }
                float k = 1f - t;
                lr.widthMultiplier = k;
                var c = lr.startColor; c.a = k; lr.startColor = lr.endColor = c;
            }
        }

        /// <summary>A spray of ink. <paramref name="depth"/> (-1 towards the camera .. 1 away) leans it through depth,
        /// as a blade sweeping through the scene throws it.</summary>
        public static void Burst(Vector3 position, Vector2 direction, Color color, int count = 18, float speed = 6f, float depth = 0f)
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
            go.transform.rotation = Quaternion.LookRotation(BurstDirection(direction, depth), Vector3.back);

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
