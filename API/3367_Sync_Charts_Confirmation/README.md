# Sync Charts Confirmation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy mirrors the idea of the original MQL "SyncCharts" utility by monitoring two candle feeds of the same instrument and
making trading decisions only when both streams confirm the same trend direction. The master series acts as the reference chart
(the one a trader typically watches), while the follower series represents an auxiliary chart (for example, a faster timeframe or
an alternative aggregation). By forcing both streams to agree before entering the market, the system filters out noise coming from
temporary desynchronization between chart intervals.

The setup works best on instruments that exhibit multi-timeframe trend structure such as index futures and liquid currency pairs.
Because both charts must move together before a trade is taken, false signals are reduced and the strategy naturally limits
exposure during chaotic market phases when timeframes disagree or new candles print at different moments.

## Details

- **Entry Criteria**:
  - **Long**: Both the master and follower simple moving averages (SMAs) slope upward on their most recent finished candles, and
    the timestamps of those candles differ by less than the synchronization tolerance.
  - **Short**: Both SMAs slope downward and the timestamp difference is within the tolerance window.
- **Exit Criteria**:
  - Time desynchronization: if the latest candles are separated by more than the allowed tolerance the position is flattened.
  - Trend disagreement: if one SMA turns up while the other turns down the open position is closed immediately.
- **Stops**: Implicit flatten logic acts as a soft stop. No separate hard stop is submitted.
- **Long/Short**: Both sides are traded.
- **Default Values**:
  - Master candle: 5 minute timeframe.
  - Follower candle: 1 minute timeframe.
  - SMA length: 20 periods on both streams.
  - Synchronization tolerance: 15 seconds between candle open times.
- **Filters**:
  - Category: Trend confirmation / multi-timeframe.
  - Direction: Bi-directional.
  - Indicators: SMA (dual stream).
  - Stops: No fixed stop, auto-flatten when charts diverge.
  - Complexity: Medium (multi-subscription with synchronization checks).
  - Timeframe: Configurable (default intraday).
  - Seasonality: None.
  - Neural networks: No.
  - Divergence: Uses timeframe divergence as a filter (requires agreement, not price divergence).
  - Risk level: Moderate due to confirmation requirement.

## How it works

1. Two candle subscriptions are created via the high-level StockSharp API: one for the master chart and another for the follower.
2. Each feed is processed by an SMA with the same length, yielding a trend direction flag (`up` if the SMA value rises versus the
   previous candle, `down` otherwise).
3. Whenever both candles finish, the strategy verifies that their timestamps are close enough (absolute difference below the
   configured tolerance).
4. If the charts are synchronized and both trends point up, the strategy buys (closing any short first). If both trends point down,
   it sells short (closing any long first).
5. Any loss of synchronization or trend disagreement triggers an immediate flatten to keep the account aligned with the charts the
   trader watches.

## Recommended usage

- Apply to the same instrument on two different timeframes that normally correlate (e.g., 5-minute and 1-minute, or hourly and
  15-minute).
- Increase the synchronization tolerance if you work with exotic data sources that print candles with minor delays.
- Combine with an external risk manager or add-on stop module when deploying to live trading.
