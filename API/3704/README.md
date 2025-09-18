# Pull All Ticks Strategy

## Overview

The **Pull All Ticks Strategy** is a StockSharp port of the MetaTrader script `pull all ticks.mq5` (ID 56324). The original tool scans the broker for the oldest available tick data and downloads the entire history in repeated batches while saving its progress to disk. This C# strategy reproduces the same workflow with StockSharp's high-level market data subscriptions. It continuously receives ticks, keeps track of the earliest and latest timestamps, and persists progress information in a status file so the download can resume after restarts.

Unlike trading strategies, this component focuses purely on data collection. It subscribes to the tick stream of the configured `Security`, writes progress updates to disk, and stops automatically when the configured lower date limit is reached.

## Behaviour

1. When the strategy starts it computes the path to the status file using the `ManagerFolder` and `StatusFileName` parameters.
2. If an existing progress file is found, the last processed timestamps, tick counter, and packet counter are restored.
3. The strategy subscribes to tick trades using `SubscribeTrades().Bind(ProcessTrade).Start()` so it receives every tick in chronological order.
4. Each tick updates the stored oldest and latest timestamps, increments the tick counter, and triggers a status save every time a full packet (defined by `TickPacketSize`) has been processed.
5. Progress is written to disk in a human-readable key-value format and also logged via `LogInfo` to mimic the MetaTrader `Comment` output.
6. When the oldest tick processed crosses the configured `OldestLimit` (and `LimitDate` is enabled), the strategy stops automatically, mirroring the MetaTrader script that stops once the historical boundary is reached.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `LimitDate` | Enables or disables stopping when the configured oldest date is reached. Mirrors the `limitDate` input of the MQL script. |
| `OldestLimit` | The lower time boundary for historical scanning. When the oldest tick falls below this timestamp the strategy stops. |
| `TickPacketSize` | Number of ticks processed before the strategy persists its state. Equivalent to `tick_packets` in the original script. |
| `RequestDelay` | Minimum delay between two status updates. It replaces the 44 ms sleep found in the MQL code to avoid excessive disk operations. |
| `ManagerFolder` | Folder where the progress file is saved. Matches `MANAGER_FOLDER` from the original script. |
| `StatusFileName` | Name of the status file. Equivalent to `MANAGER_STATUS_FILE`. |

All parameters are exposed via `StrategyParam<T>` and can be optimized or modified at runtime inside Designer, Shell, or Runner.

## Usage

1. Assign the desired `Security` before starting the strategy.
2. Configure the parameters as needed (for example set a custom `ManagerFolder` path).
3. Start the strategy. Ticks will begin streaming, the state will be restored from the status file if it exists, and progress will be written back to disk on every packet.
4. The strategy can be stopped manually at any time. On stop the progress file is updated so the next run resumes from the latest known state.

## Differences from the MQL Version

- StockSharp delivers ticks via event subscriptions instead of `OnTimer`, so there is no explicit polling loop.
- The progress information is stored using UTF-8 text rather than binary serialization to simplify inspection and troubleshooting.
- Status updates are throttled using a configurable delay, avoiding manual calls to `Sleep` while still matching the intent of the original timeout.
- File and directory handling uses .NET APIs (`Directory.CreateDirectory`, `File.WriteAllText`) instead of MetaTrader's file functions.

## Requirements

- StockSharp connection capable of providing tick-level data for the chosen instrument.
- Write permissions for the configured manager folder.

Once these requirements are met, the strategy will reproduce the historical tick pulling workflow provided by the MetaTrader script while integrating seamlessly with StockSharp tooling.
