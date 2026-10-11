/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core;

/// <summary>Dependency credits verified against restored assets/nuspec metadata; keep them current when package locks change.</summary>
public static class AboutComponents
{
   /// <summary>Direct/transitive application dependencies, including the self-contained runtime and Microsoft SDK binary licenses.</summary>
   public static IReadOnlyList<AboutComponent> Runtime { get; } = Array.AsReadOnly<AboutComponent>(
   [
      new("AngleSharp.Css", "1.0.2", "MIT", "https://github.com/AngleSharp/AngleSharp.Css", "https://licenses.nuget.org/MIT"),
      new("AngleSharp", "1.7.2", "MIT", "https://github.com/AngleSharp/AngleSharp", "https://licenses.nuget.org/MIT"),
      new("HtmlSanitizer", "9.2.1039", "MIT", "https://github.com/mganss/HtmlSanitizer", "https://licenses.nuget.org/MIT"),
      new("KofTwentyTwo.AppKit.Updates", "0.1.0", "MIT", "https://github.com/KofTwentyTwo/AppKit", "https://licenses.nuget.org/MIT"),
      new("KofTwentyTwo.AppKit.WinUI", "0.1.0", "MIT", "https://github.com/KofTwentyTwo/AppKit", "https://licenses.nuget.org/MIT"),
      new("KofTwentyTwo.AppKit", "0.1.0", "MIT", "https://github.com/KofTwentyTwo/AppKit", "https://licenses.nuget.org/MIT"),
      new("Markdig", "1.4.0", "BSD-2-Clause", "https://github.com/xoofx/markdig", "https://licenses.nuget.org/BSD-2-Clause"),
      new("Microsoft.Web.WebView2", "1.0.3719.77", "Microsoft package license", "https://aka.ms/webview", "https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.3719.77/License"),
      new("Microsoft.Windows.AI.MachineLearning", "2.1.74", "Microsoft package license", "https://github.com/microsoft/windowsappsdk", "https://www.nuget.org/packages/Microsoft.Windows.AI.MachineLearning/2.1.74/License"),
      new("Microsoft.WindowsAppSDK.AI", "2.5.5", "Microsoft package license", "https://github.com/microsoft/windowsappsdk", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.AI/2.5.5/License"),
      new("Microsoft.WindowsAppSDK.Base", "2.0.4", "Microsoft package license", "https://github.com/microsoft/windowsappsdk", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Base/2.0.4/License"),
      new("Microsoft.WindowsAppSDK.DWrite", "2.1.0", "Microsoft package license", "https://aka.ms/WindowsAppSDK", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.DWrite/2.1.0/License"),
      new("Microsoft.WindowsAppSDK.Foundation", "2.3.12", "Microsoft package license", "https://aka.ms/windowsappsdk", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Foundation/2.3.12/License"),
      new("Microsoft.WindowsAppSDK.InteractiveExperiences", "2.1.9", "Microsoft package license", "https://aka.ms/windowsappsdk", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.InteractiveExperiences/2.1.9/License"),
      new("Microsoft.WindowsAppSDK.ML", "2.1.94", "Microsoft package license", "https://github.com/microsoft/windowsml", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.ML/2.1.94/License"),
      new("Microsoft.WindowsAppSDK.Runtime", "2.5.1", "Microsoft package license", "https://github.com/microsoft/windowsappsdk", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Runtime/2.5.1/License"),
      new("Microsoft.WindowsAppSDK.Search", "2.5.5", "Microsoft package license", "https://github.com/microsoft/windowsappsdk", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Search/2.5.5/License"),
      new("Microsoft.WindowsAppSDK.Widgets", "2.0.5", "Microsoft package license", "https://github.com/microsoft/windowsappsdk", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Widgets/2.0.5/License"),
      new("Microsoft.WindowsAppSDK.WinUI", "2.3.9", "Microsoft package license", "https://github.com/microsoft/windowsappsdk", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK.WinUI/2.3.9/License"),
      new("Microsoft.WindowsAppSDK", "2.5.1", "Microsoft package license", "https://github.com/microsoft/windowsappsdk", "https://www.nuget.org/packages/Microsoft.WindowsAppSDK/2.5.1/License"),
      new("System.Numerics.Tensors", "9.0.0", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
      new("Velopack", "1.2.161", "MIT", "https://github.com/velopack/velopack", "https://licenses.nuget.org/MIT"),
      new(".NET Runtime", "10", "MIT and third-party notices", "https://github.com/dotnet/runtime", "https://github.com/dotnet/runtime/blob/main/LICENSE.TXT"),
   ]);



   /// <summary>User-facing acknowledgements include shipped open-source libraries; the full inventory remains available for license audits.</summary>
   public static IReadOnlyList<AboutComponent> OpenSource { get; } = Array.AsReadOnly(Runtime
      .Where(component => !component.License.Contains("Microsoft package license", StringComparison.Ordinal))
      .Append(new AboutComponent("WinUI 3", "2.3.9 (package)", "MIT (open-source project)", "https://github.com/microsoft/microsoft-ui-xaml", "https://github.com/microsoft/microsoft-ui-xaml/blob/main/LICENSE"))
      .ToArray());



   /// <summary>Build, analysis and test dependencies remain in the audit inventory rather than the user-facing About dialog.</summary>
   public static IReadOnlyList<AboutComponent> Development { get; } = Array.AsReadOnly<AboutComponent>(
   [
      new("Meziantou.Analyzer", "3.0.296", "MIT", "https://github.com/meziantou/Meziantou.Analyzer", "https://licenses.nuget.org/MIT"),
      new("Microsoft.Windows.SDK.BuildTools.MSIX", "1.7.251221100", "Microsoft package license", "https://aka.ms/WinSDKProjectURL", "https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools.MSIX/1.7.251221100/License"),
      new("Microsoft.Windows.SDK.BuildTools", "10.0.28000.2705", "Package license", "https://aka.ms/WinSDKProjectURL", "https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools/10.0.28000.2705/License"),
      new("Microsoft.CodeCoverage", "18.10.1", "MIT", "https://github.com/microsoft/vstest", "https://licenses.nuget.org/MIT"),
      new("Microsoft.NET.Test.Sdk", "18.10.1", "MIT", "https://github.com/microsoft/vstest", "https://licenses.nuget.org/MIT"),
      new("Microsoft.TestPlatform.ObjectModel", "18.10.1", "MIT", "https://github.com/microsoft/vstest", "https://licenses.nuget.org/MIT"),
      new("Microsoft.TestPlatform.TestHost", "18.10.1", "MIT", "https://github.com/microsoft/vstest", "https://licenses.nuget.org/MIT"),
      new("coverlet.collector", "10.1.0", "MIT", "https://github.com/coverlet-coverage/coverlet", "https://licenses.nuget.org/MIT"),
      new("xunit.abstractions", "2.0.3", "Package license", "https://github.com/xunit/xunit", "https://www.nuget.org/packages/xunit.abstractions/2.0.3/License"),
      new("xunit.analyzers", "1.18.0", "Apache-2.0", "https://github.com/xunit/xunit.analyzers", "https://licenses.nuget.org/Apache-2.0"),
      new("xunit.assert", "2.9.3", "Apache-2.0", "https://github.com/xunit/xunit", "https://licenses.nuget.org/Apache-2.0"),
      new("xunit.core", "2.9.3", "Apache-2.0", "https://github.com/xunit/xunit", "https://licenses.nuget.org/Apache-2.0"),
      new("xunit.extensibility.core", "2.9.3", "Apache-2.0", "https://github.com/xunit/xunit", "https://licenses.nuget.org/Apache-2.0"),
      new("xunit.extensibility.execution", "2.9.3", "Apache-2.0", "https://github.com/xunit/xunit", "https://licenses.nuget.org/Apache-2.0"),
      new("xunit.runner.visualstudio", "4.0.0", "Apache-2.0", "https://github.com/xunit/visualstudio.xunit", "https://licenses.nuget.org/Apache-2.0"),
      new("xunit", "2.9.3", "Apache-2.0", "https://github.com/xunit/xunit", "https://licenses.nuget.org/Apache-2.0"),
      new("FlaUI.Core", "5.0.0", "MIT", "https://github.com/FlaUI/FlaUI", "https://www.nuget.org/packages/FlaUI.Core/5.0.0/License"),
      new("FlaUI.UIA3", "5.0.0", "MIT", "https://github.com/FlaUI/FlaUI", "https://www.nuget.org/packages/FlaUI.UIA3/5.0.0/License"),
      new("Interop.UIAutomationClient", "10.19041.0", "MIT", "https://github.com/Roemer/UIAutomation-Interop", "https://www.nuget.org/packages/Interop.UIAutomationClient/10.19041.0/License"),
      new("Microsoft.Win32.SystemEvents", "8.0.0", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
      new("System.CodeDom", "8.0.0", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
      new("System.Configuration.ConfigurationManager", "8.0.1", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
      new("System.Diagnostics.EventLog", "8.0.1", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
      new("System.Diagnostics.PerformanceCounter", "8.0.1", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
      new("System.Drawing.Common", "8.0.10", "MIT", "https://github.com/dotnet/winforms", "https://licenses.nuget.org/MIT"),
      new("System.Management", "8.0.0", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
      new("System.Security.Cryptography.ProtectedData", "8.0.0", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
      new("System.Security.Permissions", "8.0.0", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
      new("System.Windows.Extensions", "8.0.0", "MIT", "https://github.com/dotnet/runtime", "https://licenses.nuget.org/MIT"),
   ]);
}
