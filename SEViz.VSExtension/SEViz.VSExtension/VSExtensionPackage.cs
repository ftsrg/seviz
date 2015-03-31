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
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Windows.Forms;
using System.IO.Pipes;
using System.IO;
using System.Text;
using System.Threading;
using EnvDTE;

namespace SEViz.VSExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidVSExtensionPkgString)]
    public sealed class VSExtensionPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VSExtensionPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            Pipeserver.package = this;
            ThreadStart pipeThread = new ThreadStart(Pipeserver.createPipeServer);
            System.Threading.Thread listenerThread = new System.Threading.Thread(pipeThread);
            listenerThread.Name = "VSPipe listener";
            listenerThread.SetApartmentState(ApartmentState.STA);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        public class Pipeserver
        {
            public static VSExtensionPackage package;
            private static NamedPipeServerStream pipeServer;
            private static readonly int BufferSize = 256;

            public static void createPipeServer()
            {
                Decoder decoder = Encoding.UTF8.GetDecoder();
                Byte[] bytes = new Byte[BufferSize];
                char[] chars = new char[BufferSize];
                int numBytes = 0;
                StringBuilder msg = new StringBuilder();

                try
                {
                    pipeServer = new NamedPipeServerStream("vspipe"+System.Diagnostics.Process.GetCurrentProcess().Id, PipeDirection.In, 1,
                                                    PipeTransmissionMode.Message,
                                                    PipeOptions.Asynchronous);
                    while (true)
                    {
                        pipeServer.WaitForConnection();

                        do
                        {
                            msg.Length = 0;
                            do
                            {
                                numBytes = pipeServer.Read(bytes, 0, BufferSize);
                                if (numBytes > 0)
                                {
                                    int numChars = decoder.GetCharCount(bytes, 0, numBytes);
                                    decoder.GetChars(bytes, 0, numBytes, chars, 0, false);
                                    msg.Append(chars, 0, numChars);
                                }
                            } while (numBytes > 0 && !pipeServer.IsMessageComplete);
                            decoder.Reset();
                            if (numBytes > 0)
                            {
                                
                                package.SelectLines(msg.ToString());
                            }
                        } while (numBytes != 0);
                        pipeServer.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        public void SelectLines(string urls)
        {
            foreach (var url in urls.Split('|'))
            {
                if (url != "0")
                {
                    var fileUrl = url.Split(':')[0] + ":" + url.Split(':')[1];
                    var line = url.Split(':')[2];
                    EnvDTE.DTE app = (EnvDTE.DTE)GetService(typeof(SDTE));
                    //MessageBox.Show(fileUrl);
                    //MessageBox.Show(line);

                    var window = app.ItemOperations.OpenFile(fileUrl);
                    window.Activate();
                    var selection = (TextSelection)window.Selection;


                    selection.GotoLine(Int32.Parse(line), true);

                    var objEditPt = selection.ActivePoint.CreateEditPoint();
                    objEditPt.SetBookmark();

                    //selection.OutlineSection();
                }
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidVSExtensionCmdSet, (int)PkgCmdIDList.cmdShowInPV);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID );
                mcs.AddCommand( menuItem );
            }

        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            /*
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "VSExtension",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
            */
            EnvDTE.DTE app = (EnvDTE.DTE)GetService(typeof(SDTE));
            if (app.ActiveDocument != null && app.ActiveDocument.Type == "Text")
            {
                EnvDTE.TextDocument text = (EnvDTE.TextDocument)app.ActiveDocument.Object(String.Empty);
                if (!text.Selection.IsEmpty)
                {

                    // File url
                    using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "pexpipe",
                                              PipeDirection.Out,
                                                 PipeOptions.Asynchronous))
                    {
                        try
                        {
                            pipeClient.Connect(2000);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("PexVisualiser is not running. Start it, then try again.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                            return;
                        }
                        using (StreamWriter sw = new StreamWriter(pipeClient))
                        {
                            sw.WriteLine(text.Parent.FullName + "|" + text.Selection.TopPoint.Line.ToString() + "|" + text.Selection.BottomPoint.Line.ToString());
                        }
                    }

                    //MessageBox.Show(text.Parent.Path + text.Parent.FullName +  "|" + text.Selection.TopPoint.Line.ToString()); 
                    //work with text.Selection.Text
                }
            }
        }

    }
}
