using SybilDetection.UI.ViewModels.API;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SybilDetection.UI.Helper.APIHelper.ENS
{
    public class ENSServices : IENSServices
    {
        public async Task<string> GetAddressByDomain(string domain)
        {
            var address = string.Empty;

            if (string.IsNullOrEmpty(domain)) return address;

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://gateway.thegraph.com/api/aa5a4aa1e6e9fa8c4ee4c3949d341574/subgraphs/id/5XqPmWe6gjyrJtFn9cLy237i4cWw2j9HcUJEXsP5qGtH");

                var query = $@"{{""query"": ""query getENSResolvedAddress {{ domains(where: {{name: \""{domain}\""}}) {{ name resolvedAddress {{ id }} expiryDate }} }} "", ""operationName"": ""domains""}}";

                var content = new StringContent(query, System.Text.Encoding.UTF8, "application/json");

                request.Content = content;
                var response = await client.SendAsync(request);

                if (response.EnsureSuccessStatusCode().StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var jsonResult = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var finalResultsList = JsonSerializer.Deserialize<DomainResponse>(jsonResult, options);

                    if (finalResultsList.Data.Domains.Any())
                    {
                        var finalResults = finalResultsList.Data.Domains.FirstOrDefault().ResolvedAddress.Id.ToLower().ToString();

                        if (!string.IsNullOrEmpty(finalResults))
                            address = finalResults;
                    }
                }

                return address;

            }
            catch { return address; }
        }
    }
}