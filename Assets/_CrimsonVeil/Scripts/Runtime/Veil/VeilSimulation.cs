using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    [Serializable]
    public class VeilSettings
    {
        [Header("Cloth")]
        [Min(3)] public int nodeCount = 32;
        public float length = 3.3f;
        [Range(1, 20)] public int constraintIterations = 14;
        [Range(0.9f, 1f)] public float damping = 0.985f;
        public float airDrag = 4.5f;
        public float gravityScale = 0.08f;
        public float depthOffset = 0.12f;
        public float depthWobble = 0.08f;

        [Header("Vortex shedding (Karman street)")]
        public float strouhal = 0.21f;
        public float bodyDiameter = 0.45f;
        public float idleShedFrequency = 0.8f;
        public float circulationGain = 1.6f;
        public float idleCirculation = 0.9f;
        public float coreRadius = 0.2f;
        public float viscosity = 0.03f;
        public float vortexLifetime = 1.8f;
        [Min(1)] public int maxVortices = 40;
        public float lateralShedOffset = 0.3f;

        [Header("Life (a scarf with a will of its own, not a cape)")]
        [Tooltip("The run wind is felt at most this strongly (m/s), so the scarf trails without stretching into a sheet.")]
        public float maxRelativeWind = 1.2f;
        [Tooltip("Serpentine undulation travelling from the neck to the tip (acceleration, m/s²).")]
        public float undulation = 7f;
        public float undulationWaves = 1.6f;
        public float undulationSpeed = 3.2f;
        [Tooltip("Extra undulation per m/s of the hero's speed.")]
        public float undulationPerSpeed = 2.2f;
        [Tooltip("Upward drift of the tail, like smoke rising.")]
        public float buoyancy = 2.6f;
        [Tooltip("The tip slowly curls into a loop.")]
        public float tipCurl = 5f;
        [Tooltip("Magical breeze (m/s) that keeps the scarf floating behind the hero even at rest (key art).")]
        public float drift = 1.4f;
        [Tooltip("Upward part of the breeze (m/s).")]
        public float driftLift = 0.5f;

        [Header("Hardening (weapon forms)")]
        public float hardenRate = 45f;

        [Header("Simulation")]
        public float maxSubstep = 1f / 120f;
        public float teleportDistance = 5f;
    }

    public struct Vortex
    {
        public Vector2 position;
        public float circulation;
        public float age;
        public float lifetime;
        public float coreRadius0;
        public float viscosity;

        /// <summary>Fades in quickly (no popping) and out linearly over its lifetime.</summary>
        public float Fade => Mathf.Clamp01(age / 0.08f) * (1f - Mathf.Clamp01(age / lifetime));
        public float Strength => circulation * Fade;
        /// <summary>Lamb-Oseen core grows by viscous diffusion: rc² = rc0² + 4νt.</summary>
        public float CoreRadius => Mathf.Sqrt(coreRadius0 * coreRadius0 + 4f * viscosity * age);
    }

    /// <summary>
    /// The magic scarf: a Verlet ribbon pushed around by a 2D Kármán vortex street that the
    /// hero's body sheds as it moves through the air. Weapon forms "harden" the ribbon by
    /// pulling nodes towards a target shape while the vortices still make it ripple.
    /// Pure C#, no MonoBehaviour, fully deterministic and testable.
    /// </summary>
    public sealed class VeilSimulation
    {
        static readonly Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

        public VeilSettings Settings { get; }
        public int Count { get; }
        public Vector3[] Positions { get; }
        public Vector3[] Targets { get; }
        /// <summary>Rest length of segment i → i+1 for the hardened shape.</summary>
        public float[] TargetRest { get; }
        public float FlowRestLength { get; }

        public float Hardness { get; set; }
        /// <summary>
        /// Optional per-node hardness (0..1). When <see cref="UseNodeHardness"/> is set, hardening can
        /// travel along the ribbon as a wave instead of snapping all at once.
        /// </summary>
        public float[] NodeHardness { get; private set; }
        public bool UseNodeHardness { get; set; }
        public Vector2 Wind { get; set; }

        float H(int i) => UseNodeHardness ? Mathf.Clamp01(NodeHardness[i]) : Mathf.Clamp01(Hardness);
        /// <summary>Slow magical drift of the air around the hero (e.g. "behind and slightly up").</summary>
        public Vector2 AmbientFlow { get; set; }
        public Vector3 AnchorVelocity { get; private set; }
        /// <summary>Which way (-1/+1 in x) the magical breeze blows; 0 = no breeze. Set to minus the hero's facing.</summary>
        public float DriftDirection { get; set; }
        public IReadOnlyList<Vortex> Vortices => vortices;
        public int ShedCount { get; private set; }

        readonly Vector3[] previous;
        readonly List<Vortex> vortices = new List<Vortex>();
        float shedPhase;
        int nextSign = 1;
        float time;
        Vector3 lastAnchor;

        public VeilSimulation(VeilSettings settings, Vector3 anchor)
        {
            Settings = settings ?? new VeilSettings();
            Count = Mathf.Max(3, Settings.nodeCount);
            Positions = new Vector3[Count];
            previous = new Vector3[Count];
            Targets = new Vector3[Count];
            TargetRest = new float[Count - 1];
            NodeHardness = new float[Count];
            FlowRestLength = Settings.length / (Count - 1);
            Reset(anchor);
        }

        public void Reset(Vector3 anchor)
        {
            var dir = new Vector3(-0.3f, -0.95f, 0f).normalized;
            for (int i = 0; i < Count; i++)
            {
                Positions[i] = previous[i] = Targets[i] = anchor + dir * (FlowRestLength * i);
                if (i < Count - 1) TargetRest[i] = FlowRestLength;
            }
            vortices.Clear();
            shedPhase = 0f;
            lastAnchor = anchor;
            AnchorVelocity = Vector3.zero;
        }

        /// <summary>Recompute <see cref="TargetRest"/> from the current <see cref="Targets"/>.</summary>
        public void RecomputeTargetRest()
        {
            for (int i = 0; i < Count - 1; i++)
                TargetRest[i] = Vector3.Distance(Targets[i], Targets[i + 1]);
        }

        public void Step(float dt, Vector3 anchor)
        {
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            dt = Mathf.Min(dt, 0.1f);

            if ((anchor - lastAnchor).magnitude > Settings.teleportDistance)
                Reset(anchor);

            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / Mathf.Max(0.001f, Settings.maxSubstep)));
            float h = dt / steps;
            Vector3 from = lastAnchor;
            Vector3 anchorVel = (anchor - from) / dt;
            AnchorVelocity = anchorVel;

            for (int k = 1; k <= steps; k++)
            {
                Vector3 a = Vector3.Lerp(from, anchor, (float)k / steps);
                SubStep(h, a, anchorVel, a - anchor);
            }

            lastAnchor = anchor;
        }

        void SubStep(float h, Vector3 a, Vector3 anchorVel, Vector3 targetOffset)
        {
            var s = Settings;
            time += h;
            Vector2 air = Wind + AmbientFlow;

            // 1) Advect and age vortices (they live in the air, not on the body).
            for (int i = vortices.Count - 1; i >= 0; i--)
            {
                var v = vortices[i];
                v.age += h;
                v.position += air * h;
                if (v.age >= v.lifetime) vortices.RemoveAt(i);
                else vortices[i] = v;
            }

            // 2) Shed alternating vortices at the Strouhal frequency f = St·U/D (+ idle magic).
            Vector2 rel = air - new Vector2(anchorVel.x, anchorVel.y);
            float speed = rel.magnitude;
            float freq = s.idleShedFrequency + s.strouhal * speed / Mathf.Max(0.05f, s.bodyDiameter);
            shedPhase += freq * h;
            while (shedPhase >= 1f)
            {
                shedPhase -= 1f;
                Shed(a, rel, speed);
            }

            // 2b) Hardened parts are carried rigidly by the body, so a running hero never outruns the weapon.
            Vector3 carry = anchorVel * h;
            for (int i = 1; i < Count; i++)
            {
                float hi = H(i);
                if (hi <= 0.001f) continue;
                Positions[i] += carry * hi;
                previous[i] += carry * hi;
            }

            // 3) Verlet integration with drag towards the local air velocity.
            Vector3 g = Gravity * s.gravityScale;
            var bodyVel = new Vector2(anchorVel.x, anchorVel.y);
            float bodySpeed = bodyVel.magnitude;
            float wavePhase = s.undulationSpeed * time;
            for (int i = 1; i < Count; i++)
            {
                Vector3 p = Positions[i];
                Vector3 vel = (p - previous[i]) / h;
                float f = (float)i / (Count - 1);
                float loose = 1f - H(i);

                // Feel the run wind only up to a point: the scarf trails, it doesn't become a taut sheet.
                Vector2 airV = SampleAirVelocity(p);
                Vector2 relAir = airV - bodyVel;
                if (relAir.sqrMagnitude > s.maxRelativeWind * s.maxRelativeWind) relAir = relAir.normalized * s.maxRelativeWind;
                airV = Vector2.Lerp(airV, bodyVel + relAir, loose);
                // The veil floats on its own breeze: it streams behind the hero even standing still.
                if (DriftDirection != 0f)
                    airV += new Vector2(DriftDirection * s.drift, s.driftLift) * (loose * Mathf.SmoothStep(0f, 1f, f * 4f));
                Vector3 acc = new Vector3((airV.x - vel.x) * s.airDrag, (airV.y - vel.y) * s.airDrag, 0f) + g;

                if (loose > 0.001f)
                {
                    // Its own life: a serpentine wave running down the scarf (stronger when moving),
                    // a tail that rises like smoke and a tip that curls over.
                    Vector3 tan = Positions[Mathf.Min(i + 1, Count - 1)] - Positions[i - 1];
                    tan.z = 0f;
                    if (tan.sqrMagnitude > 1e-8f)
                    {
                        tan.Normalize();
                        var normal = new Vector3(-tan.y, tan.x, 0f);
                        float amp = (s.undulation + s.undulationPerSpeed * bodySpeed) * f;
                        float wave = Mathf.Sin(s.undulationWaves * Mathf.PI * 2f * f - wavePhase);
                        acc += normal * (amp * wave + s.tipCurl * Mathf.SmoothStep(0f, 1f, (f - 0.7f) / 0.3f)) * loose;
                    }
                    acc.y += s.buoyancy * f * f * loose;
                }
                Vector3 next = p + (p - previous[i]) * s.damping + acc * (h * h);
                previous[i] = p;
                Positions[i] = next;
            }

            // 4) Root is pinned to the anchor.
            Positions[0] = a;
            previous[0] = a - anchorVel * h;

            // 5) Hardening: pull towards the weapon shape and bleed velocity.
            for (int i = 1; i < Count; i++)
            {
                float hi = H(i);
                if (hi <= 0.001f) continue;
                float f = 1f - Mathf.Exp(-s.hardenRate * hi * h);
                Positions[i] = Vector3.Lerp(Positions[i], Targets[i] + targetOffset, f);
                previous[i] = Vector3.Lerp(previous[i], Positions[i], f * 0.5f);
            }

            // 6) Distance constraints (rest length morphs from cloth to weapon shape, node by node).
            for (int it = 0; it < s.constraintIterations; it++)
            {
                for (int i = 0; i < Count - 1; i++)
                {
                    float rest = Mathf.Lerp(FlowRestLength, TargetRest[i], 0.5f * (H(i) + H(i + 1)));
                    Vector3 d = Positions[i + 1] - Positions[i];
                    d.z = 0f; // 2.5D: lengths live in the picture plane; depth is only a cosmetic wobble
                    float len = d.magnitude;
                    if (len < 1e-6f) continue;
                    Vector3 corr = d * ((len - rest) / len);
                    if (i == 0) Positions[1] -= corr;
                    else
                    {
                        Positions[i] += corr * 0.5f;
                        Positions[i + 1] -= corr * 0.5f;
                    }
                }
            }

            // 7) 2.5D: keep the ribbon just behind the hero with a little depth flutter.
            for (int i = 1; i < Count; i++)
            {
                float t = (float)i / (Count - 1);
                float z = a.z + s.depthOffset + Mathf.Sin(time * 2.3f + i * 0.55f) * s.depthWobble * t;
                Positions[i].z = z;
                previous[i].z = z;
            }
        }

        void Shed(Vector3 anchor, Vector2 rel, float speed)
        {
            var s = Settings;
            Vector2 dir = speed > 1e-3f ? rel / speed : Vector2.down;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float d = s.bodyDiameter;

            var vortex = new Vortex
            {
                position = new Vector2(anchor.x, anchor.y) + dir * (d * 0.5f) + perp * (s.lateralShedOffset * nextSign),
                circulation = nextSign * (s.circulationGain * speed * d + s.idleCirculation * d),
                age = 0f,
                lifetime = s.vortexLifetime,
                coreRadius0 = s.coreRadius,
                viscosity = s.viscosity,
            };

            if (vortices.Count >= s.maxVortices) vortices.RemoveAt(0);
            vortices.Add(vortex);
            ShedCount++;
            nextSign = -nextSign;
        }

        /// <summary>Air velocity at a world point: wind + ambient drift + all vortices.</summary>
        public Vector2 SampleAirVelocity(Vector3 p)
        {
            Vector2 v = Wind + AmbientFlow;
            var q = new Vector2(p.x, p.y);
            for (int i = 0; i < vortices.Count; i++)
            {
                var vx = vortices[i];
                v += InducedVelocity(q, vx.position, vx.Strength, vx.CoreRadius);
            }
            return v;
        }

        /// <summary>
        /// Lamb-Oseen vortex: u_θ = Γ/(2πr)·(1 − e^(−r²/rc²)). Finite at the core, 1/r far away.
        /// Positive Γ rotates counter-clockwise.
        /// </summary>
        public static Vector2 InducedVelocity(Vector2 point, Vector2 center, float circulation, float coreRadius)
        {
            Vector2 r = point - center;
            float r2 = r.sqrMagnitude;
            if (r2 < 1e-8f) return Vector2.zero;
            float rc2 = Mathf.Max(1e-6f, coreRadius * coreRadius);
            float k = circulation / (2f * Mathf.PI * r2) * (1f - Mathf.Exp(-r2 / rc2));
            return new Vector2(-r.y, r.x) * k;
        }
    }
}
