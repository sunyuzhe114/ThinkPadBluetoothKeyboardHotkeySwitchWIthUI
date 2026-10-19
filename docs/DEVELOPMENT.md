# 开发与验证说明

## 目录结构

| 文件 | 职责 |
| --- | --- |
| `src/Program.cs` | 程序入口、启动参数、当前用户会话内单实例、自检。 |
| `src/MainForm.cs` | 中文控制窗口、模式切换、兼容选项和登录启动开关。 |
| `src/TrayApplication.cs` | 托盘菜单、窗口生命周期、状态更新、重启监听。 |
| `src/MappingEngine.cs` | 不依赖 WinForms 的按键映射状态机。 |
| `src/F1Diagnostics.cs` | 显式启用后仅采集 F1／静音事件的限时、有界诊断。 |
| `src/KeyboardHook.cs` | 独立线程、低级键盘钩子、事件过滤和故障处理。 |
| `src/Native.cs` | Win32 声明、输入结构和合成按键输出。 |
| `src/StatusIcons.cs` | 创建 F/H/! 状态图标。 |
| `src/SettingsStore.cs` | 当前用户模式与兼容选项的 JSON 存储。 |
| `src/StartupRegistration.cs` | 当前用户 Run 启动项的读取、注册和取消。 |
| `src/app.manifest` | 普通权限运行，不自动请求管理员权限。 |
| `tests/Program.cs` | 无第三方测试框架的映射回归断言。 |
| `tests-windows/Program.cs` | 检查真实 WinForms 布局尺寸、列宽、按钮边界和长提示换行，不安装键盘钩子。 |
| `build.ps1` | 先执行映射和布局测试，再发布 Windows x64 程序。 |
| `.github/workflows/release.yml` | 标签触发的 Windows 构建、测试和 Releases 附件发布。 |

`bin/`、`obj/`、`dist/`、`artifacts/`、生成的自检报告以及本地实施计划不提交。
仓库不包含开发机的 SSH 配置、个人设置或启动项。

## 事件处理流程

```text
物理按键
  -> 专用消息循环线程上的 WH_KEYBOARD_LL
  -> 跳过自身输出、低完整性注入与无关合成键；接纳系统合成的表内多媒体键
  -> MappingEngine 判断按下 / 重复 / 抬起
  -> SendInput 输出映射结果，或透传原始事件
```

界面把模式变更发送到监听线程，状态机负责释放已映射的按键，并处理原物理键
尚未抬起时的后续事件。F10/F11 的组合键兼容逻辑还会跟踪修饰键状态。
按键输出失败时停止映射并通知界面；这不是驱动层键盘改写，也不是蓝牙设备控制。

设置存储在 `%LOCALAPPDATA%\ThinkPadKeyboardSwitcher\settings.json`。
设置通过临时文件替换写入；登录启动的真实状态来自当前用户注册表，不写入该 JSON。
窗口关闭默认隐藏，只有“退出”才会结束消息循环并释放钩子。

## 自动化验证

### 纯映射测试

```powershell
dotnet run --project tests/KeyboardSwitcher.Tests.csproj -c Release
```

当前测试包含 130 个断言，末行应为 `130 assertions passed`。覆盖：

- F1-F9 与对应热键的双向按下、长按重复和抬起。
- 关闭映射时透传，普通字母、F12 和普通 Tab 不改写。
- 系统合成的 F1-F9 对应热键完整映射，自身输出和其他合成键不递归。
- 低完整性级别注入透传，合成热键长按时关闭模式能够释放和消化抬起。
- 持有映射键时关闭模式，释放输出并消化尚未结束的物理按键事件。
- F10/F11 组合键的输出顺序、修饰键释放与恢复。
- 退出释放持有组合键，以及关闭组合键兼容后的透传。
- 诊断不记录普通文字，事件数量受限。

这些测试直接验证状态机，不依赖实体键盘，不证明 Windows 实际收到的硬件事件一致。

### 编译检查

```powershell
dotnet build src/KeyboardSwitcher.csproj -c Release
dotnet run --project tests-windows -c Release
```

发布可使用根目录 `build.ps1`。默认自包含，`-FrameworkDependent` 依赖目标电脑
安装 .NET 8 Desktop Runtime；`-Offline` 仅适用于运行时与依赖已缓存的电脑。
当前测试项目没有第三方 NuGet 依赖，测试仍需要 .NET 8 运行时。

