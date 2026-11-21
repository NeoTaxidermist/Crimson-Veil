using CrimsonVeil.Combat;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Weapons are made of the same flame as the scarf; the transformation is a vortex into the palm.</summary>
    public class VeilWeaponLookTests
    {
        [Test]
        public void SpiralIn_StartsAndEndsStill_SpinsInBetween()
        {
            Assert.AreEqual(0f, VeilTransform.SpiralIn(0f), 1e-4f, "leaves the scarf where it is");
            Assert.AreEqual(0f, VeilTransform.SpiralIn(1f), 1e-4f, "the finished weapon is not twisted");
            Assert.Greater(VeilTransform.SpiralIn(0.35f), 90f, "mid-way the cloth winds into the palm");
        }

        [Test]
        public void Vortex_SpinsFastestNearThePalm()
        {
            Assert.AreEqual(1f, VeilTransform.VortexFalloff(0f), 1e-4f);
            Assert.Greater(VeilTransform.VortexFalloff(0.3f), VeilTransform.VortexFalloff(1.5f));
            Assert.Less(VeilTransform.VortexFalloff(2.5f), 0.1f, "far cloth only drifts in, it isn't swept around");
        }

        [Test]
        public void Braid_OnlyOnTheWeapon_AndStaysCloseToTheBlade()
        {
            for (float f = 0f; f <= 1f; f += 0.05f)
            {
                Assert.AreEqual(0f, VeilRenderer.WeaponBraid(f, 1.3f, 0.7f, 0.2f, 0f), 1e-5f, "loose cloth isn't braided");
                Assert.LessOrEqual(Mathf.Abs(VeilRenderer.WeaponBraid(f, 1.3f, 0.7f, 0.2f, 1f)), 0.2f * 0.5f, "strands wrap the blade, no loose loops");
            }
        }

        [Test]
        public void TheBraid_IsAHelix_InFrontOfTheBladeAndBehindIt()
        {
            bool front = false, behind = false;
            for (float f = 0f; f <= 1f; f += 0.01f)
            {
                float side = VeilRenderer.WeaponBraid(f, 1.3f, 0.7f, 0.2f, 1f), depth = VeilRenderer.WeaponBraidDepth(f, 1.3f, 0.7f, 0.2f, 1f);
                Assert.AreEqual(0.2f * 0.24f, new Vector2(side, depth).magnitude, 1e-4f, "round the blade at one radius");
                Assert.AreEqual(0f, VeilRenderer.WeaponBraidDepth(f, 1.3f, 0.7f, 0.2f, 0f), 1e-6f, "loose cloth isn't braided");
                if (depth < -0.04f) front = true;
                if (depth > 0.04f) behind = true;
            }
            Assert.IsTrue(front && behind, "it winds in front of the blade and behind it");
        }

        [Test]
        public void SetWeapons_AreSolid_ARidgedBlade_RoundShaftsAndCords()
        {
            VeilFormLibrary.SwordHilt(out _, out _, out float guard);
            Assert.AreEqual(VeilRenderer.BladeRidge, VeilRenderer.WeaponCup(VeilForm.Sword, 0.95f, true), "the blade has a ridge");
            Assert.AreEqual(VeilRenderer.RoundCup, VeilRenderer.WeaponCup(VeilForm.Sword, guard * 0.5f, true), "its grip is round");
            Assert.AreEqual(VeilRenderer.RoundCup, VeilRenderer.WeaponCup(VeilForm.Spear, 0.5f, true), "a round shaft");
            Assert.AreEqual(VeilRenderer.BladeRidge, VeilRenderer.WeaponCup(VeilForm.Spear, 0.95f, true), "a ridged head");
            Assert.AreEqual(VeilRenderer.RoundCup, VeilRenderer.WeaponCup(VeilForm.Whip, 0.7f, true));
            Assert.AreEqual(VeilRenderer.RoundCup, VeilRenderer.WeaponCup(VeilForm.Bow, 0.7f, true));
            Assert.AreEqual(VeilRenderer.RoundCup, VeilRenderer.WeaponCup(VeilForm.Sword, 0.7f, false), "the braided strands are cords");
            Assert.AreEqual(0f, VeilRenderer.WeaponCup(VeilForm.Flow, 0.7f, true), "loose cloth bows by its own measure");
            Assert.Greater(VeilRenderer.RoundCup, VeilRenderer.BladeRidge, "a cord rounder than a blade");
        }

        [Test]
        public void TheShieldRing_CirclesTheBody_FrontHalfTowardsTheCamera()
        {
            // the camera looks along +z: towards it is -z
            Assert.Less(VeilRenderer.ShieldRingDepth(0.8f, 1f, 1f), -0.2f, "facing right: the half in front of him comes towards the camera");
            Assert.Greater(VeilRenderer.ShieldRingDepth(-0.8f, 1f, 1f), 0.2f, "the half behind him passes behind his body");
            Assert.AreEqual(VeilRenderer.ShieldRingDepth(0.8f, 1f, 1f), VeilRenderer.ShieldRingDepth(-0.8f, -1f, 1f), 1e-6f, "mirror-true");
            Assert.AreEqual(0f, VeilRenderer.ShieldRingDepth(0.8f, 1f, 0f), 1e-6f, "only once it has set");
        }

        [Test]
        public void ACut_SweepsThroughDepth_BehindThenThroughThePictureThenInFront()
        {
            Assert.Greater(VeilWeaponPose.SweepTilt(VeilForm.Sword, 0.18f), 0.5f, "wound up: the point behind him");
            Assert.Less(VeilWeaponPose.SweepTilt(VeilForm.Sword, 0.55f), -0.5f, "following through in front of him");
            Assert.AreEqual(0f, VeilWeaponPose.SweepTilt(VeilForm.Sword, 1f), 1e-4f, "back in the picture at the end");
            Assert.AreEqual(0f, VeilWeaponPose.SweepTilt(VeilForm.Sword, -1f), "not attacking: in the picture");
            Assert.Greater(VeilWeaponPose.SweepTilt(VeilForm.Whip, 0.25f), 0.3f, "the lash as well");
            Assert.Less(Mathf.Abs(VeilWeaponPose.SweepTilt(VeilForm.Spear, 0.2f)), 0.25f, "a thrust barely turns");
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                Assert.AreEqual(0f, VeilWeaponPose.SweepTilt(VeilForm.Shield, t), "the shield's ring stays round the body");
                Assert.AreEqual(0f, VeilWeaponPose.SweepTilt(VeilForm.Bow, t), "the bow is aimed in the picture");
            }
            Assert.Less(VeilWeaponPose.SweepDirection(VeilForm.Sword, 0.35f), -0.3f, "striking, the blade comes towards the camera");
        }

        [Test]
        public void TheWeaponTurns_AboutTheHand_ItsFarEndFurthestThroughDepth()
        {
            Vector3 hand = new Vector3(1f, 1f, -0.3f);
            Assert.AreEqual(0f, VeilSimulation.SweepDepth(hand, hand, 0.7f), 1e-6f, "the hand holds still");
            float near = VeilSimulation.SweepDepth(hand + new Vector3(0.3f, 0f, 0f), hand, 0.7f);
            float far = VeilSimulation.SweepDepth(hand + new Vector3(0f, 1f, 0f), hand, 0.7f);
            Assert.Greater(near, 0f, "+ tilt: behind");
            Assert.AreEqual(Mathf.Sin(0.7f), far, 1e-5f, "a metre out, sin of the tilt");
            Assert.Greater(far, near);
            Assert.Less(VeilSimulation.SweepDepth(hand + Vector3.right, hand, -0.7f), 0f, "- tilt: in front");
        }

        [Test]
        public void AHitsSpray_FliesOnWithTheBlade_ThroughDepth()
        {
            Assert.AreEqual(0f, CombatFx.BurstDirection(Vector2.right, 0f).z, 1e-6f, "flat: in the picture");
            var d = CombatFx.BurstDirection(new Vector2(1f, 0.4f), -1f);
            Assert.Less(d.z, -0.6f, "towards the camera");
            Assert.AreEqual(1f, d.magnitude, 1e-5f);
            Assert.AreEqual(CombatFx.BurstDirection(Vector2.right, -5f), CombatFx.BurstDirection(Vector2.right, -1f), "at most 45 degrees");
        }

        [Test]
        public void ARope_KeepsItsBraidTurnsInMetres_NotDrawnOutIntoALine()
        {
            Assert.AreEqual(1f, VeilRenderer.RopeBraidScale(2.5f, 2.6f), 1e-5f, "as long as the scarf: the weapon's own braid");
            Assert.AreEqual(3f, VeilRenderer.RopeBraidScale(7.8f, 2.6f), 1e-4f, "pulled out three times: three times the turns");
            Assert.AreEqual(1f, VeilRenderer.RopeBraidScale(5f, 0f), 1e-5f);
            // its strands swell out round it, but still wrap it (no loose loops off a taut rope)
            Assert.Greater(VeilRenderer.RopeBraidSwell, 1f);
            Assert.LessOrEqual(Mathf.Abs(VeilRenderer.WeaponBraid(0.3f, 0f, 1.2f, 0.2f, 1f)) * VeilRenderer.RopeBraidSwell, 0.2f * 0.5f);
            Assert.That(VeilRenderer.RopeWidth, Is.InRange(VeilRenderer.WhipLashWidth, 1.2f), "thin and taut, a little fuller than the lash");
            // the rope's every strand runs the whole way (braided to the ring), like a bow's
            foreach (var st in VeilRenderer.Strands)
                Assert.AreEqual(1f, VeilRenderer.StrandEnd(st, false, true, 1f, 0.3f), 1e-5f);
        }

        [Test]
        public void FlameTongues_PeelOffAlongTheBlade_NotOnlyAtTheTip()
        {
            const float hand = 0.3f;
            float min = 1f, max = 0f;
            foreach (var st in VeilRenderer.Strands)
            {
                if (st.start <= 0f) continue;
                float e = VeilRenderer.WeaponStrandEnd(st, hand);
                Assert.Greater(e, hand, "on the blade, past the hand");
                Assert.LessOrEqual(e, 1f);
                min = Mathf.Min(min, e);
                max = Mathf.Max(max, e);
            }
            Assert.Greater(max - min, 0.25f, "spread along the blade");
        }

        [Test]
        public void Release_ExhalesUpAndBack_OnlyWhileMelting()
        {
            Assert.AreEqual(Vector2.zero, VeilController.ReleaseBreath(0f, 1));
            Assert.AreEqual(Vector2.zero, VeilController.ReleaseBreath(1f, 1));
            var mid = VeilController.ReleaseBreath(0.5f, 1);
            Assert.Greater(mid.y, 1f, "lifts the melting flame");
            Assert.Less(mid.x, 0f, "and blows it back behind a hero facing right");
            Assert.Greater(VeilController.ReleaseBreath(0.5f, -1).x, 0f);
        }

        [Test]
        public void WeaponCore_RunsAllTheWayToTheTip()
        {
            var core = VeilRenderer.Strands[1];
            Assert.AreEqual(1f, VeilRenderer.StrandEnd(core, true, false, 1f, 0.3f), 1e-5f, "a solid core along the whole blade / shaft");
            foreach (var st in VeilRenderer.Strands)
                Assert.AreEqual(1f, VeilRenderer.StrandEnd(st, false, true, 1f, 0.3f), 1e-5f, "every bow strand runs round the whole bow");
        }

        [Test]
        public void Spear_FlameTonguesStartInFrontOfTheHand_NotBehindIt()
        {
            var shape = new Vector2[32];
            float butt = VeilFormLibrary.BuildShape(VeilForm.Spear, shape);
            float grip = VeilFormLibrary.GripFraction(VeilForm.Spear);
            Assert.Greater(grip, butt + 0.15f, "the spear is gripped well in front of its butt");
            foreach (var st in VeilRenderer.Strands)
            {
                if (st.start <= 0f) continue;
                Assert.Greater(VeilRenderer.StrandEnd(st, false, false, 1f, grip), grip, "tongues flare in front of the hand");
            }
            float swordStart = VeilFormLibrary.BuildShape(VeilForm.Sword, shape), swordGrip = VeilFormLibrary.GripFraction(VeilForm.Sword);
            Assert.That(swordGrip - swordStart, Is.InRange(0.02f, 0.09f), "a sword is gripped by its hilt, just in front of the pommel");
        }

        [Test]
        public void Sword_HasAHilt_PommelGripAndCrossguard()
        {
            VeilFormLibrary.SwordHilt(out float pommel, out float fist, out float guard);
            Assert.Less(pommel, fist); Assert.Less(fist, guard);
            Assert.Less(VeilRenderer.TipProfile(VeilForm.Sword, Mathf.Lerp(pommel, guard, 0.55f)), 0.45f, "a slim grip in the fist");
            Assert.Greater(VeilRenderer.TipProfile(VeilForm.Sword, guard), 2f, "a broad crossguard in front of the fist");
            Assert.Greater(VeilRenderer.TipProfile(VeilForm.Sword, pommel), 0.5f, "a knob of a pommel behind it");
            Assert.AreEqual(1f, VeilRenderer.TipProfile(VeilForm.Sword, guard + (guard - pommel) * 2f), 1e-3f, "then the full blade");
        }

        [Test]
        public void Weapons_AreGrippedInThePalm_NotAtTheWrist()
        {
            var go = new GameObject("hand");
            try
            {
                go.transform.rotation = Quaternion.Euler(0f, 0f, 180f); // the hand hangs down
                var palm = CrimsonVeil.Veil.VeilController.PalmPoint(go.transform);
                Assert.AreEqual(-CrimsonVeil.Veil.VeilController.PalmOffset, palm.y, 1e-4f, "a little way along the hand from the wrist");
                Assert.That(CrimsonVeil.Veil.VeilController.PalmOffset, Is.InRange(0.05f, 0.1f), "the middle of the palm (the hand is ~0.1 m to the fingertips)");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ASetWeapon_IsHeldRigidly_TheWhipStaysSupple()
        {
            Assert.AreEqual(1f, VeilSimulation.RigidHold(VeilFormLibrary.Hardness(VeilForm.Sword)), 1e-3f, "the blade goes with the fist");
            Assert.AreEqual(1f, VeilSimulation.RigidHold(VeilFormLibrary.Hardness(VeilForm.Spear)), 1e-3f);
            Assert.AreEqual(1f, VeilSimulation.RigidHold(VeilFormLibrary.Hardness(VeilForm.Bow)), 1e-3f);
            Assert.AreEqual(0f, VeilSimulation.RigidHold(VeilFormLibrary.Hardness(VeilForm.Whip)), 1e-3f, "a whip rolls and lags");
            Assert.AreEqual(0f, VeilSimulation.RigidHold(0.3f), 1e-3f, "cloth stays cloth");
        }

        [Test]
        public void Whip_HasAHandle_ThenTheLash()
        {
            VeilFormLibrary.WhipHandleSpan(out float butt, out float fist, out float stockEnd);
            Assert.Less(butt, fist); Assert.Less(fist, stockEnd);
            // The handle is a solid stock of its own (VeilController.UpdateWhipHandle): a hand's grip wide, the fist
            // clenched round it; the ink is the lash, leaving its collar full width and tapering to the tip.
            Assert.That(VeilController.WhipHandleWidth, Is.InRange(0.03f, 0.06f), "a grip a hand closes round");
            Assert.AreEqual(VeilRenderer.WhipLashWidth, VeilRenderer.TipProfile(VeilForm.Whip, stockEnd), 1e-4f);
            Assert.Less(VeilRenderer.TipProfile(VeilForm.Whip, 1f), 0.2f, "the lash tapers to a fine tip");
            Assert.AreEqual(VeilFormLibrary.GripFraction(VeilForm.Whip), fist, 1e-4f, "the fist holds the handle");
            Assert.Greater(fist - butt, 0.01f, "a knob behind the little finger");
        }

        [Test]
        public void TheSwordHand_ClenchesAsTheWeaponForms()
        {
            Assert.AreEqual(0f, CrimsonVeil.Player.HeroPoseSolver.FistWeight(0f), 1e-4f, "open with the scarf loose");
            Assert.AreEqual(1f, CrimsonVeil.Player.HeroPoseSolver.FistWeight(0.5f), 1e-4f, "a fist once the grip has formed");
            Assert.That(CrimsonVeil.Player.HeroPoseSolver.FistWeight(0.2f), Is.InRange(0.05f, 0.95f), "closing as it forms");
        }

        [Test]
        public void TheHiltHandleAndGrip_AreHeldInTheFist_FromTheCast()
        {
            Assert.IsTrue(VeilFormLibrary.HeldSpan(VeilForm.Sword, out float a, out float b));
            VeilFormLibrary.SwordHilt(out float pommel, out float fist, out float guard);
            Assert.AreEqual(pommel, a, 1e-4f); Assert.Greater(b, guard, "the whole hilt");
            Assert.IsTrue(VeilFormLibrary.HeldSpan(VeilForm.Whip, out a, out b));
            Assert.IsTrue(VeilFormLibrary.HeldSpan(VeilForm.Bow, out a, out b));
            Assert.IsFalse(VeilFormLibrary.HeldSpan(VeilForm.Spear, out a, out b), "the spear's shaft slides through the fist");
            Assert.IsFalse(VeilFormLibrary.HeldSpan(VeilForm.Shield, out a, out b), "the shield floats round the body");
        }

        [Test]
        public void Sword_EndsInASharpPoint()
        {
            Assert.AreEqual(1f, VeilRenderer.TipProfile(VeilForm.Sword, 0.6f), 1e-4f, "full blade");
            Assert.AreEqual(0f, VeilRenderer.TipProfile(VeilForm.Sword, 1f), 1e-4f, "comes to a point");
            Assert.IsTrue(VeilRenderer.Pointed(VeilForm.Sword), "no flame curl over the point");
        }

        [Test]
        public void ShieldRing_BurnsWithTonguesOfFlame()
        {
            // Along the ring at one moment: several sharp tongues, not a smooth band.
            int tongues = 0;
            bool up = false;
            float maxLick = 0f, minFlicker = 9f, maxFlicker = 0f;
            for (int i = 0; i <= 400; i++)
            {
                float f = 0.2f + 0.8f * i / 400f;
                float l = VeilRenderer.ShieldFlameLick(f, 1.3f, 0.8f, 0.12f);
                maxLick = Mathf.Max(maxLick, l);
                if (!up && l > 0.4f) { tongues++; up = true; }
                if (up && l < 0.05f) up = false;
                float fl = VeilRenderer.ShieldFlicker(f, 1.3f, 0.8f);
                minFlicker = Mathf.Min(minFlicker, fl); maxFlicker = Mathf.Max(maxFlicker, fl);
            }
            Assert.GreaterOrEqual(tongues, 4, "tongues of flame all round the ring");
            Assert.LessOrEqual(maxLick, 1f);
            Assert.Greater(maxFlicker - minFlicker, 0.4f, "its body flickers");
            Assert.Greater(minFlicker, 0.5f, "but never breaks");
            // And they move.
            float change = 0f;
            for (int i = 0; i <= 100; i++)
            {
                float f = 0.2f + 0.8f * i / 100f;
                change += Mathf.Abs(VeilRenderer.ShieldFlameLick(f, 1.3f, 0.8f, 0.12f) - VeilRenderer.ShieldFlameLick(f, 1.35f, 0.8f, 0.12f));
            }
            Assert.Greater(change, 2f, "the flames run and flicker");
        }

        [Test]
        public void ShieldFlames_AreBroadTongues_NotNeedles()
        {
            int lit = 0, n = 0;
            for (int i = 0; i <= 400; i++, n++)
                if (VeilRenderer.ShieldFlameLick(0.2f + 0.8f * i / 400f, 1.3f, 0.8f, 0.12f) > 0.2f) lit++;
            Assert.Greater(lit / (float)n, 0.22f, "each tongue is a broad stroke licking out (needle spikes read as a zigzag)");
        }

        [Test]
        public void AfterASomersault_TheScarfSwingsBack_InsteadOfBeingFoldedFlat()
        {
            Assert.AreEqual(0f, VeilRenderer.SettleAfterSwirl(0.1f), 1e-4f, "just after the whirl: no folding against the chest");
            Assert.AreEqual(1f, VeilRenderer.SettleAfterSwirl(1f), 1e-4f, "back to normal a moment later");
        }

        [Test]
        public void Spear_HasALeafShapedHead()
        {
            float max = 0f;
            for (float t = 0.86f; t <= 1f; t += 0.005f) max = Mathf.Max(max, VeilRenderer.TipProfile(VeilForm.Spear, t));
            Assert.Greater(max, 2.5f, "the head is much wider than the slim shaft");
            Assert.AreEqual(1f, VeilRenderer.TipProfile(VeilForm.Spear, 0.7f), 1e-4f, "plain shaft");
            Assert.AreEqual(0f, VeilRenderer.TipProfile(VeilForm.Spear, 1f), 1e-4f, "sharp point");
            Assert.IsTrue(VeilRenderer.Pointed(VeilForm.Spear));
        }

        [Test]
        public void Weapons_AreVisibleBands_ButSlim()
        {
            foreach (var form in new[] { VeilForm.Whip, VeilForm.Sword, VeilForm.Spear, VeilForm.Shield, VeilForm.Bow })
                Assert.GreaterOrEqual(VeilFormLibrary.Width(form), 0.06f, form + " must not be a hairline");
            Assert.LessOrEqual(VeilFormLibrary.Width(VeilForm.Spear), 0.08f, "a spear is a slim shaft");
            Assert.LessOrEqual(VeilFormLibrary.Width(VeilForm.Sword), 0.17f, "a blade, not a plank");
        }
    }
}
