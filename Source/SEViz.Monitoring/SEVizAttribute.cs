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

        private Dictionary<int,Tuple<bool,string>> EmittedTestResult { get; set; }

        private List<string> Z3CallLocations { get; set; }

        private string UnitNamespace { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Visualizes symbolic execution.
        /// </summary>
        public SEVizAttribute()
        {
        }

        /// <summary>
        /// Visualizes symbolic execution and marks edges that are pointing in and out of the unit borders.
        /// </summary>
        /// <param name="unitNamespace">The namespace of the unit to use.</param>
        public SEVizAttribute(string unitNamespace)
        {
            UnitNamespace = unitNamespace;
        }

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

                Z3CallLocations.Add(location);
            };

            // Subscribing to test emitting handler
            host.Log.GeneratedTestHandler += (generatedTestEventArgs) =>
            {
                // Getting the number of the corresponding run
                var run = generatedTestEventArgs.GeneratedTest.Run;

                // Storing the result of the test (this is called before AfterRun)
                EmittedTestResult.Add(run, new Tuple<bool, string>(generatedTestEventArgs.GeneratedTest.IsFailure,generatedTestEventArgs.GeneratedTest.MethodCode));
            };


            return null;
        }

        public void AfterExploration(IPexExplorationComponent host, object data)
        {
            // Modifying shape based on Z3 calls
            foreach (var vertex in Vertices)
            {
                if (Z3CallLocations.Contains(vertex.MethodName + ":" + vertex.ILOffset)) vertex.Shape = SENode.NodeShape.Ellipse;
            }

            // Adding vertices and edges to the graph
            Graph.AddVertexRange(Vertices);
            Graph.AddEdgeRange(Edges);

            // Checking if temporary SEViz folder exists
            if (!Directory.Exists(Path.GetTempPath() + "SEViz"))
            {
                var dir = Directory.CreateDirectory(Path.GetTempPath() + "SEViz");

            }

            // Getting the temporary folder
            var tempDir = Path.GetTempPath() + "SEViz\\";

            // Serializing the graph into graphml
            SEGraph.Serialize(Graph, tempDir);

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
            foreach (var node in nodesInPath)
            {
                var vertex = new SENode(node.UniqueIndex, null, null, null, null, null, false);

                // Adding the method name this early in order to color edges
                string methodName = null;
                int offset = 0;
                if (node.CodeLocation.Method == null)
                {
                    if (node.InCodeBranch.Method != null)
                    {
                        methodName = node.InCodeBranch.Method.FullName;
                    }
                }
                else
                {
                    methodName = node.CodeLocation.Method.FullName;
                    offset = node.CodeLocation.Offset;
                }
                // Setting the method name
                vertex.MethodName = methodName;

                // Setting the offset
                vertex.ILOffset = offset;

                var nodeIndex = nodesInPath.ToList().IndexOf(node);
                if (nodeIndex > 0)
                {
                    var prevNode = nodesInPath[nodeIndex - 1];
                    // If there is no edge between the previous and the current node
                    if (Edges.Where(e => e.Source.Id == prevNode.UniqueIndex && e.Target.Id == node.UniqueIndex).Count() == 0)
                    {
                        var prevVertex = Vertices.Where(v => v.Id == prevNode.UniqueIndex).FirstOrDefault();
                        var edge = new SEEdge(new Random().Next(), prevVertex, vertex);
                        Edges.Add(edge);

                        // Edge coloring based on unit border detection
                        if (UnitNamespace != null)
                        {
                            // Checking if pointing into the unit from outside
                            if(!prevVertex.MethodName.StartsWith(UnitNamespace) && vertex.MethodName.StartsWith(UnitNamespace))
                            {
                                edge.Color = SEEdge.EdgeColor.Green;
                            }

                            // Checking if pointing outside the unit from inside
                            if(prevVertex.MethodName.StartsWith(UnitNamespace) && !vertex.MethodName.StartsWith(UnitNamespace))
                            {
                                edge.Color = SEEdge.EdgeColor.Red;
                            }
                        }
                    }
                }

                // If the node is new then it is added to the list and the metadata is filled
                if (Vertices.Where(v => v.Id == node.UniqueIndex).Count() == 0)
                {
                    Vertices.Add(vertex);

                    // Adding source code mapping
                    vertex.SourceCodeMappingString = MapToSourceCodeLocationString(host, node);

                    // Setting the border based on mapping existence
                    vertex.Border = vertex.SourceCodeMappingString == null ? SENode.NodeBorder.Single : SENode.NodeBorder.Double;

                    // Setting the color
                    if (nodesInPath.LastOrDefault() == node)
                    {
                        if (!EmittedTestResult.ContainsKey(runId))
                        {
                            vertex.Color = SENode.NodeColor.Orange;
                        }
                        else
                        {
                            if (EmittedTestResult[runId].Item1)
                            {
                                vertex.Color = SENode.NodeColor.Red;
                            }
                            else
                            {
                                vertex.Color = SENode.NodeColor.Green;
                            }
                            vertex.GenerateTestCode = EmittedTestResult[runId].Item2;
                        }
                    }
                    else
                    {
                        vertex.Color = SENode.NodeColor.White;
                    }

                    // Setting the default shape
                    vertex.Shape = SENode.NodeShape.Rectangle;

                    // Adding path condition
                    vertex.PathCondition = PrettyPrintPathCondition(host, node);

                    // Setting the status
                    vertex.Status = node.ExhaustedReason.ToString();

                    // Calculating the incremental path condition based on the full
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
                Vertices.Where(v => v.Id == node.UniqueIndex).FirstOrDefault().Runs += (runId + ";");
            }
        }

        #endregion

        public void Initialize(IPexExplorationEngine host)
        {
            Graph = new SEGraph();
            Vertices = new List<SENode>();
            Edges = new List<SEEdge>();
            EmittedTestResult = new Dictionary<int, Tuple<bool,string>>();
            Z3CallLocations = new List<string>();
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
            string output = "";
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
            catch (Exception) { }
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
            var prevSplittedCondition = prevPc.Split(new string[] { "&& " }, StringSplitOptions.None);
            
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
