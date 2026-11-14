using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The double jump's somersault and the scarf winding round the body.</summary>
    public class SomersaultTests
    {
        [Test]
        public void TurnsOnceForward_AndLandsUpright()
        {
            Assert.AreEqual(0f, Somersault.Angle(-1f, 1), 1e-4f, "not flipping");
            Assert.AreEqual(-180f, Somersault.Angle(0.5f, 1), 1e-3f, "facing right: half way, upside down, turning clockwise");
            Assert.AreEqual(180f, Somersault.Angle(0.5f, -1), 1e-3f, "facing left: the other way");
            Assert.AreEqual(-360f, Somersault.Angle(0.9999f, 1), 0.1f, "a full turn by the end");
            float prev = 0f;
            for (int i = 1; i < 100; i++)
            {
                float a = Somersault.Angle(i / 100f, 1);
                Assert.LessOrEqual(a, prev + 1e-4f, "never turns back");
                prev = a;
            }
        }

        [Test]
        public void TucksThroughTheMiddle()
        {
            Assert.AreEqual(0f, Somersault.Tuck(0f), 1e-4f, "0 = unset: no tuck");
            Assert.Greater(Somersault.Tuck(0.5f), 0.95f);
            Assert.Less(Somersault.Tuck(0.97f), 0.15f, "opened out to land");
        }

        [Test]
        public void ScarfWrap_WindsInFast_UnwindsAfter()
        {
            Assert.AreEqual(0f, Somersault.Wrap(-1f, -1f), 1e-4f);
            Assert.AreEqual(1f, Somersault.Wrap(0.3f, -1f), 1e-4f);
            Assert.Greater(Somersault.Wrap(-1f, 0.1f), 0.5f, "still wound just after the turn");
            Assert.AreEqual(0f, Somersault.Wrap(-1f, 0.5f), 1e-4f, "unwound");
        }

        [Test]
        public void Swirl_CarriesClothRoundTheBody_OntoARing()
        {
            var c = Vector2.zero;
            // Clockwise (negative speed): a point to the right of the centre is carried downwards.
            var v = VeilSimulation.SwirlVelocity(new Vector2(0.5f, 0f), c, -10f, 0.5f);
            Assert.Less(v.y, -4f);
            Assert.AreEqual(0f, v.x, 1e-3f, "on the ring: no pull in or out");
            Assert.Less(VeilSimulation.SwirlVelocity(new Vector2(2f, 0f), c, 0f, 0.5f).x, 0f, "far out: drawn in");
            Assert.Greater(VeilSimulation.SwirlVelocity(new Vector2(0.1f, 0f), c, 0f, 0.5f).x, 0f, "too close: pushed out");
        }

        [Test]
        public void ScarfWindsRoundTheHero_DuringTheSomersault()
        {
            var settings = new VeilSettings();
            var sim = new VeilSimulation(settings, new Vector3(0f, 1.5f, 0f));
            for (int i = 0; i < 120; i++) sim.Step(1f / 60f, new Vector3(0f, 1.5f, 0f)); // trailing loose behind
            var centre = new Vector2(0f, 0.95f);
            sim.SwirlCenter = centre;
            sim.SwirlSpeed = -2f * Mathf.PI / Somersault.Duration * 1.2f;
            sim.SwirlWeight = 1f;
            int n = sim.Count;
            float swept = 0f;
            Vector2 prev = (Vector2)sim.Positions[n - 1] - centre;
            int frames = Mathf.RoundToInt(Somersault.Duration * 60f) - 1;
            float worstMean = 0f;
            for (int i = 1; i <= frames; i++)
            {
                // The neck turns round the hips with the body, as in the game.
                float angle = Somersault.Angle((float)i / (frames + 1), 1);
                Vector2 neck = centre + (Vector2)(Quaternion.Euler(0f, 0f, angle) * new Vector3(0f, 0.55f, 0f));
                sim.Step(1f / 60f, neck);
                Vector2 tip = (Vector2)sim.Positions[n - 1] - centre;
                swept += Vector2.SignedAngle(prev, tip);
                prev = tip;
                if (i > frames / 2)
                {
                    float sum = 0f;
                    for (int k = n / 2; k < n; k++) sum += Vector2.Distance(sim.Positions[k], centre);
                    worstMean = Mathf.Max(worstMean, sum / (n - n / 2));
                }
            }
            Assert.Less(swept, -180f, "the end of the scarf goes round the body with it (clockwise), trailing a little");
            Assert.Less(worstMean, sim.SwirlRadius + 0.6f, "the tail stays wound close round the body, not flung out");
        }
    }
}
