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

        // the room: a floor (top y = 0), a tunnel as low as the level's, a block higher than a jump, a deck 10 m up, a whip ring
        const float TunnelLeft = 20f, TunnelRight = 26f, TunnelBottom = 1.35f;   // (PrototypeSceneBuilder.LowPassage)
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

        Mesh baked;

        /// <summary>The top of the hero's body (m over his ground): the skinned mesh as it is drawn, his hair and all.</summary>
        float BodyTop()
        {
            if (baked == null) baked = new Mesh();
            float top = -9f;
            foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.BakeMesh(baked, true);
                var t = smr.transform;
                foreach (var v in baked.vertices) top = Mathf.Max(top, t.TransformPoint(v).y);
            }
            return top - Pos.y;
        }

        /// <summary>The lowest point of the hero's body as it is drawn (world y; the room's floor is at 0).</summary>
        float BodyBottom()
        {
            if (baked == null) baked = new Mesh();
            float low = 9f;
            foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.BakeMesh(baked, true);
                var t = smr.transform;
                foreach (var v in baked.vertices) low = Mathf.Min(low, t.TransformPoint(v).y);
            }
            return low;
        }

        [UnityTest]
        public IEnumerator StandingCrouchingCrawling_TheBodyLiesOnTheFloor_NotInItNorOverIt()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            yield return Wait(0.6f);
            Assert.AreEqual(0f, BodyBottom(), 0.025f, "standing: the soles on the floor (it stood 4 cm over it)");
            input.aimY = -1f;
            yield return Wait(0.6f);
            Assert.AreEqual(0f, BodyBottom(), 0.025f, "crouched on the floor");
            input.moveX = 1f;
            float lo = 9f, hi = -9f;
            yield return Wait(1.5f, () => { float b = BodyBottom(); lo = Mathf.Min(lo, b); hi = Mathf.Max(hi, b); });
            Assert.Greater(lo, -0.03f, "crawling: nothing through the floor");
            Assert.Less(hi, 0.04f, "crawling: always something on the floor (it rose up to 9 cm off it)");
        }

        /// <summary>The lowest of the feet's joints over his ground (m).</summary>
        float FeetLowest()
        {
            float y = 9f;
            foreach (var n in new[] { "Foot.L", "Foot.R", "Toe.L", "Toe.R" }) y = Mathf.Min(y, RigJoint.FindDeep(player.transform, n).position.y);
            return y - Pos.y;
        }

        [UnityTest]
        public IEnumerator StoppingFromARun_AtAnyPointOfTheStride_HeBrakesOnTheFloor()
        {
            // (the brake takes 2-3 frames: played out at a run, a stop in the run's flight left him a moment in the air)
            yield return StartAt(new Vector3(-40f, 0.05f, 0f));
            yield return Wait(0.6f);
            const float standing = 0f;   // (the floor: the drawn body on it)
            foreach (float phase in new[] { 0f, 0.25f, 0.5f, 0.75f })
            {
                yield return StartAt(new Vector3(-40f, 0.05f, 0f));
                input.moveX = 1f;
                yield return Wait(0.7f);
                yield return Until(() => Mathf.Abs(Mathf.DeltaAngle(mocap.GaitPhase * 360f, phase * 360f)) < 20f, 1f);
                input.moveX = 0f;
                // (the brake's first instants are still the run - a frame or so, at 60 fps; the fault held him in the
                // run's flight for a quarter of a second)
                float from = Time.time;
                yield return Wait(0.05f);
                float highest = 0f; string when = "";
                yield return Wait(0.3f, () =>
                {
                    float h = BodyBottom() - standing;
                    if (h > highest) { highest = h; when = $"{Time.time - from:F3}s after, {mocap.Current} w{mocap.Weight:F2}, {Speed:F1} m/s, frame {Time.deltaTime * 1000f:F0} ms"; }
                });
                Assert.Less(highest, 0.03f, $"stopping at {phase:F2} of the stride: a foot stays on the floor as he brakes ({when})");
                Assert.Contains(HeroMocap.RunStop, new List<string>(played), "the last steps brake");
                yield return Wait(0.6f);
            }
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
        public IEnumerator TheScarf_IsWoundRoundTheNeck_AndFallsAgainstTheBody_RunningJumpingAndInTheSomersault()
        {
            // the scarf as 3D cloth on the real hero: its body as capsules round his bones (VeilController), the wrap
            // round his neck, the loose cloth free in depth but never inside the body
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            var sim = veil.Simulation;
            var neck = RigJoint.FindDeep(player.transform, "Neck");
            float worstIn = 0f, worstWrap = 0f, zMin = 9f, zMax = -9f;
            string where = "";
            int frames = 0;
            var probe = player.AddComponent<EndOfFrameProbe>();
            probe.each = () =>
            {
                if (sim.Body.Count == 0) return;
                frames++;
                for (int i = 1; i < sim.Count; i++)
                {
                    var p = sim.Positions[i];
                    if (sim.IsWrapped(i))
                    {
                        worstWrap = Mathf.Max(worstWrap, Mathf.Abs(Vector3.Distance(p, sim.Positions[0]) - sim.Settings.neckRadius));
                        continue;
                    }
                    zMin = Mathf.Min(zMin, p.z - Pos.z); zMax = Mathf.Max(zMax, p.z - Pos.z);
                    foreach (var c in sim.Body)
                    {
                        float into = c.radius - Vector3.Distance(p, VeilSimulation.ClosestOnSegment(c.a, c.b, p));
                        if (into > worstIn) { worstIn = into; where = $"node {i} {into:F3} m into a body capsule (r {c.radius:F2}), {mocap.Current}"; }
                    }
                }
                // and its strands (VeilStrands): cloth of their own, off the body too
                var strands = veil.Strands;
                for (int s = 0; s < strands.Count; s++)
                    for (int j = 1; j < VeilStrands.Nodes; j++)
                    {
                        var p = strands.Positions[s][j];
                        foreach (var c in sim.Body)
                        {
                            float into = c.radius - Vector3.Distance(p, VeilSimulation.ClosestOnSegment(c.a, c.b, p));
                            if (into > worstIn) { worstIn = into; where = $"strand {s} node {j} {into:F3} m into a body capsule (r {c.radius:F2}), {mocap.Current}"; }
                        }
                    }
            };
            input.moveX = 1f; yield return Wait(0.8f);
            input.jump = true; input.jumpHeld = true; yield return Wait(0.25f);
            input.jump = true; yield return Wait(0.9f);   // the double jump's somersault: the scarf winds round him
            input.jumpHeld = false; input.moveX = -1f; yield return Wait(0.8f);
            input.moveX = 0f; yield return Wait(0.8f);
            probe.each = null;
            Debug.Log($"[3D scarf] {frames} frames: worst {where}; the wrap off the neck ring by {worstWrap:F3} m; the cloth's depth {zMin:F2}..{zMax:F2} m");
            Assert.Greater(frames, 50);
            Assert.IsNotNull(neck);
            Assert.Less(worstIn, 0.02f, "the loose cloth stays out of the body (within 2 cm)");
            Assert.Less(worstWrap, 0.01f, "the wound part lies round the neck");
            Assert.Greater(zMax - zMin, 0.1f, "the cloth moves in depth");
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

        /// <summary>A round of the moves: running both ways, jumps and a somersault, the crouch and a crawl, a slide,
        /// blows of the sword and the whip, a slide down a tall wall (one the test puts beyond the ring).</summary>
        IEnumerator MoveRound()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            input.moveX = 1f; yield return Wait(0.6f);
            input.jump = true; input.jumpHeld = true; yield return Wait(0.25f);
            input.jump = true; yield return Wait(0.6f);   // the double jump: the somersault
            input.jumpHeld = false; input.moveX = -1f; yield return Wait(0.6f);
            input.moveX = 0f; input.aimY = -1f; yield return Wait(0.4f);
            input.moveX = 1f; yield return Wait(0.4f);   // crawling
            input.dash = true; yield return Wait(0.5f);   // a slide
            input.aimY = 0f; input.moveX = 0f; yield return Wait(0.3f);
            yield return Chain(VeilForm.Sword, VeilForm.Sword);
            input.cast = VeilForm.Whip; yield return Wait(0.8f);
            // a slide down the tall wall: jump at it and keep pushing into it
            yield return StartAt(new Vector3(TallWall - 1.5f, 0.05f, 0f));
            input.moveX = 1f; input.jump = true; input.jumpHeld = true;
            yield return Until(() => controller.Motor.State == MotorState.WallSlide, 2f);
            input.jumpHeld = false; yield return Wait(0.8f);
            input.moveX = 0f; yield return Wait(0.3f);
        }

        const float TallWall = 60f;
   // the face of the wall the garbage test slides down

        /// <summary>
        /// The managed memory allocated under each of the hero's profiler markers (Perf), frame by frame as the profiler
        /// records it - exact bytes, the sum over every frame read.
        /// </summary>
        sealed class GarbageMeter
        {
            public readonly Dictionary<string, double> Bytes = new Dictionary<string, double>();
            /// <summary>In how many frames each marker allocated anything.</summary>
            public readonly Dictionary<string, int> Frames = new Dictionary<string, int>();
            int read = -1;

            public void Start()
            {
                UnityEditorInternal.ProfilerDriver.ClearAllFrames();
                UnityEditorInternal.ProfilerDriver.memoryRecordMode = UnityEditorInternal.ProfilerMemoryRecordMode.None;
                UnityEditorInternal.ProfilerDriver.enabled = true;
                UnityEngine.Profiling.Profiler.enabled = true;
                read = -1;
            }

            /// <summary>Reads the frames recorded since the last call (the profiler keeps only the last few hundred).</summary>
            public void ReadNew()
            {
                int last = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
                for (int f = Mathf.Max(read + 1, UnityEditorInternal.ProfilerDriver.firstFrameIndex); f < last; f++)
                {
                    using (var view = UnityEditorInternal.ProfilerDriver.GetHierarchyFrameDataView(f, 0,
                               UnityEditor.Profiling.HierarchyFrameDataView.ViewModes.Default, UnityEditor.Profiling.HierarchyFrameDataView.columnGcMemory, false))
                    {
                        if (view == null || !view.valid) continue;
                        Walk(view, view.GetRootItemID());
                    }
                    read = f;
                }
                // Read, the frames go: the editor's profiler keeps every frame it records when its window isn't open, and
                // at hundreds of frames a second the test took tens of gigabytes until the editor stopped responding.
                UnityEditorInternal.ProfilerDriver.ClearAllFrames();
            }

            void Walk(UnityEditor.Profiling.HierarchyFrameDataView view, int id)
            {
                var children = new List<int>();
                view.GetItemChildren(id, children);
                foreach (var c in children)
                {
                    string name = view.GetItemName(c);
                    if (name.StartsWith("CV."))
                    {
                        Bytes.TryGetValue(name, out double v);
                        double here = view.GetItemColumnDataAsDouble(c, UnityEditor.Profiling.HierarchyFrameDataView.columnGcMemory);
                        Bytes[name] = v + here;
                        Frames.TryGetValue(name, out int k);
                        Frames[name] = k + (here > 0 ? 1 : 0);
                    }
                    Walk(view, c);
                }
            }

            public void Stop()
            {
                ReadNew();
                UnityEditorInternal.ProfilerDriver.enabled = false;
            }

            public double Of(Perf.Id id) => Bytes.TryGetValue(Perf.Names[(int)id], out double v) ? v : 0;
        }

        [UnityTest]
        public IEnumerator TheHero_AllocatesNoGarbage_FrameByFrame()
        {
            // The first round warms up (a clip, a move, a path met for the first time may allocate once), the second is
            // measured: each part of the hero must allocate nothing - every byte is garbage the collector must clear
            // later, in a pause. (It was kilobytes a frame: new arrays for every key of a blow's choreography - hundreds
            // of kilobytes a round of blows - a copy of the bones each frame in the motion capture, an array of the two
            // arms and a search by name for the fingers sliding down a wall, an array of the two eyes.)
            Box("tall wall", new Vector3(TallWall + 2f, 8f, 0f), new Vector3(4f, 16f, 4f));
            yield return MoveRound();
            var meter = new GarbageMeter();
            meter.Start();
            bool slid = false, struck = false;
            var probe = player.AddComponent<EndOfFrameProbe>();
            int frames = 0;
            probe.each = () =>
            {
                slid |= controller.Motor.State == MotorState.WallSlide;
                struck |= veil.IsAttacking;
                if (++frames % 60 == 0) meter.ReadNew();
            };
            yield return MoveRound();
            probe.each = null;
            meter.Stop();
            var sb = new System.Text.StringBuilder($"[garbage] a round of the moves ({frames} frames):");
            foreach (var kv in meter.Bytes) sb.Append($" {kv.Key} {kv.Value:F0} B in {meter.Frames[kv.Key]} frames;");
            Debug.Log(sb.ToString());
            Assert.IsTrue(slid, "the round slid down the wall (its brace was measured)");
            Assert.IsTrue(struck, "and struck blows");
            Assert.Greater(meter.Bytes.Count, 5, "the profiler saw the hero's markers");
            foreach (var id in new[] { Perf.Id.Player, Perf.Id.Animator, Perf.Id.TouchUp, Perf.Id.Springs, Perf.Id.Face, Perf.Id.Veil, Perf.Id.VeilDraw })
                Assert.AreEqual(0, meter.Of(id), $"{Perf.Names[(int)id]}: no garbage frame by frame");
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
            yield return Until(() => Pos.x > TunnelLeft + 1.5f, 3f, () => { if (Pos.x > TunnelLeft - 0.3f) highest = Mathf.Max(highest, BodyTop()); });
            Assert.Greater(Pos.x, TunnelLeft + 1.5f, "in under the tunnel");
            Assert.Less(Time.time - t0, 1.6f, "2.5 m at a crawl - not stuck at the mouth");
            Assert.Less(highest, TunnelBottom - 0.03f, "under the roof, his hair and all (not through it)");
            input.moveX = 0f;
            float still = 0f;
            yield return Wait(0.8f, () => still = Mathf.Max(still, BodyTop()));
            Assert.Less(still, TunnelBottom - 0.03f, "crouched still in the tunnel, his hair under the roof");
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
            float t0 = Time.time, highest = 0f, lowest = 9f, rise = 0f, sunk = 9f;
            yield return Until(() => Pos.x > TunnelLeft + 1.5f, 3f, () =>
            {
                float h = HeadHeight;
                if (controller.Motor.IsSliding && Time.time - t0 > 0.05f) sunk = Mathf.Min(sunk, BodyBottom());
                if (Pos.x > TunnelLeft - 0.3f) highest = Mathf.Max(highest, BodyTop());   // under the roof, hair and all
                if (Time.time - t0 > 0.15f && controller.Motor.IsSliding) { lowest = Mathf.Min(lowest, h); rise = Mathf.Max(rise, h - lowest); }
            });
            Assert.Contains(HeroMocap.Slide, new List<string>(played), "the captured slide");
            Assert.Greater(Pos.x, TunnelLeft + 1.5f, "into the tunnel");
            Assert.Less(Time.time - t0, 2f, "the slide doesn't stop dead at the mouth");
            Assert.Less(rise, 0.2f, "once down the slide stays down: the body doesn't get up in it");
            Assert.Greater(sunk, -0.03f, "sliding, the leading foot doesn't go through the floor (it sank 4-17 cm)");
            Assert.Less(highest, TunnelBottom - 0.03f, "under the roof, his hair and all (not through it)");
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
        public IEnumerator TheSwordsCut_SweepsThroughDepth_WoundUpBehindHim_FollowingThroughInFront()
        {
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            float behind = -9f, inFront = 9f, firstFront = -1f, lastBehind = -1f;
            input.cast = VeilForm.Sword;
            yield return Until(() => veil.IsAttacking, 2f);
            var sim = veil.Simulation;
            while (veil.IsAttacking)
            {
                // the blade's tip against the hand's own depth, as the cut runs
                float tip = sim.Positions[sim.Count - 1].z - sim.WeaponPivot.z;
                float t = veil.AttackProgress;
                if (tip > 0.3f) lastBehind = t;
                if (tip < -0.3f && firstFront < 0f) firstFront = t;
                behind = Mathf.Max(behind, tip); inFront = Mathf.Min(inFront, tip);
                yield return null;
            }
            Assert.Greater(behind, 0.3f, "wound up with its point behind the hero");
            Assert.Less(inFront, -0.3f, "following through in front of him");
            Assert.Less(lastBehind, firstFront, "behind first, then through the picture, then in front");
        }

        [UnityTest]
        public IEnumerator TheWeapon_IsHeldInThePalm_InDepthToo()
        {
            // (it stood a fixed 0.3 m before the neck: up to 0.36 m off the real palm - from the side it looked held,
            // from three-quarters the sword started at his waist and the spear ran through his body)
            foreach (var form in new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip })
            {
                yield return StartAt(new Vector3(-12f, 0.05f, 0f));
                yield return Wait(0.6f);   // the last blow's recovery
                veil.Charge.Restore();
                input.cast = form;
                yield return Until(() => veil.IsAttacking, 2f);
                var sim = veil.Simulation;
                float worst = 0f;
                int measured = 0;
                while (veil.IsAttacking)
                {
                    if (veil.TransformProgress > 0.95f)
                    {
                        Vector3 palm = VeilController.PalmPoint(veil.Hand);
                        // the bit of the weapon nearest the palm in the picture
                        int near = -1; float best = 9f;
                        for (int i = Mathf.CeilToInt(veil.HandFraction * (sim.Count - 1)) - 2; i < sim.Count; i++)
                        {
                            if (i < 0) continue;
                            float d = Vector2.Distance(sim.Positions[i], palm);
                            if (d < best) { best = d; near = i; }
                        }
                        if (near >= 0 && best < 0.25f)
                        {
                            float want = VeilSimulation.WeaponPlaneFor(palm.z, veil.Anchor.position.z);
                            worst = Mathf.Max(worst, Mathf.Abs(sim.Positions[near].z - want));
                            measured++;
                        }
                    }
                    yield return null;
                }
                Assert.Greater(measured, 5, $"{form}: the weapon passes through the fist");
                Assert.Less(worst, 0.08f, $"{form}: held in the palm's depth, not a fixed way in front of the body");
            }
            Assert.AreEqual(-0.285f, VeilSimulation.WeaponPlaneFor(-0.3f, 0f), 1e-5f, "just behind the fingers");
            Assert.AreEqual(VeilSimulation.WeaponBehindNeck, VeilSimulation.WeaponPlaneFor(0.4f, 0f), 1e-5f, "a hand drawn back behind him: the weapon stays clear of the torso");
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

        /// <summary>A low thing to strike (a trigger: the hero steps into it, not against it); counts the blows.</summary>
        HitDummy LowTarget(float ahead, float height, float bottom = 0f)
        {
            var b = Box("low target", new Vector3(Pos.x + ahead, bottom + height * 0.5f, 0f), new Vector3(0.4f, height, 1f));
            b.GetComponent<Collider>().isTrigger = true;
            return b.AddComponent<HitDummy>();
        }

        /// <summary>The lowest point (m over the floor) of the weapon part of the veil.</summary>
        float WeaponLow()
        {
            var p = veil.Simulation.Positions;
            int n = veil.Simulation.Count;
            int start = Mathf.Clamp(Mathf.FloorToInt(veil.HandFraction * (n - 1)), 0, n - 1);
            float low = 99f;
            for (int i = start; i < n; i++) low = Mathf.Min(low, p[i].y);
            return low;
        }

        static readonly (VeilForm form, float ahead)[] LowBlows =
        {
            (VeilForm.Sword, 1.2f), (VeilForm.Spear, 1.6f), (VeilForm.Whip, 2.4f), (VeilForm.Shield, 0.9f), (VeilForm.Bow, 5f),
        };

        [UnityTest]
        public IEnumerator FromTheCrouch_EveryWeapon_StrikesLow_AndHeStaysDownThroughIt()
        {
            foreach (var (form, ahead) in LowBlows)
            {
                yield return StartAt(new Vector3(-12f, 0.05f, 0f));
                input.aimY = -1f;
                yield return Wait(0.5f);
                Assert.IsTrue(controller.Motor.IsCrouching, "down held: crouched");
                var target = LowTarget(ahead, 0.35f);
                input.cast = form;
                bool started = false, stood = false, low = true, standingClip = false;
                float headTop = 0f, weaponLow = 99f, lowest = 99f, lowestAt = -1f;
                yield return Until(() => started && veil.AttackProgress < 0f, 3f, () =>
                {
                    if (veil.AttackProgress < 0f) return;
                    if (!started) { started = true; low = veil.LowBlow; }
                    // down let go mid-blow: he stays down until it ends
                    if (veil.AttackProgress > 0.2f) input.aimY = 0f;
                    if (!controller.Motor.IsCrouching) stood = true;
                    headTop = Mathf.Max(headTop, HeadHeight);
                    if (veil.AttackProgress > 0.3f && veil.AttackProgress < 0.75f) weaponLow = Mathf.Min(weaponLow, WeaponLow() - Pos.y);
                    if (veil.AttackProgress > 0.1f && WeaponLow() - Pos.y < lowest) { lowest = WeaponLow() - Pos.y; lowestAt = veil.AttackProgress; }
                    if (mocap.Current == HeroMocap.SlashA || mocap.Current == HeroMocap.Stab || mocap.Current == HeroMocap.WhipA
                        || mocap.Current == HeroMocap.ShieldBash) standingClip = true;
                });
                yield return Wait(0.3f);
                Debug.Log($"[low blow] {form}: head {headTop:F2} m, weapon low {weaponLow:F2} m (lowest {lowest:F2} at {lowestAt:F2}), hits {target.Hits}");
                Assert.IsTrue(started, $"{form}: the blow was struck");
                Assert.IsTrue(low, $"{form}: from the crouch it is a low blow");
                Assert.IsFalse(stood, $"{form}: he stays down through the blow, down let go or not");
                Assert.IsFalse(standingClip, $"{form}: not a blow captured standing laid over the crouch");
                Assert.Less(headTop, 1.1f, $"{form}: the head stays low (under a tunnel's roof)");
                Assert.AreEqual(1, target.Hits, $"{form}: it strikes a thing {0.35f} m tall on the floor");
                if (form != VeilForm.Bow && form != VeilForm.Shield)
                    Assert.Less(weaponLow, 0.5f, $"{form}: the weapon sweeps low over the floor");
                // (the whip's lash lies along the floor; the shield's bash bursts the ring out all round, standing too)
                if (form != VeilForm.Whip && form != VeilForm.Shield)
                    Assert.Greater(lowest, -0.12f, $"{form}: over the floor, not into it");
                Assert.IsFalse(controller.Motor.IsCrouching, $"{form}: once it is over, down let go, he stands up");
                foreach (var a in Object.FindObjectsByType<VeilArrow>(FindObjectsSortMode.None)) Object.Destroy(a.gameObject);
                Object.Destroy(target.gameObject);
                yield return Wait(1.5f);   // (the weapon melts back)
            }
        }

        /// <summary>
        /// The shield's ring as drawn - its centre and its half-width and half-height (crouched it is an oval) - and how
        /// far out towards it the drawn body reaches (the scarf and the feet on the floor excluded): 1 on the ring, less inside.
        /// </summary>
        const float FeetOnTheFloor = 0.15f;

        /// <summary>
        /// How far (m) the crouched ring's flame may lick into the floor or a 1.35 m tunnel's roof: the crouched body is
        /// 1.3 m tall, so a ring round it with room over the head reaches a little into the stone.
        /// </summary>
        const float RingIntoStone = 0.15f;

        /// <summary>Widens <paramref name="box"/> (relative to the hero) to the shield ring's points as drawn now.</summary>
        void GrowByRing(ref Rect box)
        {
            var p = veil.Simulation.Positions;
            int n = veil.Simulation.Count;
            for (int i = Mathf.CeilToInt(veil.HandFraction * (n - 1)); i < n; i++)
            {
                float x = (p[i].x - Pos.x) * controller.Motor.Facing, y = p[i].y - Pos.y;
                box = Rect.MinMaxRect(Mathf.Min(box.xMin, x), Mathf.Min(box.yMin, y), Mathf.Max(box.xMax, x), Mathf.Max(box.yMax, y));
            }
        }

        /// <summary>
        /// How far out towards the ring (an oval filling <paramref name="ring"/>, relative to the hero) the drawn body
        /// reaches: 1 on the ring, less inside it (the scarf and the feet on the floor excluded).
        /// </summary>
        float BodyTowardsRing(Rect ring, out Vector2 worstPoint)
        {
            float body = 0f;
            worstPoint = Vector2.zero;
            var mesh = new Mesh();
            foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!smr.enabled) continue;
                smr.BakeMesh(mesh, true);
                var t = smr.transform;
                foreach (var v in mesh.vertices)
                {
                    Vector3 w = t.TransformPoint(v);
                    var l = new Vector2((w.x - Pos.x) * controller.Motor.Facing, w.y - Pos.y);
                    // (the feet stand on the floor: the ring runs along it past them, as standing it goes into it)
                    if (l.y < FeetOnTheFloor) continue;
                    var d = l - ring.center;
                    float e = new Vector2(d.x / (ring.width * 0.5f), d.y / (ring.height * 0.5f)).magnitude;
                    if (e > body) { body = e; worstPoint = l; }
                }
            }
            Object.Destroy(mesh);
            return body;
        }

        [UnityTest]
        public IEnumerator Crouched_TheShieldsRing_GoesRoundTheWholeBody_NotThroughTheHead()
        {
            foreach (bool tunnel in new[] { false, true })
            {
                yield return StartAt(tunnel ? new Vector3((TunnelLeft + TunnelRight) * 0.5f, 0.05f, 0f) : new Vector3(-12f, 0.05f, 0f));
                input.aimY = tunnel ? 0f : -1f;
                yield return Wait(0.4f);
                Assert.IsTrue(controller.Motor.IsCrouching);
                input.held = VeilForm.Shield; input.cast = VeilForm.Shield;
                yield return Wait(0.8f);
                Assert.Greater(veil.ShieldLow, 0.99f);
                var probe = player.GetComponent<EndOfFrameProbe>();
                if (probe == null) probe = player.AddComponent<EndOfFrameProbe>();
                // the ring's shape over a turn of it (it spins, its gap with it), then the body against it
                var ring = Rect.MinMaxRect(99f, 99f, -99f, -99f);
                probe.each = () => GrowByRing(ref ring);
                yield return Wait(0.6f);
                float worst = 0f;
                Vector2 at = Vector2.zero;
                probe.each = () => { float b = BodyTowardsRing(ring, out var w); if (b > worst) { worst = b; at = w; } };
                yield return Wait(0.3f);
                probe.each = null;
                input.held = null; input.aimY = 0f;
                Debug.Log($"[crouched ring{(tunnel ? " in the tunnel" : "")}] ring x {ring.xMin:F2}..{ring.xMax:F2} y {ring.yMin:F2}..{ring.yMax:F2}; the body reaches {worst:F2} of the way to it, at ({at.x:F2}, {at.y:F2})");
                Assert.Less(worst, 0.9f, $"{(tunnel ? "in the tunnel" : "crouched")}: the ring goes round the whole crouched body with room to spare, the head too");
                Assert.Greater(ring.yMin, -RingIntoStone, "not deep in the floor");
                if (tunnel) Assert.Less(ring.yMax + Pos.y, TunnelBottom + RingIntoStone, "nor deep in the tunnel's roof");
                yield return Wait(1.5f);
            }
        }

        [UnityTest]
        public IEnumerator InTheLowTunnel_TheShieldsRingDrawsIn_AndABlowUpStaysLow()
        {
            yield return StartAt(new Vector3((TunnelLeft + TunnelRight) * 0.5f, 0.05f, 0f));
            yield return Wait(0.3f);
            Assert.IsTrue(controller.Motor.IsCrouching, "under the tunnel's roof he is down");
            input.held = VeilForm.Shield; input.cast = VeilForm.Shield;
            float bottom = 99f, top = -99f;
            yield return Wait(1f, () =>
            {
                if (veil.TransformProgress < 0.95f || veil.ShieldLow < 0.99f) return;
                var p = veil.Simulation.Positions;
                int n = veil.Simulation.Count;
                for (int i = Mathf.CeilToInt(veil.HandFraction * (n - 1)); i < n; i++) { bottom = Mathf.Min(bottom, p[i].y); top = Mathf.Max(top, p[i].y); }
            });
            input.held = null;
            Debug.Log($"[low shield] ring {bottom:F2} .. {top:F2} m");
            Assert.Greater(veil.ShieldLow, 0.99f, "crouched, the ring drew in");
            Assert.Greater(bottom, -RingIntoStone, "the ring is not deep in the floor");
            Assert.Less(top, TunnelBottom + RingIntoStone, "nor deep in the tunnel's roof");
            yield return Wait(1.5f);
            // up and a blow under the roof: no rising strike into it, a low blow
            input.aimY = 1f;
            yield return Wait(0.1f);
            input.cast = VeilForm.Sword;
            float blade = -9f, bladeAt = -1f, bladeMorph = 0f;
            yield return Until(() => veil.AttackProgress >= 0.5f, 2f);
            Assert.IsTrue(veil.LowBlow, "a low blow");
            Assert.IsFalse(veil.Rising, "no rising strike under a roof");
            Assert.IsTrue(controller.Motor.IsGrounded, "he stays on the floor");
            input.aimY = 0f;
            // (the whole of the next blow: the cut stays under the roof)
            yield return Wait(1.2f);
            input.cast = VeilForm.Sword;
            yield return Until(() => veil.AttackProgress >= 0.95f, 2f, () =>
            {
                if (veil.AttackProgress < 0.05f || veil.ShapeMorph < 0.95f) return;   // (the formed blade: the scarf pouring into it streams up first)
                var p = veil.Simulation.Positions;
                int n = veil.Simulation.Count;
                for (int i = Mathf.CeilToInt(veil.HandFraction * (n - 1)); i < n; i++) if (p[i].y > blade) { blade = p[i].y; bladeAt = veil.AttackProgress; bladeMorph = veil.ShapeMorph; }
            });
            Debug.Log($"[low cut in the tunnel] blade top {blade:F2} m at t {bladeAt:F2}, morph {bladeMorph:F2}");
            Assert.Less(blade, TunnelBottom + 0.1f, "the low cut stays under the tunnel's roof");
        }

        [UnityTest]
        public IEnumerator UpOutOfTheCrouch_ARisingStrike_CarriesHimUp_AndThrowsTheFoeUp()
        {
            foreach (var form in new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip })
            {
                yield return StartAt(new Vector3(-12f, 0.05f, 0f));
                input.aimY = -1f;
                yield return Wait(0.4f);
                var target = LowTarget(0.3f, 1.2f, 1.6f);   // over him
                float y0 = Pos.y;
                input.aimY = 1f;     // let go of down, up held: and the blow
                yield return null;
                input.cast = form;
                float top = y0;
                bool rising = false;
                yield return Until(() => veil.AttackProgress >= 0.95f, 2f, () => { top = Mathf.Max(top, Pos.y); rising |= veil.Rising; });
                input.aimY = 0f;
                yield return Until(() => controller.Motor.IsGrounded, 3f);
                Debug.Log($"[rising] {form}: rose {top - y0:F2} m, hits {target.Hits}, knock up {target.LastKnock.y:F1}");
                Assert.IsTrue(rising, $"{form}: a rising strike");
                Assert.Greater(top - y0, 1f, $"{form}: it carries him up into the air");
                Assert.AreEqual(1, target.Hits, $"{form}: it strikes what is over him");
                Assert.GreaterOrEqual(target.LastKnock.y, VeilController.RiseKnock * 0.99f, $"{form}: and throws it up");
                Object.Destroy(target.gameObject);
                yield return Wait(1.5f);
            }
            // standing (not just up out of the crouch), up and a blow is the usual cut overhead: no leap
            yield return StartAt(new Vector3(-12f, 0.05f, 0f));
            input.aimY = 1f;
            yield return Wait(0.6f);
            input.cast = VeilForm.Sword;
            yield return Until(() => veil.AttackProgress >= 0.5f, 2f);
            Assert.IsFalse(veil.Rising, "standing: no rising strike");
            Assert.IsTrue(controller.Motor.IsGrounded, "he stays on the ground");
            input.aimY = 0f;
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
            int hanging = controller.BodyFacing; bool turnedHanging = false;
            yield return Wait(1.2f, () => turnedHanging |= controller.BodyFacing != hanging);
            Assert.IsFalse(turnedHanging, "hanging still he doesn't twist round");
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
            // swinging out on his own: he comes round to the last arrow's way (once he swings that way) and stays so
            int turnsOnHisOwn = 0, lastFacing = controller.BodyFacing;
            yield return Wait(2.5f, () => { if (controller.BodyFacing != lastFacing) { turnsOnHisOwn++; lastFacing = controller.BodyFacing; } });
            Assert.LessOrEqual(turnsOnHisOwn, 1, "swinging out on his own he doesn't turn round and round");
            Assert.AreEqual(-1, controller.BodyFacing, "he faces the way of the last arrow he pumped with");
            Assert.Greater(widest, 0.35f, "the keys swing him up");
            Assert.Less(offRope, 0.2f, "both fists on the rope: " + worst);
            Assert.Contains(HeroMocap.Swing, new List<string>(played), "the captured swing");
            // let go as he swings back, his back to the way he flies (he turned round to the way he flew as he let go)
            yield return Until(() => controller.Motor.SwingVelocity.x * controller.BodyFacing < -1f, 3f, watch);
            Assert.Less(controller.Motor.SwingVelocity.x * controller.BodyFacing, -1f, "swinging back");
            int beforeLetGo = controller.BodyFacing;
            input.jump = true;
            yield return Wait(0.2f);
            Assert.IsFalse(controller.Motor.IsSwinging, "let go");
            Assert.AreEqual(beforeLetGo, controller.BodyFacing, "letting go he keeps the way he faces (he turned round as he let go)");
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

    /// <summary>A thing to strike: counts the blows that land on it and keeps the last one's knock.</summary>
    public sealed class HitDummy : MonoBehaviour, IDamageable
    {
        public int Hits { get; private set; }
        public Vector2 LastKnock { get; private set; }

        public bool ReceiveHit(in DamageInfo info)
        {
            Hits++;
            LastKnock = info.knockback;
            return true;
        }
    }
}
