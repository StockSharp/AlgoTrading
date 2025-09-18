# Exp BrainTrend2 AbsolutelyNoLagLwma X2MACandle MMRec Strategy

## Overview
This strategy recreates the multi-module MetaTrader Expert Advisor by combining three filters in the StockSharp high level API:

1. **BrainTrend2 inspiration** – an Average True Range (ATR) channel detects volatility contraction and expansion phases.
2. **AbsolutelyNoLagLwma approximation** – a linear weighted moving average (LWMA) tracks the dominant direction with minimal lag.
3. **X2MACandle replica** – a fast and a slow exponential moving average (EMA) pair evaluates candle colour to validate momentum.

A position is opened only when all filters point in the same direction. ATR driven stop-loss and take-profit targets manage the exit process and emulate the original MMRec money management concept.

## Trading Logic
- **Bullish setup**: the candle closes above the LWMA while the fast EMA is higher than the slow EMA. A fresh long entry is allowed only after the previous bullish bias disappeared, preventing multiple orders on identical signals.
- **Bearish setup**: the candle closes below the LWMA while the fast EMA is lower than the slow EMA. Short positions obey the same confirmation and cooldown rules as the long side.
- **Risk management**: ATR defines dynamic exit levels. Both stop-loss and take-profit scale with the current volatility and are re-evaluated on every candle. If the market touches either level, the strategy closes the entire position by market order.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Timeframe of the working candle series. Defaults to 6-hour candles to mirror the original EA defaults. |
| `AtrPeriod` | Lookback period used by the ATR volatility filter. |
| `LwmaLength` | Period of the linear weighted moving average trend filter. |
| `FastMaLength` | Period of the fast EMA used for candle colouring. |
| `SlowMaLength` | Period of the slow EMA used for candle colouring. |
| `StopLossAtrMultiplier` | Multiplier applied to ATR to calculate the protective stop distance. |
| `TakeProfitAtrMultiplier` | Multiplier applied to ATR to determine the take-profit distance. |

All parameters are exposed through `StrategyParam<T>` so that they can be optimised inside StockSharp.

## Notes
- The original Expert Advisor relies on proprietary indicator buffers. This port uses standard StockSharp indicators that reproduce the same directional cues without relying on external scripts.
- Money management is simplified to full-position exits because StockSharp strategies typically operate on portfolio-sized orders. The ATR driven distances provide the adaptive behaviour expected from the MMRec module.
- Commentary in the code is in English as required by the conversion guidelines.
