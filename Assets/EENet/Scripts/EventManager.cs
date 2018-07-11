using System;
using System.Collections.Generic;
using System.IO;
using GameDevWare.Serialization;

namespace EENet
{
    public class EventManager : IDisposable
    {

        private Dictionary<uint, Action<Dictionary<string, object>>> callbackMap;

        private Dictionary<string, List<Action<Dictionary<string, object>>>> eventMap;

        private IProtocol protocol;


        public EventManager(IProtocol protocol)
        {
            this.callbackMap = new Dictionary<uint, Action<Dictionary<string, object>>>();
            this.eventMap = new Dictionary<string, List<Action<Dictionary<string, object>>>>();
            this.protocol = protocol;
        }

        public void AddCallback(uint id, Action<Dictionary<string, object>> callback)
        {
            if (id > 0 && callback != null)
            {
                this.callbackMap.Add(id, callback);
            }
        }

        public void InvokeCallback(uint id, byte[] msg)
        {
            if (!callbackMap.ContainsKey(id)) return;
            Dictionary<string, object> dic = protocol.Unmarshal<Dictionary<string, object>>(msg);
            callbackMap[id].Invoke(dic);
            // after invoke. remove id
            callbackMap.Remove(id);
        }

        public void AddOnEvent(string eventName, Action<Dictionary<string, object>> callback)
        {
            List<Action<Dictionary<string, object>>> list = null;
            if (this.eventMap.TryGetValue(eventName, out list))
            {
                list.Add(callback);
            }
            else
            {
                list = new List<Action<Dictionary<string, object>>>();
                list.Add(callback);
                this.eventMap.Add(eventName, list);
            }
        }

        public void InvokeOnEvent(string eventName, byte[] msg)
        {
            if (!this.eventMap.ContainsKey(eventName)) return;
            List<Action<Dictionary<string, object>>> list = this.eventMap[eventName];
            var dic = protocol.Unmarshal<Dictionary<string, object>>(msg);
            foreach (Action<Dictionary<string, object>> action in list)
            {
                action.Invoke(dic);
            }
        }


        public void Dispose()
        {
            this.callbackMap.Clear();
            this.eventMap.Clear();
        }
    }
}
