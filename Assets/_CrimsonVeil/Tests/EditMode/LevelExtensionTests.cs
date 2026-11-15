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
        public void TheShaft_IsAWallJumpShaft()
        {
            var d = Get("tower ledge D");
            var pillar = Get("tower pillar");
            var wall = Get("tower left wall");
            var top = Get("tower top");
            Assert.GreaterOrEqual(pillar.Bottom - d.top, 1.95f, "the hero walks in under the pillar");
            Assert.That(pillar.Left - wall.Right, Is.InRange(2.8f, 3.6f), "as wide as the first shaft (wall jumps reach across)");
            Assert.LessOrEqual(wall.Left, d.Left, "the shaft's wall stands on the ledge's side");
            Assert.AreEqual(top.top, pillar.top, 0.01f, "out over the pillar onto the top");
            Assert.Greater(wall.top, top.top + 2f, "the wall rises past the top: no way out to the left");
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
                    Assert.GreaterOrEqual(o.Bottom - p.top, 1.95f, o.name + " over " + p.name + " leaves no room to stand");
                }
            }
            var floor = Get("tower floor");
            Assert.LessOrEqual(floor.Left, 115.01f, "the tower floor joins the far side (it ends at 115)");
            Assert.GreaterOrEqual(Get("tower left wall").Bottom - floor.top, 1.95f, "walk into the tower under its wall");
        }

        [Test]
        public void TheLevel_IsLongerAndClimbsHigh()
        {
            float right = PrototypeSceneBuilder.Extension.Max(b => b.Right);
            float high = PrototypeSceneBuilder.ExtensionRoute.Select(r => Get(r.to).top).Max();
            float low = PrototypeSceneBuilder.ExtensionRoute.Select(r => Get(r.to).top).Min();
            Assert.Greater(right, 210f, "it went on to x 117 before");
            Assert.Greater(high - low, 25f, "a climb up a tower and down a well");
        }
    }
}
