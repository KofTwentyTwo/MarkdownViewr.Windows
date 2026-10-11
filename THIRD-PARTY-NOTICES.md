# Third-party notices

These notices accompany the libraries and platform components used by MarkdownViewr.

## Markdig 1.4.0

Source: <https://github.com/xoofx/markdig>

Copyright (c) 2016-2026, Alexandre Mutel
All rights reserved.

Redistribution and use in source and binary forms, with or without modification
, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

## HtmlSanitizer 9.2.1039

Source: <https://github.com/mganss/HtmlSanitizer>

The MIT License (MIT)

Copyright (c) 2013-2016 Michael Ganss and HtmlSanitizer contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## AngleSharp 1.7.2

Source: <https://github.com/AngleSharp/AngleSharp>

The MIT License (MIT)

Copyright (c) 2013 - 2026 AngleSharp

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## AngleSharp.Css 1.0.2

Source: <https://github.com/AngleSharp/AngleSharp.Css>

The MIT License (MIT)

Copyright (c) 2013 - 2026 AngleSharp

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## markdownViewr resources, frontmatter and navigation behavior

This Windows application is based on **Darin Kelkhoff's macOS markdownViewr**.
Original project: [darinkelkhoff/markdownViewr](https://github.com/darinkelkhoff/markdownViewr).
Author website: [kelkhoff.com](https://kelkhoff.com/). Windows issues and support
belong to the Windows repository; this attribution does not imply endorsement.

Per-window reading zoom bounds/steps and native minus/percentage/plus behavior are
adapted from ContentView.swift and MarkdownViewrApp.swift. Font-size scaling is
adapted from ThemeManager.swift; original theme JSON and line height stay unchanged.

Frontmatter delimiter/metadata display behavior is adapted from MarkdownDocument.swift at the same pinned commit. Named editor identities, list ordering/removal and file/folder opening semantics are adapted from EditorConfig.swift there; Windows uses bounded .exe choices and one literal process argument. Vim sequence/navigation, nested source folding and heading-anchor scroll interpolation semantics are adapted from Resources/template.html there, with native Windows Ctrl+F, literal source controls and input focus guards. The 20 original JSON themes are copied unchanged from Darin Kelkhoff's markdownViewr at commit 6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021.

Source: <https://github.com/darinkelkhoff/markdownViewr/tree/6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021/markdownViewr/Resources/themes>

MIT License

Copyright (c) 2026 Darin Kelkhoff

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

The print defaults, Clean Printing palette and print-media layout rules in
`Core/Printing/` and the native print adapter are adapted from Darin Kelkhoff's
MIT-licensed markdownViewr (`6d08bc9`), with original/adaptation copyright headers
retained. Actual PDF acceptance uses the built-in Windows PDF API; no additional
PDF renderer/parser package is shipped.

## Complete application and tooling inventory

About → Components links each direct/transitive application dependency.
Development/test tools are acknowledged separately and are not described as
shipped application libraries. Microsoft SDK packages include their own binary
license terms; they are not all labeled MIT. Inventory below was checked against
restored package assets and packaged license metadata.

| Component | Version | Scope | Project | License |
| --- | --- | --- | --- | --- |
| AngleSharp.Css | 1.0.2 | Application | [Project](https://github.com/AngleSharp/AngleSharp.Css) | [MIT](https://licenses.nuget.org/MIT) |
| AngleSharp | 1.7.2 | Application | [Project](https://github.com/AngleSharp/AngleSharp) | [MIT](https://licenses.nuget.org/MIT) |
| HtmlSanitizer | 9.2.1039 | Application | [Project](https://github.com/mganss/HtmlSanitizer) | [MIT](https://licenses.nuget.org/MIT) |
| KofTwentyTwo.AppKit.Updates | 0.1.0 | Application | [Project](https://github.com/KofTwentyTwo/AppKit) | [MIT](https://licenses.nuget.org/MIT) |
| KofTwentyTwo.AppKit.WinUI | 0.1.0 | Application | [Project](https://github.com/KofTwentyTwo/AppKit) | [MIT](https://licenses.nuget.org/MIT) |
| KofTwentyTwo.AppKit | 0.1.0 | Application | [Project](https://github.com/KofTwentyTwo/AppKit) | [MIT](https://licenses.nuget.org/MIT) |
| Markdig | 1.4.0 | Application | [Project](https://github.com/xoofx/markdig) | [BSD-2-Clause](https://licenses.nuget.org/BSD-2-Clause) |
| Meziantou.Analyzer | 3.0.296 | Development | [Project](https://github.com/meziantou/Meziantou.Analyzer) | [MIT](https://licenses.nuget.org/MIT) |
| Microsoft.Web.WebView2 | 1.0.3719.77 | Application | [Project](https://aka.ms/webview) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.3719.77/License) |
| Microsoft.Windows.AI.MachineLearning | 2.1.74 | Application | [Project](https://github.com/microsoft/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.Windows.AI.MachineLearning/2.1.74/License) |
| Microsoft.Windows.SDK.BuildTools.MSIX | 1.7.251221100 | Development | [Project](https://aka.ms/WinSDKProjectURL) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools.MSIX/1.7.251221100/License) |
| Microsoft.Windows.SDK.BuildTools | 10.0.28000.2705 | Development | [Project](https://aka.ms/WinSDKProjectURL) | [Package license](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools/10.0.28000.2705/License) |
| Microsoft.WindowsAppSDK.AI | 2.5.5 | Application | [Project](https://github.com/microsoft/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.AI/2.5.5/License) |
| Microsoft.WindowsAppSDK.Base | 2.0.4 | Application | [Project](https://github.com/microsoft/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Base/2.0.4/License) |
| Microsoft.WindowsAppSDK.DWrite | 2.1.0 | Application | [Project](https://aka.ms/WindowsAppSDK) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.DWrite/2.1.0/License) |
| Microsoft.WindowsAppSDK.Foundation | 2.3.12 | Application | [Project](https://aka.ms/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Foundation/2.3.12/License) |
| Microsoft.WindowsAppSDK.InteractiveExperiences | 2.1.9 | Application | [Project](https://aka.ms/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.InteractiveExperiences/2.1.9/License) |
| Microsoft.WindowsAppSDK.ML | 2.1.94 | Application | [Project](https://github.com/microsoft/windowsml) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.ML/2.1.94/License) |
| Microsoft.WindowsAppSDK.Runtime | 2.5.1 | Application | [Project](https://github.com/microsoft/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Runtime/2.5.1/License) |
| Microsoft.WindowsAppSDK.Search | 2.5.5 | Application | [Project](https://github.com/microsoft/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Search/2.5.5/License) |
| Microsoft.WindowsAppSDK.Widgets | 2.0.5 | Application | [Project](https://github.com/microsoft/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Widgets/2.0.5/License) |
| Microsoft.WindowsAppSDK.WinUI | 2.3.9 | Application | [Project](https://github.com/microsoft/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.WinUI/2.3.9/License) |
| Microsoft.WindowsAppSDK | 2.5.1 | Application | [Project](https://github.com/microsoft/windowsappsdk) | [Microsoft package license](https://www.nuget.org/packages/Microsoft.WindowsAppSDK/2.5.1/License) |
| System.Numerics.Tensors | 9.0.0 | Application | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| Velopack | 1.2.161 | Application | [Project](https://github.com/velopack/velopack) | [MIT](https://licenses.nuget.org/MIT) |
| Microsoft.CodeCoverage | 18.10.1 | Development | [Project](https://github.com/microsoft/vstest) | [MIT](https://licenses.nuget.org/MIT) |
| Microsoft.NET.Test.Sdk | 18.10.1 | Development | [Project](https://github.com/microsoft/vstest) | [MIT](https://licenses.nuget.org/MIT) |
| Microsoft.TestPlatform.ObjectModel | 18.10.1 | Development | [Project](https://github.com/microsoft/vstest) | [MIT](https://licenses.nuget.org/MIT) |
| Microsoft.TestPlatform.TestHost | 18.10.1 | Development | [Project](https://github.com/microsoft/vstest) | [MIT](https://licenses.nuget.org/MIT) |
| coverlet.collector | 10.1.0 | Development | [Project](https://github.com/coverlet-coverage/coverlet) | [MIT](https://licenses.nuget.org/MIT) |
| xunit.abstractions | 2.0.3 | Development | [Project](https://github.com/xunit/xunit) | [Package license](https://www.nuget.org/packages/xunit.abstractions/2.0.3/License) |
| xunit.analyzers | 1.18.0 | Development | [Project](https://github.com/xunit/xunit.analyzers) | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| xunit.assert | 2.9.3 | Development | [Project](https://github.com/xunit/xunit) | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| xunit.core | 2.9.3 | Development | [Project](https://github.com/xunit/xunit) | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| xunit.extensibility.core | 2.9.3 | Development | [Project](https://github.com/xunit/xunit) | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| xunit.extensibility.execution | 2.9.3 | Development | [Project](https://github.com/xunit/xunit) | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| xunit.runner.visualstudio | 4.0.0 | Development | [Project](https://github.com/xunit/visualstudio.xunit) | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| xunit | 2.9.3 | Development | [Project](https://github.com/xunit/xunit) | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) |
| FlaUI.Core | 5.0.0 | Development | [Project](https://github.com/FlaUI/FlaUI) | [MIT](https://www.nuget.org/packages/FlaUI.Core/5.0.0/License) |
| FlaUI.UIA3 | 5.0.0 | Development | [Project](https://github.com/FlaUI/FlaUI) | [MIT](https://www.nuget.org/packages/FlaUI.UIA3/5.0.0/License) |
| Interop.UIAutomationClient | 10.19041.0 | Development | [Project](https://github.com/Roemer/UIAutomation-Interop) | [MIT](https://www.nuget.org/packages/Interop.UIAutomationClient/10.19041.0/License) |
| Microsoft.Win32.SystemEvents | 8.0.0 | Development | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| System.CodeDom | 8.0.0 | Development | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| System.Configuration.ConfigurationManager | 8.0.1 | Development | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| System.Diagnostics.EventLog | 8.0.1 | Development | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| System.Diagnostics.PerformanceCounter | 8.0.1 | Development | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| System.Drawing.Common | 8.0.10 | Development | [Project](https://github.com/dotnet/winforms) | [MIT](https://licenses.nuget.org/MIT) |
| System.Management | 8.0.0 | Development | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| System.Security.Cryptography.ProtectedData | 8.0.0 | Development | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| System.Security.Permissions | 8.0.0 | Development | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| System.Windows.Extensions | 8.0.0 | Development | [Project](https://github.com/dotnet/runtime) | [MIT](https://licenses.nuget.org/MIT) |
| .NET Runtime | 10 | Application platform | [Project](https://github.com/dotnet/runtime) | [MIT](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT), [third-party notices](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT) |

Additional packaged license and notice files are retained verbatim under
`THIRD-PARTY-LICENSES/` and copied to `ThirdPartyLicenses/` in application output.
Identical texts shared by multiple SDK packages are stored once.

- Microsoft.Web.WebView2 1.0.3719.77: [Microsoft.Web.WebView2-1.0.3719.77-LICENSE.txt](THIRD-PARTY-LICENSES/Microsoft.Web.WebView2-1.0.3719.77-LICENSE.txt)
- Microsoft.Web.WebView2 1.0.3719.77: [Microsoft.Web.WebView2-1.0.3719.77-NOTICE.txt](THIRD-PARTY-LICENSES/Microsoft.Web.WebView2-1.0.3719.77-NOTICE.txt)
- Microsoft.Windows.AI.MachineLearning 2.1.74: [Microsoft.Windows.AI.MachineLearning-2.1.74-ThirdPartyNotices.txt](THIRD-PARTY-LICENSES/Microsoft.Windows.AI.MachineLearning-2.1.74-ThirdPartyNotices.txt)
- Microsoft.Windows.AI.MachineLearning 2.1.74: [Microsoft.Windows.AI.MachineLearning-2.1.74-license.txt](THIRD-PARTY-LICENSES/Microsoft.Windows.AI.MachineLearning-2.1.74-license.txt)
- Microsoft.WindowsAppSDK 2.5.1: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK 2.5.1: [Microsoft.WindowsAppSDK.Base-2.0.4-NOTICE.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.Base-2.0.4-NOTICE.txt)
- Microsoft.WindowsAppSDK.AI 2.5.5: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK.Base 2.0.4: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK.Base 2.0.4: [Microsoft.WindowsAppSDK.Base-2.0.4-NOTICE.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.Base-2.0.4-NOTICE.txt)
- Microsoft.WindowsAppSDK.DWrite 2.1.0: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK.Foundation 2.3.12: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK.InteractiveExperiences 2.1.9: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK.ML 2.1.94: [Microsoft.WindowsAppSDK.ML-2.1.94-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.ML-2.1.94-license.txt)
- Microsoft.WindowsAppSDK.Runtime 2.5.1: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK.Runtime 2.5.1: [Microsoft.WindowsAppSDK.Base-2.0.4-NOTICE.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.Base-2.0.4-NOTICE.txt)
- Microsoft.WindowsAppSDK.Search 2.5.5: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK.Widgets 2.0.5: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK.WinUI 2.3.9: [Microsoft.WindowsAppSDK.AI-2.5.5-license.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.AI-2.5.5-license.txt)
- Microsoft.WindowsAppSDK.WinUI 2.3.9: [Microsoft.WindowsAppSDK.WinUI-2.3.9-NOTICE.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.WinUI-2.3.9-NOTICE.txt)
- Microsoft.WindowsAppSDK.WinUI 2.3.9: [Microsoft.WindowsAppSDK.WinUI-2.3.9-tools-NOTICE.txt](THIRD-PARTY-LICENSES/Microsoft.WindowsAppSDK.WinUI-2.3.9-tools-NOTICE.txt)
- System.Numerics.Tensors 9.0.0: [System.Numerics.Tensors-9.0.0-LICENSE.TXT](THIRD-PARTY-LICENSES/System.Numerics.Tensors-9.0.0-LICENSE.TXT)
- System.Numerics.Tensors 9.0.0: [System.Numerics.Tensors-9.0.0-THIRD-PARTY-NOTICES.TXT](THIRD-PARTY-LICENSES/System.Numerics.Tensors-9.0.0-THIRD-PARTY-NOTICES.TXT)


## Reference document stylesheet and containers

The embedded `Themes/Document.css` and document container/navigation adaptations
are based on Darin Kelkhoff's MIT-licensed markdownViewr at commit
`6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021`. The original document rules, variable
aliases and `#content` / `#content-inner` structure are retained with Windows
sanitization, scrolling and print integration. Source files retain Darin's
copyright and MIT identifiers; the full original MIT notice above accompanies
the application. Original project: <https://github.com/darinkelkhoff/markdownViewr>.
Author website: <https://kelkhoff.com/>.
