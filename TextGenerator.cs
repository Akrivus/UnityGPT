﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class TextGenerator : IEmbedding, IText, IToolCaller
{
    string prompt = "You are a helpful assistant inside of a Unity scene.";
    string model = "gpt-3.5-turbo";
    int maxTokens = 1024;
    float temperature = 0.5F;

    List<Message> messages = new List<Message>();
    List<Tool> tools = new List<Tool>();

    Dictionary<string, IToolCall> Tools = new Dictionary<string, IToolCall>();

    public event EventHandler<Choice.Chunk> NextToken;
    public event EventHandler<ToolCallReference> ToolCall;
    public event EventHandler<string> TextCompleted;

    public string Prompt => prompt;

    public TextGenerator(string prompt, string model, int maxTokens, float temperature)
    {
        this.prompt = prompt;
        this.model = model;
        this.maxTokens = maxTokens;
        this.temperature = temperature;
    }

    public async Task<string> GenerateTextAsync(string content)
    {
        IntroduceSystemPrompt(content, out var message);
        var req = new GenerateText(model, maxTokens, temperature, messages);
        var res = await ChatGenerator.API.PostAsync<GeneratedText<Choice>>("chat/completions", req);
        if (res.ToolCall)
            await InvokeToolCallsAsync(res.ToolCalls);
        return res.Content;
    }

    public IEnumerator GenerateText(string content)
    {
        IntroduceSystemPrompt(content, out var message);
        var req = new GenerateText(model, maxTokens, temperature, messages);
        req.Stream = true;
        yield return ChatGenerator.API.PostForSSE<GeneratedText<Choice.Chunk>>("chat/completions", req,
            (chunk) => NextToken?.Invoke(this, chunk.Choice));
        TextCompleted?.Invoke(this, message.Content);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var req = new GenerateEmbedding(text);
        var res = await ChatGenerator.API.PostAsync<GeneratedEmbedding>("embeddings", req);
        return res.Embedding;
    }

    public void InvokeToolCall(ToolCallReference tool, IToolCall toolCall)
    {
        var arg = toolCall.EntryPoint.Invoke(toolCall, new object[] { QuickJSON.Deserialize(tool.Function.Arguments, toolCall.ArgType) });
        var content = QuickJSON.Serialize(arg);
        messages.Add(new Message(content, tool));
    }

    public async Task<string> InvokeToolCallsAsync(List<ToolCallReference> tools)
    {
        foreach (var tool in tools)
            InvokeToolCall(tool, Tools[tool.Function.Name]);
        var req = new GenerateText(model, maxTokens, temperature, messages);
        var res = await ChatGenerator.API.PostAsync<GeneratedText<Choice>>("chat/completions", req);
        return res.Content;
    }

    public IEnumerator InvokeToolCalls(List<ToolCallReference> tools)
    {
        foreach (var tool in tools)
            InvokeToolCall(tool, Tools[tool.Function.Name]);
        var req = new GenerateText(model, maxTokens, temperature, messages);
        req.Stream = true;
        yield return ChatGenerator.API.PostForSSE<GeneratedText<Choice.Chunk>>("chat/completions", req,
            (chunk) => NextToken?.Invoke(this, chunk.Choice));
    }

    void IntroduceSystemPrompt(string content, out Message message, Message.Roles role = Message.Roles.User)
    {
        if (messages.Count == 0)
            messages.Add(new Message(prompt, Message.Roles.System));
        messages.Add(message = new Message(content, role));
    }
}
