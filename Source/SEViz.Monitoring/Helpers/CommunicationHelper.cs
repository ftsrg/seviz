using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;


namespace SEViz.Monitoring.Helpers
{
    public static class CommunicationHelper
    {
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        /// <summary>
        /// Gets the Visual Studio DTE instance by searching the running objects table using COM.
        /// </summary>
        /// <param name="processId">The VS instance Id to search for.</param>
        /// <returns>The searched DTE object.</returns>
        public static DTE GetDTEByProcessId(int processId)
        {
            // TODO refactor
            string idToSearch = "!VisualStudio.DTE.14.0:" + processId.ToString();
            object runningInstance = null;

            IBindCtx bindCtx = null;
            IRunningObjectTable runningObjectTable = null;
            IEnumMoniker enumMonikers = null;

            try {
                Marshal.ThrowExceptionForHR(CreateBindCtx(0, out bindCtx));
                bindCtx.GetRunningObjectTable(out runningObjectTable);
                runningObjectTable.EnumRunning(out enumMonikers);

                IMoniker[] moniker = new IMoniker[1];
                IntPtr numberFetched = IntPtr.Zero;

                while (enumMonikers.Next(1, moniker, numberFetched) == 0)
                {
                    IMoniker runningObjectMoniker = moniker[0];
                    string name = null;

                    try
                    {
                        if (runningObjectMoniker != null)
                        {
                            runningObjectMoniker.GetDisplayName(bindCtx, null, out name);
                        }
                    }
                    catch (UnauthorizedAccessException) {
                        
                    }
                   
                    if (!string.IsNullOrEmpty(name) && string.Equals(name, idToSearch, StringComparison.Ordinal))
                    {
                        Marshal.ThrowExceptionForHR(runningObjectTable.GetObject(runningObjectMoniker, out runningInstance));

                        break;
                    }
                }
            } finally
            {
                if (enumMonikers != null) Marshal.ReleaseComObject(enumMonikers);
                if (runningObjectTable != null) Marshal.ReleaseComObject(runningObjectTable);
                if (bindCtx != null) Marshal.ReleaseComObject(bindCtx);
            }
            return (DTE)runningInstance;
        }
    }
}
