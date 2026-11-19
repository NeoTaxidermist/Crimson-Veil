using CrimsonVeil.Veil;
using NUnit.Framework;

namespace CrimsonVeil.Tests
{
    /// <summary>The chain of blows: positions across weapons, the reset after a pause, the finisher, variety, the morph strike.</summary>
    public class VeilComboTests
    {
        static VeilCombo.Blow Strike(VeilCombo c, VeilForm form, float pauseAfter = 0.1f)
        {
            var b = c.Begin(form);
            c.Tick(0.3f);   // the blow plays (no time counts towards the pause while it does)
            c.End();
            c.Tick(pauseAfter);
            return b;
        }

        [Test]
        public void ThreeBlows_TheThirdIsTheFinisher_ThenItStartsAgain()
        {
            var c = new VeilCombo();
            var a = Strike(c, VeilForm.Sword);
            var b = Strike(c, VeilForm.Sword);
            var f = Strike(c, VeilForm.Sword);
            Assert.AreEqual(0, a.step); Assert.AreEqual(1, b.step); Assert.AreEqual(2, f.step);
            Assert.IsFalse(a.finisher || b.finisher);
            Assert.IsTrue(f.finisher);
            Assert.AreEqual(1f, a.DamageScale, 1e-5f, "an ordinary blow");
            Assert.AreEqual(VeilCombo.FinisherDamage, f.DamageScale, 1e-5f, "the finisher hits harder");
            Assert.Greater(f.KnockbackScale, 1f, "and throws further");
            Assert.Greater(f.ExtraRecovery, 0f, "and leaves the body longer to recover");
            Assert.AreEqual(0, Strike(c, VeilForm.Sword).step, "after the finisher a new chain");
        }

        [Test]
        public void APause_StartsTheChainAgain_SoALoneBlowIsAlwaysTheFirst()
        {
            var c = new VeilCombo();
            Strike(c, VeilForm.Sword, VeilCombo.ResetAfter + 0.1f);
            Assert.AreEqual(0, Strike(c, VeilForm.Sword, VeilCombo.ResetAfter + 0.1f).step);
            Assert.AreEqual(0, Strike(c, VeilForm.Sword).step);
            Assert.AreEqual(1, Strike(c, VeilForm.Sword).step, "in rhythm it goes on");
        }

        [Test]
        public void TheChainGoesOnAcrossWeapons_AndTheFinisherOfTheLastOne()
        {
            var c = new VeilCombo();
            Strike(c, VeilForm.Sword);
            Strike(c, VeilForm.Sword);
            var f = Strike(c, VeilForm.Whip);
            Assert.IsTrue(f.finisher, "sword, sword, whip: one combo, the whip's finisher");
            Assert.AreEqual(VeilForm.Whip, f.form);
            Assert.AreEqual(2, f.variety);
            Assert.IsTrue(f.morph, "the whip poured into mid-chain");
        }

        [Test]
        public void TheMoreWeaponsInTheChain_TheHarderTheFinisher()
        {
            float Finisher(params VeilForm[] forms)
            {
                var c = new VeilCombo();
                VeilCombo.Blow b = default;
                foreach (var f in forms) b = Strike(c, f);
                Assert.IsTrue(b.finisher);
                return b.DamageScale;
            }
            float one = Finisher(VeilForm.Sword, VeilForm.Sword, VeilForm.Sword);
            float two = Finisher(VeilForm.Whip, VeilForm.Whip, VeilForm.Whip) ;
            float mixed = Finisher(VeilForm.Sword, VeilForm.Sword, VeilForm.Spear);
            float three = Finisher(VeilForm.Sword, VeilForm.Whip, VeilForm.Spear);
            Assert.AreEqual(one, two, 1e-5f, "one weapon throughout: the plain finisher");
            Assert.Greater(mixed, one, "two weapons: harder");
            Assert.Greater(three, mixed * 1.05f, "three: harder still (the morph strike adds to it too)");
        }

        [Test]
        public void TheMorphStrike_IsQuickerAndAToucherHarder_ButNotTheFirstBlow()
        {
            var c = new VeilCombo();
            var first = Strike(c, VeilForm.Spear);
            Assert.IsFalse(first.morph, "a chain opened with the spear is no morph");
            var m = Strike(c, VeilForm.Sword);
            Assert.IsTrue(m.morph);
            Assert.AreEqual(VeilCombo.MorphSpeed, m.Speed, 1e-5f);
            Assert.AreEqual(VeilCombo.MorphDamage, m.DamageScale, 1e-5f);
            Assert.IsFalse(Strike(c, VeilForm.Sword).morph, "the same weapon again is no morph");

            var bow = new VeilCombo();
            Strike(bow, VeilForm.Sword);
            Assert.AreEqual(1f, Strike(bow, VeilForm.Bow).Speed, "the bow keeps its draw");
        }

        [Test]
        public void ThePauseCountsOnlyAfterTheBlow()
        {
            var c = new VeilCombo();
            c.Begin(VeilForm.Whip);
            c.Tick(VeilCombo.ResetAfter * 2f);   // a long blow, no pause
            c.End();
            Assert.AreEqual(1, c.Begin(VeilForm.Whip).step);
            c.Reset();
            Assert.AreEqual(0, c.Step);
            Assert.AreEqual(0, c.Begin(VeilForm.Sword).step);
        }
    }
}
