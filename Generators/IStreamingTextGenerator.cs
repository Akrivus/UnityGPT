using RSG;
using System;

public interface IStreamingTextGenerator : ITextGenerator
{
    public IPromise<string> RespondTo(string prompt, Action<string> tokenCallback);

    public event Action<string> OnStreamReceived;
    public event Action<string> OnStreamEnded;
}
