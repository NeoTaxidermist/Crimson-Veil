using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;

namespace CrimsonVeil.Tests
{
    /// <summary>
    /// The Windows build is the fast one: IL2CPP (C++, 240 fps in the level) and Direct3D 11 first (the game is held
    /// back by the processor, and Direct3D 12 cost it 0.6 ms a frame). Mono gave 161 fps, with 11 ms hitches.
    /// </summary>
    [Category("Tools")]
    public class BuildSettingsTests
    {
        [Test]
        public void TheWindowsBuild_IsIl2Cpp_Released()
        {
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));
            Assert.AreEqual(Il2CppCompilerConfiguration.Release, PlayerSettings.GetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone));
        }

        [Test]
        public void TheWindowsBuild_DrawsWithDirect3D11First()
        {
            Assert.IsFalse(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64), "chosen, not left to Unity (it picks Direct3D 12)");
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
            Assert.AreEqual(GraphicsDeviceType.Direct3D11, apis[0]);
            CollectionAssert.Contains(apis, GraphicsDeviceType.Direct3D12, "with Direct3D 12 to fall back on");
        }
    }
}
