using RSG;
using System;

public class QuickTool : IToolCall
{
    public Type ArgType => typeof(Args);
    public Tool Tool => _tool;

    private Func<Args, IPromise<string>> _promise;
    private Tool _tool;

    public QuickTool(Func<Args, IPromise<string>> promise, string name, string description, params ToolParam[] parameters)
    {
        _promise = promise;
        _tool = new Tool(name, description, parameters);
    }

    public QuickTool(Action<Args> promise, string name, string description, params ToolParam[] parameters)
        : this((args) => Box(promise, args), name, description, parameters) { }

    public QuickTool(Func<Args, string> promise, string name, string description, params ToolParam[] parameters)
        : this((args) => Resolve(promise, args), name, description, parameters) { }

    public IPromise<string> Execute(object @object)
    {
        return _promise((Args)@object);
    }

    private static IPromise<string> Box(Action<Args> promise, Args args, string response = "OK")
    {
        promise(args);
        return Promise<string>.Resolved(response);
    }

    private static IPromise<string> Resolve(Func<Args, string> promise, Args args)
    {
        return Promise<string>.Resolved(promise(args));
    }

    public class Args
    {
        public string Value { get; set; }
        public string Query { get; set; }

        public int? Limit { get; set; }

        public float? Threshhold { get; set; }

        public float Score { get; set; }
    }
}