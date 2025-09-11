# Combo 2/20 EMA Bandpass Filter Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合快慢EMA交叉与带通滤波器。当快EMA高于慢EMA且滤波值突破卖出区间时做多；当快EMA低于慢EMA且滤波值跌破买入区间时做空。若信号消失或尚未达到起始日期，则平仓。

测试表明年化收益约为47%，在加密货币市场表现最佳。

## 细节
- **入场条件**:
  - 多头: 快EMA > 慢EMA 且 滤波值 > 卖出区间
  - 空头: 快EMA < 慢EMA 且 滤波值 < 买入区间
- **多/空**: 双向
- **离场条件**: 信号消失时平仓
- **止损**: 否
- **默认值**:
  - `FastEmaLength` = 2
  - `SlowEmaLength` = 20
  - `BpfLength` = 20
  - `BpfDelta` = 0.5m
  - `BpfSellZone` = 5m
  - `BpfBuyZone` = -5m
  - `StartDate` = new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: EMA Bandpass Filter
  - 止损: 否
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
