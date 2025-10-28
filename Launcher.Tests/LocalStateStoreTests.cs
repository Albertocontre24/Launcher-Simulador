using System;
using System.IO;
using Launcher.Core.Services;
using Xunit;

namespace Launcher.Tests
{
    public class LocalStateStoreTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly LocalStateStore _store;

        public LocalStateStoreTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            _store = new LocalStateStore(_tempDir);
        }

        [Fact]
        public void SaveAndLoad_State_PersistsCorrectly()
        {
            var state = new LocalState { InstalledVersion = "1.2.3" };
            _store.Save(state);

            var loaded = _store.Load();
            Assert.Equal("1.2.3", loaded.InstalledVersion);
        }

        [Fact]
        public void Load_WhenNoFile_ReturnsDefault()
        {
            var emptyStore = new LocalStateStore(Path.Combine(_tempDir, "nonexistent"));
            var result = emptyStore.Load();

            Assert.NotNull(result);
            Assert.Null(result.InstalledVersion);
        }

        public void Dispose() => Directory.Delete(_tempDir, true);
    }
}
