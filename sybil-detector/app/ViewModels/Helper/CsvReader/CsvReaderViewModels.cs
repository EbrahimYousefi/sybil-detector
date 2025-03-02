using SybilDetection.UI.Helper.ReadData.ListConverter;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SybilDetection.UI.ViewModels.Helper.CsvReader
{
    public class CsvReaderForScrollClaimerViewModels
    {
        public string ADDRESS { get; set; }
        public string TOTAL_CLAIM_AMOUNTS { get; set; }
    }

    public class CsvReaderViewModels
    {
        public string index { get; set; }
        public string cluster_id { get; set; }
        public string cluster_head { get; set; }
        public string cluster_size_bt { get; set; }
        public string temporal_patterns_risk_score { get; set; }
        public string similar_amounts_risk_score { get; set; }
        public string bulk_transfers_risk_score { get; set; }
        public string risk_factors { get; set; }
        public string cluster_size_ub { get; set; }
        public string transactions { get; set; }
        public string behavior_risk_score { get; set; }
        public string method_similarity_score { get; set; }
        public string value_similarity_score { get; set; }
        public string target_similarity_score { get; set; }
        public string temporal_pattern_score { get; set; }
        public string details { get; set; }
        public string addresses { get; set; }
    }

    public class ClusterViewModels
    {
        public string index { get; set; }
        public string cluster_id { get; set; }
        public string cluster_head { get; set; }
        public string cluster_size_bulk_transfers { get; set; }
        public string cluster_size_user_behavior { get; set; }
        public string temporal_patterns_risk_score { get; set; }
        public string similar_amounts_risk_score { get; set; }
        public string bulk_transfers_risk_score { get; set; }
        public string risk_factors { get; set; }
        public string transactions { get; set; }
        public string method_similarity_score { get; set; }
        public string value_similarity_score { get; set; }
        public string target_similarity_score { get; set; }
        public string temporal_pattern_score { get; set; }
        public string behavior_risk_score { get; set; }
        public string details { get; set; }
        public string addresses { get; set; }
        public string total_sybil_score { get; set; }
        public string status { get; set; }
    }

    [XmlRoot("clusters")]
    public class ClusterViewModelsList
    {
        [XmlElement("cluster")]
        public List<ClusterViewModels> Clusters { get; set; }
    }

    public class AddressWithCluster
    {
        [XmlElement("value")]
        public string value { get; set; }

        [XmlElement("cluster_ids")]
        public string cluster_ids { get; set; }
    }

    [XmlRoot("addresses")]
    public class AddressWithClusterList
    {
        [XmlElement("address")]
        public List<AddressWithCluster> Addresses { get; set; }
    }

    public class AddressClusterResult
    {
        public string Address { get; set; }
        public string BestClusterId { get; set; }
        public string BestClusterHead { get; set; }
        public string BestSybilScore { get; set; }
        public string ClusterSybilStatus { get; set; }
    }
}