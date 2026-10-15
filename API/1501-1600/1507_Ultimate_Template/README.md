# Ultimate Strategy Template
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

RSI momentum strategy with two EMAs as a trend filter. Signals are evaluated on finished candles, while percentage take-profit and stop-loss protection remains active during the 80-candle signal cooldown.

## Details

- **Entry Criteria**: Long when RSI crosses above 50 while the fast EMA is above the slow EMA; short when RSI crosses below 50 while the fast EMA is below the slow EMA.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite RSI crossing, take profit, or stop loss.
- **Stops**: Percent stop loss and take profit.
- **Cooldown**: 80 finished candles after a signal entry or RSI exit; it does not disable take-profit or stop-loss protection.
- **Default Values**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
  - `CandleType` = 5 minutes
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: RSI, EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Medium
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
