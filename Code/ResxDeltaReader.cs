using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace ResxWeb
{
    public class ResxDeltaReader
    {
        public IList<string> KeysToIgnore { get; set; }

        public ResxDeltaReader()
        {
            KeysToIgnore = new List<string> { "Aspose_Words_lic" };
        }

        public IEnumerable<ResxKey> ReadDefaultFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new ApplicationException("The file '" + filePath + "' does not exist.");

            var result = new List<ResxKey>();
            ReadDefaultFile(Path.GetDirectoryName(filePath), filePath, result);
            return result;
        }

        public IEnumerable<ResxKey> ReadDefaultDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new ApplicationException("The directory '" + directoryPath + "' does not exist.");

            var result = new List<ResxKey>();
            ReadDefaultDirectory(directoryPath, directoryPath, result);
            return result;
        }

        public IEnumerable<ResxKey> ReadDelta(IEnumerable<ResxKey> existingResources, string deltaDirectoryPath)
        {
            var allCultures = existingResources.SelectMany(x => x.Values.Keys).Distinct().ToList();

            foreach (var resource in existingResources)
            {
                var deltaPath = GetDeltaFilePath(deltaDirectoryPath, resource.RelativeFilePath, "");
                UpdateResourceFromDelta(resource, "", deltaPath);

                foreach (var culture in allCultures)
                {
                    deltaPath = GetDeltaFilePath(deltaDirectoryPath, resource.RelativeFilePath, culture);
                    UpdateResourceFromDelta(resource, culture, deltaPath);
                }
            }

            return existingResources;
        }

        private void UpdateResourceFromDelta(ResxKey resource, string culture, string filePath)
        {
            if (File.Exists(filePath))
            {
                var xml = XDocument.Load(filePath);
                var root = xml.Root;
                if (root == null)
                    return;

                var element = root.Elements("data").FirstOrDefault(x => x.Attribute("name").Value == resource.Key);
                if (element == null)
                    return;

                if (!resource.Values.ContainsKey(culture))
                    resource.Values[culture] = new ResxValue("");

                resource.Values[culture].HasChangedInDelta = true;

                var valueNode = element.Element("value");
                resource.Values[culture.ToLower()].Value = valueNode == null ? "" : valueNode.Value;
            }
        }

        private void ReadDefaultFile(string rootPath, string path, IList<ResxKey> result)
        {
            ReadResources(rootPath, path, path, "", result);
            ReadAdditionalCultures(rootPath, path, result);
        }

        private void ReadResources(string rootPath, string defaultFilePath, string cultureFilePath, string culture, IList<ResxKey> result)
        {
            var xml = XDocument.Load(cultureFilePath);
            var root = xml.Root;
            if (root == null)
                throw new ArgumentException("There's no root node in '" + cultureFilePath + "'.");

            foreach (var node in root.Elements("data"))
            {
                var key = node.Attribute("name").Value;
                if (KeysToIgnore.Select(x => x.ToLower()).Any(x => x == key.ToLower()))
                    continue;

                var relativePath = defaultFilePath.Substring(rootPath.Length + 1);

                var resource = result.FirstOrDefault(x => x.Key == key && x.RelativeFilePath == relativePath);
                if (resource == null)
                    result.Add(resource = new ResxKey(relativePath, key));

                var valueNode = node.Element("value");
                resource.Values[culture] = new ResxValue(valueNode == null ? "" : valueNode.Value);
            }
        }

        private void ReadAdditionalCultures(string rootPath, string defaultFilePath, IList<ResxKey> result)
        {
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(defaultFilePath), Path.GetFileNameWithoutExtension(defaultFilePath) + ".*.resx"))
            {
                var fileName = Path.GetFileNameWithoutExtension(f);
                var culture = fileName.Substring(fileName.LastIndexOf(".") + 1).ToLower();
                ReadResources(rootPath, defaultFilePath, f, culture, result);
            }
        }

        private void ReadDefaultDirectory(string rootPath, string path, IList<ResxKey> result)
        {
            foreach (var f in Directory.GetFiles(path, "*.resx"))
                if (IsDefaultResxFile(f))
                    ReadDefaultFile(rootPath, f, result);
            foreach (var d in Directory.GetDirectories(path))
                ReadDefaultDirectory(rootPath, d, result);
        }

        private bool IsDefaultResxFile(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path) ?? "";
            var reverseIndex = fileName.Length - fileName.LastIndexOf(".", StringComparison.InvariantCulture);
            return path.Length < 9 || (reverseIndex != 3 && reverseIndex != 6);
        }

        public static string GetDeltaFilePath(string deltaDirectoryPath, string relativeFilePath, string culture)
        {
            return Path.Combine(deltaDirectoryPath, Path.GetDirectoryName(relativeFilePath), Path.GetFileNameWithoutExtension(relativeFilePath) + (culture == "" ? "" : "." + culture) + ".resx");
        }

        public static string GetDeltaDirectoryPath(string username, string sourceAlias)
        {
            return HttpContext.Current.Server.MapPath("~/App_Data/" + username + "/" + sourceAlias);
        }
    }
}