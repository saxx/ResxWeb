using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace ResxWeb
{
    public class ResxDeltaWriter
    {

        #region Delta

        public void SaveDelta(IEnumerable<ResxKey> existingResources, FormCollection form, string deltaDirectoryPath)
        {
            if (Directory.Exists(deltaDirectoryPath))
                Directory.Delete(deltaDirectoryPath, true);

            var allCultures = existingResources.SelectMany(x => x.Values.Keys).Distinct().ToList();

            foreach (var resource in existingResources)
            {
                foreach (var culture in allCultures)
                    SaveDelta(form, deltaDirectoryPath, resource, culture);
            }
        }

        private void SaveDelta(FormCollection form, string deltaDirectoryPath, ResxKey resource, string culture)
        {
            var oldValue = resource.Values.ContainsKey(culture) ? resource.Values[culture].Value ?? "" : null;
            var newValue = form[resource.RelativeFilePath + "|" + resource.Key + "|" + culture];

            //fix line breaks
            oldValue = oldValue == null ? null : oldValue.Replace("\r", "").Replace("\n", Environment.NewLine);
            newValue = newValue == null ? null : newValue.Replace("\r", "").Replace("\n", Environment.NewLine);

            if (oldValue != newValue && !string.IsNullOrEmpty(newValue))
            {
                var deltaPath = ResxDeltaReader.GetDeltaFilePath(deltaDirectoryPath, resource.RelativeFilePath, culture);

                XDocument xml;
                if (File.Exists(deltaPath))
                    xml = XDocument.Load(deltaPath);
                else
                    xml = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement("root"));

                var root = xml.Root;
                var node = new XElement("data");
                root.Add(node);

                node.Add(new XAttribute("name", resource.Key), new XAttribute(XNamespace.Xml + "space", "preserve"), new XElement("value", newValue));

                if (!Directory.Exists(Path.GetDirectoryName(deltaPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(deltaPath));
                xml.Save(deltaPath);
            }
        }

        #endregion

        public string BuildArchive(string defaultSourcePath, string username, string sourceAlias, IEnumerable<ResxKey> resources)
        {
            //delete all existing temp subfolders
            foreach (var d in Directory.GetDirectories(GetTempPath()))
                Directory.Delete(d, true);
            foreach (var f in Directory.GetFiles(GetTempPath()))
                File.Delete(f);

            CopyOriginalFilesToTempPath(defaultSourcePath, username, sourceAlias);

            ApplyDeltaValues(username, sourceAlias, resources);

            var zipFilePath = Path.Combine(GetTempPath(), "Archive.zip");
            ZipFile.CreateFromDirectory(Path.Combine(GetTempPath(), username, sourceAlias), zipFilePath, CompressionLevel.Optimal, false);
            return zipFilePath;
        }

        private void ApplyDeltaValues(string username, string sourceAlias, IEnumerable<ResxKey> resources)
        {
            var allCultures = resources.SelectMany(x => x.Values.Keys).Distinct().ToList();

            foreach (var resource in resources)
            {
                foreach (var culture in allCultures)
                {
                    if (!resource.Values.ContainsKey(culture))
                        continue;

                    var newValue = resource.Values[culture].Value;
                    if (string.IsNullOrEmpty(newValue))
                        continue;

                    var filePath = Path.Combine(GetTempPath(), username, sourceAlias, Path.GetDirectoryName(resource.RelativeFilePath),
                                                Path.GetFileNameWithoutExtension(resource.RelativeFilePath) + "." + (culture == "" ? "" : culture + ".") + "resx");
                    if (!File.Exists(filePath))
                        continue;

                    var xmlHasChanged = false;

                    var xml = XDocument.Load(filePath);
                    var root = xml.Root;
                    if (root == null)
                        throw new ApplicationException("There's no root node in '" + filePath + "'.");

                    var node = root.Elements("data").FirstOrDefault(x => x.Attribute("name").Value == resource.Key);
                    if (node == null)
                        root.Add(node = new XElement("data", new XAttribute("name", resource.Key), new XAttribute(XNamespace.Xml + "space", "preserve")));

                    var valueNode = node.Element("value");
                    if (valueNode == null)
                    {
                        node.Add(new XElement("value", newValue));
                        xmlHasChanged = true;
                    }
                    else if (valueNode.Value != newValue)
                    {
                        valueNode.Value = newValue;
                        xmlHasChanged = true;
                    }

                    if (xmlHasChanged)
                        xml.Save(filePath);
                }
            }
        }

        private void CopyOriginalFilesToTempPath(string defaultSourcePath, string username, string sourceAlias)
        {
            var targetDirectory = Path.Combine(GetTempPath(), username, sourceAlias);
            Directory.CreateDirectory(targetDirectory);

            if (File.Exists(defaultSourcePath))
            {
                File.Copy(defaultSourcePath, Path.Combine(targetDirectory, Path.GetFileName(defaultSourcePath)));
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(defaultSourcePath), Path.GetFileNameWithoutExtension(defaultSourcePath) + ".*.resx"))
                    File.Copy(file, Path.Combine(targetDirectory, Path.GetFileName(file)));
            }
            else if (Directory.Exists(defaultSourcePath))
                CopyDirectoryRecursive(defaultSourcePath, targetDirectory);
        }

        private void CopyDirectoryRecursive(string directory, string targetDirectory)
        {
            foreach (var f in Directory.GetFiles(directory, "*.resx"))
            {
                Directory.CreateDirectory(targetDirectory);
                File.Copy(f, Path.Combine(targetDirectory, Path.GetFileName(f)));
            }

            foreach (var d in Directory.GetDirectories(directory))
                CopyDirectoryRecursive(d, Path.Combine(targetDirectory, Path.GetFileName(d)));
        }


        private static string GetTempPath()
        {
            return HttpContext.Current.Server.MapPath("~/App_Data/Temp");
        }
    }
}