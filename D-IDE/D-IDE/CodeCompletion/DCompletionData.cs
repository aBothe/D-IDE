using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor;
using System.Windows.Forms;
using D_Parser;
using ICSharpCode.NRefactory.Ast;

namespace D_IDE
{
    public class DCompletionData : ICompletionData, IComparable
    {
        public DNode data;
        public DNode parent;

        public string text;
        public string description;
        public int imageindex;
        double priority;

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }


        public int ImageIndex
        {
            get { return imageindex; }
            set { imageindex = value; }
        }

        public double Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        public virtual bool InsertAction(TextArea textArea, char ch)
        {
            textArea.InsertString(text);
            return false;
        }

        public DCompletionData(string t, string d, int i)
        {
            this.text = t;
            this.description = d;
            this.ImageIndex = i;
        }

        public DCompletionData(DNode data, DNode parent, int Icon)
        {
            this.data = data;
            this.parent = parent;
            this.ImageIndex = Icon;
            Init();
        }

        public DCompletionData(DNode data, DNode parent)
        {
            this.data = data;
            this.parent = parent;
            this.ImageIndex = GetImageIndex(D_IDEForm.icons, parent, data);
            Init();
        }

        /// <summary>
        /// Builds expression description string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string BuildDescriptionString(DNode data)
        {
            return BuildDescriptionString(data, null, true);
        }
        public static string BuildDescriptionString(DNode data_, DModule mod)
        {
            return BuildDescriptionString(data_, mod, true);
        }

        /// <summary>
        /// Builds expression description string
        /// </summary>
        /// <param name="data"></param>
        /// <param name="IncludeDesc"></param>
        /// <returns></returns>
        public static string BuildDescriptionString(DNode data, DModule mod, bool IncludeDesc)
        {
            if (data == null) return "";
            string ret = "";

            if (data.fieldtype == FieldType.Root)
            {
                if (data.name != "")
                    ret += data.name;
                else
                    ret += data.module;

                if (mod != null && IncludeDesc)
                {
                    ret += "\r\n\r\n" + mod.FileName;
                }
                return ret;
            }

            string path = (data.module != null && data.module != "" ? (data.module + ".") : "");
            DNode tdt = data;
            while (tdt != null && tdt.Parent != null && (tdt.Parent as DNode).fieldtype != FieldType.Root)
            {
                path = path.Insert(0, (tdt.Parent as DNode).name + ".");
                tdt = (tdt.Parent as DNode);
            }


            foreach (int m in data.modifiers)
            {
                ret += DTokens.GetTokenString(m) + " ";
            }

            if (data.fieldtype == FieldType.Constructor)
            {
                ret += data.name;
                goto addparams;
            }

            string dataType = data.Type != null ? data.Type.ToString() : "";
            if (data.fieldtype == FieldType.AliasDecl)
            {
                ret += dataType;
                return ret;
            }

            ret +=
                (dataType == "" ? DTokens.GetTokenString(data.TypeToken) : dataType) + " " + // Type ID
                (!String.IsNullOrEmpty(path) ? (path) : "") + // Module path
                (dataType != data.name ? data.name : ""); // int : MyType // Field Name

            addparams:
            if (data.TemplateParameters.Count > 0)
            {
                ret += "(";
                foreach (DNode p in data.TemplateParameters)
                {
                    foreach (int m in p.modifiers)
                    {
                        ret += DTokens.GetTokenString(m) + " ";
                    }
                    if (p.Type != null)
                        ret += (p.Type.ToString() != p.name ? p.Type.ToString() + " " : "") + p.name + ",";
                    else ret += p.name + ",";
                }
                ret = ret.Trim(',') + ")";
            }

            if (data is DClassLike)
            {
                if ((data as DClassLike).BaseClass != null)
                    ret += " : " + (data as DClassLike).BaseClass.ToString();
                if ((data as DClassLike).ImplementedInterface != null)
                    ret += ((data as DClassLike).BaseClass != null ? ", " : " : ") + (data as DClassLike).ImplementedInterface.ToString();
            }

            if (data is DEnum && (data as DEnum).EnumBaseType != null)
            {
                ret += " : " + (data as DEnum).EnumBaseType.ToString();
            }

            if (data is DMethod)
            {
                ret += "(";
                foreach (DNode p in (data as DMethod).Parameters)
                {
                    foreach (int m in p.modifiers)
                    {
                        ret += DTokens.GetTokenString(m) + " ";
                    }
                    if (p.Type != null)
                        ret += (p.Type.ToString() != p.name ? p.Type.ToString() + " " : "") + p.name + ",";
                    else ret += p.name + ",";
                }
                ret = ret.Trim(',') + ")";
            }

            if (data is DVariable && !String.IsNullOrEmpty((data as DVariable).Value))
            {
                if (data is DEnumValue)
                    ret = (data as DEnumValue).Value; // Show its value only
                else
                    ret += " =" + (data as DVariable).Value;
            }

            if (IncludeDesc && !String.IsNullOrEmpty(data.desc))
            {
                ret += "\n" + data.desc;
            }

            if (ret.Length > 512) { ret = ret.Remove(509); ret += "..."; }

            return ret;
        }

