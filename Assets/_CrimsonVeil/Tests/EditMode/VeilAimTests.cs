using System.Collections.Generic;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Aiming without a mouse: eight directions from the keys / stick and a gentle aim assist.</summary>
    public class VeilAimTests
    {
        [Test]
        public void Keys_GiveEightDirections()
        {
            var sword = VeilForm.Sword;
            Assert.AreEqual(0f, VeilAim.Direction(0f, 0f, false, sword));
            Assert.AreEqual(90f, VeilAim.Direction(0f, 1f, false, sword), "up");
            Assert.AreEqual(45f, VeilAim.Direction(1f, 1f, false, sword), "up and ahead");
            Assert.AreEqual(-90f, VeilAim.Direction(0f, -1f, true, sword), "down, in the air: under the feet");
            Assert.AreEqual(-45f, VeilAim.Direction(-1f, -1f, true, sword), "down and ahead, in the air");
            Assert.AreEqual(0f, VeilAim.Direction(0f, -1f, false, sword), "a sword doesn't cut into the floor");
            Assert.AreEqual(-45f, VeilAim.Direction(0f, -1f, false, VeilForm.Bow), "the bow can shoot down a slope");
        }

        [Test]
        public void TheShield_IsNotAimed()
        {
            Assert.AreEqual(0f, VeilAim.Limit(VeilForm.Shield));
            foreach (var f in new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Bow })
                Assert.AreEqual(90f, VeilAim.Limit(f), f.ToString());
        }

        [Test]
        public void Assist_TurnsOntoATargetInTheCone()
        {
            var origin = new Vector2(0f, 1.5f);
            var targets = new List<Vector2> { new Vector2(2f, 2.6f) }; // ~29° up: aimed "ahead" it is too far off
            Assert.AreEqual(0f, VeilAim.Assist(0f, origin, 1, targets, 4f, 90f), 1e-3f, "outside the cone: no help");
            targets[0] = new Vector2(2f, 2.1f); // ~17° up
            Assert.AreEqual(Mathf.Atan2(0.6f, 2f) * Mathf.Rad2Deg, VeilAim.Assist(0f, origin, 1, targets, 4f, 90f), 1e-2f, "in the cone: on the target");
        }

        [Test]
        public void Assist_IgnoresWhatIsBehindOrOutOfReach()
        {
            var origin = Vector2.zero;
            var targets = new List<Vector2> { new Vector2(-2f, 0.2f), new Vector2(9f, 0.5f) };
            Assert.AreEqual(0f, VeilAim.Assist(0f, origin, 1, targets, 3f, 90f), 1e-3f);
            // facing left, the one on the left is in front
            Assert.AreNotEqual(0f, VeilAim.Assist(0f, origin, -1, targets, 3f, 90f));
        }

        [Test]
        public void Assist_PrefersTheTargetOnTheLine()
        {
            var origin = Vector2.zero;
            var targets = new List<Vector2> { new Vector2(2f, 0.6f), new Vector2(2.5f, 0.05f) };
            float a = VeilAim.Assist(0f, origin, 1, targets, 4f, 90f);
            Assert.Less(Mathf.Abs(a), 3f, "the one straight ahead, not the one above the line");
        }

        [Test]
        public void AnAimedSwing_TurnsTheArm()
        {
            var basePose = new HeroPose();
            var level = HeroAttackChoreography.Apply(basePose, VeilForm.Sword, 0.5f, 0f, 0f);
            var up = HeroAttackChoreography.Apply(basePose, VeilForm.Sword, 0.5f, 0f, 90f);
            var down = HeroAttackChoreography.Apply(basePose, VeilForm.Sword, 0.5f, 0f, -90f);
            Assert.Greater(up.armR, level.armR + 60f, "aimed up: a rising cut ends over the head");
            Assert.Less(up.chest, level.chest, "the body leans back into a rising cut");
            Assert.Less(down.armR + down.elbowR, 25f, "aimed down: the point goes under the feet");
            Assert.Greater(down.kneeL, level.kneeL + 20f, "the knees are drawn up for a strike under the feet");
        }

        [Test]
        public void AnArcherAimingUp_RaisesBothArmsAndLeansBack()
        {
            var basePose = new HeroPose();
            var level = HeroAttackChoreography.Apply(basePose, VeilForm.Bow, 0.3f, 0f, 0f);
            var up = HeroAttackChoreography.Apply(basePose, VeilForm.Bow, 0.3f, 0f, 90f);
            var down = HeroAttackChoreography.Apply(basePose, VeilForm.Bow, 0.3f, 0f, -45f);
            Assert.Greater(up.armR, level.armR + 50f, "the bow arm rises along the arrow");
            Assert.Greater(up.armL, level.armL + 50f, "and the drawing arm with it");
            Assert.Less(up.spine + up.chest, level.spine + level.chest - 10f, "the body leans back");
            Assert.Greater(up.head, level.head, "eyes up");
            Assert.Less(down.armR, level.armR, "aiming down the arms go down");
            Assert.Greater(down.spine + down.chest, level.spine + level.chest, "and the body leans over");
        }

        [Test]
        public void TheAim_ComesInOverTheWindUp()
        {
            var basePose = new HeroPose();
            var start = HeroAttackChoreography.Apply(basePose, VeilForm.Sword, 0f, 0f, 90f);
            var level = HeroAttackChoreography.Apply(basePose, VeilForm.Sword, 0f, 0f, 0f);
            Assert.AreEqual(level.armR, start.armR, 1e-3f, "no jump at the start of the blow");
        }
    }
}
