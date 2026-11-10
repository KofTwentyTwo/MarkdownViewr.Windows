/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Renaming one real file over another, with other real handles open on the destination.</summary>
public sealed class FileSwapTests
{
   /// <summary>The rename replaces the destination and removes the source name.</summary>
   [Fact]
   public void TryRenameOver_ExistingDestination_ReplacesIt()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("stored.json"), "old");
      File.WriteAllText(temp.File("new.tmp"), "new");
      Assert.True(FileSwap.TryRenameOver(temp.File("new.tmp"), temp.File("stored.json")));
      Assert.Equal("new", File.ReadAllText(temp.File("stored.json")));
      Assert.False(File.Exists(temp.File("new.tmp")));
   }



   /// <summary>A reader that allows deletion does not stop the rename, and goes on reading the content it opened.</summary>
   [Fact]
   public void TryRenameOver_DestinationOpenByAReaderThatAllowsDeletion_ReplacesItUnderTheReader()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("stored.json"), "old content");
      File.WriteAllText(temp.File("new.tmp"), "new");
      using(var reader = new StreamReader(new FileStream(temp.File("stored.json"), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete)))
      {
         Assert.True(FileSwap.TryRenameOver(temp.File("new.tmp"), temp.File("stored.json")));
         Assert.Equal("new", File.ReadAllText(temp.File("stored.json")));
         Assert.Equal("old content", reader.ReadToEnd());
      }
      Assert.Equal(["stored.json"], Directory.GetFiles(temp.Path).Select(file => Path.GetFileName(file)!), StringComparer.Ordinal);
   }



   /// <summary>A handle that does not allow deletion refuses the rename with a sharing violation, and both files stay as they were.</summary>
   [Fact]
   public void TryRenameOver_DestinationOpenWithoutDeleteSharing_IsASharingViolation()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("stored.json"), "old");
      File.WriteAllText(temp.File("new.tmp"), "new");
      using(new FileStream(temp.File("stored.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
      {
         IOException refused = Assert.Throws<IOException>(() => FileSwap.TryRenameOver(temp.File("new.tmp"), temp.File("stored.json")));
         Assert.Equal(unchecked((int)0x80070020), refused.HResult);
      }
      Assert.Equal("old", File.ReadAllText(temp.File("stored.json")));
      Assert.Equal("new", File.ReadAllText(temp.File("new.tmp")));
   }



   /// <summary>A read-only destination is access denied, as it is for a managed move.</summary>
   [Fact]
   public void TryRenameOver_ReadOnlyDestination_IsAccessDenied()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("stored.json"), "old");
      File.WriteAllText(temp.File("new.tmp"), "new");
      File.SetAttributes(temp.File("stored.json"), FileAttributes.ReadOnly);
      try
      {
         Assert.Throws<UnauthorizedAccessException>(() => FileSwap.TryRenameOver(temp.File("new.tmp"), temp.File("stored.json")));
      }
      finally
      {
         File.SetAttributes(temp.File("stored.json"), FileAttributes.Normal);
      }
      Assert.Equal("old", File.ReadAllText(temp.File("stored.json")));
   }



   /// <summary>A source that does not exist is reported with the Windows error for a missing file.</summary>
   [Fact]
   public void TryRenameOver_MissingSource_IsAnIoFailure()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("stored.json"), "old");
      IOException missing = Assert.Throws<IOException>(() => FileSwap.TryRenameOver(temp.File("absent.tmp"), temp.File("stored.json")));
      Assert.Equal(unchecked((int)0x80070002), missing.HResult);
      Assert.Equal("old", File.ReadAllText(temp.File("stored.json")));
   }



   /// <summary>A destination that does not exist yet is simply created by the rename.</summary>
   [Fact]
   public void TryRenameOver_NoDestinationYet_RenamesTheSource()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("new.tmp"), "new");
      Assert.True(FileSwap.TryRenameOver(temp.File("new.tmp"), temp.File("stored.json")));
      Assert.Equal("new", File.ReadAllText(temp.File("stored.json")));
   }



   /// <summary>Only the errors a volume gives for an unimplemented rename mean "use another way"; every other error is a failure.</summary>
   [Theory]
   [InlineData(1, true)]
   [InlineData(50, true)]
   [InlineData(87, true)]
   [InlineData(0, false)]
   [InlineData(2, false)]
   [InlineData(5, false)]
   [InlineData(32, false)]
   [InlineData(17, false)]
   public void IsUnsupported_WindowsError_SeparatesAnUnimplementedRenameFromAFailure(int error, bool unsupported)
      => Assert.Equal(unsupported, FileSwap.IsUnsupported(error));
}
