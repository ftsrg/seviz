//------------------------------------------------------------------------------
// <copyright file="ViewerWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SEViz.Integration
{
    using ViewModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Windows;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Media;
    using GraphSharp.Controls;
    using QuickGraph.Algorithms.Search;
    using System.Collections.Generic;
    using Resources;
    using System.Threading;
    using System.ComponentModel;
    using System;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using System.Windows.Media.Effects;
    using System.Windows.Input;
    using Common;
    using Common.Model;
    using EnvDTE;
    using System.IO;

    /// <summary>
    /// Interaction logic for ViewerWindowControl.
    /// </summary>
    public partial class ViewerWindowControl : UserControl
    {

        private SENode currentSubtreeRoot;
        private ViewerWindow _parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewerWindowControl"/> class.
        /// </summary>
        public ViewerWindowControl(ViewerWindow parent)
        {
            _parent = parent;
            InitializeComponent();

            var vm = new SEGraphViewModel();

            DataContext = vm;

            GraphControl.LayoutAlgorithmType = "EfficientSugiyama";
            GraphControl.HighlightAlgorithmType = "Simple";

            vm.LoadGraph(null);

            GraphControl.Graph = vm.Graph;
            GraphControl.AfterLayoutCallback = AfterRelayout;
        }

        public void AfterRelayout()
        {
            DecorateVerticesBackground();
        }

        public void FindAndSelectNodesByLocation(string location, int startLine, int endLine)
        {
            var foundedNodes = new List<SENode>();
            for (int i = startLine; i <= endLine; i++)
            {
                var match = GraphControl.Graph.Vertices.Where(v => v.PathCondition.Contains(location + ":" + i.ToString())).FirstOrDefault();
                if(match != null) foundedNodes.Add(match);
            }
            if(foundedNodes.Count > 0)
            {
                SelectNodesVisually(foundedNodes);
            } else
            {
                VsShellUtilities.ShowMessageBox(
                    ViewerWindowCommand.Instance.ServiceProvider,
                    "No nodes found for the selected source lines.",
                    "SEViz information",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private void SelectNodesVisually(List<SENode> nodes)
        {
            DeselectAllVisually();
            foreach(var n in nodes)
            {
                n.Select();
            }
            DecorateVerticesBackground();   
        }

        private void DeselectAllVisually()
        {
            foreach(var v in GraphControl.Graph.Vertices)
            {
                if(v.IsSelected)
                v.Deselect();      
            }
            DecorateVerticesBackground();
        }

        private void DecorateVerticesBackground()
        {
            foreach(var v in GraphControl.Graph.Vertices)
            {
                DecorateVertexBackground(v);
            }
        }

        private void DecorateVertexBackground(SENode v)
        {
            if (GraphControl.GetVertexControl(v) != null)
            {
                GraphControl.GetVertexControl(v).Background = new SolidColorBrush(Converters.SevizColorToWpfColor(v.Color));
            }
            
        }

        private void SelectNodeWithProperties(SENode node)
        {
            IVsWindowFrame frame = null;

            if (frame == null)
            {
                var shell = _parent.GetVsService(typeof(SVsUIShell)) as IVsUIShell;
                if (shell != null)
                {
                    var guidPropertyBrowser = new
                    Guid(ToolWindowGuids.PropertyBrowser);
                    shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate,
                    ref guidPropertyBrowser, out frame);
                }
            }

            if (frame != null)
            {
                frame.Show();
            }

            var selContainer = new Microsoft.VisualStudio.Shell.SelectionContainer();
            var items = new List<SENode>();

            items.Add(node);
            selContainer.SelectedObjects = items;

            ITrackSelection track = _parent.GetVsService(typeof(STrackSelection)) as ITrackSelection;
            if (track != null)
            {
                track.OnSelectChange(selContainer);
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
            {
                // If control is pressed then to nothing
            }
            else
            {
                DeselectAllVisually();
            }

            
        }

        private void VisuallySelectNode(SENode node)
        {
            node.Select();
            DecorateVerticesBackground();
        }

        private void MapNodesToSourceLinesBookmarks(SENode node)
        {
            var dte = _parent.GetVsService(typeof(SDTE)) as EnvDTE.DTE;
            var fileUrl = node.SourceCodeMappingString.Split(':')[0] + ":" + node.SourceCodeMappingString.Split(':')[1];
            var line = node.SourceCodeMappingString.Split(':')[2];
            var window = dte.ItemOperations.OpenFile(fileUrl);
            if (window != null)
            {
                window.Activate();
                var selection = window.Selection as TextSelection;
                selection.GotoLine(int.Parse(line), true);
                var objectEditPoint = selection.ActivePoint.CreateEditPoint();
                objectEditPoint.SetBookmark();
            }
        }

        /// <summary>
        /// Handles left clicks.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Node_OnClick(object sender, RoutedEventArgs e)
        {
            var node = (sender as VertexControl).Vertex as SENode;
            if (!node.IsSelected)
            {
                SelectNodeWithProperties(node);
                VisuallySelectNode(node);
            } else
            {
                MapNodesToSourceLinesBookmarks(node);
            }
        }

        public List<SENode> GetNodesOfRun(string runId)
        {
            return GraphControl.Graph.Vertices.Where(v => v.Runs.Split(';').Contains(runId.Split(';')[0])).ToList();
        }

        /// <summary>
        /// Handles right clicks.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void Node_OnRightClick(object sender, RoutedEventArgs e)
        {
            
            currentSubtreeRoot = ((sender as VertexControl).Vertex as SENode);
            
            if (currentSubtreeRoot.CollapsedSubtreeNodes.Count == 0)
            {
                // In order to revert to original color!
                if (currentSubtreeRoot.IsSelected)
                {
                    currentSubtreeRoot.Deselect();
                    DecorateVertexBackground(currentSubtreeRoot);
                }

                // Collapsing
                // If there are edges going out of it
                IEnumerable<SEEdge> edges = null;
                if(GraphControl.Graph.TryGetOutEdges(currentSubtreeRoot,out edges))
                {
                    if(edges.Count() == 0)
                    {
                        // Has no out edges --> leaf node --> select the nodes of the matching run of the leaf node
                        var vm = (SEGraphViewModel)DataContext;
                        SelectNodesVisually(GetNodesOfRun(currentSubtreeRoot.Runs));
                    }
                }

                var search = new BreadthFirstSearchAlgorithm<SENode, SEEdge>(GraphControl.Graph);
                search.SetRootVertex(currentSubtreeRoot);
                search.FinishVertex += BFS_FinishVertex;
                search.Finished += (p,args) =>
                {
                    if (currentSubtreeRoot.CollapsedSubtreeEdges.Count > 0 && currentSubtreeRoot.CollapsedSubtreeNodes.Count > 0)
                    {
                        foreach (var edge in currentSubtreeRoot.CollapsedSubtreeEdges)
                        {
                            GraphControl.Graph.HideEdge(edge);
                        }
                        foreach (var node in currentSubtreeRoot.CollapsedSubtreeNodes)
                        {
                            GraphControl.Graph.HideVertex(node);
                        }
                        SelectNodeWithProperties(currentSubtreeRoot);
                        currentSubtreeRoot.Collapse();
                        DecorateVerticesBackground();
                    }
                };

                search.Compute();
            }
            else
            {
                // Expanding
                foreach (var vertex in ((sender as VertexControl).Vertex as SENode).CollapsedSubtreeNodes)
                {
                    GraphControl.Graph.UnhideVertex(vertex);
                }
                currentSubtreeRoot.CollapsedSubtreeNodes.Clear();

                GraphControl.Graph.UnhideEdges(((sender as VertexControl).Vertex as SENode).CollapsedSubtreeEdges);
                currentSubtreeRoot.CollapsedSubtreeEdges.Clear();

                currentSubtreeRoot.Expand();
                
                DecorateVerticesBackground();
            }
            
        }

        

        private void BFS_FinishVertex(SENode vertex)
        {
            IEnumerable<SEEdge> edges = null;
            if(GraphControl.Graph.TryGetOutEdges(vertex,out edges))
            {
                if (edges.Count() > 0)
                {
                    foreach (var edge in edges)
                    {
                        currentSubtreeRoot.CollapsedSubtreeEdges.Add(edge);
                    }
                }
            }

            if (!currentSubtreeRoot.Equals(vertex))
            {
                vertex.Deselect();
                currentSubtreeRoot.CollapsedSubtreeNodes.Add(vertex);
                
            }
        }

    }
}