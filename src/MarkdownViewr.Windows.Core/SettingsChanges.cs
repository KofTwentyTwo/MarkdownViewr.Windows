/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using System.Text.Json.Nodes;


namespace MarkdownViewr.Windows.Core;

/// <summary>Merges only a document window's changed preferences so its older snapshot cannot overwrite sibling settings.</summary>
public static class SettingsChanges
{
   /// <summary>Copies the source-generated settings contract without reflection or shared mutable arrays.</summary>
   public static AppSettings Copy(AppSettings settings)
      => JsonSerializer.SerializeToNode(settings, AppSettingsContext.Default.AppSettings)!.Deserialize(AppSettingsContext.Default.AppSettings)!;



   /// <summary>Preserves current store values for every property the window has not changed since its last successful save.</summary>
   public static AppSettings Merge(AppSettings current, AppSettings before, AppSettings edited)
   {
      JsonObject result = JsonSerializer.SerializeToNode(current, AppSettingsContext.Default.AppSettings)!.AsObject();
      JsonObject baseline = JsonSerializer.SerializeToNode(before, AppSettingsContext.Default.AppSettings)!.AsObject();
      JsonObject changes = JsonSerializer.SerializeToNode(edited, AppSettingsContext.Default.AppSettings)!.AsObject();
      foreach((string name, JsonNode? value) in changes)
      {
         if(!JsonNode.DeepEquals(value, baseline[name]))
         {
            result[name] = value?.DeepClone();
         }
      }
      return result.Deserialize(AppSettingsContext.Default.AppSettings)!;
   }
}
