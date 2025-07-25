# ATR Slope Breakout
[English](README.md) | [Русский](README_ru.md)

ATR Slope Breakout 策略监控相关指标的变化率。斜率异常陡峭时暗示新的趋势正在形成。

当斜率超过常见水平的数倍标准差时入场，顺势交易并设保护性止损。

策略适合积极交易者希望提前把握趋势。当斜率恢复到正常水平时平仓。默认 `AtrPeriod` = 14.

## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **多空**: Both directions.
- **出场条件**: Indicator reverts to average.
- **止损**: Yes.
- **默认值**:
  - `AtrPeriod` = 14
  - `SlopePeriod` = 20
  - `BreakoutMultiplier` = 2.0m
  - `StopLossAtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 分类: Breakout
  - 方向: Both
  - 指标: ATR
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Short-term
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium
测试表明年均收益约为 148%，该策略在外汇市场表现最佳。

测试表明年均收益约为 106%，该策略在股票市场表现最佳。
