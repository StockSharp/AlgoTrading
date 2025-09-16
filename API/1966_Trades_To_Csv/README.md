# Trades To CSV Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy demonstrates how to export closed trade information to a CSV file while trading based on indicator signals. A buy or sell position is opened when the Commodity Channel Index (CCI) and the MACD histogram fall inside predefined ranges. When the opposite signal appears or profit targets are reached, the position is closed and the trade details are appended to a CSV file.

The CSV file records order side, profit or loss, ticket number, open and close prices, timestamps, symbol and volume.

## Details

- **Entry Criteria**:
  - **Long**: `CCI` between -125 and -42 and `MACD` histogram between -0.00114 and 0.00038.
  - **Short**: `CCI` between 125 and 208 and `MACD` histogram between -0.00038 and 0.00190.
- **Exit Criteria**:
  - Opposite signal.
  - Take profit or stop loss reached.
- **Stops**: Absolute take profit and stop loss values.
- **Default Values**:
  - `CandleType` = 1-minute candles
  - `TakeProfit` = 50
  - `StopLoss` = 50
  - `Volume` = 0.1
  - `CciPeriod` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `FileName` = `myfilename.csv`
- **Filters**:
  - Category: Indicators
  - Direction: Both
  - Indicators: CCI, MACD
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
