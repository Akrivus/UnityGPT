using RSG;
using System;
using System.Reflection;

public class TextAnalyzer
{
    static readonly string Prompt = "Report the sentiment of the following text.\nText:";

    Sentiment sentiment = new Sentiment();
    ToolCallingAgent agent;

    public TextAnalyzer()
    {
        agent = new ToolCallingAgent(Prompt,
            TextModel.GPT35_Turbo, 256, 0.1f, sentiment);
    }

    public IPromise<float> Analyze(string text)
    {
        sentiment.Reset();
        agent.Execute("Sentiment", text).Then(_ => agent.ResetContext());
        return sentiment.Run;
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

        public Promise<float> Run;

        public Sentiment()
        {
            Reset();
        }

        public string Predict(Args args)
        {
            Run.Resolve(args.Score);
            return "{\"success\": \"true\"}";
        }

        public void Reset()
        {
            Run = new Promise<float>();
        }
    }
}