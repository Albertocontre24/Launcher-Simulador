using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace Launcher.Tests
{
    public class ManifestProviderTests
    {
        [Fact]
        public async Task GetAsync_ValidJson_ReturnsManifest()
        {
            // Arrange
            var json = "{\"version\": \"1.5.0\"}";
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var provider = new LocalManifestProvider();

            var path = Path.GetTempFileName();
            await File.WriteAllTextAsync(path, "{\"version\": \"1.5.0\"}");

            var result = await provider.GetAsync(new Uri(path), CancellationToken.None);


            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.StartsWith("1.5.0", result.Value!.Version);
        }

        [Fact]
        public async Task GetAsync_InvalidJson_ReturnsFailure()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("not valid json")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var provider = new ManifestProvider(httpClient);

            // Act
            var result = await provider.GetAsync(new Uri("http://fake.url/manifest.json"), CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
        }
    }
}
