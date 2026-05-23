# Dororo on RHEL 9 / X11

[MelanTech/Dororo](https://github.com/MelanTech/Dororo) 的下游移植 —— 一个 Live2D 桌面宠物，运行于 **Red Hat Enterprise Linux 9** （以及兼容发行版：Fedora、AlmaLinux、Rocky）的 **X11 会话**。

[English → README.md](README.md)

---

## 这是什么

Dororo 是一个桌面宠物：一个小动画角色（"Doro"），漂浮在你桌面的透明无边框窗口里。她会闲逛、目光跟随你的光标、对点击作出反应。上游项目针对 Windows，使用 Win32 API 实现桌面集成功能（点击穿透透明、置顶、全局光标追踪、全屏检测、移动到回收站、登录自启动）。

本分支将每一个 Win32 专用部分替换为 X11 / EWMH / freedesktop.org 等价物，同时保留 Windows 构建路径完整：

| 原版 (Win32) | 本分支 (Linux) |
|---|---|
| `WS_EX_LAYERED + WS_EX_TRANSPARENT` 点击穿透 | `XShapeCombineRectangles(ShapeInput, …)` |
| `WS_EX_TOOLWINDOW`（隐藏任务栏图标） | EWMH `_NET_WM_STATE_SKIP_TASKBAR` + `_NET_WM_STATE_SKIP_PAGER` |
| `GetForegroundWindow` + 尺寸比较 | `_NET_ACTIVE_WINDOW` + `_NET_WM_STATE_FULLSCREEN` |
| `GetCursorPos` | `XQueryPointer(root, …)` |
| `SHFileOperation FO_DELETE`（回收站） | `gio trash`（XDG Trash 规范） |
| 启动文件夹 `.lnk` (COM) | XDG Autostart `~/.config/autostart/<name>.desktop` |

C# 代码在运行时通过 `OperatingSystem.IsWindows() / IsLinux()` 分支，单一源代码树可同时构建两个平台。

> **不支持 Wayland。** Wayland 有意隐藏了桌宠窗口所依赖的 API（点击穿透、全局光标查询、前台窗口检查）。请在 GNOME 登录界面选择 **GNOME on Xorg**，并通过 `echo $XDG_SESSION_TYPE` 验证为 `x11`。

---

## 快速开始（预构建二进制）

从本仓库的 [Releases 页面](../../releases) 下载最新 tarball，然后在 RHEL 9 X11 桌面上：

```bash
sudo dnf install -y \
    libX11 libXext libXfixes libXrandr libXrender libXcursor libXinerama libXi \
    libGL libEGL fontconfig alsa-lib pulseaudio-libs glib2

tar -xzf Dororo-RHEL9.tar.gz
cd Dororo-RHEL9/prebuilt
chmod +x Dororo.x86_64
./Dororo.x86_64
```

完整部署指南：用浏览器打开 [`doc/deploy-guide.html`](doc/deploy-guide.html)。

---

## 从源代码构建

如果你想自己构建二进制（或修改代码），请按照 [`doc/build-guide.html`](doc/build-guide.html) ——一份面向从未接触过 Godot 或 Live2D 的人编写的 16 节详细教程。简要步骤：

1. 安装 `gcc-c++`、`dotnet-sdk-8.0`、`scons`、X11 / GL 开发库。
2. 安装 Godot 4.4 **.NET** 版。
3. 从 <https://www.live2d.com/sdk/download/native/> 下载 Live2D Cubism Native SDK **5-r.1** 版本（个人使用免费，需接受许可）。
4. 编译 [MizunagiKB/gd_cubism](https://github.com/MizunagiKB/gd_cubism) GDExtension 链接该 SDK。
5. 将生成的 `libgd_cubism.linux.*.x86_64.so` 文件放入 `addons/gd_cubism/bin/`。
6. `dotnet build`，然后 `godot --headless --export-release "Linux" export/Dororo.x86_64`。

---

## 为什么 Live2D `.so` 不在本仓库中

`gd_cubism` 静态链接 Live2D Cubism Native SDK，即 Live2D 的专有代码。其许可禁止重新分发 SDK 或其编译产物。你自己接受 EULA 后用该 SDK 从源代码构建是允许的；将生成的 `.so` 提交到公共仓库则不允许。

Releases 中的预构建二进制是基于相同条款的个人使用分发 —— 如果你打算商业使用 Dororo，请先阅读 [Live2D 的许可证](https://www.live2d.com/sdk/download/native/)。

---

## 与上游的差异

UX 与 Windows 原版相同，包含以下有意为之的下游改动：

- **Linux/X11 支持** —— 七个 C# autoload 重写为跨平台版本。
- **移除 LLM 聊天功能** —— OpenAI 客户端和聊天 UI 已删除（与移植本身无关；如需恢复可从上游轻松拉取）。
- **`window/subwindows/embed_subwindows=true`** —— 设置对话框现在渲染于主窗口内。这是 WSLg 的 XWayland 上点击处理的必要条件；在真实 X 服务器上无害。
- **修复了闲逛 Doro 转向右侧时的点击区域 bug** —— `hit_area_handler.gd` 中的 `recalc_mouse_position` 在角色面朝右时对 X 坐标双重翻转，导致点击落在任何点击区域之外。（已作为单独的 PR 提交给上游。）
- **`mouse_follow` 默认值改为 `true`** —— 桌宠在首次启动时无需打开设置即可跟随你的目光。

完整的文件级 diff 叙述请参见 [`doc/build-guide.html`](doc/build-guide.html) 第 14 节。

---

## 文档

在浏览器中打开这些（精心排版的 HTML，非默认 GitHub Markdown 渲染）：

- **[`doc/index.html`](doc/index.html)** —— 入口页
- **[`doc/build-guide.html`](doc/build-guide.html)** —— 从源代码构建，初学者级
- **[`doc/deploy-guide.html`](doc/deploy-guide.html)** —— 部署预构建二进制
- **[`doc/about.html`](doc/about.html)** —— 上游项目原始说明

---

## 状态

- ✅ 在 RHEL 9.7 上构建并运行（已在 WSL 2 + WSLg 下测试）
- ✅ 所有手势工作：拖拽、点击表情、目光跟随、设置对话框
- ⚠️ Linux 默认 **禁用点击穿透** —— XShape ShapeInput 切换在 WSLg 的 XWayland 上与兄弟 Godot Window 节点（设置对话框）冲突。在真实 RHEL X11 桌面上可以安全启用，详见构建指南。
- ⚠️ WSLg 上鼠标跟随仅限于 WSL 的 X 会话范围内。在原生 RHEL X11 上可全局追踪。
- ❓ Fedora / AlmaLinux / Rocky —— 应当能工作（包名相同），未亲测。

---

## 致谢

- 原项目：[MelanTech/Dororo](https://github.com/MelanTech/Dororo)
- Live2D GDExtension：[MizunagiKB/gd_cubism](https://github.com/MizunagiKB/gd_cubism)
- Live2D Doro 角色 / Cubism 技术：[Live2D Inc.](https://www.live2d.com/)
- Linux 移植与文档：[@last-Medjay](https://github.com/last-Medjay)

## 许可证

与上游相同 —— 请参见 [`LICENSE`](LICENSE)。Live2D Cubism Native SDK 有自己的[专有条款](https://www.live2d.com/sdk/download/native/)，适用于任何基于其构建的二进制 `.so`。
