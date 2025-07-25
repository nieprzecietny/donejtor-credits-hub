using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace SponsorWebhook.Tests;

public class SponsorWebhookTests
{
    [Fact]
    public async Task Run_WritesData_ToMocks()
    {
        var context = new SimpleFunctionContext();
        string payload = "{ \"sponsorship\": { \"sponsor\": { \"login\": \"octocat\" } } }";
        var request = new TestHttpRequestData(context, payload);

        var mock = new Mock<SponsorFunctions.SponsorWebhook>(NullLoggerFactory.Instance) { CallBase = true };
        mock.Protected().Setup<Task>("SaveToTableAsync", ItExpr.IsAny<string>(), "octocat", payload)
            .Returns(Task.CompletedTask).Verifiable();
        mock.Protected().Setup<Task>("CommitToRepositoryAsync", ItExpr.IsAny<string>(), "octocat")
            .Returns(Task.CompletedTask).Verifiable();

        await mock.Object.Run(request);

        mock.Protected().Verify("SaveToTableAsync", Times.Once(), ItExpr.IsAny<string>(), "octocat", payload);
        mock.Protected().Verify("CommitToRepositoryAsync", Times.Once(), ItExpr.IsAny<string>(), "octocat");
    }
}
