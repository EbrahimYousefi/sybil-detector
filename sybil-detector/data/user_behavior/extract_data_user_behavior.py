import re
from datetime import datetime
import pandas as pd
import pickle
from typing import List, Tuple


def extract_transaction_date_time (df: pd.DataFrame, column_name: str = "ALL_TRANSACTIONS") -> List[List[datetime]] :
    pattern = re.compile(r'Time: (\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.000 UTC)')

    def extract_all_timestamps (text) :
        try :
            matches = re.findall(pattern, str(text))
            if matches :
                return [datetime.strptime(ts.replace('.000 UTC', ''), '%Y-%m-%d %H:%M:%S') for ts in matches]
            return []
        except :
            print("Error occurred while converting strings to timestamps\nCheck code and data")
            return []

    transactions_timestamps = list(df[column_name].apply(extract_all_timestamps))
    transactions_timestamps_file_path: str = "./saved_columns/transactions_timestamps.pkl"
    with open(transactions_timestamps_file_path, 'wb') as f :
        pickle.dump(transactions_timestamps, f)
    print(f"Transactions timestamps file successfully written in {transactions_timestamps_file_path}")
    return transactions_timestamps


def extract_transaction_data (df: pd.DataFrame, column_name: str = "ALL_TRANSACTIONS") \
        -> Tuple[List[List[str]], List[List[float]], List[List[str]]] :

    def process_row(row):
        if '[No Transaction]' in row:
            return '', '', ''

        transactions = re.split(r'\s+Time:', row)

        transactions[0] = transactions[0].lstrip('[')

        transactions = [transactions[0]] + [f'Time:{t}' for t in transactions[1 :]]

        target_addresses = []
        methods = []
        values = []
        address_pattern = re.compile(r'Target Address: ([^|]+)')
        method_pattern = re.compile(r'Method: ([^|\s\]]+)')
        value_pattern = re.compile(r'Value: ([^|]+)')

        for transaction in transactions :

            addr_match = re.search(address_pattern, transaction)
            method_match = re.search(method_pattern, transaction)
            value_match = re.search(value_pattern, transaction)

            if addr_match :
                target_addresses.append(addr_match.group(1).strip())
            if method_match :
                methods.append(method_match.group(1).strip())
            if value_match :
                try :
                    value_number = float(value_match.group(1).strip())
                    values.append(value_number)

                except ValueError:
                    print(f"Error while casting {value_match.group(1).strip()}\n"
                          f"It will replaced by 0.00\nCheck code and data")
                    values.append(0.0) 

        addresses_str = target_addresses if target_addresses else ''
        methods_str = methods if methods else ''
        values_float = values if values else 0.0

        return addresses_str, methods_str, values_float

    results = [process_row(row) for row in df[column_name].str.replace('\n', '')]

    address, method, value = zip(*results)
    transactions_target_addresses = list(map(lambda x: [x], list(address)))
    transactions_target_addresses_path: str = "./saved_columns/transactions_target_addresses.pkl"
    with open(transactions_target_addresses_path, 'wb') as f :
        pickle.dump(transactions_target_addresses, f)
    print(f"Transactions target addresses file successfully written in {transactions_target_addresses_path}")

    transactions_values = list(map(lambda x: [x], list(value)))
    transactions_values_path: str = "./saved_columns/transactions_values.pkl"
    with open(transactions_values_path, 'wb') as f :
        pickle.dump(transactions_values, f)
    print(f"Transactions values file successfully written in {transactions_values_path}")

    transactions_methods = list(map(lambda x: [x], list(method)))
    transactions_methods_path: str = "./saved_columns/transactions_methods.pkl"
    with open(transactions_methods_path, 'wb') as f :
        pickle.dump(transactions_methods, f)
    print(f"Transactions methods file successfully written in {transactions_methods_path}")

    return transactions_target_addresses, transactions_values, transactions_methods


def extract_address (df: pd.DataFrame, column_name: str = "ADDRESS") -> List[List[str]] :
    addresses_raw: pd.Series = df[column_name].str[0:]
    fixed_addresses: pd.Series = addresses_raw.str.split()
    addresses = list(fixed_addresses.apply(lambda lst : [str(x) for x in lst]))
    addresses_path: str = "./saved_columns/addresses.pkl"
    with open(addresses_path, 'wb') as f :
        pickle.dump(addresses, f)
    print(f"Addresses file successfully written in {addresses_path}")
    return addresses


def extract_cluster_id (df: pd.DataFrame, column_name: str = "CLUSTER_IDS") -> List[List[int]] :
    cluster_id_raw: pd.Series = df[column_name].str[1 :-1]
    fixed_cluster_id = cluster_id_raw.str.split()
    cluster_id = list(fixed_cluster_id.apply(lambda lst: list(map(int, lst))))
    cluster_ids_path: str = "./saved_columns/cluster_ids.pkl"
    with open(cluster_ids_path, 'wb') as f :
        pickle.dump(cluster_id, f)
    print(f"Cluster ids file successfully written in {cluster_ids_path}")
    return cluster_id


def run_extractor(raw_data: pd.DataFrame):
    addresses: List[List[str]] = extract_address(df=raw_data)
    timestamps = extract_transaction_date_time(df=raw_data)
    cluster_ids = extract_cluster_id(df=raw_data)
    transactions_data = extract_transaction_data(df=raw_data)
    target_addresses: List[List[str]] = transactions_data[0]
    transactions_values: List[List[float]] = transactions_data[1]
    transactions_methods: List[List[str]] = transactions_data[2]


def generate_formatted_data():
    with open("./saved_columns/addresses.pkl", 'rb') as file :
        addresses = pickle.load(file)

    with open("./saved_columns/cluster_ids.pkl", 'rb') as file:
        cluster_ids = pickle.load(file)

    with open("./saved_columns/transactions_methods.pkl", 'rb') as file:
        transactions_methods = pickle.load(file)

    with open("./saved_columns/transactions_target_addresses.pkl",
              'rb') as file:
        transactions_target_addresses = pickle.load(file)

    with open("./saved_columns/transactions_timestamps.pkl", 'rb') as file:
        transactions_timestamps = pickle.load(file)

    with open("./saved_columns/transactions_values.pkl", 'rb') as file :
        transactions_values = pickle.load(file)

    result = [{"address": add, "cluster_id": c_id, "transaction_timestamp": tx_ts,
               "transaction_method": tx_met, "transactions_target_address": tx_tg,
               "transactions_value": tx_value} for add, c_id, tx_ts, tx_met, tx_tg, tx_value in
              zip(addresses, cluster_ids, transactions_timestamps, transactions_methods,
                  transactions_target_addresses, transactions_values)]
    result.reverse()

    formatted_data_path: str = "./saved_columns/formatted_data.pkl"
    with open(formatted_data_path, 'wb') as f:
        pickle.dump(result, f)
    print(f"Formatted data file successfully written in {formatted_data_path}")
    return result