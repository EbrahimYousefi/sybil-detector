using System.Collections.Generic;

namespace SybilDetection.UI.ViewModels.API
{
    public class DomainResponse
    {
        public DataModel Data { get; set; }
    }

    public class DataModel
    {
        public List<DomainModel> Domains { get; set; }
    }

    public class DomainModel
    {
        public string ExpiryDate { get; set; }
        public string Name { get; set; }
        public ResolvedAddress ResolvedAddress { get; set; }
    }

    public class ResolvedAddress
    {
        public string Id { get; set; }
    }

    public class WalletRequest
    {
        public string Address { get; set; }
    }
}