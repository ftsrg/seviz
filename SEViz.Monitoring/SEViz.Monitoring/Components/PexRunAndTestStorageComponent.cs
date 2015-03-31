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
