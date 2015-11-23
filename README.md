<a href="http://ftsrg.github.io/seviz" title="ftsrg.github.io/seviz"><img src="http://docs.inf.mit.bme.hu/seviz/images/seviz-logo.png" width="200" /></a>
<a href="http://ftsrg.github.io/seviz" title="ftsrg.github.io/seviz"><img src="https://seviz.visualstudio.com/DefaultCollection/_apis/public/build/definitions/f0992fd0-b212-4fd9-b74b-4be525d6556f/2/badge" /></a>

# [Symbolic Execution VisualiZer (SEViz)](http://ftsrg.github.io/seviz)

## Description

SEViz is a tool for visualizing symbolic execution-based test generation carried out by [IntelliTest](https://msdn.microsoft.com/en-us/library/dn823749.aspx).

SEViz can enhance test generation for *complex programs* by visually presenting the information required to quickly identify modifications that enable the generation of further test inputs and increase coverage.

SEViz can also be used in *education* and training by showing the process and result of symbolic execution in a step-by-step manner on simpler programs.

The source code of the project consists of the following projects:

* `SEViz.Monitoring`: captures information about the test generation of Visual Studio's IntelliTest (formerly [Pex](http://research.microsoft.com/en-us/projects/pex/)),
* `SEViz.Integration`: an extension for Visual Studio that is able to interactively visualize the test generation process and links source code instructions to the visualization,
* `SEViz.Common`: contains common data structures used by the previous two projects,
* `GraphSharp` and `GraphSharp.Controls`: a fork of the GitHub project [GrapSharp](https://github.com/andypelzer/GraphSharp/tree/c9c2c8d9070e3c541f2cf60ec0b6213e0235e727), modified for the needs of SEViz.

For more information see the [tool's website](http://ftsrg.github.io/seviz) (with user manual and examples).

## Building the project

1. Prerequisites for the build:
 * .NET framework v4.5
 * Visual Studio 2015 Enterprise (includes IntelliTest)
2. Download the zip or clone the repository.
3. Open the Visual Studio solution found in the Source folder as Administrator.
4. Build the whole solution. This triggers two pre-build events:
 * Downloading the required NuGet packages.
 * Invoking the PowerShell script that copies the necessary Pex assemblies into the corresponding folder.
