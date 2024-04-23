using System;
using System.Collections;

public interface IChatBehavior
{
    public bool IsReady { get; }

    public IEnumerator RespondTo(string content, Action<string> callback);
}
