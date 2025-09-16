# Engulfing Momentum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **ENGULFING** MetaTrader expert advisor to the StockSharp high-level API. It combines a bullish/bearish engulfing pattern on the working timeframe with higher-timeframe momentum confirmation and a monthly MACD trend filter. Risk management reproduces the original break-even and trailing behaviour using stop distances measured in instrument steps.

## How it Works

1. **Candlestick Pattern** – the latest finished candle must engulf the previous bar in the direction of the trade. The strategy also checks that the bar two periods ago overlaps the prior bar, mirroring the original fractal-based confirmation.
2. **Trend Filter** – fast and slow *weighted* moving averages (LWMA analogue) gate entries. Long trades require the fast average to trade above the slow average and vice versa for shorts.
3. **Momentum Filter** – a 14-period momentum indicator calculated on a higher timeframe must deviate from the neutral level (100) by at least the configured threshold on any of the last three values. This reproduces the `MomLevelB/MomLevelS` checks from the MQL code.
4. **MACD Filter** – a monthly (30-day) MACD series must show the main line above the signal line for longs and below for shorts, just like the `MacdMAIN0` vs `MacdSIGNAL0` comparison in the EA.
5. **Order Handling** – the strategy always flips the position when an opposite signal appears. Protective logic closes trades whenever stop, target, break-even or trailing rules fire.

## Risk Management

- **Stop Loss / Take Profit** – distances are configured in instrument steps (ticks). They mirror the `Stop_Loss` and `Take_Profit` inputs of the original EA.
- **Trailing Stop** – optional trailing measured in steps. The stop follows the best price achieved after entry.
- **Break-Even Move** – once price advances by `BreakEvenTriggerSteps`, the stop is moved to the entry plus `BreakEvenOffsetSteps`, reproducing the "no loss" feature (`USEMOVETOBREAKEVEN`).

Money-based targets from the MQL script (`Use_TP_In_Money`, `Take_Profit_In_percent`) are intentionally omitted to keep the logic consistent with StockSharp's unit system. Percentage or currency-based exits can be recreated by adjusting the stop/take parameters.

## Parameters

| Parameter | Description |
| --- | --- |
| `FastMaPeriod` / `SlowMaPeriod` | Lengths of the weighted moving averages used for trend confirmation. |
| `MomentumPeriod` | Momentum length on the higher timeframe. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Minimum absolute deviation from 100 required for the momentum filter. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD configuration applied to `MacdCandleType`. |
| `StopLossSteps`, `TakeProfitSteps` | Protective stop and target distances in price steps. Set to zero to disable. |
| `TrailingStopSteps` | Optional trailing stop distance (0 disables trailing). |
| `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Distance required before moving the stop to break-even and the offset applied. |
| `CandleType` | Primary timeframe where engulfing patterns are evaluated. |
| `HigherCandleType` | Higher timeframe used for the momentum filter (defaults to 1 hour). |
| `MacdCandleType` | Timeframe for the MACD trend filter (defaults to 30 days ≈ monthly). |

## Usage

1. Attach the strategy to a security and set `CandleType`, `HigherCandleType`, and `MacdCandleType` to match your preferred timeframes.
2. Adjust the MA and momentum parameters if you want to align with a different market structure.
3. Configure stop, take profit, trailing and break-even distances in price steps that correspond to your instrument's tick size.
4. Start the strategy; it will subscribe to all necessary candle feeds automatically and begin evaluating signals once indicators are formed.

## Notes & Differences from the Original EA

- Weighted moving averages replicate the LWMA calculations used in MQL without manually iterating over prices.
- Break-even and trailing logic is applied on completed candles, matching the bar-by-bar approach of the EA while leveraging StockSharp protection helpers.
- Money-based trailing and percentage-based exits are not ported because StockSharp operates on instrument units; equivalent behaviour can be achieved by calibrating the step-based parameters.
- The strategy assumes one position at a time, which is the common usage scenario of the source EA even though it exposed a `Max_Trades` input.

Tune the thresholds and timeframes to match the asset you are trading. Higher-volatility instruments often require larger step distances or wider momentum thresholds to avoid premature exits.
