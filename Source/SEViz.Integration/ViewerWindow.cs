//------------------------------------------------------------------------------
// <copyright file="ViewerWindow.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SEViz.Integration
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using System.Windows.Media;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("531b782e-c65a-44c8-a902-1a43ae3f568a")]
    public class ViewerWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewerWindow"/> class.
        /// </summary>
        public ViewerWindow() : base(null)
        {
            this.Caption = "SEViz";
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new ViewerWindowControl(this);
        }

        internal object GetVsService(Type service)
        {
            return GetService(service);
        }
    }
}
