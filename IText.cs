using System;
using System.Collections;
using System.Threading.Tasks;

public interface IText
{
    public event EventHandler<TextEvent> TextComplete;

    public Task<string> GenerateTextAsync(string content);
    public IEnumerator GenerateText(string content);
    public void ClearMessages();
}

public class TextEvent
{
    public string Message { get; private set; }

    public TextEvent(string message)
    {
        Message = message;
    }
}