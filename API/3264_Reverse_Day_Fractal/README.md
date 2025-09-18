# Reverse Day Fractal Strategy

## Overview
Reverse Day Fractal is a price action strategy that looks for sharp reversals after an intraday breakout. The algorithm analyses the last three finished candles. When the current bar forms a new extreme beyond the previous two candles and closes back in the opposite direction, it treats this pattern as a failed breakout and enters a reversal trade. Protective orders are managed through configurable take-profit, stop-loss and trailing-stop distances measured in price steps.

## Trading Logic
- **Bullish setup**:
  - The current finished candle makes a *lower* low than each of the two preceding candles.
  - The candle closes *above* its open price, indicating a bullish rejection of the new low.
  - When these conditions are met and the strategy is allowed to trade, it opens a long position. Optionally it can close an existing short first.
- **Bearish setup**:
  - The current finished candle makes a *higher* high than each of the two preceding candles.
  - The candle closes *below* its open price, indicating a bearish rejection of the new high.
  - When these conditions are satisfied it opens a short position, optionally closing an existing long first.
- **Position management**: the strategy can be configured to allow only one open position at a time (default behaviour). When disabled it will reverse an existing position by adding the volume required to change the direction.
- **Risk controls**: on start the strategy calls `StartProtection` to apply take-profit, stop-loss and trailing-stop protections using the configured point distances. When a trailing stop is enabled the protective stop will follow the price in discrete steps.

## Parameters
- `Trade Volume` – order volume for new entries.
- `Take Profit` – distance to the profit target measured in price steps. Set to zero to disable.
- `Stop Loss` – distance to the protective stop measured in price steps. Set to zero to disable.
- `Trailing Stop` – trailing stop distance in price steps. Set to zero to disable.
- `Trailing Step` – minimum movement (in steps) required before the trailing stop is adjusted.
- `Only One Position` – when enabled the strategy ignores new signals while a position is open.
- `Candle Type` – candle data type used for calculations (default: 1-hour time frame).

## Notes
- Signals are generated only on finished candles provided by the configured subscription.
- The strategy keeps the most recent two candle extremes in memory; therefore it needs at least two completed candles after start before it can generate a signal.
- Default parameter values replicate the original MQL4 expert advisor: 0.01 lot volume, 20-point stop loss, 10-point take profit, 25-point trailing stop and 5-point trailing step.
