using CrimsonVeil.Core;
using NUnit.Framework;
using UnityEngine;

namespace CrimsonVeil.Tests
{
    /// <summary>The sound and picture settings: volumes in range, the choices going round, the labels in Czech.</summary>
    [Category("UI")]
    public class GameSettingsTests
    {
        float listenerBefore;

        [SetUp] public void SetUp() { listenerBefore = AudioListener.volume; GameSettings.BeginTests(); }
        [TearDown] public void TearDown() { GameSettings.EndTests(); AudioListener.volume = listenerBefore; }

        [Test]
        public void TheVolumes_StayInRange_AndTheOverallOneDrivesTheListener()
        {
            GameSettings.SetMaster(0.4f);
            Assert.AreEqual(0.4f, GameSettings.Master, 1e-5f);
            Assert.AreEqual(0.4f, AudioListener.volume, 1e-5f, "the overall volume is the listener's");
            GameSettings.SetMusic(1.7f);
            Assert.AreEqual(1f, GameSettings.Music, "no louder than full");
            GameSettings.SetEffects(-0.2f);
            Assert.AreEqual(0f, GameSettings.Effects, "no quieter than silent");
            GameSettings.SetMusic(float.NaN);
            Assert.AreEqual(0f, GameSettings.Music);
            GameSettings.ResetSound();
            Assert.AreEqual(GameSettings.DefaultMaster, GameSettings.Master);
            Assert.AreEqual(GameSettings.DefaultMusic, GameSettings.Music);
            Assert.AreEqual(GameSettings.DefaultEffects, GameSettings.Effects);
        }

        [Test]
        public void TheChoices_GoRound_BothWays()
        {
            Assert.AreEqual(1, GameSettings.Cycle(0, 3, 1));
            Assert.AreEqual(0, GameSettings.Cycle(2, 3, 1), "past the last: the first");
            Assert.AreEqual(2, GameSettings.Cycle(0, 3, -1), "before the first: the last");
            Assert.AreEqual(0, GameSettings.Cycle(5, 0, 1), "nothing to choose from");
            GameSettings.SetMode(GameSettings.WindowMode.Window);
            Assert.AreEqual(FullScreenMode.Windowed, GameSettings.ToUnity(GameSettings.Mode));
            Assert.AreEqual(FullScreenMode.FullScreenWindow, GameSettings.ToUnity(GameSettings.WindowMode.Borderless));
            Assert.AreEqual(FullScreenMode.ExclusiveFullScreen, GameSettings.ToUnity(GameSettings.WindowMode.Fullscreen));
        }

        [Test]
        public void TheSettings_SpeakCzech()
        {
            Assert.AreEqual("80 %", GameSettings.Percent(0.8f));
            Assert.AreEqual("1920 × 1080", GameSettings.Label(new Vector2Int(1920, 1080)));
            Assert.AreEqual("V okně", GameSettings.Label(GameSettings.WindowMode.Window));
            Assert.AreEqual("Celá obrazovka", GameSettings.Label(GameSettings.WindowMode.Fullscreen));
        }

        [Test]
        public void TheResolutions_AreListedOnce_SmallestFirst()
        {
            var list = GameSettings.Resolutions();
            for (int i = 1; i < list.Count; i++)
            {
                Assert.AreNotEqual(list[i - 1], list[i], "each resolution once, not once per refresh rate");
                Assert.IsTrue(list[i - 1].x < list[i].x || (list[i - 1].x == list[i].x && list[i - 1].y < list[i].y));
            }
        }
    }
}
