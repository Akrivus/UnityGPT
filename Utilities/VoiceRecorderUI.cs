using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VoiceRecorder))]
public class VoiceRecorderUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI label;
    [SerializeField]
    private TextMeshProUGUI chatLog;
    [SerializeField]
    private ScrollRect chatScroll;
    [SerializeField]
    private Button toggle;

    private TextMeshProUGUI toggleLabel;

    [SerializeField]
    private Color color;
    [SerializeField]
    private float speed = 1;
    [SerializeField]
    private WhisperAgent whisper;

    private VoiceRecorder recorder;

    private float maxNoiseLevel;

    void Awake()
    {
        recorder = GetComponent<VoiceRecorder>();
    }

    void Start()
    {
        toggleLabel = toggle.GetComponentInChildren<TextMeshProUGUI>();
        toggle.onClick.AddListener(() => ToggleChatWindow());

        SetGraph(0);
    }

    void Update()
    {
        var noise = Mathf.Clamp01((recorder.NoiseLevel - recorder.NoiseFloor) / (recorder.NoiseFloor * 2));
        var silence = Mathf.Sin(Mathf.Pow(Time.realtimeSinceStartup * speed, 1f + Mathf.Clamp01(recorder.SecondsOfSilence / recorder.MaxPauseLength)));
        var a = Mathf.Clamp01(Mathf.Clamp01(noise + silence) + 0.2f);
        color = new Color(color.r, color.g, color.b, a);

        label.color = color;
        label.enabled = recorder.IsRecording && !recorder.IsCalibrating;

        if (recorder.NoiseLevel > maxNoiseLevel)
            SetGraph(recorder.NoiseLevel);

        DebugGUI.Graph("NoiseLevel", recorder.NoiseLevel);
        DebugGUI.Graph("NoiseFloor", recorder.NoiseFloor);
    }

    public void AddNewMessage(string name)
    {
        chatLog.text += $"\n<b>{name}</b>: ";
    }

    public void AddText(string text)
    {
        chatLog.text += text;
    }

    void ToggleChatWindow()
    {
        var chatActive = chatScroll.gameObject.activeSelf;
        chatScroll.gameObject.SetActive(!chatActive);
        toggleLabel.text = chatActive ? "Show Chat" : "Hide Chat";
    }

    void SetGraph(float max)
    {
        DebugGUI.SetGraphProperties("NoiseLevel", "Noise Level", 0, max, 0, Color.green, false);
        DebugGUI.SetGraphProperties("NoiseFloor", "Noise Floor", 0, max, 0, Color.red, false);
        maxNoiseLevel = max;
    }
}