# Calculation Position Size Based on Risk Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Demonstrates sizing trades from account risk and a stop-loss percentage. Entries are random to show position sizing logic.

## Details

- **Entry Criteria**:
  - **Long**: every 333rd bar.
  - **Short**: every 444th bar.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Stop loss only.
- **Stops**: Stop Loss.
- **Default Values**:
  - `Stop Loss %` = 10
  - `Risk Value` = 2
  - `Risk Is Percent` = true
  - `Long Period` = 333
  - `Short Period` = 444
- **Filters**:
  - Category: Risk Management
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
