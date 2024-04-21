using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

[JsonConverter(typeof(StringEnumConverter))]
public enum EmbeddingModel
{
    [EnumMember(Value = "text-embedding-3-small")]
    TextEmbedding3Small,
    [EnumMember(Value = "text-embedding-3-large")]
    TextEmbedding3Large,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TextModel
{
    [EnumMember(Value = "gpt-3.5-turbo")]
    GPT_3p5_Turbo,
    [EnumMember(Value = "gpt-4-turbo")]
    GPT_4_Turbo
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TextToSpeechModel
{
    [EnumMember(Value = "tts-1")]
    TTS_1,
    [EnumMember(Value = "tts-1-hd")]
    TTS_1HD,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum WhisperModel
{
    [EnumMember(Value = "whisper-1")]
    Whisper_1,
}