using System;
using System.Diagnostics;
using UnityEngine;

public class WaitFor : CustomYieldInstruction
{
    private Func<bool> condition;
    private int timeout;

    private Stopwatch stopwatch = new Stopwatch();

    public override bool keepWaiting => stopwatch.ElapsedMilliseconds < timeout && !condition();

    public WaitFor(Func<bool> condition, int timeout = 5000)
    {
        this.condition = condition;
        this.timeout = timeout;

        stopwatch.Start();
    }
}