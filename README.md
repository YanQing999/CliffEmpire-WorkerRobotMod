# Cliff Empire 工作机器人 Mod（REMASTER 版）/ Worker Robot Mod (REMASTER)

🤖 **工人会累，机器人不会。** / Workers are overrated. Build robots.

专为 Cliff Empire **REMASTER** 版制作的机器人系统 Mod：机器人补足城市劳动力缺口，每个机器人顶 **2 个工人**，让你的城市满负荷生产。
A BepInEx mod for the **REMASTER** edition of Cliff Empire: robots fill your labor deficit, each doing the work of **2 workers**.

---

## 📦 下载 / Download

**→ [最新版本 / Latest Release](https://github.com/YanQing999/CliffEmpire-WorkerRobotMod/releases/latest)** ←

---

## ✨ 功能 / Features

| 功能 | 说明 |
|---|---|
| 🤖 **工作机器人** | `F9` 制造（800 钱/台），`F10` 拆除 |
| ⚡ **双倍效率** | 每个机器人顶 2 个工人 |
| 🏭 **机器人工厂解锁** | 每座工厂提供 50 个机器人名额（需配合创意工坊建筑「Robot Factory」）|
| 💾 **自动存档** | 机器人数量随游戏保存（独立 `.wrmod` 文件，不碰游戏存档）|
| 🗺️ **多场景独立** | 各场景独立计数，空间站禁用 |
| 🌏 **中英双语HUD** | 点击按钮切换语言 |

> ⚠️ **注意：场景里必须有机器人工厂才能按 F9 制造机器人——没有工厂就没有机器人！** / You need at least one Robot Factory on the scene to press F9 — no factory, no robots!

---

## 🚀 安装 / Install（一次性，约2分钟）

**中文：**
1. Steam → 右键 **Cliff Empire** → 管理 → 浏览本地文件 → 进入 **`REMASTER`** 文件夹
2. 把压缩包内所有文件解压覆盖到 `REMASTER` 文件夹（提示覆盖就选"是"）
3. 从 Steam 启动游戏 — 首次启动需 1–3 分钟生成运行库文件

然后在创意工坊订阅 **Robot Factory** 建筑，建造后按 `F9` 开始造机器人。🤖

**English:**
1. Steam → right-click **Cliff Empire** → Manage → Browse local files → open the **`REMASTER`** folder
2. Extract this zip's contents into the `REMASTER` folder (overwrite if asked)
3. Launch from Steam — first launch takes 1–3 minutes to generate runtime files

Then build a **Robot Factory** (Steam Workshop) and press `F9`. 🤖

---

## 🛠️ 源码 / Build from Source

BepInEx 6 (IL2CPP) 插件源码在本仓库 `WorkerRobotMod_REMASTER/` 目录：
- `Plugin.cs` — 全部插件源码 / all plugin source
- `WorkerRobotMod_REMASTER.csproj` — 编译配置（HintPath 指向游戏 BepInEx 目录，按需改为你自己的路径）

**编译 / Build:**
1. 装好 .NET 6 SDK，且游戏已安装 BepInEx（BE.788+）
2. 改 csproj 里的 `HintPath` 指向你本机的游戏目录
3. `dotnet build -c Release` → 产物：`bin/Release/net6.0/WorkerRobotMod_REMASTER.dll`
4. 放入 `REMASTER/BepInEx/plugins/` 即生效

---

## 🗑️ 卸载 / Uninstall

删除 `REMASTER` 文件夹中的：`winhttp.dll`、`doorstop_config.ini`、`.doorstop_version`、`BepInEx/`、`dotnet/`。游戏存档完全不受影响。
Delete from the `REMASTER` folder: `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `BepInEx/`, `dotnet/`. Your saves are untouched.

---

## 🐛 排障 / Troubleshooting

机器人不生效？把日志发来：`...\REMASTER\BepInEx\LogOutput.log`
Robots not working? Attach your log: `...\REMASTER\BepInEx\LogOutput.log`

---

## ⚠️ 说明 / Notes

- 完整机器人系统需要创意工坊建筑 **「机器人工厂 - 提升劳动力」**（该建筑单独使用也可提供 40 居住空间与电池产出，无需本 Mod）
  The full robot system requires the Steam Workshop building **"Robot Factory - Labor Booster"** (the building also works standalone, providing 40 housing and battery production, without this mod).
- 本 Mod 只影响劳动力计算 — 绝不修改游戏存档
  This mod only affects labor calculation — game saves are never modified.
- 由 [YanQing999](https://github.com/YanQing999) 制作，感谢游玩！🎮
  Made by [YanQing999](https://github.com/YanQing999). Enjoy! 🎮
