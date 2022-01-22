using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Shell32;
using System.IO;
using System.Collections;
using System.Diagnostics;
using Microsoft.Win32;

namespace BasicSearch
{
    public partial class MainForm : Form
    {
        const string APP_NAME = "BasicSearch";

        const string REGISTRY_STARTUP = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        string REGISTRY_APP_FOLDER = String.Format("SOFTWARE\\Windows PowerUps\\{0}", APP_NAME);
        const string REGISTRY_FIRST_RUN = "DidRunOnce";

        public MainForm()
        {
            InitializeComponent();
            Initialize();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                GetActiveWindow();
            }
            base.WndProc(ref m);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void Initialize()
        {
            if (IsAlreadyRunning())
            {
                CloseApplication();
            }

            SetupWindowStatus();
            SetupHotKey();
            SetupTrayMenuContext();
            SetStartup();

            if (IsFirstRun())
            {
                ShowAboutForm();
            }
        }

        private bool IsAlreadyRunning()
        {
            Process[] p = Process.GetProcessesByName(APP_NAME);
            if (p.Length > 1)
            {
                MessageBox.Show(string.Format("It seems like {0} is already running...", APP_NAME));
                return true;
            }
            return false;
        }

        private void SetupWindowStatus()
        {
            this.ShowInTaskbar = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.WindowState = FormWindowState.Minimized;
        }

        private void SetupHotKey()
        {
            // Alt = 1, Ctrl = 2, Shift = 4, Win = 8
            //also can sum them up, 3 = alt and ctrl
            NativeMethods.RegisterHotKey(this.Handle, this.GetType().GetHashCode(), 6, (int)Keys.F);
        }

        private void SetupTrayMenuContext()
        {
            ContextMenu menu;
            menu = new ContextMenu();
            menu.MenuItems.Add(0, new MenuItem("About", new EventHandler(About_Click)));
            menu.MenuItems.Add(1, new MenuItem("Exit", new EventHandler(Exit_Click)));
            notifyIcon.ContextMenu = menu;
            notifyIcon.BalloonTipText = String.Format("{0} is running", APP_NAME);
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.BalloonTipTitle = APP_NAME;
            notifyIcon.ShowBalloonTip(200);
        }

        private void SetStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(REGISTRY_STARTUP, true);

            if (rk.GetValue(APP_NAME) == null)
                rk.SetValue(APP_NAME, Application.ExecutablePath);
        }

        private bool IsFirstRun()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(REGISTRY_APP_FOLDER, true);

            if (rk == null)
            {
                // this is first run, create Key and return
                rk = Registry.CurrentUser.CreateSubKey(REGISTRY_APP_FOLDER);
            }

            if (rk == null)
            {
                // unhandeled error
                return false;
            }

            object regValue = rk.GetValue(REGISTRY_FIRST_RUN);
            bool? didRunOnce = false;

            if (regValue != null)
            {
                try
                {
                    didRunOnce = Convert.ToBoolean(regValue);
                }
                catch
                {
                    // someone messed up the content of the registry value, continue
                }
            }

            if (didRunOnce == false)
            {
                // first run
                rk.SetValue(REGISTRY_FIRST_RUN, true);
                return true;
            }

            return false;
        }

        private void GetActiveWindow()
        {
            // get the current active window handle
            int handle = NativeMethods.GetForegroundWindow();

            // get the window size
            NativeMethods.RECT mainRect = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(handle, ref mainRect);

            SHDocVw.InternetExplorer foregroundWindow = new SHDocVw.ShellWindows().Cast<SHDocVw.InternetExplorer>().Where(hwnd => hwnd.HWND == handle).FirstOrDefault();
            ArrayList itemList = new ArrayList();

            if (foregroundWindow != null)
            {
                // found the handle of the foreground window!

                //get the items in the window
                Shell32.Folder folder = ((Shell32.IShellFolderViewDual2)foregroundWindow.Document).Folder;
                Shell32.FolderItems items = folder.Items();
                foreach (Shell32.FolderItem item in items)
                {
                    itemList.Add(item);
                }
                
                // create the TextBox overlay Form
                TextboxForm form = new TextboxForm(foregroundWindow, itemList);
                form.Location = new Point(mainRect.Left + 10, mainRect.Bottom - 35);
                form.Show();
            }
        }

        protected void Exit_Click(Object sender, System.EventArgs e)
        {
            CloseApplication();
        }

        protected void About_Click(Object sender, System.EventArgs e)
        {
            ShowAboutForm();
        }

        private void ShowAboutForm()
        {
            AboutForm form = new AboutForm();
            form.Show();
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            About_Click(sender, e);
        }

        private void CloseApplication()
        {
            // remove notify icon (and make sure the user won't see it after the program was closed)
            notifyIcon.Visible = false;
            notifyIcon.Icon.Dispose();
            notifyIcon.Dispose();

            // close the program
            this.Close();
        }
    }
}
