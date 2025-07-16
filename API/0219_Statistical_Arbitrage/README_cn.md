# Statistical Arbitrage Strategy
[English](README.md) | [Русский](README_ru.md)

该统计套利策略根据两只相关资产相对于自身均线的位置进行交易，利用价差在短期内回归的特性。

当第一只资产低于其均线而第二只高于自身均线时做多第一只并做空第二只；反之则做空第一只做多第二只。第一只资产回到均线上方或下方时平仓，表明价差已恢复正常。

适合习惯于在两只工具之间保持中性敞口的交易者。内置的止损在价差持续扩大时限制回撤。

## 细节
- **入场条件**:
  - 多头: `Asset1 < MA1 && Asset2 > MA2`
  - 空头: `Asset1 > MA1 && Asset2 < MA2`
- **多/空**: 双向
- **离场条件**:
  - 多头: 当Asset1收盘价上穿MA1
  - 空头: 当Asset1收盘价下穿MA1
- **止损**: 对价差使用百分比止损
- **默认值**:
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**:
  - 类别: Arbitrage
  - 方向: 双向
  - 指标: Moving Averages
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 中等
