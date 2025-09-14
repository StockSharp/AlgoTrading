# Ultra WPR Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy applies a Williams %R oscillator smoothed by two moving averages. The crossing of the fast and slow smoothed lines generates trading signals. A long position opens when the fast line rises above the slow line, and a short position opens when the fast line falls below the slow line.

The approach seeks to follow emerging momentum while capping risk with configurable take‑profit and stop‑loss levels.

## Details
- **Entry Criteria**:
  - **Long**: Fast line crosses above slow line
  - **Short**: Fast line crosses below slow line
- **Long/Short**: Both sides
- **Exit Criteria**:
  - **Long**: Exit when fast line crosses below slow line
  - **Short**: Exit when fast line crosses above slow line
- **Stops**: Yes, price based take‑profit and stop‑loss
- **Default Values**:
  - `CandleType` = TimeSpan.FromHours(4)
  - `WprPeriod` = 13
  - `FastLength` = 3
  - `SlowLength` = 53
  - `TakeProfit` = 0.2m
  - `StopLoss` = 0.1m
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Williams %R, Moving Average
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: H4
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

