using UnityEngine;

namespace CrimsonVeil.World
{
    public sealed class GrassProp : ProceduralProp
    {
        public int blades = 40;
        public float width = 3f;
        public Vector2 heightRange = new Vector2(0.3f, 1.1f);
        public float bladeWidth = 0.05f;

        protected override Mesh Build() => ProceduralMeshes.Grass(seed, blades, width, heightRange, bladeWidth);
    }
}
