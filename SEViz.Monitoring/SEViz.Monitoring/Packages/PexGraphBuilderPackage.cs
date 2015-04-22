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

using SEViz.Monitoring.Components;
using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.ComponentModel;
using Microsoft.ExtendedReflection.Emit;
using Microsoft.ExtendedReflection.Interpretation;
using Microsoft.ExtendedReflection.Metadata;
using Microsoft.ExtendedReflection.Reasoning.ExecutionNodes;
using Microsoft.ExtendedReflection.Symbols;
using Microsoft.ExtendedReflection.Utilities.Safe.IO;
using Microsoft.Pex.Engine.ComponentModel;
using Microsoft.Pex.Engine.Packages;
using Microsoft.Pex.Engine.TestGeneration;
using Microsoft.Pex.Framework.Packages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SEViz.Monitoring.Packages
{
    

    public class PexGraphBuilderPackageAttribute : PexPathPackageAttributeBase
    {
        private string outFileUrl;

        private IPexPathComponent pathComponent;

        private string includeList;

        private string excludeList;

        public PexGraphBuilderPackageAttribute(string outFileUrl, string includeList, string excludeList)
        {
            this.outFileUrl = outFileUrl;
            this.includeList = includeList;
            this.excludeList = excludeList;
            
        }

        /// <summary>
        /// The method, which is called after each Pex run.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="data"></param>
        protected override void AfterRun(IPexPathComponent host, object data)
        {
            
            try
            {
                var nodesInPath = host.PathServices.CurrentExecutionNodeProvider.ReversedNodes.Reverse().ToArray();

                var storage = ((Tuple<PexExecutionNodeStorageComponent, PexRunAndTestStorageComponent>)data).Item1;

                var runStorage = ((Tuple<PexExecutionNodeStorageComponent, PexRunAndTestStorageComponent>)data).Item2;

                var numberOfRuns = host.ExplorationServices.Driver.Runs;

                if (runStorage.Runs.Where(t => t.Item1 == numberOfRuns).FirstOrDefault() == null)
                {
                    runStorage.Runs.Add(new Tuple<int, int>(numberOfRuns, 0));
                }

                var nodeIndexes = new List<int>();

                foreach (var node in nodesInPath)
                {

                    nodeIndexes.Add(node.UniqueIndex);

                    if (storage.NodeInstances.ContainsKey(node.UniqueIndex))
                    {
                        storage.NodeInstances.Remove(node.UniqueIndex);
                    }
                    storage.NodeInstances.Add(node.UniqueIndex, node);

                    var gvStringBuilder = new StringBuilder();

                    var ind = nodesInPath.ToList().IndexOf(node);

                    IExecutionNode prevNode = null;
                    if (ind > 0)
                    {
                        prevNode = nodesInPath[ind - 1];
                        List<int> knownSuccessors = new List<int>();
                        if (storage.KnownSuccessors.ContainsKey(prevNode.UniqueIndex))
                        {
                            knownSuccessors = storage.KnownSuccessors[prevNode.UniqueIndex];
                        }
                        if (!knownSuccessors.Contains(node.UniqueIndex))
                        {
                            var splittedIncludeList = new List<string>(includeList.Split(';').ToArray());

                            if (prevNode.CodeLocation.Method != null && splittedIncludeList.Count > 0 && node.CodeLocation.Method != null)
                            {

                                if ((prevNode.CodeLocation.Method.FullName.StartsWith("System")
                                    || prevNode.CodeLocation.Method.FullName.StartsWith("Microsoft")
                                    || splittedIncludeList.Where(e => prevNode.CodeLocation.Method.FullName.StartsWith(e)).Count() > 0) &&
                                    (!node.CodeLocation.Method.FullName.StartsWith("System")
                                    && !node.CodeLocation.Method.FullName.StartsWith("Microsoft")
                                    && splittedIncludeList.Where(e => node.CodeLocation.Method.FullName.StartsWith(e)).Count() == 0))
                                {
                                    gvStringBuilder.AppendLine(prevNode.UniqueIndex + " -> " + node.UniqueIndex + " [color=red]");
                                }
                                else
                                {
                                    gvStringBuilder.AppendLine(prevNode.UniqueIndex + " -> " + node.UniqueIndex);
                                }
                            }
                            else
                            {
                                gvStringBuilder.AppendLine(prevNode.UniqueIndex + " -> " + node.UniqueIndex);
                            }
                            knownSuccessors.Add(node.UniqueIndex);
                        }
                        storage.KnownSuccessors.Remove(prevNode.UniqueIndex);
                        storage.KnownSuccessors.Add(prevNode.UniqueIndex, knownSuccessors);
                    }
                    
                    gvStringBuilder.AppendLine(CreateSameRankStatement(node, storage));

                    using (var gvWriter = new StreamWriter(outFileUrl, true))
                    {
                        gvWriter.WriteLine(gvStringBuilder.ToString());
                    }

                    if (storage.Nodes.Where(n => n.Item1 == node.UniqueIndex).Count() == 0)
                    {
                        using (var infoWriter = new StreamWriter(outFileUrl + ".info", true))
                        {
                            infoWriter.WriteLine("<Tip Tag=\"" + node.UniqueIndex + "\">");
                            infoWriter.WriteLine("<TextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xml:space=\"preserve\">");

                            var tuple = MapToSourceCode(host, node);

                            if (tuple != null)
                            {
                                using (var gvWriter = new StreamWriter(outFileUrl, true))
                                {
                                    gvWriter.WriteLine(node.UniqueIndex + " [ color=\"gray\" peripheries=2 ]");
                                }
                            }

                            string methodName = "No method available";

                            if (node.CodeLocation.Method == null)
                            {
                                if (node.InCodeBranch.Method != null)
                                {
                                    methodName = node.InCodeBranch.Method.FullName;
                                }
                            }
                            else
                            {
                                methodName = node.CodeLocation.Method.FullName;
                            }

                            var pathCondition = PrettyPrintPathCondition(node);

                            infoWriter.WriteLine("<Bold>" + pathCondition.Replace('\"', '.').Replace("<", ";lt;").Replace(">", ";gt;").Replace("&", "&amp;") + "</Bold>"
                                               + "<LineBreak />" + methodName.Replace("<", ";lt;").Replace(">", ";gt;").Replace("&", "&amp;")
                                               + "<LineBreak />" + ((tuple == null) ? "No source mapping available" : (tuple.Item2 + ":" + tuple.Item1))

                                               );

                            infoWriter.WriteLine("</TextBlock>");
                            infoWriter.WriteLine("</Tip>");
                            storage.Nodes.Add(new Tuple<int, int>(node.UniqueIndex, node.Depth));
                            storage.NodeLocations.Add(node.UniqueIndex, methodName + ":" + node.CodeLocation.Offset);
                        }
                    }
                }
                
                runStorage.NodesInPath.Add(numberOfRuns, nodeIndexes);

            }
            catch (Exception e)
            {
                // TODO : Exception handling ?
            }
        }
        
        /// <summary>
        /// The method, which is called before each Pex run.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        protected override object BeforeRun(IPexPathComponent host)
        {
            pathComponent = host;

            var runStorage = host.GetService<PexRunAndTestStorageComponent>();

            return new Tuple<PexExecutionNodeStorageComponent, PexRunAndTestStorageComponent>(host.GetService<PexExecutionNodeStorageComponent>(),runStorage );
        }

        /// <summary>
        /// Maps the node to the nearest sequence point's line in the source code.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="node"></param>
        /// <returns>Null or a tuple containing the line and the URL of the document.</returns>
        private Tuple<int,string> MapToSourceCode(IPexPathComponent host, IExecutionNode node)
        {
            try
            {
                var symbolManager = host.GetService<ISymbolManager>();
                var sourceManager = host.GetService<ISourceManager>();
                MethodDefinitionBodyInstrumentationInfo nfo;
                if (node.CodeLocation.Method == null)
                {
                    if (node.InCodeBranch.Method == null) { return null; }
                    else { node.InCodeBranch.Method.TryGetBodyInstrumentationInfo(out nfo); }
                }
                else { node.CodeLocation.Method.TryGetBodyInstrumentationInfo(out nfo); }
                SequencePoint point;
                int targetOffset;
                nfo.TryGetTargetOffset(node.InCodeBranch.BranchLabel, out targetOffset);
                if (symbolManager.TryGetSequencePoint(node.CodeLocation.Method == null ? node.InCodeBranch.Method : node.CodeLocation.Method, node.CodeLocation.Method == null ? targetOffset : node.CodeLocation.Offset, out point))
                {
                    return new Tuple<int, string>(point.Line, point.Document);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                // TODO : Exception handling?
                return null;
            }
        }

        /// <summary>
        /// Creates GraphViz rank=same statements for nodes, which have same heights.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        private string CreateSameRankStatement(IExecutionNode node, PexExecutionNodeStorageComponent storage)
        {
            StringBuilder b = new StringBuilder();
            try
            {

                b.Append("{rank=same; " + node.UniqueIndex);

                foreach (var knownNode in storage.Nodes)
                {

                    if (knownNode != node)
                    {
                        if (knownNode.Item2 == node.Depth) b.Append(" " + knownNode.Item1);
                    }
                }

                b.AppendLine(" }");
            }
            catch (Exception e)
            {
                // TODO : Exception handling?
            }
            return b.ToString();
        }

        /// <summary>
        /// Pretty prints the path condition of the given execution node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string PrettyPrintPathCondition(IExecutionNode node)
        {
            string output = "";
            try
            {
                if (node.GetPathCondition().Conjuncts.Count != 0)
                {
                    TermEmitter termEmitter = new TermEmitter(pathComponent.GetService<TermManager>());
                    SafeStringWriter safeStringWriter = new SafeStringWriter();
                    IMethodBodyWriter methodBodyWriter = pathComponent.GetService<IPexTestManager>().Language.CreateBodyWriter(safeStringWriter, VisibilityContext.Private, 2000);
                    if (termEmitter.TryEvaluate(node.GetPathCondition().Conjuncts, 2000, methodBodyWriter))
                    {
                        for (int i = 0; i < node.GetPathCondition().Conjuncts.Count - 1; i++)
                        {
                            methodBodyWriter.ShortCircuitAnd();
                        }

                        methodBodyWriter.Statement();

                        output = safeStringWriter.ToString().Remove(safeStringWriter.ToString().Count() - 3);
                    }
                }
            }
            catch (Exception e)
            {
                // TODO : Exception handling?
            }
            return output;
        }
    }
}
