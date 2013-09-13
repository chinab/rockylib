using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Runtime.Caching;

namespace System
{
    public sealed class XmlHelper
    {
        #region Static
        private static Hashtable watcherMap;
        private static FileSystemWatcher fileWatcher;

        public static string XmlConfigDirectory
        {
            get
            {
                string configDirectory = System.Configuration.ConfigurationManager.AppSettings["XmlConfigDirectory"];
                if (string.IsNullOrEmpty(configDirectory))
                {
                    configDirectory = "/Config/";
                }
                configDirectory = Hub.CombinePath(configDirectory);
                Hub.CreateDirectory(configDirectory);
                return configDirectory;
            }
        }

        public static void WatcherConfig(string fileName, Action<FileSystemEventArgs> changeCallback)
        {
            if (fileWatcher == null)
            {
                fileWatcher = new FileSystemWatcher(XmlConfigDirectory, "*.xml");
                fileWatcher.EnableRaisingEvents = true;
                fileWatcher.NotifyFilter = NotifyFilters.LastWrite;

                watcherMap = Hashtable.Synchronized(new Hashtable());
                fileWatcher.Changed += (sender, e) =>
                {
                    Action<FileSystemEventArgs> callback = (Action<FileSystemEventArgs>)watcherMap[e.Name];
                    if (callback != null)
                    {
                        string key = "XmlConfig" + e.Name;
                        MemoryCache.Default.Remove(key);

                        callback(e);
                    }
                };
            }
            watcherMap[fileName] = changeCallback;
        }
        public static T GetConfig<T>(string fileName) where T : class
        {
            string key = "XmlConfig" + fileName;
            T item = MemoryCache.Default[key] as T;
            if (item == null)
            {
                MemoryCache.Default[key] = item = Deserialize<T>(File.ReadAllText(XmlConfigDirectory + fileName));
            }
            return item;
        }
        public static void SetConfig<T>(string fileName, T item) where T : class
        {
            File.WriteAllText(XmlConfigDirectory + fileName, Serialize<T>(item));
        }

        public static string Serialize<T>(T item)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(item.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                xmlSerializer.Serialize(stream, item);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public static T Deserialize<T>(string xml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                return (T)xmlSerializer.Deserialize(stream);
            }
        }
        #endregion

        #region Fields
        private string filePath;
        private XmlDocument xDoc;
        #endregion

        #region Properties
        public string FilePath
        {
            get { return filePath; }
        }
        public XmlDocument Document
        {
            get { return xDoc; }
        }
        #endregion

        #region Constructor
        public XmlHelper(string xmlFile, string root = null)
        {
            xDoc = new XmlDocument();
            if (!File.Exists(xmlFile))
            {
                xDoc.AppendChild(xDoc.CreateXmlDeclaration("1.0", "utf-8", null));
                xDoc.AppendChild(xDoc.CreateElement(String.Empty, root ?? "body", String.Empty));
                xDoc.Save(xmlFile);
            }
            xDoc.Load(this.filePath = xmlFile);
            //XmlDeclaration xDeclaration = (XmlDeclaration)xDoc.FirstChild;
            //XmlTextWriter xWriter = new XmlTextWriter(xmlFile, System.Text.Encoding.GetEncoding(encoding));
            //xWriter.Formatting = Formatting.Indented;
            //xWriter.IndentChar = '\t';
            //xWriter.Indentation = 1;
            //xDoc.Save(xWriter);
            //xWriter.Close();
        }
        #endregion

        #region Methods
        ///// <summary>
        ///// ht[body],ht[Element]array(,),ht[ElementString]array(,)
        ///// </summary>
        ///// <param name="filePath"></param>
        ///// <param name="ht"></param>
        ///// <returns></returns>
        //public void Write(Hashtable ht)
        //{
        //    if (ht.ContainsKey("body") && ht.ContainsKey("Count") && ht.ContainsKey("StartElement") && ht.ContainsKey("Element") && ht.ContainsKey("ElementString"))
        //    {
        //        XmlWriter xw = XmlWriter.Create(filePath);
        //        xw.WriteStartDocument();
        //        xw.WriteStartElement(ht["body"].ToString());
        //        string[] element = ht["Element"].ToString().Split(',');
        //        string[] elementString = ht["ElementString"].ToString().Split(',');
        //        int count = int.Parse(ht["Count"].ToString());
        //        int j = 0;
        //        for (int i = 0; i < elementString.Length; i++)
        //        {
        //            if (i % count == 0)
        //            {
        //                xw.WriteStartElement(ht["StartElement"].ToString());
        //            }
        //            xw.WriteElementString(element[j], elementString[i]);
        //            if (j == element.Length - 1)
        //            {
        //                j = 0;
        //            }
        //            else
        //            {
        //                j++;
        //            }
        //            if (i % count == count - 1)
        //            {
        //                xw.WriteEndElement();
        //            }
        //        }
        //        xw.WriteEndElement();
        //        xw.WriteEndDocument();
        //        xw.Close();
        //    }
        //}

        public float Execute(string xpath)
        {
            float rtnValue = 0;
            rtnValue = Convert.ToSingle(((new XmlDocument()).CreateNavigator()).Evaluate(xpath.Replace("/", "div")));
            return rtnValue;
        }

