using System.Linq;
using CrimsonVeil.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The hero model (ArtSource/hero_build.py): the rig the game poses, the crimson trim, the hair.</summary>
    public class HeroModelTests
    {
        const string HeroPath = "Assets/_CrimsonVeil/Art/Hero/Hero.fbx";

        static GameObject Hero() => AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);

        [Test]
        public void HasEveryBoneTheAnimatorAndTheVeilUse()
        {
            var names = Hero().GetComponentsInChildren<Transform>(true).Select(t => t.name).ToList();
            foreach (var b in new[] { "Hips", "Spine", "Chest", "Neck", "Head", "UpperArm.L", "LowerArm.L", "Hand.L", "UpperArm.R",
                                      "LowerArm.R", "Hand.R", "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R" })
                CollectionAssert.Contains(names, b);
        }

        static Transform Bone(string name) => Hero().GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        [Test]
        public void TheJoints_TurnWhereARealBodyTurns()
        {
            // (a standard humanoid skeleton taken as a reference for the pivots only; the mesh and its shape are his own)
            var up = Bone("UpperChest");
            Assert.AreEqual("Chest", up.parent.name, "the chest bends in two: the ribs and the upper chest");
            Assert.AreEqual("UpperChest", Bone("Neck").parent.name);
            Assert.AreEqual("UpperChest", Bone("Shoulder.L").parent.name);
            Assert.AreEqual("UpperChest", Bone("Shoulder.R").parent.name);
            Assert.That(Bone("Neck").position.y, Is.InRange(1.49f, 1.53f), "the neck turns at its base");
            foreach (var s in new[] { "L", "R" })
            {
                var arm = Bone("UpperArm." + s).position;
                Assert.That(Mathf.Abs(arm.x), Is.InRange(0.17f, 0.205f), "the shoulder turns in the middle of the shoulder, not at its outside");
                Assert.That(arm.y, Is.InRange(1.43f, 1.46f));
                var hip = Bone("UpperLeg." + s).position;
                Assert.That(Mathf.Abs(hip.x), Is.InRange(0.08f, 0.1f), "the thigh turns at the head of its bone");
                Assert.That(hip.y, Is.InRange(0.94f, 0.97f));
            }
        }

        [Test]
        public void TheThumb_HasThreeJoints_LikeARealOne()
        {
            foreach (var s in new[] { "L", "R" })
            {
                var tip = Bone($"Thumb.{s}.2");
                Assert.AreEqual($"Thumb.{s}.1", tip.parent.name);
                Assert.AreEqual($"Thumb.{s}.0", tip.parent.parent.name);
            }
        }

        [Test]
        public void WindsweptHair_IsMadeOfSpringChains()
        {
            var bones = Hero().GetComponentsInChildren<Transform>(true);
            var roots = bones.Where(t => t.name.StartsWith("Hair.") && t.name.EndsWith(".0")).ToArray();
            Assert.GreaterOrEqual(roots.Length, 40, "every lock swings on a chain of its own (the middle row traced from the concept, locks fanned out beside it)");
            var head = bones.First(t => t.name == "Head");
            foreach (var r in roots)
            {
                Assert.AreSame(head, r.parent, r.name + " grows from the head");
                Assert.AreEqual(1, r.childCount, r.name + ": three bones in a row");
                Assert.AreEqual(1, r.GetChild(0).childCount, r.name);
            }
        }

        [Test]
        public void TheHair_IsCombedBack_AndHasWidth_NotThreeHornsSeenFromTheFront()
        {
            // (the model faces -z in Unity's space after import: its back is +z, its left +x)
            var bones = Hero().GetComponentsInChildren<Transform>(true);
            var head = bones.First(t => t.name == "Head");
            var roots = bones.Where(t => t.name.StartsWith("Hair.") && t.name.EndsWith(".0")).ToArray();
            Vector3 fwd = Hero().transform.InverseTransformDirection(bones.First(t => t.name == "Toe.L").position - bones.First(t => t.name == "Foot.L").position);
            fwd.y = 0f; fwd.Normalize();
            int back = 0, left = 0, right = 0;
            float widest = 0f;
            foreach (var r in roots)
            {
                Transform tip = r.GetChild(0).GetChild(0);
                Vector3 along = Hero().transform.InverseTransformDirection(tip.position - r.position).normalized;
                if (Vector3.Dot(along, fwd) < -0.15f) back++;
                float side = Hero().transform.InverseTransformPoint(tip.position).x;
                if (side > 0.07f) left++;
                if (side < -0.07f) right++;
                widest = Mathf.Max(widest, Mathf.Abs(side));
            }
            Assert.Greater(back, roots.Length / 2, "most of the locks are swept back");
            Assert.GreaterOrEqual(left, 6, "locks stand off the head over his left ear");
            Assert.GreaterOrEqual(right, 6, "and over his right");
            // (measured at the locks' last joints, two thirds of the way along: the skull is 0.078 m to the side there)
            Assert.Greater(widest, 0.085f, "the hair is wider than the skull");
        }

        [Test]
        public void TheTunicsFreePanels_AndTheSashEnds_HangOnClothChains_ThatMoveTheMesh()
        {
            var bones = Hero().GetComponentsInChildren<Transform>(true);
            Assert.AreEqual(4, bones.Count(t => t.name.StartsWith("Cloth.") && t.name.EndsWith(".0")), "front and back panels, each split in two");
            Assert.AreEqual(2, bones.Count(t => t.name.StartsWith("Sash.") && t.name.EndsWith(".0")), "the sash's two ends");
            foreach (var t in bones.Where(t => t.name.StartsWith("Cloth.") && t.name.EndsWith(".0")))
                Assert.AreEqual("Hips", t.parent.name, "the panels hang from the hips");

            // they really carry the cloth: vertices are weighted to them
            var smr = Hero().GetComponentInChildren<SkinnedMeshRenderer>();
            var weighted = new System.Collections.Generic.HashSet<string>();
            foreach (var w in smr.sharedMesh.boneWeights)
            {
                if (w.weight0 > 0.05f) weighted.Add(smr.bones[w.boneIndex0].name);
                if (w.weight1 > 0.05f) weighted.Add(smr.bones[w.boneIndex1].name);
                if (w.weight2 > 0.05f) weighted.Add(smr.bones[w.boneIndex2].name);
            }
            foreach (var n in new[] { "Cloth.0.1", "Cloth.1.1", "Cloth.2.1", "Cloth.3.1", "Sash.0.2", "Sash.1.2" })
                CollectionAssert.Contains(weighted, n, n + " moves part of the mesh");
        }

        [Test]
        public void TheBoots_AreAsLongAsAGrownMansFeet()
        {
            // everything of the mesh near the ground is the soles: their reach from heel to toe
            // (measured on the posed mesh in the world: the mesh's own axes are the exporter's)
            var go = Object.Instantiate(Hero());
            try
            {
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                var m = smr.transform.localToWorldMatrix;
                var vs = baked.vertices.Select(v => m.MultiplyPoint3x4(v)).ToArray();
                float minY = vs.Min(v => v.y);
                var soles = vs.Where(v => v.y < minY + 0.03f).ToArray();
                float length = soles.Max(v => v.z) - soles.Min(v => v.z);
                Assert.That(length, Is.InRange(0.24f, 0.3f), "a foot of ~0.27 m for a man of 1.83 m (the drawing's read short)");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void TheHairCoversTheTemples_NoDeepReceedingCorners()
        {
            // Over the temples (in front of the ears, at the height of the upper forehead) the head is wider than the
            // skull alone (0.078 m half-width there): a layer of hair lies over them.
            var go = Object.Instantiate(Hero());
            try
            {
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                var m = smr.transform.localToWorldMatrix;
                var vs = baked.vertices.Select(v => m.MultiplyPoint3x4(v)).ToArray();
                // which way is forward: the toes are ahead of the heels
                float minY = vs.Min(v => v.y);
                var soles = vs.Where(v => v.y < minY + 0.03f).ToArray();
                var ankle = go.GetComponentsInChildren<Transform>().First(t => t.name == "Foot.L").position;
                float fwd = Mathf.Sign((soles.Max(v => v.z) + soles.Min(v => v.z)) * 0.5f - ankle.z);
                float temple = vs.Where(v => v.y > 1.72f && v.y < 1.75f && fwd * v.z > 0.03f && fwd * v.z < 0.09f)
                                 .Max(v => Mathf.Abs(v.x));
                Assert.Greater(temple, 0.082f, "hair over the temples");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void InkBody_WithACrimsonScarfAndTrim()
        {
            var smr = Hero().GetComponentInChildren<SkinnedMeshRenderer>();
            var mesh = smr.sharedMesh;
            Assert.AreEqual(3, mesh.subMeshCount, "ink body + crimson scarf, chest trim and sash + the eyes' whites and the teeth");
            var crimson = mesh.GetSubMesh(1).bounds;
            Assert.Less(crimson.size.magnitude, mesh.bounds.size.magnitude * 0.6f, "crimson only as accents");
            var ink = new Material(Shader.Find("Hidden/InternalErrorShader"));
            var red = new Material(ink);
            var mats = LevelKitFactory.HeroMaterials(smr, ink, red);
            Assert.AreSame(ink, mats[0]);
            Assert.AreSame(red, mats[1]);
        }

        /// <summary>Heights (m above the soles) and sideways positions of the mesh's vertices (submesh or all).</summary>
        static (float h, float x)[] Points(int submesh = -1)
        {
            var mesh = Hero().GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            var b = mesh.bounds;
            int up = b.size.y >= b.size.z ? 1 : 2;
            var v = mesh.vertices;
            var idx = submesh < 0 ? Enumerable.Range(0, v.Length) : mesh.GetIndices(submesh).Distinct();
            return idx.Select(i => (v[i][up] - b.min[up], v[i].x)).ToArray();
        }

        [Test]
        public void Tunic_HangsFromTheBelt_OverBothThighs()
        {
            var band = Points().Where(p => p.h > 0.64f && p.h < 0.74f).ToArray();
            float width = band.Max(p => p.x) - band.Min(p => p.x);
            Assert.Greater(width, 0.4f, "above the knees the tunic covers both legs (the legs alone are ~0.37 m across)");
        }

        [Test]
        public void Belt_SitsAtTheWaist_WithTheCrimsonSashKnot()
        {
            var red = Points(1);
            Assert.IsTrue(red.Any(p => p.h > 1.0f && p.h < 1.12f), "the buckle's inset and the sash knot at the waist");
            Assert.IsFalse(red.Any(p => p.h > 1.18f && p.h < 1.32f), "no crimson ring round the chest any more");
        }

        [Test]
        public void Head_IsASculptedFace_WithANoseAndRoundCheeks()
        {
            var mesh = Hero().GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            var b = mesh.bounds;
            int up = b.size.y >= b.size.z ? 1 : 2, depth = 3 - up;
            var nose = mesh.vertices.Where(v => v[up] - b.min[up] > 1.66f && v[up] - b.min[up] < 1.68f).ToArray();
            // the face is on one side of the depth axis: the nose stands out there, not behind
            float best = float.MinValue;
            foreach (float s in new[] { 1f, -1f })
            {
                float mid = nose.Where(v => Mathf.Abs(v.x) < 0.006f).Max(v => s * v[depth]);
                float side = nose.Where(v => Mathf.Abs(v.x) > 0.03f && Mathf.Abs(v.x) < 0.036f).Max(v => s * v[depth]);
                best = Mathf.Max(best, mid - side);
            }
            Assert.Greater(best, 0.012f, "the nose stands out of the face (the old traced head was a flat slab)");
            float width = nose.Max(v => v.x) - nose.Min(v => v.x);
            Assert.That(width, Is.InRange(0.13f, 0.2f), "a head's width at the cheekbones (with the ears)");
        }

        [Test]
        public void Hair_IsOneMassWithAFewLocks_NotAHedgehog()
        {
            var mesh = Hero().GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            Assert.Less(mesh.vertexCount, 45000, "the hair is a smooth mass and some fifty locks (hundreds of strands blew it up)");
        }

        [Test]
        public void IsHeroSized()
        {
            var b = Hero().GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.bounds;
            float h = Mathf.Max(b.size.y, b.size.z);
            Assert.That(h, Is.InRange(1.8f, 2.1f), "about 1.8 m tall with the hair");
        }
    }
}
