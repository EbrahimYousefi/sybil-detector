from datetime import datetime
from typing import List, Dict, Any
from collections import defaultdict


class SybilDetector :
    def __init__ (self,
                  amount_similarity_threshold: float = 0.15,
                  min_batch_size: int = 5,
                  batch_time_window_minutes: int = 30) :
        self.amount_similarity_threshold = amount_similarity_threshold
        self.min_batch_size = min_batch_size
        self.batch_time_window_minutes = batch_time_window_minutes

    def analyze_temporal_patterns (self, timestamps: List[datetime]) -> Dict[str, Any] :
        if not timestamps :
            return {"batches" : 0, "largest_batch" : 0}

        sorted_times = sorted(timestamps)

        batches = []
        current_batch = [sorted_times[0]]

        for i in range(1, len(sorted_times)) :
            time_diff = (sorted_times[i] - current_batch[-1]).total_seconds() / 60

            if time_diff <= self.batch_time_window_minutes :
                current_batch.append(sorted_times[i])
            else :
                if len(current_batch) >= self.min_batch_size :
                    batches.append(current_batch)
                current_batch = [sorted_times[i]]

        if len(current_batch) >= self.min_batch_size :
            batches.append(current_batch)

        return {
            "batches" : len(batches),
            "largest_batch" : max([len(batch) for batch in batches]) if batches else 0
        }

    def analyze_amount_patterns (self, amounts: List[float]) -> Dict[str, Any] :
        if not amounts :
            return {"similar_amounts" : 0, "unique_amounts" : 0}

        amount_groups = defaultdict(list)
        sorted_amounts = sorted(amounts)

        for amount in sorted_amounts :
            found_group = False
            for base_amount in amount_groups:
                if abs(amount - base_amount) / max(base_amount, amount) <= self.amount_similarity_threshold :
                    amount_groups[base_amount].append(amount)
                    found_group = True
                    break
            if not found_group :
                amount_groups[amount].append(amount)

        return {
            "similar_amounts" : max(len(group) for group in amount_groups.values()),
            "unique_amounts" : len(amount_groups)
        }

    def detect_sybil_patterns (self, cluster: Dict) -> Dict[str, Any] :
        temporal_patterns = self.analyze_temporal_patterns(cluster['first_timestamp'])
        amount_patterns = self.analyze_amount_patterns(cluster['received_amount'])

        risk_factors = []
        risk_factors_description: str = ''
        risk_score = 0
        temporal_patterns_risk_score = 0
        similar_amounts_risk_score = 0

        if temporal_patterns["batches"] > 0 :
            temporal_patterns_risk_score += 2
            temporal_patterns_batches_description: str = f"Found {temporal_patterns['batches']} transaction batches|"
            risk_factors_description += temporal_patterns_batches_description

            if temporal_patterns["largest_batch"] >= self.min_batch_size : 
                temporal_patterns_batch_size_risk = temporal_patterns["largest_batch"] / cluster['cluster_size']
                temporal_patterns_risk_score += round(temporal_patterns_batch_size_risk, 2)
                temporal_patterns_largest_batch_description: str = f"Largest batch contains {temporal_patterns['largest_batch']} transactions|"
                risk_factors_description += temporal_patterns_largest_batch_description

        if amount_patterns["similar_amounts"] >= self.min_batch_size :
            similar_amounts_risk_score += 2
            amount_patterns_similar_amounts_description = f"Found {amount_patterns['similar_amounts']} similar amount transactions"
            risk_factors_description += amount_patterns_similar_amounts_description

        unique_amount_ratio = amount_patterns["unique_amounts"] / cluster['cluster_size']
        unique_amount_risk: float = 1 if round(1 - unique_amount_ratio, 2) >= 0.8 else round(1 - unique_amount_ratio, 2)
        similar_amounts_risk_score += unique_amount_risk

        if unique_amount_ratio < 0.5 :
            amount_patterns_unique_amounts_description = f"Low variety in transaction amounts: {unique_amount_ratio:.2%} unique|"
            risk_factors_description += amount_patterns_unique_amounts_description

        risk_score = round(temporal_patterns_risk_score + similar_amounts_risk_score, 2)
        risk_factors.append(risk_factors_description)

        return {
            "cluster_id": cluster['cluster_id'],
            "cluster_head": cluster['cluster_head'],
            "cluster_size": cluster['cluster_size'],
            "temporal_patterns_risk_score": temporal_patterns_risk_score,
            "similar_amounts_risk_score": similar_amounts_risk_score,
            "total_risk_score": risk_score,
            "risk_factors": risk_factors,
            "is_suspicious": risk_score >= 3,
        }

    def analyze_clusters (self, clusters: List[Dict]) -> List[Dict] :
        return [self.detect_sybil_patterns(cluster) for cluster in clusters]

    def get_detailed_report (self, clusters: List[Dict]) -> Dict[str, Any] :
        analyses = self.analyze_clusters(clusters)
        suspicious_clusters = [a for a in analyses if a['is_suspicious']]

        return {
            'total_clusters' : len(clusters),
            'suspicious_clusters' : len(suspicious_clusters),
            'suspicious_percentage' : (len(suspicious_clusters) / len(clusters)) * 100 if clusters else 0,
            'high_risk_clusters' : [a for a in analyses if a['total_risk_score'] > 4],
            'detailed_results' : analyses
        }