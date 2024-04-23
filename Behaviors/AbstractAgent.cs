using System;
using System.Collections;
using UnityEngine;

public abstract class AbstractAgent : MonoBehaviour, IChatBehavior
{
    public virtual bool IsReady { get; set; }

    public abstract IEnumerator RespondTo(string message, Action<string> callback);
}
