using CrimsonVeil.Veil;

namespace CrimsonVeil.Player
{
    /// <summary>Abstracts the input source so gameplay can be driven by tests, replays or AI.</summary>
    public interface IPlayerInput
    {
        float MoveX { get; }
        /// <summary>Vertical aim (-1 down .. 1 up): aims the bow, turns the spear into a downward thrust, climbs the whip.</summary>
        float AimY { get; }
        bool JumpPressed { get; }
        bool JumpHeld { get; }
        bool DashPressed { get; }
        /// <summary>
        /// A weapon cast this frame, if any. The veil forms the weapon, strikes at once and melts back into
        /// cloth — weapons are cast like spells, not switched. The attack button casts the sword.
        /// </summary>
        VeilForm? CastPressed { get; }
        /// <summary>Is the button of <paramref name="form"/> held down (drawing the bow, keeping the shield up)?</summary>
        bool CastHeld(VeilForm form);
    }
}
