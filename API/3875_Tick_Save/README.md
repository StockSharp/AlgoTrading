# Tick Save Strategy

## Overview

The **Tick Save Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `TickSave`. The strategy continuously monitors the best bid price for a configurable basket of securities and stores each change into CSV files. The output mirrors the original expert by building a monthly file per symbol and optionally appending diagnostic markers.

## Core Features

- Subscribes to Level 1 data for every instrument listed in the `Symbol List` parameter.
- Writes records containing the server timestamp and the latest bid price whenever the value changes.
- Automatically creates directory hierarchy `<Output Root>/<Server Folder>/<Symbol>_YYYY.MM.csv`.
- Optional diagnostic markers identical to the original expert (`Connection lost` and `Expert was stopped`).
- Works with any security resolvable through the strategy `SecurityProvider`.

## Parameters

| Name | Description |
| ---- | ----------- |
| `Symbol List` | Comma-separated identifiers of the securities to record. |
| `Write Warnings` | When enabled, the strategy inserts diagnostic markers along with each tick and when it stops. |
| `Output Root` | Root directory where CSV files are stored. Defaults to a `Ticks` subfolder inside the application directory. |
| `Server Folder` | Optional subfolder representing the trading server or environment. When empty the folder is inferred from the attached portfolio or security board. |

## Output Format

Each CSV line contains two comma-separated columns:

1. Server time formatted as `yyyy-MM-dd HH:mm:ss`.
2. Bid price in invariant culture format.

When `Write Warnings` is enabled the diagnostic markers are written on separate lines to indicate connection issues and shutdown events, mimicking the MetaTrader 4 behaviour.

## Usage Notes

1. Attach the strategy to a connector that exposes the desired securities.
2. Populate `Symbol List` with identifiers understood by the connected `SecurityProvider`.
3. Optionally adjust the directories or enable diagnostic markers.
4. Start the strategy to begin streaming ticks into the generated CSV files.
