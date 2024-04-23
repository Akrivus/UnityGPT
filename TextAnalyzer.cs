using RSG;
using System;
using System.Reflection;
using UnityEngine;

public class TextAnalyzer
{
    static readonly string Prompt = "Report the sentiment of the following text.\nText:";

    Sentiment sentiment = new Sentiment();
    TextGenerator generator;

    public TextAnalyzer()
    {
        generator = new TextGenerator(Prompt,
            TextModel.GPT_3_Turbo, 256, 0.1f);
        generator.AddTool(sentiment);
    }

    public IPromise<float> Analyze(string text)
    {
        generator.ResetContext();
        return generator.Execute<Sentiment>(text).Then((message) =>
        {
            Debug.Log(message);
            return Promise<float>.Resolved(Sentiment.Score);
        });
    }

    public class Sentiment : IToolCall
    {
        public class Args
        {
            public float Score { get; set; }
        }

        public Type ArgType => typeof(Args);
        public Tool Tool => new Tool("Sentiment",
            "Reports sentiment score.",
            new NumberParam("Score", "Sentiment score.", true, -1, 1));
        public string Execute(object args) => Execute((Args) args);

        public static float Score { get; private set; }

        public string Execute(Args args)
        {
            Score = args.Score;
            return Score.ToString();
        }
    }
}