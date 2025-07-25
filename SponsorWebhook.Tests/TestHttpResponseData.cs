using System;
using System.IO;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SponsorWebhook.Tests;

public class TestHttpResponseData : HttpResponseData
{
    private readonly MemoryStream _body = new MemoryStream();

    public TestHttpResponseData(FunctionContext context) : base(context)
    {
        Headers = new HttpHeadersCollection();
        Cookies = (HttpCookies?)Activator.CreateInstance(typeof(HttpCookies), true)!;
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; }
    public override Stream Body { get => _body; set => _body.SetLength(0); }
    public override HttpCookies Cookies { get; }
}
