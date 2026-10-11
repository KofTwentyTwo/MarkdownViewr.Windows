/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json.Serialization;


namespace MarkdownViewr.Windows;


/// <summary>Activation JSON is a fixed generated schema with unknown fields rejected, rather than a general command protocol.</summary>
[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(ActivationRequest))]
internal sealed partial class ActivationJsonContext : JsonSerializerContext;
