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
using Microsoft.Pex.Engine.ComponentModel;
using Microsoft.Pex.Framework.Packages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Management;
using SEViz.Monitoring.Helpers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SEViz.Common;
using System.ComponentModel.Design;

namespace SEViz.Monitoring.PexPackages
{
    public class PexGraphBuilderPackageHelper : PexExplorationPackageAttributeBase
    {

        private string outFileUrl;

        public PexGraphBuilderPackageHelper(string outFileUrl)
        {
            this.outFileUrl = outFileUrl;
        }

        /// <summary>
        /// Initialization of the Pex engine.
        /// </summary>
        /// <param name="host"></param>
        protected override void Initialize(IPexExplorationEngine host)
        {
            host.AddComponent("NodeStorage", new PexExecutionNodeStorageComponent());
            host.AddComponent("RunStorage", new PexRunAndTestStorageComponent());
            
        }

        /// <summary>
        /// Method, which is called before each exploration.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        protected override object BeforeExploration(IPexExplorationComponent host)
        {
            
            using (var gvWriter = new StreamWriter(outFileUrl, true))
            {
                gvWriter.WriteLine("digraph {");
                gvWriter.WriteLine("node [ fontsize = \"16\" shape = \"box\" fillcolor = \"white\"];");
            }

            using (var infoWriter = new StreamWriter(outFileUrl + ".info", true))
            {
                infoWriter.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                infoWriter.WriteLine("<GraphInfo>");
            }

            host.Log.ProblemHandler += (p1) => {
                try
                {
                    var nodeStorage = host.GetService<PexExecutionNodeStorageComponent>();
                    nodeStorage.Z3Locations.Add((p1.FlippedLocation.Method == null ? "" : (p1.FlippedLocation.Method.FullName + ":" + p1.FlippedLocation.Offset)));
                }
                catch (Exception ex)
                {
                    // TODO : Exception handling?
                }
            } ;

            using (var testWriter = new StreamWriter(outFileUrl + ".tests", true))
            {
                testWriter.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                testWriter.WriteLine("<Methods>");
            }

            host.Log.GeneratedTestHandler += (p1) =>
            {
                try
                {
                    var currentRun = p1.GeneratedTest.Run;
                    var runStorage = host.GetService<PexRunAndTestStorageComponent>();

                    using (var testWriter = new StreamWriter(outFileUrl + ".tests", true))
                    {
                        testWriter.WriteLine("<MethodCode Run=\"" + currentRun + "\" Exception=\"" + p1.GeneratedTest.ExceptionState + "\" Failed=\"" + p1.GeneratedTest.IsFailure + "\"><![CDATA[");
                        testWriter.WriteLine(p1.GeneratedTest.MethodCode);
                        testWriter.WriteLine("]]></MethodCode>");
                    }

                    runStorage.Runs.Add(new Tuple<int, int>(currentRun, p1.GeneratedTest.IsFailure ? 2 : 1));
                }
                catch (Exception ex)
                {
                    // TODO : Exception handling?
                }
            };

            return null;
        }

