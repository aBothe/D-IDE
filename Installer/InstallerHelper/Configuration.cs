using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;

namespace DIDE.Installer
{
    public class Configuration
    {
        public static bool IsValid(string filePath, List<string> xmlPaths)
        {
            var isValid = false;
            var file = new FileInfo(filePath);

            var xmlDoc = new XmlDocument();
            if (file.Exists)
            {
                xmlDoc.Load(file.FullName);
                Dictionary<string, string> dict = GetNodeValues(xmlPaths, xmlDoc);
                foreach (string p in dict.Values) isValid = true;
                isValid = false;
            }
            return true;
        }

        public static Dictionary<string, string> GetNodeValues(List<string> xmlPaths, XmlDocument xmlDoc)
        {
            var nodeValues = new Dictionary<string, string>();
            foreach (string nodePath in xmlPaths)
            {
                XmlNodeList nodes = xmlDoc.SelectNodes(nodePath);
                if (nodes.Count == 1)
                {
                    nodeValues[nodePath] = nodes[0].Value;
                }
            }
            return nodeValues;
        }

        public static void CreateConfigurationFile(string filePath, Dictionary<string, string[]> nodeHash)
        {
            FileInfo file = new FileInfo(filePath);

            bool isNew = true;
            XmlDocument xmlDoc = new XmlDocument();
            if (file.Exists)
            {
                isNew = false;
                xmlDoc.Load(file.FullName);
            }
            foreach (string nodePath in nodeHash.Keys)
            {
                string[] nodeNames = nodePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                XmlNode xmlParent = xmlDoc;
                XmlNodeList searchResult = null;
                string[] values = nodeHash[nodePath];
                string xpath = "//";

                for (int i = 0; i < nodeNames.Length; i++)
                {
                    XmlElement childNode = null;
                    bool lastNode = (nodeNames.Length == (i + 1));
                    string nodeName = nodeNames[i];
                    xpath += nodeName;
                    searchResult = xmlDoc.SelectNodes(xpath);

                    if (searchResult.Count == 1)
                    {
                        childNode = searchResult[0] as XmlElement;
                    }
                    else if ((searchResult.Count > 1) && (i == nodeNames.Length - 1))
                    {
                        foreach (string val in values)
                        {
                            for (int j=searchResult.Count-1; j>=0; j--)
                            {
                                if (searchResult[j].InnerText != null &&
                                    searchResult[j].InnerText.Equals(val, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    searchResult[j].ParentNode.RemoveChild(searchResult[j]);
                                }
                            }
                        }
                    }

                    string[] pieces = nodeName.Split(new char[] { '[', ']', '=', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (childNode == null)
                    {
                        if (pieces.Length >= 3)
                        {
                            childNode = xmlDoc.CreateElement(pieces[0].Trim());

                            for (int j = 2; j < pieces.Length; j += 2)
                            {
                                XmlAttribute attr = xmlDoc.CreateAttribute(pieces[j - 1].TrimStart('@').Trim());
                                attr.Value = pieces[j].Trim().Trim('\'', '"');
                                childNode.Attributes.Append(attr);
                            }
                        }
                        else
                        {
                            childNode = xmlDoc.CreateElement(nodeName);
                        }
                    }

                    if (lastNode)
                    {
                        if (values.Length > 1)
                        {
                            foreach (string value in values)
                            {
                                XmlElement clone = childNode.Clone() as XmlElement;
                                clone.InnerText = value;
                                xmlParent.AppendChild(clone);
                            }
                        }
                        else
                        {
                            if (values.Length == 1) childNode.InnerText = values[0];
                            xmlParent.AppendChild(childNode);
                        }
                    }
                    else
                    {
                        xmlParent.AppendChild(childNode);
                    }

                    xmlParent = childNode;
                    xpath += "/";
                }

            }

            if (isNew)
            {
                XmlElement root = xmlDoc.DocumentElement;
                XmlDeclaration xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                xmlDoc.InsertBefore(xmlDecl, root);
            }
            xmlDoc.Save(filePath);
        }
    }
}
