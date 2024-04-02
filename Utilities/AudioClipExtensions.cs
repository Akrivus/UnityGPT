using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class AudioClipExtensions
{
    public static bool Save(this AudioClip clip, string filename)
    {
        if (!filename.EndsWith(".wav"))
            filename += ".wav";
        var filepath = Path.Combine(Application.persistentDataPath, filename);
        Directory.CreateDirectory(Path.GetDirectoryName(filepath));
        clip = clip.Trim(0f);
        Debug.Log($"Saving {clip.name} to {filepath}");

        using (var stream = CreateEmpty(filepath))
        {
            stream.Seek(0, SeekOrigin.Begin);
            WriteHeader(clip, stream);
            ConvertAndWrite(clip, stream);
        }
        return true;
    }

    static AudioClip Trim(this AudioClip clip, float min)
    {
        var data = new float[clip.samples];
        clip.GetData(data, 0);
        var samples = TrimList(data.ToList(), min);
        var ch = clip.channels;
        var fy = clip.frequency;

        clip = AudioClip.Create(string.Empty, samples.Count, ch, fy, false);
        clip.SetData(samples.ToArray(), 0);
        return clip;
    }

    static List<float> TrimList(List<float> samples, float min)
    {
        var start = 0;
        var end = samples.Count - 1;
        for (var i = 0; i < samples.Count; i++)
            if (Mathf.Abs(samples[i]) > min)
            {
                start = i;
                break;
            }
        for (var i = samples.Count - 1; i >= 0; i--)
            if (Mathf.Abs(samples[i]) > min)
            {
                end = i;
                break;
            }
        return samples.GetRange(start, end - start);
    }

    static FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < 44; i++)
            fileStream.WriteByte(emptyByte);

        return fileStream;
    }

    static void ConvertAndWrite(AudioClip clip, FileStream stream)
    {
        var data = new float[clip.samples];
        clip.GetData(data, 0);
        byte[] bytes = new byte[data.Length * clip.channels * 2];

        for (int i = 0; i < data.Length; ++i)
        {
            var s = (short)(data[i] * short.MaxValue);
            BitConverter.GetBytes(s).CopyTo(bytes, i * 2);
        }

        stream.Write(bytes, 0, bytes.Length);
    }

    static void WriteHeader(AudioClip clip, FileStream stream)
    {
        var bitsPerSample = 16 / 8;
        stream.Write(Encoding.UTF8.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(36 + stream.Length), 0, 4);
        stream.Write(Encoding.UTF8.GetBytes("WAVE"), 0, 4);
        stream.Write(Encoding.UTF8.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((ushort)(1)), 0, 2);
        stream.Write(BitConverter.GetBytes((ushort)(clip.channels)), 0, 2);
        stream.Write(BitConverter.GetBytes((uint)(clip.frequency)), 0, 4);
        stream.Write(BitConverter.GetBytes((uint)(clip.frequency * clip.channels * bitsPerSample)), 0, 4);
        stream.Write(BitConverter.GetBytes((ushort)(clip.channels * bitsPerSample)), 0, 2);
        stream.Write(BitConverter.GetBytes((ushort)(16)), 0, 2);
        stream.Write(Encoding.UTF8.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes((uint)(clip.samples * clip.channels - bitsPerSample)), 0, 4);
    }
}
