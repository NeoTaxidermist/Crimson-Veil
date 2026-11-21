using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    [Serializable]
    public class VeilSettings
    {
        [Header("Cloth")]
        [Min(3)] public int nodeCount = 35;
        [Tooltip("The scarf's length (m): round the neck (WrapNodes segments), then 3.2 m back from it in its animation sheet, plus the curls.")]
        public float length = 3.5f;
        [Range(1, 20)] public int constraintIterations = 14;
        [Range(0.9f, 1f)] public float damping = 0.985f;
        public float airDrag = 4.5f;
        public float gravityScale = 0.08f;
        public float depthOffset = 0.12f;
        public float depthWobble = 0.08f;
        [Tooltip("How far (m) in front of the hero a hardened weapon is held.")]
        public float weaponDepth = 0.3f;

        [Header("Depth (3D cloth)")]
        [Tooltip("Pull (1/s²) of the loose cloth back towards its depth just behind the hero: it may swing round the body and out of the picture plane, but its side view stays the scarf's.")]
        public float depthStiffness = 30f;
        [Tooltip("Damping (1/s) of the loose cloth's motion in depth.")]
        public float depthDamping = 3f;
        [Tooltip("A wandering push (m/s²) in depth, growing towards the tail: the cloth lives in three dimensions.")]
        public float depthFlutter = 3f;
        [Tooltip("Furthest (m) the loose cloth may stray in depth from its place behind the hero.")]
        public float maxDepth = 0.7f;
        [Tooltip("Radius (m) of the scarf's wrap round the neck.")]
        public float neckRadius = 0.085f;
        [Tooltip("How many segments of the scarf are wound round the neck (from the throat, round the side towards the camera, to the back).")]
        [Min(0)] public int wrapNodes = 3;
        [Tooltip("How far (m) the cloth stays off the body it falls against.")]
        public float bodySkin = 0.03f;

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
        [Tooltip("Body speed (m/s) above which shedding gets no stronger, so falls and dashes don't throw the scarf around.")]
        public float maxShedSpeed = 8f;

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
        public float buoyancy = 1.4f;
        [Tooltip("The tip slowly curls into a loop.")]
        public float tipCurl = 5f;
        [Tooltip("Magical breeze (m/s) that keeps the scarf floating behind the hero even at rest (key art).")]
        public float drift = 1.4f;
        [Tooltip("Upward part of the breeze (m/s).")]
        public float driftLift = 0.1f;
        [Tooltip("Weightless cloth: its vertical speed relative to the hero is limited to this (m/s), so jumps don't fling it up and landings don't slam it down. 0 = off.")]
        public float maxRelativeFall = 2.5f;

        [Header("Rest shape (the scarf's animation sheet, VeilRestShape)")]
        [Tooltip("Pull (1/s²) of the loose cloth towards the sheet's S-shaped centre line behind the hero.")]
        public float restShapeStiffness = 120f;
        [Tooltip("Damping (1/s) of the cloth's motion relative to the hero while it holds the sheet's shape.")]
        public float restShapeDamping = 7f;

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
    /// hero's body sheds as it moves through the air. It is cloth in three dimensions: wound round the neck, it swings
    /// round the body and out of the picture plane and falls against the body (capsules round its bones), pulled back
    /// towards its place just behind the hero. Weapon forms "harden" the ribbon by
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
        /// <summary>Optional path the cloth is led along (world space) and how firmly (0..1) per node: a weapon melting back.</summary>
        public Vector3[] GuideTargets { get; private set; }
        public float[] GuideWeight { get; private set; }
        public Vector2 Wind { get; set; }

        /// <summary>0 = cloth behind the hero, 1 = weapon in front of the hero, by node hardness.</summary>
        public static float WeaponDepthBlend(float hardness) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.7f, hardness));

        float H(int i) => UseNodeHardness ? Mathf.Clamp01(NodeHardness[i]) : Mathf.Clamp01(Hardness);
        /// <summary>Slow magical drift of the air around the hero (e.g. "behind and slightly up").</summary>
        public Vector2 AmbientFlow { get; set; }
        public Vector3 AnchorVelocity { get; private set; }
        /// <summary>Extra acceleration (m/s²) on the loose cloth, growing towards the tail (e.g. a weapon melting back).</summary>
        public Vector2 Breath { get; set; }
        /// <summary>Which way (-1/+1 in x) the magical breeze blows; 0 = no breeze. Set to minus the hero's facing.</summary>
        public float DriftDirection { get; set; }
        /// <summary>
        /// A whirl of air round a point (the somersault): the loose cloth is carried round <see cref="SwirlCenter"/>
        /// at <see cref="SwirlSpeed"/> (rad/s, + = anticlockwise) and drawn onto a ring of <see cref="SwirlRadius"/>,
        /// so it winds round the body. <see cref="SwirlWeight"/> 0..1 fades it in and out.
        /// </summary>
        public Vector2 SwirlCenter { get; set; }
        public float SwirlSpeed { get; set; }
        public float SwirlRadius { get; set; } = 0.75f;
        public float SwirlWeight { get; set; }
        /// <summary>The span of the veil (fractions) held in the fist, and how firmly (0..1): it sits exactly on the
        /// weapon's shape from the moment of the cast, and a little behind the palm so the fist is drawn over it.</summary>
        public float HeldFrom { get; set; }
        public float HeldTo { get; set; }
        public float HeldWeight { get; set; }

        /// <summary>The set weapon's turn through depth (radians, + far end behind the hero) about
        /// <see cref="WeaponPivot"/>: a blow sweeps through the depth of the scene (VeilWeaponPose.SweepTilt).</summary>
        public float WeaponTilt { get; set; }
        /// <summary>Where the weapon is held (the palm), the point it turns about through depth.</summary>
        public Vector3 WeaponPivot { get; set; }

        /// <summary>Depth (m) off the weapon's plane of a point <paramref name="p"/> of it turned by
        /// <paramref name="tilt"/> about <paramref name="pivot"/>: its distance from the hand in the picture, into depth.</summary>
        public static float SweepDepth(Vector3 p, Vector3 pivot, float tilt)
        {
            if (tilt == 0f) return 0f;
            float dx = p.x - pivot.x, dy = p.y - pivot.y;
            return Mathf.Sqrt(dx * dx + dy * dy) * Mathf.Sin(tilt);
        }
        /// <summary>How far (m) behind the weapon's plane the held part lies, so the fist in front of it covers it.</summary>
        public const float HeldTuck = 0.08f;

        /// <summary>The sharpest bend (degrees) the loose cloth makes at a node before it is straightened.</summary>
        public const float MaxKink = 60f;

        /// <summary>How far (0..1) the bend at <paramref name="b"/> between <paramref name="a"/> and <paramref name="c"/> goes
        /// past MaxKink (0 within it, 1 folded right back on itself).</summary>
        public static float KinkExcess(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 u = b - a, v = c - b;
            float lu = u.magnitude, lv = v.magnitude;
            if (lu < 1e-6f || lv < 1e-6f) return 0f;
            float cos = Vector3.Dot(u, v) / (lu * lv);
            float limit = Mathf.Cos(MaxKink * Mathf.Deg2Rad);
            return cos >= limit ? 0f : (limit - cos) / (limit + 1f);
        }
        /// <summary>
        /// Which way the scarf trails round the body while it is wound in the somersault (+1 anticlockwise,
        /// -1 clockwise; 0 = against <see cref="SwirlSpeed"/>). The body turns one way, the cloth lags the other.
        /// </summary>
        public float WrapTrail { get; set; }
        /// <summary>How far (m) the wrapped scarf's spiral widens from the neck to its end.</summary>
        public const float WrapWiden = 0.25f;
        /// <summary>How fast (1/s) the loose cloth is laid onto the wrap while it is wound.</summary>
        public const float WrapRate = 38f;

        /// <summary>
        /// Where node at <paramref name="along"/> metres from the neck lies when the scarf is wound round the body:
        /// a spiral round <paramref name="centre"/> that starts at the neck and winds on, trailing the turn, widening
        /// by <see cref="WrapWiden"/> to its end.
        /// </summary>
        /// <summary>0..1: how much harder the scarf is pulled back into its shape <paramref name="since"/> s after the
        /// somersault let go (1 at once, gone after 0.5 s; 0 if it hasn't been wound).</summary>
        public static float UnwindBoost(float since) => since >= 0.5f ? 0f : 1f - Mathf.SmoothStep(0f, 1f, since / 0.5f);

        /// <summary>0..1 how rigidly a node of hardness <paramref name="hardness"/> is held on the weapon's shape: not at
        /// all while it is cloth, fully once set.</summary>
        public static float RigidHold(float hardness) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.77f, 0.9f, hardness)); // (the whip, set at 0.75, stays supple)

        public static Vector2 WrapPoint(Vector2 centre, Vector2 neck, float along, float length, float trail)
        {
            Vector2 n0 = neck - centre;
            float r0 = Mathf.Max(n0.magnitude, 0.3f);
            float r = r0 + WrapWiden * Mathf.Clamp01(along / Mathf.Max(length, 1e-3f));
            float angle = Mathf.Atan2(n0.y, n0.x) + Mathf.Sign(trail) * along / (0.5f * (r0 + r));
            return centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }
        /// <summary>
        /// 0..1: how firmly the loose cloth keeps the shape of its animation sheet (VeilRestShape): streaming back
        /// level in an S behind the hero (towards <see cref="DriftDirection"/>). Full at rest, eased off when the hero
        /// runs or somersaults, so the cloth's own physics shows through.
        /// </summary>
        public float RestShapeWeight { get; set; }

        /// <summary>Horizontal reach of the sheet's S per metre of scarf (its waves take up the rest).</summary>
        public const float RestReach = 0.88f;

        /// <summary>
        /// Turning round, the breeze does not flip the whole scarf at once (it crumpled round the head): the turn runs
        /// down it from the neck, the tail last, like a flag swung round. Seconds for the turn to reach the tip.
        /// </summary>
        public const float TurnTravel = 0.45f;
        /// <summary>Seconds one point of the scarf takes to swing over.</summary>
        public const float TurnBlend = 0.3f;

        /// <summary>The breeze's direction (-1..1) at <paramref name="f"/> (0 neck, 1 tip), <paramref name="clock"/> s after it turned from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static float DriftAt(float from, float to, float clock, float f)
            => Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, (clock - f * TurnTravel) / TurnBlend));

        /// <summary>The breeze's direction (-1..1) at <paramref name="f"/> along the scarf right now (see <see cref="TurnTravel"/>).</summary>
        public float DriftAt(float f) => DriftAt(driftFrom, driftTo, turnClock, f);

        /// <summary>
        /// Radians the sheet's S is tilted out of the level (+ = the tail up): it streams against the hero's motion, up
        /// behind him as he falls, instead of keeping level and catching on the ledge he dropped off.
        /// </summary>
        public float TrailTilt { get; set; }

        /// <summary>The sheet's offset (dx along the breeze, dy up) tilted by <paramref name="tilt"/>, for a breeze towards <paramref name="dir"/>.</summary>
        public static Vector2 Tilted(float dx, float dy, float dir, float tilt)
        {
            float t = tilt * (dir < 0f ? -1f : 1f);
            float c = Mathf.Cos(t), sn = Mathf.Sin(t);
            return new Vector2(dx * c - dy * sn, dx * sn + dy * c);
        }

        float driftFrom, driftTo, turnClock = 99f;

        /// <summary>Node index after which the ribbon is not held together (-1 = whole): node i and i + 1 move apart freely.</summary>
        public int DetachedAfter { get; set; } = -1;

        /// <summary>Air velocity of the swirl at <paramref name="p"/> relative to the moving centre.</summary>
        public static Vector2 SwirlVelocity(Vector2 p, Vector2 center, float speed, float radius)
        {
            Vector2 d = p - center;
            float r = d.magnitude;
            if (r < 1e-4f) return Vector2.zero;
            Vector2 dir = d / r;
            var tangent = new Vector2(-dir.y, dir.x);
            // Round the ring at its speed (not faster further out, which would fling the tail off) and pulled onto it.
            return tangent * (speed * radius) - dir * ((r - radius) * 22f);
        }
        public IReadOnlyList<Vortex> Vortices => vortices;
        public int ShedCount { get; private set; }

        /// <summary>A part of the hero's body the cloth falls against: a capsule from <see cref="a"/> to <see cref="b"/>.</summary>
        public struct BodyCapsule
        {
            public Vector3 a, b;
            public float radius;
            public BodyCapsule(Vector3 a, Vector3 b, float radius) { this.a = a; this.b = b; this.radius = radius; }
        }

        /// <summary>The hero's body as capsules (filled each frame by the owner): the loose cloth stays outside them.</summary>
        public readonly List<BodyCapsule> Body = new List<BodyCapsule>();

        /// <summary>Up along the neck (the wrap winds round it).</summary>
        public Vector3 NeckUp { get; set; } = Vector3.up;
        /// <summary>Where the throat faces (the hero's facing): the wrap starts there.</summary>
        public Vector3 NeckForward { get; set; } = Vector3.right;
        /// <summary>Which way is towards the camera (the wrap goes round that side of the neck).</summary>
        public Vector3 TowardsCamera { get; set; } = Vector3.back;

        /// <summary>
        /// Where node <paramref name="i"/> (1..WrapNodes) of the scarf lies wound round the neck: on a ring of
        /// <paramref name="radius"/> about the neck at <paramref name="centre"/>, from the throat (node 1) round the side
        /// towards the camera, a segment's length on from the one before.
        /// </summary>
        public static Vector3 WrapRingPoint(Vector3 centre, Vector3 up, Vector3 forward, Vector3 towardsCamera, float radius, float segment, int i)
        {
            Vector3 f = Vector3.ProjectOnPlane(forward, up);
            if (f.sqrMagnitude < 1e-6f) f = Vector3.ProjectOnPlane(Vector3.right, up);
            f.Normalize();
            Vector3 side = Vector3.ProjectOnPlane(towardsCamera, up);
            side = Vector3.ProjectOnPlane(side, f);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(up, f);
            side.Normalize();
            float angle = (i - 1) * segment / Mathf.Max(0.01f, radius);
            return centre + (f * Mathf.Cos(angle) + side * Mathf.Sin(angle)) * radius;
        }

        /// <summary>0..1 how firmly node <paramref name="i"/> is held on the neck's wrap (the loose cloth's part of it).</summary>
        float WrapHold(int i) => i >= 1 && i <= Settings.wrapNodes ? (1f - H(i)) * (1f - GuideWeight[i]) : 0f;   // (a weapon melting back is led off it)

        /// <summary>The node lies wound round the neck (1..WrapNodes, while cloth).</summary>
        public bool IsWrapped(int i) => WrapHold(i) > 0.5f;

        /// <summary>How free node <paramref name="i"/> is to be moved by the constraints (0: held on the neck or the anchor).</summary>
        float Free(int i) => i == 0 ? 0f : 1f - WrapHold(i);

        readonly Vector3[] previous;
        readonly List<Vortex> vortices = new List<Vortex>();
        float shedPhase;
        int nextSign = 1;
        float time;
        float sinceWrap = 10f;
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
            GuideTargets = new Vector3[Count];
            GuideWeight = new float[Count];
            wallContact = new float[Count];
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
            if (wallContact != null) Array.Clear(wallContact, 0, wallContact.Length);
            shedPhase = 0f;
            lastAnchor = anchor;
            AnchorVelocity = Vector3.zero;
            driftFrom = driftTo = DriftDirection;
            turnClock = 99f;
        }

        /// <summary>The scarf's length (m) wound round the neck.</summary>
        public float WrapLength => Mathf.Min(Settings.wrapNodes, Count - 2) * FlowRestLength;

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
            if (DriftDirection != driftTo)
            {
                // a turn while the last one is still running starts from where the middle of the scarf is
                driftFrom = driftTo == 0f ? DriftDirection : DriftAt(0.5f);
                driftTo = DriftDirection;
                turnClock = driftFrom == driftTo ? 99f : 0f;
            }
            turnClock += h;
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
            float speed = Mathf.Min(rel.magnitude, s.maxShedSpeed); // a fall must not whip up a storm
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
            // Just after the somersault the scarf unwinds at once: for a moment it is pulled back into its shape
            // much harder and may move up and down freely (it lies wound round the legs as the hero lands).
            sinceWrap = SwirlWeight > 0.01f ? 0f : sinceWrap + h;
            float unwind = UnwindBoost(sinceWrap);
            var bodyVel = new Vector2(anchorVel.x, anchorVel.y);
            float bodySpeed = bodyVel.magnitude;
            float wavePhase = s.undulationSpeed * time;
            // Cloth beyond a point that touches a wall no longer streams into it: it hangs and drapes down the wall.
            float blocked = 0f;
            for (int i = 1; i < Count; i++)
            {
                wallContact[i] = Mathf.Max(0f, wallContact[i] - h * 2.5f);
                blocked = Mathf.Max(blocked, wallContact[i]);
                Vector3 p = Positions[i];
                Vector3 vel = (p - previous[i]) / h;
                float f = (float)i / (Count - 1);
                float loose = (1f - H(i)) * (1f - 0.85f * blocked);

                // Feel the run wind only up to a point: the scarf trails, it doesn't become a taut sheet.
                Vector2 airV = SampleAirVelocity(p);
                Vector2 relAir = airV - bodyVel;
                if (relAir.sqrMagnitude > s.maxRelativeWind * s.maxRelativeWind) relAir = relAir.normalized * s.maxRelativeWind;
                airV = Vector2.Lerp(airV, bodyVel + relAir, loose);
                // The veil floats on its own breeze: it streams behind the hero even standing still.
                float driftHere = DriftAt(f);
                if (DriftDirection != 0f)
                    airV += new Vector2(driftHere * s.drift, s.driftLift) * (loose * Mathf.SmoothStep(0f, 1f, f * 4f));
                if (SwirlWeight > 0f)
                    airV = Vector2.Lerp(airV, bodyVel + SwirlVelocity(p, SwirlCenter, SwirlSpeed, SwirlRadius), loose * SwirlWeight);
                // In the whirl the cloth is gripped by the air far harder: it winds round within the turn.
                float drag = s.airDrag * (1f + 5f * SwirlWeight * loose);
                Vector3 acc = new Vector3((airV.x - vel.x) * drag, (airV.y - vel.y) * drag, 0f) + g;

                float shaped = RestShapeWeight * loose * Mathf.Abs(driftHere);
                if (shaped > 0.001f)
                {
                    // The sheet's shape: its centre line d metres back from the neck. Its S takes up length: it reaches
                    // back ~RestReach of d (a straighter target would pull the S flat). (Measured from the end of the
                    // wrap round the neck: the sheet is the part that streams off it.)
                    float dAlong = Mathf.Max(0f, f * s.length - WrapLength) * LengthScale;
                    Vector2 off = Tilted(driftHere * dAlong * RestReach, VeilRestShape.Height(dAlong, time), driftHere, TrailTilt);
                    var target = new Vector3(a.x + off.x, a.y + off.y, p.z);
                    Vector3 pull = (target - p) * (s.restShapeStiffness * (1f + 3f * unwind));
                    Vector3 still = new Vector3(anchorVel.x - vel.x, anchorVel.y - vel.y, 0f) * (s.restShapeDamping * (1f + 1.5f * unwind));
                    acc += new Vector3(pull.x + still.x, pull.y + still.y, 0f) * shaped;
                }

                if (loose > 0.001f)
                {
                    // Its own life: a serpentine wave running down the scarf (stronger when moving),
                    // a tail that rises like smoke and a tip that curls over (quieter while it holds the sheet's shape).
                    float own = 1f - 0.9f * shaped;
                    Vector3 tan = Positions[Mathf.Min(i + 1, Count - 1)] - Positions[i - 1];
                    tan.z = 0f;
                    if (tan.sqrMagnitude > 1e-8f)
                    {
                        tan.Normalize();
                        var normal = new Vector3(-tan.y, tan.x, 0f);
                        float amp = (s.undulation + s.undulationPerSpeed * bodySpeed) * f;
                        float wave = Mathf.Sin(s.undulationWaves * Mathf.PI * 2f * f - wavePhase);
                        acc += normal * (amp * wave + s.tipCurl * Mathf.SmoothStep(0f, 1f, (f - 0.7f) / 0.3f)) * loose * own;
                    }
                    acc.y += s.buoyancy * f * f * loose * own;
                    acc += new Vector3(Breath.x, Breath.y, 0f) * (f * loose);
                }
                // Depth: the loose cloth is pulled back towards its place just behind the hero (with a little wander
                // of its own), so it can swing round the body and out of the picture plane yet keeps the scarf's side view.
                if (loose > 0.001f)
                {
                    float zRest = a.z + s.depthOffset + Mathf.Sin(time * 2.3f + i * 0.55f) * s.depthWobble * f;
                    acc.z += ((zRest - p.z) * s.depthStiffness - vel.z * s.depthDamping
                              + Mathf.Sin(time * 1.7f + i * 0.9f) * Mathf.Cos(time * 0.6f - i * 0.3f) * s.depthFlutter * f) * loose;
                }
                Vector3 next = p + (p - previous[i]) * s.damping + acc * (h * h);
                // The veil is weightless: it rides along with the hero's jumps and falls instead of carrying
                // their momentum — no flinging up over the head at the apex, no slapping onto the floor on landing.
                if (s.maxRelativeFall > 0f && loose > 0.001f)
                {
                    Vector3 relV = (next - p) / h - anchorVel;
                    // A breath (weapon melting back) may lift it faster than the normal limit.
                    float limit = relV.y > 0f ? s.maxRelativeFall + Mathf.Max(0f, Breath.y) * 0.25f : s.maxRelativeFall;
                    limit += (SwirlWeight + unwind) * 40f; // winding round the body (and off it) moves up and down freely
                    if (Mathf.Abs(relV.y) > limit)
                    {
                        relV.y = Mathf.Lerp(relV.y, Mathf.Sign(relV.y) * limit, loose);
                        next = p + (anchorVel + relV) * h;
                    }
                }
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

            // 5a) Round the neck: the first segments of the cloth are wound round it (from the throat round the side
            // towards the camera, to the back), held there; the rest streams off the wrap.
            LayWrap(a);

            // 5b) The somersault: the loose cloth is laid round the body as a spiral from the neck, trailing the
            // turn, so it wraps the hero as the body turns (rather than tangling in the whirl beside him).
            if (SwirlWeight > 0.001f)
            {
                float trail = WrapTrail != 0f ? WrapTrail : -Mathf.Sign(SwirlSpeed);
                if (trail != 0f)
                {
                    float rate = 1f - Mathf.Exp(-WrapRate * SwirlWeight * h);
                    for (int i = 1; i < Count; i++)
                    {
                        float loose = (1f - H(i)) * Free(i);
                        if (loose <= 0.001f) continue;
                        Vector2 w = WrapPoint(SwirlCenter, a, Mathf.Max(0f, (float)i / (Count - 1) * s.length - WrapLength) * LengthScale, s.length * LengthScale, trail);
                        var target = new Vector3(w.x, w.y, Positions[i].z);
                        Positions[i] = Vector3.Lerp(Positions[i], target, rate * loose);
                    }
                }
            }

            // 6) Distance constraints (rest length morphs from cloth to weapon shape, node by node).
            for (int it = 0; it < s.constraintIterations; it++)
            {
                for (int i = 0; i < Count - 1; i++)
                {
                    if (i == DetachedAfter) continue; // a weapon that floats free of the hand (the shield's ring)
                    float rest = Mathf.Lerp(FlowRestLength * LengthScale, TargetRest[i], 0.5f * (H(i) + H(i + 1)));
                    // (in three dimensions: the cloth swings round the body and out of the picture plane; set weapons
                    // keep their depth in step 7, so their shapes still measure in the plane)
                    Vector3 d = Positions[i + 1] - Positions[i];
                    float hard = 0.5f * (WeaponDepthBlend(H(i)) + WeaponDepthBlend(H(i + 1)));
                    d.z *= 1f - hard;
                    float len = d.magnitude;
                    if (len < 1e-6f) continue;
                    // the anchor and the wrap round the neck are held: the free end of a segment takes it all
                    float wa = Free(i), wb = Free(i + 1);
                    float sum = wa + wb;
                    if (sum < 1e-4f) continue;
                    Vector3 corr = d * ((len - rest) / len);
                    Positions[i] += corr * (wa / sum);
                    Positions[i + 1] -= corr * (wb / sum);
                }
                // Cloth doesn't fold sharply: where the loose cloth bends at a node by more than MaxKink, the node is
                // drawn towards the middle of its neighbours (gentle curves - its S - are left alone). Without it,
                // turning round crumpled it node by node into a zigzag, drawn as broken pieces with gaps between them.
                for (int i = 2; i < Count - 1; i++)
                {
                    float loose = (1f - H(i)) * Free(i) * Free(i - 1);
                    if (loose <= 0f) continue;
                    float kink = KinkExcess(Positions[i - 1], Positions[i], Positions[i + 1]);
                    if (kink <= 0f) continue;
                    Positions[i] += ((Positions[i - 1] + Positions[i + 1]) * 0.5f - Positions[i]) * (0.8f * kink * loose);
                }
            }

            // 6b) A set weapon is a solid thing in the hand: once a node has hardened it sits exactly on the weapon's
            // shape (the blade goes with the fist through the fastest swing instead of trailing behind it); only
            // the half-set cloth in between stays soft. Its previous position moves along, so no speed is invented.
            for (int i = 1; i < Count; i++)
            {
                float rigid = RigidHold(H(i));
                float fi = (float)i / (Count - 1);
                if (HeldWeight > 0f && fi >= HeldFrom && fi <= HeldTo) rigid = Mathf.Max(rigid, HeldWeight);
                if (rigid <= 0f) continue;
                Vector3 want = Targets[i] + targetOffset;
                want.z = Positions[i].z;
                Vector3 shift = (want - Positions[i]) * rigid;
                Positions[i] += shift;
                previous[i] += shift;
            }

            // 6c) A weapon melting back into cloth is led along a path (VeilTransform.MeltProgress) instead of
            // collapsing on its own; the previous position moves along, so it neither springs nor bunches up.
            for (int i = 1; i < Count; i++)
            {
                float guide = GuideWeight[i];
                if (guide <= 0f) continue;
                Vector3 want = GuideTargets[i];
                want.z = Positions[i].z;
                Vector3 shift = (want - Positions[i]) * guide;
                Positions[i] += shift;
                previous[i] += shift;
            }

            // 6d) The loose cloth can't pass through walls: it drapes against them (see CollideWithObstacles), nor
            // through the hero: it falls against the body and slides round it (see CollideWithBody).
            CollideWithBody();
            CollideWithObstacles();
            LayWrap(a);   // (the wrap held where it is after all the rest)

            // 7) Depth: the loose cloth moves in it freely (its spring above keeps it behind the hero, within
            // maxDepth); a weapon in the hand is held in front of the body, so the torso never hides it.
            for (int i = 1; i < Count; i++)
            {
                if (IsWrapped(i)) continue;
                float t = (float)i / (Count - 1);
                float weapon = WeaponDepthBlend(H(i));
                float z = Positions[i].z;
                float zRest = a.z + s.depthOffset;
                z = Mathf.Clamp(z, zRest - s.maxDepth, zRest + s.maxDepth);
                z = Mathf.Lerp(z, a.z - s.weaponDepth + SweepDepth(Positions[i], WeaponPivot, WeaponTilt), weapon);
                if (HeldWeight > 0f && t >= HeldFrom && t <= HeldTo) z = Mathf.Lerp(z, a.z - s.weaponDepth + HeldTuck, HeldWeight);
                // A weapon melting back passes over the body: drawn in front, so the torso doesn't swallow it.
                z = Mathf.Lerp(z, a.z - s.weaponDepth, GuideWeight[i]);
                float held = Mathf.Max(weapon, Mathf.Max(GuideWeight[i], HeldWeight > 0f && t >= HeldFrom && t <= HeldTo ? HeldWeight : 0f));
                previous[i].z = Mathf.Lerp(previous[i].z + (z - Positions[i].z), z, held);   // (held: no speed in depth)
                Positions[i].z = z;
            }
        }

        /// <summary>
        /// Solid level geometry near the scarf, as rectangles in the picture plane (min.xy, max.xy). Filled each
        /// frame by the owner (VeilController gathers the walls and floors around the hero).
        /// </summary>
        public readonly List<Rect> Obstacles = new List<Rect>();

        /// <summary>
        /// How long the loose scarf is, as a share of its full length (its strength: casting and a torn scarf
        /// shorten it, it grows back). The cloth itself is shorter - its curls stay at its end - rather than its tail
        /// fading out, which read as a scarf cut off.
        /// </summary>
        public float LengthScale = 1f;

        /// <summary>
        /// The top of the floor under (x, y): the highest obstacle top below the point, or one it has sunk into by
        /// up to <paramref name="sunk"/> m. -infinity when there is none.
        /// </summary>
        public float FloorUnder(float x, float y, float sunk = 0.6f)
        {
            float best = float.NegativeInfinity;
            for (int o = 0; o < Obstacles.Count; o++)
            {
                var r = Obstacles[o];
                if (x < r.xMin || x > r.xMax || r.yMax > y + sunk) continue;
                if (r.yMax > best) best = r.yMax;
            }
            return best;
        }
        /// <summary>Lays the loose cloth's first segments round the neck (held there, no speed of their own).</summary>
        void LayWrap(Vector3 a)
        {
            var s = Settings;
            int n = Mathf.Min(s.wrapNodes, Count - 2);
            for (int i = 1; i <= n; i++)
            {
                float hold = WrapHold(i);
                if (hold <= 0f) continue;
                Vector3 ring = WrapRingPoint(a, NeckUp, NeckForward, TowardsCamera, s.neckRadius, FlowRestLength, i);
                Vector3 shift = (ring - Positions[i]) * hold;
                Positions[i] += shift;
                previous[i] += shift;
            }
        }

        /// <summary>
        /// Keeps the loose cloth out of the hero's body (<see cref="Body"/>): a node inside a capsule is put on its
        /// surface (plus <see cref="VeilSettings.bodySkin"/>) straight out from its axis; it loses its speed into the
        /// body and keeps most of its slide round it. The wrap round the neck and set weapons are left alone.
        /// </summary>
        void CollideWithBody()
        {
            if (Body.Count == 0) return;
            float skin = Settings.bodySkin;
            for (int i = 1; i < Count; i++)
            {
                float loose = (1f - Mathf.Max(RigidHold(H(i)), GuideWeight[i])) * Free(i);
                if (loose <= 0.01f) continue;
                for (int c = 0; c < Body.Count; c++)
                {
                    var cap = Body[c];
                    Vector3 p = Positions[i];
                    Vector3 axis = ClosestOnSegment(cap.a, cap.b, p);
                    Vector3 off = p - axis;
                    float dist = off.magnitude;
                    float reach = cap.radius + skin;
                    if (dist >= reach) continue;
                    Vector3 n = dist > 1e-5f ? off / dist : TowardsCamera.normalized;
                    Vector3 push = n * (reach - dist) * loose;
                    Positions[i] += push;
                    Vector3 v = Positions[i] - previous[i];
                    float vn = Vector3.Dot(v, n);
                    Vector3 slide = (v - n * vn) * 0.9f + n * Mathf.Max(0f, vn);
                    previous[i] = Positions[i] - Vector3.Lerp(v, slide, loose);
                }
            }
        }

        /// <summary>The point of the segment a-b nearest to p.</summary>
        public static Vector3 ClosestOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 1e-10f) return a;
            return a + ab * Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
        }

        float[] wallContact;
        /// <summary>0..1 how recently node <paramref name="i"/> was stopped by a wall.</summary>
        public float WallContact(int i) => wallContact[i];

        /// <summary>How far off a wall the cloth stays (m), so its width doesn't sink into the surface.</summary>
        public const float WallSkin = 0.06f;

        /// <summary>
        /// Keeps the loose cloth out of the obstacles. Walked from the neck to the tip, a node that ended up in a
        /// wall is put back where its segment entered it (the side the scarf came from), not at the nearest face —
        /// a node deep in a thick pillar would otherwise pop out on its far side, as if the scarf ran through it.
        /// It loses its speed into the wall and slides along it with a little friction. Set weapons are left alone
        /// (they are held in the hand).
        /// </summary>
        void CollideWithObstacles()
        {
            if (Obstacles.Count == 0) return;
            for (int i = 1; i < Count; i++)
            {
                float loose = 1f - Mathf.Max(RigidHold(H(i)), GuideWeight[i]);
                if (loose <= 0.01f) continue;
                Vector2 p = Positions[i];
                Vector2 from = Positions[i - 1];
                for (int o = 0; o < Obstacles.Count; o++)
                {
                    Rect r = Obstacles[o];
                    Rect grown = new Rect(r.xMin - WallSkin, r.yMin - WallSkin, r.width + 2f * WallSkin, r.height + 2f * WallSkin);
                    if (!grown.Contains(p)) continue;
                    Vector2 outP = grown.Contains(from) ? NearestOutside(grown, p) : EntryPoint(grown, from, p);
                    Vector2 push = outP - p;
                    Vector3 shift = new Vector3(push.x, push.y, 0f) * loose;
                    Positions[i] += shift;
                    wallContact[i] = 1f;
                    // velocity: drop the part into the wall, keep most of the slide along it
                    Vector2 v = (Vector2)(Positions[i] - previous[i]);
                    Vector2 n = push.sqrMagnitude > 1e-10f ? push.normalized : Vector2.zero;
                    float vn = Vector2.Dot(v, n);
                    Vector2 slide = (v - n * vn) * 0.85f + n * Mathf.Max(0f, vn);
                    Vector2 newPrev = (Vector2)Positions[i] - Vector2.Lerp(v, slide, loose);
                    previous[i] = new Vector3(newPrev.x, newPrev.y, previous[i].z);
                    p = Positions[i];
                }
            }
        }

        /// <summary>Where the segment from <paramref name="from"/> (outside) to <paramref name="to"/> (inside) enters the rectangle.</summary>
        public static Vector2 EntryPoint(Rect r, Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            float tEnter = 0f;
            // slab test: the latest entry over both axes
            for (int axis = 0; axis < 2; axis++)
            {
                float o = from[axis], dir = d[axis];
                float min = axis == 0 ? r.xMin : r.yMin, max = axis == 0 ? r.xMax : r.yMax;
                if (Mathf.Abs(dir) < 1e-8f) continue;
                float t0 = (min - o) / dir, t1 = (max - o) / dir;
                tEnter = Mathf.Max(tEnter, Mathf.Min(t0, t1));
            }
            tEnter = Mathf.Clamp01(tEnter);
            Vector2 e = from + d * tEnter;
            // just outside the face it crossed
            Vector2 c = r.center;
            float dx = Mathf.Min(Mathf.Abs(e.x - r.xMin), Mathf.Abs(e.x - r.xMax));
            float dy = Mathf.Min(Mathf.Abs(e.y - r.yMin), Mathf.Abs(e.y - r.yMax));
            if (dx <= dy) e.x = e.x < c.x ? r.xMin - 1e-3f : r.xMax + 1e-3f;
            else e.y = e.y < c.y ? r.yMin - 1e-3f : r.yMax + 1e-3f;
            return e;
        }

        /// <summary>The closest point just outside the rectangle.</summary>
        /// <summary>
        /// <paramref name="p"/> put out of every obstacle (grown by <paramref name="margin"/>) the shortest way, in the
        /// picture plane: for what is drawn or simulated on top of the cloth (its strands, its whorls), which would
        /// otherwise pass into a wall or a low ceiling and be cut off by it.
        /// </summary>
        public Vector3 OutOfObstacles(Vector3 p, float margin)
        {
            for (int o = 0; o < Obstacles.Count; o++)
            {
                Rect r = Obstacles[o];
                Rect grown = new Rect(r.xMin - margin, r.yMin - margin, r.width + 2f * margin, r.height + 2f * margin);
                Vector2 q = new Vector2(p.x, p.y);
                if (!grown.Contains(q)) continue;
                q = NearestOutside(grown, q);
                p.x = q.x; p.y = q.y;
            }
            return p;
        }

        public static Vector2 NearestOutside(Rect r, Vector2 p)
        {
            float l = p.x - r.xMin, ri = r.xMax - p.x, b = p.y - r.yMin, t = r.yMax - p.y;
            float m = Mathf.Min(Mathf.Min(l, ri), Mathf.Min(b, t));
            if (m == l) return new Vector2(r.xMin - 1e-3f, p.y);
            if (m == ri) return new Vector2(r.xMax + 1e-3f, p.y);
            if (m == b) return new Vector2(p.x, r.yMin - 1e-3f);
            return new Vector2(p.x, r.yMax + 1e-3f);
        }

        void Shed(Vector3 anchor, Vector2 rel, float speed)
        {
            var s = Settings;
            Vector2 dir = rel.sqrMagnitude > 1e-6f ? rel.normalized : Vector2.down;
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
