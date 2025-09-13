# Genie Pivot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements the "Genie" pivot point reversal scalping system originally written in MQL4. It scans the last eight candles to detect sudden reversals at pivot points. A long trade is triggered when seven consecutive lows decrease and the current candle makes a higher low while closing above the previous high. A short trade is triggered when seven consecutive highs increase and the current candle makes a lower high while closing below the previous low.

The strategy uses a fixed position size (Strategy.Volume) and applies both a trailing stop and a take-profit measured in absolute price units. These parameters can be optimized and allow the method to capture quick reversals while protecting open profits.

## Details

- **Entry Criteria**:
  - **Long**: `Low[7] > Low[6] > ... > Low[1]` && `Low[1] < Low[0]` && `High[1] < Close[0]`.
  - **Short**: `High[7] < High[6] < ... < High[1]` && `High[1] > High[0]` && `Low[1] > Close[0]`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Trailing stop or take-profit is hit.
- **Stops**:
  - Take-profit: absolute distance from entry.
  - Trailing stop: absolute distance, trailing as the trade moves in favor.
- **Default Values**:
  - `TakeProfit` = 500.
  - `TrailingStop` = 200.
  - `CandleType` = 1 minute.
- **Filters**:
  - Category: Reversal.
  - Direction: Both.
  - Indicators: None.
  - Stops: Yes.
  - Complexity: Simple.
  - Timeframe: Short-term.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Medium.
