# MACD模式交易者
[English](README.md) | [Русский](README_ru.md)

该策略在MACD的剧烈反转时开仓。它寻找在极小中间值周围出现的两个巨大峰值。当前一根MACD为正且当前值大幅跌入负区时做空；当条件相反时做多。止损和止盈基于最近的高点和低点。

该算法适用于动量快速反转的高波动市场。仅使用市价单，并依据历史K线计算风险水平。

## 详情

- **入场条件**: 基于 `RatioThreshold` 的MACD峰值比例。
- **多空方向**: 双向。
- **退出条件**: 在最近极值加偏移处止损或相反峰值出现。
- **止损**: 是。
- **默认值**:
  - `FastEmaPeriod` = 24
  - `SlowEmaPeriod` = 13
  - `StopLossBars` = 22
  - `TakeProfitBars` = 32
  - `OffsetPoints` = 40
  - `RatioThreshold` = 5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 模式
  - 方向: 双向
  - 指标: MACD
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
