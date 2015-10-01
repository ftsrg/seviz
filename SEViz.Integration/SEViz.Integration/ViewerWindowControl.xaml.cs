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
    using Model;
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
            
            DataContext = new SEGraphViewModel();

            GraphControl.LayoutAlgorithmType = "EfficientSugiyama";
            GraphControl.HighlightAlgorithmType = "Simple";
            GraphControl.Graph = ((SEGraphViewModel)DataContext).Graph;

            DecorateVerticesBackground();
        }

        private void SelectNodes(List<SENode> nodes)
        {
            DeselectAll();
            foreach(var n in nodes)
            {
                n.Select();
            }
            DecorateVerticesBackground();   
        }

        private void DeselectAll()
        {
            foreach(var v in GraphControl.Graph.Vertices)
            {
                v.Deselect();
            }
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

        /// <summary>
        /// Handles left clicks.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Node_OnClick(object sender, RoutedEventArgs e)
        {
            var node = (sender as VertexControl).Vertex as SENode;
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

            var selContainer = new SelectionContainer();
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
            } else
            {
                DeselectAll();
            }

            node.Select();
            DecorateVerticesBackground();
            
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
                        SelectNodes(vm.Data.Runs[currentSubtreeRoot]);
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
                currentSubtreeRoot.CollapsedSubtreeNodes.Add(vertex);
                
            }
        }

    }
}