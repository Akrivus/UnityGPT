using System;
using UnityEngine;

public interface IVectorDB
{
    void Add(string content, float[] vector);
    void Add(string content);
    VectorResult[] Find(float[] vector, int count = 1);

    public static float Distance(float[] a, float[] b)
    {
        Vector3 dab = Vector3.zero;
        for (int i = 0; i < a.Length; i++)
            dab += new Vector3(a[i] * b[i], a[i] * a[i], b[i] * b[i]);
        dab = new Vector3(dab.x, Mathf.Sqrt(dab.y), Mathf.Sqrt(dab.z));
        return 1 - dab.x / (dab.y * dab.z);
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

[Serializable]
public class VectorResult
{
    public string Content { get; private set; }
    public float Distance { get; private set; }

    public VectorResult(string content, float distance)
    {
        Content = content;
        Distance = distance;
    }
}