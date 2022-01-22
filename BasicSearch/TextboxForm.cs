using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Drawing.Text;

namespace BasicSearch
{
    public partial class TextboxForm : Form
    {
        SHDocVw.InternetExplorer window;
        ArrayList items;
        Shell32.FolderItem selectedItem;
        int selectedIndex;

        public TextboxForm(SHDocVw.InternetExplorer window, ArrayList list)
        {
            InitializeComponent();

            this.window = window;
            this.items = list;

            ShowInTaskbar = false;
            // make the form transparent
            this.BackColor = Color.LimeGreen;
            this.TransparencyKey = Color.LimeGreen;

            // remove the title bar
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;

            NativeMethods.SetForegroundWindow(this.Handle);
        }

        private void TextboxForm_Load(object sender, EventArgs e)
        {
            searchBox.Select();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            if (window != null && searchBox.Text.Length != 0)
            {
                string text = searchBox.Text.ToLower();
                for(int i = 0; i < items.Count; i++)
                {
                    Shell32.FolderItem item = (Shell32.FolderItem)items[i];
                    string str = item.Name.ToString().ToLower();
                    if (str.IndexOf(text) == 0)
                    {
                        SelectItem(i);
                        break;
                    }
                }
            }
        }

        private void UnSelectAllItems(SHDocVw.InternetExplorer window)
        {
            // get window item and deselect all the selected items in it
            foreach (Shell32.FolderItem item in ((Shell32.IShellFolderViewDual2)window.Document).SelectedItems())
            {
                ((Shell32.IShellFolderViewDual2)window.Document).SelectItem(item, 0);
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (selectedItem != null)
                {
                    // open the selected folder (navigate to it)
                    try
                    {
                        window.Navigate(selectedItem.Path);
                    }
                    catch (Exception ex)
                    {
                        // check if library file
                        if (selectedItem.Path.Contains(@".library-ms"))
                        {
                            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            string folder = selectedItem.Path.Substring(selectedItem.Path.LastIndexOf("\\"));
                            string uri = appdata + "\\Microsoft\\Windows\\Libraries" + folder;
                            window.Navigate(uri);
                        }
                    }

                    //NativeMethods.SetForegroundWindow((IntPtr)handle);
                    this.Close();
                }
            }
            else if (e.KeyCode == Keys.Up)
            {
                int nextItem = selectedIndex - 1;
                if (nextItem >= 0)
                {
                    SelectItem(nextItem);
                }
            }
            else if (e.KeyCode == Keys.Down)
            {
                int nextItem = selectedIndex + 1;
                if (nextItem < items.Count)
                {
                    SelectItem(nextItem);
                }
            }
        }

        private void SelectItem(int index)
        {
            Shell32.FolderItem item = (Shell32.FolderItem)items[index];
            UnSelectAllItems(window);
            selectedItem = item;
            selectedIndex = index;
            // make sure the user can see the selected item
            ((Shell32.IShellFolderViewDual2)window.Document).SelectItem(item, 8);
            // actually select the item
            ((Shell32.IShellFolderViewDual2)window.Document).SelectItem(item, 1);
            // https://msdn.microsoft.com/en-us/library/windows/desktop/bb774047(v=vs.85).aspx - what every number above means
        }

        private void TextboxForm_Deactivate(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
