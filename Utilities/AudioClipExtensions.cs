using System;
using System.Text;
using UnityEngine;

public static class AudioClipExtensions
{
    static int bitsPerSample = 16;

    public static byte[] ToByteArray(this AudioClip clip, float floor)
    {
        clip = clip.Trim(floor / clip.frequency);
        var bytes = new byte[44 + clip.samples * clip.channels * 2];
        var offset = WriteHeader(clip, bytes);
        clip.GetDataByteArray(bytes, offset);
        return bytes;
    }

    public static AudioClip Trim(this AudioClip clip, float floor)
    {
        var data1 = new float[clip.samples];
        clip.GetData(data1, 0);
        var start = 0;
        var end = data1.Length - 1;
        for (var i = 0; i < data1.Length; ++i)
            if (Mathf.Abs(data1[i]) > floor)
            {
                start = i;
                break;
            }
        for (var i = data1.Length - 1; i >= 0; --i)
            if (Mathf.Abs(data1[i]) > floor)
            {
                end = i;
                break;
            }
        var data2 = new float[end - start + 1];
        for (var i = 0; i < data2.Length; ++i)
            data2[i] = data1[start + i];
        clip = AudioClip.Create(clip.name, data2.Length, clip.channels, clip.frequency, false);
        clip.SetData(data2, 0);
        return clip;
    }

    public static void GetDataByteArray(this AudioClip clip, byte[] bytes, int offset = 0)
    {
        if (bytes.Length < offset + clip.samples * clip.channels)
            throw new ArgumentException("Buffer is too small for this AudioClip.");
        var data = new float[clip.samples];
        clip.GetData(data, 0);
        for (int i = 0; i < data.Length; ++i)
            BitConverter.GetBytes((short)(data[i] * short.MaxValue)).CopyTo(bytes, offset + i * 2);
    }

    static int WriteHeader(this AudioClip clip, byte[] bytes, int offset = 0)
    {
        offset = WriteData(Encoding.UTF8.GetBytes("RIFF"), bytes, offset);
        offset = WriteData(BitConverter.GetBytes(36 + bytes.Length), bytes, offset);
        offset = WriteData(Encoding.UTF8.GetBytes("WAVE"), bytes, offset);
        offset = WriteData(Encoding.UTF8.GetBytes("fmt "), bytes, offset);
        offset = WriteData(BitConverter.GetBytes(16), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((ushort)(1)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((ushort)(clip.channels)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((uint)(clip.frequency)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((uint)(clip.frequency * clip.channels * bitsPerSample)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((ushort)(clip.channels * bitsPerSample)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((ushort)(16)), bytes, offset);
        offset = WriteData(Encoding.UTF8.GetBytes("data"), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((uint)(clip.samples * clip.channels - bitsPerSample)), bytes, offset);
        return offset;
    }

    static int WriteData(byte[] buffer, byte[] bytes, int offset)
    {
        buffer.CopyTo(bytes, offset);
        return offset + buffer.Length;
    }
}
