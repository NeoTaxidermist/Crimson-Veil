using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// The hero's motion-capture clips, baked onto his own rig (Editor: HeroMocapBaker): by key, whether they loop, and
    /// for a blow where in the clip the blade meets (the hand fastest), so the game can line its own hit up with it.
    /// </summary>
    [CreateAssetMenu(menuName = "Crimson Veil/Hero Mocap Library")]
    public sealed class HeroMocapLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string key;
            public AnimationClip clip;
            public bool loop;
            /// <summary>0..1 through the clip where a blow lands (0 when not a blow).</summary>
            public float impact;
        }

        public Entry[] entries = new Entry[0];

        public bool TryGet(string key, out Entry entry)
        {
            foreach (var e in entries)
                if (e.key == key && e.clip != null) { entry = e; return true; }
            entry = default;
            return false;
        }
    }
}
