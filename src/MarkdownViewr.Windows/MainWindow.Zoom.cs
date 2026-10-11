/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Owns native menu/toolbar reading zoom without changing independent windows or persisted theme data.</summary>
public sealed partial class MainWindow
{
   private readonly Core.Navigation.ReaderZoom _readerZoom = new();
   private bool _zooming;



   /// <summary>Refreshes native controls only after the matching ready document has completed state restoration.</summary>
   private void InitializeZoom()
   {
      _browser.DocumentReady += (_, _) => RefreshZoomCommands();
      RefreshZoomCommands();
   }



   /// <summary>Native menus and compact toolbar buttons use the same bounded command path.</summary>
   private async void Zoom_Click(object sender, RoutedEventArgs args)
   {
      if(sender is FrameworkElement { Tag: string value } && Enum.TryParse(value, out Core.Navigation.ReaderCommand command))
      {
         await ChangeZoomAsync(command);
      }
   }



   /// <summary>Serializes font-size changes and lets existing ready-page find/source handlers restore current state.</summary>
   private async Task ChangeZoomAsync(Core.Navigation.ReaderCommand command)
   {
      if(_closed || _zooming || !_browser.IsReady || _documentPath is null)
      {
         return;
      }
      _zooming = true;
      try
      {
         _readerZoom.Apply(command);
         await _browser.ApplyZoomAsync(_readerZoom.Scale);
         HoldSourceSync(preserveSelection: false);
      }
      catch(Exception exception) when(exception is System.Runtime.InteropServices.COMException or ObjectDisposedException or InvalidOperationException)
      {
         if(!_closed)
         {
            App.Log.Warning("The reading zoom could not be changed.");
            StatusText.Text = "The reading zoom could not be changed.";
         }
      }
      finally
      {
         _zooming = false;
         RefreshZoomCommands();
      }
   }



   /// <summary>The rounded percentage is an accessible reset control; actual rendering is checked separately.</summary>
   private void RefreshZoomCommands()
   {
      bool enabled = !_closed && !_zooming && _documentPath is not null && _browser.IsReady;
      foreach(Control control in new Control[] { ZoomInMenu, ZoomOutMenu, ZoomResetMenu, ZoomInButton, ZoomOutButton, ZoomResetButton })
      {
         control.IsEnabled = enabled;
      }
      string percent = Math.Round(_readerZoom.Scale * 100).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
      ZoomResetButton.Content = percent;
      Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(ZoomResetButton, "Actual size (" + percent + ")");
   }
}
