# SR Rate Indicator Strategy

## Overview

This strategy is a C# port of the MetaTrader 5 expert **Exp_SR-RateIndicator**. It reproduces the original trading logic using Sto
ckSharp's high level API and a custom implementation of the SR Rate oscillator. The indicator measures how far the weighted candl
e price is located inside a smoothed support/resistance channel and paints a color code that highlights extreme readings.

The algorithm processes finished candles from a configurable time frame. Whenever the oscillator color jumps to the bullish or be
arish extreme the strategy closes any opposite position and opens a new trade in the direction of the signal. Protective stop los
s and take profit levels are applied with the same point distances used in the MetaTrader version.

## SR Rate Oscillator

The indicator calculates a Gaussian-smoothed band around price using a configurable window length:

1. For every bar, the high, low and weighted close are smoothed with one-sided Gaussian weights of length six.
2. The highest smoothed high and the lowest smoothed low over the window define a dynamic range.
3. The current smoothed weighted close is normalized inside that range and mapped to the `[-100, 100]` interval.
4. The final oscillator value is converted to five color states: `0` (strong bearish), `1` (soft bearish), `2` (neutral), `3` (soft
 bullish) and `4` (strong bullish).

A strong bullish color (`4`) indicates that price reached the upper extreme of the range, while a strong bearish color (`0`) signa
ls a visit to the lower extreme.

## Trading Rules

1. Subscribe to candles of the configured type and calculate the SR Rate oscillator on every finished bar.
2. Shift the signal evaluation by `SignalBar` closed candles (default: one bar back) to mimic the Expert Advisor behaviour.
3. When the shifted color becomes `4` and the previous color is below `4`:
   - Close any existing short position if long exits are enabled.
   - Open a new long position if long entries are enabled and no other position is active.
4. When the shifted color becomes `0` and the previous color is above `0`:
   - Close any existing long position if short exits are enabled.
   - Open a new short position if short entries are enabled and no other position is active.
5. Only one position can be open at any time. New signals are ignored until the previous trade is closed.
6. Optional stop loss and take profit levels are expressed in price points and automatically converted to absolute prices using th
e security price step.

## Parameters

| Name | Description |
|------|-------------|
| `OrderVolume` | Trade volume used for every market order. |
| `EnableLongEntries` | Enable/disable opening of long positions. |
| `EnableShortEntries` | Enable/disable opening of short positions. |
| `EnableLongExits` | Close long positions when a strong bearish color appears. |
| `EnableShortExits` | Close short positions when a strong bullish color appears. |
| `StopLossPoints` | Stop loss distance in instrument points (converted using the price step). |
| `TakeProfitPoints` | Take profit distance in instrument points (converted using the price step). |
| `SlippagePoints` | Maximum tolerated slippage when closing positions. Preserved for compatibility; no explicit slippage control is a
pplied by the high level API. |
| `CandleType` | Candle type and timeframe used to calculate the indicator. |
| `SignalBar` | Number of closed candles to shift the signal evaluation (default 1). |
| `WindowSize` | Length of the rolling window used by the SR Rate normalization. |
| `HighLevel` | Oscillator level that defines the bullish extreme (default +20). |
| `LowLevel` | Oscillator level that defines the bearish extreme (default -20). |

## Notes

- The strategy works with any instrument that supplies standard OHLC candles.
- Signals are processed only on finished candles; intrabar recalculations are ignored just like in the MetaTrader implementation.
- Slippage handling in the original expert relied on execution settings. StockSharp's market orders already honour exchange rules,
 therefore the `SlippagePoints` parameter is kept only for documentation purposes.
- The indicator stores only the minimal amount of history required to evaluate the window, preventing unnecessary memory usage.
- Python version is intentionally omitted according to the project guidelines.
