using Newtonsoft.Json;
using System.Collections.Generic;

public class GenerateText
{
    public string Model { get; set; }
    public int MaxTokens { get; set; }
    public float Temperature { get; set; }
    public List<Message> Messages { get; set; }
    public List<ToolReference> Tools { get; set; }
    public ToolCallReference ToolChoice { get; set; }
    public bool Stream { get; set; } = false;

    public GenerateText(string model, int maxtokens, float temperature, List<Message> messages, List<Tool> tools = null, bool stream = false, string toolChoice = null)
    {
        Model = model;
        MaxTokens = maxtokens;
        Temperature = temperature;
        Messages = messages;
        Tools = new List<ToolReference>();
        Stream = stream;

        if (toolChoice != null)
            ToolChoice = new ToolCallReference(toolChoice);
        if (tools != null)
            foreach (var tool in tools)
                Tools.Add(new ToolReference
                {
                    Function = tool
                });
    }

    public bool ShouldSerializeTools() => Tools.Count > 0;

    public bool ShouldSerializeToolChoice() => ToolChoice != null;
}

public class Message
{
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string Content { get; set; }
    public Roles Role { get; set; }
    public ToolCallReference[] ToolCalls { get; set; }
    public string ToolCallId { get; set; }

    public Message(string content, Roles role = Roles.User)
    {
        Content = content;
        Role = role;
    }

    public Message(ToolCallReference[] toolCalls)
        : this(null, Roles.Assistant)
    {
        ToolCalls = toolCalls;
    }

    public Message(string content, string toolCallId)
        : this(content, Roles.Tool)
    {
        ToolCallId = toolCallId;
    }

    public Message() { }
}

public class ToolReference
{
    public string Type { get; set; } = "function";
    public Tool Function { get; set; }
}

public class ToolCallReference
{
    public string Type { get; set; } = "function";
    public string Id { get; set; }
    public int Index { get; set; }
    public FunctionCall Function { get; set; }

    public ToolCallReference() { }

    public ToolCallReference(string name)
    {
        Function = new FunctionCall
        {
            Name = name
        };
    }

    public bool ShouldSerializeId() => Id != null;
}

public class FunctionCall
{
    public string Name { get; set; }
    public string Arguments { get; set; }
}

public class GeneratedText<T> where T : Choice
{
    public string Id { get; set; }
    public string Object { get; set; }
    public int Created { get; set; }
    public string Model { get; set; }
    public T[] Choices { get; set; }
    public Usage Usage { get; set; }

    public T Choice => Choices[0];
    public string Content => Choice.Content;
    public FinishReasons FinishReason => Choice.FinishReason;
    public ToolCallReference[] ToolCalls => Choice.Message.ToolCalls;
    public bool ToolCall => ToolCalls != null && ToolCalls.Length > 0;
}

public class Choice
{
    public int Index { get; set; }
    public virtual Message Message { get; set; }
    public FinishReasons FinishReason { get; set; }

    [JsonIgnore]
    public string Content => Message.Content;

    public class Chunk : Choice
    {
        public Message Delta { get; set; }
        public override Message Message => Delta;

        public bool ShouldSerializeMessage() => false;
    }
}

public enum Roles
{
    User, Assistant, System, Function, Tool
}

public enum FinishReasons
{
    Stop, Length, FunctionCall, ToolCalls, ContentFilter, Null
}