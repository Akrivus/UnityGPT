using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TextGenerator : IEmbedding, IText, IToolCaller
{
    public string Prompt { get; set; } = "You are a helpful assistant that helps people with their computer problems.";
    public TextModel Model { get; set; } = TextModel.GPT35_Turbo;
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 0.5F;

    bool hasSystemPrompt = true;

    List<Message> messages = new List<Message>();
    Dictionary<string, IToolCall> Tools = new Dictionary<string, IToolCall>();

    public event EventHandler<TextEventArgs> TextComplete;
    public event EventHandler<TextEventArgs> TextUpdate;
    public event EventHandler<MessagesClearedEventArgs> MessagesCleared;

    List<Tool> tools => Tools.Select((kp) => kp.Value.Tool).ToList();

    public TextGenerator(string prompt, TextModel model, int maxTokens, float temperature, bool hasSystemPrompt = true)
    {
        Prompt = prompt;
        Model = model;
        MaxTokens = maxTokens;
        Temperature = temperature;
        this.hasSystemPrompt = hasSystemPrompt;
    }

    public async Task<string> GenerateTextAsync(string content)
    {
        if (hasSystemPrompt && messages.Count == 0)
            messages.Add(new Message(Prompt, Message.Roles.System));
        messages.Add(new Message(content, Message.Roles.User));
        return await GenerateTextAsync(messages);
    }

    public IEnumerator GenerateText(string content)
    {
        if (hasSystemPrompt && messages.Count == 0)
            messages.Add(new Message(Prompt, Message.Roles.System));
        messages.Add(new Message(content, Message.Roles.User));
        yield return GenerateText(messages);
    }

    public async Task<string> GenerateTextAsync(List<Message> messages)
    {
        var req = new GenerateText(Model, MaxTokens, Temperature, messages, tools);
        var res = await ChatAgent.API.PostAsync<GeneratedText<Choice>>("chat/completions", req);
        if (res.ToolCall)
            await CallTools(res.ToolCalls);
        return res.Content;
    }

    public IEnumerator GenerateText(List<Message> messages)
    {
        var req = new GenerateText(Model, MaxTokens, Temperature, messages, tools);
        req.Stream = true;
        yield return ChatAgent.API.PostForSSE<GeneratedText<Choice.Chunk>>("chat/completions", req,
            (chunk) => ProcessChunk(chunk),
            (chunks) => ProcessChunks(chunks));
    }

    void ProcessChunk(GeneratedText<Choice.Chunk> chunk)
    {
        if (chunk.ToolCall) CallTools(chunk.ToolCalls).Wait();
        TextUpdate?.Invoke(this, new TextEventArgs(chunk.Choice.Content));
    }

    void ProcessChunks(List<GeneratedText<Choice.Chunk>> chunks)
    {
        var choices = chunks.Select((chunk) => chunk.Choice).Where((choice) => choice.Content != null).ToList();
        var content = string.Join("", chunks.Select((chunk) => chunk.Content));
        var message = new Message(content, Message.Roles.System);
        messages.Add(message);
        TextComplete?.Invoke(this, new TextEventArgs(message.Content));
    }

    public void ClearMessages()
    {
        MessagesCleared?.Invoke(this, new MessagesClearedEventArgs(messages.ToArray()));
        messages.Clear();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var req = new GenerateEmbedding(text);
        var res = await ChatAgent.API.PostAsync<GeneratedEmbedding>("embeddings", req);
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

    public void RemoveTool(string name)
    {
        Tools.Remove(name);
    }

    public void RemoveTools(params string[] names)
    {
        foreach (var name in names)
            Tools.Remove(name);
    }
}
