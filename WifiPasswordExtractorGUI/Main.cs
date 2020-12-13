using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WifiPasswordExtract;

namespace WifiPasswordExtractorGUI
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void btn_search_Click(object sender, EventArgs e)
        {
            Processing prc = new Processing();
            prc.Process = Extractor.ExtractPasswordsAsync();
            prc.ShowDialog();

            MakeTable(prc.Process.Result);
        }

        private void MakeTable(IEnumerable<WifiCredential> creds)
        {
            lview_pass.Items.Clear();

            foreach (var cred in creds)
            {
                var item = new ListViewItem(cred.SSID);

                item.SubItems.Add(cred.AuthType);

                if (!cred.Open)
                    item.SubItems.Add(cred.Password);

                if (cred.OneX)
                {
                    item.SubItems.Add(cred.Username);
                    item.SubItems.Add(cred.Domain);
                }

                lview_pass.Items.Add(item);
            }

            lview_pass.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void btn_close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void btn_csv_Click(object sender, EventArgs e)
        {
            if (sfd_csv.ShowDialog() != DialogResult.OK) return;

            List<string> lviewlines = new List<string>();
            foreach (ListViewItem lvi in lview_pass.Items)
            {
                string lstr = string.Empty;
                foreach (ListViewItem.ListViewSubItem lvic in lvi.SubItems)
                {
                    lstr += '"' + lvic.Text.Replace("\"", "\\\"") + '"' + ',';
                }
                lstr = lstr.TrimEnd(',');
                lviewlines.Add(lstr);
            }

            UseWaitCursor = true;
            await Task.Run(() => System.IO.File.WriteAllLines(sfd_csv.FileName, lviewlines.ToArray(), Encoding.UTF8));
            UseWaitCursor = false;
        }
    }
}