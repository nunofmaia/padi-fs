using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace padiFS
{
    public class PuppetMaster : MarshalByRefObject, IPuppetMaster
    {
        private static Form1 form;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            form = new Form1();

            Application.Run(form);
        }

        public void RegisterClose(string s)
        {
            form.ClosedProcesses(s);
        }
    }
}
