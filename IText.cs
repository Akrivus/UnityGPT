using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IText
{
    public Task<string> GenerateTextAsync(string content);
    public IEnumerator GenerateText(string content);
    public Task<string> GenerateTextAsync(List<Message> messages);
    public IEnumerator GenerateText(List<Message> messages);
    public IText Clone();
}