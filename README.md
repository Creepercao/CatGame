# 喵屋 CatGame

基于 Unity 的休闲放置游戏，支持 Windows 与 Android 横屏。摸猫收集猫毛，购买猫咪与用品，观看成就 CG。购买火箭触发结局后仍可继续游玩。

## 下载与运行

在本仓库Releases下载Windows x64 ZIP，完整解压后运行CatGame.exe。无需安装Unity或Python。可选外置修改器位于Debugger/CatGameDebugger.exe，游戏运行时自动连接；主菜单、对话或暂停时不可修改，进入正常玩法后使用。请勿移动单个EXE而遗漏数据目录。

存档位于Windows用户的AppData/LocalLow/CatGameStudio/喵屋下；重新解压不清除已有进度。修改器改变运行状态，由游戏正常保存。

Android ARM64 APK 见 [Android 预览版](https://github.com/Creepercao/CatGame/releases/tag/android-v1.0.0-preview.1)，附件包含 APK、安装说明和 SHA-256 校验文件。打开 APK 后按系统提示安装，也可用 `adb -s <设备序列号> install -r CatGame-Android.apk`。游戏包名为 `com.catgamestudio.catgame`，同签名的旧版可以覆盖安装。Android 版使用触摸操作，核心玩法离线运行。已在 MuMu、Android 14 模拟器和小米 22081212C / Android 15 完成验证；真机短时性能采样不代表其他设备、耗电表现或长期稳定性。

新版普通保存会保留上一份已提交存档。主档缺失或损坏时会尝试恢复该备份，可能回退最近一轮保存后的进度。

## 开发环境

- Unity 2022.3.21f1。Windows 构建使用 Windows Build Support，Android 构建使用 Android Build Support 和 Unity 配套的 SDK、NDK、JDK。
- 使用Unity Hub打开仓库根目录，等待包解析和资源导入。
- 打开Assets/Scenes/MainMenu.unity后点击Play。
- 选择菜单CatGame → 4. 构建 Windows 版，输出Build/CatGame.exe。
- 切换到 Android 后选择 CatGame → 5. 构建 Android APK，输出 `Build/Android/CatGame.apk`。
- 页面主要由C#与UiKit在运行时创建，游戏场景、素材导入参数和工程设置均保留。

## 目录

| 路径 | 内容 |
|---|---|
| Assets/Scripts | 玩法、存档、UI、经济系统与本机修改器连接支持 |
| Assets/Resources | 运行时加载的猫咪、房间、用品、CG和界面素材 |
| Assets/Scenes | 主菜单、猫窝及公园预览场景 |
| Assets/Editor | 游戏场景生成、素材导入和 Windows / Android 构建入口 |
| Packages / ProjectSettings | Unity依赖与工程配置 |

此仓库只发布游戏工程。自动化测试、测试报告、QA Player脚本、CI编排、素材制作工具及个人学习资料留在本地，不进入发布历史。外置修改器作为可选二进制附于发行包；本机连接服务属于游戏集成支持，不是测试用例。可设置CATGAME_DEBUG_DISABLE=1关闭连接服务。

## 项目与素材说明

个人项目，使用 AI 辅助开发与部分素材制作。仓库未声明统一开源许可证，第三方素材的权利归原作者。Android 中文字体为 Noto Sans SC，随字体附带 SIL Open Font License。
