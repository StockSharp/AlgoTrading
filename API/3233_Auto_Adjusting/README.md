# AutoAdjustingStrategy

AutoAdjustingStrategy replicates the MetaTrader expert *Aouto Adjusting1* using StockSharp's high-level API. The port keeps the original multi-timeframe momentum filter, monthly MACD trend confirmation, and three-layer EMA stack to detect with-trend pullbacks. Stops and targets are projected from recent swing extremes and automatically adjusted every completed candle.

## Core logic

1. **Trend structure** – three exponential moving averages on the trading timeframe (6, 14, 26) must be aligned (`EMA6 < EMA14 < EMA26` for longs, inverted for shorts). The previous candle needs to tag the middle EMA, while the prior candle forms a higher low / lower high to confirm a pullback.
2. **Momentum confirmation** – momentum on the higher timeframe (mapped from the trading timeframe, e.g., H1 → D1) must deviate at least `MomentumBuyThreshold` / `MomentumSellThreshold` from 100 on any of the last three completed bars.
3. **Macro filter** – a monthly MACD(12, 26, 9) signal ensures trades align with the dominant trend (`MACD > Signal` for buys, `<` for sells).
4. **Execution** – market orders are submitted once all filters agree and no opposite exposure is present. Opposite positions are flattened before entering the new direction.
5. **Protection** – stop-loss levels are placed a configurable pad of pips beyond the lowest low / highest high of the last `CandlesBack` bars. Take-profit distances scale by `RewardRatio`. Both stop and target are re-armed on each candle close while the position is active.

## Risk and position sizing

The strategy mirrors the original risk parameterization:

- `RiskPercent` calculates an adaptive position size whenever portfolio value and price step metadata are available. The algorithm divides the allowed monetary loss by the loss per unit implied by the current stop distance.
- When risk-based sizing cannot be evaluated (e.g., missing portfolio statistics), the engine falls back to the fixed `TradeVolume` parameter.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeFrame(H1)` | Trading timeframe used for the EMA stack. |
| `MomentumCandleType` | `DataType` | Derived from `CandleType` | Higher timeframe feeding the momentum indicator (H1→D1, H4→W1, etc.). |
| `MacroMacdCandleType` | `DataType` | `TimeFrame(30 days)` | Timeframe for the macro MACD confirmation (monthly by default). |
| `PadAmount` | `decimal` | `3` | Extra pips beyond swing extremes when computing stops. |
| `RiskPercent` | `decimal` | `0.1` | Percent of portfolio equity risked per trade. |
| `RewardRatio` | `decimal` | `2` | Multiplier applied to the stop distance to place the take-profit. |
| `CandlesBack` | `int` | `3` | Number of candles inspected for swing high/low detection. |
| `MomentumBuyThreshold` | `decimal` | `0.3` | Minimum momentum deviation required to enable long entries. |
| `MomentumSellThreshold` | `decimal` | `0.3` | Minimum momentum deviation required to enable short entries. |
| `TradeVolume` | `decimal` | `1` | Fallback lot size when risk-based sizing is unavailable. |

## Charting and visualization

- Subscribe to the trading timeframe and plot the three EMAs to observe pullbacks.
- Track the momentum series on its higher timeframe panel to confirm energy thresholds.
- Monitor the MACD values from the macro timeframe to validate the trend filter.

## Notes

- The automatic timeframe mapping matches the MQL expert: M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1. Other frames keep their original value.
- The strategy avoids indicator `GetValue` calls by storing the most recent values inside the strategy and feeding them through the bind callbacks.
- Trailing behavior mirrors the original EA by recalculating the protective levels every time a candle closes.
