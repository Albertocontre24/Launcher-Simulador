using System.Threading.Tasks;
using Launcher.Core.Models;

namespace Launcher.Core.Services;

public interface IRemoteManifestService
{
    /// <summary>
    /// Fetches the remote manifest from the provided URL (manifest.json)
    /// </summary>
    /// <param name="manifestUrl">Absolute URL to the manifest.json</param>
    /// <returns>FetchResult containing RemoteManifest or Failure with message</returns>
    Task<FetchResult<RemoteManifest>> GetRemoteManifestAsync(string manifestUrl);
}