        /// <summary>
        /// Method, which is called after each exploration.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="data"></param>
        protected override void AfterExploration(IPexExplorationComponent host, object data)
        {
            base.AfterExploration(host, data);

            var runStorage = host.GetService<PexRunAndTestStorageComponent>();

            var nodeStorage = host.GetService<PexExecutionNodeStorageComponent>();

            using (var testWriter = new StreamWriter(outFileUrl + ".tests", true))
            {
                testWriter.WriteLine("</Methods>");
            }

            using (StreamWriter runWriter = new StreamWriter(outFileUrl + ".runs", true))
            {
                foreach (var run in runStorage.Runs.OrderBy(t => t.Item1))
                {
                    StringBuilder indexes = new StringBuilder();
                    
                    foreach (var node in runStorage.NodesInPath[run.Item1])
                    {
                        indexes.Append(node);
                        indexes.Append(",");
                    }

                    indexes.Remove(indexes.Length - 1, 1);

                    runWriter.WriteLine(run.Item1 + "|" + run.Item2 + "|" + indexes.ToString());
                }
            }

            var gvStringBuilder = new StringBuilder();

            foreach (var node in nodeStorage.NodeLocations)
            {
                if (nodeStorage.Z3Locations.Contains(node.Value))
                {
                    gvStringBuilder.AppendLine(node.Key + " [shape=ellipse]");
                }
            }

            foreach (var run in runStorage.Runs.OrderBy(t => t.Item1))
            {
                int last = -1;

                if (runStorage.NodesInPath.ContainsKey(run.Item1))
                {
                    last = runStorage.NodesInPath[run.Item1].Last();
                    
                }
                if (last != -1) gvStringBuilder.AppendLine(last + " [fillcolor=" + ((run.Item2 == 0) ? "orange" :  ( run.Item2 == 1 ? "limegreen" : "firebrick")) + "]");
            }

            gvStringBuilder.AppendLine("}");

            using (var gvWriter = new StreamWriter(outFileUrl, true))
            {
                gvWriter.WriteLine(gvStringBuilder.ToString());
            }

            using (var exhWriter = new StreamWriter(outFileUrl + ".exh", true))
            {
                foreach (var node in nodeStorage.NodeInstances)
                {
                    exhWriter.WriteLine(node.Key + "|" + node.Value.ExhaustedReason);
                }
            }

            using (var infoWriter = new StreamWriter(outFileUrl + ".info", true))
            {
                infoWriter.WriteLine("</GraphInfo>");
            }

            /*
            using (var metaWriter = new StreamWriter(outFileUrl + ".metainfo"))
            {
                metaWriter.WriteLine("maxdepth:"+nodeStorage.NodeInstances.Max(n => n.Value.Depth));
                metaWriter.WriteLine("nodecount:"+nodeStorage.NodeInstances.Count);
            }*/

            // Invoking GraphViz
            string strCmdText = "/C dot -Tplain-ext -o " + outFileUrl + ".plain  " + outFileUrl;
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);

            var dir = outFileUrl.TrimEnd(outFileUrl.Split('\\').Last().ToCharArray());
            string fileName = outFileUrl.Split('\\').Last();


            var myId = Process.GetCurrentProcess().Id;
            var query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", myId);
            var search = new ManagementObjectSearcher("root\\CIMV2", query);
            var results = search.Get().GetEnumerator();
            results.MoveNext();
            var queryObj = results.Current;
            var parentId = (uint)queryObj["ParentProcessId"];
            var dte = CommunicationHelper.GetDTEByProcessId(Convert.ToInt32(parentId));
            

            
            
            var prov = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);

            
            
            var shell = (IVsUIShell)prov.GetService(typeof(SVsUIShell));
            var cmdSvc = (OleMenuCommandService)prov.GetService(typeof(OleMenuCommandService));

            using(var w = new StreamWriter(@"D:\debuggg.txt",true))
            {
                if (cmdSvc == null) w.WriteLine("cmdsvc is null");
            }

            cmdSvc.GlobalInvoke(new CommandID(new Guid("58d15630-1a1f-4a3c-890a-99d1faa69970"), 0x0100));
            /*
            IVsWindowFrame frame = null;
            if (shell != null)
            {

                var guidSeviz = new Guid("531b782e-c65a-44c8-a902-1a43ae3f568a");
                shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guidSeviz, out frame);
                
            }

            if(frame != null)
            {
                frame.Show();
            }*/
            
            // Waiting GraphViz to complete... TODO
            Thread.Sleep(5000);
            /*
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(dir +  fileName,"files");
                zip.AddFile(dir +  fileName + ".plain","files");
                zip.AddFile(dir +  fileName + ".info","files");
                zip.AddFile(dir + fileName + ".runs", "files");
                zip.AddFile(dir + fileName + ".tests", "files");
                zip.AddFile(dir + fileName + ".exh", "files");

                zip.Save(dir + fileName + ".sviz");
            }*/

            // Deleting temporary files
            File.Delete(dir + fileName);
            File.Delete(dir + fileName + ".plain");
            File.Delete(dir + fileName + ".info");
            File.Delete(dir + fileName + ".runs");
            File.Delete(dir + fileName + ".tests");
            File.Delete(dir + fileName + ".exh");
        }
    }
}
