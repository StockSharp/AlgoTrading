# XIT Three MA Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp recreation of the MetaTrader 5 expert advisor **XIT_THREE_MA_CROSS.mq5**. It aligns three moving averages, checks MACD momentum separation, and sizes positions from ATR-based risk limits. The method is trend-following with momentum confirmation and targets medium-term swings on liquid currency pairs or indices.

## Overview

- **Market Regime**: Works best in instruments that trend for multiple candles on the selected timeframe.
- **Indicators**:
  - Slow, intermediate, and fast moving averages (user-selectable type) evaluated on the trading timeframe.
  - MACD (EMA-based) for momentum direction and distance between MACD and signal line.
  - Two ATR calculations (same length, independent timeframes) used to project stop-loss and take-profit distances.
- **Order Direction**: Bi-directional. The engine can open both long and short trades.
- **Position Sizing**: Calculated from the configured risk percentage and the ATR-based stop distance. When instrument metadata is incomplete, the strategy falls back to the default `Volume` property.

## Trading Logic

### Long Entry

A long position is opened when all conditions below are true on a finished candle:

1. MACD line increases compared to the previous bar (`MACD[t] > MACD[t-1]`).
2. MACD signal line increases compared to the previous bar.
3. The MACD line exceeds the signal line by at least `MacdTriggerPoints * PriceStep`.
4. Intermediate moving average rises vs the previous value.
5. Fast moving average rises vs the previous value.
6. Intermediate MA is above the slow MA.
7. Fast MA is above the intermediate MA.
8. Both ATR values are available to define stop and target distances.

### Short Entry

The short-side rules mirror the long setup with inverted comparisons:

1. MACD line decreases compared to the previous bar.
2. MACD signal line decreases compared to the previous bar.
3. The signal line is greater than the MACD line by at least `MacdTriggerPoints * PriceStep`.
4. Intermediate MA falls compared to the prior candle.
5. Fast MA falls compared to the prior candle.
6. Intermediate MA is below the slow MA.
7. Fast MA is below the intermediate MA.
8. Both ATR series have delivered a finished value.

### Exit Logic

- **Long positions** close when the fast MA drops below the intermediate MA, or price hits the ATR-based stop/take-profit levels.
- **Short positions** close when the fast MA crosses above the intermediate MA, or the ATR limits are touched.
- After closing a position, the algorithm waits for the next candle before evaluating new entries, matching the original EA behavior.

## Risk Management

- **Stop Loss**: Distance equals the latest ATR value from `AtrStopCandleType`. For longs the stop price is `Entry - ATR`, for shorts it is `Entry + ATR`.
- **Take Profit**: Distance equals the ATR value from `AtrTakeCandleType`. Targets are mirrored relative to entry price.
- **Risk Percent**: The strategy estimates the monetary loss per unit from the stop distance. If `PriceStep` and `PriceStepCost` are known, the risk per contract uses tick valuation. Otherwise, the raw price distance is used. Position size is `RiskPercent%` of the current portfolio value divided by the per-unit risk, rounded down to the nearest `VolumeStep`.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Primary timeframe for moving averages and MACD calculations. | 1-hour candles |
| `SlowMaLength` / `IntermediateMaLength` / `FastMaLength` | Periods of the moving averages. | 60 / 14 / 4 |
| `SlowMaType`, `IntermediateMaType`, `FastMaType` | Moving average families (Simple, Exponential, Smoothed, Weighted). | Simple |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD fast, slow, and signal EMA lengths. | 12 / 26 / 9 |
| `MacdTriggerPoints` | Minimum distance between MACD and its signal, measured in instrument points. Converted using `PriceStep`. | 7 |
| `AtrLength` | Period for both ATR indicators. | 14 |
| `AtrTakeCandleType` / `AtrStopCandleType` | Timeframes for take-profit and stop-loss ATR series. | 4-hour candles |
| `RiskPercent` | Percent of current portfolio value risked on each trade. | 10% |

## Usage Notes

1. Attach the strategy to a security with accurate `PriceStep`, `PriceStepCost`, and `VolumeStep` to obtain precise position sizing.
2. Ensure historical data is available for every subscribed timeframe (`CandleType`, `AtrTakeCandleType`, `AtrStopCandleType`). Missing ATR values will postpone entries.
3. The algorithm operates on fully closed candles and ignores intrabar fluctuations, mirroring the original MetaTrader logic of fetching current and previous indicator buffers.
4. Modify the moving-average types if the target market favors smoother or faster filters.

## Files

- `CS/XitThreeMaCrossStrategy.cs` – C# implementation with high-level StockSharp API, including ATR subscriptions and risk sizing.
- `README_ru.md` – Russian description of the strategy.
- `README_cn.md` – Chinese translation of the documentation.
