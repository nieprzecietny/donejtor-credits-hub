using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SponsorWebhook.Tests;

public class TestHttpRequestData : HttpRequestData
{
    private readonly MemoryStream _body;
    private readonly HttpHeadersCollection _headers;

    public TestHttpRequestData(FunctionContext context, string body, string method = "POST", Uri? url = null)
        : base(context)
    {
        _body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        _headers = new HttpHeadersCollection();
        Method = method;
        Url = url ?? new Uri("https://example.com");
    }

    public override Stream Body => _body;
    public override HttpHeadersCollection Headers => _headers;
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = Array.Empty<IHttpCookie>();
    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
    public override string Method { get; }
    public override Uri Url { get; }
    public override NameValueCollection Query { get; } = new NameValueCollection();

    public override HttpResponseData CreateResponse()
    {
        return new TestHttpResponseData(FunctionContext);
    }
}
