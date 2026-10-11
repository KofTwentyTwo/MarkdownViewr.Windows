/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.ComponentModel;
using System.Runtime.InteropServices;


namespace MarkdownViewr.Windows;

/// <summary>Routes one command from owned browser child windows before their native message dispatch.</summary>
internal sealed partial class DocumentKeyboard : IDisposable
{
   private readonly nint _window;
   private readonly Func<bool> _find;
   private readonly HookCallback _callback;
   private nint _hook;



   /// <summary>Installs a current-UI-thread message hook; no other process, thread or desktop is observed.</summary>
   public DocumentKeyboard(nint window, Func<bool> find)
   {
      _window = window;
      _find = find;
      _callback = OnMessage;
      _hook = SetWindowsHookExW(3, Marshal.GetFunctionPointerForDelegate(_callback), 0, GetCurrentThreadId());
      if(_hook == 0)
      {
         throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not initialize the document keyboard shortcut.");
      }
   }



   /// <summary>Consumes only an unmodified Ctrl+F removed from this active window's own message queue.</summary>
   private nint OnMessage(int code, nuint remove, nint pointer)
   {
      if(code >= 0 && remove == 1 && GetForegroundWindow() == _window)
      {
         NativeMessage message = Marshal.PtrToStructure<NativeMessage>(pointer);
         if(message.Message == 0x0100 && message.WParam == 0x46 && (message.Window == _window || IsChild(_window, message.Window))
            && IsDown(0x11) && !IsDown(0x10) && !IsDown(0x12) && !IsDown(0x5B) && !IsDown(0x5C) && _find())
         {
            message.Message = 0;
            message.WParam = 0;
            message.LParam = 0;
            Marshal.StructureToPtr(message, pointer, fDeleteOld: false);
         }
      }
      return CallNextHookEx(_hook, code, remove, pointer);
   }



   /// <summary>Checks only command modifiers; no keyboard input or document content is retained.</summary>
   private static bool IsDown(int key) => (GetKeyState(key) & 0x8000) != 0;



   /// <summary>Removes the native callback before the owning window and browser are destroyed.</summary>
   public void Dispose()
   {
      if(_hook != 0)
      {
         _ = UnhookWindowsHookEx(_hook);
         _hook = 0;
      }
      GC.KeepAlive(_callback);
   }



   /// <summary>The Windows message layout, including its reserved trailing field.</summary>
   [StructLayout(LayoutKind.Sequential)]
   private struct NativeMessage
   {
      public nint Window;
      public uint Message;
      public nuint WParam;
      public nint LParam;
      public uint Time;
      public int X;
      public int Y;
      public uint Private;
   }



   /// <summary>The current-thread hook callback uses the Windows calling convention.</summary>
   [UnmanagedFunctionPointer(CallingConvention.Winapi)]
   private delegate nint HookCallback(int code, nuint remove, nint pointer);



   /// <summary>Registers the callback in the named thread.</summary>
   [LibraryImport("user32.dll", SetLastError = true)]
   [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
   private static partial nint SetWindowsHookExW(int hook, nint callback, nint module, uint thread);



   /// <summary>Preserves all unhandled message processing in the hook chain.</summary>
   [LibraryImport("user32.dll")]
   [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
   private static partial nint CallNextHookEx(nint hook, int code, nuint remove, nint pointer);



   /// <summary>Releases the thread callback registration.</summary>
   [LibraryImport("user32.dll")]
   [return: MarshalAs(UnmanagedType.Bool)]
   [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
   private static partial bool UnhookWindowsHookEx(nint hook);



   /// <summary>Limits delivery to the current UI thread.</summary>
   [LibraryImport("kernel32.dll")]
   [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
   private static partial uint GetCurrentThreadId();



   /// <summary>Requires this window to own foreground input.</summary>
   [LibraryImport("user32.dll")]
   [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
   private static partial nint GetForegroundWindow();



   /// <summary>Confirms that a browser child belongs to the app window.</summary>
   [LibraryImport("user32.dll")]
   [return: MarshalAs(UnmanagedType.Bool)]
   [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
   private static partial bool IsChild(nint parent, nint child);



   /// <summary>Reads the current message thread's modifier state.</summary>
   [LibraryImport("user32.dll")]
   [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
   private static partial short GetKeyState(int key);
}
