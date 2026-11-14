using UnityEngine;

namespace CrimsonVeil.CameraSystem
{
    /// <summary>
    /// Where the side-on camera looks, platformer style (engine independent, testable):
    /// <list type="bullet">
    /// <item><b>Horizontal</b>: leads where the hero is <i>going</i> (smoothed velocity), not where he faces — turning
    /// round to strike or a tap of the stick does not swing the view. Standing, the lead relaxes towards centre.</item>
    /// <item><b>Vertical</b>: locks to the floor the hero stands on (platform snapping). Jumps do not move the
    /// camera; it re-frames when he lands on a different height or leaves a window around that floor
    /// (climbing high, falling far). A real fall is led a little so you see where you are landing.</item>
    /// <item>A jump of the hero across the level (respawn, teleport) snaps instead of a long pan.</item>
    /// </list>
    /// </summary>
    public sealed class CameraFocus
    {
        public float lookAhead = 2.2f;
        public float leadSmooth = 0.8f;
        /// <summary>The hero may rise this far above / sink this far below the floor line before the camera follows.</summary>
        public float windowUp = 3.2f, windowDown = 1.2f;
        public float maxFallLead = 3f;
        public float snapDistance = 14f;

        /// <summary>The floor line the camera is framed on.</summary>
        public float FloorY { get; private set; }
        /// <summary>Current horizontal lead (m).</summary>
        public float Lead { get; private set; }
        public Vector2 Focus { get; private set; }
        bool initialised;
        float leadVel, smoothVx;

        public void Reset(Vector2 hero)
        {
            FloorY = hero.y;
            Lead = 0f;
            leadVel = smoothVx = 0f;
            Focus = hero;
            initialised = true;
        }

        /// <summary>
        /// Advances the focus. <paramref name="supported"/> = standing, hanging on a ledge, climbing or swinging
        /// (the hero is "somewhere" — the floor line follows him there). Returns the point to look at.
        /// </summary>
        public Vector2 Step(float dt, Vector2 hero, Vector2 velocity, bool supported, float maxRunSpeed)
        {
            if (!initialised || (hero - Focus).magnitude > snapDistance + lookAhead + windowUp)
            {
                Reset(hero);
                return Focus;
            }

            // Lead from the movement, smoothed so short taps and turns do not swing the view.
            smoothVx = Mathf.Lerp(smoothVx, velocity.x, 1f - Mathf.Exp(-3f * dt));
            float wanted = Mathf.Clamp(smoothVx / Mathf.Max(0.1f, maxRunSpeed), -1f, 1f) * lookAhead;
            Lead = Mathf.SmoothDamp(Lead, wanted, ref leadVel, leadSmooth, Mathf.Infinity, dt);

            // Floor line: where he stands; in the air only when he leaves the window around it.
            if (supported) FloorY = hero.y;
            else if (hero.y > FloorY + windowUp) FloorY = hero.y - windowUp;
            else if (hero.y < FloorY - windowDown) FloorY = hero.y + windowDown;

            float lead = 0f;
            if (!supported && velocity.y < -6f && hero.y < FloorY + 0.1f)
                lead = -Mathf.Min(maxFallLead, (-velocity.y - 6f) * 0.25f); // a real fall: look a little ahead of it
            Focus = new Vector2(hero.x + Lead, FloorY + lead);
            return Focus;
        }
    }
}
