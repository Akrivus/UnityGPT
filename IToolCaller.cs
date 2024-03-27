using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IToolCaller
{
    public void InvokeToolCall(ToolCallReference reference, IToolCall tool);
    public Task<string> InvokeToolCallsAsync(List<ToolCallReference> tools);
    public IEnumerator InvokeToolCalls(List<ToolCallReference> tools);
}
