using UnityEngine;

namespace CrimsonVeil.World
{
    public enum LevelLayer
    {
        Foreground,
        Playfield,
        Near,
        Mid,
        Far,
        Sky,
    }

    /// <summary>Marks which depth layer a decoration belongs to (z and ink value are set by the level editor).</summary>
    [DisallowMultipleComponent]
    public sealed class DepthLayer : MonoBehaviour
    {
        public LevelLayer layer = LevelLayer.Near;

        public static float Z(LevelLayer layer)
        {
            switch (layer)
            {
                case LevelLayer.Foreground: return -3.2f;
                case LevelLayer.Playfield: return 0f;
                case LevelLayer.Near: return 9f;
                case LevelLayer.Mid: return 28f;
                case LevelLayer.Far: return 65f;
                default: return 120f;
            }
        }
    }
}
