#Requires -Version 7.4
<#
.SYNOPSIS
    Fails when any named assembly is below the required line coverage.

.DESCRIPTION
    Reads every coverage.cobertura.xml under -ResultsDirectory (one per test project
    run) and, for each package, takes the best line rate any report measured. A package
    can be split across test projects, and the report that exercises it most is
    authoritative. Without -Packages, every package the reports measured is gated:
    coverage.runsettings already limits measurement to the assemblies held to the bar.
    Exits 1 on any shortfall, when a package named in -Packages is missing, or when no
    report measured a single line of it.

    Every report under the directory counts, including one left by an earlier run, so
    collect into an empty directory. Run the script as its own process
    (pwsh -NoProfile -File): called in-process, its exit code does not stop the caller.

.EXAMPLE
    ./build/Assert-Coverage.ps1 -ResultsDirectory TestResults

.EXAMPLE
    ./build/Assert-Coverage.ps1 -ResultsDirectory TestResults -Packages MarkdownViewr.Windows.Core
#>
[CmdletBinding()]
param(
   [Parameter(Mandatory)] [string] $ResultsDirectory,
   [string[]] $Packages = @(),
   [ValidateRange(0, 100)] [double] $MinimumPercent = 100
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# `pwsh -File` passes "A,B" as one string; accept that as well as a real array.
$Packages = @($Packages | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$files = @(Get-ChildItem -Path $ResultsDirectory -Recurse -Filter coverage.cobertura.xml -ErrorAction SilentlyContinue)
if ($files.Count -eq 0)
{
   Write-Error "No coverage.cobertura.xml found under '$ResultsDirectory'. Did the coverage collector run?" -ErrorAction Continue
   exit 1
}

# One entry per package per report: where it was measured, how many lines, and how well.
$measured = foreach ($file in $files)
{
   $document = [xml](Get-Content -LiteralPath $file.FullName -Raw)
   foreach ($package in $document.SelectNodes('/coverage/packages/package'))
   {
      [pscustomobject]@{
         Name    = $package.GetAttribute('name')
         Report  = $file.FullName
         Lines   = $package.SelectNodes('classes/class/lines/line').Count
         Rate    = [double]::Parse($package.GetAttribute('line-rate'), [Globalization.CultureInfo]::InvariantCulture)
         Package = $package
      }
   }
}
$measured = @($measured)
if ($Packages.Count -eq 0)
{
   $Packages = @($measured | ForEach-Object { $_.Name } | Where-Object { $_ } | Sort-Object -Unique)
   if ($Packages.Count -eq 0)
   {
      Write-Error 'The coverage reports measured no packages; check the Include filter in coverage.runsettings.' -ErrorAction Continue
      exit 1
   }
}
$required = $MinimumPercent / 100
$failed = $false

foreach ($name in $Packages)
{
   $found = @($measured | Where-Object { $_.Name -eq $name })
   if ($found.Count -eq 0)
   {
      Write-Error "Package '$name' not found in any coverage report." -ErrorAction Continue
      $failed = $true
      continue
   }

   # A package with no measured lines reports a perfect rate; it proves nothing.
   $counted = @($found | Where-Object { $_.Lines -gt 0 })
   if ($counted.Count -eq 0)
   {
      Write-Error "Package '$name' has no measured lines in any coverage report." -ErrorAction Continue
      $failed = $true
      continue
   }

   $best = $counted | Sort-Object -Property Rate -Descending | Select-Object -First 1
   Write-Output ("{0} line coverage: {1}% of {2} lines (required {3}%), from {4}" -f $name, [math]::Round($best.Rate * 100, 2), $best.Lines, $MinimumPercent, $best.Report)
   if ($best.Rate -lt $required)
   {
      foreach ($class in $best.Package.SelectNodes('classes/class'))
      {
         $missed = @($class.SelectNodes('lines/line[@hits="0"]') | ForEach-Object { $_.GetAttribute('number') })
         if ($missed.Count -gt 0) { Write-Output "  uncovered: $($class.GetAttribute('filename')) lines $($missed -join ', ')" }
      }
      Write-Error "Coverage gate FAILED: $name is below $MinimumPercent%." -ErrorAction Continue
      $failed = $true
   }
}

if ($failed) { exit 1 }
Write-Output "Coverage gate passed."
