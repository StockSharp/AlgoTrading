# Liquidex V1 Strategy

Liquidex V1 is a breakout scalping strategy converted from the original MQL expert advisor. It combines a **range filter** and a **weighted moving average (WMA)** to identify short‑term opportunities.

## Trading Logic
1. For every finished candle the strategy measures its range (`high - low`).
2. If the candle range is smaller than `RangeFilter`, the candle is ignored.
3. A WMA with period `MaPeriod` is calculated using closing prices.
4. When the candle opens below the WMA and closes above it, a **buy** market order is sent.
5. When the candle opens above the WMA and closes below it, a **sell** market order is sent.
6. Each position is protected by a stop loss defined in `StopLoss`.

## Parameters
- `RangeFilter` – minimum candle range in price units required to trade.
- `MaPeriod` – number of periods for the weighted moving average.
- `StopLoss` – protective stop loss in points.
- `CandleType` – candle series used for analysis.

The strategy uses `Strategy.Volume` as order size and reverses the position when an opposite signal appears.
