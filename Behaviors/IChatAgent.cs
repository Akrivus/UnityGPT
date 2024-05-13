using System;
using System.Collections;

public interface IChatAgent
{
    public bool IsReady { get; }

    public IEnumerator RespondTo(string content, Action<string> callback);
}
