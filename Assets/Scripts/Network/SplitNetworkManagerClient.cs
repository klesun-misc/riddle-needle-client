using System;
using System.Collections.Generic;
using System.IO;
using Gui;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Network {
    /**
     * will use this to do the transport stuff of separation of client code from server code
     */
    public class SplitNetworkManagerClient : NetworkManager {

        public InputField console;
        public CamStreamTv camStreamTv;

        public void Log(String msg)
        {
            Debug.Log(msg);
            if (console != null) {
                var full = (msg + "\n" + console.text);
                console.text = full.Substring(0, Math.Min(full.Length, 1000));
            }
        }

        private String json(object data)
        {
            var sets = new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            };
            var writer = new StringWriter();
            JsonSerializer.Create(sets).Serialize(writer, data);
            return writer.ToString();
        }

        private List<NetworkClient> serverConnections = new List<NetworkClient>();

        public override void OnClientConnect(NetworkConnection conn)
        {
            var client = new NetworkClient(conn);
            base.OnClientConnect(conn);
            var config = new ConnectionConfig();
            client.Configure(config, 5);
            client.RegisterHandler(MsgType.Highest + 1, msg => {
                var str = msg.reader.ReadString();
                try {
                    var msgData = JsonConvert.DeserializeObject<Msg> (str);
                    if (msgData.type == Msg.EType.Error) {
                        Log("Server responded with error: " + msgData.strValue);
                    } else {
                        Log("unexpected event type came from server " + msgData.type + " " + str);
                    }
                } catch (Exception exc) {
                    Debug.Log("Could not parse JSON message - " + exc.Message + " - " + str);
                }
            });
            client.RegisterHandler(MsgType.Highest + 2, msg => {
                var bytes = msg.reader.ReadBytesAndSize();
                camStreamTv.SetFrameImgBytes(bytes);
            });
            serverConnections.Add(client);
        }

        public void SendMsg(Msg msg)
        {
            serverConnections.ForEach(serv => {
                var dataStr = JsonConvert.SerializeObject(msg);
                serv.SendUnreliable(MsgType.Highest + 1, new StringMessage(dataStr));
            });
        }
    }
}