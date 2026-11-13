using CrimsonVeil.Veil;

namespace CrimsonVeil.Player
{
    /// <summary>Abstracts the input source so gameplay can be driven by tests, replays or AI.</summary>
    public interface IPlayerInput
    {
        float MoveX { get; }
        /// <summary>Vertical aim (-1 down .. 1 up), used e.g. to loose arrows upwards.</summary>
        float AimY { get; }
        bool JumpPressed { get; }
        bool JumpHeld { get; }
        bool DashPressed { get; }
        bool AttackPressed { get; }
        /// <summary>Form key pressed this frame, if any.</summary>
        VeilForm? FormRequested { get; }
    }
}
