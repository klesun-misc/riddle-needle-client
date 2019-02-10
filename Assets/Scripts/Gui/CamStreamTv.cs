using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gui
{
    public class CamStreamTv : MonoBehaviour, IPointerClickHandler
    {
        public RawImage tv;

        private Texture2D tvTex;

        private void Start()
        {
            tvTex = new Texture2D(0, 0);
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void SetFrameImgBytes(byte[] bytes)
        {
            tvTex.LoadImage(bytes);
            tv.texture = tvTex;
        }
    }
}