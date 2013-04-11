using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ResxWeb.Models.ViewModels.Home
{
    public class EditViewModel
    {
        private readonly ResxDeltaReader _resxDeltaReader;

        public EditViewModel(ResxDeltaReader resxDeltaReader, User user, string source)
        {
            _resxDeltaReader = resxDeltaReader;
            AvailableCultures = new List<string>();
            Resources = new List<ResxKey>();

            CurrentUser = user;
            CurrentSection = source;
        }

        public void Fill(string path)
        {
            if (path.EndsWith(".resx", StringComparison.InvariantCultureIgnoreCase))
                Resources = _resxDeltaReader.ReadDefaultFile(path);
            else
                Resources = _resxDeltaReader.ReadDefaultDirectory(path);

            //remove cultures that are not visible to the current user
            if (CurrentUser.VisibleCultures != null && CurrentUser.VisibleCultures.Any())
                foreach (var resource in Resources)
                    foreach (var pair in resource.Values.ToList())
                        if (!CurrentUser.VisibleCultures.Contains(pair.Key))
                            resource.Values.Remove(pair.Key);

            AvailableCultures = Resources.SelectMany(x => x.Values.Keys).Distinct().ToList();
            AvailableCultures =
                AvailableCultures.Where(x => CurrentUser.VisibleCultures == null || !CurrentUser.VisibleCultures.Any() || CurrentUser.VisibleCultures.Any(y => y == x)).OrderBy(x => x.Length).ThenBy(x => x).ToList();
        }

        public IList<string> AvailableCultures { get; private set; }
        public IEnumerable<ResxKey> Resources { get; private set; }

        public User CurrentUser { get; private set; }
        public string CurrentSection { get; set; }
    }
}