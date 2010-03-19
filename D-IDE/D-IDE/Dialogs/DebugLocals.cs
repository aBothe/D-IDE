using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;

namespace D_IDE
{
    public partial class DebugLocals : DockContent
    {
        public DebugLocals()
        {
            InitializeComponent();
        }

        public void Clear()
        {
            list.Items.Clear();
        }
    }
}
