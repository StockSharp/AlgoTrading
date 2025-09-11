# Prop Firm Business Simulator
[English](README.md) | [Русский](README_ru.md)

该策略使用基于风险的仓位管理，在 Keltner 通道突破时入场。仓位数量根据账户权益的固定风险百分比计算。

在通道上下轨挂入止损单。数量计算使得通道宽度等于所设定的风险比例。

## 详情
- **入场条件**: 价格突破 Keltner 通道。
- **多空方向**: 双向。
- **退出条件**: 突破相反通道。
- **止损**: 是。
- **默认值**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 10
  - `Multiplier` = 2m
  - `RiskPerTrade` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 突破
  - 方向: 双向
  - 指标: Keltner, ATR
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
