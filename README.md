# Sybil Detector on Scroll Blockchain
This project detects sybil addresses on the Scroll blockchain by analyzing on-chain transaction data. It uses a two-step approach:

Bulk Transfers:
Identify clusters of addresses that receive funds from the same source by analyzing transaction timing and amounts.

User Behavior Analysis:
Examine transaction methods, values, target addresses, and timing within clusters to detect coordinated or automated activity.

The final Sybil Score is computed by weighting the Bulk Transfers score (30%) and the User Behavior score (70%), classifying addresses as No-Risk, Low-Risk, Moderate-Risk, or High-Risk.

Explore and use our live app at https://sybildetector.com/.

