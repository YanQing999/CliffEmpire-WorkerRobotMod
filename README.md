# Cliff Empire - Worker Robot Mod (REMASTER) / 工作机器人 Mod

🤖 **Workers are overrated. Build robots. / 工人会累，机器人不会。**

A BepInEx mod for the **REMASTER** edition of Cliff Empire that adds a worker robot system: robots fill your labor deficit, each one doing the work of **2 workers**.
专为 Cliff Empire **REMASTER** 版制作的 BepInEx mod：机器人补足劳动力缺口，每个机器人顶 **2 个工人**。

---

## 📦 Download / 下载

**→ [Latest Release / 最新版本](https://github.com/YanQing999/CliffEmpire-WorkerRobotMod/releases/latest)** ←

---

## ✨ Features / 功能

| English | 中文 |
|---|---|
| 🤖 **Worker robots** — press `F9` to build, `F10` to remove | 🤖 **工作机器人** — `F9` 制造，`F10` 拆除 |
| ⚡ **2x efficiency** — each robot replaces 2 workers | ⚡ **双倍效率** — 每个机器人顶 2 个工人 |
| 🏭 **Robot Factory unlock** — each factory = 50 robot slots (Steam Workshop building) | 🏭 **机器人工厂解锁** — 每座工厂提供 50 个机器人名额（配合创意工坊建筑）|
| 💾 **Auto save** — robot counts save with your game (`.wrmod`, game saves untouched) | 💾 **自动存档** — 机器人数量随游戏存档保存（独立 `.wrmod` 文件，不碰游戏存档）|
| 🗺️ **Per-scene** — independent per scene; orbital station excluded | 🗺️ **多场景独立** — 各场景独立计数，空间站禁用 |
| 🌏 **Bilingual HUD** — 中文 / English, click to switch | 🌏 **中英双语HUD** — 点击按钮切换 |

> ⚠️ **You need at least one Robot Factory on the scene to press F9 — no factory, no robots! / 注意：场景里必须有机器人工厂才能按 F9 制造机器人——没有工厂就没有机器人！**

---

## 🚀 Install / 安装（一次性，约2分钟）

### English
1. Steam → right-click **Cliff Empire** → Manage → Browse local files → open the **`REMASTER`** folder
2. Extract this zip's contents into the `REMASTER` folder (overwrite if asked)
3. Launch from Steam — first launch takes 1–3 minutes to generate runtime files

Then build a **Robot Factory** (Steam Workshop) and press `F9`. 🤖

### 中文
1. Steam → 右键 **Cliff Empire** → 管理 → 浏览本地文件 → 进入 **`REMASTER`** 文件夹
2. 把压缩包内所有文件解压覆盖到 `REMASTER` 文件夹（提示覆盖就选是）
3. 从 Steam 启动游戏 — 首次启动需 1–3 分钟生成运行库文件

然后在创意工坊订阅 **Robot Factory** 建筑，建造后按 `F9`。🤖

---

## 🗑️ Uninstall / 卸载

Delete from the `REMASTER` folder: `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `BepInEx/`, `dotnet/`. Your saves are untouched.
删除 `REMASTER` 文件夹中的：`winhttp.dll`、`doorstop_config.ini`、`.doorstop_version`、`BepInEx/`、`dotnet/`。游戏存档不受影响。

---

## 🐛 Troubleshooting / 排障

Robots not appearing? Attach your log: `...\REMASTER\BepInEx\LogOutput.log`
机器人不生效？把日志发来：`...\REMASTER\BepInEx\LogOutput.log`

---

## ⚠️ Notes / 说明

- Requires the Steam Workshop building **"Robot Factory - Automated Production"** for the full robot system (the building works standalone as an auto-factory too).
  完整机器人系统需要创意工坊建筑 **「Robot Factory - Automated Production」**（该建筑单独使用也可作为全自动工厂）。
- The mod only affects labor calculation — game saves are never modified.
  Mod 只影响劳动力计算 — 绝不修改游戏存档。
