using System.Collections.Generic;

namespace SybilDetection.UI.ViewModels.API
{
    public class SybilCheckerInsightsViewModel
    {
        public string TotalCluster { get; set; }
        public string TotalAddress { get; set; }
        public double AverageSybilScore { get; set; }

        public List<ClusterSybilScorePieResults> ClusterScorePieResults { get; set; }
        public List<AddressSybilScorePieResults> AddressScorePieResults { get; set; }
    }

    public class ClusterSybilScorePieResults
    {
        public string ClusterType { get; set; }
        public int NumberOfCluster { get; set; }
    }

    public class AddressSybilScorePieResults
    {
        public string AddressType { get; set; }
        public int NumberOfAddress { get; set; }
    }
}