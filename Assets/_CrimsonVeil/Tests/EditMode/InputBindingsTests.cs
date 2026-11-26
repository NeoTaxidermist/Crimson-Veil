using System.Collections.Generic;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;

namespace CrimsonVeil.Tests
{
    /// <summary>The game's controls: the two-handed defaults, rebinding with a swap, and keeping it between sessions.</summary>
    [Category("UI")]
    public class InputBindingsTests
    {
        [SetUp] public void SetUp() => InputBindings.BuildDefaultsForTests();
        [TearDown] public void TearDown() => InputBindings.EndTests();

        static string Key(string id) => InputBindings.Find(id).action.bindings[0].effectivePath;

        [Test]
        public void TheWeapons_LieUnderTheRightHand_InTheirOrder_TheLeftHandMoves()
        {
            // the right hand: the attacks along U I O P in the order sword, spear, whip, bow; the held shield under them on J
            Assert.AreEqual("<Keyboard>/u", Key("Sword"));
            Assert.AreEqual("<Keyboard>/i", Key("Spear"));
            Assert.AreEqual("<Keyboard>/o", Key("Whip"));
            Assert.AreEqual("<Keyboard>/p", Key("Bow"));
            Assert.AreEqual("<Keyboard>/j", Key("Shield"));
            CollectionAssert.AreEqual(new[] { VeilForm.Sword, VeilForm.Spear, VeilForm.Whip, VeilForm.Bow, VeilForm.Shield }, InputBindings.Weapons);
            // the left hand: moving, aiming, jumping, dashing, the stance, looking, the map
            var leftHand = new[] { "<Keyboard>/a", "<Keyboard>/d", "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/space",
                "<Keyboard>/leftShift", "<Keyboard>/leftCtrl", "<Keyboard>/q", "<Keyboard>/e", "<Keyboard>/tab" };
            foreach (var id in new[] { "MoveLeft", "MoveRight", "AimUp", "AimDown", "Jump", "Dash", "Stand", "LookUp", "LookDown", "Map" })
                CollectionAssert.Contains(leftHand, Key(id), id);
        }

        [Test]
        public void NoKeyOrButton_DoesTwoThings()
        {
            foreach (int index in new[] { 0, 1 })
            {
                var seen = new HashSet<string>();
                foreach (var e in InputBindings.Entries)
                {
                    string p = e.action.bindings[index].effectivePath;
                    if (string.IsNullOrEmpty(p)) continue;
                    Assert.IsTrue(seen.Add(p), $"{p} is bound twice");
                }
            }
        }

        [Test]
        public void RebindingToATakenKey_SwapsTheTwo()
        {
            var sword = InputBindings.Find("Sword");
            InputBindings.Rebind(sword, 0, "<Keyboard>/i");
            Assert.AreEqual("<Keyboard>/i", Key("Sword"));
            Assert.AreEqual("<Keyboard>/u", Key("Spear"), "the spear takes the sword's old key");
            InputBindings.Rebind(InputBindings.Find("Bow"), 0, "<Keyboard>/k");
            Assert.AreEqual("<Keyboard>/k", Key("Bow"), "a free key is simply taken");
            var shield = InputBindings.Find("Shield");
            InputBindings.Rebind(shield, 1, "<Gamepad>/buttonSouth");
            Assert.AreEqual("<Gamepad>/buttonSouth", shield.action.bindings[1].effectivePath);
            Assert.AreEqual("<Gamepad>/leftTrigger", InputBindings.Find("Jump").action.bindings[1].effectivePath, "on the gamepad too");
        }

        [Test]
        public void TheBindings_AreKept_AndTheDefaultsComeBack()
        {
            InputBindings.Rebind(InputBindings.Find("Sword"), 0, "<Keyboard>/k");
            string json = InputBindings.ToJson();
            InputBindings.BuildDefaultsForTests();
            Assert.AreEqual("<Keyboard>/u", Key("Sword"), "fresh: the defaults");
            InputBindings.FromJson(json);
            Assert.AreEqual("<Keyboard>/k", Key("Sword"), "read back: as he left it");
            InputBindings.ResetAll();
            Assert.AreEqual("<Keyboard>/u", Key("Sword"), "reset: the defaults");
        }

        [Test]
        public void TheSettings_ShowCzechNames_AndTheSticksAsSticks()
        {
            Assert.AreEqual("Mezerník", InputBindings.Shown(InputBindings.Find("Jump"), false));
            Assert.AreEqual("U", InputBindings.Shown(InputBindings.Find("Sword"), false));
            Assert.AreEqual("Levá páčka", InputBindings.Shown(InputBindings.Find("MoveLeft"), true));
            Assert.AreEqual("Esc", InputBindings.Shown(InputBindings.Find("Pause"), false));
        }
    }
}
