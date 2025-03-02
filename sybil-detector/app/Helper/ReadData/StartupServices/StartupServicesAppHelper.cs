using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using SybilDetection.UI.Helper.ReadData.FileMapper;
using SybilDetection.UI.ViewModels.AppSettings;
using System.IO;

namespace SybilDetection.UI.Helper.ReadData.StartupServices
{
    public class StartupServicesAppHelper : IStartupServicesAppHelper
    {
        private readonly IOptions<AppSettingsViewModel> _settings;
        private readonly IWebHostEnvironment _environment;
        private IFileMapperHelper _fileMapperHelper;

        private bool _hasRun = false;

        public StartupServicesAppHelper(
            IWebHostEnvironment environment,
            IOptions<AppSettingsViewModel> settings,
            IFileMapperHelper fileMapperHelper)
        {
            _environment = environment;
            _settings = settings;
            _fileMapperHelper = fileMapperHelper;
        }

        public void StartApp()
        {
            if (_hasRun) return;

            var clustersListPath = _settings.Value.Document.ClustersListRootPath;
            var clustersListFolder = Path.Combine(_environment.WebRootPath, $"{clustersListPath}");
            var filePath = $"{clustersListFolder}\\clusters_list.xml";

            if (!File.Exists(filePath))
            {
                _fileMapperHelper.WriteToXMLFile();
            }

            _hasRun = true;
        }
    }
}