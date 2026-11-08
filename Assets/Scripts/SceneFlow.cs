using UnityEngine.SceneManagement;

namespace CatGame
{
    /// <summary>运行场景入口。只有主菜单与猫窝会进入 Build Settings；公园图鉴使用独立背景图。</summary>
    public static class SceneFlow
    {
        public const string Menu = "MainMenu";
        public const string Home = "Home";   // 猫窝

        public static void GoTo(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            SceneManager.LoadScene(sceneName);
        }

        public static void GoMenu() { GoTo(Menu); }
        public static void GoHome() { GoTo(Home); }

        public static string Current
        {
            get { return SceneManager.GetActiveScene().name; }
        }
    }
}
