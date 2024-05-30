using RSG;

public class TextEmbedder : IEmbedding
{
    private LinkOpenAI api;

    public TextEmbedder(LinkOpenAI client)
    {
        api = client;
    }

    public IPromise<float[]> FetchEmbeddingFor(string text)
    {
        var body = new GenerateEmbedding(text);
        return api.Post<GeneratedEmbedding>(api.Uri_Embeddings, body)
            .Then(response => response.Embedding);
    }
}