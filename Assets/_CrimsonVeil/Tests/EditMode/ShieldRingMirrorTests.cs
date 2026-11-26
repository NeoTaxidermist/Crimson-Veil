using CrimsonVeil.Veil;
using NUnit.Framework;

namespace CrimsonVeil.Tests
{
    /// <summary>The shield's ring keeps its side through a turn; only the root at the hand goes round with him.</summary>
    public class ShieldRingMirrorTests
    {
        [Test]
        public void TheRing_KeepsTheSideItWasThrownRoundOn_TheRootTurnsWithHim()
        {
            // thrown round facing right, halfway through a turn to the left
            Assert.AreEqual(1f, VeilController.ShieldRingMirror(0f, 1f, 1f), 1e-5f, "the ring: not squashed into a line");
            Assert.AreEqual(1f, VeilController.ShieldRingMirror(-1f, 1f, 1f), 1e-5f, "nor mirrored once he has turned");
            Assert.AreEqual(-1f, VeilController.ShieldRingMirror(-1f, 1f, 0f), 1e-5f, "the root at the hand turns with him");
            Assert.AreEqual(0.3f, VeilController.ShieldRingMirror(0.3f, -1f, 0f), 1e-5f);
            Assert.AreEqual(-1f, VeilController.ShieldRingMirror(1f, -1f, 1f), 1e-5f, "thrown round facing left it stays so");
        }
    }
}
