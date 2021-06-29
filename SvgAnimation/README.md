<head>
    <script src="https://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.11.2.min.js"></script>
    <script src="docs/assets/js/animation.js"></script>
</head>
<div align="center">
  <img src="docs/assets/images/logo_coyote.svg" width="70%">
  <h2>Fearless coding for reliable asynchronous software</h2>
</div>

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Coyote.svg)](https://www.nuget.org/packages/Microsoft.Coyote/)
![Windows CI](https://github.com/microsoft/coyote/workflows/Windows%20CI/badge.svg)
![Linux CI](https://github.com/microsoft/coyote/workflows/Linux%20CI/badge.svg)
![macOS CI](https://github.com/microsoft/coyote/workflows/macOS%20CI/badge.svg)
[![Join the chat at https://gitter.im/Microsoft/coyote](https://badges.gitter.im/Microsoft/coyote.svg)](https://gitter.im/Microsoft/coyote?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Coyote is a .NET library and tool designed to help ensure that your code is free of concurrency bugs.

It gives you the ability to reliably *unit test the concurrency* and other sources of nondeterminism
(such as message re-orderings, timeouts and failures) in your C# code. In the heart of Coyote is a
scheduler that takes control (via binary rewriting) of your program's execution during testing and
is able to systematically explore the concurrency and nondeterminism to find safety and liveness
bugs. The awesome thing is that once Coyote finds a bug it gives you the ability to fully *reproduce*
it as many times as you want, making debugging and fixing the issue much easier.

Coyote is used by several teams in [Azure](https://azure.microsoft.com/) to systematically test
their distributed systems and services, finding hundreds of bugs before they manifest in
production. In the words of an Azure service architect:
> Coyote found several issues early in the dev process, this sort of issues that would usually bleed
> through into production and become very expensive to fix later.

<div>
<svg id="animation" viewbox="0,0,1920,1080" width="100%">
    <style>
      .title { font: bold 50px sans-serif; fill:white; }
    </style>
    <defs>
        <filter id="glow-filter" x="0" y="0" width="125%">
            <feGaussianBlur stdDeviation="5" />
            <feOffset dx="0" dy="0"/>
            <feMerge>
                <feMergeNode/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
        <filter id="glow-filter-2" x="-25%" y="-25%" width="150%" height="150%">
            <feGaussianBlur in="SourceGraphic" stdDeviation="10"/>
            <feOffset dx="0" dy="0"/>
            <feMerge>
                <feMergeNode/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
    </defs>
    <rect fill="#151520" width="100%" height="100%"/>
</svg>
<script>
    $(document).ready(function () {
        svg = $("#animation")[0];
        hero_animation.start(svg);
    });
</script>
</div>
See our [documentation](https://microsoft.github.io/coyote/) for more information about the project,
case studies, tutorials and reference documentation.

Coyote is made with :heart: by Microsoft Research.

## Contributing
This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a
CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repositories using our CLA.

## Code of Conduct
This project has adopted the [Microsoft Open Source Code of
Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of
Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact
[opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
