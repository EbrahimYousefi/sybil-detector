using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SybilDetection.UI.Helper.APIHelper.ENS;
using SybilDetection.UI.Helper.ReadData.ListConverter;
using SybilDetection.UI.ViewModels.API;
using SybilDetection.UI.ViewModels.AppSettings;
using SybilDetection.UI.ViewModels.Checker;
using SybilDetection.UI.ViewModels.Helper.CsvReader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SybilDetection.UI.Controllers.Checker
{
    public class ScrollController : Controller
    {
        private IENSServices _eNSServices;
        private readonly IOptions<AppSettingsViewModel> _settings;
        private readonly IWebHostEnvironment _environment;
        private readonly IAddressListConverter _addressListConverter;

        public ScrollController(
            IENSServices eNSServices,
            IOptions<AppSettingsViewModel> settings,
            IWebHostEnvironment environment,
            IAddressListConverter addressListConverter)
        {
            _eNSServices = eNSServices;
            _settings = settings;
            _environment = environment;
            _addressListConverter = addressListConverter;
        }

        #region Index
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Overview()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Checker([FromBody] WalletRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Address))
            {
                return Json(new { success = false, message = "Please enter an address." });
            }

            string address = request.Address.Trim();
            string addressesFilePath, clustersFilePath, airdropEligibilityFilePath;
            string apiKey = "FH22GGM8M9558ZZGEF1W7CGRTGHNWIVGGE";
            int startBlock = 0;
            int endBlock = 10303359;
            int page = 1;
            int offset = 10000;
            string sort = "asc";

            Regex evmRegex = new Regex("^0x[a-fA-F0-9]{40}$");

            Regex ensRegex = new Regex("^[a-zA-Z0-9-]+\\.eth$");

            if (!evmRegex.IsMatch(address) && !ensRegex.IsMatch(address))
                return Json(new { success = false, message = "Invalid address. Enter a valid EVM address or ENS." });

            if (ensRegex.IsMatch(address))
            {
                address = _eNSServices.GetAddressByDomain(address).Result.Trim().ToString();

                if (address == null || string.IsNullOrWhiteSpace(address))
                    return Json(new { success = false, message = "Invalid address. Enter a valid EVM address or ENS." });
            }

            var model = new CheckerResultsViewModel();

            model.address = address;

            GetFolderPath(out addressesFilePath, out clustersFilePath, out airdropEligibilityFilePath);
            CheckAndGetSybilResults(address, model, addressesFilePath, clustersFilePath, airdropEligibilityFilePath);

            var scrollTransactionResultsModel = new ScrollTransactionResultsViewModel();

            await GetEtherBalanceFromScroll(address, scrollTransactionResultsModel, apiKey);

            await GetScrollAllTransactions(address, apiKey, startBlock, endBlock, page, offset, sort, model, scrollTransactionResultsModel);
            await GetScrollSeasonMarks(address, model);

            return Json(new { success = true, message = "Successful", model = model });
        }

        [HttpPost]
        public IActionResult PassToScrollSybilCheckerResultStatus([FromBody] CheckerResultsViewModel model)
        {
            return ViewComponent("ScrollSybilCheckerResultStatus", model);
        }
        #endregion

        #region Functions
        private static async Task GetScrollAllTransactions(string address, string apiKey, int startBlock, int endBlock, int page, int offset, string sort, CheckerResultsViewModel model, ScrollTransactionResultsViewModel scrollTransactionResultsModel)
        {
            var allTransactions = new List<ScrollTransactions>();

            var client = new HttpClient();

            while (true)
            {
                string url = $"https://api.scrollscan.com/api?module=account&action=txlist&address={address}&startblock={startBlock}&endblock={endBlock}&page={page}&offset={offset}&sort={sort}&apikey={apiKey}";
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    break;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<ScrollApiTransactionResponse>(responseBody);

                if (jsonResponse == null || jsonResponse.message != "OK" || jsonResponse.result == null || jsonResponse.result.Count == 0)
                {
                    break;
                }

                allTransactions.AddRange(jsonResponse.result);
                allTransactions = allTransactions.OrderBy(a => a.timeStamp).ToList();
                page++;
            }

            if (allTransactions.Count == 0)
            {
                scrollTransactionResultsModel.has_transaction = false;
                scrollTransactionResultsModel.total_volume = 0;
                scrollTransactionResultsModel.total_transaction = 0;
                scrollTransactionResultsModel.unique_days = 0;
                scrollTransactionResultsModel.unique_weeks = 0;
                scrollTransactionResultsModel.wallet_age = 0;
            }
            else
            {
                scrollTransactionResultsModel.has_transaction = true;
                scrollTransactionResultsModel.total_transaction = allTransactions.Count;
                scrollTransactionResultsModel.total_volume = allTransactions.Sum(tx => decimal.Parse(tx.value, CultureInfo.InvariantCulture) * (decimal)Math.Pow(10, -18));

                scrollTransactionResultsModel.unique_days = allTransactions.Select(tx => DateTimeOffset.FromUnixTimeSeconds(long.Parse(tx.timeStamp)).Date).Distinct().Count();
                scrollTransactionResultsModel.unique_weeks = allTransactions.Select(tx => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                    DateTimeOffset.FromUnixTimeSeconds(long.Parse(tx.timeStamp)).DateTime,
                    CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)).Distinct().Count();

                scrollTransactionResultsModel.unique_months = allTransactions
                    .Select(tx => DateTimeOffset.FromUnixTimeSeconds(long.Parse(tx.timeStamp)).ToString("yyyy-MM"))
                    .Distinct()
                    .Count();

                long firstTransactionTimestamp = long.Parse(allTransactions[0].timeStamp);
                DateTime firstTransactionDate = DateTimeOffset.FromUnixTimeSeconds(firstTransactionTimestamp).UtcDateTime;
                scrollTransactionResultsModel.wallet_age = (int)(DateTime.UtcNow - firstTransactionDate).TotalDays;
            }

            var dailyTransactions = allTransactions
            .GroupBy(tx => DateTimeOffset.FromUnixTimeSeconds(long.Parse(tx.timeStamp)).Date)
            .Select(g => new DailyScrollTransactionResultsViewModel
            {
                day = g.Key.ToString("yyyy-MM-dd"),
                total_transaction = g.Count(),
                total_volume = g.Sum(tx => decimal.Parse(tx.value, CultureInfo.InvariantCulture) * (decimal)Math.Pow(10, -18))
            })
            .ToList();

            model.scroll_transaction_results = scrollTransactionResultsModel;
            model.daily_scroll_transaction_results = dailyTransactions;
        }

        private static async Task GetEtherBalanceFromScroll(string address, ScrollTransactionResultsViewModel scrollTransactionResultsModel, string apiKey)
        {
            string url = $"https://api.scrollscan.com/api?module=account&action=balance&address={address}&tag=latest&apikey={apiKey}";
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<EtherBalanceScrollApiResponse>(responseBody);

                if (jsonResponse != null && jsonResponse.message == "OK" && !string.IsNullOrEmpty(jsonResponse.result))
                    scrollTransactionResultsModel.ether_balance = decimal.Parse(jsonResponse.result) * (decimal)Math.Pow(10, -18);
                else
                    scrollTransactionResultsModel.ether_balance = 0;
            }
            else

                scrollTransactionResultsModel.ether_balance = 0;
        }

        private void CheckAndGetSybilResults(string address, CheckerResultsViewModel model, string addressesFilePath, string clustersFilePath, string airdropEligibilityFilePath)
        {
            var addressResult = GetAddressData(addressesFilePath, address);

            var eligibilityResult = GetAirdropEligibilityData(airdropEligibilityFilePath, address);

            if (addressResult != null)
            {
                List<string> clusterIds = addressResult.cluster_ids.Split(',').Select(id => id.Trim()).ToList();

                var bestCluster = GetHighestSybilScoreCluster(clustersFilePath, clusterIds);

                if (bestCluster != null)
                {
                    var sybilModel = new SybilResultsViewModel
                    {
                        cluster_id = bestCluster.cluster_id,
                        cluster_head = bestCluster.cluster_head,
                        cluster_size_bulk_transfers = bestCluster.cluster_size_bulk_transfers,
                        status = bestCluster.status,
                        total_sybil_score = bestCluster.total_sybil_score,
                        addresses = bestCluster.addresses.Trim('[', ']').Replace("'", "").Split(',').Select(id => id.Trim()).ToList(),
                        in_cluster = true
                    };

                    sybilModel.sybil_reason_results = AnalyzeCluster(bestCluster);

                    model.sybil_results = sybilModel;
                }
                else
                {
                    var sybilModel = new SybilResultsViewModel
                    {
                        status = "No Risk",
                        total_sybil_score = "0",
                        in_cluster = false
                    };

                    sybilModel.sybil_reason_results = AnalyzeCluster(bestCluster);

                    model.sybil_results = sybilModel;
                }
            }
            else
            {

                var sybilModel = new SybilResultsViewModel
                {
                    status = "No Risk",
                    total_sybil_score = "0",
                    in_cluster = false
                };

                sybilModel.sybil_reason_results = AnalyzeCluster(null);

                model.sybil_results = sybilModel;
            }

            if (eligibilityResult != null)
            {
                var eligibilityModel = new ScrollAirdropEligibilityStatusResultsViewModel
                {
                    amount = eligibilityResult.amount,
                    is_eligible = true
                };

                model.scroll_airdrop_eligibility_status_results = eligibilityModel;
            }
            else
            {
                var eligibilityModel = new ScrollAirdropEligibilityStatusResultsViewModel
                {
                    amount = 0,
                    is_eligible = false
                };

                model.scroll_airdrop_eligibility_status_results = eligibilityModel;
            }
        }

        public List<SybilReasonResults> AnalyzeCluster(ClusterViewModels cluster)
        {
            var results = new List<SybilReasonResults>();

            if (cluster == null)
            {
                results.Add(new SybilReasonResults
                {
                    reason = "Overall Analysis",
                    description = "This address is not part of any cluster. No sybil activity detected."
                });
                return results;
            }
            else
            {
                string overallRisk = cluster.status;
                string overallText = $"The cluster head in this cluster is: {cluster.cluster_head}. " +
                                     $"Cluster contains {cluster.cluster_size_user_behavior} address(es) and has a total sybil score of {cluster.total_sybil_score}, " +
                                     $"indicating a {overallRisk} sybil status.";
                results.Add(new SybilReasonResults
                {
                    reason = "Overall Cluster Analysis",
                    description = overallText
                });
            }

            if (!string.IsNullOrWhiteSpace(cluster.risk_factors) && !cluster.risk_factors.Contains("['']"))
            {
                string transformedRisk = TransformRiskFactors(cluster.risk_factors);
                string riskText = $"{transformedRisk}.";
                results.Add(new SybilReasonResults
                {
                    reason = "Bulk Transfers Analysis",
                    description = riskText
                });
            }

            try
            {
                string detailsJson = cluster.details.Replace("'", "\"");
                detailsJson = PreprocessDetailsJson(detailsJson);

                dynamic detailsObj = Newtonsoft.Json.JsonConvert.DeserializeObject(detailsJson);

                if (detailsObj.method_analysis != null)
                {
                    string mostCommonMethod = detailsObj.method_analysis.most_common_method;
                    mostCommonMethod = Regex.Replace(mostCommonMethod, @"0x[0-9a-fA-F]+", "same");
                    string methodText = $"{mostCommonMethod}.";
                    results.Add(new SybilReasonResults
                    {
                        reason = "Contract Interaction Analysis",
                        description = methodText
                    });
                }

                if (detailsObj.value_analysis != null)
                {
                    int largestGroup = detailsObj.value_analysis.largest_similar_value_group;
                    string valueText = $"There are {largestGroup} group of transactions with similar values.";
                    results.Add(new SybilReasonResults
                    {
                        reason = "Value Analysis",
                        description = valueText
                    });
                }

                if (detailsObj.temporal_analysis != null)
                {
                    int sequentialGroups = detailsObj.temporal_analysis.sequential_groups;
                    string temporalSeqText = $"{sequentialGroups} sequential group(s) of transactions (with intervals less than 5 minutes) were identified.";
                    results.Add(new SybilReasonResults
                    {
                        reason = "Sequential Time Analysis",
                        description = temporalSeqText
                    });

                    int largestSequence = detailsObj.temporal_analysis.largest_sequence;
                    string temporalLargestText = $"The largest sequence contains {largestSequence} transactions, indicating highly similar timing behavior.";
                    results.Add(new SybilReasonResults
                    {
                        reason = "Largest Sequence Analysis",
                        description = temporalLargestText
                    });

                    double avgTimeGap = detailsObj.temporal_analysis.avg_time_gap;
                    string temporalAvgText = $"The average time gap between transactions is {avgTimeGap} minute(s).";
                    results.Add(new SybilReasonResults
                    {
                        reason = "Average Transaction Time Analysis",
                        description = temporalAvgText
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new SybilReasonResults
                {
                    reason = "Analysis Error",
                    description = "An error occurred while parsing the details section: " + ex.Message
                });
            }

            return results;
        }

        public static string PreprocessDetailsJson(string detailsJson)
        {
            if (string.IsNullOrWhiteSpace(detailsJson))
                return detailsJson;

            string pattern = @"datetime\.datetime\(\s*(\d{4})\s*,\s*(\d{1,2})\s*,\s*(\d{1,2})\s*,\s*(\d{1,2})\s*,\s*(\d{1,2})(?:\s*,\s*(\d{1,2}))?\s*\)";
            detailsJson = Regex.Replace(detailsJson, pattern, m =>
            {
                int year = int.Parse(m.Groups[1].Value);
                int month = int.Parse(m.Groups[2].Value);
                int day = int.Parse(m.Groups[3].Value);
                int hour = int.Parse(m.Groups[4].Value);
                int minute = int.Parse(m.Groups[5].Value);
                int second = 0;
                if (m.Groups[6].Success)
                {
                    second = int.Parse(m.Groups[6].Value);
                }
                DateTime dt = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                return "\"" + dt.ToString("o") + "\"";
            });

            return detailsJson;
        }

        public string TransformRiskFactors(string riskFactors)
        {
            if (string.IsNullOrWhiteSpace(riskFactors))
                return riskFactors;

            riskFactors = riskFactors.Trim('[', ']', '\'').Trim();

            int index = riskFactors.IndexOf("Low variety in", StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                string textBefore = riskFactors.Substring(0, index);
                if (!textBefore.EndsWith(" and "))
                {
                    riskFactors = textBefore + " and " + riskFactors.Substring(index);
                }
            }

            riskFactors = riskFactors.Replace("|", " and ");

            riskFactors = riskFactors.Trim();
            if (riskFactors.EndsWith(" and "))
            {
                riskFactors = riskFactors.Substring(0, riskFactors.Length - " and ".Length).Trim();
            }

            return riskFactors;
        }

        private void GetFolderPath(out string addressesFilePath, out string clustersFilePath, out string airdropEligibilityFilePath)
        {
            var addressesListPath = _settings.Value.Document.ClusterAddressesListRootPath;
            var addressesListFolder = Path.Combine(_environment.WebRootPath, $"{addressesListPath}");
            addressesFilePath = $"{addressesListFolder}\\addresses_list.xml";
            var clustersListPath = _settings.Value.Document.ClustersListRootPath;
            var clustersListFolder = Path.Combine(_environment.WebRootPath, $"{clustersListPath}");
            clustersFilePath = $"{clustersListFolder}\\clusters_list.xml";

            var eligibilityListPath = _settings.Value.Document.ScrollAirdropClaimerList;
            var eligibilityListFolder = Path.Combine(_environment.WebRootPath, $"{eligibilityListPath}");
            airdropEligibilityFilePath = $"{eligibilityListFolder}\\scroll_airdrop_claimer_list.xml";
        }

        private static AddressWithCluster GetAddressData(string xmlPath, string searchValue)
        {
            XDocument doc = XDocument.Load(xmlPath);

            return doc.Descendants("address")
                      .Where(x => (string)x.Element("value") == searchValue)
                      .Select(x => new AddressWithCluster
                      {
                          value = (string)x.Element("value"),
                          cluster_ids = (string)x.Element("cluster_ids")
                      })
                      .FirstOrDefault();
        }

        private static ScrollAirdropEligibilityStatusResultsViewModel GetAirdropEligibilityData(string xmlPath, string searchValue)
        {
            XDocument doc = XDocument.Load(xmlPath);

            return doc.Descendants("address")
                      .Where(x => (string)x.Element("address") == searchValue)
                      .Select(x => new ScrollAirdropEligibilityStatusResultsViewModel
                      {
                          amount = (decimal)x.Element("amount"),
                          is_eligible = (string)x.Element("address") == searchValue ? true : false
                      })
                      .FirstOrDefault();
        }

        private static ClusterViewModels GetHighestSybilScoreCluster(string xmlPath, List<string> clusterIds)
        {
            XDocument doc = XDocument.Load(xmlPath);

            return doc.Descendants("cluster")
                      .Where(x => clusterIds.Contains((string)x.Element("cluster_id")))
                      .Select(x => new ClusterViewModels
                      {
                          index = (string)x.Element("index"),
                          cluster_id = (string)x.Element("cluster_id"),
                          cluster_head = (string)x.Element("cluster_head"),
                          cluster_size_bulk_transfers = (string)x.Element("cluster_size_bulk_transfers"),
                          cluster_size_user_behavior = (string)x.Element("cluster_size_user_behavior"),
                          temporal_patterns_risk_score = (string)x.Element("temporal_patterns_risk_score"),
                          similar_amounts_risk_score = (string)x.Element("similar_amounts_risk_score"),
                          bulk_transfers_risk_score = (string)x.Element("bulk_transfers_risk_score"),
                          risk_factors = (string)x.Element("risk_factors"),
                          transactions = (string)x.Element("transactions"),
                          method_similarity_score = (string)x.Element("method_similarity_score"),
                          value_similarity_score = (string)x.Element("value_similarity_score"),
                          target_similarity_score = (string)x.Element("target_similarity_score"),
                          temporal_pattern_score = (string)x.Element("temporal_pattern_score"),
                          behavior_risk_score = (string)x.Element("behavior_risk_score"),
                          details = (string)x.Element("details"),
                          addresses = (string)x.Element("addresses"),
                          total_sybil_score = (string)x.Element("total_sybil_score"),
                          status = (string)x.Element("status")
                      })
                      .OrderByDescending(x => Convert.ToInt32(x.total_sybil_score))
                      .FirstOrDefault();
        }

        private async Task GetScrollSeasonMarks(string address, CheckerResultsViewModel model)
        {
            string url = $"https://mainnet-api-sessions.scroll.io/v1/session1-marks/{address}";

            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var markModel = new ScrollAirdropActivityResultsViewModel
                {
                    season_1_mark = "0"
                };

                model.scroll_mark_activity_results = markModel;
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<ScrollApiMarksResponse>(responseBody);

            if (jsonResponse?.message == "OK" && jsonResponse.result?.marks != null)
            {
                var markModel = new ScrollAirdropActivityResultsViewModel
                {
                    season_1_mark = _addressListConverter.FormatNumber(Math.Round(decimal.Parse(jsonResponse.result.marks), 2)).ToString()
                };

                model.scroll_mark_activity_results = markModel;
            }
            else
            {
                var markModel = new ScrollAirdropActivityResultsViewModel
                {
                    season_1_mark = "0"
                };

                model.scroll_mark_activity_results = markModel;
            }
        }
        #endregion
    }
}