# Aggregate BTC Candles
[Русский](README_ru.md) | [中文](README_cn.md)

This example strategy builds synthetic BTC candles by averaging price data from several exchanges. It subscribes to candle series for each configured exchange and logs the aggregated open, high, low and close values.

## Details

- **Purpose**: data aggregation, no trading rules.
- **Data**: uses candle data from up to four exchanges.
- **Outputs**: aggregated OHLC values written to the log.

