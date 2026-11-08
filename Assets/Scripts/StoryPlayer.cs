using System;
using CatGame.Idle;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame
{
    /// <summary>Owns one CG modal; a visible event stays pending until explicitly dismissed.</summary>
    public sealed class StoryPlayer : MonoBehaviour
    {
        RectTransform _root;
        GameState _state;
        ModalGate _gate;
        GameObject _overlay;
        Action _goMenu;
        int _pointerFrame=-1;
        public bool ShowingStatistics { get; private set; }
        public const string EndingId="rocket_ending";
        public string CurrentId { get; private set; }
        public bool IsOpen { get { return _overlay!=null; } }
        public double LastOpenMilliseconds { get; private set; }
        public void Initialize(RectTransform root,GameState state,ModalGate gate,Action goMenu=null)
        { _root=root; _state=state; _gate=gate; _goMenu=goMenu??SceneFlow.GoMenu; }
        void Update()
        {
            if(_state==null || IsOpen || !_state.introCompleted || Time.frameCount==_pointerFrame) return;
            if(StoryCatalog.EnqueueUnlocked(_state)) Persist();
            if(_gate.IsBusy) return;
            bool ending=_state.rocketPurchased && !_state.endingCompleted;
            if(!ending && _state.pendingStoryEvents.Count==0) return;
            var story=ending ? new StoryDefinition(EndingId,"向星星出发","无论走多远，这里永远是猫猫的家。","EndingRocket_v1") : StoryCatalog.Find(_state.pendingStoryEvents[0]);
            if(story==null) return; // Never silently consume an unsupported saved event.
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            var sprite=Resources.Load<Sprite>(story.resource);
            if(sprite==null || !_gate.TryAcquire(this)) return;
            CurrentId=story.id;
            var dim=UiKit.Panel(_root,"StoryCG",new Color(.08f,.06f,.05f,.98f),false);
            dim.rectTransform.Stretch(); _overlay=dim.gameObject;
            var anywhere=dim.gameObject.AddComponent<Button>();
            anywhere.transition=Selectable.Transition.None; anywhere.navigation=new Navigation{mode=Navigation.Mode.None};
            anywhere.onClick.AddListener(CompleteFromPointer);
            var frame=UiKit.Node("Art",dim.transform);
            frame.anchorMin=new Vector2(0,.23f); frame.anchorMax=Vector2.one;
            frame.offsetMin=new Vector2(24,8); frame.offsetMax=new Vector2(-24,-24);
            var art=frame.gameObject.AddComponent<Image>(); art.sprite=sprite; art.preserveAspect=true; art.raycastTarget=false;
            var title=UiKit.Label(dim.transform,"Title",story.title,32,UiKit.Cream,TextAnchor.MiddleCenter,FontStyle.Bold);
            title.rectTransform.anchorMin=new Vector2(0,.16f); title.rectTransform.anchorMax=new Vector2(1,.22f);
            title.rectTransform.offsetMin=new Vector2(24,0); title.rectTransform.offsetMax=new Vector2(-24,0);
            var caption=UiKit.Label(dim.transform,"Caption",story.caption,24,UiKit.Cream,TextAnchor.MiddleCenter);
            caption.rectTransform.anchorMin=new Vector2(0,.10f); caption.rectTransform.anchorMax=new Vector2(1,.16f);
            caption.rectTransform.offsetMin=new Vector2(24,0); caption.rectTransform.offsetMax=new Vector2(-24,0);
            var next=UiKit.Btn(dim.transform,"Btn_close_CG",ending?"点击任意处查看旅程":"点击任意处继续",new Vector2(360,60),UiKit.Coral,UiKit.Cream,24,CompleteFromPointer);
            next.GetComponent<RectTransform>().At(new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,20),new Vector2(360,60));
            next.navigation=new Navigation{mode=Navigation.Mode.None};
            LastOpenMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        }
        public void CompleteFromPointer()
        {
            if(Time.frameCount==_pointerFrame || !IsOpen || ShowingStatistics) return;
            _pointerFrame=Time.frameCount; _gate.SuppressGameplayThisFrame(); Complete();
        }
        public void Complete()
        {
            if(!IsOpen) return;
            if(CurrentId==EndingId) { if(!ShowingStatistics) ShowStatistics(); return; }
            StoryCatalog.Complete(_state,CurrentId); Persist();
            Destroy(_overlay); _overlay=null; CurrentId=null; _gate.Release(this);
        }
        void ShowStatistics()
        {
            Destroy(_overlay);
            ShowingStatistics=true;
            var dim=UiKit.Panel(_root,"EndingStatistics",new Color(.08f,.06f,.05f,.88f),false);
            dim.rectTransform.Stretch(); _overlay=dim.gameObject;
            var card=UiKit.Panel(dim.transform,"Card",UiKit.Cream);
            card.rectTransform.At(new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1120,820));
            var heading=UiKit.Label(card.transform,"Thanks","感谢游玩",48,UiKit.Ink,TextAnchor.MiddleCenter,FontStyle.Bold);
            heading.rectTransform.At(new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-38),new Vector2(1040,72));
            var subtitle=UiKit.Label(card.transform,"Subtitle","这是我们的第一次星际旅行，谢谢你的每一次摸摸。",26,UiKit.InkSoft,TextAnchor.MiddleCenter);
            subtitle.rectTransform.At(new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-124),new Vector2(1040,56));
            var details=UiKit.Label(card.transform,"Details",StatisticsText(_state.firstEnding),29,UiKit.Ink,TextAnchor.MiddleLeft);
            details.rectTransform.At(new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-210),new Vector2(900,420));
            var keep=UiKit.Btn(card.transform,"Btn_continue_ending","继续游玩",new Vector2(320,66),UiKit.Coral,UiKit.Cream,26,()=>FinishEnding(false));
            keep.GetComponent<RectTransform>().At(new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-182,50),new Vector2(320,66));
            var menu=UiKit.Btn(card.transform,"Btn_menu_ending","返回主菜单",new Vector2(320,66),UiKit.PanelDark,UiKit.Ink,26,()=>FinishEnding(true));
            menu.GetComponent<RectTransform>().At(new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(182,50),new Vector2(320,66));
            keep.navigation=menu.navigation=new Navigation{mode=Navigation.Mode.None};
        }
        public static string StatisticsText(EndingStatistics s)
        {
            int hours=(int)(s.playSeconds/3600), minutes=(int)(s.playSeconds/60)%60, seconds=(int)s.playSeconds%60;
            string duration=hours+" 小时 "+minutes+" 分 "+seconds+" 秒";
            return "首次出发时的记录\n\n"+(s.hasFullPlaytimeHistory?"有效游玩：":"升级后有效游玩（旧档此前未记录）：")+duration
                +"\n累计猫毛："+Hud.FormatNumber(s.totalEarned)+"\n摸猫次数："+s.clicks
                +"\n猫咪伙伴："+s.totalCats+" 只 · "+s.breeds+" / "+CatCatalog.All.Length+" 个品种"
                +"\n用品等级："+s.itemLevels+" / "+(ItemCatalog.All.Length*ItemCatalog.LevelsPerItem)
                +"\n成就："+s.achievementCount+" / "+AchievementCatalog.All.Length;
        }
        public void FinishEnding(bool goMenu)
        {
            if(!IsOpen || CurrentId!=EndingId || !ShowingStatistics) return;
            _state.endingCompleted=true; Persist();
            Destroy(_overlay); _overlay=null; ShowingStatistics=false; CurrentId=null; _gate.Release(this);
            if(goMenu) _goMenu();
        }
        void Persist()
        {
            var save=SaveSystem.Instance;
            if(save!=null && ReferenceEquals(save.State,_state)) { save.MarkDirty(); save.SaveNow(); }
        }
        void OnDestroy()
        { if(_overlay!=null) Destroy(_overlay); if(_gate!=null) _gate.Release(this); }
    }
}
