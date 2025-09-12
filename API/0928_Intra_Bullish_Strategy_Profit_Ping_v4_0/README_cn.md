# Intra Bullish Strategy - Profit Ping v4.0 策略
[English](README.md) | [Русский](README_ru.md)

仅做多系统，使用EMA金叉并由MACD柱状图和RSI强度确认。

## 细节

- **入场条件**：
  - 短期EMA上穿长期EMA
  - MACD柱状图 > 0
  - RSI > 50
  - 收盘价高于开盘价
- **出场条件**：
  - 短期EMA下穿长期EMA
  - MACD柱状图 < 0
  - RSI < 50
  - 收盘价低于开盘价
- **指标**：
  - 指数移动平均
  - MACD
  - RSI
- **止损**：无。
- **默认值**：
  - `ShortEmaLength` = 7
  - `LongEmaLength` = 14
  - `RsiLength` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
- **过滤器**：
  - 趋势跟随
  - 单一时间框架
  - 指标：EMA、MACD、RSI
  - 止损：无
  - 复杂度：低
