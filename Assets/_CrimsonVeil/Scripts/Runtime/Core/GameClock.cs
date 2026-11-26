using UnityEngine;

namespace CrimsonVeil.Core
{
    /// <summary>
    /// The one place the game's clock is set: paused, it stands (whatever else wants it); otherwise it runs at the
    /// hit-stop's crawl while a blow lands, else at full speed.
    /// <para>(The pause menu and the hit-stop each set <c>Time.timeScale</c> themselves. Opened while a blow was landing,
    /// the menu remembered the crawl, the hit-stop's end set the world running again under the open menu - a foe
    /// struck the hero dead behind it - and closing it put the crawl back for good: the game went on at 3 %.)</para>
    /// </summary>
    public static class GameClock
    {
        public static bool Paused { get; private set; }
        static float hitStop = 1f;

        /// <summary>The scale a blow's hit-stop asks for (1 when none).</summary>
        public static float HitStopScale => hitStop;

        public static void Pause() { Paused = true; Apply(); }
        public static void Resume() { Paused = false; Apply(); }
        public static void SetHitStop(float scale) { hitStop = Mathf.Clamp01(scale); Apply(); }
        public static void EndHitStop() { hitStop = 1f; Apply(); }

        /// <summary>Running at full speed, nothing paused, nothing stopped (a new scene, the main menu).</summary>
        public static void Reset() { Paused = false; hitStop = 1f; Apply(); }

        static void Apply() => Time.timeScale = Paused ? 0f : hitStop;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnStart() { Paused = false; hitStop = 1f; }
    }
}
