using System;
using System.Collections;
using UnityEngine;

public abstract class ToolCall<T> : MonoBehaviour, IToolCall
{
    public Type ArgType => typeof(T);
    public virtual Tool Tool => new Tool(ToolName, Description, Params);
    public string Execute(object args) => CallSafe((T) args);

    public bool Pending => !_complete;
    public bool Complete => _complete;

    private bool _complete = true;

    [SerializeField]
    public string ToolName;
    [SerializeField, TextArea(2, 5)]
    public string Description;
    [SerializeField]
    public ToolParam[] Params;

    public abstract string OnCall(T args);

    public string CallSafe(T args)
    {
        _complete = true;
        return OnCall(args);
    }

    public void ContinueCallWith(IEnumerator coroutine)
    {
        _complete = false;
        StartCoroutine(StartCallCoroutine(coroutine));
    }

    private IEnumerator StartCallCoroutine(IEnumerator coroutine)
    {
        yield return coroutine;
        _complete = true;
    }
}