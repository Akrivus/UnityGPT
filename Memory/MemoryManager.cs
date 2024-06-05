using Newtonsoft.Json;
using RSG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(AgenticStateMachine))]
public class MemoryManager : MonoBehaviour
{
    private const string MEMORY_PROMPT = "What is on your mind?";
    private const string Name = "Contextualizer";

    public AgenticStateMachine ASM;

    public int EmbeddingLength = 24;

    public List<Memory> Memories = new List<Memory>();

    private TextEmbedder embedder;

    private void Awake()
    {
        ASM = ASM ?? GetComponent<AgenticStateMachine>();
        Memories = LoadMemories();
        embedder = new TextEmbedder(ASM.Client);
    }

    private void Start()
    {
        ASM.AI.AddTool(new QuickTool(WriteInJournal,
            "WriteInJournal", "Record your thoughts for future reference.",
            new StringParam("Value", "Your thoughts.")));
        ASM.AI.AddTool(new QuickTool(SearchMemory,
            "SearchMemory", "Search internal memory for additional context.",
            new StringParam("Query", "Query for semantic search.")));
        ASM.Speaker.OnBeforeContextReset += GenerateContext;
    }

    private IPromise GenerateContext(SpeechGenerator speech)
        => speech.Text.RespondTo(MEMORY_PROMPT, ASM.Name.Format(Name, ASM.Name))
            .Then(SetContext);

    private IPromise<string> SearchMemory(QuickTool.Args args)
        => Retrieve(args.Query).Then((content) => content ?? "No memory found.");

    private void WriteInJournal(QuickTool.Args args)
        => Memorize(args.Value);

    private void SetContext(string context)
    {
        Memorize(context).Then(() => ASM.Talking.Context = context);
    }

    private IPromise Memorize(string content)
        => embedder.Embed(content, EmbeddingLength).Then((embeddings) => AddMemory(content, embeddings));

    private IPromise<string> Retrieve(string content)
        => embedder.Embed(content, EmbeddingLength).Then(UpdateSemantics)
            .Then((embeddings) => {
                var memory = Memories.Find(m => m.Similarity > 0.5f);
                return memory != null ? memory.Content : null;
            });

    private float[] UpdateSemantics(float[] embeddings)
    {
        Memories.ForEach((memory) => memory.UpdateSemantics(embeddings));
        return embeddings;
    }

    private void AddMemory(string content, float[] embeddings)
    {
        Memories.Add(new Memory(content, embeddings));
        StartCoroutine(SaveMemories());
    }

    private List<Memory> LoadMemories()
    {
        var path = Application.persistentDataPath + $"/MemoriesOf{name}.json";
        if (!File.Exists(path))
            return new List<Memory>();
        var json = File.ReadAllText(path);
        var memories = JsonConvert.DeserializeObject<List<Memory>>(json);
        return memories;
    }

    private IEnumerator SaveMemories()
    {
        var path = Application.persistentDataPath + $"/MemoriesOf{name}.json";
        var json = JsonConvert.SerializeObject(Memories);
        File.WriteAllText(path, json);

        yield return null;
    }
}

[Serializable]
public class Memory
{
    [field: SerializeField]
    public string Content { get; set; }
    public float[] Embedding { get; set; }
    public DateTime Time { get; set; }

    public float Similarity => similarity;

    private float similarity;

    public Memory(string content, float[] embedding)
    {
        Content = content;
        Embedding = embedding;
        Time = DateTime.Now;
    }

    public Memory()
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