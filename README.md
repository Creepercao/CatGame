# 喵屋 CatGame

基于Unity的Windows休闲放置游戏。摸猫收集猫毛，购买猫咪与用品，观看成就CG，购买火箭触发结局后可继续游玩。

## 下载与运行

在本仓库Releases下载Windows x64 ZIP，完整解压后运行CatGame.exe。无需安装Unity或Python。可选外置修改器位于Debugger/CatGameDebugger.exe，游戏运行时自动连接；主菜单、对话或暂停时不可修改，进入正常玩法后使用。请勿移动单个EXE而遗漏数据目录。

存档位于Windows用户的AppData/LocalLow/CatGameStudio/喵屋下；重新解压不清除已有进度。修改器改变运行状态，由游戏正常保存。

## 开发环境

- Unity 2022.3.21f1，安装Windows Build Support。
- 使用Unity Hub打开仓库根目录，等待包解析和资源导入。
- 打开Assets/Scenes/MainMenu.unity后点击Play。
- 选择菜单CatGame → 4. 构建 Windows 版，输出Build/CatGame.exe。
- 页面主要由C#与UiKit在运行时创建，游戏场景、素材导入参数和工程设置均保留。

## 目录

| 路径 | 内容 |
|---|---|
| Assets/Scripts | 玩法、存档、UI、经济系统与本机修改器连接支持 |
| Assets/Resources | 运行时加载的猫咪、房间、用品、CG和界面素材 |
| Assets/Scenes | 主菜单、猫窝及公园预览场景 |
| Assets/Editor | 游戏场景生成、素材导入和Windows构建工具 |
| Packages / ProjectSettings | Unity依赖与工程配置 |

此仓库只发布游戏工程。自动化测试、测试报告、QA Player脚本、CI编排、素材制作工具及个人学习资料留在本地，不进入发布历史。外置修改器作为可选二进制附于发行包；本机连接服务属于游戏集成支持，不是测试用例。可设置CATGAME_DEBUG_DISABLE=1关闭连接服务。

## 项目与素材说明

个人项目，使用AI辅助开发与部分素材制作。仓库未声明统一开源许可证；第三方素材的权利归原作者，不应将仓库可访问视作获得全部素材的再分发或商用授权。项目仅提供当前Windows版本，不宣称移动端支持。
