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
            Assert.AreEqual(HeroMocap.SlashA, HeroMocap.Choose(s, 0f, 0).key, "no captured whip: it cuts like the sword");
            s.attackForm = VeilForm.Spear;
            var stab = HeroMocap.Choose(s, 0f, 0);
            Assert.AreEqual(HeroMocap.Stab, stab.key);
            Assert.IsTrue(stab.driven);
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
            c.sinceWallJump = 0.2f;
            Assert.AreEqual(HeroMocap.WallJump, HeroMocap.Choose(new HeroState { verticalVelocity = 10f, somersault = -1f }, c).body.key);
            Assert.IsTrue(HeroMocap.Choose(new HeroState { wallSliding = true, verticalVelocity = -2f, somersault = -1f }, c).body.IsNone, "the wall slide is procedural");
        }

        [Test]
        public void Landing_AHardFallTakesTheKnees_FromARunHeRollsOut_AnOrdinaryJumpDoesNot()
        {
            var c = HeroMocap.Context.Default;
            c.sinceLanded = 0.1f; c.landFallSpeed = 30f; c.landRunSpeed = 0f;
            Assert.AreEqual(HeroMocap.HardLanding, HeroMocap.Choose(Ground(), c).body.key);
            c.landRunSpeed = 7f;
            Assert.AreEqual(HeroMocap.LandRoll, HeroMocap.Choose(Ground(7f), c).body.key);
            c.landFallSpeed = 18f;   // (a plain jump comes down at about this)
            Assert.AreEqual(HeroMocap.Gait, HeroMocap.Choose(Ground(7f), c).body.Layer);
            c.landFallSpeed = 30f; c.sinceLanded = 3f;
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
            Assert.AreEqual(HeroMocap.Slide, sl.key); Assert.AreEqual(0.6f, sl.time01, 1e-5f);
        }

        [Test]
        public void TheWhip_TheSwingFollowsThePendulum()
        {
            var s = new HeroState { swinging = true };
            var c = HeroMocap.Context.Default; c.swingPhase = 0.25f;
            var sw = HeroMocap.Choose(s, c).body;
            Assert.AreEqual(HeroMocap.Swing, sw.key);
            Assert.AreEqual(0.25f + HeroMocap.SwingSync, sw.time01, 1e-5f);
            Assert.AreEqual(HeroMocap.Mask.NoHipsTurn, sw.mask, "the rope turns the body: the clip swings the limbs");
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
            Assert.AreEqual(HeroMocap.Mask.Upper, pick.upper.mask);
            Assert.AreEqual(HeroMocap.Gait, pick.body.Layer, "running on below the bow");
            c.drawing = true;
            Assert.AreEqual(HeroMocap.BowAim, HeroMocap.Choose(s, c).upper.key);
            c.drawing = false; s.attackT = 0.7f;
            Assert.AreEqual(HeroMocap.BowShot, HeroMocap.Choose(s, c).upper.key);
            s.attackAim = 80f;
            Assert.IsTrue(HeroMocap.Choose(s, c).upper.IsNone, "aimed steeply the procedural aim has it");

            var guard = Ground(); guard.attackForm = VeilForm.Shield;
            c = HeroMocap.Context.Default; c.shieldHeld = true;
            Assert.AreEqual(HeroMocap.BlockIdle, HeroMocap.Choose(guard, c).upper.key);
            c.sinceBlock = 0.1f;
            Assert.AreEqual(HeroMocap.Block, HeroMocap.Choose(guard, c).upper.key, "a blow on the shield jolts it");

            c = HeroMocap.Context.Default; c.sinceHurt = 0.1f;
            Assert.AreEqual(HeroMocap.Hurt, HeroMocap.Choose(Ground(), c).upper.key);
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
                                        HeroMocap.Block, HeroMocap.BowDraw, HeroMocap.BowAim, HeroMocap.BowShot, HeroMocap.Hurt, HeroMocap.Death,
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
            foreach (var key in new[] { HeroMocap.SlashA, HeroMocap.SlashB, HeroMocap.SlashC, HeroMocap.Stab, HeroMocap.ShieldBash })
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
