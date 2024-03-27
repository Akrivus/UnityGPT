using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public abstract class ToolCall<T> : MonoBehaviour, IToolCall
{
    public MethodInfo EntryPoint => GetType().GetMethod("CallSafe");
    public Type ArgType => typeof(T);
    public virtual Tool Tool => new Tool(ArgType);

    public bool Pending => !_complete;
    public bool Complete => _complete;

    bool _complete = true;

    public abstract string Prompt { get; }

    public abstract string OnCall(IToolCaller caller, T args);

    public string CallSafe(IToolCaller caller, T args)
    {
        _complete = true;
        return OnCall(caller, args);
    }

    public void ContinueCallWith(IEnumerator coroutine)
    {
        _complete = false;
        StartCoroutine(StartCallCoroutine(coroutine));
    }

    IEnumerator StartCallCoroutine(IEnumerator coroutine)
    {
        yield return coroutine;
        _complete = true;
    }
}