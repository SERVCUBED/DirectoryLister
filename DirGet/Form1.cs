using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace DirGet
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            adminBtn.Visible = !(new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void getBtn_Click(object sender, EventArgs e)
        {
            ListDirectories(false);
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            Text = @"Directory Lister";
        }

        private void getNoRootBtn_Click(object sender, EventArgs e)
        {
            ListDirectories(true);
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sw = new StreamWriter(saveFileDialog1.FileName);
                for (var i = 0; i < listBox1.Items.Count - 1; i++)
                {
                    sw.WriteLine(listBox1.Items[i]);
                }
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                    throw;
                MessageBox.Show(@"Cannot write to file:" + Environment.NewLine + ex.Message);
            }
        }

        private void browseBtn_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = uriBox.Text;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                uriBox.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        /// <summary>
        /// Lists the requested directories and adds them to the ListBox.
        /// </summary>
        /// <param name="shouldOmitRoot">True if the root directory should not be shown in the result.</param>
        private void ListDirectories(bool shouldOmitRoot)
        {
            if (!Directory.Exists(uriBox.Text))
            {
                MessageBox.Show(@"Directory does not exist or access is denied.", @"Directory Lister",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (listBox1.Items.Count > 0 && MessageBox.Show(@"Keep current items?", @"Directory Lister", MessageBoxButtons.YesNo) != DialogResult.Yes)
                listBox1.Items.Clear();

            Enabled = false;
            Text = @"Directory Lister (Working. Please wait...)";
            
            var dirs = DirectoryList(uriBox.Text, true, null);

            try
            {
                foreach (var dir in dirs.Where(dir => dir != null))
                {
                    listBox1.Items.Add(shouldOmitRoot ? dir.Replace(uriBox.Text, "") : dir);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.InnerException + Environment.NewLine +
                                ex.Source);
                if (Debugger.IsAttached)
                    throw;
            }

            Enabled = true;
            Text = $"Directory Lister ({listBox1.Items.Count} items)";
        }

        /// <summary>
        /// Returns a list of directories under RootDirectory 
        /// </summary>
        /// <param name="rootDirectory">The starting directory.</param>
        /// <param name="searchAllDirectories">When true, all sub directories will be searched as well.</param>
        /// <param name="filter">filter to be done on directory. use null for no filtering.</param>
        /// <returns>A list of directories under RootDirectory</returns>
        private List<string> DirectoryList(string rootDirectory, bool searchAllDirectories, Predicate<string> filter)
        {
            var retList = new List<string>();

            DirectoryInfo[] di;
            try
            {
                di = new DirectoryInfo(rootDirectory).GetDirectories();
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                    throw;

                if (logErrorsChk.Checked) retList.Add("> " + ex.Message);

                return retList;
            }
            
            foreach (var folder in di)
            {
                try
                {
                    if ((filter == null) || (filter(folder.FullName)))
                    {
                        retList.Add(folder.FullName);

                        if (searchAllDirectories)
                        {
                            var l = DirectoryList(folder.FullName, true, filter);
                            if (l.Count > 0)
                                retList.AddRange(l);
                        }
                    }
                }

                catch (UnauthorizedAccessException ex)
                {
                    if (logErrorsChk.Checked) retList.Add("> " + ex.Message);
                }

                catch (Exception)
                {
                    if (Debugger.IsAttached)
                        throw;
                }
            }
            
            return retList;
        }

        private void copyBtn_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
                Clipboard.SetText(listBox1.SelectedItem.ToString());
        }

        private void adminBtn_Click(object sender, EventArgs e)
        {
            var info = new ProcessStartInfo(Assembly.GetEntryAssembly().Location)
            {
                Verb = "runas",
            };

            var process = new Process
            {
                StartInfo = info
            };

            try
            {
                process.Start();
                Close();
            }
            catch (Exception)
            {
                // User pressed 'no' at UAC prompt
            }
        }
    }
}
