using CatGame.Idle;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame
{
    public sealed class RocketPurchase : MonoBehaviour
    {
        IdleGameController _economy;
        ModalGate _gate;
        RectTransform _root;
        GameObject _overlay;
        public bool IsOpen { get { return _overlay!=null; } }
        public void Initialize(RectTransform root,IdleGameController economy,ModalGate gate)
        { _root=root; _economy=economy; _gate=gate; }
        public bool Open()
        {
            if(_economy==null || _economy.State.rocketPurchased || IsOpen || !_gate.TryAcquire(this)) return false;
            var dim=UiKit.Panel(_root,"RocketConfirmation",new Color(.08f,.06f,.05f,.65f),false);
            dim.rectTransform.Stretch(); _overlay=dim.gameObject;
            var card=UiKit.Panel(dim.transform,"Card",UiKit.Cream);
            card.rectTransform.At(new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(760,380));
            var title=UiKit.Label(card.transform,"Title","向星星出发",34,UiKit.Ink,TextAnchor.MiddleCenter,FontStyle.Bold);
            title.rectTransform.At(new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-24),new Vector2(700,56));
            var message=UiKit.Label(card.transform,"Message","花费 "+Hud.FormatNumber(EndingRules.RocketPrice)+" 猫毛购买火箭。\n发射后将进入结局，之后仍可继续游玩。",26,UiKit.Ink,TextAnchor.MiddleCenter);
            message.rectTransform.At(new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,8),new Vector2(700,140));
            var cancel=UiKit.Btn(card.transform,"Btn_cancel_rocket","再陪猫猫一会",new Vector2(280,60),UiKit.PanelDark,UiKit.Ink,24,Cancel);
            cancel.GetComponent<RectTransform>().At(new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-160,28),new Vector2(280,60));
            var confirm=UiKit.Btn(card.transform,"Btn_confirm_rocket","购买并出发",new Vector2(280,60),UiKit.Coral,UiKit.Cream,24,Confirm);
            confirm.GetComponent<RectTransform>().At(new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(160,28),new Vector2(280,60));
            cancel.navigation=confirm.navigation=new Navigation{mode=Navigation.Mode.None};
            return true;
        }
        public void Confirm()
        {
            if(!IsOpen) return;
            // No yield between releasing our confirmation and validating the transaction.
            Close(); _economy.BuyRocket();
        }
        public void Cancel() { if(IsOpen) Close(); }
        void Close() { Destroy(_overlay); _overlay=null; _gate.Release(this); }
        void OnDestroy() { if(_overlay!=null) Destroy(_overlay); if(_gate!=null) _gate.Release(this); }
    }
}
