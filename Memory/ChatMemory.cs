using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SpeechAgent))]
public class ChatMemory : MonoBehaviour, IVectorDB
{
    [SerializeField] List<VectorString> data = new List<VectorString>();
    
    ITextGenerator text;
    IEmbedding embedding;

    void Awake()
    {
        embedding = new TextEmbedder();
    }

    public void Add(string content, float[] vector)
    {
        data.Add(new VectorString(content, vector));
    }

    public void Add(string content)
    {
        embedding.FetchEmbeddingFor(content).Then(vector => Add(content, vector));
    }

    public void Add()
    {
        text.RespondTo("Summarize this conversation, including what you learned and what you " +
            "would like to learn more about.").Then(text => Add(text));
    }

    public VectorResult[] Find(float[] vector, int count = 1)
    {
        return data.Select(v => new VectorResult(v.Content, IVectorDB.Distance(v.Vector, vector)))
            .OrderBy(v => v.Distance)
            .Take(count)
            .ToArray();
    }
}