using RSG;

public class TextEmbedder : IEmbedding
{
    private LinkOpenAI api;

    public TextEmbedder(LinkOpenAI client)
    {
        api = client;
    }

    public IPromise<float[]> Embed(string text, int dimensions)
    {
        var body = new GenerateEmbedding(text, dimensions);
        return api.Post<GeneratedEmbedding>(api.Uri_Embeddings, body)
            .Then(response => response.Embedding);
    }
}