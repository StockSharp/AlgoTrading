# MACD Zero Cross
[Русский](README_ru.md) | [中文](README_cn.md)
 
This system trades momentum shifts when the Moving Average Convergence Divergence (MACD) histogram approaches the zero line. A rising MACD below zero or falling MACD above zero signals a potential reversal.

Testing indicates an average annual return of about 136%. It performs best in the stocks market.

The strategy waits for the MACD line to trend toward zero while still on the opposite side. Once momentum fades, it enters anticipating a swing in price.

Trades exit when MACD crosses its signal line or a stop-loss is triggered.

## Details

- **Entry Criteria**: MACD trending toward zero from either side.
- **Long/Short**: Both directions.
- **Exit Criteria**: MACD crosses signal line or stop.
- **Stops**: Yes.
- **Default Values**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

