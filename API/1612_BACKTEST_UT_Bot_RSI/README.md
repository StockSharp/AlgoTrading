# Backtest UT Bot + RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines a UT Bot trend detector with RSI levels. Enters long on a bullish UT Bot reversal when RSI is oversold and short on a bearish reversal when RSI is overbought.

## Details

- **Entry Criteria**:
  - **Long**: UT Bot turns up and RSI < `RSI Oversold`.
  - **Short**: UT Bot turns down and RSI > `RSI Overbought`.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Take profit or stop loss percentages.
- **Stops**: Take Profit & Stop Loss.
- **Default Values**:
  - `RSI Length` = 14
  - `RSI Overbought` = 60
  - `RSI Oversold` = 40
  - `ATR Length` = 10
  - `UT Bot Factor` = 1.0
  - `Take Profit %` = 3.0
  - `Stop Loss %` = 1.5
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
