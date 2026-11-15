using CrimsonVeil.Core;
using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The rest of the body on top of the key poses: pelvis, chest, collarbones, arms, neck, wrists, toes.</summary>
    public class HeroBodyMotionTests
    {
        static HeroState Running => new HeroState { grounded = true, speed = 6f, maxSpeed = 6f };

        [Test]
        public void InTheStride_HipsAndShouldersTurnAgainstEachOther()
        {
            // a quarter into the stride the left leg leads (thighL > 0)
            var p = HeroBodyMotion.Layer(new HeroPose { thighL = 40f, thighR = -40f }, Running, 0.25f, 1f, 0f, false);
            Assert.Less(p.pelvisTurn, -3f, "the left hip comes forward with the left leg");
            Assert.Greater(p.twist, 3f, "the chest turns back: the right shoulder comes forward with the right arm");
            Assert.Greater(p.twist + p.pelvisTurn, 0f, "seen from outside the shoulders turn against the hips");
            var q = HeroBodyMotion.Layer(new HeroPose(), Running, 0.75f, 1f, 0f, false);
            Assert.Greater(q.pelvisTurn, 3f, "and the other way half a stride later");
        }

        [Test]
        public void TheCollarbonesGoWithTheArms_AndTheArmsSwingClearOfTheBody()
        {
            var still = new HeroState { grounded = true };
            var fwd = HeroBodyMotion.Layer(new HeroPose { armR = 90f }, still, 0f, 0f, 0f, false);
            var back = HeroBodyMotion.Layer(new HeroPose { armR = -40f }, still, 0f, 0f, 0f, false);
            var hang = HeroBodyMotion.Layer(new HeroPose { armR = 10f }, still, 0f, 0f, 0f, false);
            Assert.Greater(fwd.shoulderR, 5f, "the shoulder reaches forward with the arm");
            Assert.Less(back.shoulderR, 0f, "and draws back with it");
            Assert.Greater(hang.armOutR, 3f, "a hanging arm is a little off the body");
            Assert.Greater(fwd.armOutR, hang.armOutR, "an arm far forward swings out past the chest");
            Assert.Greater(back.armOutR, hang.armOutR, "and far back past the hip");
        }

        [Test]
        public void TheToesBendAsTheHeelLifts_OnlyOnTheGround()
        {
            var p = HeroBodyMotion.Layer(new HeroPose { thighL = -30f, thighR = 30f }, Running, 0.75f, 1f, 0f, false);
            Assert.Greater(p.toeL, 15f, "the trailing foot rolls over its toes");
            Assert.AreEqual(0f, p.toeR, 1e-3f, "the leading foot's toes don't bend");
            var air = HeroBodyMotion.Layer(new HeroPose { thighL = -30f }, new HeroState { grounded = false }, 0f, 0f, 0f, false);
            Assert.Less(air.toeL, 0f, "in the air the toes hang");
        }

        [Test]
        public void TheNeck_KeepsTheGazeUpAsTheBodyLeans()
        {
            var p = HeroBodyMotion.Layer(new HeroPose { spine = 20f, chest = 10f }, Running, 0f, 1f, 0f, false);
            Assert.Less(p.neck, -3f);
        }

        [Test]
        public void IntoABlow_TheBodyTurns()
        {
            var still = new HeroState { grounded = true, weaponHeld = 1f };
            var cut = HeroBodyMotion.Layer(new HeroPose { spine = 18f, chest = 14f }, still, 0f, 0f, 0f, true);
            var stand = HeroBodyMotion.Layer(new HeroPose { spine = 18f, chest = 14f }, still, 0f, 0f, 0f, false);
            Assert.Greater(cut.twist, stand.twist + 8f, "the sword shoulder turns in");
        }

        [Test]
        public void TheWrists_TrailASwingingArm_AWeaponHandLess()
        {
            Assert.Less(HeroBodyMotion.WristLag(600f, false), -10f, "an arm swinging forward leaves the hand behind");
            Assert.Greater(HeroBodyMotion.WristLag(-600f, false), 10f);
            Assert.Less(Mathf.Abs(HeroBodyMotion.WristLag(600f, true)), Mathf.Abs(HeroBodyMotion.WristLag(600f, false)) * 0.5f,
                "a hand holding a weapon is held firm (the blade goes with the forearm)");
            Assert.That(HeroBodyMotion.WristLag(1e5f, false), Is.InRange(-30f, 30f), "within what a wrist can do");
        }

        [Test]
        public void ARigJoint_HasACalibratedSecondAxis()
        {
            var bone = new GameObject("Bone").transform;
            var child = new GameObject("Child").transform;
            child.SetParent(bone, false);
            child.localPosition = new Vector3(0f, -1f, 0f);
            try
            {
                var j = RigJoint.Create(bone, Vector3.right, () => child.position, Vector3.forward)
                                .WithSecond(Vector3.forward, () => child.position, Vector3.right);
                j.Set(0f, 20f);
                Assert.Greater(child.position.x, 0.2f, "the second angle swings the child the measured way");
                j.Set(30f, 0f);
                Assert.Greater(child.position.z, 0.3f, "the main angle still works");
                Assert.AreEqual(0f, child.position.x, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(bone.gameObject);
            }
        }

        [Test]
        public void TheHeroModel_HasToes()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/Art/Hero/Hero.fbx");
            var hero = Object.Instantiate(prefab);
            try
            {
                foreach (var side in new[] { "L", "R" })
                {
                    var toe = RigJoint.FindDeep(hero.transform, "Toe." + side);
                    Assert.IsNotNull(toe, "Toe." + side);
                    Assert.AreEqual("Foot." + side, toe.parent.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(hero);
            }
        }
    }
}
