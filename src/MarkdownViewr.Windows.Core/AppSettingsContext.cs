/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using System.Text.Json.Serialization;


namespace MarkdownViewr.Windows.Core;

/// <summary>
/// Source-generated JSON serializer for <see cref="AppSettings"/>, so trimmed builds need no reflection.
/// Reading tolerates what a hand edit commonly leaves behind: a trailing comma, a comment, a quoted number.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip, NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(AppSettings))]
public sealed partial class AppSettingsContext : JsonSerializerContext
{
}
