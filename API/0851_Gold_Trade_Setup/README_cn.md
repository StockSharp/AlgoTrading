# Gold Trade Setup 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于考夫曼自适应均线和 SuperTrend 指标。
当 AMA 上升且 SuperTrend 转为上行趋势时卖出。
当 AMA 下降且 SuperTrend 转为下行趋势时买入。

## 详情

- **入场条件**: AMA 方向与 SuperTrend 翻转。
- **多空方向**: 双向。
- **退出条件**: 固定的目标和止损水平。
- **止损**: 是。
- **默认值**:
  - `AmaLength` = 14
  - `FastLength` = 2
  - `SlowLength` = 30
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `TargetMultiplier` = 3.0
  - `RiskMultiplier` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: KAMA, SuperTrend
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
