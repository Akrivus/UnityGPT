using RSG;

public interface IText
{
    public string Prompt { get; set; }
    public IPromise<string> Ask(string content);
    public IPromise<string> Listen();
    public void Tell(string content);
    public void ResetContext();
}

public class TextEventArgs
{
    public string Text { get; private set; }

    public TextEventArgs(string text)
    {
        Text = text;
    }
}