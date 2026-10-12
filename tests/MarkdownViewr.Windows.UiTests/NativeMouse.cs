/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using FlaUI.Core.WindowsAPI;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Injects pointer movement through the same Windows input queue used for button events.</summary>
internal static class NativeMouse
{
   /// <summary>Uses absolute virtual-desktop coordinates; cursor warping alone did not deliver outline movement in hosted traces.</summary>
   public static void MoveTo(Point point)
   {
      int left = User32.GetSystemMetrics(SystemMetric.SM_XVIRTUALSCREEN);
      int top = User32.GetSystemMetrics(SystemMetric.SM_YVIRTUALSCREEN);
      int width = User32.GetSystemMetrics(SystemMetric.SM_CXVIRTUALSCREEN);
      int height = User32.GetSystemMetrics(SystemMetric.SM_CYVIRTUALSCREEN);
      if(width <= 0 || height <= 0 || point.X < left || point.X >= left + width || point.Y < top || point.Y >= top + height)
      {
         throw new InvalidOperationException("The native mouse target is outside the virtual desktop.");
      }
      var mouse = new MOUSEINPUT
      {
         dx = (int)(((long)(point.X - left) * 65536 + 32768) / width),
         dy = (int)(((long)(point.Y - top) * 65536 + 32768) / height),
         dwFlags = MouseEventFlags.MOUSEEVENTF_MOVE | MouseEventFlags.MOUSEEVENTF_ABSOLUTE | MouseEventFlags.MOUSEEVENTF_VIRTUALDESK,
      };
      var input = INPUT.MouseInput(mouse);
      if(User32.SendInput(1, [input], INPUT.Size) != 1)
      {
         throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows did not accept the native mouse move.");
      }
   }
}
