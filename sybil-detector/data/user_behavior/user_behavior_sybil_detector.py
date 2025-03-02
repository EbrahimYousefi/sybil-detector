from datetime import datetime
from typing import List, Dict, Any
from collections import defaultdict
from dataclasses import dataclass
import math


@dataclass
class ClusterTransaction :
    address: str
    timestamp: datetime
    method: str
    value: float
    target_address: str


class IntraClusterAnalyzer :
    def __init__ (self,
                  method_similarity_threshold: float = 0.7,
                  value_similarity_threshold: float = 0.15,
                  target_similarity_threshold: float = 0.6,
                  temporal_window_minutes: float = 5.0,
                  min_sequence_length: int = 3):
        self.method_similarity_threshold = method_similarity_threshold
        self.value_similarity_threshold = value_similarity_threshold
        self.target_similarity_threshold = target_similarity_threshold
        self.temporal_window_minutes = temporal_window_minutes
        self.min_sequence_length = min_sequence_length

    def _process_raw_data (self, raw_data: List[Dict]) -> Dict[int, List[ClusterTransaction]] :
        cluster_transactions = defaultdict(list)

        for entry in raw_data :
            address = entry['address'][0]

            for i in range(len(entry['transaction_timestamp'])) :
                transaction = ClusterTransaction(
                    address=address,
                    timestamp=entry['transaction_timestamp'][i],
                    method=entry['transaction_method'][0][i],
                    value=entry['transactions_value'][0][i],
                    target_address=entry['transactions_target_address'][0][i]
                )

                for cluster_id in entry['cluster_id'] :
                    cluster_transactions[cluster_id].append(transaction)

        return cluster_transactions

    def analyze_method_patterns (self, transactions: List[ClusterTransaction]) -> Dict[str, Any] :
        method_counts = defaultdict(int)
        unique_addresses = set()

        for tx in transactions :
            method_counts[tx.method] += 1
            unique_addresses.add(tx.address)

        most_common_method_count = max(method_counts.values())

        method_usage_ratio = min(1.0,
                                 math.log(most_common_method_count + 1) /
                                 math.log(len(transactions) + 1)
                                 )
        risk_score = round((method_usage_ratio * 100), 2)
        most_common_method = f"{risk_score}% of transactions in this cluster used {max(method_counts, key=method_counts.get)} method"
        return {
            "risk_score" : (risk_score if risk_score >= (self.method_similarity_threshold * 100) else 0),
            "most_common_method" : most_common_method,
        }

    def _are_values_similar (self, val1: float, val2: float) -> bool :
        if val1 == 0 and val2 == 0 :
            return True
        if val1 == 0 or val2 == 0 :
            return False
        return abs(val1 - val2) / max(val1, val2) <= self.value_similarity_threshold

    def analyze_value_patterns (self, transactions: List[ClusterTransaction]) -> Dict[str, Any] :
        non_zero_transactions = [tx for tx in transactions if tx.value > 0]
        zero_transactions = [tx for tx in transactions if tx.value == 0]

        if not non_zero_transactions :
            return {
                "risk_score" : 0,
                "largest_similar_value_group" : 0,
                "avg_unique_values_per_address" : 0,
                "zero_value_ratio" : 1.0,
            }

        value_groups = defaultdict(list)
        address_value_patterns = defaultdict(set)

        for tx in non_zero_transactions :
            found_group = False
            for base_value in value_groups :
                if self._are_values_similar(tx.value, base_value) :
                    value_groups[base_value].append(tx)
                    address_value_patterns[tx.address].add(base_value)
                    found_group = True
                    break
            if not found_group :
                value_groups[tx.value].append(tx)
                address_value_patterns[tx.address].add(tx.value)

        largest_value_group = max(value_groups.values(), key=len)
        total_addresses = len(set(tx.address for tx in non_zero_transactions))
        addresses_with_similar_values = len(set(tx.address for tx in largest_value_group))

        value_similarity_ratio = addresses_with_similar_values / total_addresses
        risk_score = round((value_similarity_ratio * 100), 2)
        zero_value_ratio = round((len(zero_transactions) / len(transactions)), 4)

        avg_unique_values = sum(len(values) for values in address_value_patterns.values()) / len(address_value_patterns)
        representative_value = sum(tx.value for tx in largest_value_group) / len(largest_value_group)

        return {
            "risk_score" : (risk_score if risk_score >= 30 else 0),
            "largest_similar_value_group" : len(largest_value_group),
            "zero_value_ratio" : zero_value_ratio,
        }

    def analyze_target_patterns (self, transactions: List[ClusterTransaction]) -> Dict[str, Any] :
        address_targets = defaultdict(set)
        known_transactions = [tx for tx in transactions if tx.target_address.lower() != "unknown"]

        if not known_transactions :
            return {
                "risk_score" : 0,
                "shared_targets" : 0
            }

        for tx in known_transactions :
            address_targets[tx.address].add(tx.target_address)

        total_known_targets = len(set(tx.target_address for tx in known_transactions))

        addresses_with_known_targets = set(address_targets.keys())

        if not addresses_with_known_targets :
            return {
                "risk_score" : 0,
                "shared_targets" : 0,
            }

        shared_targets = set.intersection(
            *[targets for targets in address_targets.values()]) if address_targets else set()

        shared_target_ratio = len(shared_targets) / total_known_targets if total_known_targets > 0 else 0

        risk_score = round((shared_target_ratio * 100), 2)

        return {
            "risk_score" : (risk_score if risk_score >= (self.target_similarity_threshold * 100) else 0),
            "shared_targets" : len(shared_targets)
        }

    def analyze_temporal_patterns (self, transactions: List[ClusterTransaction]) -> Dict[str, Any] :
        if not transactions :
            return {
                "sequential_groups" : 0,
                "largest_sequence" : 0,
                "avg_time_gap" : 0,
                "risk_score" : 0,
                "sequences" : []
            }

        sorted_txs = sorted(transactions, key=lambda x : x.timestamp)

        sequences = []
        current_sequence = [sorted_txs[0]]

        for i in range(1, len(sorted_txs)) :
            time_diff = (sorted_txs[i].timestamp - current_sequence[-1].timestamp).total_seconds() / 60

            if time_diff <= self.temporal_window_minutes :
                current_sequence.append(sorted_txs[i])
            else :
                if len(current_sequence) >= self.min_sequence_length :
                    sequences.append(current_sequence)
                current_sequence = [sorted_txs[i]]

        if len(current_sequence) >= self.min_sequence_length :
            sequences.append(current_sequence)

        if sequences :
            largest_sequence = max(len(seq) for seq in sequences)

            total_gaps = []
            for sequence in sequences :
                for i in range(1, len(sequence)) :
                    gap = (sequence[i].timestamp - sequence[i - 1].timestamp).total_seconds() / 60
                    total_gaps.append(gap)

            avg_time_gap = sum(total_gaps) / len(total_gaps) if total_gaps else 0

            sequence_ratio = sum(len(seq) for seq in sequences) / len(transactions)
            max_sequence_ratio = largest_sequence / len(transactions)
            risk_score = ((sequence_ratio + max_sequence_ratio) / 2) * 100
        else :
            largest_sequence = 0
            avg_time_gap = 0
            risk_score = 0

        sequence_summaries = []
        for seq in sequences :
            sequence_summaries.append({
                "start_time" : seq[0].timestamp,
                "end_time" : seq[-1].timestamp,
                "length" : len(seq),
                "addresses" : list(set(tx.address for tx in seq)),
                "avg_gap" : sum((seq[i].timestamp - seq[i - 1].timestamp).total_seconds() / 60
                                for i in range(1, len(seq))) / (len(seq) - 1) if len(seq) > 1 else 0
            })

        return {
            "risk_score" : round(risk_score, 2),
            "sequential_groups" : len(sequences),
            "largest_sequence" : largest_sequence,
            "avg_time_gap" : round(avg_time_gap, 2),
            "sequences" : sequence_summaries
        }

    def analyze_cluster (self, cluster_id: int, transactions: List[ClusterTransaction]) -> Dict[str, Any] :
        method_analysis = self.analyze_method_patterns(transactions)
        value_analysis = self.analyze_value_patterns(transactions)
        target_analysis = self.analyze_target_patterns(transactions)
        temporal_analysis = self.analyze_temporal_patterns(transactions)

        risk_score = (
                method_analysis["risk_score"] * 0.25 +  
                value_analysis["risk_score"] * 0.30 + 
                target_analysis["risk_score"] * 0.20 + 
                temporal_analysis["risk_score"] * 0.25
        )

        return {
            "cluster_id" : cluster_id,
            "addresses" : len(set(tx.address for tx in transactions)),
            "transactions" : len(transactions),
            "behavior_risk_score" : round(risk_score, 2),
            "method_similarity_score" : round(method_analysis["risk_score"] * 0.25, 2),
            "value_similarity_score" : round(value_analysis["risk_score"] * 0.30, 2),
            "target_similarity_score" : round(target_analysis["risk_score"] * 0.20, 2),
            "temporal_pattern_score" : round(temporal_analysis["risk_score"] * 0.25, 2),
            "details" : {
                "method_analysis" : method_analysis,
                "value_analysis" : value_analysis,
                "target_analysis" : target_analysis,
                "temporal_analysis" : temporal_analysis
            }
        }

    def analyze_all_clusters (self, raw_data: List[Dict]) -> List[Dict] :
        cluster_transactions = self._process_raw_data(raw_data)
        return [
            self.analyze_cluster(cluster_id, transactions)
            for cluster_id, transactions in cluster_transactions.items()
        ]