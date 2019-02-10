using System;
using System.Collections.Generic;
using System.Linq;
using Network;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.UI;
using System.Runtime.InteropServices;

namespace Gui
{
    public class InputControls : MonoBehaviour
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [Serializable]
        public struct Kv {
            public string k;
            public Sprite v;
        }

        public SplitNetworkManagerClient client;
        public Dropdown generalDropdown;
        public Kv[] colorSpriteTuples;

        private Dictionary<string, Sprite> colorSprites = new Dictionary<string, Sprite>();
        float lastSyncAt = 0;

        void Awake ()
        {
            foreach (var pair in colorSpriteTuples) {
                colorSprites[pair.k] = pair.v;
            }
        }

        private void Log(string msg)
        {
            client.Log(msg);
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

        /** "MakeDropDownOption" */
        Dropdown.OptionData MkDdOpt(string text)
        {
            var opt = new Dropdown.OptionData (text);

            if (colorSprites.ContainsKey(text)) {
                opt.image = colorSprites [text];
            }

            return opt;
        }

        private void AskForChoice(string[] options, Action<string> cb)
        {
            generalDropdown.options.Clear ();
            generalDropdown.options.Add (new Dropdown.OptionData("none"));
            generalDropdown.AddOptions (options.ToList().Select (k => MkDdOpt(k)).ToList());

            var tmp = generalDropdown.onValueChanged;
            generalDropdown.value = 0;
            generalDropdown.onValueChanged = tmp;
            generalDropdown.Hide ();

            generalDropdown.onValueChanged.AddListener ((i) => {
                cb(generalDropdown.captionText.text);
                generalDropdown.onValueChanged.RemoveAllListeners();
                generalDropdown.Hide ();
                Cursor.lockState = CursorLockMode.Locked;
            });
            generalDropdown.Show ();
            generalDropdown.Show ();
        }

        private void OpenSpellBook()
        {
            Cursor.lockState = CursorLockMode.None;
            SetCursorPos(10, 10);

            // show available spell dropdown
            // when user chooses one of them - send the event to server
            var availableSpells = new string[] {
                // TODO: get available spells from server
                "DoNothing",
                "MegaJump",
                "Dash",
                "Float",
                "FireBall",
                "Telekinesis",
            };

            AskForChoice (availableSpells, (option) => {
                client.SendMsg(new Msg {
                    type = Msg.EType.CastSpell,
                    strValue = option,
                });
            });
        }

        private List<Msg> makeMessagesFromGuiEvent(Event e)
        {
            var msgs = new List<Msg>();
            if (e.type == EventType.KeyDown) {
                // GetKeyDown - to filter OS key auto-repeat
                if (Input.GetKeyDown(e.keyCode)) {
                    if (e.keyCode == KeyCode.E) {
                        OpenSpellBook();
                    } else if (
                        e.keyCode == KeyCode.Tab ||
                        e.keyCode == KeyCode.Escape
                    ) {
                        Cursor.lockState = CursorLockMode.None;
                    } else {
                        msgs.Add(new Msg{
                            type = Msg.EType.KeyDown,
                            keyCode = e.keyCode,
                        });
                    }
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
                Log("unhandled event - " + e.type + " " + e);
            }
            return msgs;
        }

        void OnGUI()
        {
            if (Cursor.lockState == CursorLockMode.None) {
                return; // cursor not locked - player is doing ui stuff
            }
            var e = Event.current;
            var msgs = makeMessagesFromGuiEvent(e);
            msgs.ForEach((msg) => {
                client.SendMsg(msg);
            });
        }

        void Update ()
        {
            if (Cursor.lockState == CursorLockMode.None) {
                return; // cursor not locked - player is doing ui stuff
            }
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
                client.SendMsg(msg);
            });
        }

    }
}