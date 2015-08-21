# Symbolic Execution VisualIZer (SEViz)

SEViz is a tool for visualizing symbolic execution-based test generation.

SEViz can enhance test generation for *complex programs* by visually presenting the information required to quickly identify modifications that enable the generation of further test inputs and increase coverage.

SEViz can also be used in *education* and training by showing the process and result of symbolic execution in a step-by-step manner on simpler programs.

The source code of the project consists of the following three projects:

* `SEViz.Monitoring`: captures information about the test generation of Visual Studio's [IntelliTest](https://msdn.microsoft.com/en-us/library/dn823749.aspx) (formerly [Pex](http://research.microsoft.com/en-us/projects/pex/)),
* `SEViz.VSExtension`: an extension for Visual Studio that links source code instructions to the visualization,
* `SEViz.Viewer`: a program to interactively visualize the captured test generation process.

For more information see the [tool's website](http://ftsrg.github.io/seviz).

---

SeViz uses the following components and libraries:

* [Dot2WPF](http://www.codeproject.com/Articles/18870/Dot-WPF-a-WPF-control-for-viewing-Dot-graphs): a WPF control for viewing Dot graphs, licensed under The Code Project License (CPOL),
* [CircularLoadingPanel](http://huydinhpham.blogspot.hu/2011/07/wpf-loading-panel.html): a WPF loading panel with a progress bar, licensed under the Microsoft Public License (MS-PL),
* [DotNetZip](http://dotnetzip.codeplex.com/): a class library for manipulating zip files, licensed under the Microsoft Public License (MS-PL).