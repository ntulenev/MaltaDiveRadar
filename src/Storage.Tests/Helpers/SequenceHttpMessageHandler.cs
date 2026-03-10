using System.Net;
using System.Net.Http;

namespace Storage.Tests.Helpers;

public sealed class SequenceHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;

    public SequenceHttpMessageHandler(
        IEnumerable<Func<HttpRequestMessage, HttpResponseMessage>> responses)
    {
        _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(
            responses);
    }

    public int SendCalls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        SendCalls++;

        if (_responses.Count == 0)
        {
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Empty),
                });
        }

        var factory = _responses.Dequeue();
        return Task.FromResult(factory(request));
    }
}

