using System;
using System.Collections;
using System.Threading.Tasks;

public interface IText
{
    public Task<string> GenerateTextAsync(string content);
    public IEnumerator GenerateText(string content);
    public IText Clone();
}