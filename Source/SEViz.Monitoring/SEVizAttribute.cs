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

            };

            // Subscribing to test emitting handler
            host.Log.GeneratedTestHandler += (generatedTestEventArgs) =>
            {
                // Getting the number of the corresponding run
                var run = generatedTestEventArgs.GeneratedTest.Run;

                // Finding the corresponding leaf node of the run
                foreach(var node in Graph.Vertices.Where(v => v.Runs.Split(';').Contains(run.ToString())))
                {
                    IEnumerable<SEEdge> edges = null;
                    Graph.TryGetOutEdges(node, out edges);
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
            using (var w = new StreamWriter(@"D:\debug.txt", true))
            {
                w.WriteLine("after_explore");
            }
        }

        #endregion

        #region Run package

        public object BeforeRun(IPexPathComponent host)
        {
            using (var w = new StreamWriter(@"D:\debug.txt", true))
            {
                w.WriteLine("before_run");
            }
            return null;
        }

        public void AfterRun(IPexPathComponent host, object data)
        {
            using (var w = new StreamWriter(@"D:\debug.txt", true))
            {
                w.WriteLine("after_run");
            }
        }

        #endregion

        public void Initialize(IPexExplorationEngine host)
        {
            Graph = new SEGraph();
            SolverLocations = new List<string>();
        }

        public void Load(IContainer pathContainer)
        {
        }

        protected override void Decorate(Name location, IPexDecoratedComponentElement host)
        {
            host.AddExplorationPackage(location, this);
            host.AddPathPackage(location, this);
        }
    }
}
