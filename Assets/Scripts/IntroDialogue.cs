using CatGame.Idle;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame
{
    public sealed class IntroDialogue : MonoBehaviour
    {
        static readonly string[] Lines = {
            "喵，你终于来啦！我是墨墨，以后这里就是我们的猫窝啦。",
            "摸摸我吧，每次摸摸，都会掉一点猫毛！",
            "就算你不摸我，我在猫窝里散散步，也会慢慢攒下猫毛。",
            "攒下的猫毛可以请更多伙伴回来，也能添置舒服的猫窝用品。",
            "一起把这里变成热热闹闹的家吧！"
        };
        RectTransform _parent;
        GameState _state;
        ModalGate _gate;
        GameObject _overlay;
        Text _text, _progress;
        Button _next;
        int _line;
        public bool IsOpen { get { return _overlay != null; } }
        public int LineIndex { get { return _line; } }
        public void Initialize(RectTransform parent,GameState state,ModalGate gate)
        {
            _parent=parent; _state=state; _gate=gate;
            TryOpen();
        }
        void Update()
        {
            if (!IsOpen) { TryOpen(); return; }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Advance();
        }
        void TryOpen()
        {
            if (_state == null || _state.introCompleted || IsOpen || _gate == null || !_gate.TryAcquire(this)) return;
            _line=0;
            var dim=UiKit.Panel(_parent,"IntroDialogue",new Color(.10f,.07f,.06f,.22f),false);
            _overlay=dim.gameObject; dim.rectTransform.Stretch();
            var portrait=UiKit.Node("Portrait",dim.transform).Stretch().gameObject.AddComponent<Image>();
            portrait.rectTransform.anchorMin=new Vector2(0,.14f); portrait.rectTransform.anchorMax=new Vector2(.42f,.98f);
            portrait.rectTransform.offsetMin=new Vector2(28,0); portrait.rectTransform.offsetMax=new Vector2(-20,-20);
            portrait.sprite=CatVisualLibrary.Load(CatCatalog.All[0]).Portrait;
            portrait.preserveAspect=true; portrait.raycastTarget=false;
            portrait.gameObject.AddComponent<PixelDialoguePortrait>();
            var box=UiKit.Panel(dim.transform,"DialogueBox",new Color(.99f,.96f,.91f,.98f));
            box.rectTransform.anchorMin=Vector2.zero; box.rectTransform.anchorMax=new Vector2(1,0);
            box.rectTransform.pivot=new Vector2(.5f,0); box.rectTransform.offsetMin=new Vector2(32,24); box.rectTransform.offsetMax=new Vector2(-32,324);
            var name=UiKit.Label(box.transform,"Speaker","墨墨",32,UiKit.Ink,TextAnchor.MiddleLeft,FontStyle.Bold);
            name.rectTransform.At(new Vector2(0,1),new Vector2(0,1),new Vector2(28,-14),new Vector2(300,46));
            _text=UiKit.Label(box.transform,"DialogueText","",30,UiKit.Ink,TextAnchor.UpperLeft);
            _text.rectTransform.anchorMin=new Vector2(0,1); _text.rectTransform.anchorMax=Vector2.one;
            _text.rectTransform.pivot=new Vector2(.5f,1); _text.rectTransform.anchoredPosition=new Vector2(0,-76);
            _text.rectTransform.sizeDelta=new Vector2(-56,132);
            _text.horizontalOverflow=HorizontalWrapMode.Wrap;
            _text.verticalOverflow=VerticalWrapMode.Truncate;
            _progress=UiKit.Label(box.transform,"Progress","",18,UiKit.InkSoft,TextAnchor.MiddleLeft);
            _progress.rectTransform.At(Vector2.zero,Vector2.zero,new Vector2(28,24),new Vector2(200,40));
            var skip=UiKit.Btn(box.transform,"Btn_skip_intro","跳过引导",new Vector2(180,56),UiKit.PanelDark,UiKit.Ink,22,Skip);
            skip.GetComponent<RectTransform>().At(new Vector2(1,0),new Vector2(1,0),new Vector2(-256,20),new Vector2(180,56));
            _next=UiKit.Btn(box.transform,"Btn_next_intro","继续",new Vector2(200,56),UiKit.Coral,UiKit.Cream,24,Advance);
            _next.GetComponent<RectTransform>().At(new Vector2(1,0),new Vector2(1,0),new Vector2(-28,20),new Vector2(200,56));
            skip.navigation=new Navigation{mode=Navigation.Mode.None};
            _next.navigation=new Navigation{mode=Navigation.Mode.None};
            RefreshLine();
        }
        void RefreshLine()
        {
            _text.text=Lines[_line]; _progress.text=(_line+1)+" / "+Lines.Length;
            _next.GetComponentInChildren<Text>().text=_line==Lines.Length-1 ? "开始撸猫" : "继续";
        }
        public void Advance()
        {
            if (!IsOpen) return;
            if (++_line>=Lines.Length) { Complete(); return; }
            RefreshLine();
        }
        public void Skip() { if (IsOpen) Complete(); }
        void Complete()
        {
            _state.introCompleted=true;
            var save=SaveSystem.Instance;
            if (save != null && ReferenceEquals(save.State,_state)) { save.MarkDirty(); save.SaveNow(); }
            Destroy(_overlay); _overlay=null; _gate.Release(this);
        }
        void OnDestroy() { if (_overlay != null) Destroy(_overlay); if (_gate != null) _gate.Release(this); }
    }
}
