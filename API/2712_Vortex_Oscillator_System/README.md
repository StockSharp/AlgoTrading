# Vortex Oscillator System Strategy

## Overview
The Vortex Oscillator System is a direct port of the MetaTrader 5 expert advisor that relies on the Vortex Oscillator to capture sharp shifts between positive and negative directional movement. The oscillator is constructed as the spread between the Vortex positive line (VI+) and the Vortex negative line (VI-) calculated on the selected candle series. Deep negative readings indicate that VI- dominates VI+, while strong positive values show VI+ leadership. The strategy interprets those extremes as potential inflection zones and reacts with mean-reversion style entries backed by oscillator-driven exits.

## How the Strategy Works
1. Candles are built using the configured timeframe and fed to the built-in `VortexIndicator`.
2. Once the indicator becomes formed, the oscillator value is derived as `VI+ - VI-` on every finished candle.
3. The oscillator is compared against user-defined thresholds:
   - When it falls below the buy threshold, a long setup is detected.
   - When it rises above the sell threshold, a short setup is detected.
4. Optional filters can restrict long signals to the zone between the buy threshold and a dedicated stop-loss level (and vice versa for short signals).
5. Whenever a new setup appears, the strategy closes any opposite position and opens a trade in the signal direction with the configured volume.
6. Open positions are continuously monitored. If the oscillator hits the configured stop-loss or take-profit boundaries, the position is closed immediately.

This sequence reproduces the original MetaTrader logic: trades are evaluated only on completed bars, both directions are mutually exclusive, and oscillator-based protective rules govern exits.

## Entry Rules
- **Long entry**
  - Triggered when the oscillator is less than or equal to the buy threshold.
  - If the long stop-loss option is enabled, the oscillator must also remain above the long stop-loss level.
  - Any active short position is closed before opening the long trade.
- **Short entry**
  - Triggered when the oscillator is greater than or equal to the sell threshold.
  - If the short stop-loss option is enabled, the oscillator must also remain below the short stop-loss level.
  - Any active long position is closed before opening the short trade.
- If the oscillator value is between the buy and sell thresholds, all setups are cancelled and no position change occurs.

## Exit Rules
- **Long positions**
  - Close immediately when the oscillator crosses below or equals the long stop-loss level (if enabled).
  - Close immediately when the oscillator rises to or above the long take-profit level (if enabled).
- **Short positions**
  - Close immediately when the oscillator crosses above or equals the short stop-loss level (if enabled).
  - Close immediately when the oscillator falls to or below the short take-profit level (if enabled).

The exit checks are performed after every candle close, guaranteeing a faithful recreation of the MT5 monitoring loop.

## Parameters
- **Vortex Length** – lookback period for the Vortex indicator (default 14).
- **Candle Type** – timeframe used for building candles supplied to the indicator.
- **Use Buy Stop Loss** – enables the oscillator-based stop-loss filter and exit for long trades.
- **Use Buy Take Profit** – enables the oscillator-based take-profit exit for long trades.
- **Use Sell Stop Loss** – enables the oscillator-based stop-loss filter and exit for short trades.
- **Use Sell Take Profit** – enables the oscillator-based take-profit exit for short trades.
- **Buy Threshold** – oscillator value that qualifies a long entry (default -0.75).
- **Buy Stop Loss Level** – oscillator value that closes long positions when the long stop-loss option is active (default -1.00).
- **Buy Take Profit Level** – oscillator value that closes long positions when the long take-profit option is active (default 0.00).
- **Sell Threshold** – oscillator value that qualifies a short entry (default 0.75).
- **Sell Stop Loss Level** – oscillator value that closes short positions when the short stop-loss option is active (default 1.00).
- **Sell Take Profit Level** – oscillator value that closes short positions when the short take-profit option is active (default 0.00).
- **Volume** – trade size used for new positions (default 0.1, matching the original expert advisor).

## Implementation Notes
- Processing occurs strictly on completed candles to avoid duplicating signals within the same bar.
- Oscillator thresholds can be optimized thanks to the provided ranges in the parameter metadata.
- The strategy automatically flips positions by sending a market order large enough to close the opposing side and establish the new exposure.
- Stop-loss and take-profit features work independently; enabling one does not require the other.
