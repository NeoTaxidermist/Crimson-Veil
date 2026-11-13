using UnityEngine;

namespace CrimsonVeil.World
{
    public sealed class UrnProp : ProceduralProp
    {
        public float height = 0.8f;

        protected override Mesh Build() => ProceduralMeshes.Urn(seed, height);
    }
}
