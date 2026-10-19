# 开发与验证说明

## 目录结构

| 文件 | 职责 |
| --- | --- |
| `src/Program.cs` | 程序入口、启动参数、当前用户会话内单实例、自检。 |
| `src/MainForm.cs` | 中文控制窗口、模式切换、兼容选项和登录启动开关。 |
| `src/TrayApplication.cs` | 托盘菜单、窗口生命周期、状态更新、重启监听。 |
| `src/MappingEngine.cs` | 不依赖 WinForms 的按键映射状态机。 |
| `src/KeyboardHook.cs` | 独立线程、低级键盘钩子、事件过滤和故障处理。 |
| `src/Native.cs` | Win32 声明、输入结构和合成按键输出。 |
| `src/StatusIcons.cs` | 创建 F/H/! 状态图标。 |
| `src/SettingsStore.cs` | 当前用户模式与兼容选项的 JSON 存储。 |
| `src/StartupRegistration.cs` | 当前用户 Run 启动项的读取、注册和取消。 |
| `src/app.manifest` | 普通权限运行，不自动请求管理员权限。 |
| `tests/Program.cs` | 无第三方测试框架的映射回归断言。 |
| `build.ps1` | 先执行映射测试，再发布 Windows x64 程序。 |

`bin/`、`obj/`、`dist/`、`artifacts/`、生成的自检报告以及本地实施计划不提交。
仓库不包含开发机的 SSH 配置、个人设置或启动项。

## 事件处理流程

```text
物理按键
  -> 专用消息循环线程上的 WH_KEYBOARD_LL
  -> 跳过注入事件
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

当前测试包含 72 个断言，末行应为 `72 assertions passed`。覆盖：

- F1-F9 与对应热键的双向按下、长按重复和抬起。
- 关闭映射时透传，注入事件、普通字母、F12 和普通 Tab 不改写。
- 持有映射键时关闭模式，释放输出并消化尚未结束的物理按键事件。
- F10/F11 组合键的输出顺序、修饰键释放与恢复。
- 退出释放持有组合键，以及关闭组合键兼容后的透传。

这些测试直接验证状态机，不依赖实体键盘，不证明 Windows 实际收到的硬件事件一致。

### 编译检查

```powershell
dotnet build src/KeyboardSwitcher.csproj -c Release
```

发布可使用根目录 `build.ps1`。默认自包含，`-FrameworkDependent` 依赖目标电脑
安装 .NET 8 Desktop Runtime；`-Offline` 仅适用于运行时与依赖已缓存的电脑。
当前测试项目没有第三方 NuGet 依赖，测试仍需要 .NET 8 运行时。

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

已在 Windows 11 x64 上验证构建、72 个映射断言和原生自检。
基础界面检查确认过 F/H 状态显示、模式保存、启动项注册读取以及再次启动唤回已有窗口。
尚未完成全部托盘交互、真实注销/重新登录、各型号实体键盘和管理员窗口的端到端验证。

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
