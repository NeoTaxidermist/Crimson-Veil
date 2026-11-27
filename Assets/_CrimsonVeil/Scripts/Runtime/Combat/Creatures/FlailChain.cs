using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// The shield monk's flail chain, engine independent: a row of nodes from the haft's tip (node 0, held) to the
    /// head (the last node), each segment its fixed length. The chain swings as one damped pendulum hung from the tip,
    /// driven by gravity and by the fist's own acceleration (it trails a fist that sets off, swings out when it stops);
    /// in a blow its angle is drawn onto the arc the blow draws (<see cref="ArcPoint"/>). Kept in the picture's plane
    /// (z of the tip): the game is seen from the side.
    /// <para>(A free verlet chain was tried first: frames of uneven length and the fist bobbing with every step kicked
    /// it into whirling over the haft, and folded it up. One pendulum angle stays sane at any frame rate.)</para>
    /// </summary>
    public sealed class FlailChain
    {
        readonly Vector3[] nodes;
        readonly float[] lengths;
        readonly float length;
        float angle, omega;        // radians from hanging straight down, + towards +x
        Vector3 lastTip, tipVelocity, tipAccel;
        bool hasLast;

        /// <param name="segments">Segment lengths from the tip to the head (the last one: the head's eye to its centre).</param>
        public FlailChain(float[] segments, Vector3 tip)
        {
            lengths = (float[])segments.Clone();
            nodes = new Vector3[lengths.Length + 1];
            foreach (var l in lengths) length += l;
            Reset(tip);
        }

        public int Count => nodes.Length;
        public Vector3 this[int i] => nodes[i];
        public Vector3 Head => nodes[nodes.Length - 1];

        /// <summary>Tip to head, all segments laid straight.</summary>
        public float Length => length;

        /// <summary>The chain's angle from hanging straight down (degrees, + towards +x).</summary>
        public float Angle => angle * Mathf.Rad2Deg;

        /// <summary>Hanging straight down from the tip, at rest.</summary>
        public void Reset(Vector3 tip)
        {
            angle = omega = 0f;
            tipVelocity = tipAccel = Vector3.zero;
            lastTip = tip;
            hasLast = false;
            Lay(tip);
        }

        /// <summary>
        /// One step: the tip is moved to <paramref name="tip"/> and the chain swings under gravity and the tip's
        /// acceleration (<paramref name="damping"/>: the swing speed kept per 1/60 s); with <paramref name="pull"/> &gt; 0
        /// its angle is drawn that fraction of the way towards <paramref name="target"/> (a blow); with
        /// <paramref name="maxSwing"/> &gt; 0 it never swings further than that many degrees from hanging (carried, a
        /// heavy chain jolted by every step of a walk must not end up over the fist).
        /// </summary>
        public void Step(Vector3 tip, float dt, float gravity = 9.81f, float damping = 0.985f, Vector3 target = default, float pull = 0f,
                         float maxSwing = 0f)
        {
            if (dt <= 0f) { Lay(tip); return; }
            // the fist's acceleration, smoothed (one jolt of a frame must not fling it)
            if (hasLast)
            {
                var v = (tip - lastTip) / dt;
                var a = (v - tipVelocity) / dt;
                tipAccel = Vector3.Lerp(tipAccel, Vector3.ClampMagnitude(a, 40f), 1f - Mathf.Exp(-25f * dt));
                tipVelocity = v;
            }
            hasLast = true;
            lastTip = tip;
            if (pull > 0f)
            {
                var d = target - tip;
                float want = Mathf.Atan2(d.x, -d.y);
                float before = angle;
                angle += Mathf.DeltaAngle(angle * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad * Mathf.Clamp01(pull);
                omega = (angle - before) / dt;
            }
            else
            {
                // tangential: gravity and the pseudo-force of the accelerating pivot, damped; stepped in short parts
                int n = Mathf.Clamp(Mathf.CeilToInt(dt * 120f), 1, 8);
                float h = dt / n, keep = Mathf.Pow(damping, h * 60f);
                for (int i = 0; i < n; i++)
                {
                    float s = Mathf.Sin(angle), c = Mathf.Cos(angle);
                    float alpha = (-gravity * s - (tipAccel.x * c + tipAccel.y * s)) / Mathf.Max(0.05f, length);
                    omega = (omega + alpha * h) * keep;
                    angle += omega * h;
                }
            }
            if (maxSwing > 0f)
            {
                float lim = maxSwing * Mathf.Deg2Rad;
                if (angle > lim) { angle = lim; if (omega > 0f) omega = 0f; }
                if (angle < -lim) { angle = -lim; if (omega < 0f) omega = 0f; }
            }
            Lay(tip);
        }

        void Lay(Vector3 tip)
        {
            var dir = new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f);
            float s = 0f;
            nodes[0] = tip;
            for (int i = 1; i < nodes.Length; i++)
            {
                s += lengths[i - 1];
                nodes[i] = tip + dir * s;
            }
        }

        /// <summary>
        /// A point on the circle a blow draws round <paramref name="centre"/>: <paramref name="degrees"/> 0 = straight
        /// ahead (the way he faces), 90 = straight up, 180 = behind him, -90 = straight down.
        /// </summary>
        public static Vector3 ArcPoint(Vector3 centre, int facing, float degrees, float radius)
        {
            float a = degrees * Mathf.Deg2Rad;
            return centre + new Vector3(facing * Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
        }

        /// <summary>Where the head is on its arc while he whirls it up (t 0..1): from hanging, once round over his
        /// head and back, cocked high behind him at the end.</summary>
        public static float WindupAngle(float t) => -90f - 480f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        /// <summary>The blow (t 0..1): from high behind him over his head and down in front, fast in.</summary>
        public static float SwingAngle(float t)
        {
            t = Mathf.Clamp01(t);
            float snap = 1f - (1f - t) * (1f - t) * (1f - t);
            return 150f - 190f * snap;
        }

        /// <summary>The head hurts only on the way down in front: above him and ahead to below the shoulder.</summary>
        public static bool Striking(float degrees) => degrees <= 75f && degrees >= -45f;
    }
}
