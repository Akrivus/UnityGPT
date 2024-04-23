using RSG;

public interface IEmbedding
{
    public IPromise<float[]> FetchEmbeddingFor(string text);
}
