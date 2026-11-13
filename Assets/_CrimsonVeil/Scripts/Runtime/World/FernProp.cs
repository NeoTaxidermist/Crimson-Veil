using UnityEngine;

namespace CrimsonVeil.World
{
    public sealed class FernProp : ProceduralProp
    {
        public int fronds = 9;
        public float size = 1.6f;

        protected override Mesh Build() => ProceduralMeshes.Fern(seed, fronds, size);
    }
}
