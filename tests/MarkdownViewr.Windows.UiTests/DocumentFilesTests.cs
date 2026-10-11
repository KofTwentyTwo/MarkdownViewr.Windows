/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using System.Text;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Exercises bounded snapshots and physical resource containment without launching the GUI.</summary>
public sealed class DocumentFilesTests : IDisposable
{
   private readonly string _root = Path.Combine(Path.GetTempPath(), "markdownviewr-files-" + Guid.NewGuid().ToString("N"));



   /// <summary>Creates a private document folder and a sibling outside the resource boundary.</summary>
   public DocumentFilesTests()
   {
      Directory.CreateDirectory(Path.Combine(_root, "document"));
      Directory.CreateDirectory(Path.Combine(_root, "outside"));
   }



   /// <summary>Timestamp and byte length belong to the loaded handle, and remain unchanged when the file is replaced later.</summary>
   [Fact]
   public void Open_ModifiedDocument_RetainsLoadedSnapshotMetadata()
   {
      string path = Path.Combine(_root, "document", "timestamp.md");
      File.WriteAllText(path, "# Original", Encoding.UTF8);
      DateTime timestamp = new(2026, 1, 2, 3, 4, 6, DateTimeKind.Utc);
      File.SetLastWriteTimeUtc(path, timestamp);
      var before = new FileInfo(path);
      var snapshot = DocumentFiles.Open(path);
      Assert.Equal(before.LastWriteTimeUtc, snapshot.ModifiedUtc);
      Assert.Equal(before.Length, snapshot.ByteLength);
      File.WriteAllText(path, "# A different longer snapshot", Encoding.UTF8);
      File.SetLastWriteTimeUtc(path, timestamp.AddMinutes(2));
      var current = DocumentFiles.Open(path);
      Assert.Equal("# Original", snapshot.Markdown);
      Assert.Equal(timestamp, snapshot.ModifiedUtc);
      Assert.NotEqual(snapshot.ModifiedUtc, current.ModifiedUtc);
      Assert.NotEqual(snapshot.ByteLength, current.ByteLength);
   }



   /// <summary>File loading honors the BOM and reads the selected document snapshot.</summary>
   [Fact]
   public void Open_UnicodeDocument_ReadsTheSnapshot()
   {
      string path = Path.Combine(_root, "document", "read me.md");
      File.WriteAllText(path, "# Café", Encoding.Unicode);
      var files = DocumentFiles.Open(path);
      Assert.Equal("# Café", files.Markdown);
   }



   /// <summary>A loaded snapshot does not keep the source file open or hide its subsequent deletion.</summary>
   [Fact]
   public void Open_DeletedSource_ThrowsFileNotFound()
   {
      string path = Path.Combine(_root, "document", "read.md");
      File.WriteAllText(path, "# Snapshot");
      var files = DocumentFiles.Open(path);
      File.Delete(path);
      Assert.Equal("# Snapshot", files.Markdown);
      Assert.Throws<FileNotFoundException>(() => DocumentFiles.Open(path));
   }



   /// <summary>Ordinary relative images inside the selected folder retain their exact bytes.</summary>
   [Fact]
   public void ReadImage_InsideDocumentFolder_ReturnsExactBytes()
   {
      DocumentFiles files = CreateDocument();
      byte[] expected = [1, 2, 3, 4];
      File.WriteAllBytes(Path.Combine(_root, "document", "image.png"), expected);
      Assert.Equal(expected, files.ReadImage("image.png"));
   }



   /// <summary>Physical containment also rejects traversal if a caller bypasses the earlier URI policy.</summary>
   [Fact]
   public void ReadImage_OutsideDocumentFolder_ThrowsBeforeReading()
   {
      DocumentFiles files = CreateDocument();
      File.WriteAllBytes(Path.Combine(_root, "outside", "secret.png"), [1, 2, 3]);
      Assert.Throws<IOException>(() => files.ReadImage(Path.Combine("..", "outside", "secret.png")));
   }



   /// <summary>An allowed-looking image path cannot use a directory junction to escape the document root.</summary>
   [Fact]
   public void ReadImage_JunctionOutsideDocumentFolder_ThrowsBeforeReading()
   {
      DocumentFiles files = CreateDocument();
      string link = Path.Combine(_root, "document", "images");
      File.WriteAllBytes(Path.Combine(_root, "outside", "secret.png"), [1, 2, 3]);
      var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true };
      start.ArgumentList.Add("/c");
      start.ArgumentList.Add("mklink");
      start.ArgumentList.Add("/J");
      start.ArgumentList.Add(link);
      start.ArgumentList.Add(Path.Combine(_root, "outside"));
      using Process process = Process.Start(start)!;
      Assert.True(process.WaitForExit(10000), "Junction creation did not finish.");
      Assert.Equal(0, process.ExitCode);
      try
      {
         Assert.Throws<IOException>(() => files.ReadImage(Path.Combine("images", "secret.png")));
      }
      finally
      {
         Directory.Delete(link);
      }
   }



   /// <summary>A document above the parser's bound is rejected before parsing.</summary>
   [Fact]
   public void Open_OverCharacterLimit_Throws()
   {
      string path = Path.Combine(_root, "document", "large.md");
      File.WriteAllText(path, new string('x', Core.Documents.MarkdownRenderer.MaximumCharacters + 1));
      Assert.Throws<IOException>(() => DocumentFiles.Open(path));
   }



   /// <summary>Oversized images do not allocate an unbounded browser response.</summary>
   [Fact]
   public void ReadImage_OverByteLimit_Throws()
   {
      DocumentFiles files = CreateDocument();
      string path = Path.Combine(_root, "document", "large.png");
      using(var stream = new FileStream(path, FileMode.CreateNew))
      {
         stream.SetLength(16 * 1024 * 1024 + 1);
      }
      Assert.Throws<IOException>(() => files.ReadImage("large.png"));
   }



   /// <summary>Creates the source file and returns its established physical resource capability.</summary>
   private DocumentFiles CreateDocument()
   {
      string path = Path.Combine(_root, "document", "read.md");
      File.WriteAllText(path, "# Document");
      return DocumentFiles.Open(path);
   }



   /// <summary>Deletes only the unique temporary folder owned by this test instance.</summary>
   public void Dispose() => Directory.Delete(_root, recursive: true);
}
