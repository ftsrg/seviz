/*
 * SEViz - Symbolic Execution VIsualiZation
 *
 * SEViz is a tool, which can support the test generation process by
 * visualizing the symbolic execution in a directed graph.
 *
 * Authors: Dávid Honfi <david.honfi@inf.mit.bme.hu>, Zoltán Micskei
 * <micskeiz@mit.bme.hu>, András Vörös <vori@mit.bme.hu>
 * 
 * Copyright 2015 Budapest University of Technology and Economics (BME)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 * 
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
