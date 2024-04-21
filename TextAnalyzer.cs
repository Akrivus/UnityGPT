using RSG;
using System;
using System.Reflection;

public class TextAnalyzer
{
    static readonly string Prompt = "Report the sentiment of the following text.\nText:";

    Sentiment sentiment = new Sentiment();
    ToolCaller agent;

    public TextAnalyzer()
    {
        agent = new ToolCaller(Prompt,
            TextModel.GPT_3p5_Turbo, 256, 0.1f, sentiment);
    }

    public IPromise<float> Analyze(string text)
    {
        return agent.Execute("Sentiment", text).Then(_ => agent.ResetContext()).Then(() => Promise<float>.Resolved(Sentiment.Score));
    }

    class Sentiment : IToolCall
    {
        [Tool("Sentiment", "Reports sentiment score.")]
        public class Args
        {
            [NumberParam("Score", "Sentiment score.", true, -1, 1)]
            public float Score { get; set; }
        }

        public Type ArgType => typeof(Args);
        public Tool Tool => new Tool(ArgType);
        public MethodInfo EntryPoint => typeof(Sentiment).GetMethod("Predict");

        public static float Score { get; private set; }

        public string Predict(Args args)
        {
            Score = args.Score;
            return "{\"success\": \"true\"}";
        }
    }
}