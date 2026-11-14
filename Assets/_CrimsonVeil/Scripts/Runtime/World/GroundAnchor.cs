using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>
    /// Keeps a ground plant (grass, fern, bramble, weeds, fallen leaves) rooted in the terrain under it while
    /// you edit the level. Whenever it is moved or changes layer it drops onto the topmost terrain block
    /// below and takes that block's depth:
    /// <list type="bullet">
    /// <item><b>Popředí</b>: rooted in the block's front lip (just behind the front face), so it moves with
    /// the rock in parallax instead of sliding along it or floating over a pit.</item>
    /// <item><b>Hrací plocha</b>: stays on the block's top surface (depth clamped inside the block).</item>
    /// <item>Other layers have no terrain: left alone.</item>
    /// </list>
    /// Over a pit it is flagged (red in the Scene view, reported by the Level Editor's check).
    /// Edit-time only; at runtime it does nothing.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GroundAnchor : MonoBehaviour
    {
        [Tooltip("Jak hluboko jsou kořeny zapuštěné pod hranu terénu (m).")]
        [Min(0f)] public float sink = 0.1f;

        /// <summary>False when the last snap found no terrain under the plant.</summary>
        public bool OnGround { get; private set; } = true;

        const float LipInset = 0.1f;
        const float SearchAbove = 1.5f;

        void OnEnable()
        {
            if (!Application.isPlaying) transform.hasChanged = true;
        }

        void Update()
        {
            if (Application.isPlaying || !transform.hasChanged) return;
            if (!gameObject.scene.IsValid()) return; // prefab assets on disk are never snapped
            Snap();
            transform.hasChanged = false;
        }

        /// <summary>Drops the plant onto the terrain under it. Returns false over a pit.</summary>
        public bool Snap()
        {
            var tag = GetComponent<DepthLayer>();
            var layer = tag != null ? tag.layer : LevelLayer.Foreground;
            if (layer != LevelLayer.Foreground && layer != LevelLayer.Playfield) return OnGround = true;

            Physics.SyncTransforms();
            var p = transform.position;
            if (!FindGround(p.x, p.y + SearchAbove, out float top, out var block)) return OnGround = false;
            float z = layer == LevelLayer.Foreground ? LipZ(block) : Mathf.Clamp(p.z, LipZ(block), BackZ(block));
            transform.position = new Vector3(p.x, top - sink, z);
            return OnGround = true;
        }

        /// <summary>Topmost terrain block surface at x that lies below <paramref name="fromY"/>.</summary>
        public static bool FindGround(float x, float fromY, out float top, out TerrainBlock block)
        {
            top = float.NegativeInfinity;
            block = null;
            foreach (var hit in Physics.RaycastAll(new Vector3(x, fromY, 0f), Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore))
            {
                var b = hit.collider.GetComponentInParent<TerrainBlock>();
                if (b == null || hit.point.y <= top) continue;
                top = hit.point.y;
                block = b;
            }
            return block != null;
        }

        /// <summary>Depth just behind the block's front face: where foreground plants take root.</summary>
        public static float LipZ(TerrainBlock b) => b.transform.position.z - b.depth * 0.5f + LipInset;

        static float BackZ(TerrainBlock b) => b.transform.position.z + b.depth * 0.5f - LipInset;

        void OnDrawGizmos()
        {
            if (OnGround) return;
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.9f);
            var p = transform.position;
            Gizmos.DrawWireSphere(p, 0.4f);
            Gizmos.DrawLine(p, p + Vector3.down * 3f);
        }
    }
}
