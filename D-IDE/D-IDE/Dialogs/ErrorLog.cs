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
    public partial class ErrorLog : DockContent
    {
        public ErrorLog()
        {
            InitializeComponent();
        }

        public List<ErrorMessage> buildErrors=new List<ErrorMessage>();
        public List<ErrorMessage> parserErrors = new List<ErrorMessage>();

        public void Clear()
        {
            list.Items.Clear();
        }

        public new void Update()
        {
            Clear();

            foreach (ErrorMessage em in buildErrors)
            {
                ListViewItem lvi = new ListViewItem(Path.GetFileName(em.file));
                lvi.Tag = em;
                lvi.SubItems.Add(em.line.ToString());
                lvi.SubItems.Add(em.col.ToString());
                lvi.SubItems.Add(em.description);
                list.Items.Add(lvi);
            }

            foreach (ErrorMessage em in parserErrors)
            {
                ListViewItem lvi = new ListViewItem(Path.GetFileName(em.file));
                lvi.Tag = em;
                lvi.SubItems.Add(em.line.ToString());
                lvi.SubItems.Add(em.col.ToString());
                lvi.SubItems.Add(em.description);
                list.Items.Add(lvi);
            }
        }

        public void AddBuildError(string file, int line, string desc)
        {
            buildErrors.Add(new ErrorMessage(file,line,desc));
            Update();
			OpenError(file,line,0);
        }
        public void AddParserError(string file, int line, int col, string desc)
        {
            parserErrors.Add(new ErrorMessage(file, line,col, desc));
            Update();
			OpenError(file, line, col);
        }

        public ErrorMessage Selected
        {
            get
            {
                if (list.SelectedItems.Count < 1) { return new ErrorMessage(); }
                return (ErrorMessage)list.SelectedItems[0].Tag;
            }
        }

		public static void OpenError(string file,int line,int col)
		{
			DocumentInstanceWindow diw = Form1.thisForm.Open(file);
			if (diw != null)
			{
				diw.txt.ActiveTextAreaControl.Caret.Position = new ICSharpCode.TextEditor.TextLocation(col - 1, line - 1);
				diw.txt.ActiveTextAreaControl.Caret.UpdateCaretPosition();
			}
		}

        private void list_DoubleClick(object sender, EventArgs e)
        {
            ErrorMessage em = Selected;
            DocumentInstanceWindow diw = Form1.thisForm.Open(em.file);
            if (diw != null)
            {
                diw.txt.ActiveTextAreaControl.Caret.Position = new ICSharpCode.TextEditor.TextLocation(em.col-1, em.line-1);
                diw.txt.ActiveTextAreaControl.Caret.UpdateCaretPosition();
            }
        }
    }

    public struct ErrorMessage
    {
        public string file;
        public int line;
        public int col;
        public string description;

        public ErrorMessage(string File, int Line,int Column,string Description)
        {
            file = File;
            line = Line;
            col = Column;
            description = Description;
        }
        public ErrorMessage(string File, int Line, string Description)
        {
            file = File;
            line = Line;
            col = 0;
            description = Description;
        }
    }
}
