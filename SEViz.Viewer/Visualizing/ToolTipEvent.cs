/*
 * SEViz - Symbolic Execution VIsualiZation
 *
 * SEViz is a tool, which can support the test generation process by
 * visualizing the symbolic execution in a directed graph.
 *
 * Budapest University of Technology and Economics (BME)
 *
 * Authors: Dávid Honfi <david.honfi@inf.mit.bme.hu>, Zoltán Micskei
 * <micskeiz@mit.bme.hu>, András Vörös <vori@mit.bme.hu>
 * 
 * The file is an extension of Rodemeyer.Visualizing (Dot2WPF) by Christian Rodemeyer,
 * which is licensed under the Code Project Open License (CPOL).
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Rodemeyer.Visualizing
{
    public class NodeTipEventArgs : RoutedEventArgs
    {
        public NodeTipEventArgs(RoutedEvent routedEvent, object source, object tag)
            : base(routedEvent, source)
        {
            Tag = tag;
        }

        public readonly object Tag;
        public object Content;
    }

    public delegate void NodeTipEventHandler(object sender, NodeTipEventArgs e);
}
