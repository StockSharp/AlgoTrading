# Corrected Average Breakout Strategy

This strategy trades breakouts relative to a **Corrected Average** moving average. The indicator smooths the price using a moving average and adjusts the smoothing factor based on the standard deviation of price changes.

When the price closes above the corrected average by a specified number of points and then pulls back to the breakout level, the strategy opens a long position. The opposite logic is used for short trades. Stop-loss and take-profit are applied in absolute price points.

## Parameters

- `Candle Type` – timeframe of candles used for calculations.
- `Length` – period for the moving average and standard deviation.
- `MA Type` – type of moving average (SMA, EMA, SMMA, LWMA).
- `Level Points` – breakout distance from the corrected average in price steps.
- `Stop Loss Points` – stop-loss distance from entry price in price steps.
- `Take Profit Points` – take-profit distance from entry price in price steps.
- `Enable Long` – allow opening long positions.
- `Enable Short` – allow opening short positions.

## Trading Logic

1. Calculate the moving average and standard deviation.
2. Build the corrected average using previous values and the ratio of variance to smooth sudden jumps.
3. Detect breakouts when the previous bar closes beyond the corrected average plus or minus the configured level.
4. After a breakout, wait for the next bar to return to the breakout level and open a position in the direction of the breakout.
5. Close opposite positions when a new breakout signal appears.
6. Apply stop-loss and take-profit protections.

## Notes

This strategy is a conversion from the MQL script *Exp_CorrectedAverage.mq5*. It is intended for educational purposes and requires further testing before use in live trading.
