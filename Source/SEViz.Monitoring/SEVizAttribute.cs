using Microsoft.Pex.Engine.Packages;
using Microsoft.Pex.Framework.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ExtendedReflection.ComponentModel;
using Microsoft.Pex.Engine.ComponentModel;
using Microsoft.ExtendedReflection.Metadata.Names;
using System.IO;
using SEViz.Common.Model;
using Microsoft.VisualStudio.Shell.Interop;
using SEViz.Monitoring.Helpers;
using System.Management;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.ExtendedReflection.Reasoning.ExecutionNodes;
using Microsoft.ExtendedReflection.Symbols;
using Microsoft.ExtendedReflection.Metadata;
using Microsoft.ExtendedReflection.Interpretation;
using Microsoft.ExtendedReflection.Utilities.Safe.IO;
using Microsoft.ExtendedReflection.Emit;
using Microsoft.Pex.Engine.TestGeneration;
using System.Text.RegularExpressions;

namespace SEViz.Monitoring
{
    public class SEVizAttribute : PexComponentElementDecoratorAttributeBase, IPexPathPackage, IPexExplorationPackage
    {
        #region Properties

        public string Name
        {
            get
            {
                return "SEViz";
            }
        }

        private List<SENode> Vertices { get; set; }

        private List<SEEdge> Edges { get; set; }

        public SEGraph Graph { get; private set; }

        public List<string> SolverLocations { get; private set; }

        #endregion

        #region Exploration package

        public object BeforeExploration(IPexExplorationComponent host)
        {
            // Subscribing to constraint problem handler
            host.Log.ProblemHandler += (problemEventArgs) =>
            {
                var location = problemEventArgs.FlippedLocation.Method == null ?
                               "" :
                               (problemEventArgs.FlippedLocation.Method.FullName + ":" + problemEventArgs.FlippedLocation.Offset);
                SolverLocations.Add(location);
                // TODO add location offset to node as metadata

                Vertices.Where(v => (v.MethodName + ":" + v.ILOffset) == location).FirstOrDefault().Shape = SENode.NodeShape.Ellipse;
            };

            // Subscribing to test emitting handler
            host.Log.GeneratedTestHandler += (generatedTestEventArgs) =>
            {
                // Getting the number of the corresponding run
                var run = generatedTestEventArgs.GeneratedTest.Run;

                // Finding the corresponding leaf node of the run
                foreach(var node in Vertices.Where(v => v.Runs.Split(';').Contains(run.ToString())))
                {
                    IEnumerable<SEEdge> edges = Edges.Where(e => e.Source == node);
                    if(edges.Count() == 0)
                    {
                        
                        // Adding the source code of the generated test
                        node.GenerateTestCode = generatedTestEventArgs.GeneratedTest.MethodCode;

                        // Modifying the color based on the outcome of the test
                        node.Color = (generatedTestEventArgs.GeneratedTest.IsFailure) ? SENode.NodeColor.Red : SENode.NodeColor.Green;

                        // This is the leaf node, there is nothing more to search
                        break;
                    }
                }
            };

            return null;
        }

        public void AfterExploration(IPexExplorationComponent host, object data)
        {
            // Checking if temporary SEViz folder exists
            if(!Directory.Exists(Path.GetTempPath()+"SEViz"))
            {
                Directory.CreateDirectory(Path.GetTempPath() + "SEViz");
            }

            // Getting the temporary folder
            var tempDir = Path.GetTempPath() + "SEViz";

            // Serializing the graph into graphml
            SEGraph.Serialize(Graph, tempDir);

            // Getting the process id of the parent devenv.exe instance
            var myId = Process.GetCurrentProcess().Id;
            var query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", myId);
            var search = new ManagementObjectSearcher("root\\CIMV2", query);
            var results = search.Get().GetEnumerator();
            results.MoveNext();
            var queryObj = results.Current;
            var parentId = (uint)queryObj["ParentProcessId"];
            var dte = CommunicationHelper.GetDTEByProcessId(Convert.ToInt32(parentId));

            // Getting the Visual Studio Service Provider and the shell
            var prov = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            var shell = (IVsUIShell)prov.GetService(typeof(SVsUIShell));

            IVsWindowFrame frame = null;
            if (shell != null)
            {
                // Finding the SEViz window
                var guidSeviz = new Guid("531b782e-c65a-44c8-a902-1a43ae3f568a");
                shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guidSeviz, out frame);
            }

