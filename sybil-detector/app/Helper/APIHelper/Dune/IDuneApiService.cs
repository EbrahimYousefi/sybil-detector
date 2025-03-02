using System.Threading.Tasks;

namespace SybilDetection.UI.Helper.APIHelper.Dune
{
    public interface IDuneApiService
    {
        public Task FetchAndSaveScrollAirdropStatusDataAsync();
    }
}
