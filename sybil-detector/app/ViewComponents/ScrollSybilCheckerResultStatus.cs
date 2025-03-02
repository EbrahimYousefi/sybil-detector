using Microsoft.AspNetCore.Mvc;
using SybilDetection.UI.ViewModels.Checker;
using System.Threading.Tasks;

namespace SybilDetection.UI.ViewComponents
{
    public class ScrollSybilCheckerResultStatusViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(CheckerResultsViewModel model)
        {
            return View("Default", model);
        }
    }
}