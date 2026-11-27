using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// The shield monk's flail chain, engine independent: a row of nodes from the haft's tip (node 0, held) to the
    /// head (the last node), each segment its fixed length. Loose it hangs and swings (verlet, gravity, damping);
    /// in a blow the head is pulled onto the arc the blow draws (<see cref="ArcPoint"/>) and the links follow it.
    /// Kept in the picture's plane (z of the tip): the game is seen from the side.
    /// </summary>
    public sealed class FlailChain
    {
        readonly Vector3[] nodes, prev;
        readonly float[] lengths;

        /// <param name="segments">Segment lengths from the tip to the head (the last one: the head's eye to its centre).</param>
        public FlailChain(float[] segments, Vector3 tip)
        {
            lengths = (float[])segments.Clone();
            nodes = new Vector3[lengths.Length + 1];
            prev = new Vector3[nodes.Length];
            Reset(tip);
        }

        public int Count => nodes.Length;
        public Vector3 this[int i] => nodes[i];
        public Vector3 Head => nodes[nodes.Length - 1];

        /// <summary>Tip to head, all segments laid straight.</summary>
        public float Length
        {
            get { float s = 0f; foreach (var l in lengths) s += l; return s; }
        }

        /// <summary>Hanging straight down from the tip, at rest.</summary>
        public void Reset(Vector3 tip)
        {
            float y = 0f;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (i > 0) y += lengths[i - 1];
                nodes[i] = prev[i] = tip + Vector3.down * y;
            }
        }

        /// <summary>
        /// One step: the tip is moved to <paramref name="tip"/>, the rest swings under gravity; with
        /// <paramref name="pull"/> &gt; 0 the head is drawn that fraction of the way onto <paramref name="target"/>
        /// (a blow - the links drawn out after it), then every segment is set back to its length, from the held tip outwards.
        /// </summary>
        public void Step(Vector3 tip, float dt, float gravity = 9.81f, float damping = 0.985f, Vector3 target = default, float pull = 0f)
        {
            float z = tip.z;
            nodes[0] = prev[0] = tip;
            for (int i = 1; i < nodes.Length; i++)
            {
                var v = (nodes[i] - prev[i]) * damping;
                prev[i] = nodes[i];
                nodes[i] += v + Vector3.down * (gravity * dt * dt);
            }
            if (pull > 0f)
            {
                // the whole chain drawn out towards the blow's point (the outer links the most): pulling the head
                // alone, the links would hold it back where they hang
                var along = target - tip;
                along = along.sqrMagnitude > 1e-8f ? along.normalized : Vector3.down;
                float s = 0f, total = Length;
                for (int i = 1; i < nodes.Length; i++)
                {
                    s += lengths[i - 1];
                    float w = Mathf.Clamp01(pull) * (0.35f + 0.65f * s / total);
                    nodes[i] = Vector3.Lerp(nodes[i], tip + along * s, w);
                }
            }
            for (int it = 0; it < 6; it++)
            {
                nodes[0] = tip;
                for (int i = 1; i < nodes.Length; i++)
                {
                    var d = nodes[i] - nodes[i - 1];
                    float len = d.magnitude;
                    if (len < 1e-6f) { d = Vector3.down; len = 1f; }
                    nodes[i] = nodes[i - 1] + d / len * lengths[i - 1];
                }
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].z = z;
                prev[i].z = z;
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
