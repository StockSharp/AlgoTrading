# Ask Bid Ticks Collector Strategy

## Overview

`AskBidTicksStrategy` captures every change of the best ask and bid prices published through level 1 data and exports the stream into a CSV file. The strategy mirrors the original MQL script that stored high precision tick data for microstructure analysis while adding StockSharp-specific configuration options.

## Core Logic

1. Subscribe to best bid/ask updates via the high-level `SubscribeLevel1()` helper.
2. For every message that contains both sides, format the prices and the timestamp according to user preferences.
3. Append the formatted record to a CSV file and forward a short log message to the strategy journal.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| File Name | Custom name for the CSV file. When set to `Use default`, the strategy builds `<yyyy.MM.dd>_<symbol>.csv` in the working directory. | Use default |
| Timestamp Mode | Controls how the timestamp column is formatted (standard, with milliseconds, or relative milliseconds). | Millisecond |
| Delimiter | Character used to separate columns inside the CSV file. | Tab |

## Files

- `CS/AskBidTicksStrategy.cs` – C# implementation of the strategy.

## How to Use

1. Attach the strategy to a security that provides level 1 data.
2. Configure the output parameters if a custom file path or delimiter is needed.
3. Start the strategy and monitor the generated CSV file to review tick-by-tick best ask and bid data.

## Notes

- The strategy uses the local machine clock to ensure timestamps stay in sync with exported CSV records.
- Each tick is flushed immediately to preserve the data even during unexpected terminations.
