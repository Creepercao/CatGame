using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGame
{
    /// <summary>一个品种的七套帧动画。</summary>
    public class CatBreed
    {
        public string Id;
        public string Name;
        public Sprite[] Idle;
        public Sprite[] Walk;
        public Sprite[] Run;
        public Sprite[] Jump;
        public Sprite[] Sleep;
        public Sprite[] Eat;
        public Sprite[] Attack;
        // New complete animation sets may opt into Walk during automatic roaming.
        public bool UseWalkWhileMoving;
        public bool UseSleepWhileResting;

        public Sprite Portrait { get { return Idle != null && Idle.Length > 0 ? Idle[0] : null; } }

        /// <summary>按动作名取帧数组；没有对应资源时返回 null。</summary>
        public Sprite[] Clip(string action)
        {
            switch (action)
            {
                case ActionNames.Idle: return Idle;
                case ActionNames.Walk: return Walk;
                case ActionNames.Run: return Run;
                case ActionNames.Jump: return Jump;
                case ActionNames.Sleep: return Sleep;
                case ActionNames.Eat: return Eat;
                case ActionNames.Attack: return Attack;
                default: return null;
            }
        }
    }

    /// <summary>动作全集：UI 按钮、快捷键、测试共用同一份定义。</summary>
    public static class ActionNames
    {
        public const string Idle = "idle";
        public const string Walk = "walk";
        public const string Run = "run";
        public const string Jump = "jump";
        public const string Sleep = "sleep";
        public const string Eat = "eat";
        public const string Attack = "attack";

        public static readonly string[] All = { Idle, Walk, Run, Jump, Sleep, Eat, Attack };
        public static readonly string[] Labels = { "待机", "走路", "奔跑", "跳跃", "睡觉", "吃饭", "挥爪" };

        public static string Label(string action)
        {
            for (int i = 0; i < All.Length; i++) if (All[i] == action) return Labels[i];
            return action;
        }

        public static bool IsValid(string action) { return Array.IndexOf(All, action) >= 0; }
    }

    public static class BreedLibrary
    {
        // id 必须与 Resources/Art/Cats 下的目录名一致
        static readonly string[,] Defs =
        {
            // id,       中文名
            { "Milk",    "墨墨" },
            { "Tabby",   "小棕" },
            { "Snow",    "雪球" },
        };

        public static List<CatBreed> LoadAll()
        {
            var list = new List<CatBreed>();
            for (int i = 0; i < Defs.GetLength(0); i++)
            {
                var id = Defs[i, 0];
                if (CatVisualLibrary.UsesPixel64Art(id))
                {
                    list.Add(CatVisualLibrary.Load(Array.Find(Idle.CatCatalog.All, definition => definition.id == id)));
                    continue;
                }
                var breed = new CatBreed
                {
                    Id = id,
                    Name = Defs[i, 1],
                    Idle = LoadFrames(id, "Idle"),
                    Walk = LoadFrames(id, "Walk"),
                    Run = LoadFrames(id, "Run"),
                    Jump = LoadFrames(id, "Jump"),
                    Sleep = LoadFrames(id, "Sleep"),
                    Eat = LoadFrames(id, "Eat"),
                    Attack = LoadFrames(id, "Attack"),
                };
                if (breed.Idle == null || breed.Idle.Length == 0)
                {
                    Debug.LogWarning("[CatGame] 缺少动画资源: Cats/" + id + "/Idle");
                    continue;
                }
                list.Add(breed);
            }
            return list;
        }

        static Sprite[] LoadFrames(string id, string clip)
        {
            var path = "Art/Cats/" + id + "/" + clip;
            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites == null || sprites.Length == 0) return null;
            Array.Sort(sprites, (a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
            return sprites;
        }

        static int FrameIndex(string name)
        {
            int i = name.LastIndexOf('_');
            if (i < 0) return 0;
            int n;
            return int.TryParse(name.Substring(i + 1), out n) ? n : 0;
        }
    }
}
