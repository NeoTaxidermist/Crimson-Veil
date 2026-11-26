using CrimsonVeil.Core;
using NUnit.Framework;

namespace CrimsonVeil.Tests
{
    /// <summary>The built game's measuring mode stays out of the way unless it is asked for on the command line.</summary>
    [Category("Tools")]
    public class BenchmarkTests
    {
        [Test]
        public void WithoutItsArgument_TheBenchmarkDoesNothing()
        {
            Assert.IsNull(Benchmark.Argument("-cvbench"), "the editor is not started with -cvbench: no measuring");
            Assert.IsNull(Benchmark.Argument("-no-such-argument"));
            Assert.Greater(Benchmark.Measure, 5f, "measured long enough to give a steady median");
            Assert.Greater(Benchmark.Settle, 1f, "after the scene has settled (loading hitches left out)");
        }
    }
}
