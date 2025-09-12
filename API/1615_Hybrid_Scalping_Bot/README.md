# Hybrid Scalping Bot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A hybrid scalping system combining RSI signals with EMA trend filters and optional volume confirmation. The bot can adjust signal sensitivity from very easy to strong and includes quick exit and trailing stop features.

Testing indicates an average annual return of about 35%. It works best on liquid crypto pairs.

The strategy enters long or short based on RSI thresholds and candle strength, optionally filtered by trend and volume. Positions are protected with configurable take-profit, stop-loss and trailing logic, and daily trade limits reset at the start of each session.

## Details

- **Entry Criteria**:
  - **Buy**: RSI below 30 with bullish candle, optional trend/volume filters depending on sensitivity.
  - **Sell**: RSI above 70 with bearish candle, optional trend/volume filters.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Take profit, stop loss, trailing stop or quick RSI/EMA reversal.
- **Stops**: Yes, percentage-based SL/TP and optional trailing stop.
- **Filters**:
  - Trend and volume filters depending on configuration.
