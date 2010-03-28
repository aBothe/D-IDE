using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.TextEditor.Document;
using D_Parser;

namespace D_IDE
{
    [Serializable()]
    public class DModule
    {
		public static bool ClearErrorLogBeforeParsing = true;
		[NonSerialized()]
		public DProject Project;
        public string ModuleName;
        public string mod_file;
		public string FileName
		{
			get { return mod_file; }
			set {
				mod_file = value;
				if (Project != null)
				{
					ModuleName = Path.ChangeExtension(Project.GetRelFilePath(value), null).Replace('\\', '.');
				}else
					ModuleName = Path.GetFileNameWithoutExtension(value);
			}
		}
		public List<INode> Children
		{
			get {
				if(dom!=null)
					return dom.Children;
				return new List<INode>();
			}
		}

        public bool IsParsable
        {
            get { return Parsable(mod_file); }
        }

        public static bool Parsable(string file)
        {
            return file.EndsWith(".d", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".di", StringComparison.OrdinalIgnoreCase);
        }

        private void Init()
        {
            dom = new DataType(FieldType.Root);
            import = new List<string>();
            folds = new List<FoldMarker>();
        }

        public DModule(DProject Project,string file)
        {
			this.Project = Project;
            Init();
            FileName = file;
            Parse(file);
        }

        public void Parse()
        {
            if (!File.Exists(mod_file) || !IsParsable) return;
            Parse(mod_file);
        }

        public void Parse(string file)
        {
            if (!File.Exists(file) || !Parsable(file)) return;

			if (ClearErrorLogBeforeParsing)
			{
				Form1.thisForm.errlog.parserErrors.Clear();
				Form1.thisForm.errlog.Update();
			}
            mod_file = file;

            // try { Form1.thisForm.ProgressStatusLabel.Text = "Parsing " + ModuleName; } catch { } // Perhaps these things here take too much time - so comment it out
            dom = DParser.ParseFile(ModuleName, file, out import);
            //try { Form1.thisForm.ProgressStatusLabel.Text = "Done parsing " + ModuleName; } catch { }
        }

        public DataType dom; // Contains entire data (recursive)
        public List<string> import;
        [NonSerialized()]
        public List<FoldMarker> folds;
    }
}
