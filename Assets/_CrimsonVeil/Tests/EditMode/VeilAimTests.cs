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
            Assert.Greater(up.armR, level.armR + 60f, "aimed up: the cut goes up");
            Assert.Less(down.armR, level.armR - 60f, "aimed down: the cut goes under");
            var bow = HeroAttackChoreography.Apply(basePose, VeilForm.Bow, 0.5f, 0f, 0f);
            var bowUp = HeroAttackChoreography.Apply(basePose, VeilForm.Bow, 0.5f, 0f, 60f);
            Assert.AreEqual(bow.armR, bowUp.armR, 1e-3f, "the bow turns itself, the arm stays level");
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
