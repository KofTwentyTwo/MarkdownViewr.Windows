#Requires -Version 7.4
# Copyright (c) 2026 James Maes (KofTwentyTwo)
# SPDX-License-Identifier: MIT

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Continue Pester commands so their Allman scriptblocks stay part of the call.
BeforeAll `
{
   $script:CoverageGate = Join-Path $PSScriptRoot 'Assert-Coverage.ps1'

   <#
   .SYNOPSIS
      Writes a synthetic report for one package: one line per entry of -Hits, in one class.
   #>
   function Write-CoverageReport
   {
      [CmdletBinding()]
      param(
         [Parameter(Mandatory)] [string] $Directory,
         [string] $Package = 'Probe',
         [int[]] $Hits = @(1, 1),
         # What the report claims in its summary attribute; the gate must not believe it.
         [string] $StatedRate = '1'
      )

      $number = 0
      $lines = foreach ($count in $Hits) { $number++; "<line number=`"$number`" hits=`"$count`" />" }
      $report = @"
<coverage line-rate="$StatedRate"><packages><package name="$Package" line-rate="$StatedRate"><classes>
<class filename="$Package.cs"><lines>$($lines -join '')</lines></class>
</classes></package></packages></coverage>
"@
      New-Item -ItemType Directory -Force -Path $Directory | Out-Null
      Set-Content -LiteralPath (Join-Path $Directory 'coverage.cobertura.xml') -Value $report
   }

   <#
   .SYNOPSIS
      Runs the gate as CI does, in its own process, and returns its exit code and combined output.
   #>
   function Invoke-CoverageGate
   {
      [CmdletBinding()]
      param([Parameter(ValueFromRemainingArguments)] [string[]] $Arguments)

      $output = (& pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory @Arguments 2>&1) -join "`n"
      [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
   }
}

Describe 'Required coverage gate' `
{
   BeforeEach `
   {
      $script:ReportsDirectory = Join-Path $TestDrive ([guid]::NewGuid().ToString())
      New-Item -ItemType Directory -Path $script:ReportsDirectory | Out-Null
   }

   It 'rejects a run that produced no report' `
   {
      $result = Invoke-CoverageGate -Packages Probe
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match 'No coverage.cobertura.xml'
   }

   It 'rejects an absent required package' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory
      $result = Invoke-CoverageGate -Packages Missing
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match "'Missing' not found"
   }

   It 'does not take a longer or differently cased package name for the required one' `
   {
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'longer') -Package 'ProbeExtra'
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'cased') -Package 'probe'
      $result = Invoke-CoverageGate -Packages Probe
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match "'Probe' not found"
   }

   It 'rejects coverage below the required percentage and names the uncovered lines' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Hits 1, 0, 1, 0
      $result = Invoke-CoverageGate -Packages Probe
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match 'Probe line coverage: 50% \(2 of 4 lines'
      $result.Output | Should -Match 'uncovered: Probe\.cs lines 2, 4(\s|$)'
      $result.Output | Should -Match 'Coverage gate FAILED'
   }

   It 'accepts full coverage of the required package' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory
      $result = Invoke-CoverageGate -Packages Probe
      $result.ExitCode | Should -Be 0
      $result.Output | Should -Match 'Probe line coverage: 100% \(2 of 2 lines'
      $result.Output | Should -Match 'Coverage gate passed'
   }

   It 'counts the lines itself instead of believing the stated rate <StatedRate>' -ForEach @(
      @{ StatedRate = '1' }
      @{ StatedRate = '1.5' }
      @{ StatedRate = 'NaN' }
      @{ StatedRate = '0,5' }
   ) `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Hits 1, 0 -StatedRate $StatedRate
      $result = Invoke-CoverageGate -Packages Probe
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match 'Probe line coverage: 50% \(1 of 2 lines'
   }

   It 'never shows a package just under the bar as having reached it' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Hits (@(1) * 19999 + 0)
      $result = Invoke-CoverageGate -Packages Probe
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match 'Probe line coverage: 99\.99% \(19999 of 20000 lines'
   }

   It 'rejects a package no report measured a line of' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Hits @()
      $result = Invoke-CoverageGate -Packages Probe
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match 'no measured lines'
   }

   It 'ignores an empty report when another test project measured the package' `
   {
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'first') -Hits @()
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'second') -Hits 1, 0
      $result = Invoke-CoverageGate -Packages Probe
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match 'Probe line coverage: 50% \(1 of 2 lines'
   }

   It 'takes the test project that covers the package best' `
   {
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'first') -Hits 1, 0
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'second') -Hits 1, 1
      $result = Invoke-CoverageGate
      $result.ExitCode | Should -Be 0
      $result.Output | Should -Match 'Probe line coverage: 100% \(2 of 2 lines.*second'
   }

   It 'lists uncovered lines only from the report it used' `
   {
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'first') -Hits 0, 0, 0, 1
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'second') -Hits 1, 1, 0, 1
      $result = Invoke-CoverageGate -Packages Probe
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match 'Probe line coverage: 75% \(3 of 4 lines.*second'
      $result.Output | Should -Match 'uncovered: Probe\.cs lines 3(\s|$)'
      $result.Output | Should -Not -Match 'lines 1, 2, 3'
   }

   It 'gates every package named in one comma-separated argument' `
   {
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'first') -Package 'First'
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'second') -Package 'Second' -Hits 1, 0
      $result = Invoke-CoverageGate -Packages 'First,Second'
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match 'First line coverage: 100%'
      $result.Output | Should -Match 'Coverage gate FAILED: Second'
   }

   It 'gates every measured package when none is named' `
   {
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'first') -Package 'First'
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'second') -Package 'Second' -Hits 1, 0
      $result = Invoke-CoverageGate
      $result.ExitCode | Should -Be 1
      $result.Output | Should -Match 'Coverage gate FAILED: Second'
   }

   It 'holds the package to a lower bar when one is given' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Hits 1, 0
      (Invoke-CoverageGate -MinimumPercent 50).ExitCode | Should -Be 0
      (Invoke-CoverageGate -MinimumPercent 51).ExitCode | Should -Be 1
   }
}
