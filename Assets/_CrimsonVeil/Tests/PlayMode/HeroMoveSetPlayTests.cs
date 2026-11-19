using System;
using System.Collections;
using System.Collections.Generic;
using CrimsonVeil.Combat;
using CrimsonVeil.Core;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The whole move set on the real hero (the game's prefab: the motor, the scarf, the model and its captured motion),
    /// in a room like the test room: walking and running, the turn, the jumps, the landings, the lip, the crouch and the
    /// tunnel, the slide, the dash, the combo on a wraith, the whip, the bow, the shield, blows aimed up and down, the
    /// swing on the whip, and a wraith's blow. Each checks what the game does and what the body plays.
    /// </summary>
    public class HeroMoveSetPlayTests
    {
        const string Prefabs = "Assets/_CrimsonVeil/LevelKit/Prefabs/";

        // the room: a floor (top y = 0), a 1.1 m tunnel, a block higher than a jump, a deck 10 m up, a whip ring
        const float TunnelLeft = 20f, TunnelRight = 26f, TunnelBottom = 1.1f;
        const float LedgeLeft = -20f, LedgeRight = -17f, LedgeTop = 4.2f;
        const float DeckLeft = -40f, DeckRight = -31f, DeckTop = 10f;
        static readonly Vector3 Ring = new Vector3(40f, 7.5f, 0f);

        GameObject root, holder, player;
        ScriptedInput input;
        PlayerController controller;
        VeilController veil;
        PlayerHealth health;
        HeroMocap mocap;
        Transform head, handL, handR;
        readonly HashSet<string> played = new HashSet<string>();
        readonly HashSet<string> playedUpper = new HashSet<string>();

        GameObject Box(string name, Vector3 center, Vector3 size)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.SetParent(root.transform);
            b.transform.position = center;
            b.transform.localScale = size;
            return b;
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("MoveSetRoom");
            Box("floor", new Vector3(0f, -0.5f, 0f), new Vector3(140f, 1f, 4f));
            Box("tunnel", new Vector3((TunnelLeft + TunnelRight) * 0.5f, TunnelBottom + 0.75f, 0f), new Vector3(TunnelRight - TunnelLeft, 1.5f, 4f));
            Box("ledge block", new Vector3((LedgeLeft + LedgeRight) * 0.5f, LedgeTop * 0.5f, 0f), new Vector3(LedgeRight - LedgeLeft, LedgeTop, 4f));
            Box("deck", new Vector3((DeckLeft + DeckRight) * 0.5f, DeckTop - 0.5f, 0f), new Vector3(DeckRight - DeckLeft, 1f, 4f));
            var ring = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "GrapplePoint.prefab"), root.transform);
            ring.transform.position = Ring;

            // the game's hero, its keyboard swapped for scripted keys before it wakes
            holder = new GameObject("Holder");
            holder.SetActive(false);
            player = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Player.prefab"), holder.transform);
            Object.DestroyImmediate(player.GetComponent<PlayerInputReader>());
            input = player.AddComponent<ScriptedInput>();
            controller = player.GetComponent<PlayerController>();
            controller.InputSource = input;
            veil = player.GetComponent<VeilController>();
            health = player.GetComponent<PlayerHealth>();
            mocap = player.GetComponent<HeroMocap>();
            player.transform.position = new Vector3(-8f, 0.05f, 0f);
            player.transform.SetParent(null, true);
            Object.Destroy(holder);
            player.SetActive(true);
            // (in the game the weapons are taught by relics: here he has them all)
            foreach (VeilForm f in Enum.GetValues(typeof(VeilForm))) if (f != VeilForm.Flow) veil.Unlock(f);
            head = RigJoint.FindDeep(player.transform, "Head");
            handL = RigJoint.FindDeep(player.transform, "Hand.L");
            handR = RigJoint.FindDeep(player.transform, "Hand.R");
            played.Clear(); playedUpper.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var a in Object.FindObjectsByType<VeilArrow>(FindObjectsSortMode.None)) Object.Destroy(a.gameObject);
            Object.Destroy(player);
            Object.Destroy(root);
        }

        // --- helpers -----------------------------------------------------------------------------------------

        /// <summary>Waits <paramref name="seconds"/> s, noting every clip the body and the upper body play.</summary>
        IEnumerator Wait(float seconds, Action each = null)
        {
            float end = Time.time + seconds;
            while (Time.time < end) { Note(); each?.Invoke(); yield return null; }
        }

        /// <summary>Waits until <paramref name="done"/> (or <paramref name="timeout"/> s), noting the clips.</summary>
        IEnumerator Until(Func<bool> done, float timeout, Action each = null)
        {
            float end = Time.time + timeout;
            while (Time.time < end && !done()) { Note(); each?.Invoke(); yield return null; }
            Note();
        }

        void Note()
        {
            if (mocap.Current != null) played.Add(mocap.Current);
            if (mocap.CurrentUpper != null) playedUpper.Add(mocap.CurrentUpper);
        }

        IEnumerator StartAt(Vector3 at)
        {
            controller.Teleport(at);
            input.moveX = 0f; input.aimY = 0f; input.stand = false; input.held = null;
            yield return Wait(0.5f);
            played.Clear(); playedUpper.Clear();
        }

        Vector3 Pos => player.transform.position;
        float Speed => Mathf.Abs(controller.Velocity.x);
        float HeadHeight => head.position.y - Pos.y;

        Enemy SpawnWraith(Vector3 at, float windup)
        {
            var go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Wraith.prefab"), root.transform);
            go.transform.position = at;
            var e = go.GetComponent<Enemy>();
            e.Brain.Settings.windupTime = windup;
            return e;
        }

        /// <summary>Presses the next weapon once the blow before it is well under way (in the combo's window).</summary>
        IEnumerator Chain(params VeilForm[] blows)
        {
            input.cast = blows[0];
            for (int i = 1; i < blows.Length; i++)
            {
                int step = i - 1;
                yield return Until(() => veil.Combo.Current.step == step && veil.AttackProgress >= 0.6f, 3f);
                Assert.AreEqual(step, veil.Combo.Current.step, $"blow {step + 1} of the combo struck");
                input.cast = blows[i];
            }
            int last = blows.Length - 1;
            yield return Until(() => veil.Combo.Current.step == last && veil.AttackProgress >= 0.95f, 3f);
        }

        // --- moving ------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator WalkThenRun_TheStepsKeepTheirTempo_AndHeBrakesToAStop()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            input.moveX = 0.3f;
            yield return Wait(1f);
            Assert.That(Speed, Is.InRange(1.8f, 3f), "a walk");
            Assert.AreEqual(HeroMocap.Gait, mocap.Current, "the captured walk");
            input.moveX = 1f;
            yield return Wait(0.6f);
            Assert.AreEqual(8f, Speed, 0.3f, "full speed");
            // the legs' loops at full speed: a runner's, no pattering
            float last = mocap.GaitPhase, loops = 0f, t0 = Time.time;
            yield return Wait(0.6f, () => { loops += Mathf.Repeat(mocap.GaitPhase - last, 1f); last = mocap.GaitPhase; });
            float perSecond = loops / (Time.time - t0);
            Assert.That(perSecond, Is.InRange(1.1f, 1.9f), "about 1.4 loops of steps a second at 8 m/s");
            float stopAt = Pos.x;
            input.moveX = 0f;
            yield return Wait(1f);
            Assert.Less(Speed, 0.05f, "stopped");
            Assert.Less(Pos.x - stopAt, 2.5f, "within a couple of steps");
            Assert.Contains(HeroMocap.RunStop, new List<string>(played), "the last steps brake");
        }

        [UnityTest]
        public IEnumerator TurningRoundAtARun_HeSkidsRound_AndRunsBack()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            input.moveX = 1f;
            yield return Wait(0.8f);
            input.moveX = -1f;
            yield return Wait(0.3f);
            Assert.Contains(HeroMocap.Turn, new List<string>(played), "the skid round");
            yield return Wait(0.6f);
            Assert.Less(controller.Velocity.x, -6f, "running back");
            Assert.AreEqual(-1, controller.BodyFacing, "facing back");
        }

        [UnityTest]
        public IEnumerator DoubleJump_Somersaults_AndLandsOnHisFeet()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            bool airJumped = false;
            controller.Motor.AirJumped += () => airJumped = true;
            input.jump = true; input.jumpHeld = true;
            yield return Until(() => controller.Velocity.y < 2f && !controller.Motor.IsGrounded, 1.5f);
            input.jump = true;
            float top = 0f;
            yield return Wait(0.3f, () => top = Mathf.Max(top, Pos.y));
            input.jumpHeld = false;
            yield return Until(() => controller.Motor.IsGrounded, 3f, () => top = Mathf.Max(top, Pos.y));
            Assert.IsTrue(airJumped);
            Assert.Contains(HeroMocap.Flip, new List<string>(played), "the somersault");
            Assert.Greater(top, 4.5f, "the second jump rises again");
            yield return Wait(1.6f);
            Assert.IsFalse(played.Contains(HeroMocap.LandRoll) || played.Contains(HeroMocap.HardLanding),
                "an ordinary double jump lands on its feet (no roll on the spot, no hard landing)");
            Assert.Greater(HeadHeight, 1.35f, "on his feet again");
        }

        [UnityTest]
        public IEnumerator DroppingAtARun_HeRollsOutOfIt_ForwardNotOnTheSpot()
        {
            yield return StartAt(new Vector3(DeckLeft + 2f, DeckTop + 0.05f, 0f));
            input.moveX = -0.6f;
            yield return Until(() => !controller.Motor.IsGrounded, 2f);
            yield return Until(() => controller.Motor.IsGrounded, 3f);
            input.moveX = 0f;   // the keys let go as he lands: the roll still carries him
            yield return null;
            Assert.IsTrue(controller.Motor.IsRolling, "a 10 m drop at a run: he rolls out of it");
            float from = Pos.x, lowest = 9f;
            yield return Until(() => !controller.Motor.IsRolling, 2f, () => lowest = Mathf.Min(lowest, HeadHeight));
            Assert.Contains(HeroMocap.LandRoll, new List<string>(played));
            Assert.Greater(from - Pos.x, 1.2f, "the roll carries him on (the way he came)");
            Assert.Less(lowest, 0.9f, "he goes down into the roll");
            yield return Wait(0.5f);
            Assert.Greater(HeadHeight, 1.35f, "and comes up standing");
        }

        [UnityTest]
        public IEnumerator DroppingStandingStill_IsAHardLanding_NoRoll()
        {
            yield return StartAt(new Vector3(DeckLeft - 2f, DeckTop + 0.05f, 0f));   // in the air beside the deck
            yield return Until(() => controller.Motor.IsGrounded, 3f);
            yield return null;
            Assert.IsFalse(controller.Motor.IsRolling);
            Assert.GreaterOrEqual(controller.Motor.LastFallHeight, 9f);
            yield return Wait(0.3f);
            Assert.Contains(HeroMocap.HardLanding, new List<string>(played), "the knees take it");
        }

        [UnityTest]
        public IEnumerator AtABlockHigherThanAJump_HeCatchesTheLip_AndClimbsUp()
        {
            yield return StartAt(new Vector3(LedgeRight + 3.5f, 0.05f, 0f));
            input.moveX = -0.6f;
            yield return Wait(0.3f);
            input.jump = true; input.jumpHeld = true;
            yield return Until(() => controller.Motor.State == MotorState.LedgeHang, 2f);
            input.jumpHeld = false; input.moveX = 0f;
            Assert.AreEqual(MotorState.LedgeHang, controller.Motor.State, "caught the lip");
            yield return Wait(0.6f);
            Assert.IsTrue(played.Contains(HeroMocap.HangCatch) || played.Contains(HeroMocap.HangIdle), "the captured hang");
            Assert.AreEqual(LedgeTop, handL.position.y, 0.3f, "the left hand on the lip");
            Assert.AreEqual(LedgeTop, handR.position.y, 0.3f, "the right hand on the lip");
            input.aimY = 1f;
            yield return Until(() => controller.Motor.State == MotorState.Normal && controller.Motor.IsGrounded && Pos.y > LedgeTop - 0.2f, 3f);
            input.aimY = 0f;
            Assert.Contains(HeroMocap.Mantle, new List<string>(played), "the captured mantle");
            Assert.AreEqual(LedgeTop, Pos.y, 0.1f, "standing on top");
        }

        [UnityTest]
        public IEnumerator Crouched_HeCrawlsIntoALowTunnel_WithoutStickingAtItsMouth()
        {
            yield return StartAt(new Vector3(TunnelLeft - 2.5f, 0.05f, 0f));
            input.aimY = -1f;
            yield return Wait(0.4f);
            Assert.IsTrue(controller.Motor.IsCrouching);
            Assert.Less(HeadHeight, 1.15f, "crouched low");
            input.moveX = 1f;
            yield return Until(() => Pos.x > TunnelLeft - 1f, 2f);
            float t0 = Time.time, highest = 0f;
            yield return Until(() => Pos.x > TunnelLeft + 1.5f, 3f, () => highest = Mathf.Max(highest, HeadHeight));
            Assert.Greater(Pos.x, TunnelLeft + 1.5f, "in under the tunnel");
            Assert.Less(Time.time - t0, 1.6f, "2.5 m at a crawl - not stuck at the mouth");
            Assert.Less(highest, TunnelBottom + 0.05f, "the head under the roof");
            input.aimY = 0f;
            yield return Wait(0.5f);
            Assert.IsTrue(controller.Motor.IsCrouching, "no room to stand: still crouched");
        }

        [UnityTest]
        public IEnumerator ASlideFromARun_GoesStraightIntoTheLowTunnel_LowAllTheWay()
        {
            yield return StartAt(new Vector3(TunnelLeft - 9f, 0.05f, 0f));
            input.moveX = 1f;
            yield return Wait(0.6f);
            bool slid = false;
            controller.Motor.Slid += () => slid = true;
            input.aimY = -1f; input.dash = true;
            yield return Until(() => slid, 0.5f);
            Assert.IsTrue(slid, "down + dash: a slide");
            float t0 = Time.time, highest = 0f, lowest = 9f, rise = 0f;
            yield return Until(() => Pos.x > TunnelLeft + 1.5f, 3f, () =>
            {
                float h = HeadHeight;
                if (Pos.x > TunnelLeft - 0.3f) highest = Mathf.Max(highest, h);   // under the roof
                if (Time.time - t0 > 0.15f && controller.Motor.IsSliding) { lowest = Mathf.Min(lowest, h); rise = Mathf.Max(rise, h - lowest); }
            });
            Assert.Contains(HeroMocap.Slide, new List<string>(played), "the captured slide");
            Assert.Greater(Pos.x, TunnelLeft + 1.5f, "into the tunnel");
            Assert.Less(Time.time - t0, 2f, "the slide doesn't stop dead at the mouth");
            Assert.Less(rise, 0.2f, "once down the slide stays down: the body doesn't get up in it");
            Assert.Less(highest, TunnelBottom + 0.05f, "the head under the roof"); 
        }

        [UnityTest]
        public IEnumerator Dash_ABurstOfSpeed_ThenHeRunsOn()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            input.moveX = 1f;
            yield return Wait(0.4f);
            float fastest = 0f;
            input.dash = true;
            yield return Wait(0.3f, () => fastest = Mathf.Max(fastest, Speed));
            Assert.Greater(fastest, 15f, "the dash");
            Assert.Contains(HeroMocap.Dash, new List<string>(played));
            yield return Wait(0.4f);
            Assert.AreEqual(8f, Speed, 0.5f, "running on");
        }

        // --- fighting ----------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator SwordSwordSpear_OnAWraith_ThreeHits_TheFinisherThrowsItBack()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            var w = SpawnWraith(new Vector3(-10.7f, 0f, 0f), 10f);
            yield return Wait(0.3f);
            var hits = new List<float>();
            player.GetComponent<VeilCombat>().Hit += (t, d) => hits.Add(d.amount);
            float x0 = w.transform.position.x;
            yield return Chain(VeilForm.Sword, VeilForm.Sword, VeilForm.Spear);
            yield return Wait(0.4f);
            Assert.AreEqual(3, hits.Count, "every blow of the combo lands");
            Assert.Greater(hits[2], hits[0] * 2f, "the finisher hits hardest");
            Assert.IsTrue(played.Contains(HeroMocap.SlashA) && played.Contains(HeroMocap.SlashB), "two different cuts");
            Assert.IsTrue(played.Contains(HeroMocap.SpearC), "the spear's finishing thrust");
            Assert.IsTrue(w == null || w.Health.IsDead || w.transform.position.x - x0 > 1.2f, "thrown back (or slain)");
        }

        [UnityTest]
        public IEnumerator Whip_TwoLashes_TheSecondLow()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            yield return Chain(VeilForm.Whip, VeilForm.Whip);
            Assert.IsTrue(played.Contains(HeroMocap.WhipA) && played.Contains(HeroMocap.WhipB), "two lashes");
            Assert.AreEqual(1, veil.Combo.Current.step);
            Assert.IsNull(veil.Latched, "no ring near: a blow, not a grapple");
        }

        [UnityTest]
        public IEnumerator Bow_DrawnAndLoosed_ThenOverheadFromTheSpot()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            input.held = VeilForm.Bow; input.cast = VeilForm.Bow;
            yield return Wait(1f);
            Assert.IsTrue(veil.Drawing, "held: drawn");
            Assert.Contains(HeroMocap.BowAim, new List<string>(playedUpper), "the string hand draws");
            input.held = null;
            yield return Wait(0.2f);
            var arrows = Object.FindObjectsByType<VeilArrow>(FindObjectsSortMode.None);
            Assert.AreEqual(1, arrows.Length, "loosed on release");
            Assert.Greater(arrows[0].Velocity.x, 5f, "ahead");
            Object.Destroy(arrows[0].gameObject);
            yield return Wait(0.8f);
            input.stand = true; input.aimY = 1f;
            yield return Wait(0.1f);
            input.held = VeilForm.Bow; input.cast = VeilForm.Bow;
            yield return Wait(1f);
            float x = Pos.x;
            input.held = null;
            yield return Wait(0.2f);
            arrows = Object.FindObjectsByType<VeilArrow>(FindObjectsSortMode.None);
            Assert.AreEqual(1, arrows.Length);
            Assert.Greater(arrows[0].Velocity.y, Mathf.Abs(arrows[0].Velocity.x), "overhead");
            Assert.AreEqual(x, Pos.x, 0.05f, "planted: he doesn't step");
        }

        [UnityTest]
        public IEnumerator Shield_RaisedWhileHeld_MeltsBackOnRelease()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            input.held = VeilForm.Shield; input.cast = VeilForm.Shield;
            yield return Wait(0.8f);
            Assert.AreEqual(VeilForm.Shield, veil.Form);
            Assert.AreEqual(VeilForm.Shield, veil.ShapeForm);
            Assert.Greater(veil.TransformProgress, 0.9f, "the ring formed");
            input.held = null;
            yield return Wait(1.2f);
            Assert.AreEqual(VeilForm.Flow, veil.Form, "back to the scarf");
        }

        [UnityTest]
        public IEnumerator BlowsAimedUp_CutAboveTheHead_AndDownFromALeap_UnderTheFeet()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            foreach (var form in new[] { VeilForm.Sword, VeilForm.Spear })
            {
                input.aimY = 1f;
                yield return Wait(0.15f);
                input.cast = form;
                float over = -9f;
                yield return Until(() => veil.AttackProgress >= 0.9f, 2f,
                    () => { if (veil.AttackProgress > 0.3f && veil.AttackProgress < 0.7f) over = Mathf.Max(over, veil.Hand.position.y - head.position.y); });
                Assert.Greater(over, 0.1f, $"{form} aimed up: the weapon hand goes up above the head");
                input.aimY = 0f;
                yield return Wait(1f);
            }
            // a strike under the feet from a leap
            input.jump = true; input.jumpHeld = true;
            yield return Wait(0.25f);
            input.aimY = -1f; input.cast = VeilForm.Sword;
            yield return Until(() => controller.Motor.IsGrounded && controller.Velocity.y <= 0f, 2f);
            input.aimY = 0f; input.jumpHeld = false;
            Assert.Contains(HeroMocap.DownStrike, new List<string>(played), "the strike under the feet");
        }

        [UnityTest]
        public IEnumerator TheWhip_CatchesARing_HeSwingsBothFistsOnTheRope_AndLetsGoWithAJump()
        {
            yield return StartAt(new Vector3(Ring.x - 2.5f, 0.05f, 0f));
            input.jump = true; input.jumpHeld = true;
            yield return Wait(0.25f);
            input.cast = VeilForm.Whip;
            yield return Until(() => controller.Motor.IsSwinging, 1f);
            input.jumpHeld = false;
            Assert.IsNotNull(veil.Latched, "the whip caught the ring");
            Assert.IsTrue(controller.Motor.IsSwinging);
            float widest = 0f, offRope = 0f, swingStart = Time.time;
            string worst = "";
            Action watch = () =>
            {
                widest = Mathf.Max(widest, Mathf.Abs(controller.Motor.SwingAngle));
                if (Time.time - swingStart < 0.15f) return;   // (the hands reach up for the rope as it catches)
                var grip = controller.GripPosition;
                Vector2 up = ((Vector2)controller.Motor.SwingAnchor - (Vector2)grip).normalized;
                foreach (var h in new[] { handL, handR })
                {
                    Vector2 d = (Vector2)h.position - (Vector2)grip;
                    float off = Mathf.Abs(d.x * up.y - d.y * up.x);
                    if (off > offRope) { offRope = off; worst = $"{h.name} at {Time.time - swingStart:F2}s angle {controller.Motor.SwingAngle:F2} facing {controller.BodyFacing} clip {mocap.Current} hand {(Vector2)h.position} grip {(Vector2)grip}"; }
                }
            };
            for (int i = 0; i < 4; i++)
            {
                input.moveX = i % 2 == 0 ? 1f : -1f;
                yield return Wait(0.8f, watch);
            }
            input.moveX = 0f;
            Assert.Greater(widest, 0.35f, "the keys swing him up");
            Assert.Less(offRope, 0.2f, "both fists on the rope: " + worst);
            Assert.Contains(HeroMocap.Swing, new List<string>(played), "the captured swing");
            input.jump = true;
            yield return Wait(0.2f);
            Assert.IsFalse(controller.Motor.IsSwinging, "let go");
            Assert.IsNull(veil.Latched);
        }

        [UnityTest]
        public IEnumerator AWraith_ClosesIn_WindsUpAndStrikes_TheHeroReels()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            var w = SpawnWraith(new Vector3(-8.5f, 0f, 0f), 0.5f);
            var states = new HashSet<EnemyState>();
            int absorbed = health.AbsorbCount, blocked = health.BlockCount;
            float life = health.Model.Current;
            yield return Until(() => health.Model.Current < life || health.AbsorbCount > absorbed, 5f, () => states.Add(w.Brain.State));
            Assert.IsTrue(states.Contains(EnemyState.Chase), "it closes in");
            Assert.IsTrue(states.Contains(EnemyState.Windup), "it winds up first (a telegraph)");
            Assert.IsTrue(health.Model.Current < life || health.AbsorbCount > absorbed, "the blow lands (on him or on the scarf)");
            yield return Wait(0.5f);
            Assert.IsTrue(played.Contains(HeroMocap.Stagger) || playedUpper.Contains(HeroMocap.Hurt), "he reels from it");
        }
    }
}
