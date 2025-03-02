using CsvHelper;
using CsvHelper.Configuration;
using SybilDetection.UI.ViewModels.Helper.CsvReader;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SybilDetection.UI.Helper.ReadData.CsvReaders
{
    public class CsvReaderHelper : ICsvReaderHelper
    {
        public IEnumerable<CsvReaderViewModels> ReadCsvFileV1(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });

            return csv.GetRecords<CsvReaderViewModels>().Distinct().ToList();
        }

        public IEnumerable<CsvReaderViewModels> ReadCsvFileV2(string filePath)
        {
            using (var reader = new StreamReader(filePath))

            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                var records = csv.GetRecords<CsvReaderViewModels>().ToList();

                var uniqueRecords = records.GroupBy(r => new { r.cluster_id, r.cluster_head, r.cluster_size_bt, r.addresses, r.temporal_patterns_risk_score, r.similar_amounts_risk_score, r.bulk_transfers_risk_score, r.risk_factors, r.cluster_size_ub, r.transactions, r.behavior_risk_score, r.method_similarity_score, r.value_similarity_score, r.target_similarity_score, r.temporal_pattern_score, r.details }).Select(g => g.First()).ToList();

                return uniqueRecords;
            }
        }

        public IEnumerable<CsvReaderForScrollClaimerViewModels> ReadCsvFileV3(string filePath)
        {
            using (var reader = new StreamReader(filePath))

            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                var records = csv.GetRecords<CsvReaderForScrollClaimerViewModels>().ToList();

                var uniqueRecords = records.GroupBy(r => new { r.ADDRESS, r.TOTAL_CLAIM_AMOUNTS}).Select(g => g.First()).ToList();

                return uniqueRecords;
            }
        }
    }
}