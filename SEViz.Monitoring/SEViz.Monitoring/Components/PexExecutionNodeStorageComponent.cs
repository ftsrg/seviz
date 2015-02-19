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
 * All rights reserved.
 */
using Microsoft.ExtendedReflection.ComponentModel;
using Microsoft.ExtendedReflection.Reasoning.ExecutionNodes;
using Microsoft.Pex.Engine.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SEViz.Monitoring.Components
{
    public class PexExecutionNodeStorageComponent : PexExplorationComponentBase, IService
    {
        public List<Tuple<int, int>> Nodes { get; set; }

        public Dictionary<int,IExecutionNode> NodeInstances { get; set; }

        public Dictionary<int,string> NodeLocations { get; set; }

        public List<string> Z3Locations { get; set; }

        public Dictionary<int, List<int>> KnownSuccessors { get; set; }

        public PexExecutionNodeStorageComponent()
        {
            Nodes = new List<Tuple<int, int>>();
            NodeLocations = new Dictionary<int,string>();
            NodeInstances = new Dictionary<int, IExecutionNode>();
            Z3Locations = new List<string>();
            KnownSuccessors = new Dictionary<int, List<int>>();
        }
    }
}
