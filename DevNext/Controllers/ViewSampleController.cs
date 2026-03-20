using Site.Models;
using Microsoft.AspNetCore.Mvc;

namespace Site.Controllers
{
    public class ViewSampleController : Controller
    {
        public IActionResult PartialSample()
        {
            var model = new PartialSampleViewModel();
            model.ChildList.Add(new PartialSampleChildModel { Text = "サンプル1", Number = 1, Order = 0 });
            model.ChildList.Add(new PartialSampleChildModel { Text = "サンプル2", Number = 2, Order = 1 });
            return View(model);
        }

        [HttpPost]
        public IActionResult PartialSample(PartialSampleViewModel model)
        {
            return View(model);
        }

        public IActionResult SortSample()
        {
            var model = new PartialSampleViewModel();
            model.ChildList.Add(new PartialSampleChildModel { Text = "アイテム1", Number = 1, Order = 1 });
            model.ChildList.Add(new PartialSampleChildModel { Text = "アイテム2", Number = 2, Order = 2 });
            return View(model);
        }

        [HttpPost]
        public IActionResult SortSample(PartialSampleViewModel model)
        {
            return View(model);
        }
    }
}
