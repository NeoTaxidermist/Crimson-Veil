using UnityEngine;

namespace CrimsonVeil.World
{
    public sealed class ColumnProp : ProceduralProp
    {
        public float height = 6f;
        public float radius = 0.4f;
        public bool broken;

        protected override Mesh Build() => ProceduralMeshes.Column(height, radius, broken, seed);
    }
}
