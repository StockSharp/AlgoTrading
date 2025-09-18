# MarsiEaStrategy

## Overview

`MarsiEaStrategy` replicates the logic of the original MetaTrader MARSIEA expert advisor inside the StockSharp high level API. The strategy combines a simple moving average with a relative strength index (RSI) filter and only ever holds a single position at a time. Protective stop-loss and take-profit orders are measured in pips exactly like the source implementation, while the traded volume is sized dynamically from the portfolio equity.

## Trading logic

1. **Data preparation**
   - A simple moving average (SMA) with configurable length runs on the selected candle series.
   - An RSI with configurable period uses the same candles.
   - The candle series is configurable through the `CandleType` parameter and defaults to one-minute candles.

2. **Entry rules**
   - The strategy requires both indicators to be formed and no open position to exist.
   - **Long setup:** the close price is above the SMA and the RSI is below the oversold threshold.
   - **Short setup:** the close price is below the SMA and the RSI is above the overbought threshold.
   - Only one position can be open at any time, mirroring the MetaTrader expert behaviour.

3. **Exit rules**
   - Immediately after entering a trade the strategy registers a fixed stop-loss and take-profit distance, both defined in pips.
   - There are no additional exit conditions; the protective orders handle position closure.

## Risk and position sizing

- `RiskPercent` controls the percentage of the current portfolio value risked per trade.
- The pip value is computed from `Security.PriceStep`, `Security.StepPrice` and the number of digits, emulating the `_Digits` check from MQL.
- Volume is rounded to the closest allowed `Security.VolumeStep` and respects `Security.VolumeMin` when available.
- If risk-based sizing cannot be computed (missing instrument metadata or zero stop), the strategy falls back to the `Volume` property (defaulting to 1 contract/lot).

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle series used for indicator calculations. |
| `MaPeriod` | Length of the SMA indicator. |
| `RsiPeriod` | Lookback length for the RSI. |
| `RsiOverbought` | RSI threshold that defines an overbought market for shorts. |
| `RsiOversold` | RSI threshold that defines an oversold market for longs. |
| `RiskPercent` | Percentage of equity risked per trade. |
| `StopLossPips` | Stop-loss distance expressed in pips. |
| `TakeProfitPips` | Take-profit distance expressed in pips. |

## Notes on the conversion

- The MetaTrader implementation traded at Bid/Ask prices; this port uses the candle close as the entry reference because intrabar ticks are not available in the high level API.
- Pip size follows the same rule as the MQL version: five- or three-digit symbols multiply the price step by ten.
- `StartProtection()` is invoked once so that stop-loss and take-profit orders are automatically linked to the open position by the engine.
- The strategy retains the original behaviour of skipping new entries while any position is active.
