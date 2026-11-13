using UnityEngine;

namespace CrimsonVeil.World
{
    public sealed class BannerProp : ProceduralProp
    {
        public float width = 0.8f;
        public float height = 2.6f;

        protected override Mesh Build() => ProceduralMeshes.Banner(seed, width, height);
    }
}
