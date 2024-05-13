using Proyecto26;
using RSG;
using System;
using System.Collections.Generic;

public class StreamingTextGenerator : TextGenerator, IStreamingTextGenerator
{
    public event Action<string> OnStreamReceived;
    public event Action<string> OnStreamEnded;

    protected string stream;

    public StreamingTextGenerator(PhrenProxyClient client, List<Message> messages, string model, int maxTokens = 1024, float temperature = 0.5f, string interstitialPrompt = "{0}")
        : base(client, messages, model, maxTokens, temperature, interstitialPrompt) { }

    public IPromise<string> RespondTo(string content, Action<string> tokenCallback)
    {
        AddContext(content);
        return SendMessages(tokenCallback);
    }

    public IPromise<string> SendMessages(Action<string> tokenCallback)
    {
        var sse = new ServerSentEventHandler<GeneratedText<Choice.Chunk>>();
        sse.OnServerSentEvent += (e) => DispatchGeneratedTextChunk(e.Data)
            .Then(token => tokenCallback(token));

        stream = string.Empty;

        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, true, ToolChoice);
        return api.Post(new RequestHelper
        {
            Uri = api.Uri_Chat,
            BodyString = RestClientExtensions.Serialize(body),
            DownloadHandler = sse
        }).Then(_ => DispatchGeneratedText(stream));
    }

    private IPromise<string> DispatchGeneratedTextChunk(GeneratedText<Choice.Chunk> text)
    {
        if (text.Content != null) stream += text.Content;
        OnStreamReceived?.Invoke(text.Content);
        return ExecuteToolCalls(text.ToolCalls, text.Content);
    }

    private string DispatchGeneratedText(string message)
    {
        AddMessage(message);
        OnStreamEnded?.Invoke(message);
        return message;
    }
}