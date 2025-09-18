# Monitor Strategy

## Overview

The strategy replicates the behaviour of the original MetaTrader Monitor expert by sharing best bid quotes and terminal metadata through a Windows memory-mapped file. Every running instance writes its own terminal information to the shared segment and simultaneously reads the data published by other terminals. The collected snapshot is written to the strategy log, which allows building a lightweight status dashboard without any direct network communication between terminals.

## Core Workflow

1. Subscribe to level 1 data to receive best bid updates for the configured security.
2. Open or create the memory-mapped file `Monitor_<symbol>` located under the configurable directory (defaults to `Local`).
3. Write the current terminal record (timestamp, account identifier, scaled bid, platform code and workstation name) to the slot reserved for the instance.
4. Read all available records, filter out stale entries using the configurable latency threshold, and print an aggregated status table to the log.
5. Continue synchronisation at the configured refresh interval until the strategy stops, then decrease the global usage counter and close the mapping.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| **Directory** | Folder where the shared memory file is created or opened. | `Local` |
| **File Prefix** | File name prefix used before the truncated symbol code. | `Monitor_` |
| **Refresh Interval** | Delay between synchronisation iterations. | `500 ms` |
| **Max Latency** | Maximum acceptable age of records before they are ignored. | `5000 ms` |

## Behaviour Details

- The file header mirrors the original MQL layout: the first integer stores the number of attached terminals, the second tracks the highest allocated slot. Each slot contains four integers (timestamp, account, scaled bid and platform) followed by a short UTF-8 encoded terminal name.
- Prices are stored with a scale of `0.000001`, matching the original `kprice` constant.
- Active terminals are determined by comparing the record timestamp with the current environment tick count and the latency threshold.
- All operations are guarded with a mutex to protect the shared memory block from concurrent writers.
- The log output matches the MetaTrader expert format: `{latency} ({timestamp}) | {account} | {price} | {terminal} | MT {platform}`.

## Usage Notes

1. Deploy the strategy on every terminal that needs to participate in the monitor network.
2. Ensure all participants share the same working directory for the memory-mapped file.
3. The log can be redirected to files or external dashboards using the standard StockSharp logging infrastructure.
4. No trading orders are placed; the strategy serves purely as a monitoring utility.
