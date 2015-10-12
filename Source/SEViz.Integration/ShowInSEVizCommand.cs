//------------------------------------------------------------------------------
// <copyright file="ShowInSEVizCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SEViz.Integration
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ShowInSEVizCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("74e2d6a0-87e9-4492-86f4-c78b36274c1f");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowInSEVizCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ShowInSEVizCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ShowInSEVizCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new ShowInSEVizCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var window = package.FindToolWindow(typeof(ViewerWindow), 0, true) as ViewerWindow;
            var content = window.Content as ViewerWindowControl;
            
            EnvDTE.DTE app = (EnvDTE.DTE)this.ServiceProvider.GetService(typeof(SDTE));
            if (app.ActiveDocument != null && app.ActiveDocument.Type == "Text")
            {
                EnvDTE.TextDocument text = (EnvDTE.TextDocument)app.ActiveDocument.Object(String.Empty);
                if (!text.Selection.IsEmpty)
                {
                    content.FindAndSelectNodesByLocation(text.Parent.FullName,text.Selection.TopPoint.Line,text.Selection.BottomPoint.Line);
                }
            }

                    
            
        }
    }
}
