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
        public string mod;
        public string mod_file;
        public string vdir;
        public List<INode> Children = new List<INode>();

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
            vdir = "";
        }

        public DModule(string file)
        {
            Init();
            mod_file = file;
            mod = Path.GetFileNameWithoutExtension(file);
            Parse(file);
        }

        public DModule(string file, string mod_name)
        {
            Init();
            mod_file = file;
            mod = mod_name;
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

            Form1.thisForm.errlog.parserErrors.Clear();
            Form1.thisForm.errlog.Update();

            mod_file = file;

            try { Form1.thisForm.ProgressStatusLabel.Text = "Parsing " + mod; }
            catch { }
            dom = DParser.ParseFile(mod, file, out import);
            try { Form1.thisForm.ProgressStatusLabel.Text = "Done parsing " + mod; }
            catch { }
        }

        public DataType dom; // Contains entire data (recursive)
        public List<string> import;
        [NonSerialized()]
        public List<FoldMarker> folds;
    }
}
