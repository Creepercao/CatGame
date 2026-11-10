using UnityEngine;
using UnityEngine.UI;
namespace CatGame
{
    public static class GlobalSettings
    {
        public static int NumberFormat { get { return Mathf.Clamp(PlayerPrefs.GetInt("CatGame.NumberFormat",0),0,2); } }
        public static float Volume { get { return Mathf.Clamp01(PlayerPrefs.GetFloat("CatGame.Volume",1)); } }
        public static readonly Vector2Int[] Resolutions={new Vector2Int(1024,768),new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080)};
        public static string FormatLabel { get { return "数字格式："+(NumberFormat==1?"K / M / B":NumberFormat==2?"科学计数（1亿起）":"自动（1亿起科学计数）"); } }
        public static void CycleFormat() { PlayerPrefs.SetInt("CatGame.NumberFormat",(NumberFormat+1)%3); PlayerPrefs.Save(); if(CatWorld.I!=null && CatWorld.I.Hud!=null) CatWorld.I.Hud.RefreshDisplay(); }
        public static void SetVolume(float value) { if(float.IsNaN(value)) return; AudioListener.volume=Mathf.Clamp01(value); PlayerPrefs.SetFloat("CatGame.Volume",AudioListener.volume); PlayerPrefs.Save(); }
        public static void SetResolution(int width,int height)
        {
            if(width<640 || height<480 || width>7680 || height>4320) throw new System.ArgumentOutOfRangeException("resolution");
            PlayerPrefs.SetInt("CatGame.Width",width); PlayerPrefs.SetInt("CatGame.Height",height); PlayerPrefs.Save(); Screen.SetResolution(width,height,FullScreenMode.Windowed);
        }
        public static void ApplySaved()
        {
            AudioListener.volume=Volume;
            if (Application.isMobilePlatform) return;
            int w=PlayerPrefs.GetInt("CatGame.Width",0),h=PlayerPrefs.GetInt("CatGame.Height",0);
            if(w>=640 && h>=480 && w<=7680 && h<=4320) Screen.SetResolution(w,h,FullScreenMode.Windowed);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap() { ApplySaved(); }
    }
    public sealed class GlobalSettingsControls : MonoBehaviour
    {
        Text _format,_resolution; Slider _volume;
        public static void Attach(Transform parent,Text format,Slider volume,Vector2 anchor,Vector2 position)
        {
            var c=parent.gameObject.AddComponent<GlobalSettingsControls>(); c._format=format; c._volume=volume;
            if (Application.isMobilePlatform) { c.Sync(); return; }
            var b=UiKit.Btn(parent,"Btn_resolution","",new Vector2(420,54),UiKit.PanelC,UiKit.Ink,20,c.CycleResolution);
            b.GetComponent<RectTransform>().At(anchor,anchor,position,new Vector2(420,54)); c._resolution=b.GetComponentInChildren<Text>(); c.Sync();
        }
        void CycleResolution()
        {
            int i=System.Array.FindIndex(GlobalSettings.Resolutions,r=>r.x==Screen.width && r.y==Screen.height);
            var next=GlobalSettings.Resolutions[(i+1)%GlobalSettings.Resolutions.Length]; GlobalSettings.SetResolution(next.x,next.y); Sync();
        }
        void Update() { Sync(); }
        void Sync()
        {
            if(_format!=null) _format.text=GlobalSettings.FormatLabel;
            if(_volume!=null) _volume.SetValueWithoutNotify(GlobalSettings.Volume);
            if(_resolution!=null) _resolution.text="窗口分辨率："+Screen.width+" × "+Screen.height+"  ›";
        }
    }
}
