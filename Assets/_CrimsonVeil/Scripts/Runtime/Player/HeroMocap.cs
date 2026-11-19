using System.Collections.Generic;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Motion capture over the hero's procedural pose. Each frame <see cref="HeroAnimator"/> poses him procedurally
    /// and then lets this blend a captured clip over it: the clip is sampled onto his bones and every bone is turned
    /// part of the way towards it (the weight fades in and out). Some clips run on their own clock (standing, hanging,
    /// the cutscenes' acting), some are driven by the game so they stay in step with it: the mantle by the climb's
    /// progress, a blow by the attack's - the clip's moment of impact laid onto the game's own.
    /// Running, jumping and the scarf's weapons stay procedural; the face, the hair and the cloth are not touched.
    /// </summary>
    public sealed class HeroMocap : MonoBehaviour
    {
        public const string Idle = "Idle", LookAround = "LookAround", HangIdle = "HangIdle", Mantle = "Mantle",
                            HardLanding = "HardLanding", Roll = "Roll", SlashA = "SlashA", SlashB = "SlashB", Stab = "Stab",
                            Talk = "Talk", Sad = "Sad", Angry = "Angry", Nod = "Nod", Point = "Point", GetUp = "GetUp";

        [SerializeField] HeroMocapLibrary library;
        [SerializeField] Transform model;
        [Tooltip("Overall strength of the captured motion over the procedural (0 = off).")]
        [Range(0f, 1f)] public float strength = 1f;

        /// <summary>What the game wants played this frame.</summary>
        public struct Choice
        {
            public string key;
            /// <summary>Driven by the game: the clip's time as 0..1 (else it runs on its own clock).</summary>
            public bool driven;
            public float time01;
            public float weight;
            public static Choice None => new Choice();
            public bool IsNone => string.IsNullOrEmpty(key) || weight <= 0f;
        }

        /// <summary>Seconds of standing still before the captured idle comes in.</summary>
        public const float IdleAfter = 0.25f;

        /// <summary>The game's moment of impact (0..1 through its attack) for each veil weapon.</summary>
        public static float GameImpact(VeilForm form) => form == VeilForm.Spear ? 0.45f : 0.5f;

        /// <summary>
        /// What to play for this state: the mantle in step with the climb, the hang, a blow with the sword or the whip
        /// (both cut like a sword: there is no captured whip), a spear thrust, standing still. Nothing for the rest.
        /// <paramref name="swing"/> alternates the two cuts through a combo.
        /// </summary>
        public static Choice Choose(in HeroState s, float stillTime, int swing)
        {
            // (the captured mantle ends crouched on the top, the game's stands up: it hands over in the last quarter)
            if (s.climbing) return new Choice { key = Mantle, driven = true, time01 = s.climbProgress, weight = 0.85f * MantleHandOver(s.climbProgress) };
            if (s.hanging) return s.weaponHeld < 0.1f ? new Choice { key = HangIdle, weight = 0.6f } : Choice.None;
            if (s.attacking && s.attackT >= 0f)
            {
                switch (s.attackForm)
                {
                    case VeilForm.Sword:
                    case VeilForm.Whip:
                        return new Choice { key = swing % 2 == 0 ? SlashA : SlashB, driven = true, time01 = s.attackT, weight = 0.7f };
                    case VeilForm.Spear:
                        return new Choice { key = Stab, driven = true, time01 = s.attackT, weight = 0.7f };
                }
                return Choice.None;
            }
            bool still = s.grounded && s.speed < 0.2f && !s.crouching && !s.sliding && !s.dashing && s.weaponHeld < 0.1f;
            if (still && stillTime > IdleAfter) return new Choice { key = Idle, weight = 0.8f };
            return Choice.None;
        }

        /// <summary>How much of the captured mantle shows at climb progress t: all of it, then handing over to standing.</summary>
        public static float MantleHandOver(float t) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, t));

        /// <summary>
        /// The game's attack progress laid onto the clip so that both land at once: up to the game's moment of impact the
        /// clip runs up to its own, after it the rest of each.
        /// </summary>
        public static float RemapToClip(float gameT, float gameImpact, float clipImpact)
        {
            gameT = Mathf.Clamp01(gameT);
            if (clipImpact <= 0f || clipImpact >= 1f || gameImpact <= 0f || gameImpact >= 1f) return gameT;
            return gameT <= gameImpact
                ? gameT / gameImpact * clipImpact
                : clipImpact + (gameT - gameImpact) / (1f - gameImpact) * (1f - clipImpact);
        }

        // ------------------------------------------------------------------ playing

        sealed class Layer
        {
            public HeroMocapLibrary.Entry entry;
            public float time, weight, target, fadeSpeed;
            public bool driven;
        }

        readonly List<Layer> layers = new List<Layer>();
        Transform[] bones;
        Quaternion[] proc, sampled;
        Transform hips;
        Vector3 hipsProc;
        Choice script;           // a cutscene's request (wins over the game's)
        bool scripted;

        public bool IsReady => library != null && model != null && bones != null && bones.Length > 0;
        public float Weight { get { float w = 0f; foreach (var l in layers) w += l.weight; return Mathf.Min(1f, w) * strength; } }
        public string Current => layers.Count > 0 ? layers[layers.Count - 1].entry.key : null;

        void Awake() => Bind();

        public void Bind(HeroMocapLibrary lib = null, Transform root = null)
        {
            if (lib != null) library = lib;
            if (root != null) model = root;
            if (model == null) return;
            var list = new List<Transform>();
            foreach (var n in BoneNames)
            {
                var t = CrimsonVeil.Core.RigJoint.FindDeep(model, n);
                if (t != null) list.Add(t);
            }
            bones = list.ToArray();
            proc = new Quaternion[bones.Length];
            sampled = new Quaternion[bones.Length];
            hips = CrimsonVeil.Core.RigJoint.FindDeep(model, "Hips");
        }

        /// <summary>The bones a captured clip drives (body, arms, legs, fingers; not the face, hair, cloth or sash).</summary>
        public static readonly string[] BoneNames =
        {
            "Hips", "Spine", "Chest", "Neck", "Head",
            "Shoulder.L", "UpperArm.L", "LowerArm.L", "Hand.L", "Shoulder.R", "UpperArm.R", "LowerArm.R", "Hand.R",
            "UpperLeg.L", "LowerLeg.L", "Foot.L", "Toe.L", "UpperLeg.R", "LowerLeg.R", "Foot.R", "Toe.R",
            "Thumb.L.0", "Thumb.L.1", "Finger.L.0.0", "Finger.L.0.1", "Finger.L.0.2", "Finger.L.1.0", "Finger.L.1.1", "Finger.L.1.2",
            "Finger.L.2.0", "Finger.L.2.1", "Finger.L.2.2", "Finger.L.3.0", "Finger.L.3.1", "Finger.L.3.2",
            "Thumb.R.0", "Thumb.R.1", "Finger.R.0.0", "Finger.R.0.1", "Finger.R.0.2", "Finger.R.1.0", "Finger.R.1.1", "Finger.R.1.2",
            "Finger.R.2.0", "Finger.R.2.1", "Finger.R.2.2", "Finger.R.3.0", "Finger.R.3.1", "Finger.R.3.2",
        };

        /// <summary>A cutscene plays a clip (on its own clock) until <see cref="StopScript"/>; it wins over the game's choice.</summary>
        public void Play(string key, float weight = 1f)
        {
            script = new Choice { key = key, weight = weight };
            scripted = true;
        }

        public void StopScript() => scripted = false;

        /// <summary>Blends the chosen clip over the pose the procedural animation has just set (called by HeroAnimator).</summary>
        public void Apply(Choice want, float dt)
        {
            if (!IsReady || dt <= 0f) return;
            if (scripted) want = script;

            // the wanted clip fades in; any other fades out
            Layer top = layers.Count > 0 ? layers[layers.Count - 1] : null;
            if (!want.IsNone && (top == null || top.entry.key != want.key || top.target <= 0f) && library.TryGet(want.key, out var entry))
            {
                top = layers.Find(l => l.entry.key == want.key);
                if (top != null) layers.Remove(top);
                else top = new Layer { entry = entry, time = 0f };
                layers.Add(top);
            }
            foreach (var l in layers)
            {
                bool wanted = l == top && !want.IsNone && l.entry.key == want.key;
                l.target = wanted ? want.weight : 0f;
                l.driven = wanted && want.driven;
                // driven blows and the mantle come in quickly, the idle settles in slowly
                l.fadeSpeed = l.driven ? 10f : 3.5f;
                l.weight = Mathf.MoveTowards(l.weight, l.target, dt * l.fadeSpeed);
                float len = Mathf.Max(0.01f, l.entry.clip.length);
                if (l.driven)
                {
                    float t01 = want.time01;
                    if (l.entry.impact > 0f) t01 = RemapToClip(t01, GameImpact(KeyForm(l.entry.key)), l.entry.impact);
                    l.time = Mathf.Clamp01(t01) * len;
                }
                else
                {
                    l.time += dt;
                    l.time = l.entry.loop ? Mathf.Repeat(l.time, len) : Mathf.Min(l.time, len);
                }
            }
            layers.RemoveAll(l => l.weight <= 0f && l.target <= 0f);
            if (layers.Count == 0 || strength <= 0f) return;

            // blend: procedural pose -> each layer in turn
            for (int i = 0; i < bones.Length; i++) proc[i] = bones[i].localRotation;
            hipsProc = hips != null ? hips.localPosition : Vector3.zero;
            var blended = (Quaternion[])proc.Clone();
            Vector3 hipsBlend = hipsProc;
            foreach (var l in layers)
            {
                if (l.weight <= 0f) continue;
                l.entry.clip.SampleAnimation(model.gameObject, l.time);
                float w = Mathf.Clamp01(l.weight * strength);
                for (int i = 0; i < bones.Length; i++) blended[i] = Quaternion.Slerp(blended[i], bones[i].localRotation, w);
                if (hips != null) hipsBlend = Vector3.Lerp(hipsBlend, hips.localPosition, w);
            }
            for (int i = 0; i < bones.Length; i++) bones[i].localRotation = blended[i];
            if (hips != null) hips.localPosition = hipsBlend;
        }

        static VeilForm KeyForm(string key) => key == Stab ? VeilForm.Spear : VeilForm.Sword;
    }
}
