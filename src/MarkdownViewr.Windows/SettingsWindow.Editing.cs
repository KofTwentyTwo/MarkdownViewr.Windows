/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Long configuration forms use the normal Settings window, with native Back and explicit draft actions.</summary>
internal sealed partial class SettingsWindow
{
   private readonly StackPanel _editActions = new() { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(24, 12, 24, 12), Visibility = Visibility.Collapsed };
   private TaskCompletionSource<ContentDialogResult>? _editCompletion;



   /// <summary>Window closure cancels the draft; configuration callers cannot continue editing a closed owner.</summary>
   internal bool IsOpen => !_closed;



   /// <summary>Shows one scrollable form within Settings and retains invalid drafts until validation succeeds or the user cancels.</summary>
   internal Task<ContentDialogResult> ShowEditorAsync(string title, UIElement fields, string saveLabel, Func<bool>? validate = null)
   {
      if(_closed)
      {
         return Task.FromResult(ContentDialogResult.None);
      }
      if(_editCompletion is not null)
      {
         throw new InvalidOperationException("A Settings edit is already open.");
      }
      var completion = new TaskCompletionSource<ContentDialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
      _editCompletion = completion;
      _navigation.Header = title;
      _navigation.IsBackButtonVisible = NavigationViewBackButtonVisible.Visible;
      foreach(NavigationViewItem item in _navigation.MenuItems.Cast<NavigationViewItem>())
      {
         item.IsEnabled = false;
      }
      _pageScroll.Content = null;
      _pageScroll.Content = fields;
      _pageScroll.ChangeView(null, 0, null, disableAnimation: true);
      var save = new Button { Content = saveLabel, MinWidth = 120 };
      var cancel = new Button { Content = "Cancel", MinWidth = 120 };
      AutomationProperties.SetAutomationId(save, "PrimaryButton");
      AutomationProperties.SetAutomationId(cancel, "CloseButton");
      save.Click += (_, _) =>
      {
         if(validate?.Invoke() != false)
         {
            EndEdit(ContentDialogResult.Primary);
         }
      };
      cancel.Click += (_, _) => EndEdit(ContentDialogResult.None);
      _editActions.Children.Clear();
      _editActions.Children.Add(save);
      _editActions.Children.Add(cancel);
      _editActions.Visibility = Visibility.Visible;
      return completion.Task;
   }



   /// <summary>Detaches the edit before completing its task, allowing a manager to open another page while preserving its copied draft.</summary>
   internal void EndEdit(ContentDialogResult result)
   {
      TaskCompletionSource<ContentDialogResult>? completion = _editCompletion;
      if(completion is null)
      {
         return;
      }
      _editCompletion = null;
      _pageScroll.Content = null;
      _editActions.Children.Clear();
      _editActions.Visibility = Visibility.Collapsed;
      if(!_closed)
      {
         _navigation.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
         foreach(NavigationViewItem item in _navigation.MenuItems.Cast<NavigationViewItem>())
         {
            item.IsEnabled = true;
         }
         SelectPage((string)((NavigationViewItem)_navigation.SelectedItem).Tag);
      }
      completion.TrySetResult(result);
   }
}
