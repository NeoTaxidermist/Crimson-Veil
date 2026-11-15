using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The minimap's model: the level's ground on a grid, and fog over what the hero hasn't explored.</summary>
    public class MinimapModelTests
    {
        static MinimapModel Level() => new MinimapModel(new[]
        {
            Rect.MinMaxRect(0f, -12f, 20f, 0f),     // a floor, deep into the ground
            Rect.MinMaxRect(30f, 5f, 34f, 6f),      // a ledge
        }, 0.5f, 2f, 3f);

        [Test]
        public void TheGrid_CoversTheLevel_NotTheDeepInsidesOfTheGround()
        {
            var m = Level();
            Assert.AreEqual(-2f, m.Bounds.xMin, 1e-4f);
            Assert.AreEqual(36f, m.Bounds.xMax, 1e-4f);
            Assert.AreEqual(-3f, m.Bounds.yMin, 1e-4f, "only a little below the lowest floor");
            Assert.AreEqual(8f, m.Bounds.yMax, 1e-4f);
            Assert.AreEqual(76, m.Width);
        }

        [Test]
        public void TheGround_IsMarkedSolid()
        {
            var m = Level();
            var floor = m.WorldToCell(new Vector2(10f, -0.2f));
            var air = m.WorldToCell(new Vector2(10f, 1f));
            var ledge = m.WorldToCell(new Vector2(32f, 5.5f));
            var gap = m.WorldToCell(new Vector2(25f, 5.5f));
            Assert.IsTrue(m.IsSolid(floor.x, floor.y));
            Assert.IsFalse(m.IsSolid(air.x, air.y));
            Assert.IsTrue(m.IsSolid(ledge.x, ledge.y));
            Assert.IsFalse(m.IsSolid(gap.x, gap.y));
        }

        [Test]
        public void WithTheWholeMapShown_EverythingIsVisible_AndExploringIsStillRemembered()
        {
            var m = Level();
            Assert.IsTrue(m.RevealAll, "for now the whole level shows");
            var far = m.WorldToCell(new Vector2(32f, 5.5f));
            Assert.IsTrue(m.IsVisible(far.x, far.y));
            m.Reveal(new Vector2(2f, 0f));
            var near = m.WorldToCell(new Vector2(4f, 0f));
            Assert.IsTrue(m.IsExplored(near.x, near.y), "kept for when the fog comes");
            Assert.IsFalse(m.IsExplored(far.x, far.y));
        }

        [Test]
        public void InTheFog_OnlyWhereTheHeroHasBeenShows()
        {
            var m = Level();
            m.RevealAll = false;
            m.RevealRadius = 5f;
            var far = m.WorldToCell(new Vector2(32f, 5.5f));
            var near = m.WorldToCell(new Vector2(4f, 0f));
            Assert.IsFalse(m.IsVisible(near.x, near.y));
            m.Dirty = false;
            m.Reveal(new Vector2(2f, 0f));
            Assert.IsTrue(m.IsVisible(near.x, near.y), "within the radius");
            Assert.IsFalse(m.IsVisible(far.x, far.y), "still fog over there");
            Assert.IsTrue(m.Dirty, "the picture needs redrawing");
            Assert.Greater(m.ExploredFraction(), 0f);
            Assert.Less(m.ExploredFraction(), 0.5f);
        }

        [Test]
        public void AWorldPoint_MapsOntoThePicture()
        {
            var m = Level();
            Assert.AreEqual(Vector2.zero, m.WorldToUv(new Vector2(m.Bounds.xMin, m.Bounds.yMin)));
            var uv = m.WorldToUv(m.Bounds.center);
            Assert.AreEqual(0.5f, uv.x, 1e-4f);
            Assert.AreEqual(0.5f, uv.y, 1e-4f);
        }
    }
}
