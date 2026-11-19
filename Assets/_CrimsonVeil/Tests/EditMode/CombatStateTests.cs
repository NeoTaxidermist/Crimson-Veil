using CrimsonVeil.Combat;
using NUnit.Framework;

namespace CrimsonVeil.Tests
{
    /// <summary>The hero's sense of a fight: a hostile after him in sight, blows exchanged, and how it ends.</summary>
    public class CombatStateTests
    {
        [Test]
        public void AHostileAfterHimInSight_IsAFight_ThatOutlastsItBriefly()
        {
            var c = new CombatState();
            Assert.IsFalse(c.InCombat);
            c.Tick(0.1f, engagedInSight: true, anyNear: true);
            Assert.IsTrue(c.InCombat);
            c.Tick(CombatState.EndAfter - 0.2f, false, true);
            Assert.IsTrue(c.InCombat, "a moment out of sight: still a fight (no flicker)");
            c.Tick(0.4f, false, true);
            Assert.IsFalse(c.InCombat, "no one after him for a while: over");
        }

        [Test]
        public void AnExchangeOfBlows_IsAFight_EvenWithNoOneInSight()
        {
            var c = new CombatState();
            c.Exchange();
            c.Tick(0.1f, false, true);
            Assert.IsTrue(c.InCombat);
            c.Tick(CombatState.ExchangeMemory, false, true);
            Assert.IsFalse(c.InCombat);
        }

        [Test]
        public void WithNoLivingHostileNear_TheFightIsOverAtOnce()
        {
            var c = new CombatState();
            c.Tick(0.1f, true, true);
            c.Exchange();
            c.Tick(0.05f, false, anyNear: false);
            Assert.IsFalse(c.InCombat, "all fallen (or he got away): the rings are his again at once");
        }

        [Test]
        public void Reset_EndsIt()
        {
            var c = new CombatState();
            c.Tick(0.1f, true, true);
            c.Reset();
            Assert.IsFalse(c.InCombat);
        }
    }
}
