/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>The actual restored package inventory and credit destinations must stay in sync when dependencies change.</summary>
public sealed class AboutComponentsTests
{
   /// <summary>Every application/build/test package restored for this repository is acknowledged without omitted transitive libraries.</summary>
   [Fact]
   public void Catalog_RestoredAssets_CoversEveryPackage()
   {
      string root = FindRepository();
      string[] paths = ["src/MarkdownViewr.Windows/obj/project.assets.json", "tests/MarkdownViewr.Windows.Core.Tests/obj/project.assets.json", "tests/MarkdownViewr.Windows.UiTests/obj/project.assets.json"];
      var restored = new HashSet<string>(StringComparer.Ordinal);
      foreach(string path in paths)
      {
         using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, path)));
         foreach(JsonProperty library in assets.RootElement.GetProperty("libraries").EnumerateObject())
         {
            if(string.Equals(library.Value.GetProperty("type").GetString(), "package", StringComparison.Ordinal))
            {
               restored.Add(library.Name);
            }
         }
      }
      AboutComponent[] credits = AboutComponents.Runtime.Concat(AboutComponents.Development).ToArray();
      var credited = credits.Where(credit => !string.Equals(credit.Name, ".NET Runtime", StringComparison.Ordinal)).Select(credit => credit.Name + "/" + credit.Version).ToHashSet(StringComparer.Ordinal);
      Assert.True(restored.SetEquals(credited), "About credits must match actual restored direct/transitive package versions. Missing: " + string.Join(", ", restored.Except(credited, StringComparer.Ordinal)) + "; stale: " + string.Join(", ", credited.Except(restored, StringComparer.Ordinal)));
      Assert.Equal(credits.Length, credits.Select(credit => credit.Name).Distinct(StringComparer.Ordinal).Count());
      foreach(AboutComponent credit in credits)
      {
         Assert.NotEmpty(credit.License);
         Assert.Equal("https", new Uri(credit.ProjectUrl).Scheme);
         Assert.Equal("https", new Uri(credit.LicenseUrl).Scheme);
         Assert.DoesNotContain("deprecateLicenseUrl", credit.LicenseUrl, StringComparison.Ordinal);
      }
      Assert.Contains(AboutComponents.Runtime, credit => string.Equals(credit.Name, "Microsoft.Web.WebView2", StringComparison.Ordinal));
      Assert.Contains(AboutComponents.Runtime, credit => string.Equals(credit.Name, "AngleSharp.Css", StringComparison.Ordinal));
      Assert.Contains(AboutComponents.Development, credit => string.Equals(credit.Name, "FlaUI.Core", StringComparison.Ordinal));
   }



   /// <summary>Build-produced assets are independent evidence of what the repository currently uses, not a duplicated fixture list.</summary>
   private static string FindRepository()
   {
      for(DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
      {
         if(File.Exists(Path.Combine(directory.FullName, "MarkdownViewr.Windows.slnx")))
         {
            return directory.FullName;
         }
      }
      throw new DirectoryNotFoundException("Restore/build this repository before checking its actual dependency credits.");
   }
}
