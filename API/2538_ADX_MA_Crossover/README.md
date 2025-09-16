# ADX MA Crossover

## Overview
This strategy reproduces the "ADX & MA" Expert Advisor by combining a smoothed moving average with an Average Directional Index (ADX) trend filter. The logic analyses the last two completed candles on the selected timeframe and reacts only after both the moving average and ADX have produced confirmed values. It is designed for hedging-style entries but is implemented on a netted position model, automatically reversing the position when opposite signals appear.

The moving average is calculated on the median price of each candle, matching the MetaTrader version that used an SMMA built on `(High + Low) / 2`. The ADX threshold prevents trades when the trend strength is weak, reducing false signals from short-lived crosses.

## Entry logic
- Wait until both the smoothed moving average and ADX have produced final values.
- Evaluate the previous candle (`n-1`) close relative to the smoothed MA value taken at the same candle.
- Go long when:
  - Close of candle `n-1` is above the MA value of `n-1`.
  - Close of candle `n-2` was below that MA value (bullish cross), and
  - ADX value of candle `n-1` is greater than or equal to `AdxThreshold`.
- Go short when the inverse conditions occur (bearish cross with ADX confirmation).
- Position size uses the strategy `Volume` plus the absolute value of any opposite exposure to guarantee a reversal on opposite signals.

## Exit logic
Long trades are closed when any of the following conditions triggers:
- The latest confirmed close (`n-1`) drops back below the smoothed MA (opposite cross).
- Price reaches the configured long take-profit distance in pips.
- Price falls to the configured long stop-loss distance in pips.
- Trailing stop for long trades locks in profits once price has moved `TrailingStopBuy` pips beyond the entry price.

Short trades mirror the same rules with their respective parameters and trailing logic. Each time an opposite signal appears the strategy sends a market order large enough to close the current position and open one in the new direction.

## Risk and trade management
- Distances for take profit, stop loss and trailing stop are expressed in **pips**. The strategy derives the pip size from `Security.PriceStep`; when the symbol uses 3 or 5 decimals the pip is defined as `PriceStep × 10`, matching the original MetaTrader adjustment.
- `InitializeLongTargets` and `InitializeShortTargets` compute absolute price levels immediately after sending the market order, storing the entry price approximation based on the last confirmed close.
- When trailing stops are enabled and price moves favourably beyond the configured distance, the stop level is shifted to preserve unrealised profit.
- Both target sets are reset when the position is closed so stale levels are never reused.

## Parameters
- `MaPeriod` – length of the smoothed moving average (default 15).
- `AdxPeriod` – ADX smoothing period (default 12).
- `AdxThreshold` – minimum ADX value required to confirm a trend (default 16).
- `TakeProfitBuy` / `StopLossBuy` / `TrailingStopBuy` – pip distances for long trades.
- `TakeProfitSell` / `StopLossSell` / `TrailingStopSell` – pip distances for short trades.
- `CandleType` – timeframe for input candles, default 1 minute.

Set the strategy `Volume` to control the base order size. The implementation retains the original behaviour where short trades receive their own risk settings instead of reusing the long parameters.
