import pandas as pd
from bulk_transfers_sybil_detector import SybilDetector
from bulk_transfers.extract_data_bulk_transfers import generate_clusters
from typing import List, Dict


def calculate_bulk_transfers_risk_score(input_data: list[Dict]) -> List[Dict] :
    detector = SybilDetector(
        amount_similarity_threshold=0.15,
        min_batch_size=5,
        batch_time_window_minutes=30
    )
    return detector.analyze_clusters(input_data)


path = "../Data/Scroll Sybil/Scroll Airdrop Sybil Detection - Bulk Transfers.csv"
clusters: List[Dict] = generate_clusters(file_path=path, cluster_id_column="CLUSTER_ID",
                                         cluster_size_column="CLUSTER_SIZE", clusters_head_column="SOURCE",
                                         addresses_column="CLUSTER_ADDRESS_LIST", amounts_column="CLUSTER_AMOUNT_LIST",
                                         first_timestamp_column="CLUSTER_TIME_LIST")

clusters_risk_score_bulk_transfers: pd.DataFrame = pd.DataFrame.from_dict(
    calculate_bulk_transfers_risk_score(input_data=clusters))

bulk_transfer_analyse_results: pd.DataFrame = \
    pd.merge(pd.DataFrame.from_dict(clusters), clusters_risk_score_bulk_transfers, on='cluster_id', how='inner')

bulk_transfer_analyse_results = \
    bulk_transfer_analyse_results[['cluster_id', 'cluster_head_x', 'cluster_size_x', 'addresses',
                                   'temporal_patterns_risk_score', 'similar_amounts_risk_score',
                                   'total_risk_score', 'risk_factors']]

bulk_transfer_analyse_results.rename(columns={'cluster_head_x' : 'cluster_head', 'cluster_size_x' : 'cluster_size'},
                                     inplace=True)

results_path = "results/bulk_transfer_analyse_results.csv"
bulk_transfer_analyse_results.to_csv("results/bulk_transfer_analyse_results.csv")
print(f"Bulk transfers results written in\n{results_path}")
