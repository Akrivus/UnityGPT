using Newtonsoft.Json;
using System.Collections.Generic;
using static Message;

public class GenerateText
{
    public TextModel Model { get; set; }
    public int MaxTokens { get; set; }
    public float Temperature { get; set; }
    public List<Message> Messages { get; set; }
    public List<ToolReference> Tools { get; set; }
    public string ToolChoice { get; set; } = "auto";
    public bool Stream { get; set; } = false;

    public GenerateText(TextModel model, int maxtokens, float temperature, List<Message> messages, List<Tool> functions = null)
    {
        Model = model;
        MaxTokens = maxtokens;
        Temperature = temperature;
        Messages = messages;
        Tools = new List<ToolReference>();

        if (functions != null)
            foreach (var function in functions)
                Tools.Add(new ToolReference
                {
                    Function = function
                });
    }

    public bool ShouldSerializeTools()
    {
        return Tools.Count > 0;
    }

    public bool ShouldSerializeToolChoice()
    {
        return Tools.Count > 0 && ToolChoice != "auto";
    }
}

public class Message
{
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string Content { get; set; }
    public string Name { get; set; }
    public Roles Role { get; set; }
    public FunctionCall FunctionCall { get; set; }
    public List<ToolCallReference> ToolCalls { get; set; }
    public string ToolCallId { get; set; }

    public Message(string content, Roles role = Roles.User, string name = null, string toolCallId = null)
    {
        Content = content;
        Role = role;
        Name = name;
        ToolCallId = toolCallId;
    }

    public Message(string content, ToolCallReference tool)
    {
        Content = content;
        Role = Roles.Tool;
        Name = tool.Function.Name;
        ToolCallId = tool.Id;
    }

    public Message() { }

    public enum Roles
    {
        User, Assistant, System, Function, Tool
    }

    public enum FinishReasons
    {
        Stop, Length, FunctionCall, ToolCalls, ContentFilter, Null
    }
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
    public FunctionCall Function { get; set; }
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
    public List<T> Choices { get; set; }
    public Usage Usage { get; set; }

    public T Choice => Choices[0];
    public string Content => Choice.Content;
    public FinishReasons FinishReason => Choice.FinishReason;
    public List<ToolCallReference> ToolCalls => Choice.Message.ToolCalls;
    public bool ToolCall => FinishReason == FinishReasons.ToolCalls;
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
        public bool ShouldSerializeMessage() => false;
        public Message Delta { get; set; }
        public override Message Message => Delta;
    }
}

public enum TextModel
{
    [JsonProperty("gpt-3.5-turbo")]
    GPT35_Turbo,
    [JsonProperty("gpt-4")]
    GPT4,
    [JsonProperty("gpt-4-turbo")]
    GPT4_Turbo
}