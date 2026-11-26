using System.Linq;
using CrimsonVeil.EditorTools;
using NUnit.Framework;
using UnityEngine;
using B = CrimsonVeil.EditorTools.PrototypeSceneBuilder.BlockDef;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The level's extension (the tower, the ramparts, the well, the climb out) can be played through: every step of
    /// the main way is within the hero's reach (a jump is 3.4 m, a grabbed lip up to 2.1 m over the feet, a double
    /// jump and a dash carry ~6 m), and there is room to stand everywhere on it.
    /// </summary>
    [Category("World")]
    public class LevelExtensionTests
    {
        static B Get(string name)
        {
            var found = PrototypeSceneBuilder.Extension.Where(b => b.name == name).ToArray();
            Assert.AreEqual(1, found.Length, name);
            return found[0];
        }

        static float Gap(B a, B b) => Mathf.Max(0f, Mathf.Max(b.Left - a.Right, a.Left - b.Right));

        static bool Covered(float x0, float x1, float top) =>
            PrototypeSceneBuilder.Extension.Any(b => Mathf.Abs(b.top - top) < 0.05f && b.Left <= x0 + 1e-3f && b.Right >= x1 - 1e-3f);

        [Test]
        public void EveryStepOfTheWay_IsWithinReach()
        {
            foreach (var (from, to, how) in PrototypeSceneBuilder.ExtensionRoute)
            {
                var a = Get(from);
                var b = Get(to);
                float rise = b.top - a.top, gap = Gap(a, b);
                string step = from + " -> " + to + " (" + how + "): rise " + rise + ", gap " + gap;
                switch (how)
                {
                    case "climb":
                        Assert.That(rise, Is.InRange(0.5f, 4.6f), step);
                        Assert.LessOrEqual(gap, 3f, step);
                        // a ledge floating over the hero's platform is jumped at from under it: the head hits its underside
                        // before the hands find the lip. Either it is a solid step from his ground, or there's a gap to jump across.
                        Assert.IsTrue(b.Bottom <= a.top + 0.05f || gap >= 1.5f, step + ": a floating ledge right over him can't be grabbed");
                        break;
                    case "jump": Assert.LessOrEqual(Mathf.Abs(rise), 2f, step); Assert.LessOrEqual(gap, 4.5f, step); break;
                    case "double": Assert.LessOrEqual(rise, 1.5f, step); Assert.LessOrEqual(gap, 6.5f, step); break;
                    case "drop": Assert.Less(rise, 0f, step); Assert.LessOrEqual(gap, 5f, step); break;
                    case "walk":
                        Assert.AreEqual(a.top, b.top, 0.05f, step);
                        if (gap > 0f) Assert.IsTrue(Covered(Mathf.Min(a.Right, b.Right), Mathf.Max(a.Left, b.Left), a.top), step + ": nothing to walk on between");
                        break;
                    case "shaft": break; // see TheShaft_IsAWallJumpShaft
                    default: Assert.Fail("unknown way " + how); break;
                }
            }
        }

        [Test]
        public void EveryShaft_IsAWallJumpShaft()
        {
            Assert.AreEqual(PrototypeSceneBuilder.ExtensionRoute.Count(r => r.how == "shaft"), PrototypeSceneBuilder.ExtensionShafts.Length);
            foreach (var (ledgeName, pillarName, wallName, exitName) in PrototypeSceneBuilder.ExtensionShafts)
            {
                var ledge = Get(ledgeName);
                var pillar = Get(pillarName);
                var wall = Get(wallName);
                var exit = Get(exitName);
                string s = ledgeName + " shaft: ";
                Assert.IsTrue(PrototypeSceneBuilder.ExtensionRoute.Any(r => r.from == ledgeName && r.to == exitName && r.how == "shaft"), s + "on the way");
                Assert.GreaterOrEqual(pillar.Bottom - ledge.top, 1.95f, s + "the hero walks in under the pillar");
                Assert.Less(pillar.Bottom - ledge.top, 3.5f, s + "but no higher: the shaft starts right there");
                float width = pillar.Left > wall.Right ? pillar.Left - wall.Right : wall.Left - pillar.Right;
                Assert.That(width, Is.InRange(2.8f, 3.6f), s + "as wide as the first shaft (wall jumps reach across)");
                Assert.IsTrue(wall.Left <= ledge.Left + 1e-3f || wall.Right >= ledge.Right - 1e-3f, s + "the wall stands at the ledge's end");
                Assert.GreaterOrEqual(wall.Bottom, float.MinValue);
                Assert.LessOrEqual(wall.Bottom, ledge.top + 0.05f, s + "the wall reaches down to the ledge");
                Assert.AreEqual(exit.top, pillar.top, 0.01f, s + "out over the pillar or the wall's top at the same height");
                Assert.GreaterOrEqual(wall.top, exit.top - 0.01f, s + "the wall rises to the top");
            }
        }

        [Test]
        public void ThereIsRoomToStand_OnEveryPlatformOfTheWay()
        {
            var names = PrototypeSceneBuilder.ExtensionRoute.SelectMany(r => new[] { r.from, r.to }).Distinct();
            foreach (var n in names)
            {
                var p = Get(n);
                foreach (var o in PrototypeSceneBuilder.Extension)
                {
                    if (o.name == p.name || o.Bottom <= p.top) continue;
                    if (o.Right <= p.Left + 1e-3f || o.Left >= p.Right - 1e-3f) continue;   // not over it
                    if (PrototypeSceneBuilder.ExtensionTunnels.Any(t => t.roof == o.name && t.ground == p.name)) continue;   // crawled under (see TheTunnels_...)
                    Assert.GreaterOrEqual(o.Bottom - p.top, 1.95f, o.name + " over " + p.name + " leaves no room to stand");
                }
            }
            var floor = Get("tower floor");
            Assert.LessOrEqual(floor.Left, 115.01f, "the tower floor joins the far side (it ends at 115)");
            Assert.GreaterOrEqual(Get("tower left wall").Bottom - floor.top, 1.95f, "walk into the tower under its wall");
        }

        [Test]
        public void TheTunnels_MustBeCrawledOrSlid()
        {
            Assert.GreaterOrEqual(PrototypeSceneBuilder.ExtensionTunnels.Length, 2);
            foreach (var (roofName, groundName) in PrototypeSceneBuilder.ExtensionTunnels)
            {
                var roof = Get(roofName);
                var ground = Get(groundName);
                float gap = roof.Bottom - ground.top;
                Assert.That(gap, Is.InRange(1.3f, 1.6f), roofName + ": room for the crouched hero, hair and all (1.24 m), not for a standing one (1.8 m)");
                Assert.Greater(roof.top - ground.top, 6.5f, roofName + ": too high to jump over (even with the double jump)");
                Assert.IsTrue(roof.Left > ground.Left + 0.5f && roof.Right < ground.Right - 0.5f, roofName + ": over its ground, room to stand at both ends");
            }
        }

        [Test]
        public void TheLevel_IsLongerAndClimbsHigh()
        {
            float right = PrototypeSceneBuilder.Extension.Max(b => b.Right);
            float high = PrototypeSceneBuilder.ExtensionRoute.Select(r => Get(r.to).top).Max();
            float low = PrototypeSceneBuilder.ExtensionRoute.Select(r => Get(r.to).top).Min();
            Assert.Greater(right, 330f, "it went on to x 117, then to 224 before");
            Assert.Greater(high - low, 50f, "up a tower and a belfry, along a ridge, down a well and a chasm");
            Assert.Greater(high, 40f);
            Assert.Less(low, -10f);
        }
    }
}