布局测试包含 40 个断言，覆盖 520x540、570x605、800x650、1280x800、1920x1080
客户端尺寸、列表扩展、列宽、底部按钮和较长错误提示。字体不随窗口尺寸放大；
由 Windows DPI 缩放负责系统显示比例。主窗口通过列表的百分比行吸收多余高度，
其余行按内容自动定高，避免固定高度导致最大化后的空白。
布局检查会为子控件创建真实窗口句柄，覆盖大窗口收缩后原生列宽的刷新。
列宽在布局消息完成后合并更新，避免原生列表在还原窗口时留下过宽的列。
已在本机 120 DPI 环境通过布局断言，并检查实际窗口的默认、最大化、
收起及重新显示状态；重新显示保留最大化，F 键模式和登录启动项未改变。

### Windows 原生自检

先构建发布版本，再从仓库根目录执行：

```powershell
$exe = Join-Path $PWD 'dist\ThinkPadKeyboardSwitcher.exe'
$test = Start-Process -FilePath $exe -ArgumentList '--self-test reports\native-self-test.json' -WindowStyle Hidden -Wait -PassThru
Get-Content .\reports\native-self-test.json
$test.ExitCode
```

成功时退出码为 `0`，x64 报告包含：

```json
{"status":"PASS","inputBytes":40,"hookInstallAndRemove":true,"iconWidth":32,"emittedKeys":0}
```

自检验证 Win32 输入结构尺寸、钩子安装/卸载和图标创建，不发送按键，
不改变用户模式或启动项。它在单实例判断之前执行，可以与正常程序同时运行。

## 当前验证边界

已在 Windows 11 x64 上验证构建、映射断言和原生自检。
基础界面检查确认过 F/H 状态显示、模式保存、启动项注册读取以及再次启动唤回已有窗口。
尚未完成全部托盘交互、真实注销/重新登录、各型号实体键盘和管理员窗口的端到端验证。

### 1.0.1 蓝牙 F1 修复验证

2026-10-20，在系统识别为 ThinkPad Multi Connect Bluetooth Keyboard with Trackpoint
的已连接键盘上，用户直接按 F1。1.0.0 的运行记录收到 `VK_VOLUME_MUTE (0xAD)`，
按下标记为 `0x11`，抬起为 `0x91`，旧过滤规则把它们当作注入事件放行。
这证明问题发生在软件输入过滤，不足以确定究竟由哪一个驱动或进程合成事件。

1.0.1 保留自身输出标记过滤，仅放行表内外部合成热键进入映射。
同一键盘复测记录显示 `0xAD -> VK_F1 (0x70)` 的按下、抬起均输出成功，
自身输出按预期跳过二次转换。用户确认 F1 不再静音。正常托盘模式已恢复，
登录启动项保留 `--tray`，设置保留 F 键优先；F2-F11 仍需分别做实体键盘验证。
运行报告只保留在本地忽略目录，不进入仓库。

建议发布新版本前手动检查：

1. 不运行其他重映射工具，记录键盘型号与原始 F1/Fn+F1 行为。
2. 切换 F/H 模式，检查 F1、Fn+F1、长按音量和 F12。
3. 测试 F10/F11 及 Ctrl+Alt+Tab，确认兼容开关符合使用习惯。
4. 按住目标键切换模式后松开，检查是否存在卡键。
5. 关闭和最小化窗口后检查托盘，双击图标重新打开。
6. 启用登录启动，正常注销并登录，确认仅显示托盘且保留模式；随后验证取消。
7. 退出程序后确认恢复原始输入；连接多块键盘时确认理解全局影响范围。

报告问题时附 Windows 版本、键盘型号、应用模式、兼容选项以及具体按键顺序。
不需要提供密码、SSH 私钥或完整个人配置目录。

## 发布版本

1. 更新 `src/KeyboardSwitcher.csproj` 中的版本，添加 `docs/releases/vX.Y.Z.md`。
2. 执行本地测试和界面检查，提交源码并推送 `main`。
3. 为该提交创建并推送 `vX.Y.Z` 标签，版本号必须与项目一致。
4. GitHub Actions 在 Windows 上重新构建、运行两套测试和原生自检；全部通过后，上传自包含 EXE 与 SHA-256 校验文件，再公开 Release。

发布流程仅使用仓库内置的临时 `GITHUB_TOKEN`，无须把个人令牌或 SSH 私钥写入仓库。
官方 checkout/setup-dotnet 动作固定到已核实的提交。EXE 不进入 Git 历史，现有已公开版本不会被工作流覆盖。
