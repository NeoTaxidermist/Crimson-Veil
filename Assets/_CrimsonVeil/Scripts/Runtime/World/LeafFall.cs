using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>Configures a particle system for slowly drifting crimson leaves.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class LeafFall : MonoBehaviour
    {
        [SerializeField] Vector3 area = new Vector3(10f, 1f, 3f);
        [SerializeField] float rate = 6f;
        [SerializeField] Color color = new Color(0.85f, 0.02f, 0.04f, 1f);
        [SerializeField] Vector2 drift = new Vector2(-0.5f, -0.7f);

        void Awake()
        {
            var ps = GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 9f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = color;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 250;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = area;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(drift.x);
            vel.y = new ParticleSystem.MinMaxCurve(drift.y);
            vel.z = new ParticleSystem.MinMaxCurve(0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.7f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.2f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

            ps.Play();
        }
    }
}
