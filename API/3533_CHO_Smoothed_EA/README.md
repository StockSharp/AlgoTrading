# CHO Smoothed EA Strategy

## Overview
This strategy replicates the logic of the original "CHO Smoothed EA" Expert Advisor. It watches Chaikin Oscillator crossovers on completed candles and smooths the oscillator with a configurable moving average. Optional filters limit trading to a specific session, restrict trade direction, and validate signals with zero-line confirmation. When a signal is accepted the strategy sends a market order and manages the position using fixed distances in points for stop-loss, take-profit, and trailing protection.

## Trading Logic
- Chaikin Oscillator values are calculated on each finished candle with configurable fast and slow periods.
- A moving average of the oscillator creates the signal line. The moving average period and type can be adjusted.
- Long entries occur when the oscillator crosses above the smoothed line. Short entries occur on the opposite crossover. Signals can be reversed to trade against the original direction.
- If the zero-level filter is enabled both oscillator values must be below zero for long trades and above zero for short trades.
- The strategy can automatically close opposite positions before entering a new trade, or ignore signals until the current position is flat. It can also enforce a single-position mode.
- Trading can be restricted to a daily time window. Windows that cross midnight are supported.
- After an entry the strategy stores the entry price, converts the configured point distances into price offsets, and monitors candles for stop-loss, take-profit, and trailing-stop events.

## Risk Management
- Stop-loss and take-profit levels are calculated from the entry price using point distances multiplied by the instrument price step.
- The trailing stop activates after price advances by the configured trailing step and then follows at the trailing distance.
- When a protective level is hit the position is closed immediately with a market order and all risk levels are reset.

## Parameters
- **Candle Type** – timeframe used to build the candles for indicator calculations.
- **Fast Period / Slow Period** – Chaikin Oscillator fast and slow periods.
- **Signal MA Period / Signal MA Type** – smoothing moving average settings applied to the oscillator.
- **Use Zero Level** – require both oscillator values to be on the correct side of zero before trading.
- **Trade Mode** – allow long only, short only, or both directions.
- **Reverse Signals** – swap long and short entries.
- **Close Opposite** – close existing opposite positions before opening a new trade.
- **Only One Position** – prevent entries when a position is already open.
- **Use Time Control / Start Time / End Time** – enable and configure the daily trading window.
- **Stop Loss (pts)** – distance in points for the protective stop.
- **Take Profit (pts)** – distance in points for profit targets.
- **Trailing Stop (pts)** – trailing stop distance in points.
- **Trailing Step (pts)** – minimum favorable move (in points) before moving the trailing stop.

## Additional Notes
- Set the `Volume` property of the strategy before starting it to control trade size.
- Because the strategy issues market orders, ensure sufficient liquidity and consider slippage in live environments.
- When the trading window start and end times are equal the strategy remains inactive, matching the original Expert Advisor behaviour.
