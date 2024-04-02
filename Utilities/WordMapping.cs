using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "Word Map", menuName = "UnityGPT/Word Map")]
public class WordMapping : ScriptableObject
{
    [Tooltip("List of special words and their pronounciations")]
    [SerializeField] List<Mapping> mappings = new List<Mapping>();

    public string Filter(string text)
    {
        foreach (var mapping in mappings)
            text = Regex.Replace(text, mapping.Word, mapping.Phonetic, RegexOptions.IgnoreCase);
        return text;
    }
}

[Serializable]
public class Mapping
{
    public string Word;
    public string Phonetic;
}