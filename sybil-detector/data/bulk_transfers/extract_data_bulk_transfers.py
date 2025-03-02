import pandas as pd
from datetime import datetime
from typing import List, Dict


def extract_cluster_id (data_frame: pd.DataFrame, column_name: str) -> List[int] :
    clusters_id = data_frame[column_name].to_list()
    return clusters_id


def extract_cluster_size (data_frame: pd.DataFrame, column_name: str) -> List[int] :
    clusters_size = data_frame[column_name].to_list()
    return clusters_size


def extract_received_amounts (data_frame: pd.DataFrame, column_name: str) -> List[List[float]] :
    amounts_raw: pd.Series = data_frame[column_name].str[1 : -1]
    fixed_amounts: pd.Series = amounts_raw.str.split()
    amounts: List = (fixed_amounts.apply(lambda lst : [float(x) for x in lst])).to_list()
    return amounts


def extract_clusters_head(data_frame: pd.DataFrame, column_name: str) -> List[List[str]] :
    clusters_heads_raw = data_frame[column_name]
    fixed_clusters_heads: pd.Series = clusters_heads_raw.str.split()
    clusters_head = (fixed_clusters_heads.apply(lambda lst : [str(x) for x in lst])).to_list()
    return clusters_head


def extract_first_transaction_timestamp_from_cluster_head (
        data_frame: pd.DataFrame, column_name: str) -> List[List[datetime]] :
    first_transaction_timestamp_from_cluster_head_row: pd.Series = data_frame[column_name].str[1 : -1]
    split_transactions: pd.Series = first_transaction_timestamp_from_cluster_head_row.str.split(" UTC")
    remove_ms: pd.Series = split_transactions.apply(lambda sp : [s[:-4] for s in sp])
    remove_useless_elements: pd.Series = remove_ms.apply(lambda rm : rm[:-1])
    remove_extra_spaces: pd.Series = remove_useless_elements.apply(lambda rue : [element.lstrip() for element in rue])
    first_transaction_timestamp_from_cluster_head = \
        (remove_extra_spaces.apply(lambda res : [datetime.strptime(dt, "%Y-%m-%d %H:%M:%S") for dt in res])).to_list()
    return first_transaction_timestamp_from_cluster_head


def extract_addresses(data_frame: pd.DataFrame, column_name: str) -> List[List[str]] :
    addresses_raw: pd.Series = data_frame[column_name].str[1 : -1]
    fixed_addresses: pd.Series = addresses_raw.str.split()
    addresses = fixed_addresses.to_list()
    return addresses


def generate_clusters (file_path: str, cluster_id_column: str, cluster_size_column: str,
                       clusters_head_column: str, addresses_column: str,
                       amounts_column: str, first_timestamp_column: str) -> List[Dict] :
    bulk_transfer_data = pd.read_csv(file_path)
    clusters_id = extract_cluster_id(data_frame=bulk_transfer_data, column_name=cluster_id_column)
    clusters_size = extract_cluster_size(data_frame=bulk_transfer_data, column_name=cluster_size_column)
    clusters_head = extract_clusters_head(data_frame=bulk_transfer_data, column_name=clusters_head_column)
    addresses = extract_addresses(data_frame=bulk_transfer_data, column_name=addresses_column)
    received_amounts = extract_received_amounts(data_frame=bulk_transfer_data, column_name=amounts_column)
    first_transaction_from_head_timestamp = extract_first_transaction_timestamp_from_cluster_head(
        data_frame=bulk_transfer_data, column_name=first_timestamp_column)

    result = [{"cluster_id" : c_id, "cluster_head" : c_h, "cluster_size" : c_s,
               "addresses" : add, "received_amount" : amt, "first_timestamp" : f_t}
              for c_id, c_h, c_s, add, amt, f_t in zip(clusters_id, clusters_head, clusters_size,
                                                       addresses, received_amounts,
                                                       first_transaction_from_head_timestamp)]
    return result


