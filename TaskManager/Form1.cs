using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace TaskManager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Process[] procs;
        public void updProcesses()
        {
            procs = Process.GetProcesses();

        }
        public class ListViewP
        {
            ListView lstView = new ListView();
            public Process[] getProcesses()
            {
                return Process.GetProcesses();
            }
            public ListView Initializate(TaskManager.Form1 form1)
            {
                lstView.Bounds = new Rectangle(new Point(-1, 0), new Size(form1.ClientSize.Width, form1.ClientSize.Width));
                lstView.View = View.Details;
                lstView.FullRowSelect = true;
                lstView.GridLines = true;
                lstView.Sorting = SortOrder.Ascending;
                ContextMenuStrip cntMenu = form1.contextMenuStrip1;
                lstView.ContextMenuStrip = cntMenu;
                lstView.BorderStyle = BorderStyle.None;

                lstView.Columns.Add("Имя процесса", 190, HorizontalAlignment.Left);
                lstView.Columns.Add("Pid", 100, HorizontalAlignment.Center);
                lstView.Columns.Add("RAM", 120, HorizontalAlignment.Center);

                form1.Controls.Add(lstView);

                foreach (var p in getProcesses())
                {
                    string[] row = { p.Id.ToString(), SizeSuffix(p.WorkingSet64)};
                    ListViewItem newitem = new ListViewItem();
                    if (GetProcessOwner(p) == null)
                    {
                        newitem.BackColor = Color.FromArgb(255, 212, 212);
                    }
                    newitem.Text = p.ProcessName;
                    newitem.SubItems.AddRange(row);
                    lstView.Items.Add(newitem);
                }

                theTimer();

                return lstView;

            }
            public void ListViewUpdate(Object source, ElapsedEventArgs e)
            {
                Action update = () =>
                {
                    lstView.SuspendLayout();
                    lstView.BeginUpdate();

                    Process[] processes = getProcesses();
                    string[] pids = processes.ToList().ConvertAll<string>(p => p.Id.ToString()).ToArray();
                    foreach (var p in processes)
                    {
                        string [] row = { p.Id.ToString(), SizeSuffix(p.WorkingSet64) };
                        
                        if (lstView.FindItemWithText(row[0].ToString(), true, 0) == null)
                        {
                            ListViewItem newitem = new ListViewItem();
                            if (GetProcessOwner(p) == null)
                            {
                                newitem.BackColor = Color.FromArgb(255, 212, 212);
                            }
                            newitem.Text = p.ProcessName;
                            newitem.SubItems.AddRange(row);
                            lstView.Items.Add(newitem);
                        }
                        foreach (ListViewItem l in lstView.Items)
                        {
                            if (l.SubItems[1].Text == row[0] && (l.Text != p.ProcessName || l.SubItems[2].Text != row[1]))
                            {
                                if (GetProcessOwner(p) == null)
                                {
                                    l.BackColor = Color.FromArgb(255, 212, 212);
                                }
                                l.Text = p.ProcessName;
                                l.SubItems[1].Text = row[0];
                                l.SubItems[2].Text = row[1];
                            }
                            
                            else if (!pids.Contains(l.SubItems[1].Text))
                            {
                                lstView.Items.Remove(lstView.FindItemWithText(l.SubItems[1].Text));
                            }
                        }
                    }

                    lstView.EndUpdate();
                    lstView.ResumeLayout();
                };
                if (lstView.InvokeRequired)
                    lstView.Invoke(update);
                else
                    update();
            }

            public void theTimer()
            {
                System.Timers.Timer myTimer = new System.Timers.Timer(1000);
                myTimer.Elapsed += new ElapsedEventHandler(ListViewUpdate);
                myTimer.Start();
            }

            public void killProcess()
            {
                foreach (ListViewItem slctd in lstView.SelectedItems)
                {
                    int spid;
                    Int32.TryParse(slctd.SubItems[1].Text, out spid);
                    Process.GetProcessById(spid).Kill();
                }
            }
            public void closeProcess()
            {
                foreach (ListViewItem slctd in lstView.SelectedItems)
                {
                    int spid;
                    Int32.TryParse(slctd.SubItems[1].Text, out spid);
                    var cp = Process.GetProcessById(spid);
                    cp.CloseMainWindow();
                }
            }
            [DllImport("advapi32.dll", SetLastError = true)]
            private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CloseHandle(IntPtr hObject);
            public static string GetProcessOwner(Process process)
            {
                IntPtr processHandle = IntPtr.Zero;
                try
                {
                    OpenProcessToken(process.Handle, 8, out processHandle);
                    WindowsIdentity wi = new WindowsIdentity(processHandle);
                    string user = wi.Name;
                    return user.Contains(@"\") ? user.Substring(user.IndexOf(@"\") + 1) : user;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    if (processHandle != IntPtr.Zero)
                    {
                        CloseHandle(processHandle);
                    }
                }
            }
            static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            static string SizeSuffix(Int64 value, int decimalPlaces = 1)
            {
                if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
                if (value < 0) { return "-" + SizeSuffix(-value); }
                if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }
                int mag = (int)Math.Log(value, 1024);
                decimal adjustedSize = (decimal)value / (1L << (mag * 10));
                if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
                {
                    mag += 1;
                    adjustedSize /= 1024;
                }

                return string.Format("{0:n" + decimalPlaces + "} {1}",
                    adjustedSize,
                    SizeSuffixes[mag]);
            }
        }

        public ListViewP LstViewP = new ListViewP();

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeComponent();
            LstViewP.Initializate(this);
        }

        private void killProcessToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            LstViewP.killProcess();
        }

        private void closeProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LstViewP.closeProcess();
        }
    }
}
