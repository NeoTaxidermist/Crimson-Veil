using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Runs <see cref="HeroAnimator.WallTouchUp"/> at the end of the hero's animation: after the springs of the hair and
    /// the cloth (order 20) and the face (15), before the scarf reads where the neck is (50). Added by the animator.
    /// </summary>
    [DefaultExecutionOrder(30)]
    public sealed class HeroWallTouchUp : MonoBehaviour
    {
        public HeroAnimator Animator;

        void LateUpdate()
        {
            using var marker = CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.TouchUp);
            if (Animator != null && Animator.isActiveAndEnabled) Animator.WallTouchUp();
        }
    }
}
