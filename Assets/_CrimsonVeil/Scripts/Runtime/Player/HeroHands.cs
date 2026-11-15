using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The hero's fingers (the rig's Finger.&lt;side&gt;.&lt;finger&gt;.&lt;joint&gt; and Thumb.&lt;side&gt;.&lt;joint&gt; bones): each
    /// hand closes round what it holds. The sword hand clenches round the hilt, the stock, the shaft, the bow's grip; the
    /// other hand pinches the string while drawing; an empty hand stays relaxed.
    /// Each joint curls toward the palm about an axis worked out from the rest pose (the finger's direction and the
    /// palm's), so it doesn't depend on how the bones' local axes came through the export.
    /// </summary>
    public sealed class HeroHands
    {
        /// <summary>Extra curl (degrees) of the three finger joints in a full fist, on top of the modelled relaxed curl.</summary>
        public static readonly float[] FistCurl = { 48f, 62f, 34f };
        /// <summary>The thumb's second joint, closing over the fingers.</summary>
        public const float ThumbCurl = 35f;

        sealed class Joint
        {
            public Transform bone;
            public Quaternion rest;
            public Vector3 axis; // local bend axis
        }

        sealed class Hand
        {
            public Joint[][] fingers; // 4 x 3
            public Joint thumb0, thumb1;
            public Quaternion thumbClose; // local delta of the thumb's base joint in a fist
            public float grip;
        }

        readonly Hand right, left;
        public bool IsReady => right != null || left != null;

        /// <summary>How closed each hand is now, 0 relaxed .. 1 a fist.</summary>
        public float RightGrip => right != null ? right.grip : 0f;
        public float LeftGrip => left != null ? left.grip : 0f;

        public HeroHands(Transform rigRoot, Transform bodyCentre)
        {
            right = Build(rigRoot, "R", bodyCentre);
            left = Build(rigRoot, "L", bodyCentre);
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        static Hand Build(Transform root, string side, Transform bodyCentre)
        {
            var handBone = Find(root, "Hand." + side);
            if (handBone == null) return null;
            // As many three-jointed fingers as the hand has (the hero four, the wraith's claws three), front to back.
            var list = new System.Collections.Generic.List<Transform[]>();
            for (int i = 0; i < 8; i++)
            {
                var f = new Transform[3];
                for (int k = 0; k < 3; k++) f[k] = Find(handBone, $"Finger.{side}.{i}.{k}");
                if (f[0] == null || f[1] == null || f[2] == null) break;
                list.Add(f);
            }
            if (list.Count < 2) return null;
            var bones = list.ToArray();
            int count = bones.Length;
            var t0 = Find(handBone, $"Thumb.{side}.0");
            var t1 = Find(handBone, $"Thumb.{side}.1");
            if (t0 == null || t1 == null) return null;

            // The palm's frame in the rest pose: down the hand, front (index side) and towards the palm.
            Vector3 mid = Vector3.zero;
            foreach (var f in bones) mid += f[0].position;
            Vector3 down = (mid / count - handBone.position).normalized;
            Vector3 front = (bones[0][0].position - bones[count - 1][0].position).normalized;
            Vector3 palmIn = Vector3.Cross(front, down).normalized;
            Vector3 toBody = bodyCentre != null ? bodyCentre.position - handBone.position : -handBone.right;
            toBody -= Vector3.Project(toBody, down);
            if (Vector3.Dot(palmIn, toBody) < 0f) palmIn = -palmIn; // the palm faces the body

            var hand = new Hand { fingers = new Joint[count][] };
            for (int i = 0; i < count; i++)
            {
                hand.fingers[i] = new Joint[3];
                for (int k = 0; k < 3; k++)
                {
                    var b = bones[i][k];
                    Vector3 dir = k < 2 ? bones[i][k + 1].position - b.position : b.position - bones[i][k - 1].position;
                    hand.fingers[i][k] = MakeJoint(b, dir, palmIn);
                }
            }
            Vector3 thumbDir = t1.position - t0.position;
            hand.thumb0 = MakeJoint(t0, thumbDir, palmIn);
            hand.thumb1 = MakeJoint(t1, thumbDir, palmIn);
            // In a fist the thumb swings from pointing forward to across the front of the curled fingers.
            Vector3 closed = (down * 0.75f + palmIn * 0.55f - front * 0.35f).normalized;
            Quaternion world = Quaternion.FromToRotation(thumbDir.normalized, closed);
            hand.thumbClose = Quaternion.Inverse(t0.rotation) * world * t0.rotation;
            return hand;
        }

        static Joint MakeJoint(Transform b, Vector3 dir, Vector3 palmIn)
        {
            // Turning a finger about (direction x palm) by a positive angle bends it towards the palm.
            Vector3 axisWorld = Vector3.Cross(dir.normalized, palmIn).normalized;
            return new Joint { bone = b, rest = b.localRotation, axis = b.InverseTransformDirection(axisWorld) };
        }

        /// <summary>Grip of the hand that raises the shield: half open, the ring floats off the palm.</summary>
        public const float ShieldGrip = 0.45f;

        /// <summary>
        /// How far each hand closes: the sword hand round a formed weapon (half open under the shield, which floats
        /// round the body off the palm), the other hand round the bow string while it is drawn.
        /// </summary>
        public static void Targets(VeilForm form, float weaponHeld, bool drawingString, out float rightGrip, out float leftGrip)
        {
            rightGrip = HeroPoseSolver.FistWeight(weaponHeld) * (form == VeilForm.Shield ? ShieldGrip : 1f);
            leftGrip = drawingString ? 0.85f : 0f;
        }

        /// <summary>Close the hands towards the wanted grips (quickly: a hand snaps round a hilt).</summary>
        public void Apply(float rightTarget, float leftTarget, float dt)
        {
            Pose(right, rightTarget, dt);
            Pose(left, leftTarget, dt);
        }

        static void Pose(Hand h, float target, float dt)
        {
            if (h == null) return;
            // (below 0 the fingers straighten and splay back: a claw thrown open)
            h.grip = Mathf.MoveTowards(h.grip, Mathf.Clamp(target, -0.6f, 1f), dt / 0.08f);
            float g = h.grip;
            for (int i = 0; i < h.fingers.Length; i++)
                for (int k = 0; k < 3; k++)
                {
                    var j = h.fingers[i][k];
                    j.bone.localRotation = j.rest * Quaternion.AngleAxis(FistCurl[k] * g, j.axis);
                }
            h.thumb0.bone.localRotation = h.thumb0.rest * Quaternion.Slerp(Quaternion.identity, h.thumbClose, g);
            h.thumb1.bone.localRotation = h.thumb1.rest * Quaternion.AngleAxis(ThumbCurl * g, h.thumb1.axis);
        }
    }
}
