# ClashWidget

> 精美的 Windows 桌面悬浮窗，实时显示 Clash Meta 代理速度和当前节点，支持一键切换。

基于 WPF (.NET 8) 打造，毛玻璃质感、原生窗口效果、流线型速度曲线。

## ✨ 功能

- **实时速度显示** — 300ms 刷新率，下载 ▼ / 上传 ▲ 直观展示，自适应 B/KB/MB/GB 单位
- **速度曲线图** — 60 秒历史 Sparkline，蓝色下载 + 绿色上传
- **节点一键切换** — 点击当前节点弹出下拉菜单，选择即切，切完自动断开旧连接
- **延迟测速** — 启动自动测，⚡ 按钮手动刷新
- **自定义设置** — API 地址、端口、密钥、测速 URL 均可配置
- **macOS 风格窗口控件** — 左上角红绿灯（关闭 / 最小化 / 最大化）
- **毛玻璃背景** — 系统级丙烯酸模糊 + Win11 原生圆角
- **始终置顶** — 悬浮不遮挡，可拖拽移动
- **隐藏任务栏图标** — 纯桌面悬浮小部件
- **单文件部署** — 发布版为单个 EXE，即开即用

## 📦 快速开始

### 下载运行

从 [Releases](https://git.gl-cloud.top/GuiLing/ClashWidget/releases) 下载最新版本：

- `ClashWidget.exe`（完整版，155 MB）— 无需安装 .NET，Windows 10 / 11 x64 双击运行
- `ClashWidget-lite.exe`（轻量版，~230 KB）— 需安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### 开发编译

```bash
# 要求：.NET 8 SDK
cd ClashWidget
dotnet run

# 发布单文件
dotnet publish -c Release -p:PublishSingleFile=true -p:SelfContained=true -o publish
```

## ⚙️ 配置

首次启动会在 EXE 同级目录生成 `config.json`：

```json
{
  "ApiHost": "0.0.0.0",
  "ApiPort": 9090,
  "ApiSecret": "123456",
  "TestUrl": "https://www.gstatic.com/generate_204"
}
```

| 字段 | 说明 | 默认值 |
|------|------|--------|
| `ApiHost` | Clash API 地址 | `0.0.0.0` |
| `ApiPort` | Clash API 端口 | `9090` |
| `ApiSecret` | API 密钥 | `123456` |
| `TestUrl` | 延迟测速地址 | `https://www.gstatic.com/generate_204` |

也可通过悬浮窗右上角 ⚙ 设置按钮直接修改。

> 注意：`ApiHost` 需与 Clash 配置中 `external-controller` 的地址一致，`ApiSecret` 与 `secret` 一致。

## 🏗️ 项目结构

```
ClashWidget/
├── App.xaml(.cs)               # 应用程序入口，单实例控制
├── MainWindow.xaml(.cs)        # 主悬浮窗（速度、曲线、节点）
├── SettingsWindow.xaml(.cs)    # 设置窗口
├── Models/
│   ├── AppConfig.cs            # 配置数据模型
│   └── TrafficData.cs          # API 响应模型
├── Services/
│   ├── ClashApiService.cs      # Clash REST API 客户端
│   └── ConfigService.cs        # 配置持久化
├── ViewModels/
│   ├── MainViewModel.cs        # 主窗口 ViewModel
│   └── SettingsViewModel.cs    # 设置窗口 ViewModel
├── Converters/
│   ├── SpeedConverter.cs       # 速度值 → 文本
│   └── NodeMatchConverter.cs   # 节点选中标记
└── Helpers/
    └── NativeMethods.cs        # P/Invoke（毛玻璃、置顶等）
```

## 🔧 技术栈

- **框架**：WPF + .NET 8
- **UI**：毛玻璃（`SetWindowCompositionAttribute`）、Win11 Acrylic Backdrop
- **数据**：Clash Meta REST API（`/traffic`、`/proxies`、`/connections`）
- **认证**：Bearer Token + URL Query Token
- **部署**：单文件 Publish（`PublishSingleFile` + `IncludeNativeLibrariesForSelfExtract`）

## 📄 开源协议

MIT License

This project was built with DeepSeek-V4-Pro/Gemini using Vibe Coding techniques
