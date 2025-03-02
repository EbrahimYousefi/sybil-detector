using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using SybilDetection.UI.Helper.ReadData.CsvReaders;
using SybilDetection.UI.Helper.ReadData.ListConverter;
using SybilDetection.UI.ViewModels.API;
using SybilDetection.UI.ViewModels.AppSettings;
using SybilDetection.UI.ViewModels.Helper.CsvReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SybilDetection.UI.Helper.ReadData.FileMapper
{
    public class FileMapperHelper : IFileMapperHelper
    {
        private readonly IOptions<AppSettingsViewModel> _settings;
        private readonly IWebHostEnvironment _environment;
        private ICsvReaderHelper _csvReaderHelper;
        private IAddressListConverter _addressListConverter;

        public FileMapperHelper(
            IOptions<AppSettingsViewModel> settings,
            IWebHostEnvironment environment,
            ICsvReaderHelper csvReaderHelper,
            IAddressListConverter addressListConverter)
        {
            _settings = settings;
            _environment = environment;
            _csvReaderHelper = csvReaderHelper;
            _addressListConverter = addressListConverter;
        }

        public void WriteToXMLFile()
        {
            try
            {
                var sybilResultsPath = _settings.Value.Document.SybilResultsRootPath;
                var sybilResultsPathFolder = Path.Combine(_environment.WebRootPath, $"{sybilResultsPath}");
                var sybilResults = _csvReaderHelper.ReadCsvFileV2($"{sybilResultsPathFolder}\\results.csv");

                var addressesWithClusterList = new List<AddressWithCluster>();

                XNamespace clustersXML = string.Empty;
                XElement clustersRoot = new XElement(clustersXML + "clusters");

                foreach (var cluster in sybilResults)
                {
                    string clusterHead;
                    double bulkTransfersScore, behaviorRiskScore, totalSybilScore;
                    CalculatingScore(cluster, out clusterHead, out bulkTransfersScore, out behaviorRiskScore, out totalSybilScore);

                    var clusterScoreStatus = GetClusterScoreStatus(totalSybilScore);

                    XElement clusterElement = new XElement(
                        clustersXML + "cluster",
                        new XElement(clustersXML + "index", cluster.index.ToString()),
                        new XElement(clustersXML + "cluster_id", cluster.cluster_id.ToString()),
                        new XElement(clustersXML + "cluster_head", clusterHead),
                        new XElement(clustersXML + "cluster_size_bulk_transfers", cluster.cluster_size_bt.ToString()),
                        new XElement(clustersXML + "cluster_size_user_behavior", cluster.cluster_size_ub.ToString()),
                        new XElement(clustersXML + "temporal_patterns_risk_score", cluster.temporal_patterns_risk_score.ToString()),
                        new XElement(clustersXML + "similar_amounts_risk_score", cluster.similar_amounts_risk_score.ToString()),
                        new XElement(clustersXML + "bulk_transfers_risk_score", bulkTransfersScore.ToString()),
                        new XElement(clustersXML + "risk_factors", cluster.risk_factors.ToString()),
                        new XElement(clustersXML + "transactions", cluster.transactions.ToString()),
                        new XElement(clustersXML + "method_similarity_score", cluster.method_similarity_score.ToString()),
                        new XElement(clustersXML + "value_similarity_score", cluster.value_similarity_score.ToString()),
                        new XElement(clustersXML + "target_similarity_score", cluster.target_similarity_score.ToString()),
                        new XElement(clustersXML + "temporal_pattern_score", cluster.temporal_pattern_score.ToString()),
                        new XElement(clustersXML + "behavior_risk_score", behaviorRiskScore.ToString()),
                        new XElement(clustersXML + "details", cluster.details.ToString()),
                        new XElement(clustersXML + "addresses", cluster.addresses.ToString()),
                        new XElement(clustersXML + "total_sybil_score", totalSybilScore.ToString()),
                        new XElement(clustersXML + "status", clusterScoreStatus.ToString()));

                    clustersRoot.Add(clusterElement);

                    var addresss = _addressListConverter.ConvertFromString(cluster.addresses);

                    var currentClusterId = cluster.cluster_id.ToString();

                    foreach (var address in addresss)
                    {
                        addressesWithClusterList.Add(new AddressWithCluster { value = address, cluster_ids = currentClusterId });
                    }
                }

                var clusterDocument = SaveXMLFile(clustersRoot, _settings.Value.Document.ClustersListRootPath, "clusters_list.xml");

                var groupedAddressesList = addressesWithClusterList.GroupBy(a => a.value).Select(g => new { value = g.Key, cluster_ids = string.Join(",", g.Select(a => a.cluster_ids)) }).ToList();

                XNamespace addressesXML = string.Empty;
                XElement addressesRoot = new XElement(addressesXML + "addresses");

                foreach (var address in groupedAddressesList)
                {
                    XElement addresslement = new XElement(
                        addressesXML + "address",
                            new XElement(addressesXML + "value", address.value.ToString()),
                            new XElement(addressesXML + "cluster_ids", address.cluster_ids.ToString()));

                    addressesRoot.Add(addresslement);
                }

                var addressDocument = SaveXMLFile(addressesRoot, _settings.Value.Document.ClusterAddressesListRootPath, "addresses_list.xml");


                var scrollClaimerResultPath = _settings.Value.Document.ScrollAirdropClaimerList;
                var scrollClaimerResultPathFolder = Path.Combine(_environment.WebRootPath, $"{scrollClaimerResultPath}");
                var scrollClaimerResults = _csvReaderHelper.ReadCsvFileV3($"{scrollClaimerResultPathFolder}\\scroll_airdrop_claimer_list.csv");

                XNamespace claimerXML = string.Empty;
                XElement claimerRoot = new XElement(claimerXML + "addresses");

                foreach (var address in scrollClaimerResults)
                {
                    XElement claimerElement = new XElement(
                        claimerXML + "address",
                        new XElement(claimerXML + "address", address.ADDRESS.ToString()),
                        new XElement(claimerXML + "amount", address.TOTAL_CLAIM_AMOUNTS.ToString()));

                    claimerRoot.Add(claimerElement);
                }

                var claimerDocument = SaveXMLFile(claimerRoot, _settings.Value.Document.ScrollAirdropClaimerList, "scroll_airdrop_claimer_list.xml");
            }
            catch
            {

                return;
            }
        }

        public void WriteScrollAirdropStatusToXMLFile(ScrollAirdropStatusDuneApiResponse scrollAirdropData)
        {
            try
            {
                var sybilResultsPath = _settings.Value.Document.SybilResultsRootPath;
                var sybilResultsPathFolder = Path.Combine(_environment.WebRootPath, $"{sybilResultsPath}");

                var sybilResults = _csvReaderHelper.ReadCsvFileV2($"{sybilResultsPathFolder}\\results.csv");

                var addressesWithClusterList = new List<AddressWithCluster>();

                XNamespace scrollAirdropXML = string.Empty;
                XElement scrollAirdropRoot = new XElement(scrollAirdropXML + "dates");

                foreach (var date in scrollAirdropData.result.rows)
                {
                    XElement airdropElement = new XElement(
                        scrollAirdropXML + "day",
                        new XElement(scrollAirdropXML + "date", date.DATE.ToString()),
                        new XElement(scrollAirdropXML + "amount", date.TOTAL_CLAIM_AMOUNTS.ToString()),
                        new XElement(scrollAirdropXML + "users", date.TOTAL_NUMBER_OF_WALLETS.ToString()),
                        new XElement(scrollAirdropXML + "last_update", DateTime.Now.ToString()));

                    scrollAirdropRoot.Add(airdropElement);
                }

                var airdropDocument = SaveXMLFile(scrollAirdropRoot, _settings.Value.Document.ScrollAirdropStatusRootPath, "scroll_airdrop_status.xml");
            }
            catch
            {
                return;
            }
        }

        private XDocument SaveXMLFile(XElement root, string path, string fileName)
        {
            XDocument document = new XDocument(root);

            var fliePath = path;
            var pathFolder = Path.Combine(_environment.WebRootPath, $"{fliePath}");

            if (!Directory.Exists(pathFolder))
                Directory.CreateDirectory(pathFolder);

            byte[] output = Encoding.UTF8.GetBytes(document.ToString());

            FileStream fileStream = File.Create($"{pathFolder}\\{fileName}");
            fileStream.Write(output, 0, output.Length);
            fileStream.Close();
            return document;
        }

        public double ScaleToBase100(double number)
        {
            const double maxOld = 6.0;
            const double maxNew = 100.0;

            return Math.Ceiling((number / maxOld) * maxNew);
        }

        public double ScaleAndCombine(double transfersScore, double behaviorScore)
        {
            double weightedSum = (transfersScore * 0.3) + (behaviorScore * 0.7);

            return Math.Ceiling(weightedSum);
        }

        private void CalculatingScore(CsvReaderViewModels cluster, out string clusterHead, out double bulkTransfersScore, out double behaviorRiskScore, out double totalSybilScore)
        {
            clusterHead = _addressListConverter.FixAddress(cluster.cluster_head).ToString();
            bulkTransfersScore = Math.Ceiling(!string.IsNullOrWhiteSpace(cluster.bulk_transfers_risk_score) ? ScaleToBase100(float.Parse(cluster.bulk_transfers_risk_score)) : 0);
            behaviorRiskScore = Math.Ceiling(!string.IsNullOrWhiteSpace(cluster.behavior_risk_score) ? float.Parse(cluster.behavior_risk_score) : 0);
            totalSybilScore = ScaleAndCombine(!string.IsNullOrWhiteSpace(cluster.bulk_transfers_risk_score) ? ScaleToBase100(float.Parse(cluster.bulk_transfers_risk_score)) : 0, !string.IsNullOrWhiteSpace(cluster.behavior_risk_score) ? float.Parse(cluster.behavior_risk_score) : 0);
        }

        public string GetClusterScoreStatus(double totalScore)
        {
            string status = "";

            if (totalScore >= 0 && totalScore < 30)
                status = "Low-Risk";
            else if (totalScore >= 30 && totalScore < 60)
                status = "Moderate-Risk";
            else if (totalScore >= 60 && totalScore <= 100)
                status = "High-Risk";

            return status;
        }

        public T ReadFromXMLFile<T>(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StreamReader reader = new StreamReader(filePath))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        public async Task<T> ReadFromXMLFileAsync<T>(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StreamReader reader = new StreamReader(filePath))
            {
                string xmlContent = await reader.ReadToEndAsync();
                using (StringReader stringReader = new StringReader(xmlContent))
                {
                    return await Task.Run(() => (T)serializer.Deserialize(stringReader));
                }
            }
        }

        public List<AddressClusterResult> GetTopClustersForAddresses(AddressWithClusterList addressList, ClusterViewModelsList clusterList)
        {
            if (addressList == null || clusterList == null)
                return new List<AddressClusterResult>();

              var clusterLookup = clusterList.Clusters
                .ToLookup(c => c.cluster_id);

            var result = addressList.Addresses
                .Select(address =>
                {
                    var clusterIds = address.cluster_ids
                        .Split(',')
                        .Select(id => id.Trim())
                        .Where(id => clusterLookup.Contains(id))
                        .ToList();

                    var bestCluster = clusterIds
                        .SelectMany(id => clusterLookup[id])
                        .OrderByDescending(c => Convert.ToDouble(c.total_sybil_score))
                        .FirstOrDefault();

                    return new AddressClusterResult
                    {
                        Address = address.value,
                        BestClusterId = bestCluster?.cluster_id ?? "N/A",
                        BestClusterHead = bestCluster?.cluster_head ?? "N/A",
                        ClusterSybilStatus = bestCluster?.status ?? "N/A",
                        BestSybilScore = bestCluster?.total_sybil_score ?? "0"
                    };
                })
                .ToList();

            return result;
        }
    }
}