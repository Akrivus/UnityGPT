using RSG;

public class TextEmbedder : IEmbedding
{
    const string URI = "https://api.openai.com/v1/embedding";

    public IPromise<float[]> FetchEmbeddingFor(string text)
    {
        var body = new GenerateEmbedding(text);
        return RestClientExtensions.Post<GeneratedEmbedding>(URI, body)
            .Then(response => response.Embedding);
    }
}