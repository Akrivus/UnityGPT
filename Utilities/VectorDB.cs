using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class VectorDB : MonoBehaviour
{
    List<VectorString> data = new List<VectorString>();

    public void Add(string content, float[] vector)
    {
        var vectorString = new VectorString(content, vector);
        data.Add(vectorString);
    }
}

[Serializable]
public class VectorString
{
    [TextArea(3, 10)]
    [SerializeField] string content;
    float[] vector;

    public string Content => content;
    public float[] Vector => vector;

    public VectorString(string content, float[] vector)
    {
        this.content = content;
        this.vector = vector;
    }
}