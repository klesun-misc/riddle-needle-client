using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

namespace Network
{
    /// <summary>
    ///   <para>An extension for the NetworkManager that displays a default HUD for controlling the network state of the game.</para>
    /// </summary>
    [AddComponentMenu("Network/NetworkManagerHUD")]
    [RequireComponent(typeof(NetworkManager))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SplitNetworkManagerClientHud : MonoBehaviour
    {
        /// <summary>
        ///   <para>Whether to show the default control HUD at runtime.</para>
        /// </summary>
        [SerializeField] public bool showGUI = true;

        /// <summary>
        ///   <para>The NetworkManager associated with this HUD.</para>
        /// </summary>
        public NetworkManager manager;

        /// <summary>
        ///   <para>The horizontal offset in pixels to draw the HUD runtime GUI at.</para>
        /// </summary>
        [SerializeField] public int offsetX;

        /// <summary>
        ///   <para>The vertical offset in pixels to draw the HUD runtime GUI at.</para>
        /// </summary>
        [SerializeField] public int offsetY;

        private bool m_ShowServer;

        private void Awake()
        {
            this.manager = this.GetComponent<NetworkManager>();
        }

        private void OnGUI()
        {
            if (!this.showGUI)
                return;
            int num1 = 10 + this.offsetX;
            int num2 = 40 + this.offsetY;
            bool notConnected = this.manager.client == null || this.manager.client.connection == null ||
                                this.manager.client.connection.connectionId == -1;
            if (!this.manager.IsClientConnected() && !NetworkServer.active &&
                this.manager.matchMaker == null)
            {
                if (notConnected) {
                    if (GUI.Button(new Rect((float) num1, (float) num2, 105f, 20f), "LAN Client(C)"))
                        this.manager.StartClient();
                    this.manager.networkAddress = GUI.TextField(new Rect((float) (num1 + 100), (float) num2, 95f, 20f),
                        this.manager.networkAddress);
                    int num3 = num2 + 24;
                } else {
                    GUI.Label(new Rect((float) num1, (float) num2, 200f, 20f),
                        "Connecting to " + this.manager.networkAddress + ":" + (object) this.manager.networkPort +
                        "..");
                    num2 += 24;
                    if (GUI.Button(new Rect((float) num1, (float) num2, 200f, 20f), "Cancel Connection Attempt"))
                        this.manager.StopClient();
                }
            } else {
                if (NetworkServer.active) {
                    string text = "Server: port=" + (object) this.manager.networkPort;
                    if (this.manager.useWebSockets)
                        text += " (Using WebSockets)";
                    GUI.Label(new Rect((float) num1, (float) num2, 300f, 20f), text);
                    num2 += 24;
                }

                if (this.manager.IsClientConnected()) {
                    GUI.Label(new Rect((float) num1, (float) num2, 300f, 20f),
                        "Client: address=" + this.manager.networkAddress + " port=" +
                        (object) this.manager.networkPort);
                    num2 += 24;
                }
            }

            if (this.manager.IsClientConnected() && !ClientScene.ready) {
                if (GUI.Button(new Rect((float) num1, (float) num2, 200f, 20f), "Client Ready")) {
                    ClientScene.Ready(this.manager.client.connection);
                    if (ClientScene.localPlayers.Count == 0)
                        ClientScene.AddPlayer((short) 0);
                }

                num2 += 24;
            }

            if (NetworkServer.active || this.manager.IsClientConnected()) {
                if (GUI.Button(new Rect((float) num1, (float) num2, 200f, 20f), "Stop (X)"))
                    this.manager.StopHost();
                num2 += 24;
            }
        }
    }
}