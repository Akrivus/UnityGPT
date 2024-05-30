using RSG;
using System;
using static OpinionMiner;

public class OpinionMiner : BaseToolCaller<Opinion, float>
{
    public OpinionMiner(PhrenProxyClient client, string prompt = "")
        : base(client, new Opinion(), "Report your opinion of the following text.", prompt) { }

    public IPromise<OpinionMiner> Digest(string question)
    {
        return AI.RespondTo(question).Then((response) => this);
    }

    public override IPromise<float> Call(string text, string context = "")
    {
        if (!string.IsNullOrEmpty(context))
            text += $"\n\nContext: {context}";
        return Execute(text).Then((message) => Opinion.Score);
    }

    public class Opinion : IToolCall
    {
        public class Args
        {
            public float Score { get; set; }
        }

        public static float Score { get; private set; }

        public Type ArgType => typeof(Args);
        public Tool Tool => new Tool("Opinion",
            "Report opinion.",
            new NumberParam("Score", "Sentiment score.", true, -1, 1));

        public string Execute(object args) => Execute((Args) args);

        public string Execute(Args args)
        {
            Score = args.Score;
            return Score.ToString();
        }
    }
}