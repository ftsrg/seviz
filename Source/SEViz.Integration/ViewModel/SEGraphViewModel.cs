using GraphSharp.Controls;
using SEViz.Common;
using SEViz.Common.Model;
using SEViz.Integration.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SEViz.Integration.ViewModel
{
    public class SEGraphLayout : GraphLayout<SENode, SEEdge, SEGraph>
    {
    }

    public class SEGraphViewModel
    {
        #region Properties

        private SEGraph _graph;
        public SEGraph Graph
        {
            get { return _graph; }
            set { _graph = value; }
        }

        private FileSystemWatcher fsw;

        #endregion

        public SEGraphViewModel()
        {
            fsw = new FileSystemWatcher(Path.GetTempPath()+"SEViz");
            fsw.Changed += (p1, p2) =>
            {
                // Hacking the double event firing
                lock (new object())
                {
                    fsw.EnableRaisingEvents = false;
                }

                // Getting the dispatcher to modify the UI
                var dispatcher = Application.Current.Dispatcher;
                dispatcher.BeginInvoke((Action)LoadGraphFromTemp);  
            };
            fsw.NotifyFilter = NotifyFilters.LastWrite;
            fsw.EnableRaisingEvents = true;

            LoadGraph(null);
            // Adding runs and pcs to a set of nodes
            //Graph.Vertices.ElementAt(3).Runs = "1";  Graph.Vertices.ElementAt(2).Runs = "1";  Graph.Vertices.ElementAt(1).Runs = "1"; Graph.Vertices.ElementAt(0).Runs = "1";
        }

        private void LoadGraphFromTemp()
        {
            var result = MessageBox.Show("New SEViz graph is available. Do you want to load it?", "SEViz notification", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoadGraph(SEGraph.Deserialize(Path.GetTempPath() + "SEViz/" + "temp.graphml"));
                ViewerWindowCommand.Instance.ShowToolWindow(null, null);
            }
            
            fsw.EnableRaisingEvents = true;
        }

        public void LoadGraph(SEGraph graph)
        {
            // TODO starting sample graph

            if(graph == null)
                Graph = new SEGraph();

            if(graph != null)
                Graph = graph;
            /*
            for (int i = 0; i < 8; i++)
            {
                var n = new SENode(i, null, null, null, null, null, (i==4 || i==1) ? true : false);
                graph.AddVertex(n);
            }

            graph.AddEdge(new SEEdge(0, graph.Vertices.ElementAt(0), graph.Vertices.ElementAt(1)) { Color = SEEdge.EdgeColor.Red });
            graph.AddEdge(new SEEdge(1, graph.Vertices.ElementAt(1), graph.Vertices.ElementAt(2)));
            graph.AddEdge(new SEEdge(2, graph.Vertices.ElementAt(2), graph.Vertices.ElementAt(3)));
            graph.AddEdge(new SEEdge(3, graph.Vertices.ElementAt(2), graph.Vertices.ElementAt(4)));
            graph.AddEdge(new SEEdge(4, graph.Vertices.ElementAt(0), graph.Vertices.ElementAt(5)));
            graph.AddEdge(new SEEdge(5, graph.Vertices.ElementAt(1), graph.Vertices.ElementAt(7)));
            graph.AddEdge(new SEEdge(6, graph.Vertices.ElementAt(4), graph.Vertices.ElementAt(6)));*/
            // TODO ending sample graph
            
           
            //Graph = SEGraph.Deserialize(@"D:\graph.graphml");
        }
    }
}
