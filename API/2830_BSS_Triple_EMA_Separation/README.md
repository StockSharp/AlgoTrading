# BSS Triple EMA Separation Strategy

## Overview

The **BSS Triple EMA Separation Strategy** is a StockSharp port of the MetaTrader 5 expert advisor "BSS 1_0" (MQL ID 20591). The approach monitors three moving averages with increasing lookback windows and waits for them to fan out by at least a configurable distance. When the fast, medium and slow averages are properly separated, the strategy enters in the direction of the trend while respecting a cooldown between fills and a cap on the total position size.

This implementation keeps the core behaviour of the original robot while exposing the configuration through StockSharp `StrategyParam` objects. All comments and documentation are written in English as requested.

## Trading Logic

1. Subscribe to a single candle stream defined by the `CandleType` parameter and calculate three moving averages (fast, medium, slow). Each average can use a different smoothing method (simple, exponential, smoothed, or linear weighted).
2. For a **long setup** the following conditions must be met on a finished candle:
   - `Slow MA - Medium MA >= MinimumDistance`.
   - `Medium MA - Fast MA >= MinimumDistance`.
3. For a **short setup** the inverse separation is required:
   - `Fast MA - Medium MA >= MinimumDistance`.
   - `Medium MA - Slow MA >= MinimumDistance`.
4. Before opening a trade the strategy ensures:
   - All indicators are fully formed and the strategy is allowed to trade (`IsFormedAndOnlineAndAllowTrading`).
   - The pause since the last entry (`MinimumPauseSeconds`) has elapsed.
   - Adding a new lot will not violate the `MaxPositions` exposure limit.
5. On an entry signal the strategy first closes any open position in the opposite direction. Only after the next candle does it consider opening a position in the new direction, mirroring the behaviour of the original MQL EA.
6. When a new position is opened or scaled in, the fill time is stored to enforce the cooldown between entries.

No automatic stop-loss or take-profit is used. Risk management is achieved through the distance filter, the pause between trades, and the maximum number of lots allowed per direction.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | 0.1 | Volume used for each entry order. The net position is limited to `OrderVolume * MaxPositions`. |
| `MaxPositions` | 2 | Maximum number of lots (per direction) that can be held simultaneously. |
| `MinimumDistance` | 0.0005 | Minimum price gap required between neighbouring moving averages. Choose a value appropriate for the instrument (for a 5-digit FX pair, 0.0005 equals 5 pips). |
| `MinimumPauseSeconds` | 600 | Cooldown in seconds between new entries. Closing trades does not reset the timer; only entries do. |
| `FirstMaPeriod` | 5 | Period of the fastest moving average. Must be strictly less than `SecondMaPeriod`. |
| `FirstMaMethod` | Exponential | Smoothing method used for the fast moving average (Simple, Exponential, Smoothed, LinearWeighted). |
| `SecondMaPeriod` | 25 | Period of the medium moving average. Must be strictly less than `ThirdMaPeriod`. |
| `SecondMaMethod` | Exponential | Smoothing method used for the medium moving average. |
| `ThirdMaPeriod` | 125 | Period of the slow moving average. |
| `ThirdMaMethod` | Exponential | Smoothing method used for the slow moving average. |
| `CandleType` | 1-minute time frame | Candle data source used for indicator calculations and signal evaluation. |

## Implementation Notes

- High-level StockSharp API is used: `SubscribeCandles` streams data, and `.Bind` feeds the moving averages and the signal handler simultaneously.
- The moving averages are instantiated on strategy start according to the selected methods. The default configuration matches the original EA (three exponential MAs on closing prices).
- `StartProtection()` is invoked to enable the built-in position monitoring tools provided by StockSharp.
- The strategy overrides `OnPositionChanged` to timestamp entries. This timestamp is compared against `MinimumPauseSeconds` to maintain the cooldown behaviour of the MetaTrader version.
- Opposite positions are flattened before new ones are considered, ensuring that the net exposure never flips without first going through zero, just like the original implementation where all short positions were closed before opening longs.

## Usage Guidelines

1. Select an instrument and ensure its tick size is reflected in the `MinimumDistance` value. For example:
   - EURUSD (5-digit pricing): `0.0005` equals 5 pips.
   - USDJPY (3-digit pricing): `0.05` equals 5 pips.
2. Adjust the moving average periods and methods to fit the market regime you are targeting.
3. Increase `MinimumPauseSeconds` on slower time frames to avoid over-trading, or decrease it on lower time frames if the market structure allows frequent entries.
4. Test different `MaxPositions` values in combination with your broker’s contract size to align the exposure with your risk plan.

## Limitations Compared to the MQL Version

- The MetaTrader expert allowed selecting alternative price sources (open, high, low, etc.). The StockSharp port currently operates on closing prices only, which matches the default configuration of the original robot.
- The port uses a net-position model (positive for longs, negative for shorts). When `MaxPositions` is reached no additional lots are added until the exposure is reduced, reproducing the effect of the original per-position counter.

With these considerations you can reproduce the behaviour of the original BSS strategy inside the StockSharp ecosystem and extend it with additional risk controls or analytics as needed.
