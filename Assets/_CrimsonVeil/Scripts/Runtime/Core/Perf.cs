using System;
using Unity.Profiling;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// What the hero costs each frame, part by part (the motor, the pose, the motion capture and its clip samples, the
    /// ground and wall contact, the springs, the face, the scarf and its drawing, the wraiths): a profiler marker round
    /// each - Window &gt; Analysis &gt; Profiler shows their time and the managed memory each allocates (every byte
    /// garbage the collector must clear later, in a pause; see HeroMoveSetPlayTests.TheHero_AllocatesNoGarbage).
    /// In the editor and development builds only: in a release build a scope does nothing.
    /// </summary>
    public static class Perf
    {
        public enum Id { Player, Animator, Pose, Mocap, MocapSample, Ground, Walls, TouchUp, Springs, Veil, VeilDraw, Face, Enemy }

        /// <summary>The markers' names, by <see cref="Id"/>.</summary>
        public static readonly string[] Names =
        {
            "CV.Player", "CV.HeroAnimator", "CV.HeroAnimator.Pose", "CV.HeroMocap", "CV.HeroMocap.Sample",
            "CV.HeroAnimator.Ground", "CV.HeroAnimator.Walls", "CV.HeroWallTouchUp", "CV.SpringBones", "CV.Veil", "CV.VeilRenderer",
            "CV.HeroFace", "CV.EnemyAnimator",
        };

        static readonly ProfilerMarker[] Markers = Make();

        static ProfilerMarker[] Make()
        {
            var m = new ProfilerMarker[Names.Length];
            for (int i = 0; i < m.Length; i++) m[i] = new ProfilerMarker(Names[i]);
            return m;
        }

        /// <summary>Marks a part from here to the end of the using block.</summary>
        public static Scope Begin(Id id) => new Scope(id);

        public readonly struct Scope : IDisposable
        {
            readonly int id;

            public Scope(Id id)
            {
                this.id = (int)id;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Markers[this.id].Begin();
#endif
            }

            public void Dispose()
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Markers[id].End();
#endif
            }
        }
    }
}
