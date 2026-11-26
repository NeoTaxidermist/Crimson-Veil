using CrimsonVeil.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The hero's hair in motion (<see cref="SpringBones"/> with its styling on): each lock keeps its place in the
    /// hairstyle, leans with the wind of his motion and flutters at the tip.
    /// </summary>
    [Category("Hero")]
    public class HeroHairTests
    {
        [Test]
        public void ALock_LeansWithTheWind_MoreTheFasterHeGoes_ButOnlySoFar()
        {
            Assert.AreEqual(0f, SpringBones.LeanAngle(0f, 7f, 50f), 1e-5f, "standing still: as it is combed");
            float walk = SpringBones.LeanAngle(2.5f, 7f, 50f), run = SpringBones.LeanAngle(8f, 7f, 50f), dash = SpringBones.LeanAngle(19f, 7f, 50f);
            Assert.That(walk, Is.InRange(10f, 20f), "a walk: a little");
            Assert.That(run, Is.InRange(28f, 40f), "a run: swept back");
            Assert.Greater(dash, run);
            Assert.Less(dash, 50f, "never past its limit, however fast");
            Assert.AreEqual(0f, SpringBones.LeanAngle(8f, 0f, 50f), 1e-5f, "not styled: no lean");
        }

        [Test]
        public void ALock_TurnsTowardsWhereTheWindBlows_NeverIntoTheSkull()
        {
            Vector3 up = Vector3.up * 0.05f, wind = Vector3.left * 8f;
            // a lock on the crown, the wind from in front: it leans back, by the angle asked and at its own length
            Vector3 leaned = SpringBones.Leaned(up, wind, 30f, Vector3.up);
            Assert.AreEqual(30f, Vector3.Angle(up, leaned), 0.01f);
            Assert.AreEqual(0.05f, leaned.magnitude, 1e-5f);
            Assert.Less(leaned.x, 0f, "towards where the wind blows");
            // never past the wind's own direction
            Assert.AreEqual(0f, Vector3.Angle(wind, SpringBones.Leaned(new Vector3(-0.05f, 0.01f, 0f), wind, 30f, Vector3.up)), 0.01f);
            // a lock of the fringe (forward and down, on the brow) in a head wind: blown straight back it would go
            // through the face - the wind flows up over the brow and lifts it
            Vector3 fringe = new Vector3(0.04f, -0.03f, 0f), outOfTheBrow = new Vector3(1f, 0.2f, 0f);
            Vector3 lifted = SpringBones.Leaned(fringe, wind, 30f, outOfTheBrow);
            Assert.That(Vector3.Angle(fringe, lifted), Is.InRange(20f, 30f));
            Assert.Greater(Vector3.Dot(lifted - fringe, outOfTheBrow), 0f, "away from the skull");
            Assert.Greater(lifted.y, fringe.y, "the fringe lifts");
            // a lock at the back streams with the wind; one straight into it has nowhere to lean
            Vector3 backLock = new Vector3(-0.05f, 0.01f, 0f);
            Assert.Less(Vector3.Angle(wind, SpringBones.Leaned(backLock, wind, 30f, Vector3.left)), 0.01f, "streaming behind him");
            Assert.AreEqual(Vector3.right * 0.05f, SpringBones.Leaned(Vector3.right * 0.05f, wind, 30f, Vector3.right));
            // no wind, or nothing asked: as it was
            Assert.AreEqual(fringe, SpringBones.Leaned(fringe, Vector3.zero, 30f, outOfTheBrow));
            Assert.AreEqual(fringe, SpringBones.Leaned(fringe, wind, 0f, outOfTheBrow));
        }

        [Test]
        public void TheTips_SwayStandingStill_AndFlickerAtARun()
        {
            Assert.AreEqual(1.6f, SpringBones.FlutterAngle(0f, 1.6f, 0.9f, 9f), 1e-5f, "a breath of movement standing");
            Assert.Greater(SpringBones.FlutterAngle(6f, 1.6f, 0.9f, 9f), 6f, "more in the wind of a run");
            Assert.AreEqual(9f, SpringBones.FlutterAngle(30f, 1.6f, 0.9f, 9f), 1e-5f, "never a blur");
            Assert.Less(SpringBones.FlutterRate(0f), 1f, "a slow sway standing");
            Assert.Greater(SpringBones.FlutterRate(8f), 4f, "a quick flicker at a run");
            Assert.LessOrEqual(SpringBones.FlutterRate(40f), 6.5f);
        }

        [Test]
        public void TheHerosHair_IsStyled_InTheGamesPrefab()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CrimsonVeil/LevelKit/Prefabs/Player.prefab");
            SpringBones hair = null;
            foreach (var s in player.GetComponents<SpringBones>()) if (s.Prefix == "Hair.") hair = s;
            Assert.IsNotNull(hair, "the hair's springs");
            var so = new SerializedObject(hair);
            Assert.Greater(so.FindProperty("lean").floatValue, 3f, "the locks lean with the wind of his motion");
            Assert.Greater(so.FindProperty("flutterPerSpeed").floatValue, 0f, "and flutter in it");
            Assert.That(so.FindProperty("maxBend").floatValue, Is.InRange(5f, 20f), "firm where they grow");
            Assert.Greater(so.FindProperty("tipBend").floatValue, so.FindProperty("maxBend").floatValue, "loose at the tips");
            Assert.That(so.FindProperty("coreRadius").floatValue, Is.InRange(0.08f, 0.12f), "kept out of the skull");
            Assert.AreEqual(0f, so.FindProperty("drag").floatValue, 1e-6f, "the wind leans them: it is not a force on the springs as well");
        }
    }
}
