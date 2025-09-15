# STLMCandle Strategy

This strategy trades based on the direction of the last finished candle.
If the close price is above the open price, it opens a long position and closes any short position.
If the close price is below the open price, it opens a short position and closes any long position.
It supports stop loss and take profit levels and operates on a configurable candle timeframe.

## Parameters
- `CandleType` – timeframe of candles used for analysis.
- `StopLoss` – absolute stop loss value in price units.
- `TakeProfit` – absolute take profit value in price units.

## Notes
The strategy is a simplified adaptation of the original MQL `STLMCandle` expert advisor.
It approximates the indicator by using the standard candle open and close prices.
