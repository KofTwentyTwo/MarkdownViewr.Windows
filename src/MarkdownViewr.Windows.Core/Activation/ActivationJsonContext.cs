/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json.Serialization;


namespace MarkdownViewr.Windows.Core.Activation;

/// <summary>The fixed schema of an activation message; a field it does not define is an error, so the message cannot grow into a command protocol.</summary>
[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(ActivationRequest))]
internal sealed partial class ActivationJsonContext : JsonSerializerContext;
