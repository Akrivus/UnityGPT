using Newtonsoft.Json;
using RSG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(AgenticStateMachine))]
public class ContextManager : MonoBehaviour
{
    private const string CONTEXT_PROMPT = "Describe what you've learned and what you need to remember.\n " +
        "Speak in second person, use 'You' statements, and be specific and detailed.";

    public AgenticStateMachine ASM;

    public List<Context> Contexts = new List<Context>();

    private TextEmbedder embedder;

    private void Awake()
    {
        ASM = ASM ?? GetComponent<AgenticStateMachine>();
        embedder = new TextEmbedder(ASM.Client);

        LoadMemories();
    }

    private void Start()
    {
        ASM.AI.AddTool(new QuickTool(WriteInJournal,
            "WriteInJournal", "Record your thoughts for future reference.",
            new StringParam("Value", "Your thoughts.")));
        ASM.AI.AddTool(new QuickTool(SearchContext,
            "SearchContext", "Search internal memory for additional context.",
            new StringParam("Query", "Query for semantic search."),
            new NumberParam("Threshhold", "Minimum similarity score.", false, 0.0f, 1.0f),
            new IntegerParam("Limit", "Maximum number of memories to return.", false, 1, 12)));
        ASM.Speaker.OnBeforeContextReset += GenerateSituationalContext;

    }

    private string Retrieve(int limit, float threshhold)
    {
        string retrieval = "";
        foreach (var memory in Contexts)
        {
            if (memory.Similarity < threshhold)
                break;
            retrieval += memory.Content + "\n";
            if (--limit == 0)
                break;
        }
        if (string.IsNullOrEmpty(retrieval))
            return "No memory found; refer to context.";
        return retrieval;
    }

    private float[] UpdateSemantics(float[] embedding)
    {
        foreach (var memory in Contexts)
            memory.UpdateSemantics(embedding);
        Contexts.Sort((a, b) => b.Similarity.CompareTo(a.Similarity));
        return embedding;
    }

    private void AddContext(string content, float[] embedding)
    {
        Contexts.Add(new Context(content, embedding));
        StartCoroutine(SaveMemories());
    }

    private void ReadMemories(string path)
    {
        var json = File.ReadAllText(path);
        Contexts = JsonConvert.DeserializeObject<List<Context>>(json);
    }

    private void CreateMemories(int i = 0)
    {
        if (i >= Contexts.Count) return;
        var memory = Contexts[i];
        embedder.Embed(memory.Content, EmbeddingLength)
            .Then((embedding) => {
                memory.Embedding = embedding;
                CreateMemories(i + 1);
            });
    }

    private void LoadMemories()
    {
        var path = Application.persistentDataPath + $"/MemoriesOf{name}.json";
        if (File.Exists(path))
            ReadMemories(path);
        else
            CreateMemories();
    }

    private IEnumerator SaveMemories()
    {
        var path = Application.persistentDataPath + $"/MemoriesOf{name}.json";
        var json = JsonConvert.SerializeObject(Contexts);
        File.WriteAllText(path, json);

        yield return null;
    }
}

[Serializable]
public class Context
{
    [field: SerializeField, TextArea]
    public string Content { get; set; }
    public float[] Embedding { get; set; }
    public DateTime Time { get; set; }

    public float Similarity => similarity;

    private float similarity;

    public Context(string content, float[] embedding)
    {
        Content = content;
        Embedding = embedding;
        Time = DateTime.Now;
    }

    public Context()
    {
        Time = DateTime.Now;
    }

    public void UpdateSemantics(float[] embedding)
    {
        similarity = CosineSimilarity(embedding);
    }

    private float CosineSimilarity(float[] embedding)
    {
        int n = embedding.Length;
        float dot = 0;
        double mag1 = 0;
        double mag2 = 0;

        for (int i = 0; i < n; i++)
        {
            dot += embedding[i] * Embedding[i];
            mag1 += Math.Pow(embedding[i], 2);
            mag2 += Math.Pow(Embedding[i], 2);
        }

        return dot / (float)(Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }
}