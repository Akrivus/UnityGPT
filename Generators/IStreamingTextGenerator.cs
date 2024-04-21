using RSG;
using System;

public interface IStreamingTextGenerator
{
    public IPromise<string> RespondTo(string prompt, Action<string> tokenCallback);
}
