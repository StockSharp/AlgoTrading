# JBrainTrend ReOpen Strategy

This strategy is a C# implementation inspired by the MQL5 example "JBrainTrend1Stop_ReOpen".  
It uses the Stochastic oscillator to determine overbought and oversold conditions and supports pyramiding by reopening positions when price advances by a specified step.

## Logic
- Subscribe to candles of the selected timeframe.
- Calculate Stochastic oscillator (%K and %D).
- Enter long when %K falls below 20 and short when %K rises above 80.
- Positions are closed when the opposite extreme is reached.
- After an entry, additional positions are added if price moves `PriceStep` in the direction of the trade, up to `MaxPositions`.
- Protective stop loss and take profit are applied in absolute price units.

## Parameters
- `StochPeriod` – main period of the Stochastic oscillator.
- `KPeriod` / `DPeriod` – smoothing periods for %K and %D lines.
- `CandleType` – timeframe used for analysis.
- `StopLoss` – stop loss distance in price units.
- `TakeProfit` – take profit distance in price units.
- `PriceStep` – price movement required to reopen a position.
- `MaxPositions` – maximum number of entries in one direction.
- `BuyEnabled` / `SellEnabled` – enable or disable long/short trades.

## Notes
The original MQL5 script used a custom indicator named *JBrainTrend1Stop*.  
This C# port approximates the trading concept with built‑in indicators from StockSharp for easier integration.
