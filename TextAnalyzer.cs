using RSG;
using System;

public class TextAnalyzer
{
    private static readonly string Prompt = "Report the sentiment of the following text.\nText:";

    private Sentiment sentiment = new Sentiment();
    private TextGenerator generator;

    public TextAnalyzer(PhrenProxyClient client)
    {
        generator = new TextGenerator(client, Prompt, "gpt-3.5-turbo", 256, 0.1f);
        generator.AddTool(sentiment);
    }

    public IPromise<float> Analyze(string text)
    {
        generator.ResetContext();
        return generator.Execute<Sentiment>(text).Then((message) => Sentiment.Score);
    }

    public class Sentiment : IToolCall
    {
        public class Args
        {
            public float Score { get; set; }
        }

        public static float Score { get; private set; }

        public Type ArgType => typeof(Args);
        public Tool Tool => new Tool("Sentiment",
            "Reports sentiment score.",
            new NumberParam("Score", "Sentiment score.", true, -1, 1));

        public string Execute(object args) => Execute((Args) args);

        public string Execute(Args args)
        {
            Score = args.Score;
            return Score.ToString();
        }
    }
}