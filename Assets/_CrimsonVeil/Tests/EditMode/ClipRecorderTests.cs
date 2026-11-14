using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The clip recorder plays the hero by its script of inputs (for review clips).</summary>
    public class ClipRecorderTests
    {
        [Test]
        public void ScriptedActions_ActLikeTheController()
        {
            var go = new GameObject("rec");
            try
            {
                var rec = go.AddComponent<ClipRecorder>();
                rec.Apply("move:-1");
                Assert.AreEqual(-1f, rec.MoveX);
                rec.Apply("cast:Sword");
                Assert.AreEqual(VeilForm.Sword, rec.CastPressed, "a cast is one press");
                Assert.IsNull(rec.CastPressed, "consumed by the first reader");
                rec.Apply("hold:Shield");
                Assert.IsTrue(rec.CastHeld(VeilForm.Shield), "the shield button is held down");
                rec.Apply("release");
                Assert.IsFalse(rec.CastHeld(VeilForm.Shield));
                rec.Apply("jump");
                Assert.IsTrue(rec.JumpPressed);
                Assert.IsFalse(rec.JumpPressed);
                Assert.IsTrue(rec.JumpHeld, "held until released, for a full jump");
                rec.Apply("release-jump");
                Assert.IsFalse(rec.JumpHeld);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
