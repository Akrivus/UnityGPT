using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VoiceRecorder))]
public class VoiceRecorderUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] TextMeshProUGUI chatLog;
    [SerializeField] ScrollRect chatScroll;
    [SerializeField] Button toggle;
    [SerializeField] Button resetButton;
    [SerializeField] Button calibrate;

    [SerializeField] Color color;
    [SerializeField] float speed = 1;
    [SerializeField] WhisperChat chat;

    VoiceRecorder recorder;

    void Awake()
    {
        recorder = GetComponent<VoiceRecorder>();
    }

    void Update()
    {
        var noise = Mathf.Clamp01((recorder.NoiseLevel - recorder.NoiseFloor) / (recorder.NoiseFloor * 2));
        var silence = Mathf.Sin(Mathf.Pow(Time.realtimeSinceStartup * speed, 1f + Mathf.Clamp01(recorder.SecondsOfSilence / recorder.MaxPauseLength)));
        var a = Mathf.Clamp01(Mathf.Clamp01(noise + silence) + 0.2f);
        color = new Color(color.r, color.g, color.b, a);
        label.color = color;
        label.enabled = recorder.IsRecording && !recorder.IsCalibrating;
        calibrate.onClick.AddListener(() => recorder.Calibrate());
        calibrate.gameObject.SetActive(!recorder.IsCalibrating);
        toggle.onClick.AddListener(() => chatScroll.gameObject.SetActive(!chatScroll.gameObject.activeSelf));
        resetButton.onClick.AddListener(() => chat.ResetChat());
    }

    public void AddNewMessage(string name)
    {
        chatLog.text += $"\n<b>{name}</b>: ";
    }

    public void AddText(string text)
    {
        chatLog.text += text;
    }
}