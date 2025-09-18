# PivotEMA3RLHv4 Strategy

## Overview

PivotEMA3RLHv4 is a trend-following strategy that combines the daily pivot level with short-term momentum filters. It tracks a 3-period exponential moving average (EMA) calculated on candle open prices and compares it against the same EMA computed on closing prices. The setup is validated with Heiken Ashi candles to confirm direction and with multiple Average True Range (ATR) measurements to ensure that volatility is expanding. The strategy trades a single instrument on the selected intraday timeframe and always waits for the current candle to finish before making a decision.

## Trading Logic

1. **Pivot Filter** – The previous EMA(3) of the open price must be below (for longs) or above (for shorts) the daily pivot level, while the current EMA(3) of the open price needs to cross to the opposite side of the pivot.
2. **Heiken Ashi Confirmation** – The current Heiken Ashi candle must be bullish (close above open) for longs or bearish (close below open) for shorts.
3. **Momentum Check** – The EMA(3) built on closing prices must lead the EMA on opens in the trade direction.
4. **Volatility Expansion** – At least one of the ATR(4), ATR(8), ATR(12) or ATR(24) values must increase compared with the previous candle, and the True Range (ATR with length 1) must either increase on this bar or have increased on the previous bar.
5. **Position Management** – Only one position is active at a time. Protective stops and targets are simulated internally and executed via market orders when hit.

Exit signals mirror the entry rules: when the opposite conditions appear, the strategy closes the current trade. Additionally, the optional stop-loss, take-profit, and trailing stop mechanisms may close a trade earlier.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Working timeframe for the strategy candles. |
| `StopLossPips` | Initial stop distance in pips from the entry price. Set to zero to disable. |
| `TakeProfitPips` | Profit target distance in pips. Set to zero to disable. |
| `UseTrailingStop` | Enables or disables trailing stop management. |
| `TrailingStopType` | Trailing mode: 1 keeps a fixed distance, 2 activates after price moves by `TrailingStopPips`, 3 uses the multi-stage ladder described below. |
| `TrailingStopPips` | Distance (in pips) used by trailing type 2. |
| `FirstMovePips` / `FirstStopLossPips` | Trigger distance and resulting stop offset for the first stage of trailing type 3. |
| `SecondMovePips` / `SecondStopLossPips` | Trigger distance and resulting stop offset for the second stage of trailing type 3. |
| `ThirdMovePips` / `TrailingStop3Pips` | Trigger distance and dynamic trailing distance for the final stage of trailing type 3. |

## Trailing Stop Modes

- **Type 1** – Repositions the stop so that it never lags the price by more than the initial stop distance.
- **Type 2** – Waits for price to move by `TrailingStopPips` before locking profits with the same distance.
- **Type 3** – Uses up to three thresholds: the first two move the stop to predefined offsets, while the third turns into a regular trailing stop.

## Notes

- The strategy subscribes to daily candles to calculate the pivot level from the previous day’s high, low, and close.
- Indicators are updated inside the candle handler using finished bars only, which keeps the logic compatible with both online and backtesting environments.
- The original MetaTrader version relied on broker-side stops; this port simulates them and exits with market orders when necessary.
