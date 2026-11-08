using UnityEngine;
using UnityEngine.UI;

namespace CatGame
{
    /// <summary>Display the existing cat sprite at an integer number of screen pixels per source pixel.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class PixelDialoguePortrait : MonoBehaviour
    {
        Image _image;
        void LateUpdate()
        {
            if (_image == null) _image=GetComponent<Image>();
            if (_image.sprite == null) return;
            var rect=_image.rectTransform;
            var parent=(RectTransform)rect.parent;
            float canvasScale=Mathf.Max(.0001f,Mathf.Abs(parent.lossyScale.x));
            float availableWidth=Mathf.Max(1,parent.rect.width*.42f-48)*canvasScale;
            const float bottom = 340f; // Dialogue box ends at local y=324; keep the complete frame above it.
            float availableHeight=Mathf.Max(1,parent.rect.height-bottom-24)*canvasScale;
            var size=_image.sprite.rect.size;
            int zoom=Mathf.Max(1,Mathf.FloorToInt(Mathf.Min(availableWidth/size.x,availableHeight/size.y)));
            rect.anchorMin=rect.anchorMax=Vector2.zero;
            rect.pivot=new Vector2(.5f,0);
            rect.sizeDelta=size*(zoom/canvasScale);
            rect.anchoredPosition=new Vector2(Mathf.Round(parent.rect.width*.21f*canvasScale)/canvasScale,
                Mathf.Round(bottom*canvasScale)/canvasScale);
        }
    }
}
