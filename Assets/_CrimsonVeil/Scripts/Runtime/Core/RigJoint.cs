using System;
using UnityEngine;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// A bone driven by a single sagittal angle. The rotation axis and its sign are calibrated from
    /// the rig at startup (rotate a little, see where a probe point moves), so procedural animation
    /// works regardless of bone roll or the axis convention of the imported model.
    /// </summary>
    public sealed class RigJoint
    {
        public Transform Transform { get; private set; }
        public Quaternion Rest { get; private set; }
        public Vector3 Axis { get; private set; }

        /// <param name="worldAxis">Character's lateral axis in world space.</param>
        /// <param name="probe">Point that moves with the bone (usually its child).</param>
        /// <param name="measure">Direction in which +angle should move the probe.</param>
        public static RigJoint Create(Transform t, Vector3 worldAxis, Func<Vector3> probe, Vector3 measure)
        {
            if (t == null) return null;
            var j = new RigJoint { Transform = t, Rest = t.localRotation };
            Vector3 local = Quaternion.Inverse(t.rotation) * worldAxis;

            Vector3 before = probe();
            t.localRotation = j.Rest * Quaternion.AngleAxis(10f, local);
            Vector3 after = probe();
            t.localRotation = j.Rest;

            j.Axis = Vector3.Dot(after - before, measure) >= 0f ? local : -local;
            return j;
        }

        public void Set(float degrees)
        {
            Transform.localRotation = Rest * Quaternion.AngleAxis(degrees, Axis);
        }

        public static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
