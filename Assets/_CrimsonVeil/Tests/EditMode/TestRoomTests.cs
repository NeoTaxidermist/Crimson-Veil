using System.Linq;
using CrimsonVeil.EditorTools;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The test room left of the start: grey blocks to look at the hero's movement, every weapon, no enemies.</summary>
    [Category("World")]
    public class TestRoomTests
    {
        static PrototypeSceneBuilder.BlockDef B(string name) => PrototypeSceneBuilder.TestRoomBlocks.First(b => b.name == name);

        [Test]
        public void TheFloor_RunsFromTheStartToTheEndWall()
        {
            var floor = B("test floor");
            Assert.AreEqual(0f, floor.top, 1e-4f, "level with the start");
            Assert.LessOrEqual(floor.Left, PrototypeSceneBuilder.TestRoomLeft + 1e-3f);
            Assert.GreaterOrEqual(floor.Right, -14f, "it meets the start's floor (from -14) under the back wall: no gap");
            Assert.Greater(floor.Right - floor.Left, 40f, "a long flat run: walk, run, turn round, pull up, slide");
        }

        [Test]
        public void Everything_HasAPlaceToTry()
        {
            var step = B("test step"); var ledge = B("test ledge block");
            Assert.Less(step.top, 3.4f, "a step to hop up");
            Assert.Greater(ledge.top, 3.6f, "a block higher than a jump: grab its edge and climb");
            var right = B("test shaft right"); var left = B("test shaft left");
            float gap = right.Left - left.Right;
            Assert.That(gap, Is.InRange(2.6f, 3.6f), "a wall-jump shaft");
            Assert.That(right.Bottom, Is.InRange(1.95f, 3f), "walked into under its hanging wall");
            var deck = B("test deck");
            Assert.GreaterOrEqual(deck.top, HeroMocap.HardLandingHeight + 2f, "dropped off, a hard landing");
            // off the deck's left edge he falls all the way to the floor (nothing in between)
            var wall = B("test end wall");
            foreach (var b in PrototypeSceneBuilder.TestRoomBlocks)
                if (b.name != "test floor" && b.name != "test end wall" && b.name != "test deck")
                    Assert.IsTrue(b.Right <= wall.Right + 1e-3f || b.Left >= deck.Left - 1e-3f, b.name + " is under the drop off the deck");
            var tunnel = B("test tunnel");
            Assert.That(tunnel.Bottom, Is.InRange(1.3f, 1.5f), "a low passage: crawl or slide (the crouched hero is up to 1.24 m, hair and all)");
            Assert.GreaterOrEqual(PrototypeSceneBuilder.TestRoomRings.Length, 2, "whip rings to swing on");
            foreach (var r in PrototypeSceneBuilder.TestRoomRings)
                Assert.That(r.x, Is.InRange(PrototypeSceneBuilder.TestRoomLeft, PrototypeSceneBuilder.TestRoomRight));
        }

        [Test]
        public void TheRoom_KnowsWhereItIs_AndArmsTheHeroWithEveryWeapon()
        {
            var go = new GameObject("room");
            try
            {
                var room = go.AddComponent<TestRoom>();
                Assert.IsTrue(room.Contains(-30f));
                Assert.IsFalse(room.Contains(-7f), "the start is outside it");
                var weapons = System.Enum.GetValues(typeof(VeilForm)).Cast<VeilForm>().Where(f => f != VeilForm.Flow);
                CollectionAssert.AreEquivalent(weapons, TestRoom.AllWeapons);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
