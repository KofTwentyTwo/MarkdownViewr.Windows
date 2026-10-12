/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.Json;


namespace MarkdownViewr.Windows.Core.Themes;

/// <summary>Reads only bounded theme JSON; exports retain the validated original schema and metadata.</summary>
public static class ThemeJson
{
   /// <summary>The caller owns the selected stream; an oversized/invalid file never reaches saved settings.</summary>
   public static async Task<ReaderTheme> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(stream);
      using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
      char[] text = new char[ReaderTheme.MaximumCharacters + 1];
      int count = await reader.ReadBlockAsync(text.AsMemory(), cancellationToken).ConfigureAwait(false);
      if(count > ReaderTheme.MaximumCharacters)
      {
         throw new JsonException("The theme exceeds the 128 Ki-character limit.");
      }
      return ReaderTheme.ParseCompatible(new string(text, 0, count));
   }



   /// <summary>Suggests the reference's lowercase hyphenated JSON name without Windows path characters or reserved device names.</summary>
   public static string SuggestedFileName(ReaderTheme theme)
   {
      ArgumentNullException.ThrowIfNull(theme);
      string stem = string.Concat(theme.Name.Select(character => "<>:\"/\\|?*".Contains(character, StringComparison.Ordinal) ? '_' : character))
         .Replace(' ', '-').ToLowerInvariant().Trim('.', '-');
      if(stem.Length == 0)
      {
         stem = "theme";
      }
      string device = stem.Split('.')[0];
      if(device is "con" or "prn" or "aux" or "nul" || device.Length == 4 && (device.StartsWith("com", StringComparison.Ordinal) || device.StartsWith("lpt", StringComparison.Ordinal)) && device[3] is >= '1' and <= '9' or '¹' or '²' or '³')
      {
         stem = "theme-" + stem;
      }
      return stem + ".json";
   }

}
