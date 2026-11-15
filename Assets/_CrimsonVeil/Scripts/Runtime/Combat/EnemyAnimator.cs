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
        float dissolve;
        float hurtFlash;
        MaterialPropertyBlock block;

        public bool IsReady { get; private set; }

        void Awake()
        {
            enemy = GetComponent<Enemy>();
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

            // Target pose per state. (The model already hunches ~17° forward; these lean it further only a little, so
            // it stoops and lunges rather than tipping over flat.)
            float sp = 4f, ch = 2f, hd = -3f, aL = 15f + Mathf.Sin(t * 2.1f) * 8f, eL = 25f, aR = 10f - Mathf.Sin(t * 2.1f) * 8f, eR = 30f;
            switch (state)
            {
                case EnemyState.Chase: sp = 5f; ch = 2f; hd = -4f; aL = -25f; aR = -35f; eL = eR = 20f; break;
                case EnemyState.Windup:
                    float w = Mathf.SmoothStep(0f, 1f, windup);
                    sp = Mathf.Lerp(4f, -16f, w); ch = Mathf.Lerp(2f, -8f, w); hd = Mathf.Lerp(-3f, 8f, w);
                    aL = aR = Mathf.Lerp(20f, 165f, w); eL = eR = Mathf.Lerp(30f, 50f, w);
                    break;
                case EnemyState.Strike: sp = 12f; ch = 5f; hd = -6f; aL = aR = 55f; eL = eR = 5f; break;
                case EnemyState.Recover: sp = 7f; ch = 3f; hd = 4f; aL = aR = 5f; eL = eR = 10f; break;
                case EnemyState.Hurt: sp = -16f; ch = -10f; hd = 14f; aL = -40f; aR = 30f; eL = eR = 60f; break;
                case EnemyState.Dead: sp = -8f; ch = -14f; hd = 24f; aL = aR = 120f; eL = eR = 10f; break;
            }

            float k = 1f - Mathf.Exp(-(state == EnemyState.Strike ? sharpness * 2.5f : sharpness) * dt);
            spineA = Mathf.Lerp(spineA, sp, k); chestA = Mathf.Lerp(chestA, ch, k); headA = Mathf.Lerp(headA, hd, k);
            armA = Mathf.Lerp(armA, aL, k); elbowA = Mathf.Lerp(elbowA, eL, k);
            armB = Mathf.Lerp(armB, aR, k); elbowB = Mathf.Lerp(elbowB, eR, k);

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
            visualRoot.localPosition = new Vector3(shudder, 0.18f + Mathf.Sin(t * 2.6f) * 0.08f, 0f);

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
