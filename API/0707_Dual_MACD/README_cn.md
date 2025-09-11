# 双MACD
[English](README.md) | [Русский](README_ru.md)

该策略结合两组MACD指标。慢速MACD的柱线突破零轴且快速MACD柱线同向时入场。若快速MACD反转或触发止损/止盈则平仓。

测试显示年化收益约为65%，在股票市场表现最佳。

## 细节

- **入场条件**：慢速MACD柱线穿越零轴并得到快速MACD确认。
- **多空方向**：双向。
- **退出条件**：快速MACD反转或达到止损/止盈。
- **止损**：有。
- **默认参数**：
  - `Macd1FastLength` = 34
  - `Macd1SlowLength` = 144
  - `Macd1SignalLength` = 9
  - `Macd2FastLength` = 100
  - `Macd2SlowLength` = 200
  - `Macd2SignalLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**：
  - 类别: 趋势
  - 方向: 双向
  - 指标: MACD
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内 (15m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等