            // Opening the SEViz window
            if (frame != null) frame.Show();
        }

        #endregion

        #region Run package

        public object BeforeRun(IPexPathComponent host)
        {
            return null;
        }

        public void AfterRun(IPexPathComponent host, object data)
        {
            // Getting the executions nodes in the current path
            var nodesInPath = host.PathServices.CurrentExecutionNodeProvider.ReversedNodes.Reverse().ToArray();

            // Getting the sequence id of the current run
            var runId = host.ExplorationServices.Driver.Runs;
            
            // Iterating over the nodes in the path
            foreach(var node in nodesInPath)
            {
                var vertex = new SENode(node.UniqueIndex,null,null,null,null,null,false);

                if (Vertices.Where(v => v.Id == node.UniqueIndex).Count() > 0)
                {
                    vertex = Vertices.Where(v => v.Id == node.UniqueIndex).FirstOrDefault();

                    var nodeIndex = nodesInPath.ToList().IndexOf(node);
                    if (nodeIndex > 0)
                    {
                        var prevNode = nodesInPath[nodeIndex - 1];
                        // If there is no edge between the previous and the current node
                        if (Edges.Where(e => e.Source.Id == prevNode.UniqueIndex && e.Target.Id == node.UniqueIndex).Count() == 0)
                        {
                            var prevVertex = Vertices.Where(v => v.Id == prevNode.UniqueIndex).FirstOrDefault();
                            Edges.Add(new SEEdge(new Random().Next(), prevVertex, vertex));

                            // TODO add edge coloring based on unit border detection algorithm
                        }
                    }
                } else // If the node is new then it is added to the list and the metadata is filled
                {
                    Vertices.Add(vertex);

                    // Adding source code mapping
                    vertex.SourceCodeMappingString = MapToSourceCodeLocationString(host,node);

                    // Setting the border based on mapping existence
                    vertex.Border = vertex.SourceCodeMappingString == null ? SENode.NodeBorder.Single : SENode.NodeBorder.Double;

                    // Adding the method name
                    string methodName = null;
                    int offset = 0;
                    if (node.CodeLocation.Method == null) {
                        if (node.InCodeBranch.Method != null)
                        {
                            methodName = node.InCodeBranch.Method.FullName;
                        }
                    } else
                    {
                        methodName = node.CodeLocation.Method.FullName;
                        offset = node.CodeLocation.Offset;
                    }

                    // Setting the color
                    vertex.Color = SENode.NodeColor.White;
                    
                    // Setting the shape
                    vertex.Shape = SENode.NodeShape.Rectangle;

                    // Setting the method name
                    vertex.MethodName = methodName;

                    // Setting the offset
                    vertex.ILOffset = offset;

                    // Adding path condition
                    vertex.PathCondition = PrettyPrintPathCondition(host,node);

                    // Calculating the incremental path condition based on the full
                    var nodeIndex = nodesInPath.ToList().IndexOf(node);
                    if (nodeIndex > 0)
                    {
                        var prevNode = Vertices.Where(v => v.Id == nodesInPath[nodeIndex - 1].UniqueIndex).FirstOrDefault();
                        vertex.IncrementalPathCondition = CalculateIncrementalPathCondition(vertex.PathCondition, prevNode.PathCondition);
                    } else
                    {
                        // If the node is the first one, then the incremental equals the full PC
                        vertex.IncrementalPathCondition = vertex.PathCondition;
                    }
                }

                // Adding the Id of the run
                vertex.Runs += (runId + ";");


            }
        }

        #endregion

        public void Initialize(IPexExplorationEngine host)
        {
            Graph = new SEGraph();
            SolverLocations = new List<string>();
            Vertices = new List<SENode>();
            Edges = new List<SEEdge>();
        }

        public void Load(IContainer pathContainer)
        {
        }

        protected override void Decorate(Name location, IPexDecoratedComponentElement host)
        {
            host.AddExplorationPackage(location, this);
            host.AddPathPackage(location, this);
        }

        #region Private methods 

