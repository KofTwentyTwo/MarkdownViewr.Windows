#Requires -Version 7.4
# Copyright (c) 2026 James Maes
# SPDX-License-Identifier: MIT

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Describe 'Installed update smoke boundaries' `
{
   BeforeAll `
   {
      $script:SmokeHarness = Join-Path $PSScriptRoot 'Test-InstalledUpdates.ps1'
      $script:SmokeArtifacts = Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts'
      $script:SmokeAppId = [regex]::Match((Get-Content -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'src/MarkdownViewr.Windows.Core/AppIdentity.cs') -Raw), '(?m)^\s*Id\s*=\s*"([a-z0-9][a-z0-9-]*)"').Groups[1].Value
      $ast = [Management.Automation.Language.Parser]::ParseFile($script:SmokeHarness, [ref]$null, [ref]$null)
      foreach($name in @('Assert-SmokeCliVersion', 'Get-SmokeDefaultChoiceSnapshot', 'Get-SmokeFileRegistration', 'Assert-SmokeFileRegistrationRemoved', 'Assert-SmokeInstallationOwner'))
      {
         $function = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true)
         . ([scriptblock]::Create($function.Extent.Text))
      }
   }

   It 'requires an actual matching manifest inside the resolved artifact boundary before uninstall' `
   {
      $root = Join-Path $TestDrive 'artifacts'
      $install = Join-Path $root 'owned installation'
      New-Item -ItemType Directory -Path (Join-Path $install 'current') -Force | Out-Null
      $manifest = Join-Path $install 'current/sq.version'
      '<package><metadata><id>owned-stable</id><version>0.4.0</version></metadata></package>' | Set-Content -LiteralPath $manifest
      { Assert-SmokeInstallationOwner -Install $install -PackageIdentity 'owned-stable' -ArtifactRoot $root } | Should -Not -Throw
      { Assert-SmokeInstallationOwner -Install $install -PackageIdentity 'owned-dev' -ArtifactRoot $root } | Should -Throw '*unexpected application*'
      { Assert-SmokeInstallationOwner -Install (Join-Path $root '../outside') -PackageIdentity 'owned-stable' -ArtifactRoot $root } | Should -Throw '*outside the smoke artifact boundary*'
      { Assert-SmokeInstallationOwner -Install (Join-Path $TestDrive 'artifacts-other') -PackageIdentity 'owned-stable' -ArtifactRoot $root } | Should -Throw '*outside the smoke artifact boundary*'
      { Assert-SmokeInstallationOwner -Install (Join-Path $root 'missing') -PackageIdentity 'owned-stable' -ArtifactRoot $root } | Should -Throw
      $manifest | Should -Exist
   }

   It 'accepts CLI version without Git metadata outside a checkout' `
   {
      { Assert-SmokeCliVersion -Output "Public API true 0.1.0`r`n" -Version '0.1.0' } | Should -Not -Throw
   }

   It 'accepts a prerelease version with SourceLink metadata' `
   {
      { Assert-SmokeCliVersion -Output "Public API true 0.3.0-beta.1 (123456789)`r`n" -Version '0.3.0-beta.1' } | Should -Not -Throw
   }

   It 'rejects an extended or different version token: <Actual>' -ForEach @(
      @{ Actual = '0.1.01' }, @{ Actual = '0.1.0-beta.1' }, @{ Actual = '0.4.0' }
   ) `
   {
      { Assert-SmokeCliVersion -Output "Public API true $Actual (123456789)`r`n" -Version '0.1.0' } | Should -Throw '*incorrect version*'
   }

   It 'refuses output outside the repository artifact boundary before running tools' `
   {
      { & $script:SmokeHarness -OutputDirectory (Join-Path $TestDrive 'outside') } | Should -Throw '*inside this repository artifacts*'
      Join-Path $TestDrive 'outside' | Should -Not -Exist
   }

   It 'refuses an identity mismatch that would fail to isolate the real user profile' `
   {
      $output = Join-Path $script:SmokeArtifacts ('identity-rejected-' + [guid]::NewGuid().ToString('N'))
      { & $script:SmokeHarness -AppId 'incorrect-smoke-identity' -OutputDirectory $output } | Should -Throw '*AppId must match*'
      $output | Should -Not -Exist
   }

   It 'refuses existing output instead of overwriting or consuming stale results' `
   {
      $output = Join-Path $script:SmokeArtifacts ('existing-rejected-' + [guid]::NewGuid().ToString('N'))
      New-Item -ItemType Directory -Path $output | Out-Null
      try
      {
         { & $script:SmokeHarness -AppId $script:SmokeAppId -VpkPath (Get-Command dotnet -CommandType Application).Source -OutputDirectory $output } | Should -Throw '*fresh smoke output*'
         @(Get-ChildItem -LiteralPath $output).Count | Should -Be 0
      }
      finally { Remove-Item -LiteralPath $output }
   }
}


Describe 'Actual registry readbacks for installed file types' `
{
   BeforeAll `
   {
      $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-InstalledUpdates.ps1'), [ref]$null, [ref]$null)
      foreach($name in @('Get-SmokeDefaultChoiceSnapshot', 'Get-SmokeFileRegistration', 'Assert-SmokeFileRegistrationRemoved'))
      {
         $function = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true)
         . ([scriptblock]::Create($function.Extent.Text))
      }
   }

   BeforeEach `
   {
      $script:FixtureRelative = 'Software\KofTwentyTwo\MarkdownViewr.Windows.SmokeRegistrationTests\' + [guid]::NewGuid().ToString('N')
      $script:FixtureRoot = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($script:FixtureRelative)
      $script:Exe = Join-Path $TestDrive 'installed app/current/MarkdownViewr.Windows.exe'
      $script:Identity = 'KofTwentyTwo.MarkdownViewr.stable.fixture'
      $script:Capabilities = "Software\KofTwentyTwo\MarkdownViewr\FileAssociations\$script:Identity\Capabilities"
      $key = $script:FixtureRoot.CreateSubKey('Software\RegisteredApplications')
      $key.SetValue($script:Identity, $script:Capabilities)
      $key.Dispose()
      $key = $script:FixtureRoot.CreateSubKey("Software\Classes\$script:Identity\shell\open\command")
      $key.SetValue('', '"' + $script:Exe + '" "%1"')
      $key.Dispose()
      $key = $script:FixtureRoot.CreateSubKey("$script:Capabilities\FileAssociations")
      foreach($extension in @('.md', '.markdown', '.mdown', '.mkd', '.txt')) { $key.SetValue($extension, $script:Identity) }
      $key.Dispose()
      foreach($extension in @('.md', '.markdown', '.mdown', '.mkd', '.txt'))
      {
         $key = $script:FixtureRoot.CreateSubKey("Software\Classes\$extension")
         $key.SetValue('', 'Other.Reader')
         $key.Dispose()
         $key = $script:FixtureRoot.CreateSubKey("Software\Classes\$extension\OpenWithProgids")
         $key.SetValue($script:Identity, '')
         $key.SetValue('Other.Reader', '')
         $key.Dispose()
         $key = $script:FixtureRoot.CreateSubKey("Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\$extension\UserChoice")
         $key.SetValue('ProgId', 'Other.Reader')
         $key.SetValue('Hash', 'existing-choice-fixture')
         $key.Dispose()
      }
   }

   AfterEach `
   {
      $script:FixtureRoot.Dispose()
      [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($script:FixtureRelative)
   }

   It 'recognizes only the exact installed executable and channel without changing defaults' `
   {
      $before = Get-SmokeDefaultChoiceSnapshot -RegistryRoot $script:FixtureRoot | ConvertTo-Json -Depth 3 -Compress
      $registration = Get-SmokeFileRegistration -Exe $script:Exe -Channel stable -RegistryRoot $script:FixtureRoot
      $registration.ProgId | Should -BeExactly $script:Identity
      $registration.ExtensionsVerified | Should -Be 5
      { Get-SmokeFileRegistration -Exe $script:Exe -Channel dev -RegistryRoot $script:FixtureRoot } | Should -Throw '*exactly one*'
      { Get-SmokeFileRegistration -Exe (Join-Path $TestDrive 'other.exe') -Channel stable -RegistryRoot $script:FixtureRoot } | Should -Throw '*exactly one*'
      (Get-SmokeDefaultChoiceSnapshot -RegistryRoot $script:FixtureRoot | ConvertTo-Json -Depth 3 -Compress) | Should -BeExactly $before
   }

   It 'rejects a missing Open With registration for <MissingExtension>' -ForEach @(
      @{ MissingExtension = '.md' }, @{ MissingExtension = '.markdown' }, @{ MissingExtension = '.mdown' }, @{ MissingExtension = '.mkd' }, @{ MissingExtension = '.txt' }
   ) `
   {
      $key = $script:FixtureRoot.OpenSubKey("Software\Classes\$MissingExtension\OpenWithProgids", $true)
      $key.DeleteValue($script:Identity)
      $key.Dispose()
      { Get-SmokeFileRegistration -Exe $script:Exe -Channel stable -RegistryRoot $script:FixtureRoot } | Should -Throw '*Open With extension*'
   }

   It 'rejects an advertised extension mapped to a different application' `
   {
      $key = $script:FixtureRoot.OpenSubKey("$script:Capabilities\FileAssociations", $true)
      $key.SetValue('.txt', 'Other.Reader')
      $key.Dispose()
      { Get-SmokeFileRegistration -Exe $script:Exe -Channel stable -RegistryRoot $script:FixtureRoot } | Should -Throw '*Default Apps extension*'
   }

   It 'requires complete owned cleanup while leaving unrelated Open With and defaults intact' `
   {
      $before = Get-SmokeDefaultChoiceSnapshot -RegistryRoot $script:FixtureRoot | ConvertTo-Json -Depth 3 -Compress
      $registration = Get-SmokeFileRegistration -Exe $script:Exe -Channel stable -RegistryRoot $script:FixtureRoot
      { Assert-SmokeFileRegistrationRemoved -Registration $registration -RegistryRoot $script:FixtureRoot } | Should -Throw '*registration remains*'
      $script:FixtureRoot.DeleteSubKeyTree("Software\Classes\$script:Identity")
      $script:FixtureRoot.DeleteSubKeyTree($script:Capabilities)
      $key = $script:FixtureRoot.OpenSubKey('Software\RegisteredApplications', $true)
      $key.DeleteValue($script:Identity)
      $key.Dispose()
      { Assert-SmokeFileRegistrationRemoved -Registration $registration -RegistryRoot $script:FixtureRoot } | Should -Throw '*Open With entry remains*'
      foreach($extension in @('.md', '.markdown', '.mdown', '.mkd', '.txt'))
      {
         $key = $script:FixtureRoot.OpenSubKey("Software\Classes\$extension\OpenWithProgids", $true)
         $key.DeleteValue($script:Identity)
         $key.GetValue('Other.Reader') | Should -BeExactly ''
         $key.Dispose()
      }
      { Assert-SmokeFileRegistrationRemoved -Registration $registration -RegistryRoot $script:FixtureRoot } | Should -Not -Throw
      (Get-SmokeDefaultChoiceSnapshot -RegistryRoot $script:FixtureRoot | ConvertTo-Json -Depth 3 -Compress) | Should -BeExactly $before
   }
}
