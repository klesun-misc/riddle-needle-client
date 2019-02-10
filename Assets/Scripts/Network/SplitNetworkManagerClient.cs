using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.UI;

namespace Network {
    /**
     * will use this to do the transport stuff of separation of client code from server code
     */
    public class SplitNetworkManagerClient : NetworkManager {

        public InputField console;
        public RawImage tv;

        private Texture2D tvTex;

        float lastSyncAt = 0;

        private void Log(String msg)
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

        private void Start()
        {
            tvTex = new Texture2D(0, 0);
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            var client = new NetworkClient(conn);
            base.OnClientConnect(conn);
            var config = new ConnectionConfig();
            client.Configure(config, 5);
            client.RegisterHandler(MsgType.Highest + 2, msg => {
                var bytes = msg.reader.ReadBytesAndSize();
                tvTex.LoadImage(bytes);
                tv.texture = tvTex;
            });
            serverConnections.Add(client);
        }

        private KeyCode getMouseEventKey(Event e)
        {
            var keyCode = KeyCode.None;
            if (e.button == 0) {
                keyCode = KeyCode.Mouse0;
            } else if (e.button == 1) {
                keyCode = KeyCode.Mouse1;
            } else if (e.button == 2) {
                keyCode = KeyCode.Mouse2;
            } else {
                Log("unsupported mouse button - " + e.button);
            }
            return keyCode;
        }

        private List<Msg> makeMessagesFromGuiEvent(Event e)
        {
            var msgs = new List<Msg>();
            if (e.type == EventType.KeyDown) {
                // GetKeyDown - to filter OS key auto-repeat
                if (Input.GetKeyDown(e.keyCode)) {
                    msgs.Add(new Msg{
                        type = Msg.EType.KeyDown,
                        keyCode = e.keyCode,
                    });
                }
            } else if (e.type == EventType.KeyUp) {
                msgs.Add(new Msg{
                    type = Msg.EType.KeyUp,
                    keyCode = e.keyCode,
                });
            } else if (e.type == EventType.MouseUp) {
                msgs.Add(new Msg{
                    type = Msg.EType.KeyUp,
                    keyCode = getMouseEventKey(e),
                });
            } else if (e.type == EventType.MouseDown) {
                msgs.Add(new Msg{
                    type = Msg.EType.KeyDown,
                    keyCode = getMouseEventKey(e),
                });
            } else if (e.type == EventType.Repaint || e.type == EventType.Layout) {
                // ignored if mouse position did not change
            } else {
                // fires a additional event on keydown
                Log("unhandled event - " + e.type + " " + e);
            }
            return msgs;
        }

        void OnGUI()
        {
            var e = Event.current;
            var msgs = makeMessagesFromGuiEvent(e);
            msgs.ForEach((msg) => {
                serverConnections.ForEach(serv => {
                    Cursor.lockState = CursorLockMode.Locked; // lock on any input when we are connected to any server
                    var dataStr = JsonConvert.SerializeObject(msg);
                    serv.SendUnreliable(MsgType.Highest + 1, new StringMessage(dataStr));
                });
            });
        }

        void Update ()
        {
            var msgs = new List<Msg>();
            var mouseDelta = new V2{
                x = Input.GetAxis("Mouse X"),
                y = Input.GetAxis("Mouse Y")
            };
            if (mouseDelta.toStd().magnitude > 0.000001f) {
                msgs.Add(new Msg{
                    type = Msg.EType.MouseMove,
                    mouseDelta = mouseDelta,
                });
            }
            if (Time.fixedTime - lastSyncAt > 2.5f) {
                lastSyncAt = Time.fixedTime;
                msgs.Add(new Msg{
                    type = Msg.EType.Sync,
                });
            }
            msgs.ForEach((msg) => {
                serverConnections.ForEach(serv => {
                    var dataStr = JsonConvert.SerializeObject(msg);
                    serv.SendUnreliable(MsgType.Highest + 1, new StringMessage(dataStr));
                });
            });
        }
    }

}