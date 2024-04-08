using System;
using System.Collections;
using System.Threading.Tasks;

public interface IText
{
    public event EventHandler<TextEventArgs> TextComplete;

    public Task<string> GenerateTextAsync(string content);
    public IEnumerator GenerateText(string content);
    public void ClearMessages();
}

public class TextEventArgs
{
    public string Message { get; private set; }

    public TextEventArgs(string message)
    {
        Message = message;
    }
}

public class MessagesClearedEventArgs
{
    public Message[] Messages { get; private set; }

    public MessagesClearedEventArgs(Message[] messages)
    {
        Messages = messages;
    }
}