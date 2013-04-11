using System.Collections.Generic;
using System.IO;
using System.Web;

namespace ResxWeb.Models
{
    public class Settings
    {
        public IEnumerable<User> Users { get; set; }
        public IEnumerable<Source> Sources { get; set; }

        public static Settings Load()
        {
            var pathToJson = HttpContext.Current.Server.MapPath("~/App_Data/settings.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(File.ReadAllText(pathToJson));
        }
    }

    public class User
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public IEnumerable<string> VisibleSources { get; set; }
        public IEnumerable<string> VisibleCultures { get; set; }
        public IEnumerable<string> EditableCultures { get; set; }
        public bool DisplayKey { get; set; }
        public bool DisplayFile { get; set; }
    }

    public class Source
    {
        public string Alias { get; set; }
        public string Path { get; set; }
    }
}