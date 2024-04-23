
public class GenerateEmbedding
{
    public EmbeddingModel Model { get; set; } = EmbeddingModel.TextEmbedding3Small;
    public string Input { get; set; }
    public int Dimensions { get; set; } = 1536;

    public GenerateEmbedding(string input, int dimensions = 1536)
    {
        Input = input;
        Dimensions = dimensions;
    }
}

public class GeneratedEmbedding
{
    public string Object { get; set; } = "list";
    public EmbeddingModel Model { get; set; } = EmbeddingModel.TextEmbedding3Small;
    public Embeddings[] Data { get; set; }
    public Usage Usage { get; set; }

    public float[] Embedding => Data[0].Embedding;
}

public class Embeddings
{
    public string Object { get; set; } = "embedding";
    public int Index { get; set; } = 0;
    public float[] Embedding { get; set; }
}