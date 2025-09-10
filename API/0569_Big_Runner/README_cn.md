# Big Runner 策略
[English](README.md) | [Русский](README_ru.md)

Big Runner 策略在收盘价和快速 SMA 同时与慢速 SMA 发生交叉时进行交易，表示强劲的动量。仓位大小根据账户资产百分比并乘以杠杆计算。可选的止损和止盈用于风险控制。

## 详情

- **入场条件**:
  - 当价格上穿快速 SMA 且快速 SMA 上穿慢速 SMA 时做多。
  - 当价格下穿快速 SMA 且快速 SMA 下穿慢速 SMA 时做空。
- **方向**: 多头和空头。
- **出场条件**:
  - 可选的止损和止盈基于入场价。
  - 反向信号关闭现有仓位。
- **止损**: 可配置的止损和止盈百分比。
- **默认值**:
  - `FastLength` = 5
  - `SlowLength` = 20
  - `TakeProfitLongPercent` = 4
  - `TakeProfitShortPercent` = 7
  - `StopLossLongPercent` = 2
  - `StopLossShortPercent` = 2
  - `PercentOfPortfolio` = 10
  - `Leverage` = 1
- **过滤器**:
  - 分类: 趋势跟随
  - 方向: 多头和空头
  - 指标: SMA
  - 止损: 有
  - 复杂度: 低
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
