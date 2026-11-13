using UnityEngine;

namespace CrimsonVeil.World
{
    public sealed class RubbleProp : ProceduralProp
    {
        public int blocks = 9;
        public float width = 2.5f;
        public float maxSize = 0.8f;

        protected override Mesh Build() => ProceduralMeshes.Rubble(seed, blocks, width, maxSize);
    }
}
