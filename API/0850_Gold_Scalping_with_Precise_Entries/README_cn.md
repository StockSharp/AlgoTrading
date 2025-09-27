# 黄金精确进场剥头皮策略
[English](README.md) | [Русский](README_ru.md)

该策略用于黄金剥头皮，结合EMA趋势过滤、RSI区间以及吞没形态。

## 详情

- **入场条件**: EMA趋势过滤，RSI在45到55之间，并在EMA50附近出现多头/空头吞没形态。
- **多空方向**: 双向。
- **退出条件**: 止盈或止损。
- **止损**: 基于ATR的止损和固定点数的止盈。
- **默认值**:
  - `EmaFastPeriod` = 50
  - `EmaSlowPeriod` = 200
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `RsiLower` = 45
  - `RsiUpper` = 55
  - `PipTarget` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 剥头皮
  - 方向: 双向
  - 指标: EMA, RSI, ATR
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
