using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Octokit;

namespace SponsorFunctions;

public class SponsorWebhook
{
    private readonly ILogger _logger;

    public SponsorWebhook(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SponsorWebhook>();
    }

    [Function("SponsorWebhook")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("GitHub Sponsors webhook received");

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        string? secret = Environment.GetEnvironmentVariable("GITHUB_WEBHOOK_SECRET");
        if (!string.IsNullOrEmpty(secret))
        {
            if (!req.Headers.TryGetValues("X-Hub-Signature-256", out var sigHeaders))
            {
                var res = req.CreateResponse(HttpStatusCode.Unauthorized);
                await res.WriteStringAsync("Missing signature");
                return res;
            }
            var signatureHeader = sigHeaders.First();
            var parts = signatureHeader.Split('=');
            if (parts.Length != 2 || parts[0] != "sha256")
            {
                var res = req.CreateResponse(HttpStatusCode.Unauthorized);
                await res.WriteStringAsync("Invalid signature");
                return res;
            }
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(bodyBytes);
            var expected = Convert.ToHexString(hash).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(parts[1])))
            {
                var res = req.CreateResponse(HttpStatusCode.Unauthorized);
                await res.WriteStringAsync("Invalid signature");
                return res;
            }
        }

        JsonDocument payload;
        try
        {
            payload = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteStringAsync("Invalid JSON");
            return res;
        }

        if (!payload.RootElement.TryGetProperty("sponsorship", out var sponsorship) ||
            !sponsorship.TryGetProperty("sponsor", out var sponsor) ||
            !sponsor.TryGetProperty("login", out var loginProp))
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteStringAsync("Missing sponsor login");
            return res;
        }

        string sponsorLogin = loginProp.GetString() ?? string.Empty;
        if (string.IsNullOrEmpty(sponsorLogin))
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteStringAsync("Missing sponsor login");
            return res;
        }

        string sponsorGuid = Guid.NewGuid().ToString();

        string tableConn = Environment.GetEnvironmentVariable("TABLE_CONNECTION") ?? string.Empty;
        if (!string.IsNullOrEmpty(tableConn))
        {
            var service = new TableServiceClient(tableConn);
            var table = service.GetTableClient("Sponsors");
            await table.CreateIfNotExistsAsync();
            var entity = new TableEntity("Sponsor", sponsorGuid)
            {
                {"github_login", sponsorLogin},
                {"payload", body}
            };
            await table.AddEntityAsync(entity);
        }

        string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty;
        string repoName = Environment.GetEnvironmentVariable("GITHUB_REPO") ?? string.Empty;
        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(repoName))
        {
            var client = new GitHubClient(new ProductHeaderValue("credits-hub"))
            {
                Credentials = new Credentials(token)
            };
            var parts = repoName.Split('/');
            if (parts.Length == 2)
            {
                string owner = parts[0];
                string repo = parts[1];
                string path = $"sponsors/{sponsorGuid}.json";
                string content = JsonSerializer.Serialize(new { login = sponsorLogin, guid = sponsorGuid }, new JsonSerializerOptions { WriteIndented = true });
                var createRequest = new CreateFileRequest($"Add sponsor {sponsorLogin}", content, branch: "main");
                await client.Repository.Content.CreateFile(owner, repo, path, createRequest);
            }
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteStringAsync("OK");
        return ok;
    }
}
