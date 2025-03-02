using Microsoft.AspNetCore.Mvc;
using SybilDetection.UI.ViewModels.Error;
using System.Diagnostics;


namespace SybilDetection.UI.Controllers
{
    public class HomeController : Controller
    {
        #region Index
        public IActionResult Index()
        {
            return View();
        }
        #endregion

        #region Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("/error/404")]
        public IActionResult NotFoundPage()
        {
            string originalPath = "unknown";

            if (HttpContext.Items.ContainsKey("originalPath"))
            {
                originalPath = HttpContext.Items["originalPath"] as string;
            }

            return View();
        }
        #endregion
    }
}