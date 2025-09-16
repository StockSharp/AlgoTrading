# Exp 2XMA Ichimoku Oscillator Strategy

This strategy reproduces the logic of the original MetaTrader expert advisor "Exp_2XMA_Ichimoku_Oscillator" by combining two Ichimoku-style price envelopes that are smoothed with configurable moving averages. The StockSharp implementation uses the high level strategy API and focuses on candle-based signal generation while keeping the position management rules of the source algorithm.

## Core Idea

1. Two Donchian-like midpoints are calculated on the selected timeframe:
   - The **fast midpoint** averages the highest high and lowest low across `UpPeriod1` and `DownPeriod1` bars.
   - The **slow midpoint** performs the same operation with `UpPeriod2` and `DownPeriod2` bars.
2. Each midpoint is smoothed by a moving average (`Method1`, `Method2`) of lengths `XLength1` and `XLength2`. The available smoothing methods are Simple, Exponential, Smoothed and Linear Weighted.
3. The oscillator value is the difference between the two smoothed midpoints. Four colour states describe its behaviour:
   - `PositiveRising` (0): oscillator is above zero and rising.
   - `PositiveFalling` (1): oscillator is above zero and losing momentum.
   - `NegativeRising` (3): oscillator is below zero but rising towards zero.
   - `NegativeFalling` (4): oscillator is below zero and dropping further.
   - `Neutral` (2) is assigned during warm-up.
4. Signals are evaluated using the colours of the bar located at `SignalBar` and the bar immediately before it (`SignalBar + 1`), which mirrors the buffer shifting in the MQL version.

## Trading Logic

- **Long entry**: allowed when `EnableBuyOpen` is true. If the older bar colour (`SignalBar + 1`) was rising (0 or 3) and the more recent bar (`SignalBar`) switched to a falling colour (1 or 4), the strategy closes any short position (`EnableSellClose`) and opens/extends a long position using `Volume + |Position|` units.
- **Short entry**: allowed when `EnableSellOpen` is true. If the older bar colour was falling (1 or 4) and the more recent bar switched to a rising colour (0 or 3), the strategy closes existing longs (`EnableBuyClose`) and opens/extends a short position with `Volume + |Position|` units.
- All executions happen on the close of the candle that generates the trigger. Orders are always market orders and the strategy does not apply additional stop-loss or take-profit levels; it relies solely on the colour transitions for exits.
- `StartProtection()` is enabled at start-up to use the framework’s built-in safety checks for unexpected positions.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Timeframe used for indicator calculations. | 4-hour candles |
| `UpPeriod1`, `DownPeriod1` | Lookback windows for the fast midpoint. | 6, 6 |
| `UpPeriod2`, `DownPeriod2` | Lookback windows for the slow midpoint. | 9, 9 |
| `XLength1`, `XLength2` | Smoothing lengths for the two moving averages. | 25, 80 |
| `Method1`, `Method2` | Moving average types (Simple, Exponential, Smoothed, Weighted). | Simple |
| `SignalBar` | Historical bar shift used to read oscillator colours. | 1 |
| `EnableBuyOpen`, `EnableSellOpen` | Toggle long/short entries. | true |
| `EnableBuyClose`, `EnableSellClose` | Toggle long/short exits. | true |
| `Volume` | Base trade size; existing positions are added to this value when reversing. | 1 |

## Usage Notes

- The moving average types cover the most common smoothing behaviours from the original expert. Advanced options such as custom XMA phase adjustments are not available in StockSharp and were replaced with standard indicators.
- Because the oscillator is calculated on closed candles, signals appear with the same one-bar delay that the MQL implementation used (`SignalBar = 1`). Increase `SignalBar` if you need additional confirmation bars.
- Consider combining the strategy with external risk management (portfolio manager, protective stops) when running on live markets, as exits depend exclusively on oscillator colour reversals.
