using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Collections;
using System.Net.Http.Headers;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class RestClient
{
    HttpClient _client;

    public RestClient(string baseUri, string token, string authHeader = "Authorization", string tokenType = "Bearer ")
    {
        if (!baseUri.EndsWith("/")) baseUri += "/";
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add(authHeader, $"{tokenType}{token}");
        _client.BaseAddress = new Uri(baseUri);
    }

    public async Task<HttpResponseMessage> CleanSendAsync(HttpMethod method, string uri, string @string)
    {
        var req = new HttpRequestMessage(method, uri.TrimStart('/'));
        if (@string != null)
            req.Content = new StringContent(@string, Encoding.UTF8, "application/json");
        var res = await _client.SendAsync(req);
        return res;
    }

    public async Task<bool> SendAsync(HttpMethod method, string uri)
    {
        var res = await CleanSendAsync(method, uri, null);
        return res.IsSuccessStatusCode;
    }

    public async Task<O> SendJsonAsync<I, O>(HttpMethod method, string uri, I @object)
    {
        try
        {
            string @string = null;
            if (@object != null)
                @string = QuickJSON.Serialize(@object);
            var res = await CleanSendAsync(method, uri, @string);
            var json = await res.Content.ReadAsStringAsync();
            return QuickJSON.Deserialize<O>(json);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return default;
        }
    }

    public async Task<O> SendMultiPartAsync<O>(HttpMethod method, string uri, MultipartFormDataContent content)
    {
        var req = new HttpRequestMessage(method, uri.TrimStart('/'));
        req.Content = content;
        var res = await _client.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();
        return QuickJSON.Deserialize<O>(json);
    }

    public async Task<O> GetAsync<O>(string uri)
    {
        return await SendJsonAsync<object, O>(HttpMethod.Get, uri, null);
    }

    public async Task<O> PostAsync<O>(string uri, object @object)
    {
        return await SendJsonAsync<object, O>(HttpMethod.Post, uri, @object);
    }

    public IEnumerator ListenForSSE<O>(HttpMethod method, string uri, string @string, Action<O> onDataCallback, Action<List<O>> onDoneCallback)
    {
        var req = new HttpRequestMessage(method, uri.TrimStart('/'));
        if (@string != null)
            req.Content = new StringContent(@string, Encoding.UTF8, "application/json");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var source = new CancellationTokenSource();
        Task task;
        task = _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, source.Token);
        yield return new WaitUntil(() => task.IsCompleted);

        var response = (task as Task<HttpResponseMessage>).Result;
        task = response.Content.ReadAsStreamAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        var stream = (task as Task<Stream>).Result;
        using var reader = new StreamReader(stream);
        var data = new List<O>();
        while (!reader.EndOfStream)
        {
            task = reader.ReadLineAsync();
            yield return new WaitUntil(() => task.IsCompleted);
            var buffer = (task as Task<string>).Result;
            var datagrams = buffer.Split("data: ");
            foreach (var datagram in datagrams)
                if (datagram.StartsWith("{"))
                {
                    O @object = QuickJSON.Deserialize<O>(datagram);
                    data.Add(@object);
                    onDataCallback(@object);
                }
        }
        onDoneCallback(data);
    }

    public IEnumerator PostForSSE<O>(string uri, object @object, Action<O> onDataCallback, Action<List<O>> onDoneCallback)
    {
        string @string = null;
        if (@object != null)
            @string = QuickJSON.Serialize(@object);
        yield return ListenForSSE(HttpMethod.Post, uri, @string, onDataCallback, onDoneCallback);
    }
}