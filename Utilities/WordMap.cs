using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "Word Map", menuName = "UnityGPT/Word Map")]
public class WordMap : ScriptableObject
{
    [Tooltip("List of special words and their pronounciations")]
    [SerializeField] List<WordMapping> mappings = new List<WordMapping>();

    public string Filter(string text)
    {
        foreach (var mapping in mappings)
            text = Regex.Replace(text, mapping.Old, mapping.New, RegexOptions.IgnoreCase);
        return text;
    }
}

[Serializable]
public class WordMapping
{
    public string Old;
    public string New;
}