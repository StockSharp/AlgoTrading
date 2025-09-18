# X MAN Strategy

## Overview

The X MAN strategy recreates the core logic of the MetaTrader expert advisor `X_MAN.mq4` within the StockSharp high-level API. The system trades breakouts driven by a fast and slow linear weighted moving average (LWMA) while filtering entries with multi-timeframe momentum and a monthly MACD confirmation. It is designed for trend continuation trades that are triggered only when momentum and trend structure align.

## Trading Logic

1. **Primary Trend Filter** – Two LWMAs calculated on the selected primary timeframe must be separated by at least the configurable `DistancePoints`. A long setup requires the fast LWMA to be above the slow LWMA by that margin, while a short setup needs the slow LWMA to dominate.
2. **Momentum Confirmation** – The strategy subscribes to a higher timeframe candle series and feeds it into a momentum indicator. The absolute distance of the last three momentum readings from the neutral value (100) must exceed the corresponding buy or sell threshold at least once to allow trading in that direction.
3. **MACD Filter** – A monthly candle series drives a standard (12, 26, 9) MACD. Long trades are allowed only when the MACD line is above the signal line, and short trades require the opposite relationship.
4. **Order Execution** – When all filters agree, the strategy enters using market orders. Positions are flipped only if the opposite setup appears and the current position is flat or in the opposite direction.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Primary timeframe used for the LWMA calculations. |
| `HigherCandleType` | Higher timeframe feeding the momentum filter. |
| `MacdCandleType` | Timeframe for the MACD confirmation (monthly by default). |
| `FastMaPeriod` | Length of the fast LWMA. |
| `SlowMaPeriod` | Length of the slow LWMA. |
| `MomentumPeriod` | Lookback window of the momentum oscillator. |
| `MomentumBuyThreshold` | Minimum distance from 100 required for bullish momentum. |
| `MomentumSellThreshold` | Minimum distance from 100 required for bearish momentum. |
| `DistancePoints` | Minimal separation between the fast and slow LWMA expressed in price points. |
| `TakeProfitPoints` | Optional protective take profit distance in points. |
| `StopLossPoints` | Optional protective stop loss distance in points. |

All parameters are exposed through `StrategyParam<T>` so they can be optimized inside StockSharp Designer or configured at run time.

## Risk Management

If either `TakeProfitPoints` or `StopLossPoints` is greater than zero, the strategy enables StockSharp's built-in protection module using market exits. No additional trailing or breakeven logic from the original MQL expert is implemented yet.

## Differences from the Original Expert

- The MetaTrader implementation handled equity stops, break-even moves, and complex money-management options. This conversion focuses on the core directional filters and market entries; portfolio-level money management is intentionally omitted.
- Order sizing is delegated to the hosting environment. The original lot-exponent logic is not reproduced.
- Alerts, e-mail notifications, and manual trailing-stop modifications are not included.

These changes keep the strategy concise and leverage StockSharp's high-level API while preserving the main trading concept.
