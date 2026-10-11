/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.ComponentModel;
using System.Runtime.InteropServices;


namespace MarkdownViewr.Windows;

/// <summary>Routes bounded reading commands from owned child windows before native browser dispatch.</summary>
internal sealed partial class DocumentKeyboard : IDisposable
{
   private readonly nint _window;
   private readonly Func<bool> _readingFocused;
   private readonly Func<Core.Navigation.ReaderCommand, bool> _execute;
   private readonly Core.Navigation.ReaderKeyboard _keys = new();
   private readonly HookCallback _callback;
   private nint _hook;



   /// <summary>Installs a current-UI-thread message hook; no other process, thread or desktop is observed.</summary>
   public DocumentKeyboard(nint window, Func<bool> readingFocused, Func<Core.Navigation.ReaderCommand, bool> execute)
   {
      _window = window;
      _readingFocused = readingFocused;
      _execute = execute;
      _callback = OnMessage;
      _hook = SetWindowsHookExW(3, Marshal.GetFunctionPointerForDelegate(_callback), 0, GetCurrentThreadId());
      if(_hook == 0)
      {
         throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not initialize the document keyboard shortcut.");
      }
   }



   /// <summary>Consumes only allowed reading commands removed from this active window's own message queue.</summary>
   private nint OnMessage(int code, nuint remove, nint pointer)
   {
      if(code >= 0 && remove == 1 && GetForegroundWindow() == _window)
      {
         NativeMessage message = Marshal.PtrToStructure<NativeMessage>(pointer);
         if(message.Message == 0x0100 && (message.Window == _window || IsChild(_window, message.Window)) && HandleKey(message))
         {
            message.Message = 0;
            message.WParam = 0;
            message.LParam = 0;
            Marshal.StructureToPtr(message, pointer, fDeleteOld: false);
         }
      }
      return CallNextHookEx(_hook, code, remove, pointer);
   }



   /// <summary>Does not translate text-control input; Ctrl+F remains a native window command.</summary>
   private bool HandleKey(NativeMessage message)
   {
      if(message.WParam is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or >= 0xA0 and <= 0xA5)
      {
         return false;
      }
      bool control = IsDown(0x11);
      bool shift = IsDown(0x10);
      bool alt = IsDown(0x12);
      bool windows = IsDown(0x5B) || IsDown(0x5C);
      bool focused = _readingFocused();
      bool nativeFind = message.WParam == 0x46 && control && !shift && !alt && !windows;
      if((!focused && !nativeFind) || alt || windows)
      {
         _keys.Reset();
         return false;
      }
      char character = nativeFind ? 'f' : message.WParam == 0x1B ? '\u001B' : control && !shift && message.WParam is 0x44 or 0x55 or 0x42
         ? char.ToLowerInvariant((char)message.WParam)
         : TranslateKey((uint)message.WParam, (uint)(((nuint)message.LParam >> 16) & 0xFF));
      var key = new Core.Navigation.ReaderKey(character, control, shift, alt, windows, ((nuint)message.LParam & 0x40000000) != 0);
      Core.Navigation.ReaderCommand command = _keys.Resolve(key, focused, Environment.TickCount64);
      return command == Core.Navigation.ReaderCommand.Prefix || command != Core.Navigation.ReaderCommand.None && _execute(command);
   }



   /// <summary>Uses the active layout with no dead-key-state mutation; only one ASCII reading character is considered.</summary>
   private static unsafe char TranslateKey(uint key, uint scan)
   {
      byte* keyboard = stackalloc byte[256];
      char* text = stackalloc char[8];
      if(!GetKeyboardState(keyboard))
      {
         return '\0';
      }
      keyboard[0x11] = keyboard[0xA2] = keyboard[0xA3] = 0;
      int count = ToUnicode(key, scan, keyboard, text, 8, 4);
      return count == 1 && text[0] <= 0x7F ? text[0] : '\0';
   }



   /// <summary>Focus/page/window changes cannot carry a prefix into another document or text control.</summary>
   public void Reset() => _keys.Reset();


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



   /// <summary>Reads only the owning UI thread's current modifier/layout state.</summary>
   [LibraryImport("user32.dll")]
   [return: MarshalAs(UnmanagedType.Bool)]
   [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
   private static unsafe partial bool GetKeyboardState(byte* keyboard);



   /// <summary>Translates using flag 4 so dead keys and normal native text input keep their original state.</summary>
   [LibraryImport("user32.dll")]
   [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
   private static unsafe partial int ToUnicode(uint key, uint scan, byte* keyboard, char* text, int count, uint flags);
}
