# WorldClock / Windows 桌面世界时钟小组件

[中文](#中文) | [English](#english)

[![Build](https://github.com/Hanfeng-Lin/WorldClock/actions/workflows/build.yml/badge.svg)](https://github.com/Hanfeng-Lin/WorldClock/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/Hanfeng-Lin/WorldClock)](https://github.com/Hanfeng-Lin/WorldClock/releases)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D4)](#)
[![License](https://img.shields.io/github/license/Hanfeng-Lin/WorldClock)](./LICENSE)

## 中文

WorldClock 是一个 Windows 桌面世界时钟小组件。它会贴在桌面图标层上，按 `Win + D` 显示桌面时不会像普通窗口一样被最小化，同时支持透明度、平滑圆角、双语界面、主题、托盘菜单和 12/24 小时制切换。

### 中文目录

- [功能](#功能)
- [下载](#下载)
- [使用](#使用)
- [配置文件](#配置文件)
- [编译](#编译)
- [发布](#发布)
- [用户体验路线](#用户体验路线)
- [技术说明](#技术说明)
- [English](#english)

### 功能

- 桌面常驻：挂载到桌面图标层，兼顾交互和 `Win + D` 免疫。
- 透明度：右键菜单可切换 100%、92%、85%、75%、60%。
- 圆角：使用 per-pixel alpha 渲染，圆角边缘平滑，没有直角白边。
- 双语：支持简体中文和 English。
- 主题：内置柔夜、石墨、极夜、日间主题。
- 时间制式：支持 24 小时制和 12 小时制。
- 时间换算器：选择基准城市和日期时间，换算到当前城市或全部内置城市。
- 显示选项：支持秒数开关、字体大小和宽度预设。
- 城市管理：支持搜索、排序和 80+ 内置城市/时区。
- 行为选项：支持锁定位置、重置位置和点击穿透。
- 托盘图标：点击穿透后仍可从系统托盘打开菜单。
- 开机自启：右键菜单可开启或关闭。

### 下载

从 [GitHub Releases](https://github.com/Hanfeng-Lin/WorldClock/releases) 下载最新版 zip。

最新版本：

- [v1.0.0](https://github.com/Hanfeng-Lin/WorldClock/releases/tag/v1.0.0)
- [WorldClock-v1.0.0.zip](https://github.com/Hanfeng-Lin/WorldClock/releases/download/v1.0.0/WorldClock-v1.0.0.zip)

### 使用

解压 release zip 后运行：

```powershell
.\WorldClock.exe
```

右键小组件或系统托盘图标可以打开菜单：

- `选择城市...`
- `时间换算器...`
- `语言`
- `主题`
- `时间制式`
- `显示`
- `透明度`
- `行为`
- `开机自启`
- `退出`

拖动小组件任意位置即可移动。退出时会保存位置和设置到 `settings.txt`。

如果开启了 `点击穿透`，小组件本体不会接收鼠标事件，需要从系统托盘图标右键菜单关闭点击穿透或退出。

### 配置文件

`settings.txt` 保存窗口和界面设置：

```text
X=1503
Y=73
Alpha=185
Language=zh
Theme=nord
Use24Hour=True
ShowSeconds=True
LockedPosition=False
ClickThrough=False
FontSize=normal
WidgetWidth=260
```

`cities.txt` 保存城市列表，格式是：

```text
显示名 = Windows 时区 ID
```

示例：

```text
芝加哥 = Central Standard Time
纽约 = Eastern Standard Time
伦敦 = GMT Standard Time
巴黎 = Romance Standard Time
北京 = China Standard Time
```

### 编译

需要 Windows 和 .NET Framework C# 编译器。运行：

```powershell
.\build.ps1
```

CI 或只需要生成 exe 时可以跳过开始菜单快捷方式：

```powershell
.\build.ps1 -NoShortcut
```

脚本会：

1. 生成 `clock.ico`
2. 编译 `WorldClock.exe`
3. 可选创建开始菜单快捷方式

如果编译时报 `WorldClock.exe` 正在被使用，先退出正在运行的小组件，再重新执行构建。

### 发布

本项目使用 [GitHub Actions](https://github.com/Hanfeng-Lin/WorldClock/actions/workflows/build.yml) 自动构建。

- push 到 `main`：编译并上传 artifact。
- push `v*` tag：编译、打包 zip，并发布 GitHub Release。

发布新版本示例：

```powershell
git tag v0.2.1
git push origin v0.2.1
```

建议 release zip 包含：

```text
WorldClock.exe
clock.ico
cities.txt
README.md
LICENSE
CHANGELOG.md
```

### 用户体验路线

已完成到 P2 的主要体验项：

- 城市搜索与排序
- 锁定位置和重置位置
- 秒数开关、字体大小、宽度预设
- 托盘图标
- 点击穿透
- 多显示器位置恢复
- Explorer 重启重挂载
- DPI manifest

后续仍值得考虑：

- 按地区分组城市
- 自定义城市显示名
- 添加任意 Windows 时区 ID
- 主题导入/导出
- 自定义主题颜色
- 更完整的异常日志和日志大小限制
- 跟随系统语言和系统日期格式

完整待办见 [TODO.md](./TODO.md)。

### 技术说明

WorldClock 使用了几项 Windows 桌面相关能力：

- `SetParent` 挂载到桌面图标层，避免 `Win + D` 最小化。
- `WS_EX_LAYERED` 保持透明能力。
- `UpdateLayeredWindow` 使用 32-bit ARGB 位图实现平滑圆角。
- manifest 声明 Windows 8+ 兼容性，使 child layered window 透明表现稳定。

这些 API 依赖 Windows Explorer 的桌面窗口结构。当前方案在 Windows 10/11 上可用，但 Windows 大版本更新后仍建议重新验证。

[回到顶部](#worldclock) | [English](#english)

---

## English

WorldClock is a Windows desktop world clock widget. It stays on the desktop icon layer, survives `Win + D`, and supports opacity, smooth rounded corners, bilingual UI, themes, tray menu actions, and 12/24-hour time format switching.

### Table Of Contents

- [Features](#features)
- [Download](#download)
- [Usage](#usage)
- [Configuration](#configuration)
- [Build](#build)
- [Release](#release)
- [UX Roadmap](#ux-roadmap)
- [Technical Notes](#technical-notes)
- [中文](#中文)

### Features

- Desktop resident: attaches to the desktop icon layer and survives `Win + D`.
- Opacity: switch between 100%, 92%, 85%, 75%, and 60%.
- Rounded corners: rendered with per-pixel alpha for smooth edges.
- Bilingual UI: Simplified Chinese and English.
- Themes: Catppuccin, Graphite, Nord, and Daylight.
- Time format: 24-hour and 12-hour modes.
- Time converter: choose a base city and date/time, then convert to selected cities or all built-in cities.
- Display options: seconds toggle, font size presets, and width presets.
- City management: search, ordering, and 80+ built-in cities/time zones.
- Behavior options: lock position, reset position, and click-through.
- Tray icon: keeps the menu accessible when click-through is enabled.
- Startup: enable or disable Windows startup from the context menu.

### Download

Download the latest zip from [GitHub Releases](https://github.com/Hanfeng-Lin/WorldClock/releases).

Latest release:

- [v1.0.0](https://github.com/Hanfeng-Lin/WorldClock/releases/tag/v1.0.0)
- [WorldClock-v1.0.0.zip](https://github.com/Hanfeng-Lin/WorldClock/releases/download/v1.0.0/WorldClock-v1.0.0.zip)

### Usage

Extract the release zip and run:

```powershell
.\WorldClock.exe
```

Right-click the widget or tray icon to open the menu:

- `Choose Cities...`
- `Time Converter...`
- `Language`
- `Theme`
- `Time Format`
- `Display`
- `Opacity`
- `Behavior`
- `Start with Windows`
- `Exit`

Drag the widget to move it. Position and settings are saved to `settings.txt` on exit.

When `Click Through` is enabled, the widget itself will not receive mouse events. Use the tray icon menu to disable click-through or exit.

### Configuration

`settings.txt` stores window and UI settings:

```text
X=1503
Y=73
Alpha=185
Language=en
Theme=nord
Use24Hour=True
ShowSeconds=True
LockedPosition=False
ClickThrough=False
FontSize=normal
WidgetWidth=260
```

`cities.txt` stores the city list:

```text
Display name = Windows time zone ID
```

Example:

```text
Chicago = Central Standard Time
New York = Eastern Standard Time
London = GMT Standard Time
Paris = Romance Standard Time
Beijing = China Standard Time
```

### Build

Requires Windows and the .NET Framework C# compiler. Run:

```powershell
.\build.ps1
```

For CI or exe-only builds, skip Start Menu shortcut creation:

```powershell
.\build.ps1 -NoShortcut
```

The script will:

1. Generate `clock.ico`
2. Build `WorldClock.exe`
3. Optionally create a Start Menu shortcut

If the build fails because `WorldClock.exe` is in use, exit the running widget and build again.

### Release

This project uses [GitHub Actions](https://github.com/Hanfeng-Lin/WorldClock/actions/workflows/build.yml) for automated builds.

- Push to `main`: build and upload an artifact.
- Push a `v*` tag: build, package zip, and publish a GitHub Release.

Example:

```powershell
git tag v0.2.1
git push origin v0.2.1
```

Recommended release zip contents:

```text
WorldClock.exe
clock.ico
cities.txt
README.md
LICENSE
CHANGELOG.md
```

### UX Roadmap

Completed P2 UX work:

- City search and ordering
- Lock position and reset position
- Seconds toggle, font size presets, and width presets
- Tray icon
- Click-through mode
- Multi-monitor position recovery
- Explorer restart repinning
- DPI manifest

Future ideas:

- Region-based city grouping
- Custom city display names
- Add arbitrary Windows time zone IDs
- Theme import/export
- Custom theme colors
- Better exception logs with size limits
- Follow system language and date format

See [TODO.md](./TODO.md) for the full list.

### Technical Notes

WorldClock uses several Windows desktop APIs:

- `SetParent` attaches the widget to the desktop icon layer and prevents `Win + D` minimization.
- `WS_EX_LAYERED` keeps transparency support.
- `UpdateLayeredWindow` renders smooth rounded corners with a 32-bit ARGB bitmap.
- The manifest declares Windows 8+ compatibility so child layered window transparency behaves reliably.

These APIs depend on the Windows Explorer desktop window structure. The current approach works on Windows 10/11, but major Windows updates should still be re-tested.

[Back to top](#worldclock) | [中文](#中文)
