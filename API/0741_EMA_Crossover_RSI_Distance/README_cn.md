# EMA 交叉结合 RSI 与距离策略
[English](README.md) | [Русский](README_ru.md)

该策略使用多条 EMA 和 RSI 产生多空信号，并通过检查快速 EMA 之间的距离来确认趋势强度。

## 细节

- **入场条件**：
  - EMA5 高于 EMA13。
  - EMA40 高于 EMA55。
  - RSI 高于 50 且高于其 SMA。
  - EMA5 与 EMA13 之间的距离高于其平均值，且 EMA40-EMA13 的距离在增加。
  - 收盘价高于 EMA5。
- **多空方向**：多头和空头。
- **出场条件**：
  - 信号变为中性或反向。
- **止损**：无。
- **默认值**：
  - `EmaShortLength` = 5
  - `EmaMediumLength` = 13
  - `EmaLong1Length` = 40
  - `EmaLong2Length` = 55
  - `RsiLength` = 14
  - `RsiAverageLength` = 14
  - `DistanceLength` = 5
- **筛选**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：EMA、RSI
  - 止损：否
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
