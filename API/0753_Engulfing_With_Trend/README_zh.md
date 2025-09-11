# 趋势吞没策略
[English](README.md) | [Русский](README_ru.md)

该策略将 SuperTrend 方向过滤与看涨和看跌吞没形态结合。当新的蜡烛在当前趋势方向上吞没前一根蜡烛时开仓。止损和目标根据形态区间计算。

## 细节

- **入场条件**：吞没形态与 SuperTrend 方向一致。
- **多/空**：双向。
- **退出条件**：止损或止盈。
- **止损**：是，基于蜡烛极值和 ATR 偏移。
- **默认值**：
  - `CandleType` = 5 分钟
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3
  - `BoringThreshold` = 25
  - `EngulfingThreshold` = 50
  - `StopLevel` = 200
- **过滤条件**：
  - 类别: 形态
  - 方向: 双向
  - 指标: SuperTrend, K线形态
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等
