# Fibo iSAR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines fast and slow Parabolic SAR indicators with Fibonacci retracement levels. When the fast SAR lies above the slow SAR and below the price, the strategy expects an uptrend and places a buy limit order at the 50% Fibonacci retracement of the recent range. The stop loss is placed below the swing low and the take profit at the 161% extension. For a downtrend the logic is mirrored with sell limit orders.

## Details

- **Entry Criteria**: Trend direction from fast/slow SAR; entry on 50% Fibonacci retracement.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss or take profit levels.
- **Stops**: Yes.
- **Default Values**:
  - `StepFast` = 0.02
  - `MaximumFast` = 0.2
  - `StepSlow` = 0.01
  - `MaximumSlow` = 0.1
  - `CountBarSearch` = 3
  - `IndentStopLoss` = 30
  - `FiboEntranceLevel` = 50
  - `FiboProfitLevel` = 161
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic SAR, Fibonacci
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
