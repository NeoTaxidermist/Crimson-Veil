using CrimsonVeil.Core;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// Procedural motion for the ink wraith: hover bob, lean into the chase, arms rising in a slow
    /// telegraph, a fast downward slash, recoil when hit, and an ink dissolve on death.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyAnimator : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] Transform model;
        [SerializeField] Renderer eyes;
        [Tooltip("Material slot of the glowing eyes on the eyes renderer.")]
        [SerializeField] int eyeMaterialIndex = 1;
        [SerializeField] Color eyeColor = new Color(1.2f, 0.03f, 0.05f);
        [SerializeField] float eyeFlare = 4f;
        [SerializeField] float sharpness = 14f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Enemy enemy;
        RigJoint spine, chest, head, armL, elbowL, armR, elbowR;
        float spineA, chestA, headA, armA, elbowA, armB, elbowB;
        float yaw = -90f;
        float liftA, lungeA, phase, lastX, speedSmooth;

        /// <summary>0..1 through the brain's current timed state (wind-up, strike, recover, hurt); 0 otherwise.</summary>
        public static float StateProgress(EnemyBrain brain)
        {
            var c = brain.Settings;
            float d;
            switch (brain.State)
            {
                case EnemyState.Windup: d = c.windupTime; break;
                case EnemyState.Strike: d = c.strikeTime; break;
                case EnemyState.Recover: d = c.recoverTime; break;
                case EnemyState.Hurt: d = c.hurtTime; break;
                default: return 0f;
            }
            return Mathf.Clamp01(brain.StateTime / Mathf.Max(0.01f, d));
        }
        float dissolve;
        float hurtFlash;
        MaterialPropertyBlock block;

        public bool IsReady { get; private set; }

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            lastX = transform.position.x;
            if (visualRoot == null) visualRoot = transform.Find("Visual");
            if (model == null && visualRoot != null && visualRoot.childCount > 0) model = visualRoot.GetChild(0);
            block = new MaterialPropertyBlock();
            enemy.Hurt += _ => hurtFlash = 1f;
            IsReady = Setup();
            if (IsReady)
            {
                // The claws (in the bind pose: their rest is measured here). The torn strips swing on SpringBones.
                var c = new CrimsonVeil.Player.HeroHands(model, RigJoint.FindDeep(model, "Hips"));
                claws = c.IsReady ? c : null;
            }
        }

        CrimsonVeil.Player.HeroHands claws;

        /// <summary>How the claws hold for each state: hooked while drifting, thrown wide open on the wind-up (the
        /// telegraph), raked shut through the strike.</summary>
        public static float ClawGrip(EnemyState state, float windup)
        {
            switch (state)
            {
                case EnemyState.Chase: return 0.1f;
                case EnemyState.Windup: return Mathf.Lerp(0.2f, -0.55f, Mathf.SmoothStep(0f, 1f, windup));
                case EnemyState.Strike: return 1f;
                case EnemyState.Recover: return 0.6f;
                case EnemyState.Hurt: return -0.3f;
                case EnemyState.Dead: return -0.45f;
                default: return 0.3f;
            }
        }

        bool Setup()
        {
            if (model == null || visualRoot == null) return false;
            Transform F(string n) => RigJoint.FindDeep(model, n);
            Vector3 up = visualRoot.up, forward = visualRoot.forward;
            Vector3 lateral = Vector3.Cross(up, forward).normalized;

            RigJoint J(string bone, string child, Vector3 measure)
            {
                var c = F(child);
                return c == null ? null : RigJoint.Create(F(bone), lateral, () => c.position, measure);
            }

            spine = J("Spine", "Chest", forward);
            chest = J("Chest", "Head", forward);
            var h = F("Head");
            head = h == null ? null : RigJoint.Create(h, lateral, () => h.position + h.up * 0.2f, forward);
            armL = J("UpperArm.L", "LowerArm.L", forward);
            armR = J("UpperArm.R", "LowerArm.R", forward);
            elbowL = J("LowerArm.L", "Hand.L", forward);
            elbowR = J("LowerArm.R", "Hand.R", forward);
            return spine != null;
        }

        void LateUpdate()
        {
            if (visualRoot == null) return;
            float dt = Time.deltaTime;
            float t = Time.time;
            var state = enemy.Brain.State;
            float windup = enemy.WindupProgress;

            // Target pose per state: staged blows (anticipation, action, follow-through), claws stroking with the
            // distance drifted (see CreatureAnimation.Wraith).
            Vector3 at = transform.position;
            float moved = dt > 0f ? Mathf.Abs(at.x - lastX) : 0f;
            lastX = at.x;
            float speed = dt > 0f ? moved / dt : 0f;
            speedSmooth = Mathf.Lerp(speedSmooth, speed, 1f - Mathf.Exp(-8f * dt));
            phase = Mathf.Repeat(phase + moved / CreatureAnimation.WraithStride, 1f);
            var pose = CreatureAnimation.Wraith(state, StateProgress(enemy.Brain), speedSmooth / Mathf.Max(0.1f, enemy.Brain.Settings.chaseSpeed), phase, t);

            float k = 1f - Mathf.Exp(-(state == EnemyState.Strike || state == EnemyState.Hurt ? sharpness * 2.5f : sharpness) * dt);
            spineA = Mathf.Lerp(spineA, pose.spine, k); chestA = Mathf.Lerp(chestA, pose.chest, k); headA = Mathf.Lerp(headA, pose.head, k);
            armA = Mathf.Lerp(armA, pose.armL, k); elbowA = Mathf.Lerp(elbowA, pose.elbowL, k);
            armB = Mathf.Lerp(armB, pose.armR, k); elbowB = Mathf.Lerp(elbowB, pose.elbowR, k);
            liftA = Mathf.Lerp(liftA, pose.lift, k); lungeA = Mathf.Lerp(lungeA, pose.lunge, k);

            spine?.Set(spineA); chest?.Set(chestA); head?.Set(headA);
            armL?.Set(armA); elbowL?.Set(elbowA); armR?.Set(armB); elbowR?.Set(elbowB);
            if (claws != null)
            {
                float g = ClawGrip(state, windup);
                claws.Apply(g, g, dt);
            }

            // Facing, hover bob, hurt shudder, death dissolve.
            yaw = Mathf.LerpAngle(yaw, enemy.Facing > 0 ? 90f : -90f, 1f - Mathf.Exp(-10f * dt));
            hurtFlash = Mathf.MoveTowards(hurtFlash, 0f, dt * 4f);
            float shudder = hurtFlash * Mathf.Sin(t * 70f) * 0.06f;
            visualRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            visualRoot.localPosition = new Vector3(shudder + lungeA * enemy.Facing, 0.18f + liftA + Mathf.Sin(t * 2.6f) * 0.08f, 0f);

            if (state == EnemyState.Dead)
            {
                dissolve = Mathf.MoveTowards(dissolve, 1f, dt / 1.2f);
                float d = Mathf.SmoothStep(0f, 1f, dissolve);
                visualRoot.localScale = new Vector3(1f + d * 0.6f, 1f - d, 1f + d * 0.6f);
            }

            if (eyes != null)
            {
                float flare = 1f + windup * eyeFlare + hurtFlash * 2f;
                if (state == EnemyState.Dead) flare = 1f - dissolve;
                int index = Mathf.Clamp(eyeMaterialIndex, 0, eyes.sharedMaterials.Length - 1);
                eyes.GetPropertyBlock(block, index);
                block.SetColor(BaseColorId, eyeColor * flare);
                eyes.SetPropertyBlock(block, index);
            }
        }
    }
}
