using RSG;

public interface IEmbedding
{
    public IPromise<float[]> Embed(string text, int dimensions = 1536);
}
