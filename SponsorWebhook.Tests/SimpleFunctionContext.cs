using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Extensions.DependencyInjection;

namespace SponsorWebhook.Tests;

internal class SimpleFunctionContext : FunctionContext
{
    private readonly IInvocationFeatures _features = new SimpleInvocationFeatures();

    public override string InvocationId { get; } = Guid.NewGuid().ToString();
    public override string FunctionId { get; } = Guid.NewGuid().ToString();
    public override TraceContext TraceContext { get; } = new SimpleTraceContext();
    public override BindingContext BindingContext { get; } = null!;
    public override RetryContext? RetryContext => null;
    public override IServiceProvider InstanceServices { get; set; } = null!;
    public override FunctionDefinition FunctionDefinition { get; } = null!;
    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
    public override IInvocationFeatures Features => _features;
    public override CancellationToken CancellationToken => CancellationToken.None;

    private class SimpleTraceContext : TraceContext
    {
        public override string TraceParent => string.Empty;
        public override string TraceState => string.Empty;
    }

    private class SimpleInvocationFeatures : IInvocationFeatures
    {
        private readonly Dictionary<Type, object> _storage = new();
        public void Set<T>(T instance) => _storage[typeof(T)] = instance!;
        public T? Get<T>() => _storage.TryGetValue(typeof(T), out var v) ? (T)v : default;
        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => _storage.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _storage.GetEnumerator();
    }
}
