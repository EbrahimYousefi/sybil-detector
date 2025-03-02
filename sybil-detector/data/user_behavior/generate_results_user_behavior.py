import pandas as pd
from typing import Dict, List
from extract_data_user_behavior import run_extractor, generate_formatted_data
from user_behavior_sybil_detector import IntraClusterAnalyzer
import pickle

read_file = pd.read_csv("../Data/Scroll Sybil/Scroll Airdrop Sybil Detection - User Behavior.csv")
run_extractor(raw_data=read_file)
data = generate_formatted_data()

analyzer = IntraClusterAnalyzer(
    value_similarity_threshold=0.15, 
    method_similarity_threshold=0.7,
    target_similarity_threshold=0.6,
    temporal_window_minutes=10.0,
    min_sequence_length=5
)

user_behavior_results = analyzer.analyze_all_clusters(raw_data=data)
results_path: str = "./results/user_behavior_analyse_results.csv"
results = pd.DataFrame(user_behavior_results)
results.to_csv(results_path)