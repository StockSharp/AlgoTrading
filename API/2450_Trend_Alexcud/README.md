# Trend Alexcud
[Русский](README_ru.md) | [中文](README_cn.md)

The Trend Alexcud strategy searches for strong directional movement by aligning multiple simple moving averages and the Accelerator Oscillator across three timeframes. It was converted from the original MQL5 expert "TREND_alexcud v_2".

The system watches three timeframes (default 15 minutes, 1 hour, 4 hours). On each timeframe it computes five simple moving averages (periods 5, 8, 13, 21, 34) and the Accelerator Oscillator. A timeframe is considered bullish when the closing price is above all moving averages and the Accelerator is positive. A timeframe is bearish when the closing price is below all moving averages and the Accelerator is negative.

A trade is opened only when all three timeframes agree. If they are simultaneously bullish the strategy buys, while a common bearish reading triggers a sell. The position is reversed whenever the opposite signal appears. Protective orders are managed through StockSharp's built-in risk system.

## Details

- **Entry Criteria**
  - **Long**: Price above all MAs and Accelerator > 0 on each timeframe.
  - **Short**: Price below all MAs and Accelerator < 0 on each timeframe.
- **Long/Short**: Both directions.
- **Exit Criteria**: Position reverses when the opposite signal forms.
- **Stops**: Uses built-in protection (no default values).
- **Default Values**:
  - Timeframe1 = 15m, Timeframe2 = 1h, Timeframe3 = 4h
  - MA periods = 5, 8, 13, 21, 34
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
