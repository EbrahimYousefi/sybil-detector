using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SybilDetection.UI.Helper.ReadData.FileMapper;
using SybilDetection.UI.Helper.ReadData.ListConverter;
using SybilDetection.UI.ViewModels.API;
using SybilDetection.UI.ViewModels.AppSettings;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SybilDetection.UI.ViewComponents
{
    public class ScrollAirdropStatusViewComponent : ViewComponent
    {
        private readonly IFileMapperHelper _fileMapperHelper;
        private readonly IOptions<AppSettingsViewModel> _settings;
        private readonly IWebHostEnvironment _environment;
        private readonly IAddressListConverter _addressListConverter;

        private readonly IMemoryCache _cache;

        public ScrollAirdropStatusViewComponent(
            IFileMapperHelper fileMapperHelper,
            IOptions<AppSettingsViewModel> settings,
            IWebHostEnvironment environment,
            IAddressListConverter addressListConverter,
            IMemoryCache cache)
        {
            _fileMapperHelper = fileMapperHelper;
            _settings = settings;
            _environment = environment;
            _addressListConverter = addressListConverter;

            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var data = await GetScrollAirdropStatusData();

            return View(data);
        }

        private async Task<ScrollAirdropStatusDataResults> GetScrollAirdropStatusData()
        {
            const string cacheKey = "ScrollAirdropStatusData_sAsDF";

            if (_cache.TryGetValue(cacheKey, out ScrollAirdropStatusDataResults cachedData))
            {
                return cachedData;
            }

            var fileResultsPath = _settings.Value.Document.ScrollAirdropStatusRootPath;
            var folderPath = Path.Combine(_environment.WebRootPath, fileResultsPath);
            var filePath = $"{folderPath}\\scroll_airdrop_status.xml";

            var data = await _fileMapperHelper.ReadFromXMLFileAsync<ScrollAirdropStatusDataResults>(filePath);

            if (data == null || data.DayList == null || !data.DayList.Any())
            {
                return new ScrollAirdropStatusDataResults();
            }

            var orderedDays = data.DayList.OrderBy(day => DateTime.ParseExact(day.Date, "yyyy-MM-dd HH:mm:ss.fff UTC", CultureInfo.InvariantCulture)).ToList();

            var firstRecord = orderedDays.First();
            var latestRecord = orderedDays.Last();

            DateTime firstDate = DateTime.ParseExact(firstRecord.Date, "yyyy-MM-dd HH:mm:ss.fff UTC", CultureInfo.InvariantCulture);
            DateTime newDate = firstDate.AddDays(-7);

            var previousWeekEntry = new Day
            {
                Date = newDate.ToString("yyyy-MM-dd HH:mm:ss.fff UTC"),
                Amount = 0,
                Users = 0,
                LastUpdate = null
            };

            data.DayList.Insert(0, previousWeekEntry);

            if (latestRecord != null)
            {
                data.TotalAmount = _addressListConverter.FormatNumber(latestRecord.Amount);
                data.TotalUsers = _addressListConverter.FormatNumber(latestRecord.Users);
                data.LastUpdate = latestRecord.LastUpdate;

                data.AmountPercent = (double)Math.Ceiling(55000000 / latestRecord.Amount);
                data.UsersPercent = 571564.0 / latestRecord.Users;
            }

            _cache.Set(cacheKey, data, TimeSpan.FromHours(24));

            return data;
        }

    }
}