using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace D_IDE
{
    public partial class CCWindow : Form
    {
	ICompletionData[] mcd;
	public CCWindow(ImageList icons, ICompletionData[] cd)
	{
	    InitializeComponent();
	    this.TopMost = true;
	    Show();
	    this.icons = icons;
	    lv.SmallImageList = icons;
	    mcd = cd;
	    UpdateData(cd);
	}

	public void UpdateData(ICompletionData[] cd)
	{
	    lv.Items.Clear();
	    mcd = cd;
	    foreach (ICompletionData d in mcd)
	    {
		lv.Items.Add(d.Text,d.ImageIndex);
	    }
	}

	private void lv_SelectedIndexChanged(object sender, EventArgs e)
	{
	    if (lv.SelectedItems.Count <= 0) return;
	    int si = lv.SelectedIndices[0];

	    //Point tp = lv.Items[si].Position;
	    //tTip.Show(mcd[si].Description,this,tp);
	}
    }
}
