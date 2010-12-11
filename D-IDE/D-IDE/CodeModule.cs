using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Document;
using D_Parser;

namespace D_IDE
{
    public class CodeModule:DModule
    {
        public CodeModule(){}

        public CodeModule(DProject Project, string file)
        {
            this.Project = Project;
            Parse(file);
        }

#region Properties
        public static bool ClearErrorLogBeforeParsing = true;
		public DProject Project;
        public List<FoldMarker> FoldMarkers=new List<FoldMarker>();

		public new string FileName
		{
			get { return base.FileName; }
			set {
				base.FileName = value;
				if (Project != null)
					ModuleName = Path.ChangeExtension(Project.GetRelFilePath(value), null).Replace('\\', '.');
				else
					ModuleName = Path.GetFileNameWithoutExtension(value);
			}
		}

        public bool IsParsable
        {
            get { return Parsable(FileName); }
        }

        public static bool Parsable(string file)
        {
            return file.EndsWith(".d", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".di", StringComparison.OrdinalIgnoreCase);
        }
#endregion

        public void Parse()
        {
            if (!File.Exists(FileName) || !IsParsable) return;

			string lastModuleName = ModuleName;
            var m = DParser.ParseFile(FileName);
            Assign(m);
			if (m != null && String.IsNullOrEmpty(m.ModuleName))
				ModuleName = lastModuleName;
        }

        public void Parse(string file)
        {
            if (!File.Exists(file) || !Parsable(file)) return;

			if (ClearErrorLogBeforeParsing)
			{
				D_IDEForm.thisForm.errlog.parserErrors.Clear();
				D_IDEForm.thisForm.errlog.Update();
			}
            FileName = file;
			Parse();
        }
    }
}
