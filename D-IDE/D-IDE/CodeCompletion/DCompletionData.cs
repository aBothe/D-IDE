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
        private int ii = -2;
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
            set { ii = value; }
            get
            {
                if (ii >= -1)
                    return ii;
                else
                    return GetImageIndex(D_IDEForm.icons, data.Parent, data);
            }
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
        public static string BuildDescriptionString(DNode data_, CodeModule mod)
        {
            return BuildDescriptionString(data_, mod, true);
        }

        /// <summary>
        /// Builds expression description string
        /// </summary>
        /// <param name="data"></param>
        /// <param name="IncludeDesc"></param>
        /// <returns></returns>
        public static string BuildDescriptionString(DNode data, CodeModule mod, bool IncludeDesc)
        {
            if (data == null) return "";
            string ret = "";
            var dataRoot = data.NodeRoot as DModule;

            if (data.fieldtype == FieldType.Root)
            {
                ret += mod.ModuleName;                

                if (mod != null && IncludeDesc)
                {
                    ret += "\r\n\r\n" + mod.ModuleFileName;
                }
                return ret;
            }

            string path = (dataRoot != null && dataRoot.ModuleName != "" ? (dataRoot.ModuleName + ".") : "");
            DNode tdt = data.Parent;
            while (tdt != null && !(tdt is DModule))
            {
                path = path.Insert(0, tdt.Name + ".");
                tdt = tdt.Parent;
            }

            ret += data.AttributeString;

            if (data.fieldtype == FieldType.Constructor)
            {
                ret += data.Name;
                goto addparams;
            }

            string dataType = data.Type != null ? data.Type.ToString() : "";
            if (data.fieldtype == FieldType.AliasDecl)
            {
                ret = "alias " + dataType + " " + data.Name;
                return ret;
            }

            ret +=
                (dataType == "" && data.TypeToken > 2 ? DTokens.GetTokenString(data.TypeToken) : dataType) + " " + // Type ID
                (!String.IsNullOrEmpty(path) ? (path) : "") + // Module path
                (dataType != data.Name ? data.Name : ""); // int : MyType // Field Name

            addparams:
            if (data.TemplateParameters.Count > 0)
            {
                ret += "(";
                foreach (var p in data.TemplateParameters)
                {
                    ret += p.AttributeString;
                    if (p.Type != null && p.Type.ToString() != p.Name)
                        ret += p.Type.ToString() + " " + p.Name + ",";
                    else ret += p.Name + ",";
                }
                ret = ret.Trim(',') + ")";
            }

            if (data is DClassLike)
            {
                DClassLike dc = data as DClassLike;
                if (dc.BaseClasses.Count > 0)
                {
                    ret += ": ";
                    foreach (D_Parser.TypeDeclaration td in dc.BaseClasses)
                        ret += td.ToString()+", ";
                    ret=ret.Trim(' ',',');
                }
            }

            if (data is DEnum && (data as DEnum).EnumBaseType != null)
            {
                ret += " : " + (data as DEnum).EnumBaseType.ToString();
            }

            if (data is DMethod)
            {
                ret += "(";
                foreach (var p in (data as DMethod).Parameters)
                {
                    ret += p.AttributeString;
                    if (p.Type != null)
                        ret += (p.Type.ToString() != p.Name ? p.Type.ToString() + " " : "") + p.Name + ",";
                    else ret += p.Name + ",";
                }
                ret = ret.Trim(',') + ")";
            }

            if (data is DVariable && (data as DVariable).Initializer!=null)
            {
                if (data is DEnumValue)
                    ret = (data as DEnumValue).Initializer.ToString(); // Show its value only
                else
                    ret += " =" + (data as DVariable).Initializer.ToString();
            }

            if (IncludeDesc && !String.IsNullOrEmpty(data.Description))
            {
                ret += "\n" + data.Description;
            }

            if (ret.Length > 512) { ret = ret.Remove(509); ret += "..."; }

            return ret.Trim();
        }

        private void Init()
        {
            if (data == null) return;
            DNode d = data as DNode, par = parent as DNode;
            var module = d.NodeRoot as DModule;
            if (d.fieldtype == FieldType.Root && par.fieldtype == FieldType.Root && module!=null
                && d.Name.Length > module.ModuleName.Length + 1 && d.Name.StartsWith(module.ModuleName + "."))
            {
                this.text = d.Name.Substring(module.ModuleName.Length + 1);
            }
            else
                this.text = d.Name;
            description = BuildDescriptionString(data);
        }

        static public int GetImageIndex(ImageList icons, DNode Parent, DNode Node)
        {
            if (icons == null) return -1;

            DNode parent = (Parent as DNode);
            DNode v = (Node as DNode);
            if (v.fieldtype == FieldType.Delegate)
            {
                if (v.ContainsAttribute(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateDelegate.png");
                else if (v.ContainsAttribute(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedDelegate.png");
                else if (v.ContainsAttribute(DTokens.Package))
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
                if (v.ContainsAttribute(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateClass.png");
                else if (v.ContainsAttribute(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedClass.png");
                else if (v.ContainsAttribute(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalClass.png");

                return icons.Images.IndexOfKey("Icons.16x16.Class.png");
            }

            if (parent != null)
                if (parent.fieldtype == FieldType.Enum)
                    return icons.Images.IndexOfKey("Icons.16x16.Enum.png");

            if (v.fieldtype == FieldType.Enum || v.fieldtype == FieldType.EnumValue)
            {
                if (v.ContainsAttribute(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateEnum.png");
                else if (v.ContainsAttribute(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedEnum.png");
                else if (v.ContainsAttribute(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalEnum.png");

                return icons.Images.IndexOfKey("Icons.16x16.Enum.png");
            }

            if (v.fieldtype == FieldType.Struct)
            {
                if (v.ContainsAttribute(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateStruct.png");
                else if (v.ContainsAttribute(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedStruct.png");
                else if (v.ContainsAttribute(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalStruct.png");

                return icons.Images.IndexOfKey("Icons.16x16.Struct.png");
            }

            if (v.fieldtype == FieldType.Interface)
            {
                if (v.ContainsAttribute(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateInterface.png");
                else if (v.ContainsAttribute(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedInterface.png");
                else if (v.ContainsAttribute(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalInterface.png");

                return icons.Images.IndexOfKey("Icons.16x16.Interface.png");
            }

            if (v.fieldtype == FieldType.Function || v.fieldtype == FieldType.Constructor)
            {
                if (v.ContainsAttribute(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateMethod.png");
                else if (v.ContainsAttribute(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedMethod.png");
                else if (v.ContainsAttribute(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalMethod.png");

                return icons.Images.IndexOfKey("Icons.16x16.Method.png");
            }

            if (v.fieldtype == FieldType.Variable && DTokens.BasicTypes[(int)v.TypeToken])
            {
                if (v.ContainsAttribute(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateProperty.png");
                else if (v.ContainsAttribute(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedProperty.png");
                else if (v.ContainsAttribute(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalProperty.png");

                return icons.Images.IndexOfKey("Icons.16x16.Property.png");
            }

            if (v.ContainsAttribute(DTokens.Private))
                return icons.Images.IndexOfKey("Icons.16x16.PrivateField.png");
            else if (v.ContainsAttribute(DTokens.Protected))
                return icons.Images.IndexOfKey("Icons.16x16.ProtectedField.png");
            else if (v.ContainsAttribute(DTokens.Package))
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
