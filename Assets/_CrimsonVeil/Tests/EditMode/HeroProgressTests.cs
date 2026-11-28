using CrimsonVeil.Combat;
using CrimsonVeil.Veil;
using NUnit.Framework;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// What the hero gathers (v0.110): crimson buys three upgrades of rising cost up to level 3; four blows that land
    /// tie the knot a wound is bound with; four shards make a life; a longer scarf makes every weapon cost less of it.
    /// </summary>
    [Category("Combat")]
    public class HeroProgressTests
    {
        [SetUp] public void SetUp() => HeroProgress.Reset();
        [TearDown] public void TearDown() => HeroProgress.Reset();

        [Test]
        public void Crimson_BuysUpgrades_AtRisingCost_UpToTheTop()
        {
            Assert.IsFalse(HeroProgress.Buy(HeroProgress.Upgrade.Scarf), "nothing to pay with");
            HeroProgress.AddCrimson(200);
            Assert.AreEqual(20, HeroProgress.Cost(HeroProgress.Upgrade.Scarf));
            Assert.IsTrue(HeroProgress.Buy(HeroProgress.Upgrade.Scarf));
            Assert.AreEqual(180, HeroProgress.Crimson);
            Assert.AreEqual(45, HeroProgress.Cost(HeroProgress.Upgrade.Scarf), "dearer each level");
            Assert.IsTrue(HeroProgress.Buy(HeroProgress.Upgrade.Scarf));
            Assert.IsTrue(HeroProgress.Buy(HeroProgress.Upgrade.Scarf));
            Assert.AreEqual(HeroProgress.MaxLevel, HeroProgress.Level(HeroProgress.Upgrade.Scarf));
            Assert.AreEqual(-1, HeroProgress.Cost(HeroProgress.Upgrade.Scarf));
            Assert.IsFalse(HeroProgress.Buy(HeroProgress.Upgrade.Scarf), "no higher");
            Assert.AreEqual(200 - 20 - 45 - 80, HeroProgress.Crimson);
            Assert.AreEqual(1.75f, HeroProgress.ScarfCapacity, 1e-4f);
        }

        [Test]
        public void TheUpgrades_DoWhatTheySay()
        {
            HeroProgress.AddCrimson(100);
            Assert.AreEqual(1f, HeroProgress.HealAmount);
            Assert.AreEqual(1f, HeroProgress.DamageScale);
            HeroProgress.Buy(HeroProgress.Upgrade.Bandage);
            HeroProgress.Buy(HeroProgress.Upgrade.Blade);
            Assert.AreEqual(1.5f, HeroProgress.HealAmount, 1e-4f, "a stronger binding heals half a life more");
            Assert.AreEqual(1.15f, HeroProgress.DamageScale, 1e-4f, "a keener veil");
        }

        [Test]
        public void FourBlowsThatLand_TieTheKnot_OneWoundUntiesIt()
        {
            for (int i = 0; i < 3; i++) HeroProgress.BlowLanded();
            Assert.IsFalse(HeroProgress.SpendMend(), "three blows: not yet");
            HeroProgress.BlowLanded();
            Assert.AreEqual(1f, HeroProgress.Mend, 1e-4f);
            HeroProgress.BlowLanded();
            Assert.AreEqual(1f, HeroProgress.Mend, 1e-4f, "one knot at most");
            Assert.IsTrue(HeroProgress.SpendMend());
            Assert.AreEqual(0f, HeroProgress.Mend);
        }

        [Test]
        public void FourShards_MakeALife()
        {
            for (int i = 0; i < 3; i++) Assert.IsFalse(HeroProgress.AddShard());
            Assert.IsTrue(HeroProgress.AddShard(), "the fourth completes it");
            Assert.AreEqual(1, HeroProgress.ExtraLives);
        }

        [Test]
        public void ALongerScarf_EveryWeaponCostsLessOfIt()
        {
            var c = new VeilCharge { Capacity = 1.25f };
            Assert.IsTrue(c.TryCast(VeilCasting.Cost(VeilForm.Spear)));
            Assert.AreEqual(1f - VeilCasting.Cost(VeilForm.Spear) / 1.25f, c.Value, 1e-4f);
            var plain = new VeilCharge();
            plain.TryCast(VeilCasting.Cost(VeilForm.Spear));
            Assert.Greater(c.Value, plain.Value);
        }

        [Test]
        public void DyingTakesAllTheCrimsonHeCarries()
        {
            HeroProgress.AddCrimson(17);
            Assert.AreEqual(17, HeroProgress.TakeAllCrimson());
            Assert.AreEqual(0, HeroProgress.Crimson);
        }
    }
}
