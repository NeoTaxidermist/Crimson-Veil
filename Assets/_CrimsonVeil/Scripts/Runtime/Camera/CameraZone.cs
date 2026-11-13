using UnityEngine;

namespace CrimsonVeil.CameraSystem
{
    /// <summary>
    /// While the hero is inside the zone, the follow camera eases to a different offset —
    /// e.g. pulls back and up so a high puzzle element is in frame.
    /// The range is <b>relative to this object's position</b>, so the zone moves with prefabs.
    /// </summary>
    public sealed class CameraZone : MonoBehaviour
    {
        [Tooltip("Left edge of the zone, relative to this object's x.")]
        public float minX = -10f;
        [Tooltip("Right edge of the zone, relative to this object's x.")]
        public float maxX = 10f;
        public Vector3 offset = new Vector3(0f, 4f, -16f);

        public float WorldMinX => transform.position.x + minX;
        public float WorldMaxX => transform.position.x + maxX;

        public bool Contains(float x) => x >= WorldMinX && x <= WorldMaxX;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
            Gizmos.DrawWireCube(new Vector3((WorldMinX + WorldMaxX) * 0.5f, transform.position.y + 3f, 0f), new Vector3(maxX - minX, 6f, 1f));
        }
    }
}
