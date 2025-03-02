using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SybilDetection.UI.Helper.ReadData.FileMapper;
using SybilDetection.UI.Helper.ReadData.ListConverter;
using SybilDetection.UI.ViewModels.AppSettings;
using System.Threading.Tasks;

namespace SybilDetection.UI.ViewComponents
{
    public class ScrollSybilCheckerViewComponent : ViewComponent
    {
        private readonly IFileMapperHelper _fileMapperHelper;
        private readonly IOptions<AppSettingsViewModel> _settings;
        private readonly IWebHostEnvironment _environment;
        private readonly IAddressListConverter _addressListConverter;
        public ScrollSybilCheckerViewComponent(
            IFileMapperHelper fileMapperHelper,
            IOptions<AppSettingsViewModel> settings,
            IWebHostEnvironment environment,
            IAddressListConverter addressListConverter)
        {
            _fileMapperHelper = fileMapperHelper;
            _settings = settings;
            _environment = environment;
            _addressListConverter = addressListConverter;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View();
        }
    }
}