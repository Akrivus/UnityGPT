using Proyecto26;
using RSG;
using System;
using System.Collections.Generic;
using UnityEngine;

public class StreamingTextGenerator : TextGenerator, IStreamingTextGenerator
{
    public event Action<string> OnStreamReceived;
    public event Action<string> OnStreamEnded;

    protected string stream;
    protected List<ToolCallReference> toolCallReferences = new List<ToolCallReference>();

    public StreamingTextGenerator(LinkOpenAI client, List<Message> messages, string model, int maxTokens = 1024, float temperature = 0.5f, string interstitialPrompt = "{0}")
        : base(client, messages, model, maxTokens, temperature, interstitialPrompt) { }

    public StreamingTextGenerator(LinkOpenAI client, string prompt, string model, int maxTokens = 1024, float temperature = 0.5f, string interstitialPrompt = "{0}")
        : base(client, prompt, model, maxTokens, temperature, interstitialPrompt) { }

    public new ITextGenerator Fork(string prompt)
    {
        return new StreamingTextGenerator(client, prompt, Model, MaxTokens, Temperature, InterstitialPrompt);
    }

    public IPromise<string> RespondTo(string content, Action<string> tokenCallback, params string[] args)
    {
        content = InterstitialPrompt.Format(content, args);
        AddContext(content);
        return SendMessages(tokenCallback);
    }

    public IPromise<string> SendMessages(Action<string> tokenCallback)
    {
        var sse = new ServerSentEventHandler<GeneratedText<Choice.Chunk>>();
        sse.OnServerSentEvent += (e) => DispatchGeneratedTextChunk(e.Data)
            .Then(token => tokenCallback(token));

        toolCallReferences = new List<ToolCallReference>();
        stream = string.Empty;

        var body = new GenerateText(Model, MaxTokens, Temperature, messages, tools, true, ToolChoice);
        return client.Post(new RequestHelper
        {
            Uri = client.Uri_Chat,
            BodyString = RestClientExtensions.Serialize(body),
            DownloadHandler = sse
        }).Then(_ => DispatchGeneratedText(stream, tokenCallback));
    }

    public IPromise<string> DispatchGeneratedText(string content, Action<string> tokenCallback)
    {
        if (toolCallReferences.Count > 0)
            return ExecuteToolCalls(toolCallReferences.ToArray()).Then((_) => SendMessages(tokenCallback));
        AddMessage(content);
        OnStreamEnded?.Invoke(content);
        return Promise<string>.Resolved(content);
    }

    private IPromise<string> DispatchGeneratedTextChunk(GeneratedText<Choice.Chunk> text)
    {
        if (text.Content != null) stream += text.Content;
        if (text.ToolCall)
            foreach (var toolCall in text.ToolCalls)
                DispatchToolCallChunk(toolCall);
        OnStreamReceived?.Invoke(text.Content);
        return Promise<string>.Resolved(text.Content);
    }

    private void DispatchToolCallChunk(ToolCallReference toolCall)
    {
        if (toolCallReferences.Count == toolCall.Index)
            toolCallReferences.Add(toolCall);
        var function = toolCallReferences[toolCall.Index].Function;
        function.Arguments += toolCall.Function.Arguments;
    }
}