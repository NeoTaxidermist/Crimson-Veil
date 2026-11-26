using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Holding a lip, on the real hero and as the frame is drawn: the hands are on the lip from the frame he catches it
    /// (for the first half second of every hang they were 15 to 25 cm off the stone, in the air), the body stays out of
    /// the stone all the way up and over (a forearm, then the fingers, went 11 cm into the lip's corner), he is carried
    /// over the lip (he rose the last 0.85 m with his hands off it and nothing under his feet), the hand that wields a
    /// weapon while he hangs stays out of the wall, and a jump's stretch stops at a roof.
    /// </summary>
    [Category("Hero")]
    [Category("Motor")]
    public class LedgeHoldPlayTests
    {
        const string Prefabs = "Assets/_CrimsonVeil/LevelKit/Prefabs/";
        /// <summary>How far (m) the drawn body may go into stone: a cloth fold, a hair's breadth.</summary>
        const float Tolerance = 0.05f;
        /// <summary>The block he hangs on: its face at x 10 (or -10), its lip 4.2 m up.</summary>
        const float Face = 10f, Top = 4.2f;

        GameObject root, holder, player;
        ScriptedInput input;
        PlayerController controller;
        Transform handL, handR, footL, footR;
        Mesh baked;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly Dictionary<SkinnedMeshRenderer, string[]> boneOf = new Dictionary<SkinnedMeshRenderer, string[]>();
        readonly List<Bounds> stones = new List<Bounds>();
        SkinnedMeshRenderer[] skins;
        int side = 1;
        float X(float x) => side * x;

        GameObject Box(string name, Vector3 center, Vector3 size, bool stone)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.transform.SetParent(root.transform);
            b.transform.position = center;
            b.transform.localScale = size;
            if (stone) stones.Add(new Bounds(center, size));
            return b;
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("LedgeRoom");
            stones.Clear();
            Box("floor", new Vector3(0f, -0.5f, 0f), new Vector3(80f, 1f, 4f), false);
            holder = new GameObject("Holder");
            holder.SetActive(false);
            player = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Player.prefab"), holder.transform);
            Object.DestroyImmediate(player.GetComponent<PlayerInputReader>());
            input = player.AddComponent<ScriptedInput>();
            controller = player.GetComponent<PlayerController>();
            controller.InputSource = input;
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            player.transform.SetParent(null, true);
            Object.Destroy(holder);
            player.SetActive(true);
            var veil = player.GetComponent<VeilController>();
            foreach (VeilForm f in Enum.GetValues(typeof(VeilForm))) if (f != VeilForm.Flow) veil.Unlock(f);
            foreach (var t in player.GetComponentsInChildren<Transform>())
            {
                if (t.name == "Hand.L") handL = t; else if (t.name == "Hand.R") handR = t;
                else if (t.name == "Foot.L") footL = t; else if (t.name == "Foot.R") footR = t;
            }
            skins = player.GetComponentsInChildren<SkinnedMeshRenderer>();
            boneOf.Clear();
            baked = new Mesh();
            bodyInStone = 0f; hairInStone = 0f; bodyWorst = "-";
            handsOff = 0f; leftOff = 0f; firstOff = -1f; unsupported = 0f; holdFrames = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(player);
            Object.Destroy(root);
            if (baked != null) Object.Destroy(baked);
        }

        Vector3 Pos => player.transform.position;
        MotorState State => controller.Motor.State;

        /// <summary>The bone that carries each of a skin's vertices most: looked up once per skin.</summary>
        string[] Bones(SkinnedMeshRenderer smr)
        {
            if (boneOf.TryGetValue(smr, out var names)) return names;
            var weights = smr.sharedMesh.boneWeights;
            var bones = smr.bones;
            names = new string[weights.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                int b = weights[i].boneIndex0;
                names[i] = b >= 0 && b < bones.Length && bones[b] != null ? bones[b].name : "?";
            }
            boneOf[smr] = names;
            return names;
        }

        float bodyInStone, hairInStone;
        string bodyWorst = "-";

        /// <summary>How deep (m) the drawn skin is in the stones right now: the body, and the hair apart.</summary>
        void MeasureStone(string phase)
        {
            foreach (var smr in skins)
            {
                if (!smr.enabled || smr.sharedMesh == null) continue;
                // (with the renderer's scale left to its transform: baked plainly, the take-off's stretch and the catch's
                // squash counted twice)
                smr.BakeMesh(baked, true);
                baked.GetVertices(verts);
                var bone = Bones(smr);
                var t = smr.transform;
                for (int i = 0; i < verts.Count; i++)
                {
                    Vector3 w = t.TransformPoint(verts[i]);
                    foreach (var b in stones)
                    {
                        if (w.x <= b.min.x || w.x >= b.max.x || w.y <= b.min.y || w.y >= b.max.y) continue;
                        float d = Mathf.Min(Mathf.Min(w.x - b.min.x, b.max.x - w.x), Mathf.Min(b.max.y - w.y, w.y - b.min.y));
                        string by = i < bone.Length ? bone[i] : "?";
                        if (by.StartsWith("Hair")) hairInStone = Mathf.Max(hairInStone, d);
                        else if (d > bodyInStone)
                        {
                            bodyInStone = d;
                            bodyWorst = $"{phase}, state {State}, climb {controller.Motor.ClimbProgress:F2}, at x {w.x:F2} y {w.y:F2}, {by}";
                        }
                    }
                }
            }
        }

        float handsOff, leftOff, firstOff = -1f, unsupported;
        int holdFrames;

        /// <summary>How far (m) a hand's wrist is from its place on the lip.</summary>
        float OffLip(Transform hand)
        {
            var m = controller.Motor;
            int dir = m.AttachSide != 0 ? m.AttachSide : m.Facing;
            Vector2 off = HeroPoseSolver.LipHandOffset(State == MotorState.LedgeHang ? 0f : m.ClimbProgress);
            Vector3 lip = controller.LipPoint;
            return Vector2.Distance(new Vector2(hand.position.x, hand.position.y), new Vector2(lip.x + dir * off.x, lip.y + off.y));
        }

        /// <summary>A foot down on the top: over it, the ankle no higher over it than it is standing (0.13 m).</summary>
        bool OnTheTop(Transform foot)
        {
            Vector3 lip = controller.LipPoint;
            int dir = controller.Motor.AttachSide != 0 ? controller.Motor.AttachSide : controller.Motor.Facing;
            return (foot.position.x - lip.x) * dir > 0f && foot.position.y - lip.y < 0.17f;
        }

        void MeasureHold()
        {
            bool hanging = State == MotorState.LedgeHang, climbing = State == MotorState.Climbing;
            if (!hanging && !climbing) return;
            float l = OffLip(handL), r = OffLip(handR);
            if (hanging || controller.Motor.ClimbProgress <= HeroPoseSolver.LipLetGo)
            {
                float off = Mathf.Max(l, r);
                if (firstOff < 0f) firstOff = off;
                handsOff = Mathf.Max(handsOff, off);
                leftOff = Mathf.Max(leftOff, l);
                holdFrames++;
            }
            if (climbing)
            {
                bool carried = Mathf.Min(l, r) < 0.06f || OnTheTop(footL) || OnTheTop(footR);
                if (!carried) unsupported += Time.deltaTime;
            }
        }

        /// <summary>Measures as the frame is drawn: at its end, after all the animation.</summary>
        IEnumerator Watch(float seconds, string phase, Func<bool> until = null, bool hold = true)
        {
            var probe = player.GetComponent<EndOfFrameProbe>();
            if (probe == null) probe = player.AddComponent<EndOfFrameProbe>();
            probe.each = () => { MeasureStone(phase); if (hold) MeasureHold(); };
            float end = Time.time + seconds;
            while (Time.time < end && (until == null || !until())) yield return null;
            yield return null;
            probe.each = null;
        }

        IEnumerator CatchTheLip()
        {
            Box("block", new Vector3(X(Face + 2f), Top * 0.5f, 0f), new Vector3(4f, Top, 4f), true);
            controller.Teleport(new Vector3(X(Face - 3.5f), 0.05f, 0f));
            input.moveX = 0f; input.aimY = 0f;
            yield return Watch(0.5f, "settling");
            bodyInStone = 0f; hairInStone = 0f; bodyWorst = "-";
            input.moveX = X(0.6f);
            yield return Watch(0.3f, "run-up");
            input.jump = true; input.jumpHeld = true;
            yield return Watch(2f, "jump", () => State == MotorState.LedgeHang);
            input.jumpHeld = false; input.moveX = 0f;
            Assert.AreEqual(MotorState.LedgeHang, State, "caught the lip");
        }

        [UnityTest] public IEnumerator CatchingALip_TheHandsHoldIt_AndHeIsCarriedOverIt_Right() { side = 1; return CatchingALip_TheHandsHoldIt_AndHeIsCarriedOverIt(); }
        [UnityTest] public IEnumerator CatchingALip_TheHandsHoldIt_AndHeIsCarriedOverIt_Left() { side = -1; return CatchingALip_TheHandsHoldIt_AndHeIsCarriedOverIt(); }
        IEnumerator CatchingALip_TheHandsHoldIt_AndHeIsCarriedOverIt()
        {
            yield return CatchTheLip();
            yield return Watch(0.8f, "hanging");
            Assert.AreEqual(MotorState.LedgeHang, State, "still hanging");
            int hung = holdFrames;
            float caught = firstOff, hang = handsOff;
            input.jump = true;
            yield return Watch(3f, "climbing", () => State == MotorState.Normal && controller.Motor.IsGrounded);
            yield return Watch(0.3f, "on the top", null, false);
            Debug.Log($"[lip hold] side {side}: hands off the lip {caught:F3} m in the frame he caught it, {hang:F3} m at worst hanging, {handsOff:F3} m up to the press ({holdFrames} frames); "
                      + $"{unsupported:F3} s of the climb with no hand on the lip and no foot on the top; body in the stone {bodyInStone:F3} m ({bodyWorst}), hair {hairInStone:F3} m");
            Assert.Greater(hung, 10, "hung a while");
            Assert.Less(caught, 0.04f, $"the hands are on the lip in the frame he catches it ({caught:F2} m off it; 0.15 to 0.25 m they were, for half a second)");
            Assert.Less(handsOff, 0.04f, $"and stay on it hanging, pulling up and pressing over ({handsOff:F2} m off it at worst)");
            Assert.Less(bodyInStone, Tolerance, $"the body stays out of the stone all the way up and over ({bodyInStone:F2} m in it: {bodyWorst})");
            Assert.Less(hairInStone, 0.08f, $"and the hair nearly (a lock swings {hairInStone:F2} m into the wall as he catches the lip)");
            Assert.Less(unsupported, 0.15f, $"he is carried over the lip: a hand on it or a foot on the top ({unsupported:F2} s with neither; 0.25 s and 0.85 m of rise it was)");
            Assert.Greater(Pos.y, Top - 0.05f, "standing on the top");
            Assert.Greater(side * Pos.x, Face, "over the lip");
        }

        [UnityTest] public IEnumerator OnALip_NoWeaponIsCast_AndOneInHandMeltsAway_BothHandsHold_Right() { side = 1; return OnALip_NoWeaponIsCast_AndOneInHandMeltsAway_BothHandsHold(); }
        [UnityTest] public IEnumerator OnALip_NoWeaponIsCast_AndOneInHandMeltsAway_BothHandsHold_Left() { side = -1; return OnALip_NoWeaponIsCast_AndOneInHandMeltsAway_BothHandsHold(); }
        /// <summary>
        /// Both hands are on the stone: hanging on a lip (and coming up over it) no weapon is cast, and one he had in his
        /// hand when he caught it melts back into the scarf. (He could wield one hanging by one hand - at the wall he
        /// hung on, in front of his face.)
        /// </summary>
        IEnumerator OnALip_NoWeaponIsCast_AndOneInHandMeltsAway_BothHandsHold()
        {
            var veil = player.GetComponent<VeilController>();
            Box("block", new Vector3(X(Face + 2f), Top * 0.5f, 0f), new Vector3(4f, Top, 4f), true);
            controller.Teleport(new Vector3(X(Face - 3.5f), 0.05f, 0f));
            yield return Watch(0.5f, "settling");
            bodyInStone = 0f; hairInStone = 0f; bodyWorst = "-";
            // a sword in his hand as he runs up and jumps
            input.cast = VeilForm.Sword;
            input.moveX = X(0.6f);
            yield return Watch(0.3f, "run-up");
            Assert.AreEqual(VeilForm.Sword, veil.Form, "the sword is cast on the run");
            input.jump = true; input.jumpHeld = true;
            yield return Watch(2f, "jump", () => State == MotorState.LedgeHang);
            input.jumpHeld = false; input.moveX = 0f;
            Assert.AreEqual(MotorState.LedgeHang, State, "caught the lip");
            yield return Watch(0.6f, "hanging");
            Assert.AreEqual(VeilForm.Flow, veil.Form, "the sword melted back into the scarf as he caught the lip");
            Assert.IsFalse(veil.IsAttacking, "and no blow is under way");
            handsOff = 0f;
            foreach (var form in new[] { VeilForm.Sword, VeilForm.Bow, VeilForm.Spear, VeilForm.Whip, VeilForm.Shield })
            {
                input.cast = form;
                input.held = form;
                yield return Watch(0.3f, "pressing " + form);
                Assert.AreEqual(VeilForm.Flow, veil.Form, $"no {form} is cast hanging on a lip");
            }
            input.held = null;
            Debug.Log($"[lip hold] side {side}, weapons pressed while hanging: none cast; hands {handsOff:F3} m off the lip at worst; body in the stone {bodyInStone:F3} m ({bodyWorst})");
            Assert.AreEqual(MotorState.LedgeHang, State, "still hanging");
            Assert.Less(handsOff, 0.04f, $"both hands hold the lip all the while ({handsOff:F2} m off it)");
            Assert.Less(bodyInStone, Tolerance, $"and the body stays out of the stone ({bodyInStone:F2} m in it: {bodyWorst})");
            // on the top again he is armed as ever
            input.jump = true;
            yield return Watch(3f, "climbing", () => State == MotorState.Normal && controller.Motor.IsGrounded);
            yield return Watch(0.3f, "on the top", null, false);
            input.cast = VeilForm.Sword;
            yield return Watch(0.3f, "casting on the top", null, false);
            Assert.AreEqual(VeilForm.Sword, veil.Form, "standing on the top the sword is cast again");
        }

        [UnityTest]
        public IEnumerator JumpingUnderALowRoof_TheHeadStaysUnderIt()
        {
            // a roof 2 m over the floor: a hand's breadth over his head (his capsule is 1.9 m, as tall as he is drawn),
            // less than the stretch of his take-off
            Box("roof", new Vector3(0f, 2.5f, 0f), new Vector3(3f, 1f, 4f), true);
            controller.Teleport(new Vector3(0f, 0.05f, 0f));
            yield return Watch(0.5f, "settling", null, false);
            float standing = bodyInStone;
            string standingAt = bodyWorst;
            Assert.Less(standing, 0.01f, "standing under it he is clear of it");
            bodyInStone = 0f; hairInStone = 0f; bodyWorst = "-";
            float top = 0f;
            for (int k = 0; k < 2; k++)
            {
                input.jump = true; input.jumpHeld = true;
                float end = Time.time + 0.7f;
                var probe = player.GetComponent<EndOfFrameProbe>();
                probe.each = () => MeasureStone("jumping under the roof");
                while (Time.time < end) { top = Mathf.Max(top, Pos.y); yield return null; }
                probe.each = null;
                input.jumpHeld = false;
                yield return null;
            }
            Debug.Log($"[roof] under a 2 m roof: standing, body in it {standing:F3} m ({standingAt}); jumping, {bodyInStone:F3} m ({bodyWorst}), hair {hairInStone:F3} m; he rose {top:F2} m");
            Assert.Greater(top, 0.03f, "he jumped (and the roof stopped him)");
            Assert.Less(top, 0.2f, "the roof stopped him");
            // (with a capsule of 1.8 m under the body drawn 1.9 m tall, and the take-off's stretch, the head went 0.37 m
            // into it; with the stretch stopped at the roof 0.05 m, the hair 0.10 m)
            Assert.Less(bodyInStone, 0.03f, $"the head stays under the roof ({bodyInStone:F2} m in it: {bodyWorst})");
            Assert.Less(hairInStone, 0.08f, $"and the hair with it ({hairInStone:F2} m)");
        }
    }
}
