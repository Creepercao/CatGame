using UnityEngine;
using UnityEngine.UI;
namespace CatGame
{
    // Explicit category jumps only: ordinary scrolling never snaps or hides partial cards.
    public sealed class ShopScrollRect : ScrollRect
    {
        protected override void Start()
        {
            base.Start();
            // Preserve card groups for existing UI integrations, with no visibility filtering.
            foreach(var button in content.GetComponentsInChildren<Button>(true))
                if(button.GetComponent<CanvasGroup>()==null) button.gameObject.AddComponent<CanvasGroup>();
        }
        public void Focus(RectTransform row)
        {
            Canvas.ForceUpdateCanvases(); StopMovement();
            float top=-row.anchoredPosition.y-(1-row.pivot.y)*row.rect.height;
            content.anchoredPosition=new Vector2(0,Mathf.Clamp(top-12,0,Mathf.Max(0,content.rect.height-viewport.rect.height)));
        }
    }
}