        /// <summary>
        /// Maps the execution node to source code location string.
        /// </summary>
        /// <param name="host">Host of the Pex Path Component</param>
        /// <param name="node">The execution node to map</param>
        /// <returns>The source code location string in the form of [documentlocation]:[linenumber]</returns>
        private string MapToSourceCodeLocationString(IPexPathComponent host, IExecutionNode node)
        {
            try
            {
                var symbolManager = host.GetService<ISymbolManager>();
                var sourceManager = host.GetService<ISourceManager>();
                MethodDefinitionBodyInstrumentationInfo nfo;
                if (node.CodeLocation.Method == null)
                {
                    if (node.InCodeBranch.Method == null) { return null; }
                    else { node.InCodeBranch.Method.TryGetBodyInstrumentationInfo(out nfo); }
                }
                else { node.CodeLocation.Method.TryGetBodyInstrumentationInfo(out nfo); }
                SequencePoint point;
                int targetOffset;
                nfo.TryGetTargetOffset(node.InCodeBranch.BranchLabel, out targetOffset);
                if (symbolManager.TryGetSequencePoint(node.CodeLocation.Method == null ? node.InCodeBranch.Method : node.CodeLocation.Method, node.CodeLocation.Method == null ? targetOffset : node.CodeLocation.Offset, out point))
                {
                    return point.Document + ":" + point.Line;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                // TODO Exception handling
                return null;
            }
        }

        /// <summary>
        /// Pretty prints the path condition of the execution node.
        /// </summary>
        /// <param name="host">Host of the Pex Path Component</param>
        /// <param name="node">The execution node to map</param>
        /// <returns>The pretty printed path condition string</returns>
        private string PrettyPrintPathCondition(IPexPathComponent host, IExecutionNode node)
        {
            string output = null;
            try
            {
                if (node.GetPathCondition().Conjuncts.Count != 0)
                {
                    TermEmitter termEmitter = new TermEmitter(host.GetService<TermManager>());
                    SafeStringWriter safeStringWriter = new SafeStringWriter();
                    IMethodBodyWriter methodBodyWriter = host.GetService<IPexTestManager>().Language.CreateBodyWriter(safeStringWriter, VisibilityContext.Private, 2000);
                    if (termEmitter.TryEvaluate(node.GetPathCondition().Conjuncts, 2000, methodBodyWriter))
                    {
                        for (int i = 0; i < node.GetPathCondition().Conjuncts.Count - 1; i++)
                        {
                            methodBodyWriter.ShortCircuitAnd();
                        }

                        methodBodyWriter.Statement();
                        output = safeStringWriter.ToString().Remove(safeStringWriter.ToString().Count() - 3);
                    }
                }
            }
            catch (Exception)
            {
                // TODO Exception handling
            }
            return output;

        }

        /// <summary>
        /// Calculates the incremental path condition of a node
        /// </summary>
        /// <param name="pc">The path condition of the current node</param>
        /// <param name="prevPc">The path condition of the previous node</param>
        /// <returns>The incremental path condition of the node</returns>
        private string CalculateIncrementalPathCondition(string pc, string prevPc)
        {
            var remainedLiterals = new List<string>();

            var splittedCondition = pc.Split(new string[] { "&& " },StringSplitOptions.None);
            var prevSplittedCondition = prevPc.Split(new string[] { "&&" }, StringSplitOptions.None);
            
            var currentOrdered = splittedCondition.OrderBy(c => c);
            var prevOrdered = prevSplittedCondition.OrderBy(c => c);

            foreach(var c in currentOrdered)
            {
                if(!prevOrdered.Contains(c))
                {
                    remainedLiterals.Add(c);
                }
            }

            for(int i = 1; i <= 3; i++)
            {
                foreach(var c in prevOrdered)
                {
                    var incrementedLiteral = Regex.Replace(c, "s\\d+", n => "s" + (int.Parse(n.Value.TrimStart('s')) + i).ToString());
                    remainedLiterals.Remove(incrementedLiteral);
                }
            }

            var remainedBuilder = new StringBuilder();
            foreach(var literal in remainedLiterals)
            {
                if(remainedLiterals.IndexOf(literal) != 0)
                    remainedBuilder.Append(" && ");
                remainedBuilder.Append(literal);
            }
            return remainedBuilder.ToString();
        }
        #endregion
    }
}
