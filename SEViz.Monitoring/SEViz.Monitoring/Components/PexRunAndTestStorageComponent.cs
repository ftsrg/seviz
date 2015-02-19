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
using Microsoft.Pex.Engine.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SEViz.Monitoring.Components
{

    public class PexRunAndTestStorageComponent : PexExplorationComponentBase, IService
    {
        public List<Tuple<int, int>> Runs { get; set; }

        public Dictionary<int, List<int>> NodesInPath { get; set; }

        public PexRunAndTestStorageComponent()
        {

            // Second generic parameter: 0 - no test, 1 - test ok, 2 - test fail
            Runs = new List<Tuple<int, int>>();

            NodesInPath = new Dictionary<int, List<int>>();
        }
    }
}
