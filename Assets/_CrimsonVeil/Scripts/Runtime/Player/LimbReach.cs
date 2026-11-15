using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Two-bone reach (an arm: upper arm, forearm, hand): turns the two bones so the end lands on a point in the world,
    /// the middle joint bending towards a pole. Used where a hand must stay put on something while the body moves - the
    /// hands on the lip of a ledge while hanging and mantling.
    /// </summary>
    public static class LimbReach
    {
        /// <summary>
        /// Where the middle joint goes for a limb of lengths <paramref name="a"/> (root to middle) and <paramref name="b"/>
        /// (middle to end) from <paramref name="root"/> reaching for <paramref name="target"/>, bent towards <paramref name="pole"/>.
        /// Out of reach, the limb stretches straight towards the target.
        /// </summary>
        public static Vector3 Middle(Vector3 root, float a, float b, Vector3 target, Vector3 pole)
        {
            Vector3 to = target - root;
            float d = Mathf.Clamp(to.magnitude, Mathf.Abs(a - b) + 1e-4f, a + b - 1e-4f);
            Vector3 dir = to.sqrMagnitude > 1e-8f ? to.normalized : Vector3.down;
            float x = (a * a - b * b + d * d) / (2f * d);           // along the root -> target line
            float h = Mathf.Sqrt(Mathf.Max(0f, a * a - x * x));     // off it, towards the pole
            Vector3 side = Vector3.ProjectOnPlane(pole - root, dir);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.ProjectOnPlane(Vector3.down, dir);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.ProjectOnPlane(Vector3.forward, dir);
            return root + dir * x + side.normalized * h;
        }

        /// <summary>Turns <paramref name="upper"/> and <paramref name="lower"/> so <paramref name="end"/> reaches the target
        /// (blended by <paramref name="weight"/> from the pose they have now).</summary>
        public static void Solve(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 pole, float weight)
        {
            if (upper == null || lower == null || end == null || weight <= 0f) return;
            weight = Mathf.Clamp01(weight);
            Vector3 s = upper.position, e = lower.position, w = end.position;
            float a = Vector3.Distance(s, e), b = Vector3.Distance(e, w);
            if (a < 1e-4f || b < 1e-4f) return;
            Vector3 mid = Middle(s, a, b, target, pole);
            var turnUpper = Quaternion.FromToRotation(e - s, mid - s);
            upper.rotation = Quaternion.Slerp(Quaternion.identity, turnUpper, weight) * upper.rotation;
            e = lower.position;
            w = end.position;
            var turnLower = Quaternion.FromToRotation(w - e, target - e);
            lower.rotation = Quaternion.Slerp(Quaternion.identity, turnLower, weight) * lower.rotation;
        }
    }
}
