using Launcher.Core.Utils;
using Xunit;

namespace Launcher.Tests
{
    public class VersionComparerTests
    {
        private readonly IVersionComparer _comparer = new VersionComparer();

        [Theory]
        [InlineData("1.0.0", "1.1.0", -1)]
        [InlineData("1.2.0", "1.2.0", 0)]
        [InlineData("2.0.0", "1.9.9", 1)]
        [InlineData("1.0.0-alpha", "1.0.0", -1)]
        [InlineData("1.0.1", "1.0.0-beta", 1)]
        public void Compare_SemVer_ReturnsExpectedResult(string v1, string v2, int expected)
        {
            var result = _comparer.Compare(v1, v2);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsNewerThan_ReturnsTrue_WhenVersionIsHigher()
        {
            Assert.True(_comparer.IsNewerThan("1.1.0", "1.0.0"));
        }
    }
}
