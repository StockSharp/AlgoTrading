# Scalpel EA Strategy

This strategy is a simplified conversion of the original *Scalpel EA* written for MetaTrader.
It combines a Commodity Channel Index (CCI) filter with multi-timeframe breakout
analysis. The goal is to trade in the direction of short-term momentum when
several higher timeframes confirm the movement.

## Logic

1. **Indicator** – CCI calculated on the primary timeframe. Trades are allowed
   only when the CCI value stays inside a configurable band around zero.
2. **Trend confirmation** – For 30‑minute, 1‑hour and 4‑hour candles the most
   recent highs and lows are compared with previous ones.
   - Long trades require rising lows on all three timeframes.
   - Short trades require falling highs on all three timeframes.
3. **Breakout** – Entry is triggered when the close price of the primary candle
   breaks the high (for longs) or low (for shorts) of the previous candle.
4. **Risk control** – `StartProtection` places a fixed take‑profit and stop‑loss
   measured in price units.

## Parameters

| Name | Description |
| ---- | ----------- |
| `CciPeriod` | Period of the Commodity Channel Index. |
| `CciLimit` | Absolute CCI threshold. Entries are allowed only inside ±limit. |
| `TakeProfit` | Take profit value in price units. |
| `StopLoss` | Stop loss value in price units. |
| `CandleType` | Primary timeframe for trading (default 1 minute). |

## Notes

- The strategy subscribes to additional 30‑minute, 1‑hour and 4‑hour candles
  to evaluate higher‑timeframe trends.
- Volume is taken from the base class `Strategy.Volume` property.
- Only one position is open at a time. Opposite signals close the existing position
  and open a new one.
