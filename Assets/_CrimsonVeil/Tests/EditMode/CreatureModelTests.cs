using CrimsonVeil.Combat;
using CrimsonVeil.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Creatures placed from the Level Kit wear their detailed Blender models, with the parts the game animates.</summary>
    public class CreatureModelTests
    {
        [SetUp]
        public void SetUp() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        static int Triangles(GameObject go)
        {
            int n = 0;
            foreach (var f in go.GetComponentsInChildren<MeshFilter>())
                if (f.sharedMesh != null) n += f.sharedMesh.triangles.Length / 3;
            return n;
        }

        static void AssertModel(GameObject go, int minTriangles, params string[] parts)
        {
            var visual = CreatureParts.Find(go.transform, "Visual") ?? CreatureParts.Find(go.transform, "Model");
            Assert.IsNotNull(visual, go.name + ": a visual");
            Assert.IsNotNull(PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject), go.name + ": built from the Blender model, not primitives");
            Assert.Greater(Triangles(go), minTriangles, go.name + ": detailed");
            foreach (var part in parts) Assert.IsNotNull(CreatureParts.Find(go.transform, part), go.name + " needs its animated part " + part);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials) Assert.IsNotNull(m, go.name + "/" + r.name + " has a missing material");
        }

        [Test] public void ShieldMonk_IsModelled() => AssertModel(LevelKitFactory.ShieldMonk(Vector3.zero), 3000, "Shield");
        [Test] public void RootCrawler_IsModelled() => AssertModel(LevelKitFactory.RootCrawler(Vector3.zero), 1500, "Root0", "Root8");
        [Test] public void HangedMan_IsModelled() => AssertModel(LevelKitFactory.HangedMan(Vector3.zero), 1500, "RopeLine");
        [Test] public void AshMoth_IsModelled() => AssertModel(LevelKitFactory.AshMoth(Vector3.zero), 1500, "WingL", "WingR");
        [Test] public void LanternWisp_IsModelled() => AssertModel(LevelKitFactory.LanternWisp(Vector3.zero), 800, "Flame", "Glow");
        [Test] public void PaperKite_IsModelled() => AssertModel(LevelKitFactory.PaperKite(Vector3.zero), 300, "TailLine", "ThreadLine");

        [Test]
        public void CrowSwarm_EveryCrowIsModelled()
        {
            var swarm = LevelKitFactory.CrowSwarm(Vector3.zero);
            var crows = swarm.GetComponentsInChildren<Crow>();
            Assert.AreEqual(8, crows.Length);
            foreach (var c in crows) AssertModel(c.gameObject, 500, "WingL", "WingR");
        }

        [Test]
        public void Crimson_GoesOnlyWhereTheModelPaintedIt()
        {
            var monk = LevelKitFactory.ShieldMonk(Vector3.zero);
            int crimson = 0, ink = 0;
            foreach (var r in monk.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                    if (m.name.Contains("Eyes")) crimson++; else ink++;
            Assert.Greater(ink, 0, "a black silhouette");
            Assert.Greater(crimson, 0, "with crimson details (eyes, beads, rune)");
        }
    }
}
