using System.Collections.Generic;

namespace SybilDetection.UI.Helper.ReadData.ListConverter
{
    public interface IAddressListConverter
    {
        public List<string> ConvertFromString(string text);
        public string ConvertToString(object value);
        public string FixAddress(string address);
        public string FormatNumber(decimal num);
    }
}