# 4 SMA Strategy

## Overview
The 4 SMA strategy replicates the MetaTrader expert advisor **4 SMA.mq4**. It works on 30-minute candles calculated with median prices and compares four simple moving averages (5, 20, 40, and 60 periods) to detect momentum breakouts. The StockSharp port keeps the single-position behaviour of the original code and uses high-level API helpers for market entries and risk management.

## Trading Logic
- Calculate the median price `(high + low) / 2` for each finished candle and feed it into the four SMAs.
- **Long entry** happens when the fast SMA is above the medium SMA, the medium SMA is above the slow SMA, the slow SMA is above the very slow SMA by at least one price step, and the previous slow SMA was below or equal to the very slow SMA. Only one long position can be active at a time.
- **Short entry** is the mirror condition: the fast SMA is below the medium SMA, the medium SMA is below the slow SMA, the very slow SMA is above the slow SMA by at least one price step, and the previous slow SMA was above or equal to the very slow SMA. Only one short position can be active at a time.

## Position Management
- The strategy closes longs when the slow SMA crosses below the very slow SMA and closes shorts when the slow SMA crosses above the very slow SMA.
- Protective levels are pre-computed after each entry. Stop-loss and take-profit distances follow the original point-based settings and rely on the security price step.
- Trailing stops activate after price moves beyond the configured trailing distance. The stop is trailed candle by candle and never loosened.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| CandleType | Candle series used for calculations (30-minute by default). | M30 time frame |
| TakeProfit | Take-profit distance in points. | 50 |
| StopLoss | Stop-loss distance in points. | 50 |
| TrailingStop | Trailing stop distance in points. | 11 |
| FastLength | Length of the fast SMA. | 5 |
| MediumLength | Length of the medium SMA. | 20 |
| SlowLength | Length of the slow SMA. | 40 |
| VerySlowLength | Length of the very slow SMA. | 60 |

All numeric parameters are exposed for optimisation via the StockSharp parameter UI.

## Differences from the MQL Version
- The original trailing stop manipulated MT4 orders directly; the port recalculates exit prices and issues market orders when levels are breached.
- Price-step aware calculations let the strategy operate on instruments with non-forex tick sizes.
- The StockSharp implementation relies on high-level `SubscribeCandles` bindings and strategy parameters, staying close to framework best practices.
