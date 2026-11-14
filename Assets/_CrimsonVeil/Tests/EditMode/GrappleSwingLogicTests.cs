using CrimsonVeil.Player;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The motor's whip swing: a pendulum that keeps momentum and launches the hero when he lets go.</summary>
    public class GrappleSwingLogicTests
    {
        static readonly Vector2 Anchor = new Vector2(0f, 6f);

        static MotorContacts At(Vector2 grip) => new MotorContacts { gripPosition = grip };

        /// <summary>Steps the motor while moving the hands exactly as the motor asks (no collisions).</summary>
        static Vector2 Run(MotorLogic m, ref Vector2 grip, float seconds, MotorInput input)
        {
            const float dt = 1f / 60f;
            for (float t = 0f; t < seconds; t += dt)
            {
                var v = m.Tick(dt, input, At(grip));
                grip += v * dt;
                if (!m.IsSwinging) break;
            }
            return grip;
        }

        [Test]
        public void Latching_KeepsTheMomentumAlongTheSwing()
        {
            var m = new MotorLogic(new MotorSettings()) { Velocity = new Vector2(8f, 0f) };
            Assert.IsTrue(m.BeginSwing(Anchor, new Vector2(0f, 1f)));
            Assert.AreEqual(5f, m.SwingLength, 1e-3f);
            Assert.AreEqual(0f, m.SwingAngle, 1e-3f, "hanging straight below");
            Assert.AreEqual(8f / 5f, m.SwingAngularVelocity, 1e-3f, "running speed becomes swing speed");
        }

        [Test]
        public void CannotLatch_OntoARingBelowOrOutOfReach()
        {
            var m = new MotorLogic(new MotorSettings());
            Assert.IsFalse(m.BeginSwing(new Vector2(0f, 0f), new Vector2(0f, 1f)), "ring below the hands");
            Assert.IsFalse(m.BeginSwing(new Vector2(0f, 20f), new Vector2(0f, 1f)), "beyond the whip's length");
        }

        [Test]
        public void Pendulum_SwingsBackAndForth_OnAFixedLength()
        {
            var m = new MotorLogic(new MotorSettings());
            var grip = MotorLogic.SwingPoint(Anchor, 5f, -0.8f);
            Assert.IsTrue(m.BeginSwing(Anchor, grip));
            float minX = grip.x, maxX = grip.x;
            const float dt = 1f / 60f;
            for (float t = 0f; t < 4f; t += dt)
            {
                var v = m.Tick(dt, new MotorInput(), At(grip));
                grip += v * dt;
                minX = Mathf.Min(minX, grip.x); maxX = Mathf.Max(maxX, grip.x);
                Assert.AreEqual(5f, Vector2.Distance(grip, Anchor), 0.05f, "the whip stays taut");
            }
            Assert.Greater(maxX, 2.5f, "it swings up the other side (little energy lost)");
            Assert.Less(minX, -3f);
        }

        [Test]
        public void LeaningIn_PumpsTheSwingHigher()
        {
            float Amplitude(float lean)
            {
                var m = new MotorLogic(new MotorSettings());
                var grip = MotorLogic.SwingPoint(Anchor, 5f, -0.3f);
                m.BeginSwing(Anchor, grip);
                float best = 0f;
                const float dt = 1f / 60f;
                for (float t = 0f; t < 5f; t += dt)
                {
                    // Lean the way the swing is going (what a player does by holding the direction of travel).
                    float dir = Mathf.Abs(m.SwingAngularVelocity) > 0.05f ? Mathf.Sign(m.SwingAngularVelocity) : 1f;
                    var v = m.Tick(dt, new MotorInput { moveX = dir * lean }, At(grip));
                    grip += v * dt;
                    best = Mathf.Max(best, Mathf.Abs(m.SwingAngle));
                }
                return best;
            }
            Assert.Greater(Amplitude(1f), Amplitude(0f) + 0.4f);
        }

        [Test]
        public void ClimbingTheWhip_ShortensItWithinLimits()
        {
            var s = new MotorSettings();
            var m = new MotorLogic(s);
            var grip = new Vector2(0f, 1f);
            m.BeginSwing(Anchor, grip);
            Run(m, ref grip, 3f, new MotorInput { aimY = 1f });
            Assert.AreEqual(s.swingMinLength, m.SwingLength, 1e-3f);
            Run(m, ref grip, 3f, new MotorInput { aimY = -1f });
            Assert.AreEqual(s.swingMaxLength, m.SwingLength, 1e-3f);
        }

        [Test]
        public void Jumping_LetsGo_WithTheSwingSpeedAndALift()
        {
            var s = new MotorSettings();
            var m = new MotorLogic(s) { Velocity = new Vector2(10f, 0f) };
            var grip = new Vector2(0f, 1f);
            m.BeginSwing(Anchor, grip);
            bool released = false;
            m.SwingReleased += () => released = true;
            m.Tick(1f / 60f, new MotorInput { jumpPressed = true }, At(grip));
            Assert.IsTrue(released);
            Assert.IsFalse(m.IsSwinging);
            Assert.Greater(m.Velocity.x, 10f, "the leap is faster than the swing (boost)");
            Assert.AreEqual(s.swingReleaseLift, m.Velocity.y, 0.5f);
            Assert.AreEqual(1, m.Facing);
        }

        [Test]
        public void TheLeap_CarriesItsSpeedThroughTheAir()
        {
            var s = new MotorSettings();
            var m = new MotorLogic(s) { Velocity = new Vector2(12f, 0f) };
            var grip = new Vector2(0f, 1f);
            m.BeginSwing(Anchor, grip);
            m.Tick(1f / 60f, new MotorInput { jumpPressed = true }, At(grip));
            for (int i = 0; i < 20; i++) m.Tick(1f / 60f, new MotorInput { moveX = 1f }, new MotorContacts());
            Assert.Greater(m.Velocity.x, s.maxRunSpeed + 3f, "momentum is kept, not clamped to the run speed");
        }

        [Test]
        public void AHit_KnocksTheHeroOffTheWhip()
        {
            var m = new MotorLogic(new MotorSettings());
            m.BeginSwing(Anchor, new Vector2(0f, 1f));
            bool ended = false;
            m.SwingEnded += () => ended = true;
            m.ApplyKnockback(new Vector2(-5f, 3f), 0.2f);
            Assert.IsFalse(m.IsSwinging);
            Assert.IsTrue(ended);
        }

        [Test]
        public void Reach_RequiresTheRingAboveTheHandsAndInRange()
        {
            Assert.IsTrue(GrapplePoint.CanReach(new Vector2(0f, 2f), new Vector2(3f, 7f), 7.5f));
            Assert.IsFalse(GrapplePoint.CanReach(new Vector2(0f, 2f), new Vector2(3f, 2.3f), 7.5f), "level with the hands");
            Assert.IsFalse(GrapplePoint.CanReach(new Vector2(0f, 2f), new Vector2(9f, 8f), 7.5f), "too far");
        }

        [Test]
        public void LevelKit_BuildsTheRing_WithModelGlintAndCatalogEntry()
        {
            var entry = CrimsonVeil.EditorTools.LevelKitCatalog.Find("GrapplePoint");
            Assert.IsNotNull(entry);
            Assert.AreEqual("Úchyt pro bič", entry.label);
            var go = CrimsonVeil.EditorTools.LevelKitFactory.GrapplePoint(new Vector3(3f, 6f, 0f));
            try
            {
                Assert.IsNotNull(go.GetComponent<GrapplePoint>());
                Assert.AreEqual(new Vector3(3f, 6f, 0f), go.transform.position, "the pivot is the ring the whip wraps round");
                Assert.IsNotNull(go.transform.Find("Glint"), "a glint shows when the whip would catch it");
                Assert.IsFalse(go.transform.Find("Glint").gameObject.activeSelf);
                var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
                Assert.Greater(renderers.Length, 1, "the modelled ring, chain and beam");
                Assert.IsTrue(System.Array.Exists(renderers, r => System.Array.Exists(r.sharedMaterials, m => m != null && m.name.Contains("Crimson"))),
                    "a crimson rag marks it in the grey world");
                Assert.IsEmpty(go.GetComponentsInChildren<Collider>(), "nothing to bump into: the hero swings freely under it");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void FindBest_PrefersTheRingAhead()
        {
            var ahead = new GameObject("Ahead").AddComponent<GrapplePoint>();
            var behind = new GameObject("Behind").AddComponent<GrapplePoint>();
            try
            {
                ahead.transform.position = new Vector3(3f, 6f, 0f);
                behind.transform.position = new Vector3(-2.5f, 6f, 0f);
                Assert.AreSame(ahead, GrapplePoint.FindBest(new Vector2(0f, 2f), 1, 0));
                Assert.AreSame(behind, GrapplePoint.FindBest(new Vector2(0f, 2f), -1, 0));
            }
            finally
            {
                Object.DestroyImmediate(ahead.gameObject);
                Object.DestroyImmediate(behind.gameObject);
            }
        }
    }
}
