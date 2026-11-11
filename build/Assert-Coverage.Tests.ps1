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
      Writes a synthetic report so tests can exercise release-gate exit codes.
   #>
   function Write-CoverageReport
   {
      [CmdletBinding()]
      param(
         [Parameter(Mandatory)] [string] $Directory,
         [Parameter(Mandatory)] [ValidateRange(0, 1)] [double] $Rate,
         [switch] $NoLines
      )

      $rateText = $Rate.ToString([Globalization.CultureInfo]::InvariantCulture)
      $secondLineHits = if ($Rate -eq 1) { 1 } else { 0 }
      $lines = if ($NoLines) { '' } else { "<line number=`"1`" hits=`"1`" /><line number=`"2`" hits=`"$secondLineHits`" />" }
      $report = @"
<coverage><packages><package name="Probe" line-rate="$rateText"><classes>
<class filename="Probe.cs"><lines>$lines</lines></class>
</classes></package></packages></coverage>
"@
      New-Item -ItemType Directory -Force -Path $Directory | Out-Null
      Set-Content -LiteralPath (Join-Path $Directory 'coverage.cobertura.xml') -Value $report
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
      $output = (& pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory -Packages Probe 2>&1) -join "`n"
      $LASTEXITCODE | Should -Be 1
      $output | Should -Match 'No coverage.cobertura.xml'
   }

   It 'rejects an absent required package' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Rate 1
      $output = (& pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory -Packages Missing 2>&1) -join "`n"
      $LASTEXITCODE | Should -Be 1
      $output | Should -Match 'Missing.*not found'
   }

   It 'rejects coverage below the required percentage' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Rate 0.5
      $output = (& pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory -Packages Probe 2>&1) -join "`n"
      $LASTEXITCODE | Should -Be 1
      $output | Should -Match 'Coverage gate FAILED'
   }

   It 'accepts full coverage of the required package' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Rate 1
      $output = (& pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory -Packages Probe 2>&1) -join "`n"
      $LASTEXITCODE | Should -Be 0
      $output | Should -Match 'Coverage gate passed'
   }

   It 'names the uncovered lines of a package below the bar' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Rate 0.5
      $output = (& pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory 2>&1) -join "`n"
      $LASTEXITCODE | Should -Be 1
      $output | Should -Match 'uncovered: Probe\.cs lines 2(\s|$)'
   }

   It 'rejects a package no report measured a line of' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Rate 1 -NoLines
      $output = (& pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory -Packages Probe 2>&1) -join "`n"
      $LASTEXITCODE | Should -Be 1
      $output | Should -Match 'no measured lines'
   }

   It 'ignores an empty report when another test project measured the package' `
   {
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'first') -Rate 1 -NoLines
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'second') -Rate 0.5
      $output = (& pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory -Packages Probe 2>&1) -join "`n"
      $LASTEXITCODE | Should -Be 1
      $output | Should -Match 'Probe line coverage: 50% of 2 lines'
   }

   It 'takes the test project that covers the package best' `
   {
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'first') -Rate 0.5
      Write-CoverageReport -Directory (Join-Path $script:ReportsDirectory 'second') -Rate 1
      $output = (& pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory 2>&1) -join "`n"
      $LASTEXITCODE | Should -Be 0
      $output | Should -Match 'Probe line coverage: 100% of 2 lines .*second'
   }

   It 'holds the package to a lower bar when one is given' `
   {
      Write-CoverageReport -Directory $script:ReportsDirectory -Rate 0.5
      & pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory -MinimumPercent 50 | Out-Null
      $LASTEXITCODE | Should -Be 0
      & pwsh -NoProfile -File $script:CoverageGate -ResultsDirectory $script:ReportsDirectory -MinimumPercent 51 2>&1 | Out-Null
      $LASTEXITCODE | Should -Be 1
   }
}
