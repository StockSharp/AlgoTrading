# Galender Strategy

## Overview

The **Galender Strategy** replicates the behavior of the original MQL5 "Galender" expert script using the StockSharp high level API. The strategy listens to economic news messages published by the connected data source, filters them by time range, currency, keyword, and importance, and logs every matching calendar event. The collected events are maintained in chronological order, just like the list view produced by the MQL version.

> **Note**
> The strategy focuses on analytics and notifications. It does not submit any trading orders. Its goal is to help discretionary traders monitor macroeconomic releases directly within the StockSharp environment.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `DateFrom` | Earliest UTC timestamp to include in the calendar scan. Messages published before this moment are ignored. | `2020-07-01T00:00:00Z` |
| `DateTo` | Latest UTC timestamp accepted by the filter. Messages after this moment are skipped. | `2020-09-01T00:00:00Z` |
| `CurrencyFilter` | Comma-separated list of currency codes. The strategy searches for these codes in the news headline and body. Leave empty to disable currency filtering. | `USD` |
| `KeywordFilter` | Keyword or phrase that must appear in the news text. Case is ignored. An empty string disables the keyword filter. | `interest` |
| `ImportanceFilter` | Required importance level: `None`, `Low`, `Moderate`, `High`, or `All`. Matching is performed by scanning the news text for the corresponding labels. | `All` |

## Processing Workflow

1. **Initialization** – when the strategy starts it clears the stored events and subscribes to the news feed of the selected security (the same instrument configured in the strategy settings).
2. **Message Handling** – for each incoming `NewsMessage` the strategy evaluates:
   - whether the server timestamp is between `DateFrom` and `DateTo`;
   - if the configured currencies are present in the headline or body;
   - whether the required keyword is found;
   - if the expected importance label (for example, `HIGH`, `MODERATE`, `LOW`, or `NONE`) is present.
3. **Recording Matches** – every accepted news message is converted into an internal calendar entry that stores:
   - publication time,
   - currency code inferred from the filter,
   - headline (as the event title),
   - qualitative impact (`Positive`, `Negative`, `None`, or empty),
   - text fragments that follow the `Previous`, `Forecast`, and `Actual` labels.
   The entries are stored in a list that is sorted by time to mimic the GUI table in the original EA.
4. **Logging** – the strategy writes a concise, English log message summarizing every match, including all extracted metrics.

## Differences from the MQL5 Version

- The StockSharp port replaces the graphical dialog with log output because StockSharp strategies run headless inside the trading engine.
- The MQL5 economic calendar API is not available. Instead, the strategy analyzes generic news messages provided by the connected broker or data vendor. Importance detection relies on keywords such as `high`, `moderate`, `low`, and `none` appearing in the text.
- Numeric fields (`Previous`, `Forecast`, `Actual`) are parsed heuristically from the message body. If the data vendor uses a different format the values may remain empty, ensuring that no incorrect information is reported.

## Usage Tips

- Attach the strategy to the security that corresponds to the currency you monitor so that the news feed subscription succeeds.
- Provide multiple currencies by separating them with commas, e.g., `USD,EUR,GBP`. The filter searches for each code independently and matches on word boundaries only.
- Set `ImportanceFilter` to `All` to reproduce the "All" option of the MQL5 EA. The other values restrict matches to the specified importance keywords.
- Leave `KeywordFilter` blank if you want to capture every event for the selected currency during the date range.

## Extending the Strategy

- Integrate the collected entries with a GUI component (for example, WPF or Avalonia) to recreate the interactive list of the original expert.
- Replace the keyword-based importance detection with structured metadata if the data provider supplies dedicated fields.
- Combine the calendar signals with automated trading logic—for instance, pause active strategies around high-impact news by sharing the `_entries` list with other components.
