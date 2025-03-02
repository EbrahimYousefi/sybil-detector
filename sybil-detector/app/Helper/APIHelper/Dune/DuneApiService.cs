using SybilDetection.UI.Helper.ReadData.FileMapper;
using SybilDetection.UI.ViewModels.API;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SybilDetection.UI.Helper.APIHelper.Dune
{
    public class DuneApiService : IDuneApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private IFileMapperHelper _fileMapperHelper;

        public DuneApiService(IFileMapperHelper fileMapperHelper)
        {
            _fileMapperHelper = fileMapperHelper;
        }

        public async Task FetchAndSaveScrollAirdropStatusDataAsync()
        {
            string apiUrl = "https://api.dune.com/api/v1/query/4760739/results?limit=1000";
            string apiKey = "";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Add("X-Dune-API-Key", apiKey);

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                var data = JsonSerializer.Deserialize<ScrollAirdropStatusDuneApiResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data?.result?.rows != null)

                    _fileMapperHelper.WriteScrollAirdropStatusToXMLFile(data);

                else
                    return;
            }
            catch
            {
                return;
            }
        }
    }
}
