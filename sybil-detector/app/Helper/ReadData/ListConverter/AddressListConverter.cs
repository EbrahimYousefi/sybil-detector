using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SybilDetection.UI.Helper.ReadData.ListConverter
{
    public class AddressListConverter : IAddressListConverter
    {
        public List<string> ConvertFromString(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            return text
                .Trim('[', ']') 
                .Replace("'", "") 
                .Split(',', StringSplitOptions.RemoveEmptyEntries) 
                .Select(addr => addr.Trim())
                .ToList();
        }

        public string ConvertToString(object value)
        {
            if (value is List<string> addresses)
            {
                return $"[{string.Join(", ", addresses.Select(addr => $"'{addr}'"))}]";
            }
            return "[]";
        }

        public string FixAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            return address
                .Trim('[', ']')
                .Replace("'", "").Trim().ToString();
        }

        public string FormatNumber(decimal num)
        {
            if (num < 1000)
            {
                return num.ToString();
            }
            else if (num < 1000000)
            {
                return (num / 1000).ToString("0.00") + "K";
            }
            else if (num < 1000000000)
            {
                return (num / 1000000).ToString("0.00") + "M";
            }
            else
            {
                return (num / 1000000000).ToString("0.00") + "B";
            }
        }
    }
}