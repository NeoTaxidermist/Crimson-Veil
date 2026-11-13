using System;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Owns the <see cref="VeilSimulation"/>, switches forms (toggle) and drives attacks.
    /// Transformations are choreographed: hardening runs neck → tip as a wave, shapes morph with a
    /// winding swirl, and turning around squashes the weapon through its profile instead of snapping.
    /// Runs after the player has moved so the anchor is up to date.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class VeilController : MonoBehaviour
    {
        [SerializeField] VeilSettings settings = new VeilSettings();
        [SerializeField] Transform anchor;
        [SerializeField] PlayerController player;
        [Tooltip("Smoothing time of hardening / softening.")]
        [SerializeField] float transformTime = 0.2f;
        [Tooltip("Seconds for one weapon shape to morph into another.")]
        [SerializeField] float morphTime = 0.3f;
        [SerializeField] float turnSpeed = 7f;
        [SerializeField] float idleDriftSpeed = 0.75f;
        [SerializeField] Vector2 wind = new Vector2(-0.3f, 0.05f);
        [SerializeField] Material arrowMaterial;
        [SerializeField] float arrowSpeed = 26f;
        [Tooltip("The hero's hand bone. When set, weapons grow out of the real hand instead of a fixed point.")]
        [SerializeField] Transform hand;
        [SerializeField] float handFollowSharpness = 30f;
        [Tooltip("Forearm angle (degrees, facing space) at which the weapon shapes were designed.")]
        [SerializeField] float canonicalForearmAngle = -15f;
        [Tooltip("Extra blade turn while running at full speed (degrees) — carries the blade low behind.")]
        [SerializeField] float carryAngle = -128f;
        float forearmTurn;
        float carry;
        bool morphFromCloth;

        public VeilSimulation Simulation { get; private set; }
        /// <summary>Current swing of the weapon in degrees (+ = up) — the arm animation follows it.</summary>
        public float CurrentSwing { get; private set; }
        public Transform Hand { get => hand; set => hand = value; }
        Vector2 handDelta;
        public VeilForm Form { get; private set; }
        /// <summary>Shape used for targets; stays on the last weapon while the veil softens back.</summary>
        public VeilForm ShapeForm { get; private set; }
        /// <summary>Overall hardness (0..1) — per-node values live in the simulation.</summary>
        public float Hardness { get; private set; }
        /// <summary>0..1 progress of the hardening wave.</summary>
        public float TransformProgress { get; private set; }
        public float HandFraction { get; private set; }
        public bool IsAttacking => attackTime >= 0f;
        /// <summary>Normalised progress of the current attack (0..1), or -1 when not attacking.</summary>
        public float AttackProgress { get; private set; } = -1f;
        /// <summary>Hero's life force (0..1). The veil thins and dims as it drops — no HUD needed.</summary>
        public float Vitality { get; set; } = 1f;
        public int Facing => player != null && player.Motor != null ? player.Motor.Facing : 1;
        public VeilSettings Settings => settings;

        /// <summary>Where the veil is tied (the neck). Setting it re-seats the cloth.</summary>
        public Transform Anchor
        {
            get => anchor;
            set
            {
                anchor = value != null ? value : transform;
                Simulation?.Reset(anchor.position);
            }
        }

        public event Action<VeilForm> FormChanged;
        public event Action<VeilForm> Attacked;

        Vector2[] shape, fromShape, current;
        float morph = 1f;
        float progressVel;
        float facingScale = 1f;
        float attackTime = -1f;
        bool arrowFired;
        bool aimUp;
        float time;
        IPlayerInput input;

        void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (anchor == null) anchor = transform;
            Simulation = new VeilSimulation(settings, anchor.position) { UseNodeHardness = true };
            shape = new Vector2[Simulation.Count];
            fromShape = new Vector2[Simulation.Count];
            current = new Vector2[Simulation.Count];
            facingScale = Facing;
        }

        void Start()
        {
            input = player != null && player.InputSource != null ? player.InputSource : GetComponent<IPlayerInput>();
        }

        public void SetForm(VeilForm form)
        {
            if (form == Form) return;
            Form = form;
            if (form != VeilForm.Flow)
            {
                // Morph from what the veil looks like right now: the previous weapon, or the loose cloth.
                morphFromCloth = !(TransformProgress > 0.05f && ShapeForm != VeilForm.Flow);
                if (!morphFromCloth) Array.Copy(current, fromShape, current.Length);
                else CaptureClothAsShape(fromShape);
                ShapeForm = form;
                HandFraction = VeilFormLibrary.BuildShape(form, shape);
                morph = 0f;
            }
            attackTime = -1f;
            FormChanged?.Invoke(form);
        }

        public void ToggleForm(VeilForm form) => SetForm(Form == form ? VeilForm.Flow : form);

        public bool Attack()
        {
            if (Form == VeilForm.Flow || Hardness < 0.5f * VeilFormLibrary.Hardness(Form)) return false;
            attackTime = 0f;
            arrowFired = false;
            aimUp = input != null && input.AimY > 0.5f;
            Attacked?.Invoke(Form);
            return true;
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            time += dt;

            if (input != null)
            {
                var request = input.FormRequested;
                if (request.HasValue) ToggleForm(request.Value);
                if (input.AttackPressed) Attack();
            }

            float goal = Form == VeilForm.Flow ? 0f : 1f;
            TransformProgress = Mathf.SmoothDamp(TransformProgress, goal, ref progressVel, transformTime * 0.4f, Mathf.Infinity, dt);
            if (Mathf.Abs(TransformProgress - goal) < 0.002f) { TransformProgress = goal; progressVel = 0f; }
            Hardness = VeilFormLibrary.Hardness(ShapeForm) * TransformProgress;
            morph = Mathf.Min(1f, morph + dt / Mathf.Max(0.01f, morphTime));

            int facing = Facing;
            facingScale = Mathf.MoveTowards(facingScale, facing, dt * turnSpeed);
            Vector3 a = anchor.position;

            Simulation.Wind = wind;
            Simulation.AmbientFlow = new Vector2(-facing, 0.45f).normalized * idleDriftSpeed;

            UpdateNodeHardness();
            if (TransformProgress > 0f && ShapeForm != VeilForm.Flow) UpdateTargets(a, dt);
            if (attackTime < 0f) { AttackProgress = -1f; CurrentSwing = 0f; }

            Simulation.Hardness = Hardness;
            Simulation.Step(dt, a);
        }

        void UpdateNodeHardness()
        {
            var nh = Simulation.NodeHardness;
            int n = Simulation.Count;
            float h = VeilFormLibrary.Hardness(ShapeForm);
            for (int i = 0; i < n; i++)
            {
                float f = (float)i / (n - 1);
                nh[i] = h * VeilTransform.Wave(TransformProgress, f) * VeilTransform.TipSoftness(f);
            }
        }

        void UpdateTargets(Vector3 a, float dt)
        {
            float t = -1f;
            if (attackTime >= 0f)
            {
                attackTime += dt;
                t = attackTime / Mathf.Max(0.01f, VeilFormLibrary.AttackDuration(ShapeForm));
                if (ShapeForm == VeilForm.Bow && !arrowFired && t >= 0.3f)
                {
                    arrowFired = true;
                    FireArrow(a, Facing);
                }
                if (t >= 1f)
                {
                    attackTime = -1f;
                    t = -1f;
                }
                AttackProgress = t;
            }
            CurrentSwing = VeilWeaponPose.SwingAngle(ShapeForm, t);

            // Where is the real hand, in the shape's local space? Grow the weapon from there.
            if (hand != null)
            {
                float s = Mathf.Abs(facingScale) < 0.3f ? Facing : facingScale;
                var lh = new Vector2((hand.position.x - a.x) / s, hand.position.y - a.y);
                var wanted = lh - VeilFormLibrary.Hand;
                if (wanted.sqrMagnitude > 1f) wanted = wanted.normalized; // guard against a bad first frame
                handDelta = Vector2.Lerp(handDelta, wanted, 1f - Mathf.Exp(-handFollowSharpness * dt));

                // The weapon is an extension of the forearm: it turns with it (swings, stance, running).
                var forearm = hand.parent;
                if (forearm != null)
                {
                    Vector3 d = hand.position - forearm.position;
                    float angle = Mathf.Atan2(d.y, d.x / s) * Mathf.Rad2Deg;
                    float wantedTurn = Mathf.DeltaAngle(canonicalForearmAngle, angle) * VeilWeaponPose.ForearmFollow(ShapeForm);
                    // Running: the blade is carried low and trailing behind (like the key art), not pointed ahead.
                    float run = player != null && player.Motor != null
                        ? Mathf.Clamp01(Mathf.Abs(player.Velocity.x) / Mathf.Max(0.1f, player.Settings.maxRunSpeed)) : 0f;
                    carry = Mathf.Lerp(carry, IsAttacking ? 0f : run, 1f - Mathf.Exp(-6f * dt));
                    wantedTurn += carry * carryAngle * VeilWeaponPose.CarryFactor(ShapeForm);
                    forearmTurn = Mathf.LerpAngle(forearmTurn, wantedTurn, 1f - Mathf.Exp(-handFollowSharpness * dt));
                }
            }
            bool armDriven = hand != null && hand.parent != null;
            Vector2 grip = VeilFormLibrary.Hand + handDelta;

            var targets = Simulation.Targets;
            int n = Simulation.Count;
            for (int i = 0; i < n; i++)
            {
                float f = (float)i / (n - 1);
                bool weapon = f >= HandFraction;
                // Pose around the canonical grip, move onto the real hand, turn with the forearm.
                Vector2 rest = VeilWeaponPose.Idle(ShapeForm, shape[i], weapon, time);
                rest = VeilWeaponPose.Apply(ShapeForm, rest, weapon, t, armDriven);
                rest += weapon ? handDelta : handDelta * (HandFraction > 0f ? f / HandFraction : 1f);
                if (weapon && armDriven) rest = VeilWeaponPose.Rotate(rest, grip, forearmTurn);

                Vector2 l;
                if (morph < 1f)
                {
                    float m = VeilTransform.MorphProgress(morph, f);
                    // From loose cloth the blade *grows* out of the palm, overshooting a touch before it settles.
                    if (weapon && morphFromCloth)
                        rest = grip + (rest - grip) * Mathf.Lerp(0.05f, 1f, MovementMath.EaseOutBack(m, 1.7f));
                    l = VeilTransform.MorphNode(fromShape[i], rest, m, f, time, morphFromCloth ? 0.22f : 0.14f);
                }
                else l = rest;
                current[i] = l;
                targets[i] = a + new Vector3(l.x * facingScale, l.y, 0f);
            }
            Simulation.RecomputeTargetRest();
        }

        /// <summary>Current cloth positions in the local (facing-normalised) shape space.</summary>
        void CaptureClothAsShape(Vector2[] into)
        {
            var p = Simulation.Positions;
            Vector3 a = anchor.position;
            float s = Mathf.Abs(facingScale) < 0.2f ? Facing : facingScale;
            for (int i = 0; i < into.Length; i++)
                into[i] = new Vector2((p[i].x - a.x) / s, p[i].y - a.y);
        }

        void FireArrow(Vector3 a, int facing)
        {
            var hand = VeilFormLibrary.Hand;
            var pos = a + new Vector3((hand.x + 0.35f) * facing, hand.y, 0f);
            // Hold up while attacking to loose the arrow at 50° — needed to reach high ropes.
            Vector3 dir = aimUp ? new Vector3(facing * 0.64f, 0.77f, 0f) : new Vector3(facing, 0.06f, 0f);
            VeilArrow.Spawn(pos, dir.normalized * arrowSpeed, arrowMaterial, gameObject);
        }

        void OnDrawGizmosSelected()
        {
            if (Simulation == null) return;
            foreach (var v in Simulation.Vortices)
            {
                Gizmos.color = v.circulation > 0f ? new Color(1f, 0.2f, 0.2f, v.Fade) : new Color(0.2f, 0.6f, 1f, v.Fade);
                Gizmos.DrawWireSphere(new Vector3(v.position.x, v.position.y, anchor.position.z), v.CoreRadius);
            }
        }
    }
}
