using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>How each weapon is held and moves: level spear, visible bow, a whip that really lashes.</summary>
    public class VeilWeaponMotionTests
    {
        static Vector2[] StraightWhip(int n, int handIndex)
        {
            var rest = new Vector2[n];
            for (int i = 0; i < n; i++) rest[i] = new Vector2(Mathf.Max(0, i - handIndex) * 0.15f, 0f);
            return rest;
        }

        [Test]
        public void Whip_AtRest_FollowsTheHandleAsOnePiece()
        {
            var rest = StraightWhip(24, 4);
            var outp = new Vector2[24];
            VeilWeaponPose.WhipChain(rest, 4, _ => 30f, 0.16f, outp);
            Vector2 dir = (outp[23] - outp[4]).normalized;
            Assert.AreEqual(30f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 0.5f, "no swing: the whole lash is turned with the handle");
        }

        [Test]
        public void Whip_SwingThatStops_RollsALoopDownTheLash()
        {
            // The handle swung from up-and-back (160°) to forward (0°) during the last 0.12 s, then stopped.
            float TurnAgo(float s) => s < 0.04f ? 0f : Mathf.Lerp(0f, 160f, Mathf.InverseLerp(0.04f, 0.16f, s));
            var rest = StraightWhip(24, 4);
            var outp = new Vector2[24];
            VeilWeaponPose.WhipChain(rest, 4, TurnAgo, 0.16f, outp);

            Vector2 first = outp[6] - outp[5], lastSeg = outp[23] - outp[22];
            Assert.Greater(Vector2.Angle(first, lastSeg), 120f, "near the hand it points ahead, at the tip it still points back: a loop");
            float straight = 19 * 0.15f;
            Assert.Less(Vector2.Distance(outp[4], outp[23]), straight * 0.8f, "the loop shortens the reach until it rolls out");
        }

        [Test]
        public void Whip_CracksAfterTheHandStops()
        {
            var keys = HeroAttackChoreography.For(VeilForm.Whip);
            float stop = 0f;
            foreach (var k in keys) if (k.armR > 80f && k.armR < 110f && k.elbowR < 15f) { stop = k.t; break; }
            Assert.Greater(stop, 0f, "the hand drives forward and stops at arm's length");
            Assert.Greater(VeilWeaponPose.WhipCrackAt, stop, "the crack comes after the stop, when the loop reaches the tip");
            var window = CrimsonVeil.Combat.VeilCombatProfile.ActiveWindow(VeilForm.Whip);
            Assert.IsTrue(VeilWeaponPose.WhipCrackAt >= window.x && VeilWeaponPose.WhipCrackAt <= window.y, "the crack hits");
        }

        [Test]
        public void TheLash_IsQuick_BackOverTheShoulder_Thrown_CrackedAThirdOfASecondAfterThePress()
        {
            float d = VeilFormLibrary.AttackDuration(VeilForm.Whip);
            Assert.That(d, Is.InRange(0.5f, 0.7f), "a lash, not a slow swing of a rope (it took 0.8 s)");
            Assert.Less(VeilWeaponPose.WhipWoundAt, VeilWeaponPose.WhipThrownAt);
            Assert.Less((VeilWeaponPose.WhipThrownAt - VeilWeaponPose.WhipWoundAt) * d, 0.1f, "the throw is a snap: the hand stops within a tenth of a second");
            // the stop sends the loop down the lash: it cracks when the loop has run out, and is held taut after it
            Assert.AreEqual(VeilWeaponPose.WhipThrownAt * d + VeilWeaponPose.WhipLag, VeilWeaponPose.WhipCrackAt * d, 0.02f, "the crack: the stop and the loop's run");
            Assert.That(VeilWeaponPose.WhipCrackAt * d, Is.InRange(0.25f, 0.4f), "a third of a second after the press (it was over half a second)");
            Assert.Greater(VeilWeaponPose.WhipHeldTo, VeilWeaponPose.WhipCrackAt + 0.08f, "held taut for a moment");
            var window = CrimsonVeil.Combat.VeilCombatProfile.ActiveWindow(VeilForm.Whip);
            Assert.LessOrEqual(window.y, VeilWeaponPose.WhipHeldTo + 0.03f, "it hits while it is thrown out, not as it falls");

            // thrown out, the lash is all but a straight line to its tip
            var pts = VeilFormLibrary.ControlPoints(VeilForm.Whip, out _);
            Vector2 stock = pts[pts.Count - 4], tip = pts[pts.Count - 1];
            Assert.Greater(tip.x - stock.x, 3f, "over three metres of it");
            Assert.Less(Mathf.Abs(tip.y - stock.y), 0.2f, "drooping 0.4 m it reached the floor before the target");
        }

        [Test]
        public void TheLash_IsThrownFromBehindHim_AndGoesWithItsHandleWhileItIsThrown()
        {
            // as the blow begins the lash lies out behind him - where the scarf it forms from is - whatever the arm does
            Assert.AreEqual(VeilWeaponPose.WhipBack, VeilWeaponPose.WhipLashTurn(0f, -40f, 0f), 1e-3f, "level: up and back over the shoulder");
            Assert.AreEqual(90f + VeilWeaponPose.WhipBack, VeilWeaponPose.WhipLashTurn(0.05f, -40f, 90f), 1e-3f, "aimed up: down behind the hip");
            // it joins the arm as the hand arrives back there, and is the arm's from then on
            float joining = VeilWeaponPose.WhipLashTurn(0.8f * VeilWeaponPose.WhipWoundAt, 120f, 0f);
            Assert.That(joining, Is.InRange(120f, VeilWeaponPose.WhipBack));
            Assert.AreEqual(10f, VeilWeaponPose.WhipLashTurn(VeilWeaponPose.WhipWoundAt, 10f, 0f), 1e-3f);
            Assert.AreEqual(-5f, VeilWeaponPose.WhipLashTurn(VeilWeaponPose.WhipThrownAt, -5f, 0f), 1e-3f, "the throw is the arm's");
            Assert.AreEqual(-70f, VeilWeaponPose.WhipLashTurn(-1f, -70f, 0f), 1e-3f, "not lashing: it hangs from the hand");

            // from the throw to the end of the hold the whole whip follows its handle exactly; before and after it is supple
            Assert.IsFalse(VeilWeaponPose.WhipDriven(-1f), "held in the hand");
            Assert.IsFalse(VeilWeaponPose.WhipDriven(0.02f), "forming out of the scarf");
            Assert.IsTrue(VeilWeaponPose.WhipDriven(VeilWeaponPose.WhipWoundAt), "wound up");
            Assert.IsTrue(VeilWeaponPose.WhipDriven(VeilWeaponPose.WhipCrackAt), "the crack");
            Assert.IsFalse(VeilWeaponPose.WhipDriven(VeilWeaponPose.WhipHeldTo + 0.05f), "let fall");
            Assert.Greater(VeilController.WhipCoilDelay, 0.15f, "between the blows of a combo it stays out: it is not coiled and thrown again");
        }

        [Test]
        public void TheLashLetGo_FallsFromTheStockInARopesCurve_AndLiesOnTheFloor()
        {
            // (after the crack the lash was the cloth's: it stood out level in the air and the scarf's own shape, pulling
            // its soft end back behind his neck, crumpled it into ripples - up to 19 bends in 30 nodes)
            Assert.IsTrue(VeilWeaponPose.WhipFalls(-1f), "in the hand between blows it hangs");
            Assert.IsFalse(VeilWeaponPose.WhipFalls(VeilWeaponPose.WhipCrackAt), "thrown: taut");
            Assert.IsFalse(VeilWeaponPose.WhipFalls(VeilWeaponPose.WhipHeldTo));
            Assert.IsTrue(VeilWeaponPose.WhipFalls(VeilWeaponPose.WhipHeldTo + 0.01f), "cracked and held: let fall");
            Assert.AreEqual(0f, VeilWeaponPose.WhipSlackAt(0f, 1f), 1e-5f);
            Assert.AreEqual(1f, VeilWeaponPose.WhipSlackAt(1f, 0f), 1e-4f, "wholly slack: right up to the stock");
            Assert.Greater(VeilWeaponPose.WhipSlackAt(0.5f, 1f), VeilWeaponPose.WhipSlackAt(0.5f, 0f) + 0.1f, "the tip, which nothing holds, goes first");

            const int n = 30, hand = 4;
            var lash = StraightWhip(n, hand);   // thrown out level, 3.75 m of it, the hand a metre over the floor
            var outp = new Vector2[n];
            System.Func<Vector2, float> floor = _ => -1f;
            float lies = -1f + VeilWeaponPose.WhipOnFloor;
            VeilWeaponPose.WhipSlack(lash, hand, 0f, floor, null, outp);
            for (int i = 0; i < n; i++) Assert.Less(Vector2.Distance(lash[i], outp[i]), 1e-4f, "taut: as it was thrown");
            foreach (float slack in new[] { 0.25f, 0.5f, 0.75f, 1f })
            {
                VeilWeaponPose.WhipSlack(lash, hand, slack, floor, null, outp);
                float length = 0f, sharpest = 0f;
                for (int i = hand + 1; i < n; i++)
                {
                    length += Vector2.Distance(outp[i - 1], outp[i]);
                    Assert.GreaterOrEqual(outp[i].y, lies - 1e-3f, $"never under the floor ({slack})");
                    if (i < n - 1) sharpest = Mathf.Max(sharpest, Vector2.Angle(outp[i] - outp[i - 1], outp[i + 1] - outp[i]));
                }
                Assert.AreEqual(0.15f * (n - 1 - hand), length, 0.02f, $"it keeps its length ({slack})");
                Assert.Less(sharpest, VeilWeaponPose.WhipSlackBend + 12f, $"one curve, no corner in it ({slack})");
            }
            // wholly slack it curves down from the stock, comes down onto the floor and lies along it, the way it was thrown
            Assert.Less(outp[hand + 3].y, -0.1f, "down from the stock");
            Assert.AreEqual(lies, outp[n - 1].y, 1e-3f, "its end on the floor");
            Assert.Greater(outp[n - 1].x, 1.5f, "ahead of him");
            int lying = 0;
            for (int i = hand + 1; i < n; i++) if (Mathf.Abs(outp[i].y - lies) < 1e-3f) lying++;
            Assert.Greater(lying, 8, "a good length of it lies on the floor");
            Assert.AreEqual(-90f, VeilWeaponPose.WhipHang(5f, 1f), 1e-3f, "high up it hangs straight down");
            Assert.AreEqual(-VeilWeaponPose.WhipLandAngle, VeilWeaponPose.WhipHang(0f, 1f), 1e-3f, "at the floor it runs along it, still a little down: it is laid onto it - no corner where it lands");
            Assert.AreEqual(-180f + VeilWeaponPose.WhipLandAngle, VeilWeaponPose.WhipHang(0f, -1f), 1e-3f, "...the way it was going");
            Assert.That(VeilWeaponPose.WhipLandAngle, Is.InRange(3f, VeilWeaponPose.WhipSlackBend), "down enough to land, less than one piece's bend");
        }

        [Test]
        public void TheSlackLash_MeetingAWall_DropsDownItsFace_AndRunsBackFromItsFoot()
        {
            const int n = 30, hand = 4;
            var lash = StraightWhip(n, hand);
            var outp = new Vector2[n];
            System.Func<Vector2, float> floor = _ => -1f;
            System.Func<Vector2, bool> wall = p => p.x > 1.5f;
            VeilWeaponPose.WhipSlack(lash, hand, 1f, floor, wall, outp);
            float length = 0f, furthest = 0f;
            for (int i = hand + 1; i < n; i++)
            {
                length += Vector2.Distance(outp[i - 1], outp[i]);
                furthest = Mathf.Max(furthest, outp[i].x);
            }
            Assert.LessOrEqual(furthest, 1.5f + 1e-3f, "not into the stone");
            Assert.Greater(furthest, 1.3f, "up to it");
            Assert.Less(outp[n - 1].x, 1.2f, "and back from its foot");
            Assert.AreEqual(0.15f * (n - 1 - hand), length, 0.02f, "all of it still there");
            // thrown (not yet slack) it goes where it was thrown - the fall brings it out of the wall
            VeilWeaponPose.WhipSlack(lash, hand, 0f, floor, wall, outp);
            Assert.Greater(outp[n - 1].x, 3f);
        }

        [Test]
        public void RoundARing_TheWhipRunsTautToTheIron_IsWoundRoundIt_AndItsEndHangsFromTheKnot()
        {
            // (its line used to end in the middle of the ring's hole, and the cloth did not keep to it: its end hung up to
            // 2 m short of the ring, and close under the ring the rope - a metre longer than the way there - rippled)
            foreach (var grip in new[] { new Vector2(0.5f, 1.4f), new Vector2(3.5f, 2f), new Vector2(2.1f, 5f) })
            {
                var ring = new Vector2(2f, 6f);
                Vector2 dir = (ring - grip).normalized;
                Vector2 iron = ring - dir * VeilWeaponPose.RingRim;
                Assert.Less(Vector2.Distance(grip, VeilWeaponPose.WhipOnRing(grip, ring, 0f, 0f)), 1e-4f, "from the hand");
                float gone = -1f;
                for (float along = 0f; along <= VeilWeaponPose.RopeTaut; along += 0.02f)
                {
                    Vector2 p = VeilWeaponPose.WhipOnRing(grip, ring, along, 0f) - grip;
                    Assert.Less(Mathf.Abs(p.x * dir.y - p.y * dir.x), VeilWeaponPose.RingWind + 0.04f, "taut: on the straight line, barely a sag");
                    float on = Vector2.Dot(p, dir);
                    Assert.Greater(on, gone, "running on towards the ring all the way");
                    gone = on;
                }
                Vector2 touch = VeilWeaponPose.WhipOnRing(grip, ring, VeilWeaponPose.RopeTaut, 0f);
                Assert.AreEqual(VeilWeaponPose.RingWind, Vector2.Distance(touch, iron), 1e-3f, "it meets the ring's iron (not the air in its middle)");
                for (float along = VeilWeaponPose.RopeTaut; along <= VeilWeaponPose.RopeWound; along += 0.01f)
                    Assert.AreEqual(VeilWeaponPose.RingWind, Vector2.Distance(VeilWeaponPose.WhipOnRing(grip, ring, along, 0f), iron), 1e-3f, "wound round the iron");
                Vector2 before = touch - VeilWeaponPose.WhipOnRing(grip, ring, VeilWeaponPose.RopeTaut - 0.01f, 0f);
                Vector2 after = VeilWeaponPose.WhipOnRing(grip, ring, VeilWeaponPose.RopeTaut + 0.01f, 0f) - touch;
                Assert.Less(Vector2.Angle(before, after), 35f, "no corner where it goes round");
                Vector2 knot = VeilWeaponPose.WhipOnRing(grip, ring, VeilWeaponPose.RopeWound, 0f), end = VeilWeaponPose.WhipOnRing(grip, ring, 1f, 0f);
                Assert.AreEqual(VeilWeaponPose.RopeEnd, knot.y - end.y, 1e-3f, "its end hangs down from the knot");
                Assert.AreEqual(knot.x, end.x, 1e-3f);
                Assert.AreEqual(0.08f, VeilWeaponPose.WhipOnRing(grip, ring, 1f, 0.08f).x - end.x, 1e-3f, "swayed by his swing");
            }
            Assert.That(VeilWeaponPose.RingRim, Is.InRange(0.28f, 0.4f), "the ring's iron (its rim runs from 0.28 to 0.40 m)");
        }

        [Test]
        public void LetGoOfTheRing_TheRopeIsReeledInAlongItsOwnLine_AndNothingLeaps()
        {
            // (unrolled into the scarf neck first, like the short weapons, the first pieces to turn swung the whole rope
            // round with them: its tip leapt 3 to 5 m in a frame)
            const int n = 30;
            var dir = new Vector2(0.6f, 0.8f);
            var rope = new Vector2[n];
            var behind = new Vector2[n];   // (the line the scarf is laid on: up behind him, as he drops from the ring)
            var up = new Vector2(-0.8f, 0.6f);
            for (int i = 0; i < n; i++)
            {
                rope[i] = dir * (5f * i / (n - 1));
                behind[i] = up * (3f * i / (n - 1));
            }
            var was = new Vector2[n];
            var now = new Vector2[n];
            var guide = new float[n];
            VeilTransform.ReelIn(rope, behind, 0f, was, guide);
            for (int i = 0; i < n; i++) Assert.Less(Vector2.Distance(was[i], rope[i]), 1e-3f, "it starts where the rope hangs");
            Assert.AreEqual(1f, guide[n - 1], 1e-4f, "led all the way");
            Assert.AreEqual(0f, guide[0], "the neck is the neck");
            float worst = 0f;
            for (float melt = 0.02f; melt <= 1.0001f; melt += 0.02f)
            {
                VeilTransform.ReelIn(rope, behind, melt, now, guide);
                for (int i = 0; i < n; i++)
                {
                    worst = Mathf.Max(worst, Vector2.Distance(was[i], now[i]));
                    // every node keeps to the one path: on the rope's own line, or on the scarf's
                    float offRope = Mathf.Abs(now[i].x * dir.y - now[i].y * dir.x), offScarf = Mathf.Abs(now[i].x * up.y - now[i].y * up.x);
                    Assert.Less(Mathf.Min(offRope, offScarf), 0.02f, $"node {i} at {melt:F2}: on the path");
                    was[i] = now[i];
                }
            }
            Assert.Less(worst, 0.45f, "no node leaps (8 m of path in 35 fiftieths of the melt: a third of a metre a step at most)");
            for (int i = 0; i < n; i++)
                Assert.Less(Vector2.Distance(now[i], behind[i]), 1e-3f, "at the end every node is on its own place in the scarf's line: the cloth takes it over where it lies");
            Assert.AreEqual(0f, guide[n - 1], 1e-4f, "handed over to the cloth");
            VeilTransform.ReelIn(rope, behind, 0.9f, now, guide);
            Assert.That(guide[n - 1], Is.InRange(0.5f, 1f), "still led as it arrives (a guide of an eighth let the cloth's springs have it)");
            Assert.Less(Vector2.Distance(new Vector2(1.5f, 2f), VeilTransform.AlongLine(rope, 2.5f)), 1e-3f);
            Assert.Less(Vector2.Distance(rope[n - 1], VeilTransform.AlongLine(rope, 99f)), 1e-4f, "past its end: its end");
        }

        [Test]
        public void Spear_IsLevel_AndDoesNotTiltWithTheArm()
        {
            var pts = VeilFormLibrary.ControlPoints(VeilForm.Spear, out int start);
            Assert.AreEqual(pts[start].y, pts[pts.Count - 1].y, 1e-4f, "the shaft points straight ahead");
            Assert.AreEqual(0f, VeilWeaponPose.ForearmFollow(VeilForm.Spear), "the arm pushes it, it doesn't rotate it");
            Assert.AreEqual(0f, VeilWeaponPose.CarryFactor(VeilForm.Spear));
        }

        [Test]
        public void Spear_IsHeldAlongTheShaft_ButtReachesBehindTheHand()
        {
            var pts = VeilFormLibrary.ControlPoints(VeilForm.Spear, out int start);
            Vector2 butt = pts[start], tip = pts[pts.Count - 1], hand = VeilFormLibrary.Hand;
            Assert.Greater(hand.x - butt.x, 0.95f, "the butt sticks out well behind the hand");
            Assert.Greater(tip.x - hand.x, 1.8f, "most of the shaft is in front");
            Assert.IsTrue(pts.Contains(hand), "the shaft passes through the fist");
            Assert.Less(butt.x, 0f, "the butt reaches back past the hero's neck line");
        }

        [Test]
        public void Bow_LimbsAreEqual_StringIsVisible()
        {
            var range = VeilFormLibrary.BowStringRange;
            var pts = new Vector2[64];
            float hf = VeilFormLibrary.BuildShape(VeilForm.Bow, pts);
            // Upper limb (grip → upper tip) and lower limb (lower tip → grip) take the same share of the ribbon.
            Assert.AreEqual(range.x - hf, 1f - range.y, 0.03f);
            Assert.Greater(VeilFormLibrary.BowBraceHeight, 0.15f, "a clear gap between grip and string");
            Assert.Less(VeilFormLibrary.Width(VeilForm.Bow), VeilFormLibrary.BowBraceHeight, "limbs thinner than the gap: it reads as a bow");
        }

        [Test]
        public void Bow_IsHeldOutInFront_NotHiddenByTheBody()
        {
            Assert.Greater(HeroPoseSolver.HoldArmFor(VeilForm.Bow), 65f, "bow arm extended forward");
            HeroPoseSolver.WeaponHold(VeilForm.Bow, 0f, out _, out float elbow);
            Assert.Less(elbow, 15f, "arm nearly straight");
            Assert.Less(VeilFormLibrary.BowLimbReachBack, 0.35f, "the limbs don't curl back into the torso");
            Assert.AreEqual(0f, VeilWeaponPose.ForearmFollow(VeilForm.Bow), "stays upright");
        }

        [Test]
        public void Bow_IsARecurveBowWithAString()
        {
            var pts = VeilFormLibrary.ControlPoints(VeilForm.Bow, out int grip);
            Assert.AreEqual(VeilFormLibrary.Hand, pts[grip], "the scarf runs from the neck to the hand first");
            Assert.AreEqual(VeilFormLibrary.Hand + VeilFormLibrary.BowGripOffset, pts[pts.Count - 1], "the lower limb ends back in the grip");
            float top = float.MinValue, bottom = float.MaxValue;
            foreach (var p in pts) { top = Mathf.Max(top, p.y); bottom = Mathf.Min(bottom, p.y); }
            Assert.That(top - VeilFormLibrary.Hand.y, Is.InRange(0.5f, 0.8f), "upper limb: a bow, not a pole");
            Assert.That(VeilFormLibrary.Hand.y - bottom, Is.InRange(0.5f, 0.8f), "lower limb");

            var range = VeilFormLibrary.BowStringRange;
            Assert.That(range.x, Is.InRange(0.2f, 0.6f));
            Assert.Greater(range.y - range.x, 0.25f, "the string spans tip to tip");
            Assert.AreEqual(1f, VeilFormLibrary.BowStringWeight((range.x + range.y) * 0.5f), 1e-4f);
            Assert.AreEqual(0f, VeilFormLibrary.BowStringWeight(range.x * 0.5f), 1e-4f, "limbs are not string");
        }

        [Test]
        public void Bow_StringIsDrawnThenSnapsBack()
        {
            Assert.AreEqual(0f, VeilWeaponPose.BowDraw(-1f));
            Assert.AreEqual(0f, VeilWeaponPose.BowDraw(0.02f), 1e-4f);
            Assert.AreEqual(1f, VeilWeaponPose.BowDraw(0.26f), 1e-4f, "full draw before the release at 0.3");
            Assert.AreEqual(0f, VeilWeaponPose.BowDraw(0.4f), 1e-4f, "snapped straight after the release");
        }

        [Test]
        public void Whip_RestsCoiledInTheHand()
        {
            var extended = new Vector2[32];
            var coil = new Vector2[32];
            float hfExt = VeilFormLibrary.BuildShape(VeilForm.Whip, extended);
            float hfCoil = VeilFormLibrary.BuildWhipCoil(coil);
            Assert.AreEqual(hfExt, hfCoil, 0.01f, "same ribbon, same length: the hand sits at the same place");
            int hand = Mathf.CeilToInt(hfExt * 31);
            for (int i = hand; i < 32; i++)
                Assert.Less(Vector2.Distance(coil[i], VeilFormLibrary.Hand), 1.1f, $"node {i} hangs in the coil from the hand");
            Assert.Greater(Vector2.Distance(extended[31], VeilFormLibrary.Hand), 2.5f, "while the paid-out lash is long");

            // A few clean loops that come back up to the fist — not a tight ball of yarn.
            var pts = new System.Collections.Generic.List<Vector2>();
            var fine = new Vector2[200];
            VeilFormLibrary.BuildWhipCoil(fine);
            int returns = 0;
            bool away = false;
            int start = Mathf.CeilToInt(hfExt * 199) + 2;
            Vector2 stockEnd = VeilFormLibrary.Hand + new Vector2(VeilFormLibrary.WhipHandle, 0f);
            for (int i = start; i < fine.Length; i++)
            {
                float d = Vector2.Distance(fine[i], stockEnd);
                if (d > 0.3f) away = true;
                else if (away && d < 0.06f) { returns++; away = false; }
            }
            Assert.AreEqual(VeilFormLibrary.WhipCoilLoops, returns, "each loop hangs from the end of the handle in the fist and comes back to it");
            Assert.LessOrEqual(VeilFormLibrary.WhipCoilLoops, 3);
        }

        [Test]
        public void Whip_UncoilsDuringTheWindUp_FromTheHandOut()
        {
            Assert.AreEqual(0f, VeilWeaponPose.WhipUncoil(-1f), "coiled when not attacking");
            Assert.AreEqual(1f, VeilWeaponPose.WhipUncoil(0.42f), 1e-4f, "fully out before the throw");
            Assert.Greater(VeilWeaponPose.WhipNodeOpen(0.4f, 0.1f), VeilWeaponPose.WhipNodeOpen(0.4f, 0.9f), "pays out from the hand");
            Assert.AreEqual(1f, VeilWeaponPose.WhipNodeOpen(1f, 1f), 1e-4f);
            Assert.Greater(VeilController.WhipCoilTime, 0.2f, "coils back smoothly, not instantly");
        }

        [Test]
        public void Weapon_IsInFrontOfTheHero_ClothBehind()
        {
            Assert.AreEqual(0f, VeilSimulation.WeaponDepthBlend(0f), 1e-4f);
            Assert.AreEqual(1f, VeilSimulation.WeaponDepthBlend(VeilFormLibrary.Hardness(VeilForm.Bow) * 0.9f), 1e-4f);
            Assert.Greater(new VeilSettings().weaponDepth, 0.15f);
        }
    }
}
