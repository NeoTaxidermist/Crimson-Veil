using UnityEngine;

namespace CrimsonVeil.World
{
    public sealed class ArchProp : ProceduralProp
    {
        public float span = 4f;
        public float pillarWidth = 0.8f;
        public float springHeight = 4f;
        public float crown = 0.8f;
        public float depth = 1.2f;

        protected override Mesh Build() => ProceduralMeshes.Arch(span, pillarWidth, springHeight, crown, depth);
    }
}
