# Kalman Filter Signal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses Kalman Filter indicator to detect direction changes. The filter output is compared either with price or its slope depending on selected signal mode. When the signal turns bullish, the strategy opens a long position; when bearish, opens a short. Positions are reversed on opposite signals. Stop loss and take profit are applied using absolute distances.

## Details

- **Entry Criteria**:
  - Long: signal changes to bullish
  - Short: signal changes to bearish
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: Absolute stop loss and take profit
- **Default Values**:
  - `ProcessNoise` = 1.0
  - `MeasurementNoise` = 1.0
  - `CandleType` = TimeSpan.FromHours(3).TimeFrame()
  - `Mode` = SignalMode.Kalman
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Kalman Filter
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
