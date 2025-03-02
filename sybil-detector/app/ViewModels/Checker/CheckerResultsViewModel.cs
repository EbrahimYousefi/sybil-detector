using System.Collections.Generic;

namespace SybilDetection.UI.ViewModels.Checker
{
    public class CheckerResultsViewModel
    {
        public string address { get; set; }
        public SybilResultsViewModel sybil_results { get; set; }
        public ScrollTransactionResultsViewModel scroll_transaction_results { get; set; }
        public List<DailyScrollTransactionResultsViewModel> daily_scroll_transaction_results { get; set; }
        public ScrollAirdropEligibilityStatusResultsViewModel scroll_airdrop_eligibility_status_results { get; set; }
        public ScrollAirdropActivityResultsViewModel scroll_mark_activity_results { get; set; }
    }

    public class SybilResultsViewModel
    {
        public string cluster_id { get; set; }
        public string cluster_head { get; set; }
        public string cluster_size_bulk_transfers { get; set; }
        public string details { get; set; }
        public string total_sybil_score { get; set; }
        public string status { get; set; }
        public bool in_cluster { get; set; }
        public List<string> addresses { get; set; }
        public List<SybilReasonResults> sybil_reason_results { get; set; }
    }

    public class ScrollTransactionResultsViewModel
    {
        public int total_transaction { get; set; }
        public decimal total_volume { get; set; }
        public int unique_days { get; set; }
        public int unique_weeks { get; set; }
        public int unique_months { get; set; }
        public decimal ether_balance { get; set; }
        public int wallet_age { get; set; }
        public bool has_transaction { get; set; }
    }

    public class DailyScrollTransactionResultsViewModel
    {
        public string day { get; set; }
        public int total_transaction { get; set; }
        public decimal total_volume { get; set; }
    }

    public class ScrollAirdropEligibilityStatusResultsViewModel
    {
        public bool is_eligible { get; set; }
        public decimal amount { get; set; }
    }

    public class ScrollAirdropActivityResultsViewModel
    {
        public string season_1_mark { get; set; }
    }

    public class EtherBalanceScrollApiResponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public string result { get; set; }
    }

    public class ScrollApiTransactionResponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public List<ScrollTransactions> result { get; set; }
    }

    public class ScrollTransactions
    {
        public string blockNumber { get; set; }
        public string timeStamp { get; set; }
        public string hash { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string value { get; set; }
        public string gas { get; set; }
        public string gasPrice { get; set; }
    }

    public class ScrollApiMarksResponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public ScrollMarksResult result { get; set; }
    }

    public class ScrollMarksResult
    {
        public string marks { get; set; }
        public string updateAt { get; set; }
    }

    public class SybilReasonResults
    {
        public string reason { get; set; }
        public string description { get; set; }
    }
}