/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Extends published AppKit's About body with linked contributions and complete dependency credits.</summary>
internal static class AppAbout
{
   /// <summary>Preserves AppKit's header/build identity and license, then adds contributions inside the same scrolling body.</summary>
   public static AboutDialog Create(XamlRoot root)
   {
      var dialog = new AboutDialog(AppIdentity.Info with { Attributions = [] }) { XamlRoot = root };
      var scroll = (ScrollViewer)dialog.Content;
      var body = (StackPanel)scroll.Content;
      body.MaxWidth = 580;
      scroll.MaxHeight = Math.Max(160, Math.Min(460, root.Size.Height - 220));
      dialog.Resources["ContentDialogMaxWidth"] = 680d;
      body.Children.Add(Link("AboutApplicationLicense", "MarkdownViewr MIT license", AppIdentity.Info.RepositoryUrl!.AbsoluteUri + "/blob/main/LICENSE"));
      body.Children.Add(Contributions());
      body.Children.Add(Components("Components", "AboutComponents", AboutComponents.OpenSource));
      return dialog;
   }



   /// <summary>Original application, themes and adapted code are credited below the normal app header, rather than as a separate top banner.</summary>
   private static StackPanel Contributions()
   {
      StackPanel section = Section("Contributions", "AboutContributions");
      section.Children.Add(new TextBlock { Text = "Based on Darin Kelkhoff's macOS markdownViewr. Original application, rendering resources and theme work: Copyright (c) 2026 Darin Kelkhoff; MIT License.", TextWrapping = TextWrapping.Wrap });
      section.Children.Add(Link("AboutOriginalRepository", "Original Mac app by Darin Kelkhoff", AppIdentity.OriginalRepositoryUrl.AbsoluteUri));
      section.Children.Add(Link("AboutOriginalAuthorWebsite", "Darin Kelkhoff's website — kelkhoff.com", AppIdentity.OriginalAuthorWebsite.AbsoluteUri));
      section.Children.Add(Link("AboutOriginalLicense", "Original markdownViewr MIT license", AppIdentity.OriginalRepositoryUrl.AbsoluteUri + "/blob/main/LICENSE"));
      return section;
   }



   /// <summary>Each package is identified separately with its locked version and both official project and license links.</summary>
   private static StackPanel Components(string title, string identity, IReadOnlyList<AboutComponent> entries)
   {
      StackPanel section = Section(title, identity);
      foreach(AboutComponent component in entries)
      {
         var entry = new StackPanel { Spacing = 2, Margin = new Thickness(0, 4, 0, 4) };
         entry.Children.Add(Link("AboutProject:" + component.Name, component.Name + " " + component.Version, component.ProjectUrl));
         entry.Children.Add(Link("AboutLicense:" + component.Name, "License: " + component.License, component.LicenseUrl));
         section.Children.Add(entry);
      }
      return section;
   }



   /// <summary>Native heading and panel provide normal keyboard and screen-reader ordering within the scroll view.</summary>
   private static StackPanel Section(string title, string identity)
   {
      var section = new StackPanel { Spacing = 4 };
      AutomationProperties.SetAutomationId(section, identity);
      AutomationProperties.SetName(section, title);
      section.Children.Add(new TextBlock { Text = title, FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
      return section;
   }



   /// <summary>Only curated HTTPS destinations enter About; native hyperlink controls expose their names and Invoke behavior.</summary>
   private static HyperlinkButton Link(string identity, string label, string destination)
   {
      var link = new HyperlinkButton { Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap }, NavigateUri = new Uri(destination), Padding = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Left };
      AutomationProperties.SetAutomationId(link, identity);
      AutomationProperties.SetName(link, label);
      return link;
   }
}
