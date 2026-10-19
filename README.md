# ThinkPad 蓝牙键盘切换器

Windows 可视化托盘程序，参考
[AlexeyBoiko/ThinkPad-Bluetooth-Keyboard-Hotkey-Switch](https://github.com/AlexeyBoiko/ThinkPad-Bluetooth-Keyboard-Hotkey-Switch)
的按键映射行为，独立使用 C# / WinForms 实现。

它切换的是 **功能键与多媒体热键的映射**，不是切换蓝牙配对对象，
也不读取或修改键盘固件中的 Fn Lock 状态。

项目仓库：[ThinkPadBluetoothKeyboardHotkeySwitchWIthUI](https://github.com/sunyuzhe114/ThinkPadBluetoothKeyboardHotkeySwitchWIthUI)。

## 快速开始

**直接使用：** [下载最新版 EXE](https://github.com/sunyuzhe114/ThinkPadBluetoothKeyboardHotkeySwitchWIthUI/releases/latest/download/ThinkPadKeyboardSwitcher.exe)，
或打开 [版本下载页](https://github.com/sunyuzhe114/ThinkPadBluetoothKeyboardHotkeySwitchWIthUI/releases/latest)
查看更新说明和 `SHA256SUMS.txt` 校验值。放到固定目录后双击运行，无需另装 .NET。
程序尚未进行数字签名，请确认下载来源并遵守设备的安全策略。

**从源码编译：** 仓库不提交 `dist/` 中的编译产物，可执行文件单独放在 Releases。
请在 Windows 上安装
[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)，然后在 PowerShell 中执行：

```powershell
git clone git@github.com:sunyuzhe114/ThinkPadBluetoothKeyboardHotkeySwitchWIthUI.git
cd ThinkPadBluetoothKeyboardHotkeySwitchWIthUI
.\build.ps1
& .\dist\ThinkPadKeyboardSwitcher.exe
```

SSH 克隆需要 GitHub 认可的 SSH 密钥；也可使用仓库的 HTTPS 地址克隆。
首次编译需要联网获取微软运行时。若本机策略阻止运行脚本，请先检查脚本，
再按设备管理要求在当前进程中允许执行，不需要永久修改系统策略。

默认生成 **Windows x64 自包含单文件程序**。目前在 Windows 11 x64 上完成
构建和基础验证；其他 Windows 版本、ARM64 以及不同型号实体键盘尚未完成验证。

## 运行

打开 `dist/ThinkPadKeyboardSwitcher.exe`。默认是 F 键优先，后续记住上次选择。
自包含 win-x64 版本无需另外安装 .NET。代码位于 `src/`，无需管理员权限。

- 绿色 **F**：F 键优先，启用双向映射。
- 蓝色 **H**：原始热键，不改写按键。
- 红色 **!**：出现异常，双击图标查看具体信息。
- 右键托盘图标选择模式；双击打开控制窗口。
- 关闭或最小化控制窗口会收起到托盘；菜单或窗口中的“退出”会彻底退出。
- Windows 可能把新图标放在任务栏的隐藏图标区，可手动拖到通知区域。
- 最大化或调整窗口大小时，按键列表和列宽随之调整，提示文字换行，底部按钮保持可见。
- 最大化后收起到托盘，再次打开会保留最大化状态。

## 开机自动启动

主窗口和托盘右键菜单都提供“登录后自动启动”开关。只设置当前用户：

```text
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
值名称：ThinkPadBluetoothKeyboardSwitcher
值内容："程序完整路径\ThinkPadKeyboardSwitcher.exe" --tray
```

这是登录 Windows 后启动，不是登录前运行的系统服务。自动启动时只显示
托盘图标，不弹出主窗口，恢复上次模式。可以随时取消勾选。退出程序不会
自动删除启动项。若移动 EXE，请在新位置重新勾选；不要删除仍被启动项引用
的文件。Windows 的任务管理器若另行禁用了此启动项，需由用户在那里恢复。

命令行还支持 `--tray`、`--enable-startup`、`--disable-startup`。同一用户会话
只保留一个实例；再次双击 EXE 会唤回已有窗口。

```powershell
# 静默驻留托盘，不改变已有启动项
& .\dist\ThinkPadKeyboardSwitcher.exe --tray

# 注册当前 EXE 为登录启动项，并静默启动
& .\dist\ThinkPadKeyboardSwitcher.exe --enable-startup --tray

# 取消指向当前 EXE 的启动项
& .\dist\ThinkPadKeyboardSwitcher.exe --disable-startup
```

这些启动项参数不会在设置完成后自动退出程序。建议日常直接使用界面开关。
只从新路径启动程序不会自动更新旧路径的启动项，需要重新勾选注册。

## 映射与范围

| 功能键 | 热键 |
| --- | --- |
| F1 | 静音 |
| F2 / F3 | 音量降低 / 提高 |
| F4 / F5 / F6 | 上一曲 / 播放暂停 / 下一曲 |
| F7 / F8 | 浏览器后退 / 主页 |
| F9 | 菜单 |
| F10 | Shift + Win + F21 |
| F11 | Ctrl + Alt + Tab |
| F12 | 不改变 |

F 键模式交换表中两侧的事件，原始模式完全保留设备原来的事件。
F10 / F11 的特殊组合键可通过“兼容 F10 / F11 组合键”独立关闭。
开启兼容时普通的 Ctrl+Alt+Tab 也会被映射为 F11，因此常用该快捷键时应关闭。

上游针对 ThinkPad Compact Bluetooth Keyboard with TrackPoint（4X30K12182）。
本程序沿用其键值假设，不能保证其他型号或不同固件使用相同键值。
**它是全局键盘钩子，会影响同时连接的其他键盘，不能精确只作用于某一块蓝牙键盘。**
图标表示程序的映射状态，不表示蓝牙连接状态。

## 实现与安全

- 独立线程安装 `WH_KEYBOARD_LL`，该线程执行快速事件处理，不做磁盘写入。
- 使用 `SendInput`，分别处理按下、重复和抬起，切换/退出时释放映射持有的按键。
- 跳过程序自身输出的按键，防止自递归。蓝牙消费控制热键有时由 Windows
  以合成事件传递，1.0.1 起允许这类静音、音量等表内热键参与映射。
  其他合成按键与低完整性级别的注入仍然跳过。
  不要同时运行原版工具或作用于同组键的重映射程序。
- 不安装驱动、不修改蓝牙设置、不记录键入文本、不上传数据。
- 参数文件为 `%LOCALAPPDATA%\ThinkPadKeyboardSwitcher\settings.json`。
- 普通权限程序不能可靠向管理员窗口发送按键；出现限制时恢复原始模式，
  不会自动提权或修改 UAC。管理员窗口不在默认验证范围内。
- Windows 可能在钩子线程超时后静默移除钩子；图标不是硬件反馈。可使用
  “重启监听”重新安装，或退出重开。

## 编译和测试

建议安装 .NET 8 SDK；使用更新的 SDK 时，测试仍需要 .NET 8 运行时。
在本目录运行：

```powershell
.\build.ps1
dotnet run --project tests
dotnet run --project tests-windows
$exe = Join-Path $PWD 'dist\ThinkPadKeyboardSwitcher.exe'
$test = Start-Process -FilePath $exe -ArgumentList '--self-test reports\native-self-test.json' -WindowStyle Hidden -Wait -PassThru
Get-Content .\reports\native-self-test.json
$test.ExitCode
```

`build.ps1` 先跑映射和 Windows 布局测试，再生成自包含 EXE 到 `dist/`；首次构建会从
NuGet 下载微软运行时；本机缓存齐全时可用 `.\build.ps1 -Offline` 离线打包。
`-FrameworkDependent` 可生成依赖本机 .NET 8 Desktop
Runtime 的较小版本。`--self-test` 只验证 Win32 结构、钩子安装/卸载和图标创建，
不发送按键、不修改启动项、不改变用户设置。

重新打包前请通过托盘菜单“退出”旧程序，否则正在运行的 EXE 可能无法覆盖。
关闭主窗口只是收起到托盘，不等于退出。`-Offline` 要求 NuGet 缓存已经齐全，
不能用于一台从未成功构建过的新电脑。

上游与微软 API 来源见 [THIRD-PARTY.md](THIRD-PARTY.md)。自动化验证不替代真实
键盘的 F1 / Fn+F1 / 长按音量 / F10 / F11 检查。

开发结构和复测方法见 [开发与验证说明](docs/DEVELOPMENT.md)。
版本更新见 [1.0.2 发布说明](docs/releases/v1.0.2.md)。

## 常见问题

| 问题 | 排查方法 |
| --- | --- |
| 启动后没有看到窗口 | 登录启动和 `--tray` 只显示托盘图标；查看任务栏隐藏图标区，双击 F/H/! 图标。 |
| 有绿色 F 图标但按键没变化 | 图标只表示软件模式，不是硬件反馈。先确认键盘型号、按键实际键值，再尝试“重启监听”；不要同时运行其他重映射工具。 |
| 旧版中 F1 仍然静音 | 1.0.0 会跳过所有合成事件，部分蓝牙键盘的多媒体键因此漏过映射；请重新编译 1.0.1 或更新版本，并确认退出了旧进程。 |
| 笔记本内置键盘也被改变了 | 这是全局键盘映射，当前版本不能按物理设备隔离；切换“原始热键”可停用映射。 |
| Ctrl+Alt+Tab 不再打开原来的界面 | 关闭“兼容 F10 / F11 组合键”，恢复这组快捷键的原始行为。 |
| 在管理员窗口中失效或显示红色 ! | Windows 输入权限隔离可能阻止输出；程序不会自动提权或关闭 UAC。优先在普通权限窗口使用。 |
| 勾选了自动启动，但登录后没运行 | 检查 EXE 是否被移动/删除，以及任务管理器“启动应用”是否单独禁用了该项。 |
| 移动了程序后取消启动项没有生效 | 程序只删除匹配当前 EXE 路径的启动项；先在旧位置取消，或在新位置先启用再取消。 |
| 构建时下载失败或文件占用 | 检查 NuGet 网络连接；已缓存依赖可用 `-Offline`。文件占用时先在托盘退出旧程序。 |

需要进一步排查 F1 时，可先退出已有实例，再从仓库根目录启动一次限时诊断：

```powershell
& .\dist\ThinkPadKeyboardSwitcher.exe --diagnose-f1 reports\f1-diagnostic.json
```

仅记录 F1／静音事件的键值、标记和映射结果，以及运行模式，最多 128 条，
两分钟后自动停止。不会记录字母、数字或输入的文字；不带该参数的日常运行
不会生成诊断报告。报告保存在本地，不会上传；再次启动不会给已有实例开启诊断。

## 卸载

1. 在主窗口或托盘菜单取消“登录后自动启动”。
2. 点击“退出”，释放键盘监听。
3. 删除自己保存的程序文件夹。没有安装服务或驱动，无需专用卸载器。
4. 可选：删除 `%LOCALAPPDATA%\ThinkPadKeyboardSwitcher` 中的个人设置。

## 来源与许可

本仓库独立实现上游的按键映射行为，未包含上游 C 源码或可执行文件。
目前尚未为本仓库指定开源许可证；公开可见不等于已授予任意再分发或商业使用许可。
如需以特定许可证分发，请先由维护者确认，并保留 [第三方说明](THIRD-PARTY.md)。
