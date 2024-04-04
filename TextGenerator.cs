using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TextGenerator : IEmbedding, IText, IToolCaller
{
    string prompt = "You are a helpful assistant inside of a Unity scene.";
    string model = "gpt-3.5-turbo";
    int maxTokens = 1024;
    float temperature = 0.5F;
    bool hasSystemPrompt = true;

    List<Message> messages = new List<Message>();
    Dictionary<string, IToolCall> Tools = new Dictionary<string, IToolCall>();

    public event EventHandler<TextEvent> TextComplete;
    public event EventHandler<TextEvent> TextUpdate;

    public string Prompt => prompt;

    List<Tool> tools => Tools.Select((kp) => kp.Value.Tool).ToList();

    public TextGenerator(string prompt, string model, int maxTokens, float temperature, bool hasSystemPrompt = true)
    {
        this.prompt = prompt;
        this.model = model;
        this.maxTokens = maxTokens;
        this.temperature = temperature;
        this.hasSystemPrompt = hasSystemPrompt;
    }

    public async Task<string> GenerateTextAsync(string content)
    {
        if (hasSystemPrompt && messages.Count == 0)
            messages.Add(new Message(prompt, Message.Roles.System));
        messages.Add(new Message(content, Message.Roles.User));
        return await GenerateTextAsync(messages);
    }

    public IEnumerator GenerateText(string content)
    {
        if (hasSystemPrompt && messages.Count == 0)
            messages.Add(new Message(prompt, Message.Roles.System));
        messages.Add(new Message(content, Message.Roles.User));
        yield return GenerateText(messages);
    }

    public async Task<string> GenerateTextAsync(List<Message> messages)
    {
        var req = new GenerateText(model, maxTokens, temperature, messages, tools);
        var res = await ChatGenerator.API.PostAsync<GeneratedText<Choice>>("chat/completions", req);
        if (res.ToolCall)
            await CallTools(res.ToolCalls);
        return res.Content;
    }

    public IEnumerator GenerateText(List<Message> messages)
    {
        var req = new GenerateText(model, maxTokens, temperature, messages, tools);
        req.Stream = true;
        yield return ChatGenerator.API.PostForSSE<GeneratedText<Choice.Chunk>>("chat/completions", req,
            (chunk) => ProcessChunk(chunk),
            (chunks) => ProcessChunks(chunks));
    }

    void ProcessChunk(GeneratedText<Choice.Chunk> chunk)
    {
        if (chunk.ToolCall) CallTools(chunk.ToolCalls).Wait();
        TextUpdate?.Invoke(this, new TextEvent(chunk.Choice.Content));
    }

    void ProcessChunks(List<GeneratedText<Choice.Chunk>> chunks)
    {
        var choices = chunks.Select((chunk) => chunk.Choice).Where((choice) => choice.Content != null).ToList();
        var content = string.Join("", chunks.Select((chunk) => chunk.Content));
        var message = new Message(content, Message.Roles.System);
        messages.Add(message);
        TextComplete?.Invoke(this, new TextEvent(message.Content));
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var req = new GenerateEmbedding(text);
        var res = await ChatGenerator.API.PostAsync<GeneratedEmbedding>("embeddings", req);
        return res.Embedding;
    }

    public void CallTool(ToolCallReference tool, IToolCall toolCall)
    {
        var arg = toolCall.EntryPoint.Invoke(toolCall, new object[] { QuickJSON.Deserialize(tool.Function.Arguments, toolCall.ArgType) });
        var content = QuickJSON.Serialize(arg);
        messages.Add(new Message(content, tool));
    }

    async Task<string> CallTools(List<ToolCallReference> tools)
    {
        foreach (var tool in tools)
            CallTool(tool, Tools[tool.Function.Name]);
        return await GenerateTextAsync(messages);
    }

    public void AddTool(string name, IToolCall tool)
    {
        Tools.Add(name, tool);
    }

    public void AddTools(params IToolCall[] tools)
    {
        foreach (var tool in tools)
            Tools.Add(tool.Tool.Name, tool);
    }

    public void ClearMessages()
    {
        messages.Clear();
    }
}
