using SybilDetection.UI.ViewModels.API;
using SybilDetection.UI.ViewModels.Helper.CsvReader;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SybilDetection.UI.Helper.ReadData.FileMapper
{
    public interface IFileMapperHelper
    {
        public void WriteToXMLFile();

        public T ReadFromXMLFile<T>(string filePath);

        public Task<T> ReadFromXMLFileAsync<T>(string filePath);

        public List<AddressClusterResult> GetTopClustersForAddresses(AddressWithClusterList addressList, ClusterViewModelsList clusterList);

        public void WriteScrollAirdropStatusToXMLFile(ScrollAirdropStatusDuneApiResponse scrollAirdropData);
    }
}