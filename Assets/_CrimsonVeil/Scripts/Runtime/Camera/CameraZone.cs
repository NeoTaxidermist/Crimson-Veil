using UnityEngine;

namespace CrimsonVeil.CameraSystem
{
    /// <summary>
    /// While the hero is inside [minX, maxX], the follow camera eases to a different offset —
    /// e.g. pulls back and up so a high puzzle element is in frame.
    /// </summary>
    public sealed class CameraZone : MonoBehaviour
    {
        public float minX;
        public float maxX = 10f;
        public Vector3 offset = new Vector3(0f, 4f, -16f);

        public bool Contains(float x) => x >= minX && x <= maxX;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
            Gizmos.DrawWireCube(new Vector3((minX + maxX) * 0.5f, transform.position.y + 3f, 0f), new Vector3(maxX - minX, 6f, 1f));
        }
    }
}
