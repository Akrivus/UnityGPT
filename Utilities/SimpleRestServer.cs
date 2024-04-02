using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

public class SimpleRestServer : MonoBehaviour
{
    [SerializeField] int port = 8080;
    [SerializeField] UnityEvent<HttpListenerRequest, HttpListenerResponse> listeners;
    HttpListener _listener;

    private void Awake()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        StartCoroutine(Listen());
    }

    IEnumerator Listen()
    {
        _listener.Start();
        while (enabled)
        {
            var task = _listener.GetContextAsync();
            yield return new WaitUntil(() => task.IsCompleted);
            var context = task.Result;
            var req = context.Request;
            var res = context.Response;
            listeners.Invoke(req, res);
        }
        yield return null;
    }
}