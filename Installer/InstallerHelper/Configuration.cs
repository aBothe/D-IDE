using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;

namespace DIDE.Installer
{
    public class Configuration
    {
        public static void CreateConfigurationFile(string filePath, Dictionary<string, string[]> nodeHash)
        {
            XmlDocument xmlDoc = new XmlDocument();
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

                    if (searchResult.Count > 0) childNode = searchResult[0] as XmlElement;

                    string[] pieces = nodeName.Split(new char[] { '[', ']', '=', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (childNode == null)
                    {
                        if (pieces.Length >= 3)
                        {
                            childNode = xmlDoc.CreateElement(pieces[0].Trim());

                            for (int j = 2; j < pieces.Length; j += 2)
                            {
                                XmlAttribute attr = xmlDoc.CreateAttribute(pieces[j-1].TrimStart('@').Trim());
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

            XmlElement root = xmlDoc.DocumentElement;
            XmlDeclaration xmlDecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty); //<?xml version="1.0" encoding="utf-8"?>
            xmlDoc.InsertBefore(xmlDecl, root);

            xmlDoc.Save(filePath);
        }
    }
}
