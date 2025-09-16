# Digital CCI Woodies 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于两个商品通道指数（CCI）的交叉。快速CCI对价格变化反应迅速，慢速CCI用于平滑噪音。当快速线与慢速线交叉时产生交易信号。

## 细节

- **入场条件**:
  - 多头：快速CCI上穿慢速CCI。
  - 空头：快速CCI下穿慢速CCI。
- **多/空**: 都支持。
- **出场条件**:
  - 当快速CCI下穿慢速CCI时平多。
  - 当快速CCI上穿慢速CCI时平空。
- **止损**: 无。
- **默认值**:
  - `CandleType` = 6小时K线
  - `FastLength` = 14
  - `SlowLength` = 6
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
- **筛选**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: CCI
  - 止损: 无
  - 复杂度: 低
  - 时间框架: 任意
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等
