using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using WifiPasswordExtract;

namespace WifiPasswordExtractorGUI
{
    public partial class Processing : Form
    {
        public Processing()
        {
            InitializeComponent();
        }

        public Task<IEnumerable<WifiCredential>> Process = null;

        private void Processing_Shown(object sender, EventArgs e)
        {
        }

        private void timerCheckProcess_Tick(object sender, EventArgs e)
        {
            if (Process == null) return;

            if (Process.IsCompleted)
            {
                timerCheckProcess.Enabled = false;
                Close();
            }
        }
    }
}