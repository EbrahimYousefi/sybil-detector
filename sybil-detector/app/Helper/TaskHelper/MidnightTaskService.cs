using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SybilDetection.UI.Helper.APIHelper.Dune;
using SybilDetection.UI.Helper.ReadData.FileMapper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SybilDetection.UI.Helper.TaskHelper
{
    public class MidnightTaskService : BackgroundService
    {
        private readonly ILogger<MidnightTaskService> _logger;
        private readonly IDuneApiService _duneApiService;

        public MidnightTaskService(
            ILogger<MidnightTaskService> logger,
            IDuneApiService duneApiService)
        {
            _logger = logger;
            _duneApiService = duneApiService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;
                DateTime nextMidnight = now.Date.AddDays(1);

                TimeSpan waitTime = nextMidnight - now;
                _logger.LogInformation($"Waiting {waitTime.TotalSeconds} seconds until midnight...");

                await Task.Delay(waitTime, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        RunDuneApiTask();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error executing midnight task: {ex.Message}");
                    }
                }
            }
        }

        private void RunDuneApiTask()
        {
            _logger.LogInformation("Midnight task executed at: " + DateTime.Now);

            _duneApiService.FetchAndSaveScrollAirdropStatusDataAsync();
        }
    }
}