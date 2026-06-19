# Parabolic SAR Early Buy MA Exit策略
[English](README.md) | [Русский](README_ru.md)

该策略利用Parabolic SAR的反转信号进行交易，当SAR翻到价格上方且收盘价低于`MaPeriod`期的移动平均线时，提前平掉多头头寸。

## 细节

- **入场条件**：
  - 价格与Parabolic SAR发生交叉。
- **方向**：多头和空头。
- **出场条件**：
  - 多头：SAR高于价格且收盘价低于MA (`MaPeriod`)。
  - 空头：相反的SAR交叉（由入场逻辑处理）。
- **止损**：无。
- **默认参数**：
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `MaPeriod` = 11
- **过滤器**：
  - 类型：趋势跟随
  - 方向：多空
  - 指标：Parabolic SAR, SMA
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：低
