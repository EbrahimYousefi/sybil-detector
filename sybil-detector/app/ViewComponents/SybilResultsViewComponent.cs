using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SybilDetection.UI.Helper.ReadData.FileMapper;
using SybilDetection.UI.Helper.ReadData.ListConverter;
using SybilDetection.UI.ViewModels.API;
using SybilDetection.UI.ViewModels.AppSettings;
using SybilDetection.UI.ViewModels.Helper.CsvReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SybilDetection.UI.ViewComponents
{
    public class SybilResultsViewComponent : ViewComponent
    {
        private readonly IFileMapperHelper _fileMapperHelper;
        private readonly IOptions<AppSettingsViewModel> _settings;
        private readonly IWebHostEnvironment _environment;
        private readonly IAddressListConverter _addressListConverter;

        private readonly IDistributedCache _cache;
        public SybilResultsViewComponent(
            IFileMapperHelper fileMapperHelper,
            IOptions<AppSettingsViewModel> settings,
            IWebHostEnvironment environment,
            IAddressListConverter addressListConverter,
            IDistributedCache cache)
        {
            _fileMapperHelper = fileMapperHelper;
            _settings = settings;
            _environment = environment;
            _addressListConverter = addressListConverter;

            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await GetSybilResults();

            return View(model);
        }

        private async Task<SybilCheckerInsightsViewModel> GetSybilResults()
        {
            const string cacheKey = "SybilResults_Scroll_sSRF";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<SybilCheckerInsightsViewModel>(cachedData);
            }

            var clustersList = new ClusterViewModelsList();
            var addressesList = new AddressWithClusterList();

            var clustersListPath = _settings.Value.Document.ClustersListRootPath;
            var clustersListFolder = Path.Combine(_environment.WebRootPath, clustersListPath);
            var clustersFilePath = $"{clustersListFolder}\\clusters_list.xml";

            var addressesListPath = _settings.Value.Document.ClusterAddressesListRootPath;
            var addressesListFolder = Path.Combine(_environment.WebRootPath, addressesListPath);
            var addressesFilePath = $"{addressesListFolder}\\addresses_list.xml";

            if (!File.Exists(clustersFilePath) || !File.Exists(addressesFilePath))
                _fileMapperHelper.WriteToXMLFile();
            else
            {
                var readClustersTask = _fileMapperHelper.ReadFromXMLFileAsync<ClusterViewModelsList>(clustersFilePath);
                var readAddressesTask = _fileMapperHelper.ReadFromXMLFileAsync<AddressWithClusterList>(addressesFilePath);
                await Task.WhenAll(readClustersTask, readAddressesTask);

                clustersList = readClustersTask.Result;
                addressesList = readAddressesTask.Result;
            }

            var finalResults = _fileMapperHelper.GetTopClustersForAddresses(addressesList, clustersList)
                .OrderByDescending(a => Convert.ToDouble(a.BestSybilScore));

            var result = finalResults
                .GroupBy(a => a.ClusterSybilStatus)
                .ToDictionary(g => g.Key, g => g.Select(a => a.Address).Distinct().Count());

            var totalAddresses = addressesList.Addresses.Count();
            var resultsInterpretation = result.Select(item => new AddressSybilScorePieResults
            {
                AddressType = item.Key,
                NumberOfAddress = (int)(((double)item.Value / totalAddresses) * 100),
            }).ToList();

            var model = new SybilCheckerInsightsViewModel
            {
                TotalAddress = _addressListConverter.FormatNumber(totalAddresses),
                TotalCluster = _addressListConverter.FormatNumber(clustersList.Clusters.Count()),
                AverageSybilScore = clustersList.Clusters.Average(a => int.Parse(a.total_sybil_score)),
                AddressScorePieResults = resultsInterpretation
            };

            var serializedData = JsonSerializer.Serialize(model);
            await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1439)
            });

            return model;
        }
    }
}