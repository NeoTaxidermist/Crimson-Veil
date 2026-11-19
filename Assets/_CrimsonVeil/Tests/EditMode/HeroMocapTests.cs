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
        public void StandingStill_TheCapturedIdleComesIn_RunningStaysProcedural()
        {
            Assert.IsTrue(HeroMocap.Choose(Ground(), 0.1f, 0).IsNone, "not at once");
            Assert.AreEqual(HeroMocap.Idle, HeroMocap.Choose(Ground(), 1f, 0).key);
            Assert.IsTrue(HeroMocap.Choose(Ground(6f), 1f, 0).IsNone);
            var air = new HeroState { grounded = false };
            Assert.IsTrue(HeroMocap.Choose(air, 1f, 0).IsNone);
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
                                        HeroMocap.Stab, HeroMocap.Talk, HeroMocap.Sad, HeroMocap.Nod })
            {
                Assert.IsTrue(lib.TryGet(key, out var e), key);
                Assert.Greater(e.clip.length, 0.5f, key);
                Assert.IsFalse(e.clip.humanMotion, key + " is baked onto the hero's own bones, not humanoid muscles");
                var paths = new System.Collections.Generic.HashSet<string>();
                foreach (var b in AnimationUtility.GetCurveBindings(e.clip)) paths.Add(b.path.Substring(b.path.LastIndexOf('/') + 1));
                Assert.Contains("UpperArm.R", new System.Collections.Generic.List<string>(paths), key + " drives his arms");
                Assert.IsFalse(paths.Contains("Jaw") || paths.Contains("Hair.0.0") || paths.Contains("Cloth.0.0"), "not the face, hair or cloth");
            }
            foreach (var key in new[] { HeroMocap.SlashA, HeroMocap.SlashB, HeroMocap.Stab })
            {
                lib.TryGet(key, out var e);
                Assert.That(e.impact, Is.InRange(0.1f, 0.9f), key + " knows where its blow lands");
            }
            Assert.IsTrue(lib.TryGet(HeroMocap.Idle, out var idle) && idle.loop, "the idle loops");
        }
    }
}
