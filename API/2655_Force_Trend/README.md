# Force Trend Strategy

## Overview
- Conversion of the MetaTrader 5 expert advisor **Exp_ForceTrend.mq5** located in `MQL/18817`.
- Uses the proprietary ForceTrend oscillator to detect transitions between bullish and bearish momentum.
- Implements the logic with StockSharp's high-level API, relying on candle subscriptions and built-in indicators instead of direct series access.

## ForceTrend indicator
- The indicator looks back over `Length` candles and measures the distance between the highest high and lowest low within that window.
- The mid price of the current candle is normalized within that range and smoothed twice:
  - The first stage produces an intermediate `force` value with coefficients `0.66` and `0.67`.
  - The second stage applies a logarithmic transform combined with a half-life smoothing to obtain the final ForceTrend value.
- Values above zero are treated as bullish (originally rendered in blue) and values below zero are bearish (rendered in magenta).

## Parameters
- `Length` – size of the ForceTrend lookback window; must remain positive.
- `SignalBar` – how many finished candles to shift the signal. `0` reacts to the most recent closed bar, `1` mimics the default MT5 setting by waiting for one extra bar, and larger values delay the execution even more.
- `EnableLongEntry` – if disabled the strategy will not open long positions on bullish transitions.
- `EnableShortEntry` – if disabled the strategy will not open short positions on bearish transitions.
- `EnableLongExit` – toggles whether bullish signals are allowed to close existing short positions.
- `EnableShortExit` – toggles whether bearish signals are allowed to close existing long positions.
- `CandleType` – timeframe of the candles used for indicator calculations.

## Trading rules
1. ForceTrend output is converted into a discrete direction (`+1`, `0`, `-1`).
2. Directions are stored in a fixed-length history so the strategy can compare the bar at `SignalBar` offset with the immediately preceding bar.
3. A bullish signal (`direction > 0`) triggers:
   - Closing any open short position if `EnableShortExit` is `true`.
   - Opening or reversing into a long position (market order sized as `Volume + |Position|`) when the previous direction was not bullish and `EnableLongEntry` is `true`.
4. A bearish signal (`direction < 0`) triggers the symmetric actions for long positions when `EnableLongExit`/`EnableShortEntry` are enabled.
5. Neutral ForceTrend readings inherit the last known direction so that the system does not oscillate between flat states.
6. Orders are submitted only when the strategy is fully formed, online, and trading is allowed by the StockSharp runtime.

## Implementation notes
- Candles are received through `SubscribeCandles(CandleType)`; indicator processing is performed in the `ProcessCandle` callback.
- The highest and lowest prices are obtained via StockSharp's `Highest` and `Lowest` indicators, ensuring that no manual buffer management or LINQ operations are required.
- Direction history is stored in a small fixed array sized according to `SignalBar` to reproduce the original MT5 behaviour without recreating collections for every tick.
- Position reversals use a single market order with volume equal to the sum of the desired exposure and the absolute current position, emulating the `BuyPositionOpen`/`SellPositionOpen` helpers from the MQL version.
- Money management parameters from the expert advisor (lot sizing, stop-loss and take-profit in points) are intentionally omitted; the StockSharp strategy relies on the user-configured `Volume` and optional external protection modules.
- The boolean toggles mirror the MT5 inputs (`BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose`).

## Usage hints
- Configure the `Volume` property before starting the strategy to control order size.
- Choose a candle type that matches the timeframe used during MT5 testing (default is four-hour candles).
- Combine with StockSharp risk/protection components if stop-loss or take-profit automation is required.

## Files
- Strategy implementation: `CS/ForceTrendStrategy.cs`
- Original MQL files: `MQL/18817/mql5/Experts/Exp_ForceTrend.mq5` and `MQL/18817/mql5/Indicators/ForceTrend.mq5`
