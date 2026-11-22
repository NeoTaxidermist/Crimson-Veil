using System.Collections.Generic;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Motion capture over the hero's procedural pose. Each frame <see cref="HeroAnimator"/> poses him procedurally
    /// and then lets this blend captured clips over it: a clip is sampled onto his bones and every bone (of the clip's
    /// mask) is turned part of the way towards it, the weight fading in and out. Two channels: the body (what he does
    /// with all of him - walking, running, falling, landing, hanging, a blow) and over it the upper body (drawing the
    /// bow, holding the shield, a flinch) so that he can run and aim at once.
    /// Nearly everything is driven by the game so it stays in step with it: the gait by the distance he travels (the
    /// clip's own stride, so the feet don't slide), the mantle by the climb, a blow by the attack (the clip's moment of
    /// impact laid onto the game's), the somersault by the flip, the swing by the pendulum, a landing or a flinch by the
    /// time since it happened. The face, the hair and the cloth are not touched.
    /// </summary>
    public sealed class HeroMocap : MonoBehaviour
    {
        // standing, acting (cutscenes)
        public const string Idle = "Idle", LookAround = "LookAround", InjuredIdle = "InjuredIdle",
                            Talk = "Talk", Sad = "Sad", Angry = "Angry", Nod = "Nod", Point = "Point", GetUp = "GetUp";
        // on the ground
        public const string Walk = "Walk", Run = "Run", Sprint = "Sprint", CrouchIdle = "CrouchIdle",
                            Slide = "Slide", Dash = "Dash", Roll = "Roll", HardLanding = "HardLanding", LandRoll = "LandRoll";
        // in the air, on the lip, on the whip
        public const string JumpUp = "JumpUp", Fall = "Fall", Flip = "Flip", WallJump = "WallJump",
                            HangIdle = "HangIdle", HangCatch = "HangCatch", Mantle = "Mantle", Swing = "Swing";
        // fighting
        public const string SlashA = "SlashA", SlashB = "SlashB", SlashC = "SlashC", Stab = "Stab", ShieldBash = "ShieldBash",
                            BlockIdle = "BlockIdle", Block = "Block", BowDraw = "BowDraw", BowAim = "BowAim", BowShot = "BowShot",
                            Hurt = "Hurt", Stagger = "Stagger", Death = "Death";
        // turning and stopping out of a run
        public const string Turn = "Turn", RunStop = "RunStop";
        // the whip's lashes, the spear's combo, blows in the air
        public const string WhipA = "WhipA", WhipB = "WhipB", WhipC = "WhipC", SpearB = "SpearB", SpearC = "SpearC",
                            AirSlash = "AirSlash", DownStrike = "DownStrike";

        /// <summary>In the air a blow aimed this far down (degrees) is a strike under the feet (sword, spear).</summary>
        public const float DownStrikeAim = -60f;

        /// <summary>The gait's layer (walk, run and sprint blended into one by speed).</summary>
        public const string Gait = "Gait";

        [SerializeField] HeroMocapLibrary library;
        [SerializeField] Transform model;
        [Tooltip("Overall strength of the captured motion over the procedural (0 = off).")]
        [Range(0f, 1f)] public float strength = 1f;
        [Tooltip("The hero's own carriage is added on top of the captured motion (a style layer) instead of the two whole poses being cross-faded - to the trunk, neck and head, a few degrees at most; the limbs are cross-faded as before.")]
        public bool styleLayer = true;

        /// <summary>Which bones a clip moves.</summary>
        public enum Mask
        {
            /// <summary>All of him.</summary>
            Full,
            /// <summary>The waist up (the legs stay the gait's or the procedural's).</summary>
            Upper,
            /// <summary>All but the weapon arm (it holds the veil's weapon where the procedural pose puts it).</summary>
            NoWeaponArm,
            /// <summary>All but the hips' turn: the game turns the whole body (the somersault).</summary>
            NoHipsTurn,
            /// <summary>Legs and arms the clip's, the trunk half the procedural's: the gait (his lean into the run, the
            /// weight of a start and a stop), the mantle (upright over the lip, not folded onto it).</summary>
            Gait,
            /// <summary>The gait with a weapon in hand (the weapon arm stays the procedural's).</summary>
            GaitArmed,
            /// <summary>Only the arm that draws the bow's string (the captured archer stands side-on and shoots to his
            /// side: the bow arm, the trunk and the aim stay the procedural's, which shoot where the hero faces).</summary>
            DrawArm,
            /// <summary>The legs and the hips, the trunk a little (stopping out of a run: the arms stay the game's).</summary>
            Legs,
            /// <summary>The legs alone, the trunk a little (on the whip: the rope turns the body and the hands hold it).</summary>
            LegsOnly,
        }

        /// <summary>What the game wants played this frame on one channel.</summary>
        public struct Choice
        {
            public string key;
            /// <summary>Driven by the game: the clip's time as 0..1 (else it runs on its own clock).</summary>
            public bool driven;
            public float time01;
            /// <summary>Driven by seconds since something happened (a landing, a flinch): the clip's time in seconds.</summary>
            public bool bySeconds;
            public float seconds;
            public float weight;
            public Mask mask;
            /// <summary>A gait: <see cref="key"/> and <see cref="key2"/> blended by <see cref="mix"/>, stepping at <see cref="speed"/> m/s.</summary>
            public bool gait;
            public string key2;
            public float mix, speed;
            /// <summary>Comes in at once (and what it replaces goes as quickly): out of a run, a stop or a skid round - the
            /// run left slowly, its steps stopped mid-stride, and he hung a moment in the run's flight.</summary>
            public bool snap;
            public static Choice None => new Choice();
            public bool IsNone => string.IsNullOrEmpty(key) || weight <= 0f;
            public string Layer => gait ? Gait : key;
        }

        /// <summary>Both channels' choices for a frame.</summary>
        public struct Pick
        {
            public Choice body, upper;
            public static Pick None => new Pick();
        }

        /// <summary>What happened lately, which the game's state alone does not say (kept by HeroAnimator).</summary>
        public struct Context
        {
            public float stillTime;
            /// <summary>Counts the blows of a combo (alternates the cuts).</summary>
            public int swing;
            public float sinceLanded, landFallHeight, landRunSpeed;
            public float sinceWallJump, sinceHangStart, sinceHurt, sinceBlock, sinceDeath, sinceRespawn;
            /// <summary>Since a blow the scarf took for him (a flinch, no harm), since he turned round at a run, since he
            /// pulled up out of a run.</summary>
            public float sinceFlinch, sinceTurn, sinceStop;
            public bool dead, drawing, shieldHeld;
            public float health01;
            /// <summary>The take-off speed of a jump (the rise is laid over it).</summary>
            public float jumpSpeed;
            /// <summary>0..1 through the pendulum's cycle (0 = the forward end).</summary>
            public float swingPhase;
            /// <summary>How far the pendulum swings (radians, from its angle and speed).</summary>
            public float swingAmplitude;
            public float dashProgress;
            public static Context Default => new Context
            {
                sinceLanded = 99f, sinceWallJump = 99f, sinceHangStart = 99f, sinceHurt = 99f, sinceBlock = 99f,
                sinceDeath = 99f, sinceRespawn = 99f, health01 = 1f, jumpSpeed = 12f,
                sinceFlinch = 99f, sinceTurn = 99f, sinceStop = 99f,
            };
        }

        /// <summary>Seconds of standing still before the captured idle comes in; and before he looks around.</summary>
        public const float IdleAfter = 0.25f, LookAroundAfter = 7f;

        /// <summary>The scarf's health under which he stands hurt.</summary>
        public const float InjuredBelow = 0.35f;

        /// <summary>A landing from a fall this high (m) is a hard one (the fall's speed is capped: a jump lands as fast
        /// as a drop from a tower); from a run (m/s) he rolls out of it. Above the double jump's top (about 5.7 m):
        /// at 5.5 m a double jump landed on the edge of it and now and then rolled on the spot.</summary>
        public const float HardLandingHeight = 6.5f, RollOutSpeed = 3f;

        /// <summary>
        /// The part of the captured slide (0..1) he is down in: after it the capture gets up. The game's slide is all
        /// low (and goes on crouched under a low ceiling), so it plays only this part; standing up is the game's.
        /// </summary>
        public const float SlideClipEnd = 0.45f;

        /// <summary>The part of the captured landing roll (0..1) that is the roll; after it he is up and walks off.</summary>
        public const float RollClipEnd = 0.78f;

        /// <summary>
        /// A blow aimed this steeply (degrees, up or down) on the ground: the arms and the trunk are the game's, which
        /// cut where it aims; only the legs are the capture's (the captured cuts are all level).
        /// </summary>
        public const float SteepAim = 35f;
        public const float HardLandingTime = 0.75f, LandRollTime = 1.2f, WallJumpTime = 0.55f, HangCatchTime = 0.45f,
                           HurtTime = 0.55f, BlockTime = 0.5f, GetUpTime = 2.5f,
                           StaggerTime = 0.8f, TurnTime = 0.6f, StopTime = 0.8f;

        /// <summary>A turn or a stop is out of a run this fast (m/s).</summary>
        public const float TurnFromSpeed = 3f, StopFromSpeed = 4f;

        /// <summary>The pull-up starts as the run brakes, still this much (m/s) over <see cref="StopFromSpeed"/> - not once
        /// he has nearly stopped (the brake takes 2-3 frames: played out at a run it left him in the run's flight).</summary>
        public const float StopBrakeMargin = 1.5f;

        /// <summary>Through the bow's attack (0..1): fully drawn, and the string let go.</summary>
        public const float BowFullDraw = 0.3f, BowLoose = 0.36f;

        /// <summary>Where in the captured swing (0..1) he is at the forward end of the pendulum.</summary>
        public const float SwingSync = 0.12f;

        /// <summary>
        /// Where in the captured swing (0..1) the pendulum's phase puts him: from the forward end (<see cref="SwingSync"/>)
        /// half the clip on to the back end, and the same stretch backwards on the way forward again. The captured swing
        /// is no clean loop (its end and its start have the legs tens of degrees apart): looped round the pendulum, the
        /// legs jumped from the one to the other just before the forward end, at every swing.
        /// </summary>
        public static float SwingClipTime(float phase)
        {
            float tri = 1f - Mathf.Abs(1f - 2f * Mathf.Repeat(phase, 1f));   // 0 at the forward end, 1 at the back end, back to 0
            return SwingSync + 0.5f * tri;
        }

        /// <summary>Speeds (m/s) at which the walk is all there is, the run, the sprint.</summary>
        public const float WalkSpeed = 1.6f, RunSpeed = 5.5f, SprintSpeed = 8.5f;

        /// <summary>The game's moment of impact (0..1 through its attack) for each veil weapon.</summary>
        public static float GameImpact(VeilForm form) => form == VeilForm.Spear ? 0.45f : 0.5f;

        /// <summary>
        /// What to play for this state, on both channels. The body: dead, getting up, the mantle, the lip, the whip, a
        /// blow, the air (rising, falling, the somersault, off a wall), the dash, the slide, a hard landing, crouching, the
        /// gait, standing. The upper body: the bow, the shield, a flinch.
        /// </summary>
        public static Pick Choose(in HeroState s, in Context c)
        {
            var pick = new Pick { body = Body(s, c), upper = Upper(s, c) };
            return pick;
        }

        /// <summary>The body's choice (see <see cref="Choose"/>); compatible older signature for the plain cases.</summary>
        public static Choice Choose(in HeroState s, float stillTime, int swing)
        {
            var c = Context.Default;
            c.stillTime = stillTime; c.swing = swing;
            return Body(s, c);
        }

        static Choice Body(in HeroState s, in Context c)
        {
            if (c.dead) return new Choice { key = Death, bySeconds = true, seconds = c.sinceDeath, weight = 1f };
            if (c.sinceRespawn < GetUpTime && s.grounded) return new Choice { key = GetUp, bySeconds = true, seconds = c.sinceRespawn + 0.3f, weight = 0.9f };
            // (the captured mantle folds over the lip and ends crouched on the top, the game's stands up: the trunk stays
            // half the game's, and from halfway up the game takes over)
            if (s.climbing) return new Choice { key = Mantle, driven = true, time01 = s.climbProgress, weight = 0.85f * MantleHandOver(s.climbProgress), mask = Mask.Gait };
            bool armed = s.weaponHeld >= 0.1f;
            if (s.hanging)
            {
                if (armed) return Choice.None;   // one hand on the lip and the weapon in the other: that pose is procedural
                if (c.sinceHangStart < HangCatchTime) return new Choice { key = HangCatch, bySeconds = true, seconds = c.sinceHangStart, weight = 0.7f };
                return new Choice { key = HangIdle, weight = 0.6f };
            }
            if (s.swinging) return new Choice { key = Swing, driven = true, time01 = SwingClipTime(c.swingPhase), weight = 0.75f * SwingWeight(c.swingAmplitude), mask = Mask.LegsOnly };   // (the captured swinger's arms swap hands on the rope)

            if (s.attacking && s.attackT >= 0f)
            {
                int k = ((c.swing % 3) + 3) % 3;
                bool blade = s.attackForm == VeilForm.Sword || s.attackForm == VeilForm.Spear;
                if (!s.grounded && (blade || s.attackForm == VeilForm.Whip))
                {
                    // in the air: a strike under the feet, or a cut in the leap (the game turns the body: the clip the limbs);
                    // aimed steeply up the procedural aim has the arm
                    if (blade && s.attackAim <= DownStrikeAim)
                        return new Choice { key = DownStrike, driven = true, time01 = s.attackT, weight = 0.8f, mask = Mask.NoHipsTurn };
                    float level = 1f - Mathf.Clamp01(Mathf.Abs(s.attackAim) / 70f);
                    return new Choice { key = AirSlash, driven = true, time01 = s.attackT, weight = 0.75f * level, mask = Mask.NoHipsTurn };
                }
                // from the crouch he stays down: the legs keep the crouch, the low blow is the procedural's (the captured
                // blows are all struck standing - laid over the crouch, the legs half stood up)
                if (s.lowBlow && s.grounded)
                    return s.speed > 0.2f ? Choice.None : new Choice { key = CrouchIdle, weight = 0.7f, mask = Mask.Legs };
                // standing, the whole body fights; on the move only the waist up (the legs keep going); aimed steeply up
                // or down only the legs
                var mask = s.grounded && s.speed < 2f && !s.sliding ? Mask.Full : Mask.Upper;
                if (Mathf.Abs(s.attackAim) >= SteepAim && s.attackForm != VeilForm.Shield) mask = Mask.Legs;
                switch (s.attackForm)
                {
                    case VeilForm.Sword:
                        return new Choice { key = k == 0 ? SlashA : k == 1 ? SlashB : SlashC, driven = true, time01 = s.attackT, weight = 0.7f, mask = mask };
                    case VeilForm.Whip:
                        // one-handed lashes: over the shoulder, a wind-up overhead, a low sweep
                        return new Choice { key = k == 0 ? WhipA : k == 1 ? WhipB : WhipC, driven = true, time01 = s.attackT, weight = 0.7f, mask = mask };
                    case VeilForm.Spear:
                        return new Choice { key = k == 0 ? Stab : k == 1 ? SpearB : SpearC, driven = true, time01 = s.attackT, weight = 0.7f, mask = mask };
                    case VeilForm.Shield:
                        return new Choice { key = ShieldBash, driven = true, time01 = s.attackT, weight = 0.65f, mask = mask };
                }
                // (the bow is the upper body's: the legs go on below it)
            }
            var gear = armed ? Mask.NoWeaponArm : Mask.Full;

            if (!s.grounded)
            {
                if (s.wallSliding) return Choice.None;
                if (Somersault.Active(s.somersault))
                    return new Choice { key = Flip, driven = true, time01 = s.somersault, weight = 0.85f * (1f - Somersault.Tuck(s.somersault)), mask = Mask.NoHipsTurn };   // (the captured flip is laid out: through the middle he tucks into a ball, the procedural tuck)
                if (c.sinceWallJump < WallJumpTime) return new Choice { key = WallJump, bySeconds = true, seconds = c.sinceWallJump, weight = 0.7f, mask = gear };
                if (s.verticalVelocity > 0.5f)
                    return new Choice { key = JumpUp, driven = true, time01 = 1f - Mathf.Clamp01(s.verticalVelocity / Mathf.Max(1f, c.jumpSpeed)), weight = 0.7f, mask = gear };
                return new Choice { key = Fall, weight = 0.75f, mask = gear };
            }
            if (s.dashing) return new Choice { key = Dash, driven = true, time01 = c.dashProgress, weight = 0.6f, mask = gear };
            if (s.sliding) return new Choice { key = Slide, driven = true, time01 = s.slideProgress * SlideClipEnd, weight = 0.8f, mask = gear };
            // rolling out of a hard landing (the game carries him forward: the capture rolls on the spot)
            if (s.rolling) return new Choice { key = LandRoll, driven = true, time01 = s.rollProgress * RollClipEnd, weight = 0.85f, mask = gear };
            // a blow that got through staggers him back (a blow on the scarf or the shield only makes him flinch)
            if (c.sinceHurt < StaggerTime && s.speed < 2f && !(s.attacking && s.attackT >= 0f))   // (running off again, he runs)
                return new Choice { key = Stagger, bySeconds = true, seconds = c.sinceHurt, weight = 0.85f, mask = gear };
            if (c.landFallHeight >= HardLandingHeight)
            {
                if (c.landRunSpeed < RollOutSpeed && c.sinceLanded < HardLandingTime && s.speed < 1f)
                    return new Choice { key = HardLanding, bySeconds = true, seconds = c.sinceLanded, weight = 0.8f, mask = gear };
            }
            // turning round at a run: he skids round on his feet (the game turns the body: the clip has the limbs)
            if (c.sinceTurn < TurnTime && !s.crouching && !(s.attacking && s.attackT >= 0f))
                return new Choice { key = Turn, bySeconds = true, seconds = c.sinceTurn, weight = 0.75f, mask = armed ? Mask.Legs : Mask.NoHipsTurn, snap = true };
            // pulling up out of a run: the last steps brake and he straightens (unless he sets off again)
            if (c.sinceStop < StopTime && s.speed < StopFromSpeed + StopBrakeMargin && !s.crouching && !(s.attacking && s.attackT >= 0f))
                return new Choice { key = RunStop, bySeconds = true, seconds = c.sinceStop, weight = 0.8f, mask = Mask.Legs, snap = true };
            // (crouch-walking stays procedural: the captured one shuffles short steps, at the game's speed a scurry)
            if (s.crouching)
                return s.speed > 0.2f ? Choice.None : new Choice { key = CrouchIdle, weight = 0.7f, mask = gear };
            if (s.speed > 0.2f)
            {
                GaitMix(s.speed, out var a, out var b, out float mix);
                return new Choice { key = a, key2 = b, mix = mix, gait = true, speed = s.speed, weight = 0.9f, mask = armed ? Mask.GaitArmed : Mask.Gait };
            }
            if (c.stillTime > IdleAfter)
            {
                string idle = c.health01 < InjuredBelow ? InjuredIdle : c.stillTime > LookAroundAfter && !armed ? LookAround : Idle;
                return new Choice { key = idle, weight = 0.8f, mask = gear };
            }
            return Choice.None;
        }

        static Choice Upper(in HeroState s, in Context c)
        {
            if (c.dead || s.hanging || s.climbing || s.swinging) return Choice.None;
            if (s.attackForm == VeilForm.Bow && (s.attacking || c.drawing))
            {
                // (only the drawing arm: the procedural bow arm aims wherever the game aims, level or steep)
                float w = 0.85f;
                // (the draw reaches full draw a moment before the game says it is held: until the string is let go it aims)
                if (c.drawing || (s.attackT >= BowFullDraw && s.attackT < BowLoose))
                    return new Choice { key = BowAim, weight = w, mask = Mask.DrawArm };
                if (s.attackT >= 0f && s.attackT < BowFullDraw)
                    return new Choice { key = BowDraw, driven = true, time01 = s.attackT / BowFullDraw, weight = w, mask = Mask.DrawArm };
                if (s.attackT >= BowLoose)
                    return new Choice { key = BowShot, driven = true, time01 = Mathf.InverseLerp(BowLoose, 1f, s.attackT), weight = w, mask = Mask.DrawArm };
            }
            // a flinch: a blow the scarf or the shield took - or one that got through while he can't stagger (in the air,
            // or mid-blow: the waist up only)
            float flinch = Mathf.Min(c.sinceFlinch, !s.grounded || (s.attacking && s.attackT >= 0f) ? c.sinceHurt : 99f);
            if (flinch < HurtTime) return new Choice { key = Hurt, bySeconds = true, seconds = flinch, weight = 0.75f, mask = Mask.Upper };
            if (c.shieldHeld)
            {
                if (c.sinceBlock < BlockTime) return new Choice { key = Block, bySeconds = true, seconds = c.sinceBlock, weight = 0.8f, mask = Mask.Upper };
                return new Choice { key = BlockIdle, weight = 0.6f, mask = Mask.Upper };
            }
            return Choice.None;
        }

        /// <summary>Which two gaits to blend at this speed, and how far from the first to the second.</summary>
        public static void GaitMix(float speed, out string a, out string b, out float mix)
        {
            if (speed <= RunSpeed) { a = Walk; b = Run; mix = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(WalkSpeed, RunSpeed, speed)); }
            else { a = Run; b = Sprint; mix = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RunSpeed, SprintSpeed, speed)); }
        }

        /// <summary>
        /// How much of the captured swing shows: none hanging (nearly) still - there the phase of the sway is noise and the
        /// legs jerked from one end of the clip to the other - all of it once he really swings.
        /// </summary>
        public static float SwingWeight(float amplitude) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.45f, amplitude));

        /// <summary>The pendulum's amplitude (radians) from its angle and angular velocity.</summary>
        public static float SwingAmplitude(float angle, float angularVelocity, float omega)
        {
            float v = angularVelocity / Mathf.Max(0.01f, omega);
            return Mathf.Sqrt(angle * angle + v * v);
        }

        /// <summary>0..1 through the pendulum's cycle from its angle and angular velocity (0 = the forward end).</summary>
        public static float SwingPhase(float angle, float angularVelocity, float omega)
        {
            // angle = A cos(wt), velocity = -A w sin(wt): the phase is the angle round that ellipse
            float a = Mathf.Atan2(-angularVelocity / Mathf.Max(0.01f, omega), angle);
            return Mathf.Repeat(a / (2f * Mathf.PI), 1f);
        }

        /// <summary>How much of the captured mantle shows at climb progress t: all of it, then handing over to standing.</summary>
        public static float MantleHandOver(float t) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.85f, t));

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
            public string id;
            public HeroMocapLibrary.Entry entry, entry2;
            public bool hasSecond;
            public float mix;
            public float time, weight, target, fadeSpeed;
            public Mask mask;
        }

        /// <summary>Layers no channel uses, kept for the next clip.</summary>
        readonly List<Layer> pool = new List<Layer>();

        sealed class Channel
        {
            public readonly List<Layer> layers = new List<Layer>();
        }

        readonly Channel body = new Channel(), upper = new Channel();
        Transform[] bones;
        float[][] masks;            // per Mask, per bone: how much the clip moves it
        bool[] hipsPosition;        // per Mask: whether the clip places the hips
        Quaternion[] proc, sampled, sampled2, blended;
        Quaternion[] rest, clipPose;   // the bind pose; the clips alone, blended (style layer)
        float[] cover;                 // how much of each bone the clips cover this frame (0..1)
        float[] styleOf, legOf;        // per bone: share of the hero's own carriage added; 1 for the legs
        Transform hips;
        float gaitPhase;            // 0..1 through the gait's cycle (left foot ahead at 0)
        Pick script;                // a cutscene's request (wins over the game's)
        bool scripted;

        public bool IsReady => library != null && model != null && bones != null && bones.Length > 0;
        public float Weight => ChannelWeight(body);
        public float UpperWeight => ChannelWeight(upper);
        float ChannelWeight(Channel ch) { float w = 0f; foreach (var l in ch.layers) w += l.weight; return Mathf.Min(1f, w) * strength; }
        public string Current => body.layers.Count > 0 ? body.layers[body.layers.Count - 1].id : null;
        public string CurrentUpper => upper.layers.Count > 0 ? upper.layers[upper.layers.Count - 1].id : null;
        public float GaitPhase => gaitPhase;

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
            sampled2 = new Quaternion[bones.Length];
            blended = new Quaternion[bones.Length];
            rest = new Quaternion[bones.Length];
            clipPose = new Quaternion[bones.Length];
            cover = new float[bones.Length];
            styleOf = new float[bones.Length];
            legOf = new float[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                rest[i] = bones[i].localRotation;   // (bound before the first pose: the model's bind pose)
                styleOf[i] = StyleShare(bones[i].name);
                legOf[i] = IsLegOrHips(bones[i].name) ? 1f : 0f;
            }
            hips = CrimsonVeil.Core.RigJoint.FindDeep(model, "Hips");
            int maskCount = System.Enum.GetValues(typeof(Mask)).Length;
            masks = new float[maskCount][];
            hipsPosition = new bool[maskCount];
            for (int m = 0; m < maskCount; m++)
            {
                masks[m] = new float[bones.Length];
                for (int i = 0; i < bones.Length; i++) masks[m][i] = MaskWeight((Mask)m, bones[i].name);
                // (the drawing arm alone moves no hips: crouched, the standing archer's hips stood him half up)
                hipsPosition[m] = (Mask)m != Mask.Upper && (Mask)m != Mask.DrawArm;
            }
        }

        /// <summary>How much a clip of this mask moves the bone (the waist blends in over the spine).</summary>
        public static float MaskWeight(Mask mask, string bone)
        {
            switch (mask)
            {
                case Mask.Upper:
                    if (bone == "Spine") return 0.4f;
                    if (bone == "Chest") return 0.8f;
                    return IsLegOrHips(bone) ? 0f : 1f;
                case Mask.NoWeaponArm:
                    return IsWeaponArm(bone) ? 0f : 1f;
                case Mask.NoHipsTurn:
                    return bone == "Hips" ? 0f : 1f;
                case Mask.DrawArm:
                    return IsDrawArm(bone) ? 1f : 0f;
                case Mask.LegsOnly:
                    if (bone == "Hips") return 0f;
                    goto case Mask.Legs;
                case Mask.Legs:
                    if (IsLegOrHips(bone)) return 1f;
                    return bone == "Spine" ? 0.5f : bone == "Chest" ? 0.3f : 0f;
                case Mask.Gait:
                case Mask.GaitArmed:
                    if (mask == Mask.GaitArmed && IsWeaponArm(bone)) return 0f;
                    return IsTrunk(bone) ? 0.5f : 1f;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// The style layer: where clips play, a bone takes the clip's rotation with the hero's own carriage added on top -
        /// his procedural pose's turn from the bind pose, a share of it per body part (<see cref="StyleShare"/>), instead of
        /// two whole poses being cross-faded (the clip's own bearing and his were averaged away). The result fades
        /// in over the procedural pose with the clips' weight, as before. The legs and hips are left cross-faded.
        /// </summary>
        void StyleOver()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                float c = cover[i];
                if (c <= 0f) continue;
                Quaternion own = StyleOf(Quaternion.Inverse(rest[i]) * proc[i], styleOf[i]);
                Quaternion styled = clipPose[i] * own;
                // (the legs and hips stay cross-faded as before: taken whole from the clip, they did not fit the
                // hips' blended height - crouched the weapon went into the floor, braking a foot lifted off it;
                // HeroAnimator.KeepOnGround keeps their contacts)
                if (legOf[i] > 0f) continue;
                blended[i] = Quaternion.Slerp(proc[i], styled, c);
            }
        }

        /// <summary>Degrees at most a bone's own carriage turns it on top of a clip: his bearing, not his pose.</summary>
        public const float MaxStyleAngle = 4f;

        /// <summary>
        /// The share of a bone's own turn from the bind pose that is added on top of a clip, held to
        /// <see cref="MaxStyleAngle"/>: a small lean of the trunk or tilt of the head carries over, a big bend (the
        /// crouch's, his bow) is the clip's to make - added on top of a blow captured crouched, it doubled the bend and
        /// the blade and the shield's ring went into the floor.
        /// </summary>
        public static Quaternion StyleOf(Quaternion own, float share)
        {
            Quaternion part = Quaternion.Slerp(Quaternion.identity, own, share);
            return Quaternion.RotateTowards(Quaternion.identity, part, MaxStyleAngle);
        }

        /// <summary>
        /// How much of the hero's own carriage (his procedural pose's turn from the bind pose) is added on top of a clip,
        /// per bone: the trunk, neck and head carry his lean and bearing; the arms, legs, hips, hands and fingers none -
        /// they keep the clip's contacts, reach and grips (the weapon is held where the clip's hand is).
        /// </summary>
        public static float StyleShare(string bone)
        {
            if (IsLegOrHips(bone)) return 0f;
            if (bone == "Spine" || bone == "Chest" || bone == "UpperChest") return 0.45f;
            if (bone == "Neck" || bone == "Head") return 0.4f;
            // (no arms: added on top of a clip's arm, their set pushed the hand with the weapon a hand's breadth
            // lower - crouched, the blade and the shield's ring went into the floor)
            return 0f;
        }

        static bool IsLegOrHips(string b) => b == "Hips" || b.StartsWith("UpperLeg") || b.StartsWith("LowerLeg") || b.StartsWith("Foot") || b.StartsWith("Toe");
        static bool IsDrawArm(string b) => b == "Shoulder.L" || b == "UpperArm.L" || b == "LowerArm.L" || b == "Hand.L"
                                           || b.StartsWith("Finger.L") || b.StartsWith("Thumb.L");
        static bool IsTrunk(string b) => b == "Spine" || b == "Chest" || b == "UpperChest" || b == "Neck" || b == "Head";
        static bool IsWeaponArm(string b) => b == "Shoulder.R" || b == "UpperArm.R" || b == "LowerArm.R" || b == "Hand.R"
                                             || b.StartsWith("Finger.R") || b.StartsWith("Thumb.R");

        /// <summary>The bones a captured clip drives (body, arms, legs, fingers; not the face, hair, cloth or sash).</summary>
        public static readonly string[] BoneNames =
        {
            "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
            "Shoulder.L", "UpperArm.L", "LowerArm.L", "Hand.L", "Shoulder.R", "UpperArm.R", "LowerArm.R", "Hand.R",
            "UpperLeg.L", "LowerLeg.L", "Foot.L", "Toe.L", "UpperLeg.R", "LowerLeg.R", "Foot.R", "Toe.R",
            "Thumb.L.0", "Thumb.L.1", "Thumb.L.2", "Finger.L.0.0", "Finger.L.0.1", "Finger.L.0.2", "Finger.L.1.0", "Finger.L.1.1", "Finger.L.1.2",
            "Finger.L.2.0", "Finger.L.2.1", "Finger.L.2.2", "Finger.L.3.0", "Finger.L.3.1", "Finger.L.3.2",
            "Thumb.R.0", "Thumb.R.1", "Thumb.R.2", "Finger.R.0.0", "Finger.R.0.1", "Finger.R.0.2", "Finger.R.1.0", "Finger.R.1.1", "Finger.R.1.2",
            "Finger.R.2.0", "Finger.R.2.1", "Finger.R.2.2", "Finger.R.3.0", "Finger.R.3.1", "Finger.R.3.2",
        };

        /// <summary>A cutscene plays a clip (on its own clock) until <see cref="StopScript"/>; it wins over the game's choice.</summary>
        public void Play(string key, float weight = 1f)
        {
            script = new Pick { body = new Choice { key = key, weight = weight } };
            scripted = true;
        }

        public void StopScript() => scripted = false;

        /// <summary>How many clips were sampled onto the model this frame (each a whole pass over its bones).</summary>
        public int SamplesThisFrame { get; private set; }

        /// <summary>The body channel alone (the upper body lets go).</summary>
        public void Apply(Choice want, float dt) => Apply(new Pick { body = want }, dt);

        /// <summary>Blends the chosen clips over the pose the procedural animation has just set (called by HeroAnimator).</summary>
        public void Apply(Pick want, float dt)
        {
            if (!IsReady || dt <= 0f) return;
            using var marker = CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.Mocap);
            SamplesThisFrame = 0;
            if (scripted) want = script;

            Advance(body, want.body, dt);
            Advance(upper, want.upper, dt);
            if ((body.layers.Count == 0 && upper.layers.Count == 0) || strength <= 0f) return;

            for (int i = 0; i < bones.Length; i++) proc[i] = bones[i].localRotation;
            Vector3 hipsProc = hips != null ? hips.localPosition : Vector3.zero;
            System.Array.Copy(proc, blended, proc.Length);   // (a buffer of its own: a fresh copy each frame was garbage)
            Vector3 hipsBlend = hipsProc;
            System.Array.Clear(cover, 0, cover.Length);
            Blend(body, blended, ref hipsBlend);
            Blend(upper, blended, ref hipsBlend);
            if (styleLayer) StyleOver();
            for (int i = 0; i < bones.Length; i++) bones[i].localRotation = blended[i];
            if (hips != null) hips.localPosition = hipsBlend;
        }

        /// <summary>How fast (weight per second) clips come in and go: an idle settling in, a blow or a gait, a snap.</summary>
        public const float SlowFade = 3.5f, QuickFade = 10f, SnapFade = 25f;

        void Advance(Channel ch, Choice want, float dt)
        {
            Layer top = ch.layers.Count > 0 ? ch.layers[ch.layers.Count - 1] : null;
            if (!want.IsNone && library.TryGet(want.key, out var entry))
            {
                string id = want.Layer;
                if (top == null || top.id != id || top.target <= 0f)
                {
                    Layer found = null;   // (a search with a lambda over the id: a closure each time)
                    foreach (var l in ch.layers) if (l.id == id) { found = l; break; }
                    if (found != null) ch.layers.Remove(found);
                    else
                    {
                        // (a layer of the pool: a new one for every change of clip was garbage)
                        if (pool.Count > 0) { found = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); }
                        else found = new Layer();
                        found.id = id; found.time = 0f; found.weight = 0f; found.target = 0f; found.fadeSpeed = 0f;
                        found.hasSecond = false; found.mix = 0f;
                        // a gait comes in with its feet where the steps already are
                        bool gaitOn = false;
                        foreach (var l in ch.layers) if (l.id == Gait) { gaitOn = true; break; }
                        if (want.gait && !gaitOn) gaitPhase = Mathf.Repeat(gaitPhase, 1f);
                    }
                    ch.layers.Add(found);
                    top = found;
                }
                top.entry = entry;
                top.hasSecond = want.gait && !string.IsNullOrEmpty(want.key2) && library.TryGet(want.key2, out top.entry2);
                top.mix = top.hasSecond ? Mathf.Clamp01(want.mix) : 0f;
                top.mask = want.mask;
            }
            else top = null;

            // driven blows and the like come in quickly, the idle settles in slowly; what is replaced goes as quickly as
            // its replacement comes (a run left over a quarter second, its steps stopped mid-stride: he hung in its flight)
            float inSpeed = top == null ? SlowFade : want.snap ? SnapFade : want.driven || want.bySeconds || want.gait ? QuickFade : SlowFade;
            foreach (var l in ch.layers)
            {
                bool wanted = l == top;
                l.target = wanted ? want.weight : 0f;
                l.fadeSpeed = wanted ? inSpeed : Mathf.Max(SlowFade, inSpeed);
                l.weight = Mathf.MoveTowards(l.weight, l.target, dt * l.fadeSpeed);
                float len = Mathf.Max(0.01f, l.entry.clip.length);
                if (wanted && want.gait)
                {
                    // the steps follow the ground covered, at the clip's own stride (blended between the two gaits)
                    float stride = Mathf.Lerp(Stride(l.entry), l.hasSecond ? Stride(l.entry2) : Stride(l.entry), l.mix);
                    gaitPhase = Mathf.Repeat(gaitPhase + want.speed * dt / stride, 1f);
                }
                else if (wanted && want.driven)
                {
                    float t01 = want.time01;
                    if (l.entry.impact > 0f) t01 = RemapToClip(t01, GameImpact(KeyForm(l.entry.key)), l.entry.impact);
                    l.time = (l.entry.loop ? Mathf.Repeat(t01, 1f) : Mathf.Clamp01(t01)) * len;
                }
                else if (wanted && want.bySeconds)
                    l.time = l.entry.loop ? Mathf.Repeat(want.seconds, len) : Mathf.Clamp(want.seconds, 0f, len);
                else if (l.id != Gait)
                {
                    l.time += dt;
                    l.time = l.entry.loop ? Mathf.Repeat(l.time, len) : Mathf.Min(l.time, len);
                }
            }
            for (int i = ch.layers.Count - 1; i >= 0; i--)
            {
                var l = ch.layers[i];
                if (l.weight <= 0f && l.target <= 0f) { ch.layers.RemoveAt(i); pool.Add(l); }
            }
        }

        static float Stride(in HeroMocapLibrary.Entry e) => GaitStride(e.key, e.clip != null ? e.clip.length : 0f, e.stride);

        /// <summary>
        /// Ground (m) one loop of a gait's steps carries him: the pace the clip was captured at times its length, so at
        /// that pace the steps go at the clip's own tempo and faster or slower in proportion. (The stride measured
        /// from the in-place clip's feet came out at half the real one - a running loop of 2 m - and at full speed his
        /// legs pattered through four loops a second.)
        /// </summary>
        public static float GaitStride(string key, float clipLength, float measured)
        {
            float pace = key == Walk ? WalkSpeed : key == Run ? RunSpeed : key == Sprint ? SprintSpeed : 0f;
            if (pace > 0f && clipLength > 0.05f) return pace * clipLength;
            return measured > 0.1f ? measured : 2f;
        }

        float GaitTime(in HeroMocapLibrary.Entry e) => Mathf.Repeat(gaitPhase + e.sync, 1f) * Mathf.Max(0.01f, e.clip.length);

        void Blend(Channel ch, Quaternion[] blended, ref Vector3 hipsBlend)
        {
            foreach (var l in ch.layers)
            {
                if (l.weight <= 0f) continue;
                bool gait = l.id == Gait;
                using (CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.MocapSample)) l.entry.clip.SampleAnimation(model.gameObject, gait ? GaitTime(l.entry) : l.time);
                SamplesThisFrame++;
                for (int i = 0; i < bones.Length; i++) sampled[i] = bones[i].localRotation;
                Vector3 hipsSampled = hips != null ? hips.localPosition : Vector3.zero;
                if (l.hasSecond && l.mix > 0f)
                {
                    using (CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.MocapSample)) l.entry2.clip.SampleAnimation(model.gameObject, GaitTime(l.entry2));
                    SamplesThisFrame++;
                    for (int i = 0; i < bones.Length; i++) sampled[i] = Quaternion.Slerp(sampled[i], bones[i].localRotation, l.mix);
                    if (hips != null) hipsSampled = Vector3.Lerp(hipsSampled, hips.localPosition, l.mix);
                }
                float w = Mathf.Clamp01(l.weight * strength);
                var m = masks[(int)l.mask];
                for (int i = 0; i < bones.Length; i++)
                {
                    if (m[i] <= 0f) continue;
                    float a = w * m[i];
                    blended[i] = Quaternion.Slerp(blended[i], sampled[i], a);
                    // the clips alone (for the style layer): each layer its share of what the clips cover
                    float covered = 1f - (1f - cover[i]) * (1f - a);
                    clipPose[i] = cover[i] <= 0f ? sampled[i] : Quaternion.Slerp(clipPose[i], sampled[i], a / Mathf.Max(1e-4f, covered));
                    cover[i] = covered;
                }
                if (hips != null && hipsPosition[(int)l.mask]) hipsBlend = Vector3.Lerp(hipsBlend, hipsSampled, w);
            }
        }

        static VeilForm KeyForm(string key) => key == Stab || key == SpearB || key == SpearC ? VeilForm.Spear : VeilForm.Sword;
    }
}
