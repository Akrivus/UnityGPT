using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(ChatGenerator))]
public class ChatMemory : MonoBehaviour, IVectorDB
{
    [SerializeField] List<VectorString> data = new List<VectorString>();
    
    ChatGenerator chat;
    IText text;
    IEmbedding embedding;

    void Awake()
    {
        chat = GetComponent<ChatGenerator>();
        text = chat.Text;
        embedding = chat.Embeddings;
    }

    public void Add(string content, float[] vector)
    {
        data.Add(new VectorString(content, vector));
    }

    public async Task AddAsync(string content)
    {
        Add(content, await embedding.GenerateEmbeddingAsync(content));
    }

    public async Task AddAsync()
    {
        var content = await text.GenerateTextAsync("Summarize this " +
            "conversation, including what you learned and what you " +
            "would like to learn more about.");
        await AddAsync(content);
    }

    public VectorResult[] Find(float[] vector, int count = 1)
    {
        return data.Select(v => new VectorResult(v.Content, IVectorDB.Distance(v.Vector, vector)))
            .OrderBy(v => v.Distance)
            .Take(count)
            .ToArray();
    }
}