        public string GetParentNodePath(string nodePath)
        {
            string s = nodePath.Substring(0, nodePath.LastIndexOf("/"));
            return s.EndsWith("/") ? s.Remove(s.Length - 1, 1) : s;
        }
        public string GetParentNodePath(string nodePath, out string elementName)
        {
            string s = nodePath.Substring(0, nodePath.LastIndexOf("/"));
            elementName = nodePath.Replace(s, String.Empty).Trim('/');
            int i = elementName.IndexOf("[");
            if (i != -1)
            {
                elementName = elementName.Substring(0, i);
            }
            return s.EndsWith("/") ? s.Remove(s.Length - 1, 1) : s;
        }

        public XmlNode FindNode(string nodePath)
        {
            return xDoc.SelectSingleNode(nodePath);
        }
        public XmlNodeList FindNodes(string nodePath)
        {
            return xDoc.SelectNodes(nodePath);
        }

        public XmlElement CreateElement(string elementName)
        {
            return xDoc.CreateElement(elementName);
        }
        public XmlElement CreateElement(string elementName, string elementContent)
        {
            XmlElement Element = CreateElement(elementName);
            Element.InnerText = elementContent;
            return Element;
        }
        public XmlElement CreateElement(string elementName, string[] attributeName, string[] attributeContent)
        {
            XmlElement Element = CreateElement(elementName);
            if (attributeName.Length == attributeContent.Length)
            {
                for (int i = 0; i <= attributeName.GetUpperBound(0); i++)
                {
                    Element.SetAttribute(attributeName[i], attributeContent[i]);
                }
                return Element;
            }
            return null;
        }
        public XmlElement CreateElement(string elementName, string[] attributeName, string[] attributeContent, string elementContent)
        {
            XmlElement Element = CreateElement(elementName, attributeName, attributeContent);
            Element.InnerText = elementContent;
            return Element;
        }

        public void AddNode(string parentNodePath, XmlElement eObj)
        {
            XmlNode parentNode = FindNode(parentNodePath);
            if (parentNode != null)
            {
                parentNode.AppendChild(eObj);
            }
        }

        public void AddCDataNode(string parentNodePath, string elementName, string elementContent)
        {
            XmlNode node = FindNode(parentNodePath);
            if (node != null)
            {
                XmlElement objChildElement = xDoc.CreateElement(elementName);
                XmlCDataSection xcds = xDoc.CreateCDataSection(elementContent);
                objChildElement.AppendChild(xcds);
                node.AppendChild(objChildElement);
            }
        }

        public void InsertNode(string nodePath, string elementContent)
        {
            XmlNode node = FindNode(nodePath);
            if (node == null)
            {
                string elementName;
                XmlNode n = FindNode(GetParentNodePath(nodePath, out elementName));
                if (n != null)
                {
                    n.AppendChild(CreateElement(elementName, elementContent));
                }
            }
            else
            {
                node.InnerText = elementContent;
            }
        }

        public void InsertNode(string nodePath, string[] attributeName, string[] attributeContent)
        {
            XmlNode node = FindNode(nodePath);
            if (node == null)
            {
                string elementName;
                XmlNode n = FindNode(GetParentNodePath(nodePath, out elementName));
                if (n != null && attributeName.Length == attributeContent.Length)
                {
                    n.AppendChild(CreateElement(elementName, attributeName, attributeContent));
                }
            }
            else
            {
                node.Attributes.RemoveAll();
                XmlElement e = (XmlElement)node;
                for (int i = 0; i <= attributeName.GetUpperBound(0); i++)
                {
                    e.SetAttribute(attributeName[i], attributeContent[i]);
                }
            }
        }

        public void InsertNode(string nodePath, string[] attributeName, string[] attributeContent, string elementContent)
        {
            XmlNode node = FindNode(nodePath);
            if (node == null)
            {
                string elementName;
                XmlNode n = FindNode(GetParentNodePath(nodePath, out elementName));
                if (n != null && attributeName.Length == attributeContent.Length)
                {
                    n.AppendChild(CreateElement(elementName, attributeName, attributeContent, elementContent));
                }
            }
            else
            {
                node.Attributes.RemoveAll();
                XmlElement e = (XmlElement)node;
                for (int i = 0; i <= attributeName.GetUpperBound(0); i++)
                {
                    e.SetAttribute(attributeName[i], attributeContent[i]);
                }
                node.InnerText = elementContent;
            }
        }

        public XmlElement InsertNodeWithChild(string nodePath, string childNodeName, string element, string Content)
        {
            XmlNode node = FindNode(nodePath);
            if (node != null)
            {
                XmlElement child = CreateElement(childNodeName);
                node.AppendChild(child);
                XmlElement e = CreateElement(element, Content);
                child.AppendChild(e);
                return e;
            }
            return null;
        }

        public void RemoveNode(string nodePath)
        {
            XmlNode node = FindNode(nodePath);
            if (node != null)
            {
                node.ParentNode.RemoveChild(node);
            }
        }

        public void Save()
        {
            xDoc.Save(filePath);
        }
        #endregion
    }
}