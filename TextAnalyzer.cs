using RSG;
using System;
using System.Reflection;

public class TextAnalyzer
{
    Sentiment sentiment = new Sentiment();
    TextAgent agent;

    public TextAnalyzer()
    {
        agent = new TextAgent(TextModel.GPT35_Turbo, 256, 0, sentiment);
        agent.Prompt = "Report the sentiment of the following text.\nText:";
    }

    public IPromise<float> Analyze(string text)
    {
        sentiment.Reset();
        agent.Execute("Sentiment", text);
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