using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace D_IDE
{
    /// <summary>
    /// Class managing code templates
    /// </summary>
    public static class CodeTemplate
    {
        /// <summary>
        /// Dictionary of template name -> template
        /// </summary>
        public static Dictionary<string, string> Templates = new Dictionary<string, string>();
        /// <summary>
        /// Dictionary of variable name -> variable (variable names with an '_'-Prefix are system defined)
        /// </summary>
        public static Dictionary<string, string> Variables = new Dictionary<string, string>();

        /// <summary>
        /// Indicates if the system makes the entry
        /// </summary>
        private static bool systementries = false;

        /// <summary>
        /// Static constructor
        /// </summary>
        static CodeTemplate()
        {
            systementries = true;

            //predefined system vars
            SetVar("_module", null);
            SetVar("_package", null);
            SetVar("_filename", null);
            SetVar("_date", null);

            //predefined user vars
            SetVar("user", Environment.UserName);
            SetVar("authors", Environment.UserName);
            SetVar("copyright", "Copyright ©" + DateTime.Now.Year + " by $user$");

            //predefined system templates
            SetTemplate("_module",
@"/**
 * 
 *
 * Copyright: $copyright$
 * Authors: $user$
 * Version: 1.0
 * Date: $_date$
 */
module $_package$$_module$;

/**
 *
 */
class $_module$ {

    this() {
        
    }

}
"
                );

            systementries = false;
        }

        /// <summary>
        /// Sets a variable
        /// </summary>
        public static void SetVar(string name, string content)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if (content == null) content = "";
            if (!systementries && name.StartsWith("_")) throw new InvalidOperationException("You cannot set a system var!");

            Variables[name] = content;
        }

        /// <summary>
        /// Sets a Template
        /// </summary>
        public static void SetTemplate(string name, string content)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if (content == null) content = "";

            Templates[name] = content;
        }

        /// <summary>
        /// Deletes a var
        /// </summary>
        public static void DeleteVar(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name.StartsWith("_")) throw new InvalidOperationException("Thou may not delete a system var");
            if (!Variables.ContainsKey(name)) return;

            Variables.Remove(name);
        }
        /// <summary>
        /// Deletes a Template
        /// </summary>
        public static void DeleteTemplate(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if (name.StartsWith("_")) throw new InvalidOperationException("Thou may not delete a system template");

            Templates.Remove(name);
        }

        /// <summary>
        /// Instantiates a given template
        /// </summary>
        public static string InstantiateTemplate(string name, DProject project = null, string filename = null)
        {
            if (!Templates.ContainsKey(name)) throw new KeyNotFoundException("Template not found!");

            //define vars
            systementries = true;
            if (project == null || filename == null)
            {
                SetVar("_module", null);
                SetVar("_package", null);
            }
            else
            {
                string module, package;
                project.GetModuleAndPackage(filename, out module, out package);
                SetVar("_module", module);
                SetVar("_package", package);
            }
            SetVar("_filename", filename);
            SetVar("_date", DateTime.Now.ToLongDateString());
            systementries = false;

            string template = Templates[name];
            bool changed = true;
            int loop = 0;
            while (changed && loop < 1000)
            {
                ++loop;
                changed = false;
                foreach (var variable in Variables)
                {
                    string vartag = "$" + variable.Key + "$";
                    if (template.Contains(vartag))
                    {
                        template = template.Replace(vartag, variable.Value);
                        changed = true;
                    }
                }
            }
            return template;
        }

        /// <summary>
        /// Saves template config
        /// </summary>
        public static void Save(XmlWriter xml)
        {
            xml.WriteStartElement("codetemplates");

            xml.WriteStartElement("templates");
            foreach (var template in Templates)
            {
                xml.WriteStartElement("template");
                xml.WriteStartElement("name");
                xml.WriteCData(template.Key);
                xml.WriteEndElement();
                xml.WriteStartElement("content");
                xml.WriteCData(template.Value);
                xml.WriteEndElement();
                xml.WriteEndElement();
            }
            xml.WriteEndElement();

            xml.WriteStartElement("variables");
            foreach (var variable in Variables)
            {
                if (variable.Key.StartsWith("_")) continue;
                xml.WriteStartElement("variable");
                xml.WriteStartElement("name");
                xml.WriteCData(variable.Key);
                xml.WriteEndElement();
                xml.WriteStartElement("content");
                xml.WriteCData(variable.Value);
                xml.WriteEndElement();
                xml.WriteEndElement();
            }
            xml.WriteEndElement();

            xml.WriteEndElement();
        }
        /// <summary>
        /// Loads template config
        /// </summary>
        public static void Load(XmlTextReader xml)
        {
            //is now codetemplates
            while (xml.Read())
            {
                if (xml.NodeType == XmlNodeType.Whitespace) continue;
                if (xml.LocalName == "templates")
                {
                    while (xml.Read())
                    {
                        if (xml.NodeType == XmlNodeType.Whitespace) continue;
                        if (xml.LocalName == "template")
                        {
                            string name = null;
                            string content = null;

                            while (xml.Read())
                            {
                                if (xml.NodeType == XmlNodeType.Whitespace) continue;
                                if (xml.LocalName == "name")
                                {
                                    name = xml.ReadString();
                                }
                                else if (xml.LocalName == "content")
                                {
                                    content = xml.ReadString();
                                }
                                else break;
                            }

                            if (name != null && content != null)
                                SetTemplate(name, content);
                        }
                        else break;
                    }
                }
                else if (xml.LocalName == "variables")
                {
                    while (xml.Read())
                    {
                        if (xml.NodeType == XmlNodeType.Whitespace) continue;
                        if (xml.LocalName == "variable")
                        {
                            string name = null;
                            string content = null;

                            while (xml.Read())
                            {
                                if (xml.NodeType == XmlNodeType.Whitespace) continue;
                                if (xml.LocalName == "name")
                                {
                                    name = xml.ReadString();
                                }
                                else if (xml.LocalName == "content")
                                {
                                    content = xml.ReadString();
                                }
                                else break;
                            }

                            if (name != null && content != null)
                                SetVar(name, content);
                        }
                        else break;
                    }
                }
                else break;
            }
        }
    }
}
