using System.Collections.Generic;
using System.Xml.Serialization;

namespace SybilDetection.UI.ViewModels.API
{
    public class ScrollAirdropStatusDuneApiResponse
    {
        public ScrollAirdropStatusResultSection result { get; set; }
    }

    public class ScrollAirdropStatusResultSection
    {
        public List<ScrollAirdropStatusDataRow> rows { get; set; }
    }

    public class ScrollAirdropStatusDataRow
    {
        public string DATE { get; set; }
        public double TOTAL_CLAIM_AMOUNTS { get; set; }
        public int TOTAL_NUMBER_OF_WALLETS { get; set; }
    }

    [XmlRoot("dates")]
    public class ScrollAirdropStatusDataResults
    {
        [XmlElement("day")]
        public List<Day> DayList { get; set; }
        public string TotalUsers { get; set; }
        public string TotalAmount { get; set; }
        public string LastUpdate { get; set; }
        public double AmountPercent { get; set; }
        public double UsersPercent { get; set; }
    }

    public class Day
    {
        [XmlElement("date")]
        public string Date { get; set; } 

        [XmlElement("amount")]
        public decimal Amount { get; set; }

        [XmlElement("users")]
        public int Users { get; set; }

        [XmlElement("last_update")]
        public string LastUpdate { get; set; }
    }
}