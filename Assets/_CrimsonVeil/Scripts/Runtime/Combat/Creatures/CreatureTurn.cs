using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// A creature turning round in 3D instead of a mirrored sprite flip: the body yaws through 180°,
    /// passing the moment where it faces the camera, eased in and out. Creature models are authored
    /// facing +x (yaw 0); yaw 180 faces -x; yaw 90 faces the camera (-z).
    /// </summary>
    public struct CreatureTurn
    {
        /// <summary>Turn progress 0 (facing +x) … 1 (facing -x).</summary>
        public float progress;

        public CreatureTurn(int facing) => progress = facing > 0 ? 0f : 1f;

        /// <summary>Advances the turn towards <paramref name="facing"/>; a full turn takes <paramref name="duration"/> seconds.</summary>
        public Quaternion Step(int facing, float dt, float duration = 0.35f)
        {
            progress = Mathf.MoveTowards(progress, facing > 0 ? 0f : 1f, dt / Mathf.Max(0.01f, duration));
            return Rotation;
        }

        /// <summary>Eased yaw in degrees (0 … 180, through 90 = towards the camera).</summary>
        public float Yaw => 180f * Mathf.SmoothStep(0f, 1f, progress);

        public Quaternion Rotation => Quaternion.Euler(0f, Yaw, 0f);

        /// <summary>True while mid-turn (neither side reached).</summary>
        public bool Turning => progress > 0.001f && progress < 0.999f;
    }
}
