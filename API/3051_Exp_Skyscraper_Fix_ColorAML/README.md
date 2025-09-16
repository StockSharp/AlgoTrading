# Exp Skyscraper Fix ColorAML Strategy

## Overview
This strategy recreates the MetaTrader 5 expert advisor **Exp_Skyscraper_Fix_ColorAML** inside the StockSharp framework. It merges
two independent signal generators:

1. **Skyscraper Fix** – an ATR driven channel that paints bullish or bearish regimes depending on the direction of the adaptive
   bands.
2. **ColorAML** – an adaptive market level oscillator that compares local fractal ranges to detect expansion or contraction
   phases.

The original MQL implementation managed two separate magic numbers and could hold hedged positions simultaneously. StockSharp
strategies operate on a net position, therefore conflicting signals simply offset each other and the latest entry defines the
exposure. The README highlights these differences so users can align expectations when backtesting or trading the converted
variant.

## Parameters
### Skyscraper Fix module
- **SkyscraperCandleType** – timeframe used to build the Skyscraper Fix indicator. Default: `4h` candles.
- **SkyscraperEnableLongEntry / SkyscraperEnableShortEntry** – allow the module to open long or short positions.
- **SkyscraperEnableLongExit / SkyscraperEnableShortExit** – allow the module to close open trades in the corresponding
  direction.
- **SkyscraperLength** – number of ATR samples used to determine the stair-step size. Default: `10` bars.
- **SkyscraperMultiplier** – coefficient applied to the ATR based step. Default: `0.9`.
- **SkyscraperPercentage** – optional percentage offset applied to the midline (0 disables the shift).
- **SkyscraperMode** – chooses between High/Low or Close based channel construction.
- **SkyscraperSignalBar** – number of completed candles to look back when reading the color buffer. Values must be at least `1`.
- **SkyscraperVolume** – market order volume requested on each entry.
- **SkyscraperStopLoss / SkyscraperTakeProfit** – protective distances expressed in price steps.

### ColorAML module
- **ColorAmlCandleType** – timeframe used by the ColorAML oscillator. Default: `4h` candles.
- **ColorAmlEnableLongEntry / ColorAmlEnableShortEntry** – enable new long or short entries.
- **ColorAmlEnableLongExit / ColorAmlEnableShortExit** – enable closing orders for the respective direction.
- **ColorAmlFractal** – length of the fractal range used to build the adaptive levels. Default: `6` bars.
- **ColorAmlLag** – lag parameter that controls the exponential smoothing. Default: `7`.
- **ColorAmlSignalBar** – number of completed candles to inspect in the color buffer.
- **ColorAmlVolume** – order volume for ColorAML driven entries.
- **ColorAmlStopLoss / ColorAmlTakeProfit** – protective distances in price steps.

## Trading Logic
The strategy subscribes to the requested candle series for each module and evaluates only finished candles. Both indicators are
implemented in C# following the mathematical definitions used by the original MQL code:

- **Skyscraper Fix** computes a SuperTrend-like channel. When the color buffer turns **teal (0)** the module closes any short
  exposure (if allowed) and, when the previous color was different, prepares a long entry. When the buffer flips to **firebrick
  (1)** it closes longs and schedules a short entry.
- **ColorAML** compares fractal ranges to build an adaptive level line. Color `2` signals bullish expansion, closing shorts and
  optionally opening longs. Color `0` signals bearish contraction, closing longs and optionally opening shorts. Neutral `1` keeps
  the current stance.

Each entry uses market orders sized as `ConfiguredVolume + |current position|`. This ensures that a reversal order simultaneously
closes the opposite exposure and establishes the new position when hedging is not available.

## Risk Management
`StartProtection()` is activated on start. Whenever a module opens a new position the strategy stores the entry price and
calculates stop-loss and take-profit levels using the module specific settings. Subsequent candles trigger exits if their high or
low pierces the configured thresholds. Setting the distances to zero disables the protective logic.

## Implementation Notes
- The Skyscraper Fix and ColorAML calculations were ported directly and run on internal candle buffers. No external indicators
  need to be added manually to the strategy.
- StockSharp maintains a single net position per strategy. As a result simultaneous long and short trades from the original EA
  net out. Users who relied on hedging should be aware of this difference.
- Only completed candles are processed. `SignalBar` must be at least `1`; intrabar (tick-by-tick) evaluation is not reproduced.
- Stops are enforced by monitoring candle extremes rather than server-side orders, which matches the behaviour of the converted
  framework.

## Usage
1. Attach the strategy to the desired security and portfolio.
2. Configure the parameters for both modules, aligning the candle types with available data.
3. Start the strategy. It will automatically subscribe to the necessary candles, calculate indicator colors, and place market
   orders according to the module signals.
4. Monitor the log or charts to observe regime changes, manual risk management events, and executed trades.
