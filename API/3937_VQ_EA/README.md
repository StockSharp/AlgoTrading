# VQ EA

## Overview
- Conversion of the MetaTrader expert "VQ_EA" that trades using the Volatility Quality (VQ) indicator.
- The StockSharp version approximates the VQ line with a smoothed median price to keep the logic within the high-level API.
- Positions are opened on direction changes of the smoothed line and managed with optional protective orders.

## Original MQL behaviour
1. Requests buy or sell signals from the VQ custom indicator (buffers 3 and 4).
2. Opens a new market position when a fresh signal appears and no trade is active in that direction.
3. Closes the opposite position immediately on an opposite signal.
4. Optional money-management features: fixed lots, fractional lots, break-even, trailing stop, manual log output and alert/email notifications.

## StockSharp implementation
- Instead of the proprietary VQ indicator the strategy applies a simple moving average to the median price and optionally smooths it once more.
- The slope of the smoothed series plays the role of the original colour change of the VQ line.
- A configurable filter expressed in points prevents signals caused by minor fluctuations.
- Market orders are used for entries and exits, mirroring the original EA behaviour.

### Signal generation
1. Subscribe to the selected candle type and calculate the median price for each completed candle.
2. Apply the base moving average (`Length`) and, if requested, an additional smoothing (`Smoothing`).
3. Compare the current smoothed value with the previous one. If the absolute change exceeds `FilterPoints` (converted into price units), mark the direction as rising or falling.
4. When the direction flips from down to up a long entry is issued. A flip from up to down produces a short entry. Existing positions are reversed by adding the absolute position volume to the order size.

### Risk management
- `StopLossPoints`, `TakeProfitPoints` and `TrailingStopPoints` are converted into absolute prices by multiplying with the instrument price step.
- If at least one of these protections is enabled, `StartProtection` is called with market-order adjustments so that stops follow the position like in the MQL expert.
- The optional trailing stop is activated only when `UseTrailing` is `true` and the trailing distance is greater than zero.

## Parameters
- `Length` – base smoothing period of the median price. Default: 5.
- `Smoothing` – secondary smoothing period. Default: 1 (disabled).
- `FilterPoints` – minimal move in points required to confirm that the slope changed. Default: 5.
- `StopLossPoints` – protective stop-loss in points. Default: 60 (0 disables it).
- `TakeProfitPoints` – protective take-profit in points. Default: 0 (disabled).
- `UseTrailing` – enable or disable trailing stops. Default: false.
- `TrailingStopPoints` – trailing distance in points. Default: 0 (ignored when `UseTrailing` is false).
- `CandleType` – timeframe used for calculations. Default: 1-hour candles.
- `Volume` – inherited from `Strategy.Volume`, defaults to 1 contract and is used for every fresh entry.

## Differences from the original expert
- The exact VQ buffer values are approximated by smoothed median prices; the indicator is not ported one-to-one.
- Advanced features such as break-even shifts, alert sound scheduling, manual log output and fractional-lot money management are not reproduced.
- Trailing step handling is simplified to StockSharp's built-in trailing stop manager.

## Usage notes
- Signals are generated only on finished candles, matching the "trade at bar close" mode of the original EA.
- Ensure that the instrument has a proper `PriceStep`; otherwise the strategy falls back to a step of 1.0 when converting point-based parameters.
- The strategy is intended for demonstration and can be extended with additional money-management rules if required.
