using System;
using CrimsonVeil.Combat;
using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// A crimson rope (crimson = "the veil can touch this"). Any veil weapon or arrow cuts it;
    /// cutting releases the linked <see cref="HingedBridge"/>.
    /// </summary>
    public sealed class CuttableRope : MonoBehaviour, IDamageable
    {
        [SerializeField] Transform start;
        [SerializeField] Transform end;
        [SerializeField] HingedBridge bridge;
        [SerializeField] Material material;
        [SerializeField] float width = 0.07f;
        [SerializeField] float sag = 0.25f;
        [SerializeField] int points = 12;

        public bool IsCut { get; private set; }
        public event Action Cut;

        LineRenderer line;
        CapsuleCollider hitbox;

        public void Configure(Transform a, Transform b, HingedBridge target, Material mat)
        {
            start = a;
            end = b;
            bridge = target;
            material = mat;
            Build();
        }

        void Awake() => Build();

        void Build()
        {
            if (start == null || end == null) return;
            if (line == null) line = GetComponent<LineRenderer>();
            if (line == null) line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = points;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) line.sharedMaterial = material;
            line.startColor = line.endColor = new Color(0.85f, 0.03f, 0.05f, 1f);

            if (hitbox == null)
            {
                var go = new GameObject("RopeHitbox");
                go.transform.SetParent(transform, false);
                hitbox = go.AddComponent<CapsuleCollider>();
                hitbox.isTrigger = true;
                hitbox.direction = 1;
            }
            UpdateGeometry();
        }

        void LateUpdate()
        {
            if (!IsCut) UpdateGeometry();
        }

        void UpdateGeometry()
        {
            if (line == null || start == null || end == null) return;
            Vector3 a = start.position, b = end.position;
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                line.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * (sag * 4f * t * (1f - t)));
            }
            var ht = hitbox.transform;
            ht.position = (a + b) * 0.5f + Vector3.down * sag;
            ht.rotation = Quaternion.FromToRotation(Vector3.up, b - a);
            hitbox.height = Vector3.Distance(a, b);
            hitbox.radius = 0.25f;
        }

        public bool ReceiveHit(in DamageInfo info)
        {
            if (IsCut) return false;
            IsCut = true;
            line.enabled = false;
            hitbox.enabled = false;
            CombatFx.Burst(info.point, Vector2.down, CombatFx.Crimson, 16, 3f);
            if (bridge != null) bridge.Release();
            Cut?.Invoke();
            return true;
        }
    }
}
