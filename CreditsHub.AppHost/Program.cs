var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureFunctionsProject<Projects.SponsorWebhook>("webhook")
       .WithExternalHttpEndpoints();

builder.Build().Run();
