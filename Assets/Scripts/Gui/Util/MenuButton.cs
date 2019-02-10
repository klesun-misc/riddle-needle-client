using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Gui.Util
{
    public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

        public Text theText;

        public void OnPointerEnter(PointerEventData eventData)
        {
            theText.color = Color.blue; //Or however you do your color
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            theText.color = Color.black; //Or however you do your color
        }
    }
}