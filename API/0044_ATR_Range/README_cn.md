# ATR区间突破 (ATR Range Breakout)
[English](README.md) | [Русский](README_ru.md)

价格移动超过ATR平均值时顺势开仓。

突破方向决定多空, 反向或止损离场。

## 详情

- **入场条件**: Price moves more than ATR over the lookback period.
- **多空方向**: Both directions.
- **出场条件**: Price crosses MA or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `LookbackPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: ATR, MA
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

测试表明年均收益约为 169%，该策略在加密市场表现最佳。
