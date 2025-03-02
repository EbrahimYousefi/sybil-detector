using SybilDetection.UI.ViewModels.Helper.CsvReader;
using System.Collections.Generic;

namespace SybilDetection.UI.Helper.ReadData.CsvReaders
{
    public interface ICsvReaderHelper
    {
        public IEnumerable<CsvReaderViewModels> ReadCsvFileV1(string filePath);
        public IEnumerable<CsvReaderViewModels> ReadCsvFileV2(string filePath);
        public IEnumerable<CsvReaderForScrollClaimerViewModels> ReadCsvFileV3(string filePath);
    }
}