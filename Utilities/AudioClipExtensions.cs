using System;
using System.Text;
using UnityEngine;

public static class AudioClipExtensions
{
    public const int BITS_PER_SAMPLE = 16;
    public const int BYTES_IN_FLOATS = 2;

    public static byte[] ToByteArray(float[] data, int channels, int frequency)
    {
        var samples = data.Length / channels;
        var bytes = new byte[44 + samples * BYTES_IN_FLOATS];
        var offset = WriteHeader(samples, channels, frequency, bytes);

        GetDataByteArray(data, channels, samples, bytes, offset);

        return bytes;
    }

    public static byte[] ToByteArray(this AudioClip clip, float floor = 0)
    {
        clip = clip.Trim(floor / clip.frequency);

        var data = new float[clip.samples];
        clip.GetData(data, 0);

        var channels = clip.channels;
        var frequency = clip.frequency;
        var samples = clip.samples;

        var bytes = new byte[44 + samples * channels * BYTES_IN_FLOATS];
        var offset = WriteHeader(samples, channels, frequency, bytes);

        GetDataByteArray(data, channels, samples, bytes, offset);

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

    public static void GetDataByteArray(float[] data, int channels, int samples, byte[] bytes, int offset = 0)
    {
        if (bytes.Length < offset + samples * channels)
            throw new ArgumentException("Buffer is too small for this AudioClip.");
        for (int i = 0; i < data.Length; ++i)
            BitConverter.GetBytes((short)(data[i] * short.MaxValue))
                .CopyTo(bytes, offset + i * BYTES_IN_FLOATS);
    }

    private static int WriteHeader(int samples, int channels, int frequency, byte[] bytes, int offset = 0)
    {
        offset = WriteData(Encoding.UTF8.GetBytes("RIFF"), bytes, offset);
        offset = WriteData(BitConverter.GetBytes(36 + bytes.Length), bytes, offset);
        offset = WriteData(Encoding.UTF8.GetBytes("WAVE"), bytes, offset);
        offset = WriteData(Encoding.UTF8.GetBytes("fmt "), bytes, offset);
        offset = WriteData(BitConverter.GetBytes(16), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((ushort)(1)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((ushort)(channels)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((uint)(frequency)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((uint)(frequency * channels * BITS_PER_SAMPLE)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((ushort)(channels * BITS_PER_SAMPLE)), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((ushort)(16)), bytes, offset);
        offset = WriteData(Encoding.UTF8.GetBytes("data"), bytes, offset);
        offset = WriteData(BitConverter.GetBytes((uint)(samples * channels - BITS_PER_SAMPLE)), bytes, offset);
        return offset;
    }

    private static int WriteData(byte[] buffer, byte[] bytes, int offset)
    {
        buffer.CopyTo(bytes, offset);
        return offset + buffer.Length;
    }

    private static float[] Normalize(float[] data)
    {
        float max = 0;
        for (int i = 0; i < data.Length; i++)
            max = Mathf.Max(max, Mathf.Abs(data[i]));
        for (int i = 0; i < data.Length; i++)
            data[i] /= max;
        return data;
    }

    public static float GetCurrentAmplitude(this AudioSource source)
    {
        float[] data = new float[400];
        source.clip.GetData(data, source.timeSamples);
        data = Normalize(data);

        float sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += Mathf.Abs(data[i]);
        return sum / data.Length;
    }
}
