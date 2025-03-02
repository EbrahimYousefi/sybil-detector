namespace SybilDetection.UI.ViewModels.AppSettings
{
    public class AppSettingsViewModel
    {
        public DocumentSetting Document { get; set; }
    }

    public class DocumentSetting
    {
        public string SybilResultsRootPath { get; set; }
        public string ClustersListRootPath { get; set; }
        public string ClusterAddressesListRootPath { get; set; }
        public string ScrollAirdropStatusRootPath { get; set; }
        public string ScrollAirdropClaimerList { get; set; }
    }
}