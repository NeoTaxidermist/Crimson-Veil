using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>The expressions the hero's face can hold (for the story's cutscenes; the game uses a few too).</summary>
    public enum FaceExpression { Neutral, Determined, Angry, Surprised, Sad, Pain, Weary }

    /// <summary>
    /// A pose of the face, engine independent: the jaw 0 (shut) .. 1 (wide open); the lids 0 open .. 1 shut (an upper
    /// lid below 0 is opened wide); the brows -1 (lowered) .. 1 (raised), each on its own; the frown 0 .. 1 (brows
    /// drawn together and down); where the eyes look (degrees, + right / + up, in the head's frame).
    /// </summary>
    public struct FacePose
    {
        public float jaw, lidUpper, lidLower, browL, browR, frown, lookYaw, lookPitch;

        public static FacePose Lerp(in FacePose a, in FacePose b, float t) => new FacePose
        {
            jaw = Mathf.Lerp(a.jaw, b.jaw, t), lidUpper = Mathf.Lerp(a.lidUpper, b.lidUpper, t),
            lidLower = Mathf.Lerp(a.lidLower, b.lidLower, t), browL = Mathf.Lerp(a.browL, b.browL, t),
            browR = Mathf.Lerp(a.browR, b.browR, t), frown = Mathf.Lerp(a.frown, b.frown, t),
            lookYaw = Mathf.Lerp(a.lookYaw, b.lookYaw, t), lookPitch = Mathf.Lerp(a.lookPitch, b.lookPitch, t),
        };
    }

    /// <summary>
    /// The hero's face (ArtSource/hero_build.py's face rig: Jaw, Eye.L/R, Lid.U/D.L/R, Brow.L/R): holds an expression
    /// (blended over a moment), blinks on its own now and then (now and then twice), looks where it is told (the upper
    /// lids follow the gaze), and talks - the jaw working in syllables. Cutscenes drive it through
    /// <see cref="SetExpression"/>, <see cref="LookAt"/>, <see cref="Talking"/> and <see cref="Blink"/>.
    /// </summary>
    [DefaultExecutionOrder(15)]
    public sealed class HeroFace : MonoBehaviour
    {
        [SerializeField] Transform model;

        /// <summary>Degrees the jaw drops wide open.</summary>
        public const float JawOpenDeg = 16f;
        /// <summary>Degrees the upper / lower lid turn to shut (they meet a little below the middle of the eye).</summary>
        public const float LidUpperShutDeg = 46f, LidLowerShutDeg = 18f;
        /// <summary>How far (m) a brow rises or falls, and how far a frown draws it in and down.</summary>
        public const float BrowTravel = 0.006f, FrownTravel = 0.004f;
        /// <summary>The furthest the eyes turn (degrees).</summary>
        public const float LookYawMax = 28f, LookPitchMax = 20f;
        /// <summary>Seconds of a blink: shutting fast, a moment shut, opening a little slower.</summary>
        public const float BlinkShut = 0.06f, BlinkHold = 0.04f, BlinkOpen = 0.11f;

        public FaceExpression Expression { get; private set; }
        /// <summary>The pose the face is in now (after blending, blinking, looking, talking).</summary>
        public FacePose Current { get; private set; }
        /// <summary>The jaw works in syllables while true.</summary>
        public bool Talking { get; set; }
        public bool IsReady { get; private set; }

        Transform jaw, eyeL, eyeR, lidUL, lidUR, lidDL, lidDR, browL, browR, head;
        Quaternion jawRest, eyeLRest, eyeRRest, lidULRest, lidURRest, lidDLRest, lidDRRest;
        Vector3 browLRest, browRRest;
        // the head's own frame at rest (head-local): forward out of the face, up, and its right
        Vector3 fwdH, upH, rightH;

        FacePose from, to;
        float blend = 1f, blendTime = 0.25f;
        float blinkClock = -1f, nextBlink = 2f, doubleBlink;
        Vector3? lookTarget;
        Vector2 lookAngles;
        float talkClock;

        void Awake()
        {
            if (model == null) model = transform;
            Transform F(string n) => CrimsonVeil.Core.RigJoint.FindDeep(model, n);
            head = F("Head"); jaw = F("Jaw");
            eyeL = F("Eye.L"); eyeR = F("Eye.R");
            lidUL = F("Lid.U.L"); lidUR = F("Lid.U.R"); lidDL = F("Lid.D.L"); lidDR = F("Lid.D.R");
            browL = F("Brow.L"); browR = F("Brow.R");
            IsReady = head != null && jaw != null && eyeL != null && eyeR != null && lidUL != null && lidUR != null
                      && lidDL != null && lidDR != null && browL != null && browR != null;
            if (!IsReady) return;
            jawRest = jaw.localRotation;
            eyeLRest = eyeL.localRotation; eyeRRest = eyeR.localRotation;
            lidULRest = lidUL.localRotation; lidURRest = lidUR.localRotation;
            lidDLRest = lidDL.localRotation; lidDRRest = lidDR.localRotation;
            browLRest = browL.localPosition; browRRest = browR.localPosition;
            // bones point along their local +Y (Blender's export): the eye looks along its bone, the head goes up its own
            fwdH = head.InverseTransformDirection(eyeL.up).normalized;
            upH = head.InverseTransformDirection(head.up).normalized;
            upH = (upH - fwdH * Vector3.Dot(upH, fwdH)).normalized;
            rightH = Vector3.Cross(upH, fwdH).normalized;
            from = to = Current = Pose(FaceExpression.Neutral);
            nextBlink = Random.Range(1.5f, 4f);
        }

        /// <summary>The pose of each expression (before blinking, looking and talking).</summary>
        public static FacePose Pose(FaceExpression e)
        {
            switch (e)
            {
                case FaceExpression.Determined: return new FacePose { frown = 0.5f, browL = -0.3f, browR = -0.3f, lidUpper = 0.18f, lidLower = 0.15f };
                case FaceExpression.Angry: return new FacePose { frown = 1f, browL = -0.7f, browR = -0.7f, lidUpper = 0.3f, lidLower = 0.3f, jaw = 0.08f };
                case FaceExpression.Surprised: return new FacePose { browL = 1f, browR = 1f, lidUpper = -0.25f, jaw = 0.4f };
                case FaceExpression.Sad: return new FacePose { browL = 0.45f, browR = 0.45f, frown = 0.35f, lidUpper = 0.35f, lookPitch = -8f };
                case FaceExpression.Pain: return new FacePose { frown = 1f, browL = -0.4f, browR = -0.2f, lidUpper = 0.6f, lidLower = 0.5f, jaw = 0.25f };
                case FaceExpression.Weary: return new FacePose { browL = 0.15f, browR = 0.15f, lidUpper = 0.45f, lidLower = 0.1f, jaw = 0.05f, lookPitch = -5f };
                default: return new FacePose();
            }
        }

        /// <summary>
        /// How shut a blink has the eyes <paramref name="t"/> seconds into it (0 open .. 1 shut): quickly shut, held a
        /// moment, opened a little slower; 0 outside it.
        /// </summary>
        public static float BlinkCurve(float t)
        {
            if (t < 0f) return 0f;
            if (t < BlinkShut) return Mathf.SmoothStep(0f, 1f, t / BlinkShut);
            t -= BlinkShut;
            if (t < BlinkHold) return 1f;
            t -= BlinkHold;
            if (t < BlinkOpen) return 1f - Mathf.SmoothStep(0f, 1f, t / BlinkOpen);
            return 0f;
        }

        /// <summary>Total length of a blink (s).</summary>
        public static float BlinkLength => BlinkShut + BlinkHold + BlinkOpen;

        /// <summary>The jaw talking at time t (0 .. ~0.45): syllables of uneven length and height, now and then a pause.</summary>
        public static float TalkJaw(float t)
        {
            float syllable = Mathf.Abs(Mathf.Sin(t * 9.5f + Mathf.PerlinNoise(t * 1.3f, 0.3f) * 3f));
            float level = 0.15f + 0.3f * Mathf.PerlinNoise(t * 2.1f, 7.7f);
            float pause = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.4f, Mathf.PerlinNoise(t * 0.7f, 3.1f)));
            return syllable * level * pause;
        }

        /// <summary>Into an expression over <paramref name="seconds"/>.</summary>
        public void SetExpression(FaceExpression e, float seconds = 0.25f)
        {
            from = Current;
            from.lookYaw = from.lookPitch = 0f;
            to = Pose(e);
            Expression = e;
            blend = 0f;
            blendTime = Mathf.Max(0.01f, seconds);
        }

        /// <summary>The eyes follow this point (world); null - they look ahead.</summary>
        public void LookAt(Vector3? world) => lookTarget = world;

        /// <summary>Blinks now.</summary>
        public void Blink() => blinkClock = 0f;

        void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (!IsReady || dt <= 0f) return;
            blend = Mathf.Min(1f, blend + dt / blendTime);
            var pose = FacePose.Lerp(from, to, Mathf.SmoothStep(0f, 1f, blend));

            // blinking on its own: every few seconds, now and then twice in a row
            nextBlink -= dt;
            if (nextBlink <= 0f)
            {
                Blink();
                bool twice = doubleBlink <= 0f && Random.value < 0.2f;
                nextBlink = twice ? BlinkLength + 0.08f : Random.Range(2.2f, 5.5f);
                doubleBlink = twice ? 1f : 0f;
            }
            float shut = 0f;
            if (blinkClock >= 0f)
            {
                blinkClock += dt;
                shut = BlinkCurve(blinkClock);
                if (blinkClock > BlinkLength) blinkClock = -1f;
            }
            pose.lidUpper = Mathf.Max(pose.lidUpper, shut);
            pose.lidLower = Mathf.Max(pose.lidLower, shut);

            // looking: at a point, or ahead; the eyes get there in a quick dart
            Vector2 want = new Vector2(pose.lookYaw, pose.lookPitch);
            if (lookTarget.HasValue)
            {
                Vector3 mid = (eyeL.position + eyeR.position) * 0.5f;
                Vector3 d = head.InverseTransformDirection(lookTarget.Value - mid);
                float yaw = Mathf.Atan2(Vector3.Dot(d, rightH), Vector3.Dot(d, fwdH)) * Mathf.Rad2Deg;
                float pitch = Mathf.Atan2(Vector3.Dot(d, upH), new Vector2(Vector3.Dot(d, rightH), Vector3.Dot(d, fwdH)).magnitude) * Mathf.Rad2Deg;
                want = new Vector2(yaw, pitch);
            }
            want.x = Mathf.Clamp(want.x, -LookYawMax, LookYawMax);
            want.y = Mathf.Clamp(want.y, -LookPitchMax, LookPitchMax);
            lookAngles = Vector2.Lerp(lookAngles, want, 1f - Mathf.Exp(-25f * dt));
            pose.lookYaw = lookAngles.x; pose.lookPitch = lookAngles.y;

            if (Talking) { talkClock += dt; pose.jaw = Mathf.Max(pose.jaw, TalkJaw(talkClock)); }
            Current = pose;
            Apply(pose);
        }

        void Apply(in FacePose p)
        {
            Vector3 right = head.TransformDirection(rightH), up = head.TransformDirection(upH);
            Turn(jaw, jawRest, right, p.jaw * JawOpenDeg);
            // the upper lid follows the gaze up and down; it opens wide (below 0) a little less than it shuts
            float upper = Mathf.Clamp(p.lidUpper, -0.4f, 1f) * LidUpperShutDeg - p.lookPitch * 0.6f;
            float lower = Mathf.Clamp01(p.lidLower) * LidLowerShutDeg + Mathf.Min(0f, p.lookPitch) * -0.3f;
            Turn(lidUL, lidULRest, right, upper); Turn(lidUR, lidURRest, right, upper);
            Turn(lidDL, lidDLRest, right, -lower); Turn(lidDR, lidDRRest, right, -lower);
            foreach (var (eye, rest) in new[] { (eyeL, eyeLRest), (eyeR, eyeRRest) })
            {
                eye.localRotation = rest;
                eye.rotation = Quaternion.AngleAxis(p.lookYaw, up) * Quaternion.AngleAxis(-p.lookPitch, right) * eye.rotation;
            }
            Brow(browL, browLRest, p.browL, p.frown, -1f, up, right);
            Brow(browR, browRRest, p.browR, p.frown, 1f, up, right);
        }

        static void Turn(Transform b, Quaternion rest, Vector3 axis, float degrees)
        {
            b.localRotation = rest;
            if (degrees != 0f) b.rotation = Quaternion.AngleAxis(degrees, axis) * b.rotation;
        }

        /// <summary>A brow raised / lowered, and drawn in towards the nose (inward = towards the face's middle) by a frown.</summary>
        void Brow(Transform b, Vector3 rest, float raise, float frown, float inward, Vector3 up, Vector3 right)
        {
            b.localPosition = rest;
            // which way is the nose from this brow: towards the other brow
            var other = b == browL ? browR : browL;
            Vector3 toMiddle = Vector3.Project(other.position - b.position, right).normalized;
            Vector3 offset = up * (Mathf.Clamp(raise, -1f, 1f) * BrowTravel - frown * FrownTravel * 0.5f) + toMiddle * (frown * FrownTravel);
            b.position += offset;
        }
    }
}
