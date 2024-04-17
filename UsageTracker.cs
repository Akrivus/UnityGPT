using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Proyecto26;
using System;
using UnityEngine;

public class UsageTracker : MonoBehaviour
{
    public static UsageTracker Instance { get; private set; }

    [SerializeField] string OPENAI_API_KEY;
    [SerializeField] UsageCategory promptTokens;
    [SerializeField] UsageCategory completionTokens;
    [SerializeField] UsageCategory textToSpeech;
    [SerializeField] UsageCategory speechToText;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(OPENAI_API_KEY))
            OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        RestClient.DefaultRequestHeaders["Authorization"] = "Bearer " + OPENAI_API_KEY;
        Instance = Instance ?? this;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}

[Serializable]
public class UsageCategory
{
    public float Charge;
    public int UseCount;

    public float Calculate(int uses)
    {
        return Charge / UseCount * uses;
    }
}