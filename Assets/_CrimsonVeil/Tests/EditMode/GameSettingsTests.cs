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
            Assert.AreEqual(GameSettings.Heard(0.4f), AudioListener.volume, 1e-5f, "the overall volume is the listener's (as the ear hears the slider)");
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

        /// <summary>
        /// The sliders follow the ear: half way is a quarter of the loudness (-12 dB, not -6), and the effects start
        /// turned down (full, they were too loud).
        /// </summary>
        [Test]
        public void TheSliders_FollowTheEar_AndTheEffectsStartTurnedDown()
        {
            Assert.AreEqual(0.25f, GameSettings.Heard(0.5f), 1e-5f);
            Assert.AreEqual(1f, GameSettings.Heard(1f), 1e-5f);
            Assert.AreEqual(0f, GameSettings.Heard(0f), 1e-5f);
            Assert.Less(GameSettings.DefaultEffects, 1f, "the effects are not at full by default");
            Assert.AreEqual(GameSettings.Heard(GameSettings.DefaultEffects), GameSettings.EffectsLevel, 1e-5f);
            Assert.Less(20f * Mathf.Log10(GameSettings.EffectsLevel), -3f, "by default the effects play at least 3 dB under full");
        }

        [Test]
        public void Zrusit_TakesEveryChangeBack()
        {
            var opened = GameSettings.Capture();
            GameSettings.SetMaster(0.2f);
            GameSettings.SetMode(GameSettings.WindowMode.Window);
            GameSettings.SetVSync(!opened.vsync);
            GameSettings.SetResolution(new Vector2Int(800, 600));
            GameSettings.Restore(opened);
            Assert.AreEqual(opened.master, GameSettings.Master);
            Assert.AreEqual(opened.mode, GameSettings.Mode);
            Assert.AreEqual(opened.vsync, GameSettings.VSync);
            Assert.AreEqual(opened.resolution, GameSettings.Resolution);
            Assert.AreEqual(GameSettings.Heard(opened.master), AudioListener.volume, 1e-5f, "and the volume is heard as it was");
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