        private void Init()
        {
            if (data == null) return;
            DNode d = data as DNode, par = parent as DNode;
            if (d.fieldtype == FieldType.Root && par.fieldtype == FieldType.Root
                && d.name.Length > par.module.Length + 1 && d.name.StartsWith(par.module + "."))
            {
                this.text = d.name.Substring(par.module.Length + 1);
            }
            else
                this.text = d.name;
            description = BuildDescriptionString(data);
        }

        static public int GetImageIndex(ImageList icons, DNode Parent, DNode Node)
        {
            if (icons == null) return -1;

            DNode parent = (Parent as DNode);
            DNode v = (Node as DNode);
            if (v.fieldtype == FieldType.Delegate)
            {
                if (v.modifiers.Contains(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateDelegate.png");
                else if (v.modifiers.Contains(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedDelegate.png");
                else if (v.modifiers.Contains(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalDelegate.png");

                return icons.Images.IndexOfKey("Icons.16x16.Delegate.png");
            }

            if (v == null) return icons.Images.IndexOfKey("Icons.16x16.Local.png");
            if (v.fieldtype == FieldType.Root) return icons.Images.IndexOfKey("namespace");
            if (parent != null)
                if (parent.fieldtype == FieldType.Function)
                {
                    if (v.fieldtype == FieldType.Function)
                    {
                        return icons.Images.IndexOfKey("Icons.16x16.Method.png");
                    }
                    if (v.fieldtype == FieldType.Variable)
                    {
                        return icons.Images.IndexOfKey("Icons.16x16.Local.png");
                    }
                }

            if (v.fieldtype == FieldType.Class || v.fieldtype == FieldType.Template)
            {
                if (v.modifiers.Contains(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateClass.png");
                else if (v.modifiers.Contains(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedClass.png");
                else if (v.modifiers.Contains(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalClass.png");

                return icons.Images.IndexOfKey("Icons.16x16.Class.png");
            }

            if (parent != null)
                if (parent.fieldtype == FieldType.Enum)
                    return icons.Images.IndexOfKey("Icons.16x16.Enum.png");

            if (v.fieldtype == FieldType.Enum || v.fieldtype == FieldType.EnumValue)
            {
                if (v.modifiers.Contains(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateEnum.png");
                else if (v.modifiers.Contains(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedEnum.png");
                else if (v.modifiers.Contains(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalEnum.png");

                return icons.Images.IndexOfKey("Icons.16x16.Enum.png");
            }

            if (v.fieldtype == FieldType.Struct)
            {
                if (v.modifiers.Contains(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateStruct.png");
                else if (v.modifiers.Contains(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedStruct.png");
                else if (v.modifiers.Contains(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalStruct.png");

                return icons.Images.IndexOfKey("Icons.16x16.Struct.png");
            }

            if (v.fieldtype == FieldType.Interface)
            {
                if (v.modifiers.Contains(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateInterface.png");
                else if (v.modifiers.Contains(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedInterface.png");
                else if (v.modifiers.Contains(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalInterface.png");

                return icons.Images.IndexOfKey("Icons.16x16.Interface.png");
            }

            if (v.fieldtype == FieldType.Function)
            {
                if (v.modifiers.Contains(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateMethod.png");
                else if (v.modifiers.Contains(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedMethod.png");
                else if (v.modifiers.Contains(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalMethod.png");

                return icons.Images.IndexOfKey("Icons.16x16.Method.png");
            }

            if (v.fieldtype == FieldType.Variable && DTokens.BasicTypes[(int)v.TypeToken])
            {
                if (v.modifiers.Contains(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateProperty.png");
                else if (v.modifiers.Contains(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedProperty.png");
                else if (v.modifiers.Contains(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalProperty.png");

                return icons.Images.IndexOfKey("Icons.16x16.Property.png");
            }

            if (v.modifiers.Contains(DTokens.Private))
                return icons.Images.IndexOfKey("Icons.16x16.PrivateField.png");
            else if (v.modifiers.Contains(DTokens.Protected))
                return icons.Images.IndexOfKey("Icons.16x16.ProtectedField.png");
            else if (v.modifiers.Contains(DTokens.Package))
                return icons.Images.IndexOfKey("Icons.16x16.InternalField.png");

            return icons.Images.IndexOfKey("Icons.16x16.Field.png");
        }

        #region IComparable Member

        int IComparable.CompareTo(object o)
        {
            if (o == null || !(o is DefaultCompletionData))
            {
                return -1;
            }
            return text.CompareTo(((DefaultCompletionData)o).Text);
        }

        #endregion
    }

}
