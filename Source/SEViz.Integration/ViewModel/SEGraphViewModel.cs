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
        public Dictionary<SEEdge,EdgeControl> GetEdgeControls()
        {
            return EdgeControls;
        }

        public Dictionary<SENode,VertexControl> GetVertexControls()
        {
            return VertexControls;
        }

        public void DeleteVertexControl(SENode node)
        {
            RemoveVertexControl(node);
        }

        public void AddVertexControl(SENode node)
        {
            CreateVertexControl(node);
        }

        public void DeleteEdgeControl(SEEdge edge)
        {
            RemoveEdgeControl(edge);
        }

        public void AddEdgeControl(SEEdge edge)
        {
            CreateEdgeControl(edge);
        }
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
                dispatcher.Invoke((Action)LoadGraphFromTemp);  
            };
            fsw.NotifyFilter = NotifyFilters.LastWrite;
            fsw.EnableRaisingEvents = true;
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
            if (graph == null)
            {
                Graph = new SEGraph();
            }
            else
            {
                foreach (var e in Graph.Edges.ToList()) Graph.RemoveEdge(e);
                foreach (var e in Graph.HiddenEdges.ToList()) ((List<SEEdge>)Graph.HiddenEdges).Remove(e);
                foreach (var v in Graph.Vertices.ToList()) Graph.RemoveVertex(v);
                foreach (var v in Graph.HiddenVertices.ToList()) ((List<SENode>)Graph.HiddenVertices).Remove(v);

                foreach (var v in graph.Vertices) Graph.AddVertex(v);
                foreach (var e in graph.Edges) Graph.AddEdge(e);

            }
        }
    }
}
