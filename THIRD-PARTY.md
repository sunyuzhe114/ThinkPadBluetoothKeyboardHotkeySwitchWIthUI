# Behavioral Reference

This is an independent C# implementation of the key mapping behavior documented
by AlexeyBoiko's ThinkPad-Bluetooth-Keyboard-Hotkey-Switch:
https://github.com/AlexeyBoiko/ThinkPad-Bluetooth-Keyboard-Hotkey-Switch

Reviewed on 2026-10-19. The upstream page did not expose a license. No upstream
C source or prebuilt executable is copied into or shipped with this project.
Attribution does not imply an upstream license or endorsement. Retain this note
when sharing this implementation and independently review licensing before
distributing derivatives of the original project.

The Windows API calls and key constants follow Microsoft documentation:
- https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc
- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
- https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys

Self-contained publishing bundles the Microsoft .NET runtime under its upstream
redistribution terms. No third-party NuGet libraries are referenced by this app.
