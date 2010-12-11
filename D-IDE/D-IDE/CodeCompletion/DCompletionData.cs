using System;
using System.Windows.Forms;
using D_Parser;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using Parser.Core;

namespace D_IDE
{
    public class DCompletionData : ICompletionData, IComparable
    {
        public INode data;

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
                    return GetImageIndex(D_IDEForm.icons, data);
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

        public DCompletionData(INode data, int Icon)
        {
            this.data = data;
            this.ImageIndex = Icon;
            Init();
        }

        public DCompletionData(INode data)
        {
            this.data = data;
            Init();
        }

        public static string BuildDescriptionString(INode data)
        {
            return BuildDescriptionString(data, true);
        }

        /// <summary>
        /// Builds expression description string
        /// </summary>
        public static string BuildDescriptionString(INode data, bool IncludeDesc)
        {
            var ret = data.ToString();

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
            // Show the last part of the module name if data is a module
            if (data is DModule)
            {
                var m=(data as DModule);
                var parts = m.ModuleName.Split('.');

                text=parts[parts.Length-1];
            }else
                this.text = data.Name;
            description = BuildDescriptionString(data);
        }

        static public int GetImageIndex(ImageList icons, INode _Node)
        {
            if (icons == null) return -1;

			var Node = _Node as DNode;

            if (Node == null)
                return icons.Images.IndexOfKey("Icons.16x16.Local.png");

            // Module
            if (Node is DModule)
                return icons.Images.IndexOfKey("namespace");


            // Variable
            if (Node is DVariable && (Node as DVariable).Type is DTokenDeclaration && DTokens.BasicTypes[((Node as DVariable).Type as DTokenDeclaration).Token])
            {
                if (Node.ContainsAttribute(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateProperty.png");
                else if (Node.ContainsAttribute(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedProperty.png");
                else if (Node.ContainsAttribute(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalProperty.png");

                return icons.Images.IndexOfKey("Icons.16x16.Property.png");
            }

            if (Node.Parent is DMethod)
            {
                // Local methods
                if (Node is DMethod)
                    return icons.Images.IndexOfKey("Icons.16x16.Method.png");
                // Local variables
                if (Node is DVariable)
                    return icons.Images.IndexOfKey("Icons.16x16.Local.png");
            }

            // Method
            if (Node is DMethod)
            {
                if ((Node as DMethod).SpecialType == DMethod.MethodType.Delegate)
                {
                    if (Node.ContainsAttribute(DTokens.Private))
                        return icons.Images.IndexOfKey("Icons.16x16.PrivateDelegate.png");
                    else if (Node.ContainsAttribute(DTokens.Protected))
                        return icons.Images.IndexOfKey("Icons.16x16.ProtectedDelegate.png");
                    else if (Node.ContainsAttribute(DTokens.Package))
                        return icons.Images.IndexOfKey("Icons.16x16.InternalDelegate.png");

                    return icons.Images.IndexOfKey("Icons.16x16.Delegate.png");
                }
                else
                {
                    if (Node.ContainsAttribute(DTokens.Private))
                        return icons.Images.IndexOfKey("Icons.16x16.PrivateMethod.png");
                    else if (Node.ContainsAttribute(DTokens.Protected))
                        return icons.Images.IndexOfKey("Icons.16x16.ProtectedMethod.png");
                    else if (Node.ContainsAttribute(DTokens.Package))
                        return icons.Images.IndexOfKey("Icons.16x16.InternalMethod.png");

                    return icons.Images.IndexOfKey("Icons.16x16.Method.png");
                }
            }

            // Class
            if (Node is DClassLike)
            {
                var c = Node as DClassLike;
                if (c.ClassType == DTokens.Class || c.ClassType == DTokens.Template)
                {
                    if (Node.ContainsAttribute(DTokens.Private))
                        return icons.Images.IndexOfKey("Icons.16x16.PrivateClass.png");
                    else if (Node.ContainsAttribute(DTokens.Protected))
                        return icons.Images.IndexOfKey("Icons.16x16.ProtectedClass.png");
                    else if (Node.ContainsAttribute(DTokens.Package))
                        return icons.Images.IndexOfKey("Icons.16x16.InternalClass.png");

                    return icons.Images.IndexOfKey("Icons.16x16.Class.png");
                }

                if (c.ClassType==DTokens.Struct)
                {
                    if (Node.ContainsAttribute(DTokens.Private))
                        return icons.Images.IndexOfKey("Icons.16x16.PrivateStruct.png");
                    else if (Node.ContainsAttribute(DTokens.Protected))
                        return icons.Images.IndexOfKey("Icons.16x16.ProtectedStruct.png");
                    else if (Node.ContainsAttribute(DTokens.Package))
                        return icons.Images.IndexOfKey("Icons.16x16.InternalStruct.png");

                    return icons.Images.IndexOfKey("Icons.16x16.Struct.png");
                }

                if (c.ClassType==DTokens.Interface)
                {
                    if (Node.ContainsAttribute(DTokens.Private))
                        return icons.Images.IndexOfKey("Icons.16x16.PrivateInterface.png");
                    else if (Node.ContainsAttribute(DTokens.Protected))
                        return icons.Images.IndexOfKey("Icons.16x16.ProtectedInterface.png");
                    else if (Node.ContainsAttribute(DTokens.Package))
                        return icons.Images.IndexOfKey("Icons.16x16.InternalInterface.png");

                    return icons.Images.IndexOfKey("Icons.16x16.Interface.png");
                }
            }


            // Enum
            if (Node is DEnum || Node is DEnumValue)
            {
                if (Node.ContainsAttribute(DTokens.Private))
                    return icons.Images.IndexOfKey("Icons.16x16.PrivateEnum.png");
                else if (Node.ContainsAttribute(DTokens.Protected))
                    return icons.Images.IndexOfKey("Icons.16x16.ProtectedEnum.png");
                else if (Node.ContainsAttribute(DTokens.Package))
                    return icons.Images.IndexOfKey("Icons.16x16.InternalEnum.png");

                return icons.Images.IndexOfKey("Icons.16x16.Enum.png");
            }

            if (Node.ContainsAttribute(DTokens.Private))
                return icons.Images.IndexOfKey("Icons.16x16.PrivateField.png");
            else if (Node.ContainsAttribute(DTokens.Protected))
                return icons.Images.IndexOfKey("Icons.16x16.ProtectedField.png");
            else if (Node.ContainsAttribute(DTokens.Package))
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
