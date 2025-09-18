# Trading Criteria Strategy

## Overview

Trading Criteria Strategy is a multi-timeframe trend-following approach converted from the original MQL4 "Trading Criteria" expert advisor. The port relies on linear weighted moving averages, momentum deviation filters, and MACD confirmations drawn from both trend and monthly timeframes. Risk management features include trailing stops, break-even protection, and configurable stop-loss/ take-profit targets.

## Entry Logic

1. **Primary timeframe**: Uses a fast and slow linear weighted moving average (LWMA). Long signals require the fast MA to stay above the slow MA; short signals require the opposite.
2. **Momentum filter**: Calculates momentum deviation (|Momentum-100|) on the trend timeframe and checks the three most recent values against bullish or bearish thresholds.
3. **Trend MACD filter**: Evaluates the MACD main line relative to its signal line on the same trend timeframe. Signals only trigger when the current relationship aligns with the previous bar to avoid rapid flip-flopping.
4. **Monthly MACD filter**: Confirms the larger directional bias using MACD on a monthly (or user-specified slow) timeframe.
5. **Position exposure**: Limits the maximum net position size to `MaxPositions * Volume`. If a new signal appears while holding an opposite position, the strategy will first neutralize the exposure by buying or selling enough volume.

## Exit and Risk Management

- **Stop Loss / Take Profit**: Set via `StopLossPoints` and `TakeProfitPoints`, converted into actual price offsets using the normalized pip size of the instrument.
- **Trailing stop**: Enabled with `EnableTrailing` and `TrailingStopPoints`. For longs, the stop tracks the highest price minus the trailing distance once the move exceeds the threshold; shorts mirror the logic using the lowest price.
- **Break-even move**: When enabled (`EnableBreakEven`), the stop migrates to the entry price plus an optional offset once the close price reaches `BreakEvenTriggerPoints` distance in favor of the open position.
- **Manual protective exits**: If the candle touches the calculated stop or target levels, the strategy closes the entire net position on that bar.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Base timeframe for signal generation and moving averages. |
| `TrendCandleType` | Timeframe used for momentum and MACD filters. |
| `MonthlyCandleType` | Slow timeframe providing long-term MACD confirmation. |
| `FastMaPeriod` / `SlowMaPeriod` | Lengths of the fast and slow LWMAs on the entry timeframe. |
| `MomentumPeriod` | Momentum lookback period on the trend timeframe. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Minimum deviation from 100 required for long or short entries. |
| `MaxPositions` | Maximum number of base lots that can remain open simultaneously. |
| `StopLossPoints` / `TakeProfitPoints` | Distances, in points, for protective stops and profit targets. |
| `EnableTrailing` / `TrailingStopPoints` | Activates trailing stops and sets their distance. |
| `EnableBreakEven` | Toggles break-even behavior. |
| `BreakEvenTriggerPoints` / `BreakEvenOffsetPoints` | Controls how far price must move before the stop shifts to break-even and how much offset to apply. |

## Usage Notes

- Attach the strategy to an instrument with proper candle series support for the selected timeframes.
- Ensure the security provides an accurate `PriceStep`; the implementation adjusts fractional pip instruments (3 or 5 decimal places) to match MQL conventions.
- Trailing and break-even protections operate on completed candles. In fast markets, protective levels may execute on the following bar when a gap occurs.
- The default parameter set mirrors the published MQL inputs, but they can be optimized via the built-in parameter metadata.
