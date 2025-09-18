# Graal EMA Momentum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a conversion of the MetaTrader 4 expert advisor **0Graal-CROSSmuvingi**. It trades trend reversals that occur when a fast exponential moving average (EMA) on closing prices crosses a slower EMA calculated on opening prices. A momentum oscillator confirms the breakout direction, and a fixed-distance take profit replicates the original MT4 execution model.

## Trading Idea

1. **Fast EMA on close** tracks the most recent price action.
2. **Slow EMA on open** lags behind and forms the crossover baseline.
3. **Momentum oscillator (period 14)** measures how strongly price accelerates away from the neutral value (100). The strategy only trades when momentum deviates from 100 by more than a configurable filter and continues to strengthen in the same direction.
4. **Take profit** closes trades after a predefined distance measured in instrument points, mirroring the MT4 `TakeProfit` parameter.

## Entry Rules

- **Long setup**
  - The fast EMA crosses above the slow EMA on the current finished candle while the previous bar had the fast EMA below or equal to the slow EMA.
  - Momentum (value minus 100) is greater than the `MomentumFilter` threshold and also higher than the previous bar's momentum reading.
  - Existing short positions are closed before opening a new long. The new long size equals the configured `Volume` plus any amount required to flip an open short.
- **Short setup**
  - The fast EMA crosses below the slow EMA while the previous bar had the fast EMA above or equal to the slow EMA.
  - Momentum (value minus 100) is below the negative `MomentumFilter` threshold and less than the previous bar's momentum reading.
  - Existing long positions are closed before opening a new short. The new short size equals the configured `Volume` plus the quantity needed to cover an open long.

## Exit Rules

- Positions are closed automatically when price reaches the calculated take-profit target (`TakeProfitPoints * PriceStep`).
- A new opposite signal also reverses the position immediately because the order size always includes the quantity of the current position.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `FastPeriod` | Length of the EMA on closing prices. | 13 |
| `SlowPeriod` | Length of the EMA on opening prices. | 34 |
| `MomentumPeriod` | Momentum oscillator lookback. | 14 |
| `MomentumFilter` | Minimum absolute momentum deviation from 100 required to trade. | 0.1 |
| `TakeProfitPoints` | Distance to the profit target in price points (multiplied by `PriceStep`). | 200 |
| `CandleType` | Candle data type used for calculations (15-minute timeframe by default). | 15-minute time frame |
| `Volume` | Order size used for new entries. The engine inherits it from the base class. | 1 |

## Implementation Notes

- Signals are processed on closed candles only (`CandleStates.Finished`).
- The strategy subscribes to the chosen candle type with `SubscribeCandles` and binds both EMA and momentum indicators via the high-level API.
- The slow EMA is manually updated with opening prices inside the bind callback to replicate the MT4 behavior where `PRICE_OPEN` was used.
- Take-profit management watches intrabar highs and lows to emulate MT4's point-based exit logic.
- `StartProtection()` is enabled on start to guard against unexpected open positions before the strategy begins trading.
