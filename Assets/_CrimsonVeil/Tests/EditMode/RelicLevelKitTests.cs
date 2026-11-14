using CrimsonVeil.EditorTools;
using CrimsonVeil.Veil;
using CrimsonVeil.World;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>Weapon relics and secrets in the level kit; the hero starts with the sword only.</summary>
    public class RelicLevelKitTests
    {
        [Test]
        public void Catalog_OffersARelicPerWeapon_AndSecrets()
        {
            foreach (var (id, form) in new[] { ("RelicWhip", VeilForm.Whip), ("RelicSpear", VeilForm.Spear), ("RelicShield", VeilForm.Shield), ("RelicBow", VeilForm.Bow) })
            {
                var e = LevelKitCatalog.Find(id);
                Assert.IsNotNull(e, id);
                StringAssert.StartsWith("Relikvie", e.label);
                var go = e.build();
                try
                {
                    var relic = go.GetComponent<VeilRelic>();
                    Assert.IsNotNull(relic, id);
                    Assert.AreEqual(form, relic.form);
                    Assert.IsTrue(go.GetComponent<SphereCollider>().isTrigger, "picked up by walking through it");
                    var glyph = go.transform.Find("Glyph").GetComponent<LineRenderer>();
                    Assert.Greater(glyph.positionCount, 2, "the weapon's crimson outline floats above the plinth");
                }
                finally { Object.DestroyImmediate(go); }
            }
            var secret = LevelKitCatalog.Find("Secret").build();
            try { Assert.IsNotNull(secret.GetComponent<VeilHint>()); }
            finally { Object.DestroyImmediate(secret); }
        }

        [Test]
        public void Hero_StartsWithTheSwordOnly()
        {
            var hero = LevelKitFactory.Player(Vector3.zero);
            try
            {
                var veil = hero.GetComponent<VeilController>();
                CollectionAssert.AreEquivalent(new[] { VeilForm.Sword }, veil.startUnlocked);
            }
            finally { Object.DestroyImmediate(hero); }
        }
    }
}
