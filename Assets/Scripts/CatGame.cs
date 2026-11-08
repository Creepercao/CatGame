using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGame
{
    /// <summary>
    /// 动画验证版核心：只负责品种表与当前品种。
    /// 金币 / 好感（心情） / 饱食 / 清洁 / 解锁 / 存档 / 离线收益都已移除，
    /// 玩法细节之后再单独讨论。
    /// </summary>
    public class CatGame : MonoBehaviour
    {
        public static CatGame I { get; private set; }

        public List<CatBreed> Breeds { get; private set; }
        public CatBreed Current { get; private set; }

        /// <summary>当前品种发生变化，界面与猫都要重新绑定。</summary>
        public event Action<CatBreed> BreedChanged;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Boot();
        }

        /// <summary>
        /// 加载品种表。Awake 会调用；编辑模式下做离屏预览时也可以手动调用
        /// （AddComponent 在编辑模式不会触发 Awake）。
        /// </summary>
        public void Boot()
        {
            I = this;                     // 编辑模式预览时 Awake 不会跑，单例要在这里兜住
            if (Breeds != null && Breeds.Count > 0) return;
            Breeds = BreedLibrary.LoadAll();
            Current = Breeds.Count > 0 ? Breeds[0] : null;
        }

        public CatBreed Find(string id)
        {
            if (Breeds == null) return null;
            return Breeds.Find(b => b.Id == id);
        }

        /// <summary>切换当前品种；id 不存在返回 false（当前品种不变）。</summary>
        public bool SetActiveBreed(string id)
        {
            var b = Find(id);
            if (b == null) return false;
            if (b == Current) return true;
            Current = b;
            if (BreedChanged != null) BreedChanged(b);
            return true;
        }

        void OnDestroy() { if (I == this) I = null; }
    }
}
