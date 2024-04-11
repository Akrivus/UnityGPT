using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine.Networking;

namespace Proyecto26
{
    public class ServerSentEventHandler<T> : DownloadHandlerScript
    {
        public event Action<ServerSentEvent<string>> OnServerSentEventReceived;
        public event Action<ServerSentEvent<T>> OnServerSentEvent;
        public event Action<ServerSentEvent<List<T>>> OnServerSentEventDone;

        private readonly List<T> feed = new List<T>();
        private readonly StringBuilder buffer = new StringBuilder();

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            foreach (var cter in data)
            {
                if (cter == '\n')
                    ProcessLineFeed();
                buffer.Append((char)cter);
            }
            return true;
        }

        private void ProcessLineFeed()
        {
            OnServerSentEventReceived?.Invoke(new ServerSentEvent<string> { Data = buffer.ToString() });
            var lines = buffer.ToString().Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            buffer.Clear();
            foreach (var line in lines)
            {
                string json;
                if (line.StartsWith("data: "))
                    json = line.Substring(6);
                else return;
                if (json.Equals("[DONE]"))
                    OnServerSentEventDone?.Invoke(new ServerSentEvent<List<T>> { Data = feed });
                else
                    ProcessData(json);
            }
        }

        private void ProcessData(string json)
        {
            var data = RestClientExtensions.Deserialize<T>(json);
            feed.Add(data);
            OnServerSentEvent?.Invoke(new ServerSentEvent<T> { Data = data });
        }
    }

    public class ServerSentEvent<T>
    {
        public T Data;
    }
}