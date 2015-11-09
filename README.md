# Symbolic Execution VisualiZer (SEViz)

SEViz is a tool for visualizing symbolic execution-based test generation carried out by [IntelliTest](https://msdn.microsoft.com/en-us/library/dn823749.aspx).

SEViz can enhance test generation for *complex programs* by visually presenting the information required to quickly identify modifications that enable the generation of further test inputs and increase coverage.

SEViz can also be used in *education* and training by showing the process and result of symbolic execution in a step-by-step manner on simpler programs.

The source code of the project consists of the following projects:

* `SEViz.Monitoring`: captures information about the test generation of Visual Studio's IntelliTest (formerly [Pex](http://research.microsoft.com/en-us/projects/pex/)),
* `SEViz.Integration`: an extension for Visual Studio that is able to interactively visualize the test generation process and links source code instructions to the visualization,
* `SEViz.Common`: contains common data structures used by the previous two projects,
* `GraphSharp` and `GraphSharp.Controls`: a fork of the GitHub project [GrapSharp](https://github.com/andypelzer/GraphSharp/tree/c9c2c8d9070e3c541f2cf60ec0b6213e0235e727), modified for the needs of SEViz.

For more information see the [tool's website](http://ftsrg.github.io/seviz).
