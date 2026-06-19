# 四柱动量反转策略
[English](README.md) | [Русский](README_ru.md)

四柱动量反转策略在选定时间窗口内，当收盘价连续 `BuyThreshold` 根K线低于 `Lookback` 根之前的收盘价时做多。一旦价格突破前一根K线的最高价，仓位被平掉。

## 细节

- **入场**: 在时间窗口内，连续 `BuyThreshold` 根收盘价低于 `Lookback` 根之前的收盘价。
- **出场**: 收盘价高于前一根K线最高价。
- **止损**: 无。
- **默认值**:
  - `BuyThreshold` = 4
  - `Lookback` = 4
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
