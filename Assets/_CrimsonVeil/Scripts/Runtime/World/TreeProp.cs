using UnityEngine;

namespace CrimsonVeil.World
{
    public sealed class TreeProp : ProceduralProp
    {
        public float height = 9f;
        public float trunkRadius = 0.45f;
        public int depth = 5;
        public int leavesPerTip = 14;
        public float leafSize = 0.22f;

        protected override Mesh Build() => ProceduralMeshes.Tree(seed, height, trunkRadius, depth, leavesPerTip, leafSize);
    }
}
