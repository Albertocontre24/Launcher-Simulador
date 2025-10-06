using Microsoft.VisualStudio.TestTools.UnitTesting;
using Launcher.Core.Services;
using Launcher.Core.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Threading;
using System;

namespace Launcher.Tests;

[TestClass]
public class RemoteManifestServiceTests
{
    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _responder(request, cancellationToken);
        }
    }

    [TestMethod]
    public async Task GetRemoteManifestAsync_Success_ReturnsManifest()
    {
        var json = "{\"version\":\"1.2.3\",\"timestamp\":\"2025-10-01T12:00:00Z\",\"releaseNotes\":\"notes\",\"files\":[] }";

        var handler = new FakeHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));

        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var svc = new RemoteManifestService(client);

        var result = await svc.GetRemoteManifestAsync("http://localhost:8000/manifest.json");

        Assert.AreEqual(FetchStatus.Success, result.Status);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("1.2.3", result.Data!.Version);
    }

    [TestMethod]
    public async Task GetRemoteManifestAsync_InvalidJson_ReturnsFailure()
    {
        var invalid = "not a json";
        var handler = new FakeHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(invalid, Encoding.UTF8, "application/json")
        }));

        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var svc = new RemoteManifestService(client);

        var result = await svc.GetRemoteManifestAsync("http://localhost:8000/manifest.json");

        Assert.AreEqual(FetchStatus.Failure, result.Status);
        Assert.IsTrue(result.ErrorMessage?.ToLower().Contains("json") ?? false, "Expected JSON error message");
    }

    [TestMethod]
    public async Task GetRemoteManifestAsync_Timeout_ReturnsFailure()
    {
        var handler = new FakeHandler(async (req, ct) =>
        {
            // Delay longer than client timeout to trigger TaskCanceledException
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) };
        var svc = new RemoteManifestService(client);

        var result = await svc.GetRemoteManifestAsync("http://localhost:8000/manifest.json");

        Assert.AreEqual(FetchStatus.Failure, result.Status);
        Assert.IsTrue(result.ErrorMessage?.ToLower().Contains("timed out") ?? false, "Expected timeout message");
    }
}
