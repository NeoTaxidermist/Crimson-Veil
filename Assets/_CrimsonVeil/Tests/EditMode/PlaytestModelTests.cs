using CrimsonVeil.World;
using NUnit.Framework;

namespace CrimsonVeil.Tests
{
    /// <summary>The playtest measurement: time into each section, deaths in each, the whole run.</summary>
    [Category("UI")]
    public class PlaytestModelTests
    {
        static PlaytestModel Run() => new PlaytestModel(new[]
        {
            new PlaytestModel.Section("A", -100f), new PlaytestModel.Section("B", 10f), new PlaytestModel.Section("C", 20f),
        }, 30f);

        [Test]
        public void EachSection_IsTimedFromWhenItIsFirstReached()
        {
            var m = Run();
            m.Tick(0f, 0f);
            m.Tick(40f, 12f);
            m.Tick(50f, 5f);      // back into A: no re-entry
            m.Tick(70f, 25f);
            Assert.AreEqual(0f, m.Records[0].enteredAt);
            Assert.AreEqual(40f, m.Records[0].Duration(70f), 1e-4f);
            Assert.AreEqual(40f, m.Records[1].enteredAt);
            Assert.AreEqual(30f, m.Records[1].Duration(70f), 1e-4f, "until C was reached, walking back included");
            Assert.AreEqual(2, m.Current);
            Assert.IsFalse(m.Finished);
        }

        [Test]
        public void TheRun_EndsAtTheEnd()
        {
            var m = Run();
            int ended = 0;
            m.Entered += s => { if (s == -1) ended++; };
            m.Tick(0f, 0f);
            m.Tick(100f, 31f);
            Assert.IsTrue(m.Finished);
            Assert.AreEqual(100f, m.FinishedAt);
            Assert.AreEqual(1, ended);
            m.Tick(200f, 40f);
            Assert.AreEqual(100f, m.FinishedAt, "the clock stops at the end");
            StringAssert.Contains("Dohráno za 01:40", m.Report("x"));
        }

        [Test]
        public void Deaths_CountInTheSectionWhereTheyHappen()
        {
            var m = Run();
            m.Tick(0f, 0f);
            m.Died(2f);
            m.Tick(10f, 15f);
            m.Died(15f);
            m.Died(16f);
            Assert.AreEqual(3, m.Deaths);
            Assert.AreEqual(1, m.Records[0].deaths);
            Assert.AreEqual(2, m.Records[1].deaths);
        }

        [Test]
        public void TheReport_IsATableToRead()
        {
            var m = Run();
            m.Tick(0f, 0f);
            m.Tick(65f, 12f);
            m.Died(12f);
            var r = m.Report("Crimson Veil - playtest");
            StringAssert.Contains("Nedohráno", r);
            StringAssert.Contains("Smrti celkem: 1", r);
            StringAssert.Contains("01:05", r, "B began at 1:05");
            Assert.AreEqual("12:03", PlaytestModel.Clock(723f));
        }

        [Test]
        public void ThePrototypeLevel_HasItsSectionsInOrder()
        {
            var s = PlaytestModel.PrototypeSections;
            Assert.Greater(s.Length, 10);
            for (int i = 1; i < s.Length; i++) Assert.Greater(s[i].fromX, s[i - 1].fromX);
            var m = new PlaytestModel(s, 336f);
            Assert.AreEqual("Zvonice", s[m.SectionAt(230f)].name);
            Assert.AreEqual("Propast", s[m.SectionAt(296f)].name);
        }
    }
}
