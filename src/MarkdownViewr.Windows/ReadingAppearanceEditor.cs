/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CoreAppearance = MarkdownViewr.Windows.Core.Themes.ReadingAppearance;


namespace MarkdownViewr.Windows;

/// <summary>Native routine layout fields and an advanced CSS draft validate before checked persistence.</summary>
internal sealed class ReadingAppearanceEditor
{
   private readonly ToggleSwitch _limit;
   private readonly NumberBox _width;
   private readonly TextBox _css;
   private readonly TextBlock _error = new() { TextWrapping = TextWrapping.Wrap };



   /// <summary>Native fields keep routine layout changes separate from unsubmitted CSS and print data.</summary>
   public ReadingAppearanceEditor(CoreAppearance current)
   {
      _limit = new ToggleSwitch { Header = "Limit reading width", IsOn = current.LimitWidth };
      Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(_limit, "ReadingAppearanceLimit");
      _width = new NumberBox { Header = "Maximum content width (pixels at 100%)", Minimum = 200, Maximum = 4000, SmallChange = 50, LargeChange = 200, Value = current.WidthPixels };
      Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(_width, "ReadingAppearanceWidth");
      _css = new TextBox
      {
         Header = "Global custom CSS",
         Text = current.GlobalCss,
         AcceptsReturn = true,
         TextWrapping = TextWrapping.Wrap,
         FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
         MaxLength = CoreAppearance.MaximumCssCharacters,
         Height = 160,
      };
      Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(_css, "ReadingAppearanceCss");
      Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(_error, "ReadingAppearanceError");
      Fields.Children.Add(_limit);
      Fields.Children.Add(_width);
      Fields.Children.Add(new TextBlock { Text = "Global CSS applies across document themes. Per-theme CSS takes precedence. Print layout has its own settings.", TextWrapping = TextWrapping.Wrap });
      Fields.Children.Add(_css);
      Fields.Children.Add(_error);
      _limit.Toggled += (_, _) => LayoutChanged?.Invoke(this, EventArgs.Empty);
      _width.ValueChanged += (_, _) => LayoutChanged?.Invoke(this, EventArgs.Empty);
   }



   /// <summary>The native form remains alive while navigating categories or editing a document theme.</summary>
   public StackPanel Fields { get; } = new() { Spacing = 12 };



   /// <summary>The unsubmitted CSS draft stays local even when other readers save new shared preferences.</summary>
   public string CssDraft => _css.Text;



   /// <summary>Shared saved layout updates native fields without replacing a deliberately edited CSS draft.</summary>
   public void Update(CoreAppearance appearance, bool preserveCss)
   {
      _limit.IsOn = appearance.LimitWidth;
      _width.Value = appearance.WidthPixels;
      if(!preserveCss)
      {
         _css.Text = appearance.GlobalCss;
      }
   }



   /// <summary>Prevents concurrent edits during a checked save while retaining ordinary native disabled states.</summary>
   public void SetEnabled(bool enabled)
   {
      _limit.IsEnabled = enabled;
      _width.IsEnabled = enabled;
      _css.IsEnabled = enabled;
   }



   /// <summary>Routine layout edits can be saved independently from the unsubmitted advanced CSS draft.</summary>
   public event EventHandler? LayoutChanged;



   /// <summary>Routine width changes preserve the last saved CSS until the user explicitly applies a new draft.</summary>
   public CoreAppearance? ReadLayout(string savedCss) => Validate(new CoreAppearance(_limit.IsOn, _width.Value, savedCss));



   /// <summary>Invalid input keeps Settings open and directs focus to a readable error.</summary>
   public CoreAppearance? Read() => Validate(new CoreAppearance(_limit.IsOn, _width.Value, _css.Text));



   /// <summary>Invalid native fields produce a visible, focusable validation message without saving a partial draft.</summary>
   private CoreAppearance? Validate(CoreAppearance draft)
   {
      try
      {
         draft.Validate();
         _error.Text = "";
         return draft;
      }
      catch(InvalidOperationException exception)
      {
         _error.Text = exception.Message;
         _error.StartBringIntoView();
         return null;
      }
   }
}
