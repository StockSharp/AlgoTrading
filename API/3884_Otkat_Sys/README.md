# Otkat Sys Strategy

The strategy reproduces the MetaTrader expert advisor **1_Otkat_Sys**. It monitors the previous trading day's open, close, high,
and low to decide whether to enter a position during the first three minutes after midnight (broker time) from Tuesday to
Thursday.

## Trading Logic

1. **Daily statistics** – the last completed daily candle is cached in order to compute:
   - `Open - Close` and `Close - Open` to detect whether the previous session was bearish or bullish.
   - `Close - Low` and `High - Close` to measure how deeply the price pulled back from the extremes.
2. **Entry window** – new trades are evaluated when the entry candle opens between 00:00 and 00:03. Monday and Friday are
   skipped, matching the original robot's `DayOfWeek` filters.
3. **Directional filters** – four mutually exclusive conditions mirror the MQL rules:
   - Bearish previous day (`Open - Close` above the corridor threshold) combined with a shallow retracement (`Close - Low`
     below `Pullback - Tolerance`) opens a long.
   - Bullish previous day with an extended upside retracement (`High - Close` above `Pullback + Tolerance`) also opens a long.
   - Bullish previous day with a weak upside retracement (`High - Close` below `Pullback - Tolerance`) opens a short.
   - Bearish previous day with an extended downside retracement (`Close - Low` above `Pullback + Tolerance`) opens a short.
4. **Orders** – entries are market orders placed with the configured lot size. Buy trades use a take-profit distance equal to
   `TakeProfit + 3` points (as in the original EA); shorts use exactly `TakeProfit` points. Both sides apply the same stop-loss
   distance.
5. **Time-based exit** – any open position is flattened after 22:45, replicating the nightly cleanup implemented in the MetaTrader
   script.

All threshold parameters are expressed in points and translated into price distances with the instrument's `PriceStep`.

## Parameters

| Name | Description |
| --- | --- |
| `EntryCandleType` | Timeframe used for the trading window (default: 1 minute). |
| `DailyCandleType` | Timeframe providing the daily statistics (default: 1 day). |
| `TakeProfit` | Profit target in points. Long trades add a 3-point buffer. |
| `StopLoss` | Protective stop distance in points. |
| `PullbackThreshold` | Base pullback ("Otkat") threshold in points. |
| `CorridorThreshold` | Directional corridor threshold (`KoridorOC`). |
| `ToleranceThreshold` | Pullback tolerance (`KoridorOt`). |
| `TradeVolume` | Lot size for each entry. |

The strategy automatically resets its cached values on `Reset`, subscribes to both entry and daily candle streams, and draws
candles plus trade markers when a chart area is available.
