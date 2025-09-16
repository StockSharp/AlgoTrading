# StatMaster

StatMaster strategy collects tick data and writes it to a CSV file. The strategy listens to order book updates and stores a record whenever the best bid price changes. Each record contains the date, time, ask price, bid price and spread.

## Parameters

- **Save Period** – frequency of writing the log to disk. Options:
  - `EveryMinute` – save once per minute.
  - `EveryHour` – save once per hour.
  - `EveryDay` – save once per day.

## Algorithm

1. Subscribe to the order book for the selected security.
2. For each incoming order book snapshot:
   - Extract best bid and ask prices.
   - If the bid price changed since the previous snapshot, append a line to the log in the format `DATE;TIME;ASK;BID;SPREAD`.
   - If the selected period has elapsed (minute, hour or day), write the entire log to `StatMaster_<symbol>.csv`.
3. When the strategy stops, the log is written once more to ensure all data is saved.

## Notes

The strategy does not place any trades. Its sole purpose is to archive market quotes for later analysis.
