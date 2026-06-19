# 波动率偏向模型
[English](README.md) | [Русский](README_ru.md)

统计窗口内上涨和下跌柱的比例, 当方向性偏向与最低波动范围条件同时满足时开仓。利用 ATR 设定止盈止损, 并在达到最大持仓柱数后退出。

## 详情
- **入场条件**: 偏向比率高于 `BiasThreshold` 做多, 低于 `1 - BiasThreshold` 做空, 且范围 > `RangeMin`.
- **多空方向**: 双向。
- **出场条件**: 止损、止盈或达到 `MaxBars`.
- **止损**: 有。
- **默认值**:
  - `BiasWindow` = 10
  - `BiasThreshold` = 0.6
  - `RangeMin` = 0.05
  - `RiskReward` = 2
  - `MaxBars` = 20
  - `AtrLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Volatility
  - 方向: Both
  - 指标: ATR, SMA, Highest, Lowest
  - 止损: Yes
  - 复杂度: Beginner
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
