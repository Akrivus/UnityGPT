using System.Threading.Tasks;

public interface IEmbedding
{
    public Task<float[]> GenerateEmbeddingAsync(string text);
}
