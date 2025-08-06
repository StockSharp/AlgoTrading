# Overnight Sentiment Anomaly Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This strategy trades an equity ETF only overnight when an external sentiment indicator signals extreme optimism. At the close the ETF is bought if the indicator exceeds a threshold and is sold the next morning, targeting the overnight drift associated with positive sentiment.

Intraday data is not used; the algorithm reacts to end-of-day sentiment values and places market orders at the close and next day's open.

## Details

- **Instrument**: equity ETF and sentiment data series.
- **Signal**: sentiment value above configurable `Threshold`.
- **Holding period**: market close to next day open.
- **Positioning**: long when sentiment high, otherwise flat.
- **Risk control**: order skipped when trade value below `MinTradeUsd`.
