using System.Threading.Tasks;

namespace SybilDetection.UI.Helper.APIHelper.ENS
{
    public interface IENSServices
    {
        public Task<string> GetAddressByDomain(string domain);
    }
}