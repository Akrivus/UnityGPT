using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "Word Map", menuName = "UnityGPT/Word Map")]
public class WordMapping : ScriptableObject
{
    [Tooltip("List of stop codes for generating speech fragments")]
    [SerializeField]
    private List<StopCode> stopCodes = new List<StopCode>()
    {
        new StopCode { Code = ".",  Delay = 0.2f },
        new StopCode { Code = "?",  Delay = 0.3f },
        new StopCode { Code = "!",  Delay = 0.1f },
        new StopCode { Code = "\n", Delay = 0.4f },
    };

    [Tooltip("List of special words and their pronounciations")]
    [SerializeField]
    private List<Mapping> mappings = new List<Mapping>();

    public string Filter(string text)
    {
        foreach (var mapping in mappings)
            text = Regex.Replace(text, mapping.Word, mapping.Phonetic, RegexOptions.IgnoreCase);
        return text;
    }

    public bool MatchStopCode(string token)
    {
        foreach (var code in stopCodes)
            if (token.Contains(code.Code))
                return true;
        return false;
    }

    public float GetStopCodeDelay(string text)
    {
        foreach (var code in stopCodes)
            if (text.EndsWith(code.Code))
                return code.Delay;
        return 0.0f;
    }
}

[Serializable]
public class Mapping
{
    public string Word;
    public string Phonetic;
}

[Serializable]
public class StopCode
{
    public string Code;
    public float Delay = 1.0f;
}