using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "Word Map", menuName = "UnityGPT/Word Map")]
public class WordMapping : ScriptableObject
{
    [Tooltip("List of stop codes for generating speech fragments")]
    [SerializeField]
    private string[] stopCodes = new string[] { ".", "?", "!", "\n" };

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
            if (token.Contains(code))
                return true;
        return false;
    }
}

[Serializable]
public class Mapping
{
    public string Word;
    public string Phonetic;
}