using System;
using CrimsonVeil.Player;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Owns the <see cref="VeilSimulation"/>, switches forms (toggle) and drives attacks.
    /// Runs after the player has moved so the anchor is up to date.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class VeilController : MonoBehaviour
    {
        [SerializeField] VeilSettings settings = new VeilSettings();
        [SerializeField] Transform anchor;
        [SerializeField] PlayerController player;
        [Tooltip("Seconds to fully harden / soften the veil.")]
        [SerializeField] float transformTime = 0.16f;
        [SerializeField] float idleDriftSpeed = 0.75f;
        [SerializeField] Vector2 wind = new Vector2(-0.3f, 0.05f);
        [SerializeField] Material arrowMaterial;
        [SerializeField] float arrowSpeed = 26f;

        public VeilSimulation Simulation { get; private set; }
        public VeilForm Form { get; private set; }
        /// <summary>Shape used for targets; stays on the last weapon while the veil softens back.</summary>
        public VeilForm ShapeForm { get; private set; }
        public float Hardness { get; private set; }
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

        Vector2[] shape;
        float attackTime = -1f;
        bool arrowFired;
        bool aimUp;
        IPlayerInput input;

        void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (anchor == null) anchor = transform;
            Simulation = new VeilSimulation(settings, anchor.position);
            shape = new Vector2[Simulation.Count];
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
                ShapeForm = form;
                HandFraction = VeilFormLibrary.BuildShape(form, shape);
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

        void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            if (input != null)
            {
                var request = input.FormRequested;
                if (request.HasValue) ToggleForm(request.Value);
                if (input.AttackPressed) Attack();
            }

            float target = VeilFormLibrary.Hardness(Form);
            Hardness = Mathf.MoveTowards(Hardness, target, dt / Mathf.Max(0.01f, transformTime));

            int facing = player != null && player.Motor != null ? player.Motor.Facing : 1;
            Vector3 a = anchor.position;

            Simulation.Wind = wind;
            Simulation.AmbientFlow = new Vector2(-facing, 0.45f).normalized * idleDriftSpeed;

            if (Hardness > 0f && ShapeForm != VeilForm.Flow) UpdateTargets(a, facing, dt);
            if (attackTime < 0f) AttackProgress = -1f;

            Simulation.Hardness = Hardness;
            Simulation.Step(dt, a);
        }

        void UpdateTargets(Vector3 a, int facing, float dt)
        {
            float t = -1f;
            if (attackTime >= 0f)
            {
                attackTime += dt;
                t = attackTime / Mathf.Max(0.01f, VeilFormLibrary.AttackDuration(ShapeForm));
                if (ShapeForm == VeilForm.Bow && !arrowFired && t >= 0.3f)
                {
                    arrowFired = true;
                    FireArrow(a, facing);
                }
                if (t >= 1f)
                {
                    attackTime = -1f;
                    t = -1f;
                }
                AttackProgress = t;
            }

            var targets = Simulation.Targets;
            int n = Simulation.Count;
            for (int i = 0; i < n; i++)
            {
                float frac = (float)i / (n - 1);
                Vector2 l = VeilWeaponPose.Apply(ShapeForm, shape[i], frac >= HandFraction, t);
                targets[i] = a + new Vector3(l.x * facing, l.y, 0f);
            }
            Simulation.RecomputeTargetRest();
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
