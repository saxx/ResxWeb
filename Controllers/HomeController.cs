using System;
using ResxWeb.Models;
using ResxWeb.Models.ViewModels.Home;
using System.Linq;
using System.Web.Mvc;

namespace ResxWeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var settings = LoadSettings();
            var user = LoadCurrentUser();
            var model = new IndexViewModel
                {
                    AvailableSources = settings.Sources.Select(x => x.Alias).Where(x => user.VisibleSources == null || !user.VisibleSources.Any() || user.VisibleSources.Contains(x)).OrderBy(x => x)
                };
            return View(model);
        }

        public ActionResult Edit(string id)
        {
            var settings = LoadSettings();
            var source = settings.Sources.FirstOrDefault(x => x.Alias == id);
            if (source != null)
            {
                var reader = new ResxDeltaReader();
                
                var model = new EditViewModel(reader, LoadCurrentUser(), id);
                model.Fill(source.Path);
                reader.ReadDelta(model.Resources, ResxDeltaReader.GetDeltaDirectoryPath(User.Identity.Name, id));

                return View(model);
            }
            return Redirect("~/");
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(FormCollection form, string id)
        {
            var settings = LoadSettings();
            var source = settings.Sources.FirstOrDefault(x => x.Alias == id);
            if (source != null)
            {
                var reader = new ResxDeltaReader();
                var writer = new ResxDeltaWriter();

                var model = new EditViewModel(reader, LoadCurrentUser(), id);
                model.Fill(source.Path);

                writer.SaveDelta(model.Resources, form, ResxDeltaReader.GetDeltaDirectoryPath(User.Identity.Name, id));

                return RedirectToAction("Edit", new { id });
            }
            return Redirect("~/");
        }

        public ActionResult Download(string id)
        {
            var settings = LoadSettings();
            var source = settings.Sources.FirstOrDefault(x => x.Alias == id);
            if (source != null)
            {
                var reader = new ResxDeltaReader();
                var writer = new ResxDeltaWriter();

                var model = new EditViewModel(reader, LoadCurrentUser(), id);
                model.Fill(source.Path);
                reader.ReadDelta(model.Resources, ResxDeltaReader.GetDeltaDirectoryPath(User.Identity.Name, id));

                var pathToArchive = writer.BuildArchive(source.Path, User.Identity.Name, source.Alias, model.Resources);
                return new FilePathResult(pathToArchive, "application/zip")
                    {
                        FileDownloadName = User.Identity.Name + "_" + id + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".zip"
                    };
            }
            return Redirect("~/");
        }

        private Settings LoadSettings()
        {
            return Settings.Load();
        }

        private User LoadCurrentUser()
        {
            var settings = LoadSettings();
            return settings.Users.First(x => x.UserName == User.Identity.Name);
        }
    }
}
