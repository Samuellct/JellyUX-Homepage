using System.Reflection;
using Jellyfin.Plugin.JuxHomepage.Inject;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Inject;

public sealed class FileTransformationDetectorTests
{
    [Fact]
    public void RegisterTransformation_TargetMethodThrows_DoesNotPropagateAndLogsError()
    {
        var loggerMock = new Mock<ILogger<FileTransformationDetector>>();
        var detector = new FileTransformationDetector(loggerMock.Object);

        // Simulate a successfully resolved RegisterTransformation MethodInfo whose invocation fails
        // at runtime (incompatible signature, or an exception thrown inside FileTransformation).
        var throwingMethod = typeof(ThrowingTarget).GetMethod(nameof(ThrowingTarget.RegisterTransformation))!;
        typeof(FileTransformationDetector)
            .GetField("_registerMethod", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(detector, throwingMethod);

        var exception = Record.Exception(() => detector.RegisterTransformation(new JObject()));

        Assert.Null(exception);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static class ThrowingTarget
    {
        public static void RegisterTransformation(JObject payload)
        {
            throw new InvalidOperationException("Simulated FileTransformation failure.");
        }
    }
}
