# Donejtor Credits Hub

This sample shows how to capture GitHub Sponsors webhooks using an Azure Function
written in C#. The function stores the data in Azure Table Storage. For traceability, a GUID is assigned
to each sponsor event and a JSON file is committed to a separate GitHub
repository. The GUID in Table Storage links the stored payload to the file in
the repository so that usernames can be removed later if required.

## Function overview

The function `SponsorWebhook` exposes an HTTP endpoint which GitHub can call.
It verifies the HMAC signature from the webhook, stores the payload in an Azure
Table, and then commits a small JSON file to another repository with the
sponsor's login and the generated GUID.

### Environment variables

- `GITHUB_WEBHOOK_SECRET` &ndash; secret configured in the GitHub webhook.
- `TABLE_CONNECTION` &ndash; connection string for Azure Table Storage.
- `GITHUB_TOKEN` &ndash; personal access token used to commit to the repo.
- `GITHUB_REPO` &ndash; repository in the form `owner/name` where JSON files
  will be committed.

## Deployment

1. Create a storage account and enable Table Storage.
2. Deploy the function on the consumption plan (the free tier keeps costs low).
3. Set the environment variables listed above in the function configuration.
4. Configure a GitHub Sponsors webhook to send events to the function URL.

Run `dotnet build` in the `SponsorWebhook` folder to restore NuGet packages and
compile the function locally.

### Debugging locally with .NET Aspire

The repository includes a small Aspire host project that runs the function with
its dependencies. Install the Aspire workload and launch the host with:

```bash
dotnet run --project CreditsHub.AppHost
```

The host starts the `SponsorWebhook` function and exposes an HTTP endpoint for
testing webhooks.
