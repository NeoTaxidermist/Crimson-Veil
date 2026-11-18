using System;
using System.Collections.Generic;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Owns the <see cref="VeilSimulation"/>, switches forms (toggle) and drives attacks.
    /// Transformations are choreographed: hardening runs neck → tip as a wave, shapes morph with a
    /// winding swirl, and turning around squashes the weapon through its profile instead of snapping.
    /// Runs in LateUpdate after the hero is posed (<c>HeroAnimator</c>, order 0), so a weapon grows out of
    /// the hand where it is drawn this frame — never a frame behind in a fast swing.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class VeilController : MonoBehaviour
    {
        [SerializeField] VeilSettings settings = new VeilSettings();
        [SerializeField] Transform anchor;
        [SerializeField] PlayerController player;
        [Tooltip("Smoothing time of hardening / softening.")]
        [SerializeField] float transformTime = 0.36f;
        [Tooltip("Seconds for one weapon shape to morph into another.")]
        [SerializeField] float morphTime = 0.42f;
        [Tooltip("Which way the scarf winds into the palm: -1 swings the cloth up over the shoulder into the hand " +
                 "(+1 would sweep it down past the feet).")]
        [SerializeField] float spiralDirection = -1f;
        [SerializeField] float turnSpeed = 7f;
        [SerializeField] float idleDriftSpeed = 0.75f;
        [SerializeField] Vector2 wind = new Vector2(-0.3f, 0.05f);
        [SerializeField] Material arrowMaterial;
        [SerializeField] float arrowSpeed = 26f;
        [Tooltip("The hero's hand bone. When set, weapons grow out of the real hand instead of a fixed point.")]
        [SerializeField] Transform hand;
        [Tooltip("Forearm angle (degrees, facing space) at which the weapon shapes were designed.")]
        [SerializeField] float canonicalForearmAngle = -15f;
        [Tooltip("Extra blade turn while running at full speed (degrees, + = tip up). The blade leads in a forward guard.")]
        [SerializeField] float runGuardAngle = -25f;
        float forearmTurn;
        float carry;
        bool morphFromCloth;
        bool lunged;
        float releaseTime = 99f;

        [Header("Whip")]
        [Tooltip("How long (s) the whip's tip lags behind its handle — the travelling loop.")]
        [SerializeField] float whipLag = 0.16f;
        bool whipCracked;
        Vector2[] restBuf, whipBuf, whipCoil;
        float whipOpen;
        /// <summary>Seconds the whip takes to coil back into the hand after a blow.</summary>
        public const float WhipCoilTime = 0.5f;
        /// <summary>0 = coiled in the hand, 1 = fully paid out.</summary>
        public float WhipOpen => whipOpen;
        const int TurnHistory = 128;
        readonly float[] turnTimes = new float[TurnHistory];
        readonly float[] turnValues = new float[TurnHistory];
        int turnHead = -1, turnCount;

        /// <summary>Fired when the whip's loop reaches the tip and it cracks.</summary>
        public event Action WhipCracked;
        /// <summary>Fired when the whip lashes round a grapple ring and the hero starts to swing.</summary>
        public event Action<CrimsonVeil.World.GrapplePoint> WhipLatched;

        /// <summary>The ring the whip is wrapped round (null when not swinging).</summary>
        public CrimsonVeil.World.GrapplePoint Latched { get; private set; }
        /// <summary>The ring an attack would catch right now (lit), or null.</summary>
        public CrimsonVeil.World.GrapplePoint GrappleCandidate { get; private set; }

        void RecordTurn(float at, float turn)
        {
            turnHead = (turnHead + 1) % TurnHistory;
            turnTimes[turnHead] = at;
            turnValues[turnHead] = turn;
            turnCount = Mathf.Min(turnCount + 1, TurnHistory);
        }

        /// <summary>The forearm turn (degrees) <paramref name="secondsAgo"/> seconds ago.</summary>
        float TurnAgo(float secondsAgo)
        {
            if (turnCount == 0) return forearmTurn;
            float at = time - secondsAgo;
            int idx = turnHead;
            for (int k = 0; k < turnCount - 1; k++)
            {
                int prev = (idx - 1 + TurnHistory) % TurnHistory;
                if (turnTimes[prev] <= at)
                {
                    float span = turnTimes[idx] - turnTimes[prev];
                    float u = span > 1e-6f ? Mathf.Clamp01((at - turnTimes[prev]) / span) : 1f;
                    return Mathf.LerpAngle(turnValues[prev], turnValues[idx], u);
                }
                idx = prev;
            }
            return turnValues[idx];
        }

        [Header("Bow string")]
        [Tooltip("The hand that draws the bow string (the other hand).")]
        [SerializeField] Transform drawHand;
        [SerializeField] Material stringMaterial;
        LineRenderer bowString;

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
        /// <summary>The veil's glow (0..1): dimmed while smothered by ash. (Life itself is shown by the HUD.)</summary>
        public float Vitality { get; set; } = 1f;
        public int Facing => player != null && player.Motor != null ? player.Motor.Facing : 1;
        /// <summary>Smoothed facing (-1..1): passes through 0 while the hero turns round.</summary>
        public float FacingBlend => facingScale;
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
        /// <summary>A cast that could not happen (weapon not unlocked yet, or the scarf too short): the cloth flinches.</summary>
        public event Action<VeilForm> CastFizzled;
        public event Action<VeilForm> WeaponUnlocked;

        [Header("Casting")]
        [Tooltip("Weapons the hero has from the start; the rest are unlocked by relics found in the level.")]
        public VeilForm[] startUnlocked = { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Shield, VeilForm.Bow };
        readonly System.Collections.Generic.HashSet<VeilForm> unlocked = new System.Collections.Generic.HashSet<VeilForm>();

        /// <summary>The scarf's strength — its length. Casts spend it, it grows back; full, it takes a hit for the hero.</summary>
        public VeilCharge Charge { get; } = new VeilCharge();
        /// <summary>True while the spear is thrust straight down from the air (it bounces the hero off what it hits).</summary>
        public bool ThrustingDown { get; private set; }
        /// <summary>True while the bow is held drawn.</summary>
        public bool Drawing { get; private set; }
        /// <summary>The shield was raised a moment ago: a blow landing now is parried.</summary>
        public bool InParryWindow => Form == VeilForm.Shield && sinceShieldRaised < VeilCasting.ParryWindow;
        /// <summary>The current weapon came from a cast (it melts back into cloth after the blow).</summary>
        public bool IsCast => castMode;

        Vector2[] shape, fromShape, current, meltFrom, meltPath, meltSpun;
        VeilForm meltForm;
        int meltRingStart;
        float morph = 1f;
        float meltT = -1f;
        /// <summary>0..1 how far the veil has morphed into the current weapon's shape.</summary>
        public float ShapeMorph => morph;
        /// <summary>True while a released weapon is being led back into the streaming scarf.</summary>
        public bool Melting => meltT >= 0f;
        float progressVel;
        float facingScale = 1f;
        float attackTime = -1f;
        bool arrowFired;
        float aimTilt, aimTiltTarget;
        float time;
        IPlayerInput input;
        bool castMode;
        VeilForm? pendingAttack, buffered;
        float lingerTimer = -1f, sinceShieldRaised = 99f, drawTime, recoverTimer;
        /// <summary>True while the body recovers from a blow (no new blow can begin).</summary>
        public bool Recovering => recoverTimer > 0f;

        void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (anchor == null) anchor = transform;
            Simulation = new VeilSimulation(settings, anchor.position) { UseNodeHardness = true };
            shape = new Vector2[Simulation.Count];
            fromShape = new Vector2[Simulation.Count];
            current = new Vector2[Simulation.Count];
            meltFrom = new Vector2[Simulation.Count];
            facingScale = Facing;
            foreach (var f in startUnlocked) unlocked.Add(f);
        }

        public bool IsUnlocked(VeilForm form) => unlocked.Contains(form);

        /// <summary>A relic was found: the veil learns a new weapon.</summary>
        public void Unlock(VeilForm form)
        {
            if (form == VeilForm.Flow || !unlocked.Add(form)) return;
            WeaponUnlocked?.Invoke(form);
        }

        /// <summary>Replaces the set of known weapons (level start, tests).</summary>
        public void SetUnlocked(params VeilForm[] forms)
        {
            unlocked.Clear();
            foreach (var f in forms) unlocked.Add(f);
        }

        /// <summary>
        /// Casts a weapon like a spell: the scarf whirls into it (the transformation is the wind-up), strikes at
        /// once, and melts back into cloth after <see cref="VeilCasting.Linger"/> unless pressed again (combo) or
        /// another weapon is cast (the veil pours from one weapon straight into the next). Held weapons stay:
        /// the shield guards while its button is down, the bow stays drawn. The whip, cast at a lit ring, lashes
        /// round it. Costs scarf strength (<see cref="VeilCasting.Cost"/>).
        /// </summary>
        public bool Cast(VeilForm form)
        {
            if (form == VeilForm.Flow) return false;
            if (!IsUnlocked(form)) { CastFizzled?.Invoke(form); return false; }
            if (Latched != null) return false;
            float cost = VeilCasting.Cost(form);

            // A blow is committed: nothing cuts it short (not even the whip's grapple). A press late in the blow,
            // or while recovering from it, is the follow-up; earlier presses are dropped, so mashing doesn't chain.
            if (attackTime >= 0f || pendingAttack.HasValue || recoverTimer > 0f)
            {
                bool late = recoverTimer > 0f || (attackTime >= 0f && VeilCasting.AcceptsFollowUp(AttackProgress));
                if (!late) return false;
                buffered = form;
                return true;
            }

            if (form == VeilForm.Whip && GrappleCandidate != null && player != null)
            {
                if (!Charge.CanAfford(cost)) { CastFizzled?.Invoke(form); return false; }
                var ring = GrappleCandidate;
                BeginCast(form);
                if (player.TryGrapple(ring.transform.position))
                {
                    Charge.TrySpend(cost);
                    Latched = ring;
                    whipOpen = 1f;
                    pendingAttack = null;
                    WhipLatched?.Invoke(Latched);
                    return true;
                }
            }

            if (!Charge.TrySpend(cost)) { CastFizzled?.Invoke(form); return false; }
            if (Form != form || !castMode) BeginCast(form);
            pendingAttack = form;
            if (form == VeilForm.Shield) sinceShieldRaised = 0f;
            return true;
        }

        void BeginCast(VeilForm form)
        {
            SetForm(form);
            castMode = true;
            lingerTimer = VeilCasting.Linger;
        }

        /// <summary>A blow of the veil landed: the scarf feeds on it; a downward spear bounces the hero up.</summary>
        public void NotifyHitLanded()
        {
            Charge.OnHitLanded();
            if ((ThrustingDown || DownStrike) && player != null && player.Motor != null)
            {
                player.Motor.Bounce();
                ThrustingDown = false;
                DownStrike = false;
            }
        }

        void Start()
        {
            input = player != null && player.InputSource != null ? player.InputSource : GetComponent<IPlayerInput>();
        }

        public void SetForm(VeilForm form)
        {
            if (form == Form) return;
            if (Latched != null && player != null) player.Motor.ReleaseSwing(); // the whip melts: the hero drops
            castMode = false;
            pendingAttack = null;
            ThrustingDown = false;
            DownStrike = false;
            Drawing = false;
            aimTiltTarget = 0f;
            if (form == VeilForm.Flow && TransformProgress > 0.05f) releaseTime = 0f; // start exhaling
            // Back to cloth: the weapon's shape is led, neck first, into the streaming scarf.
            if (form == VeilForm.Flow && TransformProgress > 0.05f && ShapeForm != VeilForm.Flow)
            {
                Array.Copy(current, meltFrom, current.Length);
                meltT = 0f;
                meltForm = ShapeForm;
                meltRingStart = Mathf.Clamp(Mathf.CeilToInt(HandFraction * (Simulation.Count - 1)), 1, Simulation.Count - 1);
            }
            Form = form;
            if (form != VeilForm.Flow)
            {
                StopMelt();
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

        /// <summary>Seconds the veil keeps exhaling after a weapon is released.</summary>
        public const float ReleaseBreathDuration = 0.9f;

        /// <summary>
        /// Acceleration (m/s²) lifting the veil up and back after a weapon melts back into cloth,
        /// for release phase 0..1 (zero outside it).
        /// </summary>
        public static Vector2 ReleaseBreath(float phase, int facing)
        {
            if (phase <= 0f || phase >= 1f) return Vector2.zero;
            float k = Mathf.Sin(phase * Mathf.PI);
            return new Vector2(-facing * 9f, 16f) * k;
        }

        public bool Attack()
        {
            if (IsAttacking || recoverTimer > 0f) return false; // a blow plays to its end, then the body recovers
            if (Form == VeilForm.Flow || Hardness < 0.5f * VeilFormLibrary.Hardness(Form)) return false;
            if (Latched != null) return false;
            // The whip lashes round a ring in reach instead of cracking.
            if (Form == VeilForm.Whip && GrappleCandidate != null && player != null && player.TryGrapple(GrappleCandidate.transform.position))
            {
                Latched = GrappleCandidate;
                whipOpen = 1f;
                attackTime = -1f;
                WhipLatched?.Invoke(Latched);
                return true;
            }
            StartAttack();
            return true;
        }

        void StartAttack()
        {
            attackTime = 0f;
            arrowFired = false;
            lunged = false;
            whipCracked = false;
            Drawing = false;
            drawTime = 0f;
            bool airborne = player != null && player.Motor != null && !player.IsGrounded;
            AttackAim = AimNow(ShapeForm, airborne);
            ThrustingDown = ShapeForm == VeilForm.Spear && airborne && AttackAim <= -80f;
            // A strike under the feet in the air (sword or spear) bounces the hero up off what it hits.
            DownStrike = airborne && AttackAim <= -60f && (ShapeForm == VeilForm.Sword || ShapeForm == VeilForm.Spear);
            // The sword, spear and whip are aimed by the arm (HeroAttackChoreography); the bow turns itself.
            // (Without an arm rig, e.g. a bare test body, the weapon itself is turned instead.)
            // The spear and the bow don't turn with the forearm (VeilFormLibrary.ForearmFollow): they turn themselves,
            // the arm only reaches that way.
            bool armDriven = hand != null && hand.parent != null;
            bool turnsItself = ShapeForm == VeilForm.Bow || ShapeForm == VeilForm.Spear || !armDriven;
            aimTiltTarget = ThrustingDown ? VeilCasting.DownThrustTilt : turnsItself ? AttackAim : 0f;
            // A sword driven down turns the wrist too, so the point goes straight down (the blade otherwise sits
            // well up off the forearm, as it is gripped); it turns over the wind-up.
            if (armDriven && ShapeForm == VeilForm.Sword && AttackAim < 0f)
                aimTiltTarget = SwordDownWrist * Mathf.Clamp01(-AttackAim / 90f);
            // (the spear swings round onto its line during the draw-back instead of jumping there)
            if (ThrustingDown || (turnsItself && ShapeForm != VeilForm.Spear)) aimTilt = aimTiltTarget;
            Attacked?.Invoke(Form);
        }

        /// <summary>Wrist turn (degrees) of a sword driven straight down: the point goes under the feet.</summary>
        public const float SwordDownWrist = -40f;

        /// <summary>Direction (degrees from the facing, +up) the current blow is aimed in, aim assist included.</summary>
        public float AttackAim { get; private set; }
        /// <summary>The current blow is a strike under the feet in the air (it bounces the hero off a hit).</summary>
        public bool DownStrike { get; private set; }

        readonly Collider[] aimBuffer = new Collider[32];
        readonly List<Vector2> aimTargets = new List<Vector2>();

        float aimKeyed; // the eight-way direction chosen for the current blow (before the assist)

        /// <summary>The eight-way aim from the direction keys / stick, turned onto a target in the assist cone.</summary>
        float AimNow(VeilForm form, bool airborne, float? keyed = null)
        {
            float limit = VeilAim.Limit(form);
            if (limit <= 0f || input == null) return 0f;
            float angle = Mathf.Clamp(keyed ?? VeilAim.Direction(input.MoveX, input.AimY, airborne, form), -limit, limit);
            aimKeyed = angle;
            float reach = VeilAim.Reach(form);
            Vector3 a = anchor != null ? anchor.position : transform.position;
            aimTargets.Clear();
            int count = Physics.OverlapSphereNonAlloc(a, reach, aimBuffer, ~0, QueryTriggerInteraction.Collide);
            var root = transform.root;
            for (int i = 0; i < count; i++)
            {
                var c = aimBuffer[i];
                if (c == null || !c.enabled || c.transform.IsChildOf(root)) continue;
                if (c.GetComponentInParent<CrimsonVeil.Combat.IDamageable>() == null) continue;
                aimTargets.Add(c.bounds.center);
            }
            int facing = Facing;
            if (Mathf.Abs(input.MoveX) > 0.3f) facing = input.MoveX > 0f ? 1 : -1; // the hero turns into the blow
            // Aim from where the blow leaves: an arrow flies from the bow hand, not the neck.
            Vector3 from = form == VeilForm.Bow ? ArrowOrigin(a, facing) : a;
            return VeilAim.Assist(angle, from, facing, aimTargets, reach, limit);
        }

        /// <summary>Pending blows, held weapons, follow-ups, and melting back into cloth after a cast.</summary>
        void UpdateCasting(float dt)
        {
            sinceShieldRaised += dt;
            // The blow comes the moment the weapon is firm: the transformation is its wind-up.
            if (pendingAttack.HasValue && Form == pendingAttack.Value && Hardness >= 0.5f * VeilFormLibrary.Hardness(Form))
            {
                pendingAttack = null;
                bool guard = Form == VeilForm.Shield && input != null && input.CastHeld(VeilForm.Shield);
                if (!guard) StartAttack();
            }
            // Bow held: the string stays drawn at full draw and the bow follows the aim; letting go looses.
            bool bowHeld = attackTime >= 0f && ShapeForm == VeilForm.Bow && castMode && input != null
                           && input.CastHeld(VeilForm.Bow) && drawTime < VeilCasting.MaxDraw;
            Drawing = false;
            if (bowHeld)
            {
                float hold = VeilCasting.BowHoldAt * VeilFormLibrary.AttackDuration(VeilForm.Bow);
                if (attackTime >= hold)
                {
                    attackTime = hold;
                    Drawing = true;
                    drawTime += dt;
                    // drawn, the bow follows the eight-way aim (and settles on a target in the assist cone)
                    AttackAim = AimNow(VeilForm.Bow, player != null && player.Motor != null && !player.IsGrounded);
                    aimTiltTarget = AttackAim;
                }
            }
            // Shield held: it guards (and drains the scarf); in the air it is held overhead and slows the fall.
            bool shieldHeld = Form == VeilForm.Shield && castMode && input != null && input.CastHeld(VeilForm.Shield)
                              && Charge.Drain(VeilCasting.ShieldHoldDrain, dt);
            if (player != null && player.Motor != null)
            {
                var m = player.Motor;
                m.ScarfGlide = Form == VeilForm.Flow && Charge.Value > 0.02f;
                m.Umbrella = shieldHeld && !player.IsGrounded;
                if (m.IsGliding && m.GlideTime <= 0f && Form == VeilForm.Flow) Charge.Drain(VeilCasting.GlideDrain, dt);
            }
            if (recoverTimer > 0f) recoverTimer -= dt;
            // A press late in a blow follows it up once it is spent and the body has recovered (combo, or pour
            // into another weapon).
            if (buffered.HasValue && attackTime < 0f && !pendingAttack.HasValue && recoverTimer <= 0f)
            {
                var next = buffered.Value;
                buffered = null;
                Cast(next);
            }
            // A cast weapon lingers for a follow-up, then melts back into cloth.
            if (castMode && Form != VeilForm.Flow)
            {
                if (attackTime >= 0f || pendingAttack.HasValue || shieldHeld || Drawing || Latched != null
                    || (buffered.HasValue && recoverTimer > 0f)) lingerTimer = VeilCasting.Linger;
                else
                {
                    lingerTimer -= dt;
                    if (lingerTimer <= 0f) SetForm(VeilForm.Flow);
                }
            }
            aimTilt = Mathf.MoveTowardsAngle(aimTilt, aimTiltTarget, 480f * dt);
            Charge.Tick(dt);
        }

        void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            time += dt;

            UpdateGrapple();
            if (input != null)
            {
                var cast = input.CastPressed;
                if (cast.HasValue) Cast(cast.Value);
            }

            float goal = Form == VeilForm.Flow ? 0f : 1f;
            // A cast snaps into shape (the transformation is the blow's wind-up) and melts back into cloth quickly.
            float tTime = Form == VeilForm.Flow ? VeilCasting.MeltTime
                : castMode ? VeilCasting.CastTransformTime : transformTime;
            float mTime = VeilTransform.FormMorphTime(ShapeForm, castMode ? VeilCasting.CastMorphTime : morphTime);
            TransformProgress = Mathf.SmoothDamp(TransformProgress, goal, ref progressVel, tTime * 0.4f, Mathf.Infinity, dt);
            if (Mathf.Abs(TransformProgress - goal) < 0.002f) { TransformProgress = goal; progressVel = 0f; }
            Hardness = VeilFormLibrary.Hardness(ShapeForm) * TransformProgress;
            morph = Mathf.Min(1f, morph + dt / Mathf.Max(0.01f, mTime));
            UpdateCasting(dt);

            int facing = Facing;
            facingScale = Mathf.MoveTowards(facingScale, facing, dt * turnSpeed);
            Vector3 a = anchor.position;

            // Releasing a weapon, the flame exhales back over the shoulder instead of pooling at the feet.
            UpdateSenses(dt, a);
            // Danger near: the scarf bristles — quick, nervous gusts.
            Simulation.Wind = wind + new Vector2(Mathf.Sin(time * 23f), Mathf.Sin(time * 31f + 1.3f)) * (2.4f * Danger);
            releaseTime += dt;
            Simulation.Breath = Form == VeilForm.Flow ? ReleaseBreath(releaseTime / ReleaseBreathDuration, facing) : Vector2.zero;
            // Horizontal: the veil streams behind, it doesn't rise — unless it senses a secret and leans towards it.
            Vector2 flow = new Vector2(-facing, 0f) * idleDriftSpeed;
            if (Form == VeilForm.Flow && HintPull > 0f) flow = Vector2.Lerp(flow, HintDirection * idleDriftSpeed * 2.4f, Mathf.SmoothStep(0f, 1f, HintPull));
            Simulation.AmbientFlow = flow;
            // The somersault: the scarf is swept round with the body and winds about it, then unwinds.
            float wrap = player != null ? player.FlipWrap : 0f;
            Simulation.SwirlWeight = wrap;
            if (wrap > 0f)
            {
                Simulation.SwirlCenter = (Vector2)player.transform.position + Vector2.up * Somersault.Pivot;
                // Whirl with the body as it turns (slow-fast-slow), and run down smoothly as it lets go.
                float bodySpin = Somersault.AngularVelocity(player.FlipProgress, player.FlipFacing);
                Simulation.SwirlSpeed = Mathf.Lerp(Simulation.SwirlSpeed, bodySpin, 1f - Mathf.Exp(-14f * dt));
                // the cloth trails the turn: facing right the body turns clockwise, the scarf winds anticlockwise
                Simulation.WrapTrail = player.FlipFacing;
            }
            else Simulation.SwirlSpeed = 0f;

            UpdateNodeHardness();
            if (TransformProgress > 0f && ShapeForm != VeilForm.Flow) UpdateTargets(a, dt);
            UpdateMelt(a, dt);
            if (attackTime < 0f) { AttackProgress = -1f; CurrentSwing = 0f; }

            Simulation.Hardness = Hardness;
            // The hilt / handle / grip is in the fist from the moment of the cast; the rest of the weapon grows out of it.
            if (ShapeForm != VeilForm.Flow && VeilFormLibrary.HeldSpan(ShapeForm, out float heldFrom, out float heldTo))
            {
                Simulation.HeldFrom = heldFrom; Simulation.HeldTo = heldTo;
                Simulation.HeldWeight = Mathf.SmoothStep(0f, 1f, TransformProgress / 0.12f);
            }
            else Simulation.HeldWeight = 0f;
            Simulation.DriftDirection = -Facing;
            // The animation sheet's S-shape (VeilRestShape): held firmly at rest, eased off at a run, while turning
            // round, in a somersault, when it senses a secret or bristles at danger.
            float bodySpeed = Mathf.Abs(Simulation.AnchorVelocity.x);
            Simulation.RestShapeWeight = (1f - wrap) * Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(bodySpeed / 7f)) * Mathf.Abs(facingScale)
                                         * (1f - Mathf.SmoothStep(0f, 1f, HintPull)) * (1f - 0.5f * Danger);
            // The shield's ring circles round the body on its own: once it has formed it is not tied to the hand.
            // (Only once the ring has been thrown round: cut loose earlier, the scarf hung torn off the neck.)
            Simulation.DetachedAfter = ShapeForm == VeilForm.Shield && TransformProgress > 0.3f && morph > 0.6f
                ? Mathf.Clamp(Mathf.CeilToInt(HandFraction * (Simulation.Count - 1)) - 1, 0, Simulation.Count - 2) : -1;
            GatherObstacles(a);
            Simulation.Step(dt, a);
            UpdateBowString();
            UpdateWhipHandle();
        }

        /// <summary>0..1: how close a living enemy is (the scarf bristles).</summary>
        public float Danger { get; private set; }
        /// <summary>0..1 pull of a secret nearby, and the direction to it (the loose scarf leans that way).</summary>
        public float HintPull { get; private set; }
        public Vector2 HintDirection { get; private set; }
        const float DangerRadius = 6f;
        float senseTimer, dangerTarget;
        bool relicsChecked;
        readonly Collider[] senseBuffer = new Collider[24];

        /// <summary>The scarf as a mute companion: it leans towards secrets and bristles when enemies are near.</summary>
        void UpdateSenses(float dt, Vector3 a)
        {
            // Relics already behind the hero when play starts (play from here, a checkpoint) are his.
            if (!relicsChecked && time > 0.05f)
            {
                relicsChecked = true;
                foreach (var r in VeilRelic.All)
                    if (r != null && !r.Collected && r.transform.position.x < transform.position.x - 1f) r.Collect(this, quiet: true);
            }
            HintDirection = CrimsonVeil.World.VeilHint.Pull(a, out float pull);
            HintPull = pull;

            senseTimer -= dt;
            if (senseTimer <= 0f)
            {
                senseTimer = 0.25f;
                dangerTarget = 0f;
                int count = Physics.OverlapSphereNonAlloc(a, DangerRadius, senseBuffer, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < count; i++)
                {
                    var c = senseBuffer[i];
                    if (c == null || c.transform.IsChildOf(transform)) continue;
                    bool alive = false;
                    var creature = c.GetComponentInParent<CrimsonVeil.Combat.CreatureBase>();
                    if (creature != null) alive = !creature.IsDead;
                    else
                    {
                        var enemy = c.GetComponentInParent<CrimsonVeil.Combat.Enemy>();
                        alive = enemy != null && enemy.Health != null && !enemy.Health.IsDead;
                    }
                    if (!alive) continue;
                    float d = Vector2.Distance(c.ClosestPoint(a), a);
                    dangerTarget = Mathf.Max(dangerTarget, 1f - d / DangerRadius);
                }
            }
            Danger = Mathf.MoveTowards(Danger, dangerTarget, dt * 3f);
        }

        /// <summary>Lights the ring the whip would catch; lets go of the ring when the swing has ended.</summary>
        void UpdateGrapple()
        {
            if (Latched != null && (player == null || !player.Motor.IsSwinging)) Latched = null;
            CrimsonVeil.World.GrapplePoint candidate = null;
            // Any ring in reach lights up once the hero knows the whip and has the strength to cast it.
            if (Latched == null && player != null && IsUnlocked(VeilForm.Whip) && Charge.CanAfford(VeilCasting.Cost(VeilForm.Whip)))
                candidate = CrimsonVeil.World.GrapplePoint.FindBest(player.GripPosition, Facing, ~0);
            if (candidate != GrappleCandidate)
            {
                if (GrappleCandidate != null) GrappleCandidate.SetHighlighted(false);
                if (candidate != null) candidate.SetHighlighted(true);
                GrappleCandidate = candidate;
            }
        }

        /// <summary>A thin crimson string between the bow tips, pulled back by the drawing hand.</summary>
        void UpdateBowString()
        {
            bool show = ShapeForm == VeilForm.Bow && Hardness > 0.5f * VeilFormLibrary.Hardness(VeilForm.Bow);
            if (!show)
            {
                if (bowString != null) bowString.enabled = false;
                return;
            }
            if (bowString == null)
            {
                var go = new GameObject("BowString");
                go.transform.SetParent(transform, false);
                bowString = go.AddComponent<LineRenderer>();
                bowString.positionCount = 3;
                bowString.widthMultiplier = 0.04f;
                bowString.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bowString.sharedMaterial = stringMaterial;
                bowString.startColor = bowString.endColor = new Color(1f, 0.1f, 0.1f, 0.9f);
            }
            bowString.enabled = true;
            bowString.gameObject.layer = gameObject.layer;
            // A crisp core for the string section of the ribbon: upper tip → its middle (pulled when drawn) → lower tip.
            var p = Simulation.Positions;
            int n = Simulation.Count;
            var range = VeilFormLibrary.BowStringRange;
            int top = Mathf.Clamp(Mathf.RoundToInt(range.x * (n - 1)), 0, n - 1);
            int bottom = Mathf.Clamp(Mathf.RoundToInt(range.y * (n - 1)), 0, n - 1);
            int mid = Mathf.Clamp(Mathf.RoundToInt((range.x + range.y) * 0.5f * (n - 1)), 0, n - 1);
            bowString.SetPosition(0, p[top]);
            bowString.SetPosition(1, p[mid]);
            bowString.SetPosition(2, p[bottom]);
        }

        LineRenderer whipHandle;

        /// <summary>
        /// The whip's handle is a solid thing in the fist, not ink: a dark straight stock with a knob behind the little
        /// finger and a crimson collar where the lash begins. (The ink of the veil is not drawn over it, see
        /// VeilRenderer.)
        /// </summary>
        void UpdateWhipHandle()
        {
            bool show = ShapeForm == VeilForm.Whip && Form == VeilForm.Whip
                        && Hardness > 0.5f * VeilFormLibrary.Hardness(VeilForm.Whip);
            if (!show)
            {
                if (whipHandle != null) whipHandle.enabled = false;
                return;
            }
            if (whipHandle == null)
            {
                var go = new GameObject("WhipHandle");
                go.transform.SetParent(transform, false);
                whipHandle = go.AddComponent<LineRenderer>();
                whipHandle.positionCount = 4;
                whipHandle.numCapVertices = 4;
                whipHandle.widthMultiplier = WhipHandleWidth;
                whipHandle.widthCurve = new AnimationCurve(
                    new Keyframe(0f, 1.55f), new Keyframe(0.12f, 1f), new Keyframe(0.86f, 0.92f), new Keyframe(0.93f, 1.3f), new Keyframe(1f, 1.3f));
                whipHandle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // its own copy of the string material, white, so the stock's colours are its own (dark wood, crimson collar)
                var mat = stringMaterial != null ? new Material(stringMaterial) : null;
                if (mat != null && mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                whipHandle.sharedMaterial = mat;
                // dark stock right up to a crisp crimson collar (blended, the stock smeared into the lash's red)
                var g = new Gradient { mode = GradientMode.Fixed };
                g.SetKeys(
                    new[] { new GradientColorKey(new Color(0.04f, 0.025f, 0.025f), 0.9f), new GradientColorKey(new Color(0.85f, 0.05f, 0.06f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                whipHandle.colorGradient = g;
            }
            whipHandle.enabled = true;
            whipHandle.gameObject.layer = gameObject.layer; // drawn wherever the hero is (and by any camera that sees him)
            VeilFormLibrary.WhipHandleSpan(out float butt, out float fist, out float stockEnd);
            float z = hand != null ? hand.position.z + 0.015f : Simulation.Positions[0].z; // just behind the fingers
            whipHandle.SetPosition(0, OnVeil(butt, z));
            whipHandle.SetPosition(1, OnVeil(fist, z));
            whipHandle.SetPosition(2, OnVeil(Mathf.Lerp(fist, stockEnd, 0.85f), z));
            whipHandle.SetPosition(3, OnVeil(stockEnd, z));
        }

        /// <summary>Width (m) of the whip's handle: a hand's grip.</summary>
        public const float WhipHandleWidth = 0.042f;

        Vector3 OnVeil(float fraction, float z)
        {
            var p = Simulation.Positions;
            float f = Mathf.Clamp01(fraction) * (Simulation.Count - 1);
            int i0 = Mathf.Min(Mathf.FloorToInt(f), Simulation.Count - 2);
            Vector3 v = Vector3.Lerp(p[i0], p[i0 + 1], f - i0);
            v.z = z;
            return v;
        }

        public Transform DrawHand { get => drawHand; set => drawHand = value; }

        /// <summary>How far (m) along the hand bone, from the wrist, the palm is where a weapon is gripped.</summary>
        public const float PalmOffset = 0.075f;

        /// <summary>The middle of the palm of <paramref name="hand"/> (its bone runs from the wrist along +Y).</summary>
        public static Vector3 PalmPoint(Transform hand) => hand.position + hand.up * PalmOffset;

        void UpdateNodeHardness()
        {
            var nh = Simulation.NodeHardness;
            int n = Simulation.Count;
            float h = VeilFormLibrary.Hardness(ShapeForm);
            for (int i = 0; i < n; i++)
            {
                float f = (float)i / (n - 1);
                nh[i] = h * VeilTransform.Wave(TransformProgress, f) * (ShapeForm == VeilForm.Bow || ShapeForm == VeilForm.Sword || ShapeForm == VeilForm.Spear ? 1f : VeilTransform.TipSoftness(f)); // bows end in a limb, blades and spears in a firm point
            }
        }

        readonly Collider[] obstacleBuffer = new Collider[48];

        /// <summary>
        /// The walls, floors and pillars in reach of the scarf, as rectangles in the picture plane: solid level
        /// geometry in the hero's plane. Triggers, characters (the hero, enemies) and moving bodies are ignored.
        /// </summary>
        void GatherObstacles(Vector3 a)
        {
            var list = Simulation.Obstacles;
            list.Clear();
            float reach = Simulation.Settings.length + 0.6f;
            int count = Physics.OverlapBoxNonAlloc(a, new Vector3(reach, reach, 0.6f), obstacleBuffer, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = obstacleBuffer[i];
                if (!IsObstacle(c, transform.root)) continue;
                var b = c.bounds;
                if (a.z < b.min.z - 0.2f || a.z > b.max.z + 0.2f) continue; // not in the hero's plane (background)
                list.Add(Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y));
            }
        }

        /// <summary>Is this collider solid level geometry the scarf should drape against?</summary>
        public static bool IsObstacle(Collider c, Transform hero)
        {
            if (c == null || c.isTrigger || !c.enabled) return false;
            if (c is CharacterController) return false; // the hero, walking creatures
            if (hero != null && c.transform.IsChildOf(hero)) return false;
            var body = c.attachedRigidbody;
            if (body != null && !body.isKinematic) return false; // loose props, arrows
            if (c.GetComponentInParent<CrimsonVeil.Combat.Enemy>() != null) return false;
            return true;
        }

        void StopMelt()
        {
            meltT = -1f;
            Array.Clear(Simulation.GuideWeight, 0, Simulation.GuideWeight.Length);
        }

        /// <summary>Leads a released weapon back into the scarf: each node travels from where it was in the weapon
        /// to its place on the streaming scarf's centre line, neck first, and is handed to the cloth on arrival.</summary>
        void UpdateMelt(Vector3 a, float dt)
        {
            if (meltT < 0f) return;
            bool shield = meltForm == VeilForm.Shield;
            meltT += dt / (shield ? VeilTransform.ShieldMeltTime : VeilTransform.MeltGuideTime);
            if (meltT >= 1f) { StopMelt(); return; }
            float s = Mathf.Abs(facingScale) < 0.3f ? Facing : facingScale;
            int n = Simulation.Count;
            float length = Simulation.Settings.length;
            var gt = Simulation.GuideTargets;
            if (meltPath == null || meltPath.Length != n) { meltPath = new Vector2[n]; meltSpun = new Vector2[n]; }
            var from = meltFrom;
            float chainT = meltT;
            if (shield)
            {
                // The ring first spins on round the body until it opens at the hero's back, then the scarf runs out
                // of it backwards (unrolled from the front, it hung down across the body).
                float spin = VeilTransform.ShieldOpenSpin(meltFrom, meltRingStart, VeilFormLibrary.ShieldCenter);
                VeilTransform.SpinRing(meltFrom, meltSpun, meltRingStart, VeilFormLibrary.ShieldCenter,
                    spin * Mathf.SmoothStep(0f, 1f, meltT / VeilTransform.ShieldSpinShare));
                from = meltSpun;
                chainT = Mathf.Clamp01((meltT - VeilTransform.ShieldSpinShare * 0.7f) / (1f - VeilTransform.ShieldSpinShare * 0.7f));
            }
            VeilTransform.MeltChain(from, chainT, length, time, meltPath, Simulation.GuideWeight, VeilTransform.MeltSpread(meltForm));
            for (int i = 1; i < n; i++) gt[i] = a + new Vector3(meltPath[i].x * s, meltPath[i].y, 0f);
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
                    // The hero may have moved since the draw began: settle the assist from where the arrow leaves now.
                    if (!Drawing && Mathf.Abs(aimTiltTarget - AttackAim) < 0.01f)
                    {
                        AttackAim = AimNow(VeilForm.Bow, player != null && player.Motor != null && !player.IsGrounded, aimKeyed);
                        aimTiltTarget = AttackAim;
                    }
                    FireArrow(a, Facing);
                }
                // Step into the blow (not when planted: Ctrl held, he strikes from the spot).
                if (!lunged && player != null && player.Motor != null
                    && HeroAttackChoreography.Lunge(ShapeForm, out float lungeSpeed, out float lungeAt) && t >= lungeAt)
                {
                    lunged = true;
                    if (!(input != null && input.StandHeld && player.IsGrounded)) player.Motor.Lunge(lungeSpeed);
                }
                if (t >= 1f)
                {
                    attackTime = -1f;
                    t = -1f;
                    recoverTimer = VeilCasting.Recovery(ShapeForm);
                    ThrustingDown = false;
                    DownStrike = false;
                    aimTiltTarget = 0f;
                }
                AttackProgress = t;
            }
            CurrentSwing = VeilWeaponPose.SwingAngle(ShapeForm, t);

            // Where is the real hand, in the shape's local space? Grow the weapon from there.
            if (hand != null)
            {
                float s = Mathf.Abs(facingScale) < 0.3f ? Facing : facingScale;
                // The weapon is held in the palm: the hand bone starts at the wrist, the fist is a little way along it.
                Vector3 palm = PalmPoint(hand);
                var lh = new Vector2((palm.x - a.x) / s, palm.y - a.y);
                var wanted = lh - VeilFormLibrary.Hand;
                if (wanted.sqrMagnitude > 1f) wanted = wanted.normalized; // guard against a bad first frame
                handDelta = wanted; // rigid in the hand: any smoothing here lags a fast swing

                // The weapon is an extension of the forearm: it turns with it (swings, stance, running).
                var forearm = hand.parent;
                if (forearm != null)
                {
                    Vector3 d = hand.position - forearm.position;
                    float angle = Mathf.Atan2(d.y, d.x / s) * Mathf.Rad2Deg;
                    float wantedTurn = Mathf.DeltaAngle(canonicalForearmAngle, angle) * VeilWeaponPose.ForearmFollow(ShapeForm);
                    // Running: the blade leads in a forward guard — never dragged behind the hero.
                    float run = player != null && player.Motor != null
                        ? Mathf.Clamp01(Mathf.Abs(player.Velocity.x) / Mathf.Max(0.1f, player.Settings.maxRunSpeed)) : 0f;
                    carry = Mathf.Lerp(carry, IsAttacking ? 0f : run, 1f - Mathf.Exp(-6f * dt));
                    wantedTurn += carry * runGuardAngle * VeilWeaponPose.CarryFactor(ShapeForm);
                    forearmTurn = wantedTurn;
                }
            }
            bool armDriven = hand != null && hand.parent != null;
            Vector2 grip = VeilFormLibrary.Hand + handDelta;
            RecordTurn(time, forearmTurn);

            var targets = Simulation.Targets;
            int n = Simulation.Count;
            if (restBuf == null || restBuf.Length != n) { restBuf = new Vector2[n]; whipBuf = new Vector2[n]; }
            bool whip = ShapeForm == VeilForm.Whip && armDriven && Latched == null;
            // The whip hangs coiled in the hand; it pays out during the wind-up and coils back after the blow.
            if (ShapeForm == VeilForm.Whip)
            {
                if (whipCoil == null || whipCoil.Length != n) { whipCoil = new Vector2[n]; VeilFormLibrary.BuildWhipCoil(whipCoil); }
                whipOpen = Latched != null ? 1f
                    : t >= 0f ? Mathf.Max(whipOpen, VeilWeaponPose.WhipUncoil(t)) : Mathf.MoveTowards(whipOpen, 0f, dt / WhipCoilTime);
            }
            for (int i = 0; i < n; i++)
            {
                float f = (float)i / (n - 1);
                bool weapon = f >= HandFraction;
                Vector2 basePoint = shape[i];
                if (ShapeForm == VeilForm.Whip && weapon)
                {
                    VeilFormLibrary.WhipHandleSpan(out _, out _, out float lashFrom);
                    float along = Mathf.Max(0f, f - lashFrom) / Mathf.Max(1e-3f, 1f - lashFrom); // the handle stays put
                    basePoint = Vector2.Lerp(whipCoil[i], shape[i], VeilWeaponPose.WhipNodeOpen(whipOpen, along));
                }
                // Pose around the canonical grip, move onto the real hand, turn with the forearm.
                Vector2 rest = VeilWeaponPose.Idle(ShapeForm, basePoint, weapon, time);
                rest = VeilWeaponPose.Apply(ShapeForm, rest, weapon, t, armDriven);
                // The shield bubble sits round the body: only its root follows the hand.
                float follow = weapon && ShapeForm == VeilForm.Shield ? 1f - VeilFormLibrary.BubbleWeight(basePoint) : 1f;
                rest += weapon ? handDelta * follow : handDelta * (HandFraction > 0f ? f / HandFraction : 1f);
                if (weapon && armDriven && !whip) rest = VeilWeaponPose.Rotate(rest, grip, forearmTurn);
                // Aim: the bow follows up/down, the spear turns straight down for a thrust from the air.
                // The band from the neck turns along, more towards the weapon, so it always ends at the weapon's butt:
                // left behind, its last stretch was pulled into a long half-faded line across the head.
                if (!whip && Mathf.Abs(aimTilt) > 0.01f)
                {
                    float share = weapon ? 1f : Mathf.SmoothStep(0f, 1f, HandFraction > 0f ? f / HandFraction : 1f);
                    if (share > 0f) rest = VeilWeaponPose.Rotate(rest, grip, aimTilt * share);
                }
                // A downward thrust is driven straight under the hero (both hands on the shaft at the belly).
                if (weapon && ThrustingDown) rest += new Vector2(-grip.x, -0.25f) * Mathf.Clamp01(Mathf.Abs(aimTilt) / 80f);
                restBuf[i] = rest;
            }
            if (Latched != null)
            {
                // Wrapped round the ring: the whip runs taut from the hand to it, sagging a touch, with a
                // short loop of slack beyond the ring where the tip has wound round it.
                float sx = Mathf.Abs(facingScale) < 0.3f ? Facing : facingScale;
                Vector3 r = Latched.transform.position;
                var ring = new Vector2((r.x - a.x) / sx, r.y - a.y);
                Vector2 grip0 = grip;
                Vector2 span = ring - grip0;
                var side = new Vector2(span.y, -span.x).normalized;
                VeilFormLibrary.WhipHandleSpan(out _, out _, out float ropeFrom);
                for (int i = 0; i < n; i++)
                {
                    float f = (float)i / (n - 1);
                    if (f < ropeFrom) continue; // the handle stays in the fist
                    float along = (f - ropeFrom) / Mathf.Max(1e-3f, 1f - ropeFrom);
                    float u = Mathf.Min(1f, along / 0.85f);
                    // taut under the hero's weight: barely a sag (it hung 12 cm off the line and looked doubled)
                    Vector2 p = grip0 + span * u + side * Mathf.Sin(u * Mathf.PI) * 0.03f;
                    if (along > 0.85f)
                    {
                        float w = (along - 0.85f) / 0.15f * Mathf.PI * 1.6f; // the tip wound round the ring
                        p = ring + new Vector2(Mathf.Sin(w), -Mathf.Cos(w) + 1f) * 0.16f - new Vector2(0f, 0.16f);
                    }
                    restBuf[i] = p;
                }
            }
            if (whip)
            {
                // A whip is not a stick: every bit of it repeats the handle's motion a moment later, so a
                // swing sends a loop rolling down to the tip, which cracks when the loop runs out.
                VeilFormLibrary.WhipHandleSpan(out _, out _, out float stockEnd);
                int handIndex = Mathf.Clamp(Mathf.CeilToInt(stockEnd * (n - 1)), 0, n - 1); // the stiff handle, then the lash
                Array.Copy(restBuf, whipBuf, n);
                VeilWeaponPose.WhipChain(restBuf, handIndex, TurnAgo, whipLag, whipBuf);
                Array.Copy(whipBuf, restBuf, n);
                if (t >= 0f && !whipCracked && t >= VeilWeaponPose.WhipCrackAt)
                {
                    whipCracked = true;
                    WhipCracked?.Invoke();
                }
            }

            // Drawing the bow: the drawing hand pulls the middle of the string back into a V.
            float pull = ShapeForm == VeilForm.Bow && drawHand != null ? VeilWeaponPose.BowDraw(t) : 0f;
            if (pull > 0f)
            {
                float sx = Mathf.Abs(facingScale) < 0.3f ? Facing : facingScale;
                Vector3 drawPalm = PalmPoint(drawHand); // the string is pulled by the fingers, not the wrist
                var draw = new Vector2((drawPalm.x - a.x) / sx, drawPalm.y - a.y);
                var range = VeilFormLibrary.BowStringRange;
                for (int i = 0; i < n; i++)
                {
                    float f = (float)i / (n - 1);
                    if (f <= range.x || f >= range.y) continue;
                    float u = (f - range.x) / (range.y - range.x);
                    restBuf[i] = Vector2.Lerp(restBuf[i], draw, (1f - Mathf.Abs(2f * u - 1f)) * pull);
                }
            }

            // The shield is thrown round the body from the hand like a loop, not conjured whole.
            if (ShapeForm == VeilForm.Shield && morph < 1f)
                VeilTransform.SweepRing(restBuf, Mathf.Clamp(Mathf.CeilToInt(HandFraction * (n - 1)), 0, n - 1),
                    VeilFormLibrary.ShieldCenter, grip, Mathf.SmoothStep(0f, 1f, morph));

            for (int i = 0; i < n; i++)
            {
                float f = (float)i / (n - 1);
                bool weapon = f >= HandFraction;
                Vector2 rest = restBuf[i];

                Vector2 l;
                if (morph < 1f)
                {
                    float m = VeilTransform.MorphProgress(morph, f);
                    // From loose cloth the blade *grows* out of the palm, overshooting a touch before it settles.
                    if (weapon && morphFromCloth)
                        rest = grip + (rest - grip) * Mathf.Lerp(0.05f, 1f, MovementMath.EaseOutBack(m, 1.7f));
                    l = VeilTransform.MorphNode(fromShape[i], rest, m, f, time, morphFromCloth ? 0.22f : 0.14f);
                    // The scarf is sucked into the palm as a vortex, then the weapon grows out of it.
                    // Like a real vortex it spins fastest near the palm, far cloth only drifts in.
                    if (morphFromCloth)
                        l = VeilWeaponPose.Rotate(l, grip, VeilTransform.SpiralIn(m) * spiralDirection * VeilTransform.VortexFalloff(Vector2.Distance(l, grip)));
                }
                else l = rest;
                // Back to cloth: the weapon unwinds the same way as it melts, tip first.
                if (Form == VeilForm.Flow && weapon)
                    l = VeilWeaponPose.Rotate(l, grip, -0.6f * VeilTransform.SpiralIn(1f - TransformProgress) * spiralDirection);
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

        /// <summary>Where an arrow leaves: the bow hand (raised with the aim), or the canonical grip without a rig.</summary>
        Vector3 ArrowOrigin(Vector3 a, int facing)
        {
            if (hand != null)
            {
                Vector3 p = PalmPoint(hand);
                return new Vector3(p.x, p.y, a.z);
            }
            var grip = VeilFormLibrary.Hand;
            return a + new Vector3((grip.x + 0.35f) * facing, grip.y, 0f);
        }

        void FireArrow(Vector3 a, int facing)
        {
            var pos = ArrowOrigin(a, facing);
            // Aimed in eight directions (held or at the moment of the cast), turned onto a target by the aim assist.
            float aim = Mathf.Abs(aimTiltTarget) > 1f ? aimTiltTarget * Mathf.Deg2Rad : 0.06f;
            Vector3 dir = new Vector3(facing * Mathf.Cos(aim), Mathf.Sin(aim), 0f);
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
