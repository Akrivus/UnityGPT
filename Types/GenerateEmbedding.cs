
public class GenerateEmbedding
{
    public string Input { get; set; }
    public string Model { get; set; } = "text-embedding-3-small";
    public int Dimensions { get; set; } = 512;

    public GenerateEmbedding(string input, int dimensions = 512)
    {
        Input = input;
        Dimensions = dimensions;
    }
}

public class GeneratedEmbedding
{
    public string Object { get; set; } = "list";
    public Embeddings[] Data { get; set; }
    public string Model { get; set; } = "text-embedding-3-small";
    public Usage Usage { get; set; }

    public float[] Embedding => Data[0].Embedding;
}

public class Embeddings
{
    public string Object { get; set; } = "embedding";
    public int Index { get; set; } = 0;
    public float[] Embedding { get; set; }
}