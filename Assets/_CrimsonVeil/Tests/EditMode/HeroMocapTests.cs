using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Motion capture over the hero: what plays when, in step with the game, and the baked clips in git.</summary>
    public class HeroMocapTests
    {
        const string LibraryPath = "Assets/_CrimsonVeil/Animations/Hero/HeroMocapLibrary.asset";

        static HeroState Ground(float speed = 0f) => new HeroState { grounded = true, speed = speed, maxSpeed = 8f };

        [Test]
        public void TheMantle_PlaysInStepWithTheClimb_AndHandsOverAtTheEnd()
        {
            var s = new HeroState { climbing = true, climbProgress = 0.4f };
            var c = HeroMocap.Choose(s, 0f, 0);
            Assert.AreEqual(HeroMocap.Mantle, c.key);
            Assert.IsTrue(c.driven, "driven by the climb, not its own clock");
            Assert.AreEqual(0.4f, c.time01, 1e-5f);
            Assert.Greater(c.weight, 0.6f);
            Assert.AreEqual(0f, HeroMocap.MantleHandOver(1f), 1e-4f, "standing at the end is the game's own");
            Assert.AreEqual(1f, HeroMocap.MantleHandOver(0.5f), 1e-4f);
            Assert.Less(HeroMocap.MantleHandOver(0.75f), 0.5f, "from halfway up the game stands him up (the clip folds over the lip)");
            Assert.AreEqual(HeroMocap.Mask.Gait, c.mask, "the trunk half the game's: upright over the lip");
        }

        [Test]
        public void Hanging_TheCapturedHang_UnlessAWeaponIsInHand()
        {
            Assert.AreEqual(HeroMocap.HangIdle, HeroMocap.Choose(new HeroState { hanging = true }, 0f, 0).key);
            Assert.IsTrue(HeroMocap.Choose(new HeroState { hanging = true, weaponHeld = 1f }, 0f, 0).IsNone,
                "one hand on the lip and the weapon in the other: that pose is procedural");
        }

        [Test]
        public void Blows_SwordAndWhipCutLikeASword_InTurn_TheSpearThrusts()
        {
            var s = Ground();
            s.attacking = true; s.attackT = 0.3f;
            s.attackForm = VeilForm.Sword;
            Assert.AreEqual(HeroMocap.SlashA, HeroMocap.Choose(s, 0f, 0).key);
            Assert.AreEqual(HeroMocap.SlashB, HeroMocap.Choose(s, 0f, 1).key, "the combo alternates the cuts");
            s.attackForm = VeilForm.Whip;
            Assert.AreEqual(HeroMocap.WhipA, HeroMocap.Choose(s, 0f, 0).key, "the whip lashes one-handed");
            Assert.AreEqual(HeroMocap.WhipB, HeroMocap.Choose(s, 0f, 1).key);
            Assert.AreEqual(HeroMocap.WhipC, HeroMocap.Choose(s, 0f, 2).key);
            s.attackForm = VeilForm.Spear;
            var stab = HeroMocap.Choose(s, 0f, 0);
            Assert.AreEqual(HeroMocap.Stab, stab.key);
            Assert.IsTrue(stab.driven);
            Assert.AreEqual(HeroMocap.SpearB, HeroMocap.Choose(s, 0f, 1).key, "the spear has its combo");
            Assert.AreEqual(HeroMocap.SpearC, HeroMocap.Choose(s, 0f, 2).key);
            s.attackForm = VeilForm.Bow;
            Assert.IsTrue(HeroMocap.Choose(s, 0f, 0).IsNone, "the bow stays procedural");
        }

        [Test]
        public void StandingStill_TheCapturedIdleComesIn_ThenHeLooksAround_OrStandsHurt()
        {
            Assert.IsTrue(HeroMocap.Choose(Ground(), 0.1f, 0).IsNone, "not at once");
            Assert.AreEqual(HeroMocap.Idle, HeroMocap.Choose(Ground(), 1f, 0).key);
            var c = HeroMocap.Context.Default;
            c.stillTime = HeroMocap.LookAroundAfter + 1f;
            Assert.AreEqual(HeroMocap.LookAround, HeroMocap.Choose(Ground(), c).body.key, "a long while still, he looks round");
            c.health01 = 0.2f;
            Assert.AreEqual(HeroMocap.InjuredIdle, HeroMocap.Choose(Ground(), c).body.key, "the scarf low, he stands hurt");
            var armed = Ground(); armed.weaponHeld = 1f;
            c = HeroMocap.Context.Default; c.stillTime = 1f;
            var idle = HeroMocap.Choose(armed, c).body;
            Assert.AreEqual(HeroMocap.Idle, idle.key);
            Assert.AreEqual(HeroMocap.Mask.NoWeaponArm, idle.mask, "the weapon arm holds the weapon");
        }

        [Test]
        public void Moving_TheGaitBlendsWalkRunAndSprint_BySpeed_SteppingWithTheGround()
        {
            var walk = HeroMocap.Choose(Ground(1f), 0f, 0);
            Assert.IsTrue(walk.gait);
            Assert.AreEqual(HeroMocap.Walk, walk.key);
            Assert.AreEqual(0f, walk.mix, 1e-4f, "slow: all walk");
            Assert.AreEqual(1f, walk.speed, 1e-4f, "the steps follow the speed over the ground");
            var jog = HeroMocap.Choose(Ground(4f), 0f, 0);
            Assert.AreEqual(HeroMocap.Walk, jog.key); Assert.AreEqual(HeroMocap.Run, jog.key2);
            Assert.That(jog.mix, Is.InRange(0.3f, 0.95f));
            var full = HeroMocap.Choose(Ground(8f), 0f, 0);
            Assert.AreEqual(HeroMocap.Run, full.key); Assert.AreEqual(HeroMocap.Sprint, full.key2);
            Assert.Greater(full.mix, 0.5f, "full speed is mostly the sprint");
            Assert.AreEqual(HeroMocap.Mask.Gait, full.mask, "the trunk keeps half the procedural lean");
            var crouch = Ground(1.5f); crouch.crouching = true;
            Assert.IsTrue(HeroMocap.Choose(crouch, 0f, 0).IsNone, "crouch-walking stays procedural");
            crouch.speed = 0f;
            Assert.AreEqual(HeroMocap.CrouchIdle, HeroMocap.Choose(crouch, 0f, 0).key);
        }

        [Test]
        public void InTheAir_RisingFallingTheSomersaultAndOffAWall()
        {
            var c = HeroMocap.Context.Default; c.jumpSpeed = 18f;
            var rise = new HeroState { verticalVelocity = 17f, somersault = -1f };
            var early = HeroMocap.Choose(rise, c).body;
            Assert.AreEqual(HeroMocap.JumpUp, early.key);
            rise.verticalVelocity = 3f;
            Assert.Greater(HeroMocap.Choose(rise, c).body.time01, early.time01, "the rise runs on as the jump slows");
            Assert.AreEqual(HeroMocap.Fall, HeroMocap.Choose(new HeroState { verticalVelocity = -5f, somersault = -1f }, c).body.key);
            var flip = HeroMocap.Choose(new HeroState { verticalVelocity = 5f, somersault = 0.4f }, c).body;
            Assert.AreEqual(HeroMocap.Flip, flip.key);
            Assert.AreEqual(0.4f, flip.time01, 1e-5f, "in step with the game's somersault");
            Assert.AreEqual(HeroMocap.Mask.NoHipsTurn, flip.mask, "the game turns the body over: the clip only the limbs");
            Assert.Less(flip.weight, 0.2f, "through the middle he is tucked into a ball (the captured flip is laid out)");
            var takeOff = HeroMocap.Choose(new HeroState { verticalVelocity = 12f, somersault = 0.05f }, c).body;
            Assert.Greater(takeOff.weight, 0.6f, "the take-off and the opening to land are the clip's");
            c.sinceWallJump = 0.2f;
            Assert.AreEqual(HeroMocap.WallJump, HeroMocap.Choose(new HeroState { verticalVelocity = 10f, somersault = -1f }, c).body.key);
            Assert.IsTrue(HeroMocap.Choose(new HeroState { wallSliding = true, verticalVelocity = -2f, somersault = -1f }, c).body.IsNone, "the wall slide is procedural");
        }

        [Test]
        public void Landing_AHardFallTakesTheKnees_FromARunHeRollsOut_AnOrdinaryJumpDoesNot()
        {
            var c = HeroMocap.Context.Default;
            c.sinceLanded = 0.1f; c.landFallHeight = 8f; c.landRunSpeed = 0f;
            Assert.AreEqual(HeroMocap.HardLanding, HeroMocap.Choose(Ground(), c).body.key);
            c.landRunSpeed = 7f;
            var rolling = Ground(5f); rolling.rolling = true; rolling.rollProgress = 0.5f;
            var roll = HeroMocap.Choose(rolling, c).body;
            Assert.AreEqual(HeroMocap.LandRoll, roll.key, "the game's roll plays the captured one");
            Assert.IsTrue(roll.driven, "in step with the roll that carries him");
            Assert.AreEqual(0.5f * HeroMocap.RollClipEnd, roll.time01, 1e-5f);
            Assert.AreNotEqual(HeroMocap.LandRoll, HeroMocap.Choose(Ground(0f), c).body.key,
                "no roll on the spot: without the game's roll (it stopped him, or he jumped out) the capture doesn't tumble in place");
            c.landFallHeight = 3.4f;   // (a plain jump comes down from its own height)
            Assert.AreEqual(HeroMocap.Gait, HeroMocap.Choose(Ground(7f), c).body.Layer);
            c.landFallHeight = 8f; c.sinceLanded = 3f;
            Assert.AreEqual(HeroMocap.Gait, HeroMocap.Choose(Ground(7f), c).body.Layer, "and then he runs on");
        }

        [Test]
        public void OnTheGround_TheDashAndTheSlide_InStepWithTheGame()
        {
            var c = HeroMocap.Context.Default; c.dashProgress = 0.3f;
            var dash = Ground(15f); dash.dashing = true;
            var d = HeroMocap.Choose(dash, c).body;
            Assert.AreEqual(HeroMocap.Dash, d.key); Assert.AreEqual(0.3f, d.time01, 1e-5f);
            var slide = Ground(10f); slide.sliding = true; slide.slideProgress = 0.6f;
            var sl = HeroMocap.Choose(slide, c).body;
            Assert.AreEqual(HeroMocap.Slide, sl.key); Assert.AreEqual(0.6f * HeroMocap.SlideClipEnd, sl.time01, 1e-5f);
            slide.slideProgress = 1f;
            Assert.AreEqual(HeroMocap.SlideClipEnd, HeroMocap.Choose(slide, c).body.time01, 1e-5f,
                "to its end the slide stays down (the capture gets up after it: the game is still low)");
        }

        [Test]
        public void TheCapturedSlide_IsDownUpToItsEnd_TheRollIsARoll()
        {
            var lib = AssetDatabase.LoadAssetAtPath<HeroMocapLibrary>(LibraryPath);
            var hero = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Hero/Hero.fbx"));
            try
            {
                var head = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "Head");
                Assert.IsTrue(lib.TryGet(HeroMocap.Slide, out var slide));
                for (float t = 0.15f; t <= HeroMocap.SlideClipEnd + 1e-4f; t += 0.05f)
                {
                    slide.clip.SampleAnimation(hero, slide.clip.length * t);
                    Assert.Less(head.position.y, 0.95f, $"the slide's part that plays is down (at {t:F2})");
                }
                Assert.IsTrue(lib.TryGet(HeroMocap.LandRoll, out var roll));
                float lowest = 9f;
                for (float t = 0f; t <= HeroMocap.RollClipEnd; t += 0.05f)
                {
                    roll.clip.SampleAnimation(hero, roll.clip.length * t);
                    lowest = Mathf.Min(lowest, head.position.y);
                }
                Assert.Less(lowest, 0.5f, "through the roll the head goes down to the ground");
                roll.clip.SampleAnimation(hero, roll.clip.length * HeroMocap.RollClipEnd);
                Assert.Greater(head.position.y, 1.3f, "by its end he is up again");
            }
            finally { Object.DestroyImmediate(hero); }
        }

        [Test]
        public void ABlowAimedSteeply_OnTheGround_TheArmsAreTheGames_TheLegsTheCaptures()
        {
            var s = Ground();
            s.attacking = true; s.attackT = 0.3f;
            foreach (var form in new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip })
            {
                s.attackForm = form;
                s.attackAim = 0f;
                Assert.AreEqual(HeroMocap.Mask.Full, HeroMocap.Choose(s, 0f, 0).mask, $"{form} level: the whole body");
                s.attackAim = 60f;
                Assert.AreEqual(HeroMocap.Mask.Legs, HeroMocap.Choose(s, 0f, 0).mask, $"{form} up: the cut goes where it aims");
                s.attackAim = -45f;
                Assert.AreEqual(HeroMocap.Mask.Legs, HeroMocap.Choose(s, 0f, 0).mask, $"{form} low");
                s.attackAim = HeroMocap.SteepAim - 5f;
                Assert.AreEqual(HeroMocap.Mask.Full, HeroMocap.Choose(s, 0f, 0).mask, $"{form} a little up is still the capture's");
            }
        }

        [Test]
        public void HangingStillOnTheWhip_TheCapturedSwingFadesOut()
        {
            var s = new HeroState { swinging = true };
            var c = HeroMocap.Context.Default;
            c.swingAmplitude = HeroMocap.SwingAmplitude(0.02f, 0.03f, 1.5f);
            Assert.AreEqual(0f, HeroMocap.Choose(s, c).body.weight, 1e-4f, "at rest the sway's phase is noise: no jerking legs");
            c.swingAmplitude = HeroMocap.SwingAmplitude(0f, 1.2f, 1.5f);
            Assert.AreEqual(0.8f, c.swingAmplitude, 1e-4f, "through the bottom the amplitude is in the speed");
            Assert.AreEqual(0.75f, HeroMocap.Choose(s, c).body.weight, 1e-4f, "really swinging: the whole captured swing");
            float last = 0f;
            for (float a = 0f; a <= 0.6f; a += 0.02f)
            {
                float w = HeroMocap.SwingWeight(a);
                Assert.GreaterOrEqual(w, last, "it grows smoothly with the swing");
                last = w;
            }
        }

        [Test]
        public void TheWhip_TheSwingFollowsThePendulum()
        {
            var s = new HeroState { swinging = true };
            var c = HeroMocap.Context.Default; c.swingPhase = 0.25f;
            var sw = HeroMocap.Choose(s, c).body;
            Assert.AreEqual(HeroMocap.Swing, sw.key);
            Assert.AreEqual(0.25f + HeroMocap.SwingSync, sw.time01, 1e-5f);
            Assert.AreEqual(HeroMocap.Mask.LegsOnly, sw.mask, "the rope turns the body and the hands hold it: the clip swings the legs");
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.LegsOnly, "UpperArm.R"), "no swapping hands on the rope");
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.LegsOnly, "Hips"));
            Assert.AreEqual(1f, HeroMocap.MaskWeight(HeroMocap.Mask.LegsOnly, "LowerLeg.L"));
            // the pendulum's phase: 0 at the forward end, a quarter on through the bottom going back
            Assert.AreEqual(0f, HeroMocap.SwingPhase(1f, 0f, 2f), 1e-4f);
            Assert.AreEqual(0.25f, HeroMocap.SwingPhase(0f, -2f, 2f), 1e-4f);
            Assert.AreEqual(0.5f, HeroMocap.SwingPhase(-1f, 0f, 2f), 1e-4f);
            Assert.AreEqual(0.75f, HeroMocap.SwingPhase(0f, 2f, 2f), 1e-4f);
        }

        [Test]
        public void CatchingTheLip_ThenHanging()
        {
            var c = HeroMocap.Context.Default; c.sinceHangStart = 0.1f;
            Assert.AreEqual(HeroMocap.HangCatch, HeroMocap.Choose(new HeroState { hanging = true }, c).body.key);
            c.sinceHangStart = 2f;
            Assert.AreEqual(HeroMocap.HangIdle, HeroMocap.Choose(new HeroState { hanging = true }, c).body.key);
        }

        [Test]
        public void TheUpperBody_DrawsTheBow_GuardsWithTheShield_Flinches_WhileTheLegsGoOn()
        {
            var s = Ground(5f);
            s.attackForm = VeilForm.Bow; s.attacking = true; s.attackT = 0.1f;
            var c = HeroMocap.Context.Default;
            var pick = HeroMocap.Choose(s, c);
            Assert.AreEqual(HeroMocap.BowDraw, pick.upper.key);
            Assert.AreEqual(HeroMocap.Mask.DrawArm, pick.upper.mask, "only the string hand: the bow arm aims where the game aims");
            Assert.AreEqual(HeroMocap.Gait, pick.body.Layer, "running on below the bow");
            c.drawing = true;
            Assert.AreEqual(HeroMocap.BowAim, HeroMocap.Choose(s, c).upper.key);
            c.drawing = false; s.attackT = HeroMocap.BowFullDraw + 0.01f;
            Assert.AreEqual(HeroMocap.BowAim, HeroMocap.Choose(s, c).upper.key, "fully drawn before the game says held: still aiming, not loosing");
            s.attackT = 0.7f;
            Assert.AreEqual(HeroMocap.BowShot, HeroMocap.Choose(s, c).upper.key);
            s.attackAim = 80f;
            Assert.AreEqual(HeroMocap.BowShot, HeroMocap.Choose(s, c).upper.key, "aimed steeply too: the bow arm is procedural");

            var guard = Ground(); guard.attackForm = VeilForm.Shield;
            c = HeroMocap.Context.Default; c.shieldHeld = true;
            Assert.AreEqual(HeroMocap.BlockIdle, HeroMocap.Choose(guard, c).upper.key);
            c.sinceBlock = 0.1f;
            Assert.AreEqual(HeroMocap.Block, HeroMocap.Choose(guard, c).upper.key, "a blow on the shield jolts it");

            c = HeroMocap.Context.Default; c.sinceFlinch = 0.1f;
            Assert.AreEqual(HeroMocap.Hurt, HeroMocap.Choose(Ground(), c).upper.key, "a blow the scarf took: a flinch");
        }

        [Test]
        public void ABlowThatGetsThrough_StaggersHim_InTheAirHeFlinches()
        {
            var c = HeroMocap.Context.Default; c.sinceHurt = 0.1f;
            var pick = HeroMocap.Choose(Ground(), c);
            Assert.AreEqual(HeroMocap.Stagger, pick.body.key, "standing, the whole body reels back");
            Assert.IsTrue(pick.upper.IsNone, "no flinch on top of the stagger");
            var air = new HeroState { verticalVelocity = -3f, somersault = -1f };
            pick = HeroMocap.Choose(air, c);
            Assert.AreEqual(HeroMocap.Hurt, pick.upper.key, "in the air only the waist up");
            Assert.AreNotEqual(HeroMocap.Stagger, pick.body.key);
            Assert.AreEqual(HeroMocap.Gait, HeroMocap.Choose(Ground(5f), c).body.Layer, "running off again, he runs");
            c.sinceHurt = HeroMocap.StaggerTime + 0.1f;
            Assert.AreNotEqual(HeroMocap.Stagger, HeroMocap.Choose(Ground(), c).body.key, "and then it is over");
        }

        [Test]
        public void TurningRoundAtARun_HeSkids_PullingUp_HeBrakes()
        {
            var c = HeroMocap.Context.Default; c.sinceTurn = 0.2f;
            var turn = HeroMocap.Choose(Ground(2f), c).body;
            Assert.AreEqual(HeroMocap.Turn, turn.key);
            Assert.AreEqual(0.2f, turn.seconds, 1e-5f);
            Assert.AreEqual(HeroMocap.Mask.NoHipsTurn, turn.mask, "the game turns the body round: the clip has the limbs");
            var armed = Ground(2f); armed.weaponHeld = 1f;
            Assert.AreEqual(HeroMocap.Mask.Legs, HeroMocap.Choose(armed, c).body.mask, "with a weapon only the legs skid");

            c = HeroMocap.Context.Default; c.sinceStop = 0.3f;
            var stop = HeroMocap.Choose(Ground(0.5f), c).body;
            Assert.AreEqual(HeroMocap.RunStop, stop.key);
            Assert.AreEqual(HeroMocap.Mask.Legs, stop.mask, "the legs brake, the arms stay the game's");
            Assert.AreEqual(HeroMocap.Gait, HeroMocap.Choose(Ground(5f), c).body.Layer, "setting off again, he runs");
            Assert.AreEqual(1f, HeroMocap.MaskWeight(HeroMocap.Mask.Legs, "LowerLeg.L"));
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.Legs, "UpperArm.R"));
        }

        [Test]
        public void Death_AndGettingUpAgain()
        {
            var c = HeroMocap.Context.Default; c.dead = true; c.sinceDeath = 0.5f;
            var pick = HeroMocap.Choose(Ground(), c);
            Assert.AreEqual(HeroMocap.Death, pick.body.key);
            Assert.AreEqual(1f, pick.body.weight, 1e-5f);
            Assert.IsTrue(pick.upper.IsNone);
            c = HeroMocap.Context.Default; c.sinceRespawn = 0.5f;
            Assert.AreEqual(HeroMocap.GetUp, HeroMocap.Choose(Ground(), c).body.key);
        }

        [Test]
        public void Masks_WhichBonesAClipMoves()
        {
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.Upper, "UpperLeg.L"));
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.Upper, "Hips"));
            Assert.AreEqual(1f, HeroMocap.MaskWeight(HeroMocap.Mask.Upper, "UpperArm.R"));
            Assert.That(HeroMocap.MaskWeight(HeroMocap.Mask.Upper, "Spine"), Is.InRange(0.1f, 0.9f), "the waist blends in");
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.NoWeaponArm, "Hand.R"));
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.NoWeaponArm, "Finger.R.1.2"));
            Assert.AreEqual(1f, HeroMocap.MaskWeight(HeroMocap.Mask.NoWeaponArm, "Hand.L"));
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.NoHipsTurn, "Hips"));
            Assert.AreEqual(0.5f, HeroMocap.MaskWeight(HeroMocap.Mask.Gait, "Chest"), 1e-5f);
            Assert.AreEqual(1f, HeroMocap.MaskWeight(HeroMocap.Mask.Gait, "LowerLeg.R"));
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.GaitArmed, "UpperArm.R"));
            Assert.AreEqual(1f, HeroMocap.MaskWeight(HeroMocap.Mask.DrawArm, "LowerArm.L"));
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.DrawArm, "UpperArm.R"), "the bow arm is the game's");
            Assert.AreEqual(0f, HeroMocap.MaskWeight(HeroMocap.Mask.DrawArm, "Chest"));
        }

        [Test]
        public void TheGait_StepsAtTheClipsOwnTempo_NotAPatter()
        {
            // at the pace it was captured at, a loop of steps takes the clip's own length
            Assert.AreEqual(HeroMocap.RunSpeed * 0.7f, HeroMocap.GaitStride(HeroMocap.Run, 0.7f, 2f), 1e-4f);
            Assert.AreEqual(HeroMocap.SprintSpeed * 0.7f, HeroMocap.GaitStride(HeroMocap.Sprint, 0.7f, 1.8f), 1e-4f);
            Assert.AreEqual(HeroMocap.WalkSpeed * 1.37f, HeroMocap.GaitStride(HeroMocap.Walk, 1.37f, 1.5f), 1e-4f);
            // full speed (8 m/s), mostly the sprint: under two loops (four steps) a second, as a sprinter runs
            HeroMocap.GaitMix(8f, out var a, out var b, out float mix);
            float stride = Mathf.Lerp(HeroMocap.GaitStride(a, 0.7f, 2f), HeroMocap.GaitStride(b, 0.7f, 1.8f), mix);
            Assert.Less(8f / stride, 2f, "no pattering legs");
            Assert.Greater(8f / stride, 1.2f);
            Assert.AreEqual(2.5f, HeroMocap.GaitStride("Other", 1f, 2.5f), 1e-4f, "anything else: its measured stride");
        }

        [Test]
        public void BlowsInTheAir_ACutInTheLeap_AStrikeUnderTheFeet()
        {
            var s = new HeroState { verticalVelocity = 4f, somersault = -1f, attacking = true, attackT = 0.3f, attackForm = VeilForm.Sword };
            var cut = HeroMocap.Choose(s, 0f, 0);
            Assert.AreEqual(HeroMocap.AirSlash, cut.key);
            Assert.AreEqual(HeroMocap.Mask.NoHipsTurn, cut.mask, "the game turns the body: the clip has the limbs");
            Assert.Greater(cut.weight, 0.5f);
            s.attackAim = -80f;
            Assert.AreEqual(HeroMocap.DownStrike, HeroMocap.Choose(s, 0f, 0).key, "aimed down from the air: under the feet");
            s.attackForm = VeilForm.Spear;
            Assert.AreEqual(HeroMocap.DownStrike, HeroMocap.Choose(s, 0f, 0).key);
            s.attackForm = VeilForm.Whip;
            Assert.AreEqual(HeroMocap.AirSlash, HeroMocap.Choose(s, 0f, 0).key, "the whip has no strike under the feet");
            s.attackForm = VeilForm.Sword; s.attackAim = 85f;
            Assert.Less(HeroMocap.Choose(s, 0f, 0).weight, 0.15f, "aimed steeply up the procedural aim has the arm");
        }

        [Test]
        public void ABlow_LandsInTheClip_WhenItLandsInTheGame()
        {
            Assert.AreEqual(0.3f, HeroMocap.RemapToClip(0.5f, 0.5f, 0.3f), 1e-5f, "game impact -> clip impact");
            Assert.AreEqual(0f, HeroMocap.RemapToClip(0f, 0.5f, 0.3f), 1e-5f);
            Assert.AreEqual(1f, HeroMocap.RemapToClip(1f, 0.5f, 0.3f), 1e-5f);
            float a = HeroMocap.RemapToClip(0.25f, 0.5f, 0.3f), b = HeroMocap.RemapToClip(0.75f, 0.5f, 0.3f);
            Assert.Less(a, 0.3f); Assert.Greater(b, 0.3f);
        }

        [Test]
        public void TheBakedClips_AreInTheProject_OnTheHerosOwnRig()
        {
            // (the Mixamo sources stay outside git; the motion baked onto our hero is ours to keep)
            var lib = AssetDatabase.LoadAssetAtPath<HeroMocapLibrary>(LibraryPath);
            Assert.IsNotNull(lib, "the library is in the project");
            foreach (var key in new[] { HeroMocap.Idle, HeroMocap.HangIdle, HeroMocap.Mantle, HeroMocap.SlashA, HeroMocap.SlashB,
                                        HeroMocap.Stab, HeroMocap.Talk, HeroMocap.Sad, HeroMocap.Nod, HeroMocap.InjuredIdle, HeroMocap.LookAround,
                                        HeroMocap.Walk, HeroMocap.Run, HeroMocap.Sprint, HeroMocap.CrouchIdle, HeroMocap.Slide, HeroMocap.Dash,
                                        HeroMocap.HardLanding, HeroMocap.LandRoll, HeroMocap.Fall, HeroMocap.Flip, HeroMocap.WallJump,
                                        HeroMocap.HangCatch, HeroMocap.Swing, HeroMocap.SlashC, HeroMocap.ShieldBash, HeroMocap.BlockIdle,
                                        HeroMocap.Block, HeroMocap.BowDraw, HeroMocap.BowAim, HeroMocap.BowShot, HeroMocap.Hurt, HeroMocap.Stagger, HeroMocap.Turn, HeroMocap.RunStop, HeroMocap.WhipA, HeroMocap.WhipB, HeroMocap.WhipC, HeroMocap.SpearB, HeroMocap.SpearC, HeroMocap.AirSlash, HeroMocap.DownStrike, HeroMocap.Death,
                                        HeroMocap.GetUp })
            {
                Assert.IsTrue(lib.TryGet(key, out var e), key);
                Assert.Greater(e.clip.length, key == HeroMocap.JumpUp ? 0.1f : 0.4f, key);
                Assert.IsFalse(e.clip.humanMotion, key + " is baked onto the hero's own bones, not humanoid muscles");
                var paths = new System.Collections.Generic.HashSet<string>();
                foreach (var b in AnimationUtility.GetCurveBindings(e.clip)) paths.Add(b.path.Substring(b.path.LastIndexOf('/') + 1));
                Assert.Contains("UpperArm.R", new System.Collections.Generic.List<string>(paths), key + " drives his arms");
                Assert.IsTrue(paths.Contains("UpperChest") && paths.Contains("Thumb.R.2"), key + " drives the upper chest and the thumb's tip too");
                Assert.IsFalse(paths.Contains("Jaw") || paths.Contains("Hair.0.0") || paths.Contains("Cloth.0.0"), "not the face, hair or cloth");
            }
            foreach (var key in new[] { HeroMocap.SlashA, HeroMocap.SlashB, HeroMocap.SlashC, HeroMocap.Stab, HeroMocap.ShieldBash,
                                        HeroMocap.WhipA, HeroMocap.WhipB, HeroMocap.WhipC, HeroMocap.SpearB, HeroMocap.SpearC,
                                        HeroMocap.AirSlash, HeroMocap.DownStrike })
            {
                lib.TryGet(key, out var e);
                Assert.That(e.impact, Is.InRange(0.1f, 0.9f), key + " knows where its blow lands");
            }
            Assert.IsTrue(lib.TryGet(HeroMocap.Idle, out var idle) && idle.loop, "the idle loops");
            foreach (var key in new[] { HeroMocap.Walk, HeroMocap.Run, HeroMocap.Sprint })
            {
                lib.TryGet(key, out var g);
                Assert.IsTrue(g.loop, key);
                Assert.That(g.stride, Is.InRange(1f, 3.5f), key + " knows how far a loop of its steps carries him");
                Assert.That(g.sync, Is.InRange(0f, 1f), key);
            }
        }
    }
}
