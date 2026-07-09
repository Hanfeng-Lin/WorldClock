# WorldClock

一个 Windows 桌面世界时钟小组件。它会贴在桌面图标层上，按 `Win + D` 显示桌面时不会像普通窗口一样被最小化，同时支持整体透明度、圆角、双语界面、主题和 12/24 小时制切换。

English: A small Windows desktop world clock widget that stays on the desktop layer, survives `Win + D`, and supports opacity, rounded corners, bilingual UI, themes, and 12/24-hour time.

## 功能

- 桌面常驻：挂载到桌面图标层，兼顾交互和 `Win + D` 免疫。
- 透明度：右键菜单可切换 100%、92%、85%、75%、60%。
- 圆角：使用 per-pixel alpha 渲染，圆角边缘平滑，没有直角白边。
- 双语：支持简体中文和 English。
- 主题：内置柔夜、石墨、极夜、日间主题。
- 时间制式：支持 24 小时制和 12 小时制。
- 显示选项：支持秒数开关、字体大小和宽度预设。
- 城市管理：右键选择城市，支持搜索、排序，配置会写入 `cities.txt`。
- 行为选项：支持锁定位置、重置位置和点击穿透。
- 托盘图标：点击穿透后仍可从系统托盘打开菜单。
- 开机自启：右键菜单可开启或关闭。

## 使用

直接运行：

```powershell
.\WorldClock.exe
```

右键小组件可以打开菜单：

- `选择城市...`
- `语言`
- `主题`
- `时间制式`
- `显示`
- `透明度`
- `行为`
- `开机自启`
- `退出`

拖动小组件任意位置即可移动。退出时会保存位置和设置到 `settings.txt`。

## 配置文件

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

## 编译

需要 Windows 和 .NET Framework 自带的 C# 编译器。运行：

```powershell
.\build.ps1
```

脚本会：

1. 生成 `clock.ico`
2. 编译 `WorldClock.exe`
3. 创建开始菜单快捷方式

如果编译时报 `WorldClock.exe` 正在被使用，先退出正在运行的小组件，再重新执行构建。

## 发布建议

建议不要把 `WorldClock.exe` 直接长期提交到 Git 历史里。更推荐：

- 源码放在 GitHub repo。
- 编译产物放在 GitHub Releases。
- 每次发布上传一个 zip，例如 `WorldClock-v1.0.0.zip`。

zip 内容建议：

```text
WorldClock.exe
clock.ico
cities.txt
README.md
```

建议 `.gitignore` 忽略：

```gitignore
WorldClock.exe
clock.ico
pin.log
```

## 用户体验改进方向

当前已完成到 P2 的主要体验项：城市搜索与排序、锁定位置、重置位置、秒数开关、字体大小、宽度预设、托盘图标、点击穿透、多显示器位置恢复、Explorer 重启重挂载和 DPI manifest。

以下是后续仍值得考虑的改进点。

### 安装和更新

- 做一个 release zip，附带默认配置和 README。
- 增加 GitHub Actions，打 tag 后自动编译并生成 Release。
- 增加版本号和“检查更新”入口。
- 提供便携模式说明：所有配置都保存在程序目录。

### 首次启动体验

- 首次运行时自动放在主屏右上角，但避开任务栏和屏幕边缘。
- 如果配置坐标超出当前显示器，自动拉回可见区域。
- 首次运行可以默认显示本地、纽约、伦敦、北京、东京等常用城市。
- 空城市列表时给出友好的默认内容，而不是只显示 UTC。

### 多显示器和 DPI

- 保存窗口所在显示器，而不只是保存 X/Y。
- 显示器数量变化后自动重新定位。
- 更完整支持不同缩放比例，比如 125%、150%、混合 DPI。
- 增加“重置位置”菜单，防止窗口跑到不可见区域。

### 城市选择

- 支持按地区分组，比如美洲、欧洲、亚洲、大洋洲。
- 支持自定义显示名。
- 支持添加任意 Windows 时区 ID。

### 视觉和布局

- 增加字体大小选项：紧凑、标准、大号。
- 增加宽度选项，适配更长城市名和 12 小时制时间。
- 增加秒针开关：显示 `HH:mm` 或 `HH:mm:ss`。
- 增加边框开关和阴影效果。
- 主题可以导出/导入，允许用户自定义颜色。

### 交互

- 双击切换 12/24 小时制或打开城市选择。
- 鼠标悬停时显示更详细日期、时区和 UTC 偏移。
- 增强托盘菜单，比如直接显示当前本地时间和下一次更新状态。

### 稳定性

- 增加异常日志，但限制大小。
- 对无效配置做更明确的回退。

### 国际化

- 把中英文文案集中到资源表，避免散落在代码里。
- 增加跟随系统语言选项。
- 日期格式也提供“跟随系统区域”选项。
- 城市名同时保存内部 key，切换语言时不依赖显示名匹配。

## 已知技术点

这个小组件使用了几项 Windows 桌面相关能力：

- `SetParent` 挂载到桌面图标层，避免 `Win + D` 最小化。
- `WS_EX_LAYERED` 保持透明能力。
- `UpdateLayeredWindow` 使用 32-bit ARGB 位图实现平滑圆角。
- manifest 声明 Windows 8+ 兼容性，使 child layered window 透明表现稳定。

这些 API 依赖 Windows Explorer 的桌面窗口结构。当前方案在 Windows 10/11 上可用，但 Windows 大版本更新后仍建议重新验证。
