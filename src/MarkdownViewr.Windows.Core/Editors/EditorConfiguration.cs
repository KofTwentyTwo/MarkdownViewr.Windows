/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Editors;

/// <summary>A stable named native editor choice; folder mode passes the document parent instead of the file.</summary>
public sealed record EditorConfiguration(Guid Id, string Name, string Executable, bool OpensFolder = false)
{
   /// <summary>The maximum persisted and native editor command count.</summary>
   public const int MaximumEditors = 32;



   /// <summary>Stored choices cannot introduce empty identities, unbounded labels or executable command strings.</summary>
   public static bool IsValid(EditorConfiguration? editor)
      => editor is not null && editor.Id != Guid.Empty && editor.Name is { Length: > 0 and <= 80 } &&
         !string.IsNullOrWhiteSpace(editor.Name) && !editor.Name.Any(char.IsControl) && EditorLaunch.IsExecutablePath(editor.Executable);



   /// <summary>Preserves valid order/identities and missing installed editors; bounds and repairs untrusted saved settings.</summary>
   public static EditorConfiguration[] Sanitize(IEnumerable<EditorConfiguration?>? editors)
   {
      var seen = new HashSet<Guid>();
      return (editors ?? []).Take(MaximumEditors).Where(editor => IsValid(editor) && seen.Add(editor!.Id)).Select(editor => editor! with { Name = editor.Name.Trim() }).ToArray();
   }
}
