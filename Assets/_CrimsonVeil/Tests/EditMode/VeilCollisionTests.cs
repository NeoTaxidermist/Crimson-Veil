using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The scarf drapes against walls instead of passing through them.</summary>
    public class VeilCollisionTests
    {
        static VeilSimulation Scarf(Vector3 neck)
        {
            var sim = new VeilSimulation(new VeilSettings(), neck) { DriftDirection = -1f, RestShapeWeight = 1f };
            sim.Reset(neck);
            return sim;
        }

        [Test]
        public void Scarf_StreamingIntoAPillar_StopsAtItsFace()
        {
            var neck = new Vector3(0f, 1.5f, 0f);
            var sim = Scarf(neck);
            // a thick pillar 1 m behind the hero (the scarf streams back, -x, 2.8 m long)
            var pillar = Rect.MinMaxRect(-2.5f, -1f, -1f, 6f);
            sim.Obstacles.Add(pillar);
            for (int k = 0; k < 240; k++) sim.Step(1f / 60f, neck);
            for (int i = 1; i < sim.Count; i++)
            {
                Vector2 p = sim.Positions[i];
                Assert.IsFalse(pillar.Contains(p), $"node {i} inside the pillar at {p}");
                Assert.Greater(p.x, pillar.xMax - 1e-3f, $"node {i} came out on the far side of the pillar");
            }
            Assert.Greater(sim.WallContact(sim.Count - 1) + sim.WallContact(sim.Count / 2), 0f, "it rests against the wall");
        }

        [Test]
        public void WithoutWalls_TheScarfStreamsFreely()
        {
            var neck = new Vector3(0f, 1.5f, 0f);
            var sim = Scarf(neck);
            for (int k = 0; k < 240; k++) sim.Step(1f / 60f, neck);
            Assert.Less(sim.Positions[sim.Count - 1].x, -1.5f, "streams well back when nothing is in the way");
        }

        [Test]
        public void EntryPoint_IsOnTheFaceTheSegmentCrossed()
        {
            var r = Rect.MinMaxRect(-2f, 0f, -1f, 4f);
            // from the right, deep into the box: back out on the right face, not the nearer left one
            Vector2 e = VeilSimulation.EntryPoint(r, new Vector2(0f, 2f), new Vector2(-1.8f, 2f));
            Assert.Greater(e.x, r.xMax);
            Assert.AreEqual(2f, e.y, 1e-3f);
            // from above
            e = VeilSimulation.EntryPoint(r, new Vector2(-1.5f, 5f), new Vector2(-1.5f, 3.5f));
            Assert.Greater(e.y, r.yMax);
        }

        [Test]
        public void TheHeroAndEnemies_AreNotWalls()
        {
            var hero = new GameObject("Hero");
            var cc = hero.AddComponent<CharacterController>();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.GetComponent<Collider>().isTrigger = true;
            try
            {
                Assert.IsFalse(VeilController.IsObstacle(cc, hero.transform));
                Assert.IsTrue(VeilController.IsObstacle(wall.GetComponent<Collider>(), hero.transform));
                Assert.IsFalse(VeilController.IsObstacle(trigger.GetComponent<Collider>(), hero.transform));
            }
            finally
            {
                Object.DestroyImmediate(hero);
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(trigger);
            }
        }
    }
}
