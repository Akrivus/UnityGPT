using Proyecto26;
using RSG;
using System;

using static Message;

public class StreamingTextGenerator : TextGenerator, IStreamingTextGenerator
{
    public event Action<GeneratedText<Choice.Chunk>> OnGeneratedTextStreamReceived;
    public event Action<string> OnGeneratedTextStreamEnded;

    protected string stream;

    public StreamingTextGenerator(string prompt, TextModel model, int maxTokens = 1024, float temperature = 0.5f) : base(prompt, model, maxTokens, temperature) { }

    public IPromise<string> RespondTo(string content, Action<string> tokenCallback)
    {
        AddContext(content);
        return SendMessages(tokenCallback);
    }

    public IPromise<string> SendMessages(Action<string> tokenCallback)
    {
        var sse = new ServerSentEventHandler<GeneratedText<Choice.Chunk>>();
        sse.OnServerSentEvent += (e) => DispatchGeneratedTextChunk(e.Data).Then(token => tokenCallback(token));

        stream = string.Empty;

        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, true, ToolChoice);
        return RestClient.Post(new RequestHelper
        {
            Uri = URI,
            BodyString = RestClientExtensions.Serialize(body),
            DownloadHandler = sse
        }).Then(_ => DispatchGeneratedText(stream));
    }

    private IPromise<string> DispatchGeneratedTextChunk(GeneratedText<Choice.Chunk> text)
    {
        if (text.Content != null) stream += text.Content;
        OnGeneratedTextStreamReceived?.Invoke(text);
        return DispatchForToolCalls(text).Then(result => result);
    }

    private string DispatchGeneratedText(string message)
    {
        messages.Add(new Message(message, Roles.Assistant));
        OnGeneratedTextStreamEnded?.Invoke(message);
        return message;
    }
}