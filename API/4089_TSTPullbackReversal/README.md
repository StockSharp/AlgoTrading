# TST Pullback Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The **TST Pullback Reversal Strategy** watches for aggressive intrabar reversals. It was converted from the original MetaTrader 4 expert advisor `TST.mq4` and rebuilt using the high-level StockSharp API. The method looks for candles where price has pulled sharply away from the candle open after setting an intraday extreme, then fades that move expecting mean reversion. The strategy trades both long and short and uses static stop-loss and take-profit levels measured in price steps.

## Signal Logic
- **Long setup**
  1. The candle closes below its open (`Open > Close`).
  2. The distance between the candle high and the close is greater than `GapPoints * PriceStep`.
  3. No trade was executed earlier on the same bar.
  When satisfied, the strategy closes any short exposure and buys `OrderVolume` units (plus the size required to flip from a short to a long position).

- **Short setup**
  1. The candle closes above its open (`Close > Open`).
  2. The distance between the close and the candle low is greater than `GapPoints * PriceStep`.
  3. No trade was executed earlier on the same bar.
  When satisfied, the strategy closes any long exposure and sells `OrderVolume` units (plus the size required to flip from a long to a short position).

## Position Management
- A new trade immediately assigns static stop-loss and take-profit levels calculated from the fill price and the `StopLossPoints`/`TakeProfitPoints` parameters.
- On each finished candle the strategy checks the candle's high/low to see whether the stop or target was touched and exits the position if triggered. Stop-loss checks take precedence over take-profit checks.
- After an exit the stored risk levels are cleared, but the strategy still remembers the bar time to avoid re-entering during the same candle (reproducing the `NevBar()` guard from the MQL4 version).

## Parameters
- `StopLossPoints` (default `500`): distance from entry to the protective stop, expressed in price steps.
- `TakeProfitPoints` (default `100`): distance from entry to the profit target, expressed in price steps.
- `GapPoints` (default `500`): minimum pullback between the candle extreme and the close required to generate a signal.
- `OrderVolume` (default `0.1`): quantity sent with every new market order.
- `CandleType` (default `1 hour`): timeframe of the candles supplied through `SubscribeCandles`.

All distance-based settings are multiplied by the instrument's `PriceStep`. If the security does not report a step the strategy falls back to `1`.

## Implementation Notes
- The conversion uses StockSharp's high-level API and does not create custom indicator collections.
- Only finished candles are processed to stay compatible with the Strategy Designer; this approximates the intrabar decisions of the MT4 robot by using completed bar data.
- A dedicated flag `_lastSignalBarTime` replicates the `NevBar()` guard from the MQL4 code so that only one order can be opened per candle.
- Order volume handling mirrors the MT4 behaviour: existing opposite positions are flattened before establishing the new direction in a single market order.
- Stop-loss and take-profit orders are simulated inside the strategy logic (instead of server-side orders) to match the original distances while keeping control within StockSharp.

## Usage Tips
- Choose `GapPoints` relative to the volatility of the traded instrument; larger values reduce trade frequency but filter smaller pullbacks.
- Because stop and target checks rely on finished candles, consider using shorter candle durations if you need tighter execution.
- Combine the strategy with additional filters (trend, time of day, volume) when deploying to live markets to reduce whipsaw trades.
